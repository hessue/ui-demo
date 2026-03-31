using UnityEngine;

namespace BlockAndDagger
{
    public class SpawnerBlock : MonoBehaviour
    {
        /// <summary>
        /// "Occupied" or reserverd by other spawn request
        /// </summary>
        public bool IsBusy { get; set; }
        private string _spawnId;
        [field : SerializeField] public EnemyType SpawningType { get; set; }

        public void InitGizmo(int spawnId)
        {
            _spawnId = spawnId.ToString();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, _spawnId);
#else
            // In player builds we can't use UnityEditor.Handles; keep Gizmos simple.
#endif
        }
    }
}
