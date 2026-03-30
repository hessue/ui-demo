using UnityEngine;

namespace BlockAndDagger
{
    public interface IFieldObject
    {
        public void Init();
        public void StartLiving();

        public Transform transform { get; }
        
        public PoolStatus PoolStatus { get; set; }
    }
}
