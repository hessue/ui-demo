using UnityEngine;

namespace BlockAndDagger
{
    public class EnemyController : MonoBehaviour
    {
        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _grounded;
        //public float _movementSpeed = 3f;
        private float _gravityValue = -9.81f;

        public bool IsGrounded => _grounded;
        
        public void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        void Update()
        {
            _grounded = _characterController.isGrounded;
            if (_grounded && _velocity.y < 0.1f)
            {
                _velocity.y = 0f;
            }
            else
            {
                _velocity.y += _gravityValue * Time.deltaTime;
            }
            
            //TODO: confuses NavMeshAgent, unify movement
            _characterController.Move(_velocity * Time.deltaTime);
        }
    }
}
