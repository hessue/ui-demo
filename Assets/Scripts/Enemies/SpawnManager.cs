using UnityEngine;

namespace BlockAndDagger
{
    public class SpawnManager
    {
        [SerializeField] private int _activeCount;
        private Level _level; 
        private readonly GOPool _goPool;
        
        public SpawnManager(Level level, GOPool goPool)
        {
            _level = level;
            _goPool = goPool;
        }
        
        public GameObject SpawnObject(LevelObjectType levelObjectType, Vector3 pos, bool asActiveWithActiveNavMesh)
        {
            return _goPool.GetOrCreate(levelObjectType, asActiveWithActiveNavMesh, pos);
        }
        
        public SpawnerBlock GetSpawnSpawnerBlock(int index)
        {
            if (_level.SpawnPositions.Length < index)
            {
                return null;
            }

            return _level.SpawnPositions[index].GetComponent<SpawnerBlock>();
        }
    }
}