using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BlockAndDagger
{
    public sealed class PrefabManager : MonoBehaviour
    {
        [SerializeField] public Player m_playerPrefab;
        [SerializeField] public GameObject m_levelIconItemPrefab;
        [SerializeField] public Material m_unbuildMaterial;
        [SerializeField] public Material m_highlighMaterial;
        [SerializeField] public GameObject m_enemyPrefab;
        
        private static int _instantiatedPlayerCount;
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }
        
        /// <summary>
        /// </summary>
        /// <returns>Note! Not active by default</returns>
        public Player CreateNewPlayer()
        {
            _instantiatedPlayerCount++;
            var player = Instantiate(m_playerPrefab);
            // Enforce injection: resolver must inject the instantiated player
            _resolver.InjectGameObject(player.gameObject);

            player.Init(GameManager.Instance.MenuManager.IngameUI); //TODO: inject
            player.name = "Player " + _instantiatedPlayerCount;
            player.gameObject.SetActive(false);
            return player;
        }

        public GameObject CreateNewObject(LevelObjectType type)
        {
            GameObject prefab = null;
            switch (type)
            {
                case  LevelObjectType.EnemyCreep:
                case  LevelObjectType.EnemyWolf:
                    prefab = m_enemyPrefab;
                    break;
                default:
                    throw new NotSupportedException("TODO: LevelObjectType supported");
            }
            
            return InstantiateObject(prefab);
        }
        
        private GameObject InstantiateObject(GameObject prefab, string nameSuffix = "")
        {
            var obj = Instantiate(prefab);
            obj.name = prefab.name + " " + nameSuffix;
            var fieldObject = obj.GetComponent<IFieldObject>();
            fieldObject.Init();
            
            obj.gameObject.SetActive(false);
            return obj;
        }
        
        public void ResetInstantiatedPlayerCount()
        {
            _instantiatedPlayerCount = 0;
        }
    }
}