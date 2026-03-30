using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BlockAndDagger.UI
{
    internal sealed class MousePointerInputInternal : IPointerInput
    {
        private MenuInputActions _menuInputActions;
        private Camera _cam;
        private int _layerMask;
        private bool _leftPressed;
        private bool _leftReleased;
        private const float RaycastMaxDistanceLocal = 100f;

        public void Init(MenuInputActions menuInputActions, Camera cam, int layerMask)
        {
            _menuInputActions = menuInputActions;
            _cam = cam;
            _layerMask = layerMask;
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
            _leftPressed = true;
        }

        private void OnPressEnded(InputAction.CallbackContext ctx)
        {
            _leftReleased = true;
        }

        public bool TryGetHit(out Block block, bool inGame)
        {
            block = null;
            if (_leftPressed && _leftReleased)
            {
                Vector2 mousePos2;
                if (Mouse.current != null)
                {
                    mousePos2 = Mouse.current.position.ReadValue();
                    Debug.Log($"Mouse position from Input System: {mousePos2}");
                }
                else
                {
                    mousePos2 = UnityEngine.Input.mousePosition;
                    Debug.Log($"Mouse position from Input System: {mousePos2}");
                }

                Ray ray = _cam.ScreenPointToRay(new Vector3(mousePos2.x, mousePos2.y, 0f));

                bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

                if (isOverUI)
                {
                    ResetPressState();
                    return false;
                }

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

            return false;
        }

        public void ResetPressState()
        {
            _leftPressed = false;
            _leftReleased = false;
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

