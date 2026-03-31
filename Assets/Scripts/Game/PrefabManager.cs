using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BlockAndDagger
{
    public sealed class PrefabManager : MonoBehaviour
    { 
        private static int _instantiatedPlayerCount;

        [SerializeField] private Player m_playerPrefab;
        [SerializeField] private GameObject m_levelIconItemPrefab;
        [SerializeField] private Material m_unbuildMaterial;
        [SerializeField] private Material m_highlighMaterial;
        [SerializeField] private GameObject m_enemyPrefab;

        public GameObject LevelIconItemPrefab => m_levelIconItemPrefab;
        public Material UnbuildMaterial => m_unbuildMaterial;
        public Material HighlighMaterial => m_highlighMaterial;
        public GameObject EnemyPrefab => m_enemyPrefab;

        /// <summary>
        /// </summary>
        /// <returns>Note! Not active by default</returns>
        public Player CreateNewPlayer()
        {
            _instantiatedPlayerCount++;
            var player = Instantiate(m_playerPrefab); //TODO:_resolver.InjectGameObject(player.gameObject); sort ths out
            var gm = GameManager.Instance;

            var playerControls = player.GetComponent<PlayerControls>();
            playerControls.ManualConstruct(gm.AudioManager, gm); 

            player.Init(gm.MenuManager.IngameUI);
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