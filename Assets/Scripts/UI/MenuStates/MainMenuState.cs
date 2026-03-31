using UnityEngine;

namespace BlockAndDagger
{
    public sealed class MainMenuState : State
    {
        public MainMenuState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }
        
        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
}
