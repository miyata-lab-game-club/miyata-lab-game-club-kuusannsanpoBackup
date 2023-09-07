using UnityEngine;

namespace PlateauToolkit.Sandbox
{
    /// <summary>
    /// The definition of a prop
    /// </summary>
    [SelectionBase]
    public class PlateauSandboxProp : MonoBehaviour, IPlateauSandboxPlaceableObject
    {
        [SerializeField] Collider m_Collider;

        public Collider Collider => m_Collider;

        public void SetPosition(in Vector3 position)
        {
            transform.position = position;
        }
    }
}