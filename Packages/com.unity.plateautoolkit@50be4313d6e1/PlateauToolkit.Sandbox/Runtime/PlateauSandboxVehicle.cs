using UnityEngine;

namespace PlateauToolkit.Sandbox
{
    public interface IPlateauSandboxPlaceableObject
    {
        /// <summary>
        /// Set position of the object.
        /// </summary>
        void SetPosition(in Vector3 position);

        Collider Collider { get; }
    }

    /// <summary>
    /// The definition of a vehicle
    /// </summary>
    [SelectionBase]
    public class PlateauSandboxVehicle :
        MonoBehaviour,
        IPlateauSandboxMovingObject
    {
        /// <summary>
        /// The list of <see cref="Transform" /> of wheels.
        /// </summary>
        [SerializeField] Transform[] m_Wheels;

        /// <summary>
        /// The radius of the wheels [m].
        /// </summary>
        [SerializeField] float m_WheelRadius = 0.3f;

        [SerializeField] Transform m_BackWheelAxisTransform;

        /// <summary>
        /// The length of wheelbase [m].
        /// </summary>
        [SerializeField] float m_Wheelbase = 2.5f;

        /// <summary>
        /// The main collider of the vehicle.
        /// </summary>
        [SerializeField] Collider m_Collider;

        public Collider Collider => m_Collider;

        public void SetPosition(in Vector3 position)
        {
            transform.position = position - (m_BackWheelAxisTransform.position - transform.position);
        }

        public void OnMove(Vector3 position, float velocity, float moveDelta, Vector3 up, Vector3 trackForward, Vector3 lookAhead)
        {
            // Align the object to the spline.
            transform.forward = Vector3.Lerp(transform.forward, trackForward, Time.deltaTime);

            SetPosition(position);

            SpinWheels(moveDelta);
        }

        /// <summary>
        /// Spin wheels.
        /// </summary>
        /// <param name="moveDelta">the delta how long the vehicle moves</param>
        void SpinWheels(float moveDelta)
        {
            if (m_WheelRadius <= 0)
            {
                return;
            }

            float r = 360 * moveDelta / (2 * Mathf.PI * m_WheelRadius);
            foreach (Transform wheelTransform in m_Wheels)
            {
                wheelTransform.Rotate(new Vector3(r, 0, 0));
            }
        }
    }
}