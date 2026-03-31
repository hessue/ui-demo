using UnityEngine;

namespace BlockAndDagger
{
    public class LevelMakerState : State
    {
        public LevelMakerState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }
        
        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
    
    
    public class ChooseBlueprintState : State
    {
        public ChooseBlueprintState(GameObject targetUI, MenuState menuState) : base(targetUI, menuState)
        {
        }
        
        public override void ActivateMenu()
        {
            TargetUI.SetActive(true);
        }
    }
}
