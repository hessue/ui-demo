using System;
using UnityEngine;

namespace BlockAndDagger
{
    public enum MenuState
    {
        MainMenu = 0,
        LevelMaker = 1,
        LevelSelection = 2,
        Play = 3,
        InGameMenu = 4,
        ChooseAbilitiesMenu = 5,
        Pause = 6,
        //BlueprintSelection = 7,
        LevelMakerChooseBlueprint = 8
    }

    public class SimpleStateMachine
    {
        public State CurrentState { get; private set; }

        public void SetState(State newState)
        {
            //disable previous panel
            if (CurrentState is not null)
            {
                CurrentState.TargetUI.SetActive(false);
            }

            CurrentState = newState;
            CurrentState.Execute();
        }
    }
}