using System;
using BlockAndDagger;
using BlockAndDagger.Utils;
using UnityEngine;

public sealed class ProgressBar : MonoBehaviour
{
    private BattleStateChange[] list;
    public int CurrentStateIndex { get; private set; } = 0;
    private SimpleMonoTimer.MinimalTimer m_timer;
    private bool isTimerDone;

    public void MoveToNextBattleState()
    {
        if (CurrentStateIndex + 1 >= list.Length)
        {
            TheEnd();
        }
        
        CurrentStateIndex++;
        //var data = list[CurrentStateIndex];
        m_timer.Reset(5f);
    }

    public void ShowBar()
    {
        
    }

    public void ActivateBar()
    {
        //await Awaitable.WaitForSecondsAsync(2f);
        m_timer = SimpleMonoTimer.MinimalTimer.Start(3f);
        Debug.Log("Minimal timer started!");
    }

    private void Update()
    {
        if (m_timer.IsCompleted && !isTimerDone)
        {
            Debug.Log("Minimal Timer completed!");
            MoveToNextBattleState();
            isTimerDone = true;
        }
    }

    public void LoadData(BattleStateChange[] battleStateChange)
    {
        this.list = battleStateChange ?? throw new NullReferenceException();
    }
    
    public void StartProgress()
    {
        MoveToNextBattleState();
    }

    private void TheEnd()
    {
        Debug.Log("The End of Progress Bar reached!");
    }
    
}



