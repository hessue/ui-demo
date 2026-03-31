using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class LevelSelectionState : State
    {
        public LevelSelectionState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }
        
        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
}
