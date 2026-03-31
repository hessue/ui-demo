using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockAndDagger.MenuControls
{
    public class UISwipeDetector
    {
        public bool IsPointerDown { get; private set; }
        public Vector2 LastPointerDelta { get; private set; }
        private Vector2 _currentPointerScreenPos;
        private Vector2 _previousPointerScreenPos;
        private readonly float _minimumSwipeDistancePixels;
        private readonly float _pointerSmoothTime;
        private RectTransform _swipeArea;

        // Internal state
        private Vector2 _smoothedPointerDelta = Vector2.zero;
        private Vector2 _pointerDeltaVelocity = Vector2.zero;
        private bool _pointerStartedInSwipeArea;
        private bool _wasPointerDownLastFrame;

        public UISwipeDetector(RectTransform swipeArea, float minimumSwipeDistancePixels, float pointerSmoothTime)
        {
            _swipeArea = swipeArea;
            _minimumSwipeDistancePixels = minimumSwipeDistancePixels;
            _pointerSmoothTime = pointerSmoothTime;
            IsPointerDown = false;
            LastPointerDelta = Vector2.zero;
            _currentPointerScreenPos = Vector2.zero;
            _previousPointerScreenPos = Vector2.zero;
        }

        public void Update(Camera cam)
        {
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (GameManager.Instance.MenuManager.CurrentState != MenuState.LevelMaker)
            {
                ResetState();
                return;
            }

            bool mousePressed = Mouse.current != null && Mouse.current.leftButton != null && Mouse.current.leftButton.isPressed;
            bool touchPressed = false;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var t in touchscreen.touches)
                {
                    if (t != null && t.press != null && t.press.isPressed)
                    {
                        touchPressed = true;
                        break;
                    }
                }
            }
            bool currentlyPressed = mousePressed || touchPressed;

            if (!currentlyPressed)
            {
                ResetState();
                return;
            }

            if (!_wasPointerDownLastFrame)
            {
                Vector2 initPos = Vector2.zero;
                if (mousePressed && Mouse.current.position != null)
                {
                    initPos = Mouse.current.position.ReadValue();
                }
                else if (touchPressed)
                {
                    // Find the first active touch and use its position
                    foreach (var t in touchscreen.touches)
                    {
                        if (t != null && t.press != null && t.press.isPressed)
                        {
                            initPos = t.position.ReadValue();
                            break;
                        }
                    }
                }

                _currentPointerScreenPos = initPos;

                if (_swipeArea == null)
                {
                    _pointerStartedInSwipeArea = true;
                }
                else
                {
                    Canvas parentCanvas = _swipeArea.GetComponentInParent<Canvas>();
                    Camera raycastCam = cam;
                    if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        raycastCam = null;
                    }

                    _pointerStartedInSwipeArea = RectTransformUtility.RectangleContainsScreenPoint(_swipeArea, initPos, raycastCam);
                }

                _smoothedPointerDelta = Vector2.zero;
                _pointerDeltaVelocity = Vector2.zero;
            }

            _wasPointerDownLastFrame = true;

            IsPointerDown = (_swipeArea == null || _pointerStartedInSwipeArea);

            if (!IsPointerDown)
            {
                LastPointerDelta = Vector2.zero;
                return;
            }

            Vector2 currentPos = Vector2.zero;
            if (mousePressed && Mouse.current != null && Mouse.current.position != null)
            {
                currentPos = Mouse.current.position.ReadValue();
            }
            else if (touchPressed)
            {
                foreach (var t in touchscreen.touches)
                {
                    if (t != null && t.press != null && t.press.isPressed)
                    {
                        currentPos = t.position.ReadValue();
                        break;
                    }
                }
            }

            _previousPointerScreenPos = _currentPointerScreenPos;
            Vector2 rawDelta = currentPos - _previousPointerScreenPos;
            _currentPointerScreenPos = currentPos;

            float maxReasonableDelta = Mathf.Max(Screen.width, Screen.height) * 0.75f;
            if (rawDelta.magnitude > maxReasonableDelta)
            {
                rawDelta = Vector2.zero;
            }

            if (Mathf.Abs(rawDelta.x) < _minimumSwipeDistancePixels && Mathf.Abs(rawDelta.y) < _minimumSwipeDistancePixels)
            {
                rawDelta = Vector2.zero;
            }

            _smoothedPointerDelta = Vector2.SmoothDamp(_smoothedPointerDelta, rawDelta, ref _pointerDeltaVelocity, _pointerSmoothTime);
            LastPointerDelta = _smoothedPointerDelta;
        }

        private void ResetState()
        {
            IsPointerDown = false;
            LastPointerDelta = Vector2.zero;
            _pointerStartedInSwipeArea = false;
            _wasPointerDownLastFrame = false;
            _smoothedPointerDelta = Vector2.zero;
            _pointerDeltaVelocity = Vector2.zero;
            _currentPointerScreenPos = Vector2.zero;
            _previousPointerScreenPos = Vector2.zero;
        }
    }
}

