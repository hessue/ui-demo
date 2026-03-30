using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;

namespace BlockAndDagger
{
    internal sealed class TouchPointerInputInternal : IPointerInput
    {
        private MenuInputActions _menuInputActions;
        private Camera _cam;
        private int _layerMask;
        private bool _touchPressed;
        private bool _touchReleased;
        private const float RaycastMaxDistanceLocal = 100f;

        public void Init(MenuInputActions menuInputActions, Camera cam, int layerMask)
        {
            _menuInputActions = menuInputActions;
            _cam = cam;
            _layerMask = layerMask;
            EnhancedTouchSupport.Enable();
            if (_menuInputActions != null)
            {
                _menuInputActions.Menu.Enable();
                _menuInputActions.Menu.SelectTarget.performed += OnPressStarted;
                _menuInputActions.Menu.SelectTarget.canceled += OnPressEnded;
                _menuInputActions.Menu.MousePos.Enable();
            }
        }

        private void OnPressStarted(InputAction.CallbackContext ctx)
        {
            _touchPressed = true;
        }

        private void OnPressEnded(InputAction.CallbackContext ctx)
        {
            _touchReleased = true;
        }

        public bool TryGetHit(out Block block, bool inGame)
        {
            block = null;
            if (_touchPressed && _touchReleased)
            {
                Vector2 pos = Vector2.zero;
                if (_menuInputActions != null)
                {
                    pos = _menuInputActions.Menu.MousePos.ReadValue<Vector2>();
                }
                else if (Input.touchCount > 0)
                {
                    pos = Input.GetTouch(Input.touchCount - 1).position;
                }

                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    ResetPressState();
                    return false;
                }

                Ray ray = _cam.ScreenPointToRay(new Vector3(pos.x, pos.y, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, RaycastMaxDistanceLocal, _layerMask))
                {
                    var tile = hit.transform.GetComponent<Block>();
                    if (tile != null)
                    {
                        if (!inGame || tile.Data.isBluePrintBlock)
                        {
                            block = tile;
                            ResetPressState();
                            return true;
                        }
                    }
                }

                ResetPressState();
            }

            // Use Input System Enhanced Touch as primary source
            var activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            if (activeTouches.Count > 0)
            {
                for (int i = 0; i < activeTouches.Count; i++)
                {
                    var t = activeTouches[i];
                    if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                    {
                        int touchId = t.touchId;
                        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId))
                        {
                            return false;
                        }

                        Vector2 screenPos = t.screenPosition;
                        Ray ray = _cam.ScreenPointToRay(screenPos);
                        if (Physics.Raycast(ray, out RaycastHit hit, RaycastMaxDistanceLocal, _layerMask))
                        {
                            var tile = hit.transform.GetComponent<Block>();
                            if (tile != null)
                            {
                                if (!inGame || tile.Data.isBluePrintBlock)
                                {
                                    block = tile;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void ResetPressState()
        {
            _touchPressed = false;
            _touchReleased = false;
        }

        public void Dispose()
        {
            if (_menuInputActions != null)
            {
                _menuInputActions.Menu.SelectTarget.performed -= OnPressStarted;
                _menuInputActions.Menu.SelectTarget.canceled -= OnPressEnded;
                _menuInputActions.Menu.MousePos.Disable();
                _menuInputActions.Menu.SelectTarget.Disable();
            }
        }
    }
}

