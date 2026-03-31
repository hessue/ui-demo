using UnityEngine;
using UnityEngine.InputSystem;

namespace BlockAndDagger
{
    public interface IPointerInput
    {
        void Init(MenuInputActions menuInputActions, Camera cam, int layerMask);
        bool TryGetHit(out Block block, bool inGame);
        void ResetPressState();
        void Dispose();
    }
}

