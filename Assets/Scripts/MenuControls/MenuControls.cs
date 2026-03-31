using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BlockAndDagger.MenuControls
{
    public sealed class MenuControls : MonoBehaviour
    {
        //TODO: remove [SerializeField] once good values
        [SerializeField] private float m_dragMultiplier = 0.5f;
        [SerializeField] private float m_minimumSwipeDistancePixels = 10f;
        [SerializeField] private float m_dragSensitivity = 0.02f;
        [SerializeField] private float m_pointerSmoothTime = 0.03f;
        [SerializeField] private float m_smoothTime = 0.08f;
        private float m_groundPlaneY;
        private InputAction _swipeAction;
        private bool _isPointerDown;
        private Vector2 _lastPointerDelta = Vector2.zero;
        private Vector3 _smoothedVelocity = Vector3.zero;
        private Vector3 _velocityRef = Vector3.zero;
        private Camera _cam;
        private UISwipeDetector _uiSwipeDetector;
        // If no swipe area is defined, treat presses as always valid
        private RectTransform _swipeArea;
        private bool m_detectionEnabled = false;
        
        public MenuInputActions MenuInputActions { get; private set; }

        private void Awake()
        {
            MenuInputActions = new MenuInputActions();
            _swipeAction = MenuInputActions.Menu.Swipe;
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().name == Constants.LevelSelectionSceneName)
            {
                _swipeArea = GameManager.Instance.MenuManager.LevelMakerUI.SwipeArea.GetComponent<RectTransform>();
                _cam = Camera.main;
                _uiSwipeDetector = new UISwipeDetector(_swipeArea, m_minimumSwipeDistancePixels, m_pointerSmoothTime);
            }
        }

        private void OnEnable()
        {
            if (MenuInputActions == null)
            {
                MenuInputActions = new MenuInputActions();
            }

            MenuInputActions.Menu.Enable();
            _swipeAction = MenuInputActions.Menu.Swipe;
            _swipeAction.Enable();
        }

        private void OnDisable()
        {
            if (_swipeAction != null)
            {
                _swipeAction.Disable();
            }

            if (MenuInputActions != null)
            {
                MenuInputActions.Menu.Disable();
            }
        }

        private void Update()
        {
            //TODO: make a scene loaded and initialized event and call this
            m_detectionEnabled = GameManager.Instance.MenuManager != null && GameManager.Instance.MenuManager.CurrentState == MenuState.LevelMaker;
            if (m_detectionEnabled)
            {
                _uiSwipeDetector?.Update(_cam);
                _isPointerDown = _uiSwipeDetector != null && _uiSwipeDetector.IsPointerDown;
                _lastPointerDelta = _uiSwipeDetector != null ? _uiSwipeDetector.LastPointerDelta : Vector2.zero;

                UpdateMovement();
            }
        }

        private void UpdateMovement()
        {
            if (!_isPointerDown || _lastPointerDelta == Vector2.zero)
            {
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-6f);

            // Use detector's pointer delta (screen pixels) and map to camera axes.
            Vector3 right = _cam.transform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = _cam.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float scalar = m_dragSensitivity * m_dragMultiplier / dt;
            Vector3 desiredVelocity = right * (_lastPointerDelta.x * scalar) + forward * (_lastPointerDelta.y * scalar);
            _smoothedVelocity = Vector3.SmoothDamp(_smoothedVelocity, desiredVelocity, ref _velocityRef, m_smoothTime);
            _cam.transform.position += _smoothedVelocity * dt;
        }
    }
}