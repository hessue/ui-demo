using UnityEngine;

namespace BlockAndDagger
{
    public sealed partial class PlayerControls
    { 
    //DASH
        /// Dash-related fields (distance-driven; no Rigidbody-based!!)
        private bool _isDashing = false;
        private Vector3 _dashDirection = Vector3.zero;
        private float _dashRemainingDistance = 0f;
        /// meters per second
        private float _dashSpeed = 0f;
        private float _dashTimeRemaining = 0f;
        /// meters
        private float _dashDistance = 1f;
        /// seconds
        private float _dashDuration = 0.18f;
        private float _postJumpSlowRemaining = 0f;
        private bool _isPostJumpSlowed = false;
        
    //JUMP
        private bool _awaitingLanding = false;
        /// Reduce base horizontal speed on landing (0..100 slower)
        private const float PostJumpSlowReducePercent = 40f;
        private const float PostJumpSlowDurationSeconds = 0.4f;
        /// Experimental feature. Default value should be high enough to get over fence or even an enemy, but not on top of wall
        private const float _jumpHeight = 0.5f;
        
        private void UseAbility(Ability ability)
        {
            Debug.Log($"{nameof(ability)} ' ' {ability.PlayerAbility} performed");
            switch (ability.PlayerAbility)
            {
                case PlayerAbility.Clide:
                    break;
                case PlayerAbility.Speed:
                    break;
                case PlayerAbility.Jump:
                    SimpleJump(ability);
                    break;
                case PlayerAbility.Dash:
                    Dash(ability); 
                    break;
                case PlayerAbility.Attack:
                    break;
                case PlayerAbility.Tnt:
                    break;
                case PlayerAbility.Mine:
                    break;
            }
        }
        
        private void SimpleJump(Ability ability)
        {
            if (_groundedPlayer)
            {
                _m_audioManager.PlaySFX("jump", 0.1f);
                if (ability.HasLimitUsage)
                {
                    ability._allowAbility = false;
                }

                _playerVelocity.y = Mathf.Sqrt(_jumpHeight * -3.0f * GravityValue);
                _awaitingLanding = true;
            }
        }
        
        private void UpdateSimpleJumpPostState()
        {
            if (_awaitingLanding && _groundedPlayer && !_wasGrounded)
            {
                _isPostJumpSlowed = true;
                _postJumpSlowRemaining = PostJumpSlowDurationSeconds;
                _awaitingLanding = false;
            }

            if (_isPostJumpSlowed)
            {
                _postJumpSlowRemaining -= Time.deltaTime;
                if (_postJumpSlowRemaining <= 0f)
                {
                    _isPostJumpSlowed = false;
                    _postJumpSlowRemaining = 0f;
                }
            }

            _wasGrounded = _groundedPlayer;
        }

        private void Dash(Ability ability)
        {
            if (ability.HasLimitUsage)
            {
                ability._allowAbility = false;
            }

            if (_isDashing) {
                return;
            }
            
            _m_audioManager.PlaySFX("dash", 1f);
            
            Vector3 faceDir = transform.forward;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude < 0.0001f)
            {
                faceDir = (transform.rotation * Vector3.forward);
                faceDir.y = 0f;
            }
            faceDir.Normalize();
            
            float appliedDistance = Mathf.Max(0f, _dashDistance);
            if (appliedDistance < 0.01f)
            {
                appliedDistance = 0.3f;
            }

            // compute speed so the appliedDistance is covered in dashDuration
            float speed = (_dashDuration > 0f) ? (appliedDistance / _dashDuration) : appliedDistance;

            // prefer movement input direction if present
            Vector3 dashDir = faceDir;
            if (_moveAction != null)
            {
                Vector2 mv = _moveAction.ReadValue<Vector2>();
                if (mv.sqrMagnitude > (_controllerDeadzone * _controllerDeadzone))
                {
                    dashDir = new Vector3(mv.x, 0f, mv.y).normalized;
                }
            }

            _dashDirection = (dashDir.sqrMagnitude > 0.0001f) ? dashDir : faceDir;
            _dashRemainingDistance = appliedDistance;
            _dashSpeed = speed;
            _dashTimeRemaining = _dashDuration;
            _isDashing = true;
        }
        
        private void UpdateDash()
        {
            if (_isDashing)
            {
                _dashTimeRemaining -= Time.deltaTime;
                if (_dashTimeRemaining <= 0f)
                {
                    _isDashing = false;
                    _dashDirection = Vector3.zero;
                    _dashTimeRemaining = 0f;
                    _dashSpeed = 0f;
                }
            }
        }
    }
}
