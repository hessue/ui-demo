using BlockAndDagger.Sound;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace BlockAndDagger
{
    [RequireComponent(typeof(CharacterController))]
    public sealed partial class PlayerControls : MonoBehaviour
    {
        //private static int playerCount = 1;
        private const float GravityValue = -9.81f;
        public float _movementSpeed = 3f;
        private float _controllerDeadzone = 0.1f;
        private float _rotationSpeed = 1000f;
        private CharacterController _characterController;
        private Vector3 _playerVelocity;
        private bool _groundedPlayer;
        private int _controllerNumber;
        private long _playerId;
        private PlayerInputActions _playerInputActions;
        private InputAction _ability1Action;
        private InputAction _ability2Action; 
        private InputAction _moveAction;
        private InputAction _cheatCompleteLevel;
        private InputAction _cheatKillPlayer;
        private InputAction _togglePause;
        private InputAction _ingameMenu;
        private InputAction _continue;
        private bool _wasGrounded = true;
        private Ability ability1;
        private Ability ability2;
        private IMobileAudioManager _m_audioManager;

        [Inject]
        public void Construct(IMobileAudioManager audioManager)
        {
            _m_audioManager = audioManager;
        }
        
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _characterController.enabled = false;
            _playerInputActions  = new PlayerInputActions();
            EnableInGameInputs();
    
            ability1 = new Ability(PlayerAbility.Jump);
            ability2 = new Ability(PlayerAbility.Dash, true, 2);

            SetActions();
        }
  
        private void Start()
        {
            _characterController.enabled = true;
            _playerVelocity = _characterController.velocity;
            _wasGrounded = _characterController.isGrounded;
        }

        private void OnEnable()
        {
            EnableInGameInputs();
            _ability1Action.performed += Ability1;
            _ability2Action.performed += Ability2;
            _togglePause.performed += TogglePause;
#if UNITY_EDITOR
            _cheatCompleteLevel.performed += DevCheatsCompleteLevel;
            //_cheatKillPlayer.performed += DevCheatsKillPlayer;
#endif
        }

        private void OnDisable()
        {
            _ability1Action.performed -= Ability1;
            _ability2Action.performed -= Ability2;
            _togglePause.performed -= TogglePause;
#if UNITY_EDITOR
            _cheatCompleteLevel.performed -= DevCheatsCompleteLevel;
            //_cheatKillPlayer.performed -= DevCheatsKillPlayer;
#endif
            DisableInGameInputs();
        }

        public void Ability1(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && ability1._allowAbility)
            {
                UseAbility(ability1);
            }
        }
        
        public void Ability2(InputAction.CallbackContext ctx)
        {
            if (ctx.performed && ability2._allowAbility)
            {
                UseAbility(ability2);
            }
        }
        
        public void TogglePause(InputAction.CallbackContext ctx)
        {
            //TODO: GameUI Onclick should be enough
            /*if (ctx.performed)
            {
                Debug.Log("Pause performed");
                GameManager.Instance.ToggleInGamePause();
            }*/
        }

        public void ToggleIngameMenu(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                Debug.Log("IngameMenu performed");
                GameManager.Instance.IngameUI.ToggleMenuButtonClicked();
            }
        }

        private void ApplyManualGravity()
        {
            _playerVelocity.y += GravityValue * Time.deltaTime;
        }
        
        void Update()
        {
            ApplyManualGravity();
           
            _groundedPlayer = _characterController.isGrounded;
            if (_groundedPlayer && _playerVelocity.y < 0.1f)
            {
                _playerVelocity.y = 0f;
            }

            MovePlayer();
            RotateTowardsVector();
            
            ability1.UpdateAbilityLimiter(_groundedPlayer);
            ability2.UpdateAbilityLimiter(_groundedPlayer);
            UpdateDash();
            UpdateSimpleJumpPostState();
        }

#if UNITY_EDITOR
        private void DevCheatsCompleteLevel(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                GameManager.Instance.AudioManager.StopMusic();
                GameManager.Instance.ProceedToNextLevel();
            }
        }
        
        private void DevCheatsKillPlayer(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                GameManager.Instance.AudioManager.StopMusic();
                GameManager.Instance.Players[0].Kill("Cheats Death");
            }
        }
