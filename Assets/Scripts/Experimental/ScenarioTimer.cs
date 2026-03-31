using System;

namespace BlockAndDagger
{
    public class ScenarioTimer
    {
        private float _time;
        private bool _paused;
        private bool _running;
        private Action _callback;
        private int _repeatTimes;

        public bool Ready { get; private set; }
        public bool Started { get; private set; }
        public int RepeatedTimes { get; private set; }
        public bool Obsolete { get; private set; }
        private ILogger _logger;
        
        public void MarkAsObsolete()
        {
            _running = false;
            Obsolete = true;
        }

        public BaseScenario ScenarioData { get; }

        public ScenarioTimer(BaseScenario scenarioData, ILogger logger)
        {
            ScenarioData = scenarioData;
            _logger = logger;
        }

        public void StartTimer()
        {
            if (ScenarioData.scenarioType == ScenarioType.TimeSurvivor)
            {
                _logger.Log($"{ScenarioData?.description}. {ScenarioData.interval} second timer started");
            }

            _time = ScenarioData.interval;
            _running = true;
            Started = true;
        }
        
        public void OnUpdate(float deltaTime)
        {
            if (!_running)
                return;
            
            _time -= deltaTime;

            if (_time < 0 && !_paused)
            {
                Ready = true;
            }
        }
        
        public void Pause()
        {
            _paused = true;
            _running = false;
        }
        public void Resume()
        {
            _paused = false;
            _running = true;
        }

        public void Stop()
        {
            _running = false;
            Ready = false;
        }
        
        public void RestartImmediatelyIfRepeatsLeft()
        {
            RepeatedTimes++;
            if (ScenarioData.repeatTimes > RepeatedTimes)
            {
                Stop();
                StartTimer();
            }
            else
            {
                MarkAsObsolete();
            }
        }
    }
}