using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UnityEngine;

namespace BlockAndDagger
{
    //TODO: Absolut garbage, just get rid off the system.timers and do the Unity way
    public class Timers
    {
        public List<(ScenarioTimer Timer, Action<object, ElapsedEventArgs> Callback)> _list;
        private bool _isRunning;
        private float _updateTimer;
        private List<int> _removableIndexes = new();

        //TODO: cleaner constructor/init
        public Timers(List<(ScenarioTimer, Action<object, ElapsedEventArgs>)> timers)
        {
            _list = new List<(ScenarioTimer Timer, Action<object, ElapsedEventArgs> OnComplete)>(timers.OrderBy(x => x.Item1.ScenarioData.interval));
        }
        
        public void OnUpdate()
        {
            if (_list is null || !_isRunning)
                return;

            if (_list.Count > 0)
            {
                for (int i = 0; i < _list.Count; i++)
                {
                    var timer = _list[i];

                    if (timer.Timer.Obsolete)
                    {
                       // _removableIndexes.Add(i);
                        continue;
                    }

                    if (!timer.Timer.Started)
                    {
                        timer.Timer.StartTimer();
                    }
                    else
                    {
                        timer.Timer.OnUpdate(Time.deltaTime);
                        
                        if (timer.Timer.Ready)
                        {
                            timer.Callback?.Invoke(timer,null);
                        }
                    }
                }
            }
        }

        public void Start()
        {
            _isRunning = true;
        }
        
        public void PauseAllTimers(bool pause)
        {
            _isRunning = pause;
            foreach (var timer in _list)
            {
                if (timer.Timer != null)
                {
                    if (pause)
                    {
                        timer.Timer.Pause();
                    }
                    else
                    {
                        timer.Timer.Resume();
                    }
                }
            }
        }
        
        public void StopAndDisposeAllTimers()
        {
            _isRunning = false;
            _list.ToList().ForEach(x =>
            {
                if (x.Timer == null) 
                    return;
                
                x.Timer.Stop();
                x.Timer.MarkAsObsolete();
            });
            _list.Clear();
        }
    }
}