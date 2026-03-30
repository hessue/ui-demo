using UnityEngine;

namespace BlockAndDagger
{
    public sealed class PlayState : State
    {
        public PlayState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }

        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
}
