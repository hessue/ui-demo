
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class CustomizeState : State
    {
        public CustomizeState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }
        
        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
}
