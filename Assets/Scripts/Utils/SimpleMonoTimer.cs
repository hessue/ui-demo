using UnityEngine;

namespace BlockAndDagger.Utils
{
    public class SimpleMonoTimer
    {
        public readonly struct MinimalTimer
        {
            public static MinimalTimer Start(float duration) => new(duration);
            //public bool IsCompleted => Time.time >= _triggerTime && _triggerTime != 0;
            public bool IsCompleted => _triggerTime != 0 && Time.time >= _triggerTime;
            public bool IsStarted => _triggerTime != 0;

            // Return a new timer started with the provided duration.
            public MinimalTimer Reset(float duration) => new MinimalTimer(duration);

            
   
            private readonly float _triggerTime;
            private MinimalTimer(float duration) => _triggerTime = Time.time + duration;
        }
    }
}