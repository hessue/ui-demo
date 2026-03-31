using UnityEngine;

namespace BlockAndDagger
{
    public interface IBuildable
    {
        public bool IsBuild { get; set; }
        public float BuildTime  { get; }
        void SetBuildState(bool completed);
    }
}
