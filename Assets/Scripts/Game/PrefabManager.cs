using System;
using BlockAndDagger.Utils;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class PrefabManager : MonoBehaviour, IPrefabManager
    { 
        private static int _instantiatedPlayerCount;
        [Header("Scene assets")]
        [SerializeField] private GameAssets m_gameAssets;
        [SerializeField] private LevelSelectionAssets m_levelSelectionAssets;
        
        [ReadOnly] [SerializeField] 
        private CrossSceneAssets m_crossSceneAssets; //loaded separately or persistent pinned addressable

        //TODO: move to PrefabManagerAddressable
        public LevelSelectionAssets LevelSelectionAssets
        {
            get => m_levelSelectionAssets; 
            set => m_levelSelectionAssets = value;
        }

        //TODO: move to PrefabManagerAddressable
        public GameAssets GameAssets
        {
            get => m_gameAssets;
            set => m_gameAssets = value;
        }
        
        public CrossSceneAssets CrossSceneAssets
        {
            get => m_crossSceneAssets; 
            set => m_crossSceneAssets = value;
        }
        
        public void InitCrossSceneAssets()
        {
            m_crossSceneAssets = FindFirstObjectByType<CrossSceneAssets>();
        }

        /// <summary>
        /// </summary>
        /// <returns>Note! Not active by default</returns>
        public Player CreateNewPlayer()
        {
            _instantiatedPlayerCount++;
            var player = Instantiate(GameAssets.PlayerPrefab); //TODO:_resolver.InjectGameObject(player.gameObject); sort ths out
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
                    prefab = m_gameAssets.EnemyPrefab;
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