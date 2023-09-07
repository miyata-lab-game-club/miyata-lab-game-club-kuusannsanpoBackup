using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Sandbox
{
    /// <summary>
    /// Define an moveable object.
    /// </summary>
    interface IPlateauSandboxMovingObject : IPlateauSandboxPlaceableObject
    {
        /// <summary>
        /// An event when movement began.
        /// </summary>
        void OnMoveBegin()
        {
        }

        /// <summary>
        /// An event when movement is updated.
        /// </summary>
        /// <remarks>
        /// The position is updated by <see cref="PlateauSandboxTrackMovement" /> using <see cref="SetPosition(in Vector3)" />,
        /// Then define behavior of the object in this interface.
        /// </remarks>
        /// <param name="position"></param>
        /// <param name="velocity">The current velocity of the object</param>
        /// <param name="moveDelta">The delta of distance that the object moves in a frame</param>
        /// <param name="trackUp">The up </param>
        /// <param name="trackForward">The forward direction of a point in the track where the object is placed curretnly</param>
        /// <param name="lookaheadForward">The forward to a current lookahead point</param>
        void OnMove(Vector3 position, float velocity, float moveDelta, Vector3 trackUp, Vector3 trackForward, Vector3 lookaheadForward);

        /// <summary>
        /// An event when movement ends.
        /// </summary>
        void OnMoveEnd()
        {
        }
    }

    [ExecuteAlways]
    class PlateauSandboxTrackMovement : MonoBehaviour, IPlateauSandboxTrackRunner
    {
        [SerializeField] PlateauSandboxTrack m_Track;

        /// <summary>Max velocity (m/s)</summary>
        [SerializeField] float m_MaxVelocity;

        /// <summary>Offset from the position of <see cref="m_Track" /></summary>
        [SerializeField] Vector3 m_TrackOffset;

        /// <summary>Offset from the current position where is the origin of collision detection</summary>
        // [SerializeField] Vector3 m_CollisionDetectOriginOffset = new Vector3(0, 0, 3f);

        /// <summary>Size of a sphere of collision detection</summary>
        [SerializeField] float m_CollisionDetectRadius = 0.5f;

        /// <summary>Size of a sphere of collision detection</summary>
        [SerializeField] float m_CollisionDetectHeight = 0.5f;

        /// <summary>Distance of collision detection from its origin</summary>
        [SerializeField] float m_MinCollisionDetectDistance = 5f;

        [SerializeField] bool m_RunOnAwake = true;
        [SerializeField] bool m_IsPaused;

        [HideInInspector]
        /// <summary></summary>
        [SerializeField] float m_SplineContainerT;

        [HideInInspector]
        [SerializeField] Object m_SerializedMovingObject;

        Coroutine m_RandomWalkCoroutine;

        IPlateauSandboxMovingObject m_MovingObject;

        float m_CurrentVelocity;
        float m_MoveDelta;

        public float CurrentVelocity => m_CurrentVelocity;

        public bool HasTrack => m_Track != null;

        public PlateauSandboxTrack Track
        {
            set => m_Track = value;
        }

        public bool IsMoving => m_RandomWalkCoroutine != null;

        public float SplineContainerT => m_SplineContainerT;

        public float MaxSplineContainerT => m_Track.MaxSplineContainerT;

        public bool IsPaused { get => m_IsPaused; set => m_IsPaused = value; }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (m_Track == null)
            {
                m_SplineContainerT = 0f;
                return;
            }
            m_SplineContainerT = Mathf.Clamp(m_SplineContainerT, 0, m_Track.MaxSplineContainerT);

            ApplyPosition();
        }
#endif

        void SetUpTarget()
        {
            TryGetComponent(out IPlateauSandboxMovingObject movingObject);

            if (movingObject == null)
            {
                Debug.LogError("移動制御が可能なオブジェクトが設定されていません");
                return;
            }

            m_MovingObject = movingObject;

            if ((Object)movingObject == m_SerializedMovingObject)
            {
                return;
            }

            switch (movingObject)
            {
                case PlateauSandboxVehicle vehicle:
                    m_MaxVelocity = 20;
                    m_SerializedMovingObject = vehicle;
                    break;
                case PlateauSandboxAvatar avatar:
                    m_MaxVelocity = 1.5f;
                    m_SerializedMovingObject = avatar;
                    break;
            }
        }

        void Awake()
        {
            SetUpTarget();

            if (!Application.isPlaying)
            {
                return;
            }
        }

        void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_RunOnAwake)
            {
                StartRandomWalk();
            }
        }

        internal void ApplyPosition()
        {
            if (m_MovingObject == null)
            {
                SetUpTarget();
            }

            (Vector3 position, Vector3 forward, Vector3 up) = m_Track.GetTransformBySplineContainerT(m_SplineContainerT);

            // Apply the offset.
            position += transform.right * m_TrackOffset.x;
            position += transform.up * m_TrackOffset.y;
            position += transform.forward * m_TrackOffset.z;

            m_MovingObject.SetPosition(position);
            transform.rotation = Quaternion.LookRotation(forward, up);
        }

        public bool TrySetSplineContainerT(float t)
        {
            if (IsMoving)
            {
                return false;
            }

            m_SplineContainerT = t;
            ApplyPosition();
            return true;
        }

        [ContextMenu("Start Movement")]
        public void StartRandomWalk()
        {
            if (m_RandomWalkCoroutine != null)
            {
                Debug.LogWarning("既に移動を開始しています");
                return;
            }
            if (m_Track == null)
            {
                return;
            }
            if (m_MovingObject == null)
            {
                Debug.LogError("移動に対応したオブジェクトにアタッチされていません");
                return;
            }

            m_MovingObject.OnMoveBegin();

            m_IsPaused = false;
            m_RandomWalkCoroutine = StartCoroutine(RandomWalkEnumerator());
        }

        [ContextMenu("Stop Movement")]
        public void Stop()
        {
            if (m_RandomWalkCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_RandomWalkCoroutine);
            m_RandomWalkCoroutine = null;
            m_MovingObject.OnMoveEnd();

            m_IsPaused = false;
        }

        float IPlateauSandboxTrackRunner.GetMoveDelta()
        {
            return m_MoveDelta;
        }

        float IPlateauSandboxTrackRunner.GetCollisionDistance()
        {
            // The value depends on velocity of the moving object.
            return Mathf.Max(m_CurrentVelocity, m_MinCollisionDetectDistance);
        }

        /// <summary>
        /// <see cref="IEnumerator" /> to move along <see cref="PlateauSandboxTrack" />
        /// </summary>
        IEnumerator RandomWalkEnumerator()
        {
            // Prepare a random seed which decides which paths the enuerator chooses.
            int seed = Random.Range(int.MinValue, int.MaxValue);

            // Enumerator positions of movement along a track.
            IEnumerator<(float, float)> moveEnumerator = m_Track.GetRandomWalkWithCollision(m_SplineContainerT, seed, this);
            while (true)
            {
                // Move to the next position.
                if (!moveEnumerator.MoveNext())
                {
                    break;
                }

                while (m_IsPaused)
                {
                    yield return null;
                }

                (float t, float collisionT) = moveEnumerator.Current;
                m_SplineContainerT = t;

                (Vector3 collisionPoint, Vector3 collisionUp) = m_Track.GetPositionAndUpBySplineContainerT(collisionT);

                Vector3 lookaheadPoint = collisionPoint + collisionUp * m_CollisionDetectHeight;
                (Vector3 trackPosition, Vector3 trackForward, Vector3 trackUp) = m_Track.GetTransformBySplineContainerT(t);

                // Apply the offset.
                trackPosition += Vector3.Cross(trackUp, trackForward) * m_TrackOffset.x;
                trackPosition += trackUp * m_TrackOffset.y;
                trackPosition += trackForward * m_TrackOffset.z;

                // Calculate lookahead forward and an angle between the forward and transform.forward.
                Vector3 lookaheadForward = lookaheadPoint - transform.position;
                lookaheadForward.Normalize();
                float lookaheadAngle = Vector3.Angle(lookaheadForward, transform.forward);

                // Avoid having ratio = 0.
                float minVelocityRatio = 0.01f;

                // Calculate max velocity depending on the state of movement.
                float velocityRatio = Mathf.Max(1 - Mathf.Clamp01(lookaheadAngle / 90), minVelocityRatio);

                // Current Max Velocity
                float maxVelocity = m_Track.SpeedLimit == null ?
                    m_MaxVelocity : Mathf.Min(m_MaxVelocity, m_Track.SpeedLimit.Value);

                float maxCurrentMaxVelocity = maxVelocity * velocityRatio;
                float timeScale = 1f;

                // Calculate current velcoity of the moving object and move delta on a frame.
                // (IMPORTANT) These values will be used as parameters for the movement enumerator through the interface.
                m_CurrentVelocity = Mathf.Lerp(m_CurrentVelocity, maxCurrentMaxVelocity, Time.deltaTime * timeScale);
                m_MoveDelta = m_CurrentVelocity * Time.deltaTime;

                m_MovingObject.OnMove(
                    trackPosition, m_CurrentVelocity, m_MoveDelta, trackUp, trackForward, lookaheadForward);

                // Set position of the moving object.
                // m_MovingObject.SetPosition(trackPosition);

                yield return null;
            }

            Stop();
        }
    }
}