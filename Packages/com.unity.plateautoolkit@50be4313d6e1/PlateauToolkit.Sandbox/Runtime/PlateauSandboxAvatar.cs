using UnityEngine;

namespace PlateauToolkit.Sandbox
{
    [SelectionBase]
    public class PlateauSandboxAvatar :
        MonoBehaviour,
        IPlateauSandboxMovingObject
    {
        const float k_LerpSpeed = 0.5f;

        [SerializeField] Animator m_Animator;
        [SerializeField] Collider m_Collider;

        public Collider Collider => m_Collider;

        float m_Speed;

        public void SetPosition(in Vector3 position)
        {
            transform.position = position;
        }

        public void OnMoveBegin()
        {
            if (m_Animator == null)
            {
                return;
            }
            m_Animator.SetBool("IsWalking", true);
        }

        public void OnMove(Vector3 position, float velocity, float moveDelta, Vector3 trackUp, Vector3 trackForward, Vector3 lookaheadForward)
        {
            m_Speed = moveDelta / Time.deltaTime;
            transform.forward = Vector3.Lerp(transform.forward, trackForward, Time.deltaTime);

            SetPosition(position);
        }

        public void OnMoveEnd()
        {
            m_Speed = 0;

            if (m_Animator == null)
            {
                return;
            }
            m_Animator.SetBool("IsWalking", false);
        }

        void LateUpdate()
        {
            if (m_Animator == null)
            {
                return;
            }
            float currentMoveSpeed = m_Animator.GetFloat("MoveSpeed");
            m_Animator.SetFloat(
                "MoveSpeed",
                Mathf.Lerp(currentMoveSpeed, m_Speed, Time.deltaTime * k_LerpSpeed));
        }
    }
}