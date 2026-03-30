using UnityEngine;

namespace BlockAndDagger
{
    public sealed class FollowCamera : MonoBehaviour
    {
        public Transform m_target;
        /// <summary>
        /// Good values something like y = 20, z = -7
        /// </summary>
        public Vector3 m_Offset;
        private float _smoothTime = 0.3f;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _targetPos; 

        public void Init(Transform targetToFollow)
        {
            m_target = targetToFollow;
        }
        
        private void LateUpdate()
        {
            if (m_target is null)
            {
                return;
            }
            
            _targetPos = m_target.position;
            Vector3 heightenedTarget = new Vector3(_targetPos.x + m_Offset.x, _targetPos.y + m_Offset.y, _targetPos.z + m_Offset.z);
            transform.position = Vector3.SmoothDamp(transform.position, heightenedTarget, ref _velocity, _smoothTime);
        }
    }
}











