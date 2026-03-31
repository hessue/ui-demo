using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    public class GOPool : MonoBehaviour
    {
        public List<(LevelObjectType LevelObjectType, GameObject GO)> pooledObjects;
        public int ActiveCount;
        public int amountToPool;

        void Awake()
        {
            pooledObjects = new();
        }

        public void PrepopulateWithEnemies(LevelEvents levelEvents)
        {
            if (levelEvents is null)
            {
                Debug.Log("LevelEvent data is empty. Skipping event creation");
                return;
            }

            foreach (var e in levelEvents.events)
            {
                if (e.scenarioType != ScenarioType.Enemywave)
                {
                    continue;
                }

                foreach (var wave in e.waves)
                {
                    var enemyType =  wave.spawnEnemyTypes.First();
                    for (int i = 0; i < wave.enemyCountToSpawn; i++)
                    {
                        _ = CreateObjectAndAddPool(enemyType);
                    }
                }
            }
        }

        public GameObject GetOrCreate(LevelObjectType levelObjectType, bool asActiveWithActiveNavMesh, Vector3 pos = default)
        {
            var exist = pooledObjects.FirstOrDefault(x => x.LevelObjectType == levelObjectType && !x.GO.activeInHierarchy);
            if (exist.GO is null)
            {
                exist = (levelObjectType, CreateObjectAndAddPool(levelObjectType));
                pooledObjects.Add(exist);
            }

            var obj = exist.GO.GetComponent<IFieldObject>();
            
            obj.Init();
           
            obj.transform.position = new Vector3(pos.x, pos.y + Constants.SpawningPosOffsetY, pos.z);
            if (asActiveWithActiveNavMesh)
            {
                obj.transform.gameObject.SetActive(true);
                obj.StartLiving();// With NavMeshAgent case use Enemy.TurnAliveOnFirstGroundHit method
                ActiveCount++;
            }
            else
            {
                obj.PoolStatus = PoolStatus.SpawnedButWaitingActivation;
            }
            
            return exist.GO;
        }
        
        private GameObject CreateObjectAndAddPool(LevelObjectType levelObjectType)
        {
            var obj = GameManager.Instance.PrefabManager.CreateNewObject(levelObjectType);
            pooledObjects.Add((levelObjectType, obj));
            return obj;
        }

        public void DestroyAllPoolObjects()
        {
            foreach (var item in pooledObjects)
            {
                item.GO.SetActive(false);
                Destroy(item.GO);
            }
            
            pooledObjects.Clear();
        }

        public void UnactivateAllPoolObjects()
        {
            foreach (var go in pooledObjects)
            {
                go.GO.GetComponent<Enemy>().KillAndReturnToPool();
            }
        }

        //TODO:
        private void UnactivateLevelObject(IFieldObject obj)
        {
            obj.transform.gameObject.SetActive(false);
            ActiveCount--;
        }
    }
}
