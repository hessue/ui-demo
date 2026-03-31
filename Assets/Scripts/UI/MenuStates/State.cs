using UnityEngine;

namespace BlockAndDagger
{
    public abstract class State
    {
        public MenuState MenuState { get; }
        public GameObject TargetUI;

        protected State(GameObject targetUI, MenuState menuState)
        {
            MenuState = menuState;
            TargetUI = targetUI;
        }

        public void Execute()
        {
            //Debug.Log($"Switched to state: {(TargetUI != null ? TargetUI.name : "<null>")}");

            if (TargetUI == null)
            {
                Debug.LogError($"State.Execute: TargetUI is null for menu state. Skipping ActivateMenu().");
            }
            ActivateMenu();
        }
        
        public virtual void ActivateMenu()
        {
        }
    }
}