#endif
        
        private void MovePlayer()
        {
            // Build a displacement vector and use CharacterController.Move once
            Vector3 displacement = Vector3.zero;
            Vector3 verticalDisplacement = Vector3.up * (_playerVelocity.y * Time.deltaTime);

            if (_isDashing)
            {
                // move up to dashSpeed * dt, but don't exceed remaining distance
                float step = Mathf.Min(_dashRemainingDistance, _dashSpeed * Time.deltaTime);
                displacement = _dashDirection * step; // displacement in meters this frame
                _dashRemainingDistance -= step;
                if (_dashRemainingDistance <= 0f)
                {
                    _isDashing = false;
                    _dashDirection = Vector3.zero;
                    _dashTimeRemaining = 0f;
                    _dashSpeed = 0f;
                }
            }
            else
            {
                var moveDir = _moveAction.ReadValue<Vector2>();
                // Target horizontal velocity only
                Vector3 horizontal = new Vector3(moveDir.x, 0f, moveDir.y) * _movementSpeed;

                // Apply slowdown multiplier if in post-jump slow state (only to horizontal)
                if (_isPostJumpSlowed)
                {
                    float reductionPct = Mathf.Clamp(PostJumpSlowReducePercent, 0f, 100f);
                    float speedMultiplier = Mathf.Clamp01(1f - (reductionPct / 100f));
                    horizontal *= speedMultiplier;
                }

                displacement = horizontal * Time.deltaTime;
            }

            CollisionFlags flags = _characterController.Move(displacement + verticalDisplacement);
            
            // If we were dashing and hit a side wall, stop the dash early
            if (_isDashing && (flags & CollisionFlags.Sides) != 0)
            {
                _isDashing = false;
                _dashDirection = Vector3.zero;
                _dashTimeRemaining = 0f;
                _dashRemainingDistance = 0f;
                _dashSpeed = 0f;
            }
        }

        private void RotateTowardsVector()
        {
            var moveDir = _moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(moveDir.x) > _controllerDeadzone ||
                Mathf.Abs(moveDir.y) > _controllerDeadzone)
            {
                Vector3 playerDirection = Vector3.right * moveDir.x + Vector3.forward * moveDir.y;
                if (playerDirection.sqrMagnitude > 0.0f)
                {
                    Quaternion newRotation = Quaternion.LookRotation(playerDirection, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, newRotation,
                        _rotationSpeed * Time.deltaTime);
                }
            }
        }

#region InputSetup


        private void SetActions()
        {
#if UNITY_EDITOR
            _playerInputActions.AllActions.Enable();
            _moveAction = _playerInputActions.AllActions.Move;
            _ability1Action = _playerInputActions.AllActions.Ability1Button;
            _ability2Action = _playerInputActions.AllActions.Ability2Button;
            _togglePause = _playerInputActions.AllActions.TogglePause;
            //_ingameMenu = _playerInputActions.AllActions.IngameMenu;
            //_continue = _playerInputActions.AllActions.Continue;
            _cheatCompleteLevel = _playerInputActions.AllActions.CheatCompleteLevel;
                    
            //TestingMobileUI();
               
#else
                     _playerInputActions.UI.Enable();
                    _moveAction = _playerInputActions.UI.Move;
                    _ability1Action = _playerInputActions.UI.Ability1Button;
                    _ability2Action = _playerInputActions.UI.Ability2Button;
                   // _togglePause = _playerInputActions.UI.Pause;
                    _ingameMenu = _playerInputActions.UI.IngameMenu;
                    _continue = _playerInputActions.UI.Continue;
#endif
        }

        private void TestingMobileUI()
        {
            _moveAction = _playerInputActions.UI.Move;
            _ability1Action = _playerInputActions.UI.Ability1Button;
            _ability2Action = _playerInputActions.UI.Ability2Button;
            _togglePause = _playerInputActions.UI.TogglePause;
            _ingameMenu = _playerInputActions.UI.IngameMenu;
            _continue = _playerInputActions.UI.Continue;
        }

        public void EnableInGameInputs()
        {
            // Disable all actions at start as recommended
            _playerInputActions.AllActions.Disable();
            _playerInputActions.UI.Disable();
            
#if UNITY_EDITOR
            _playerInputActions.AllActions.Enable();
           // _playerInputActions.UI.Enable(); //testing line
#else
            _playerInputActions.UI.Enable();
#endif
        }

        ///Used example on Pause
        public void DisableInGameInputs()
        {
#if UNITY_EDITOR
            _playerInputActions.AllActions.Disable();
#else
            _playerInputActions.UI.Disable();
#endif
        }
        
#endregion
        
        
    }
}
