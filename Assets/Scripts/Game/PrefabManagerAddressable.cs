using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using BlockAndDagger.Core;
namespace BlockAndDagger
{
    //TODO: Experimental, not ready
    public sealed class PrefabManagerAddressable : MonoBehaviour, IPrefabManager
    {
        private IAddressablesManager _addressablesManager;
    
        [SerializeField] private bool m_autoPreloadOnAwake = true;
        [SerializeField] private string m_preloadGroupKey;

        private Material _unbuildMaterialLoaded;
        private AsyncOperationHandle<Material> _unbuildMaterialHandle;
        private string _appliedPinnedKey;
        
        public LevelSelectionAssets LevelSelectionAssets { get; set; }
        public GameAssets GameAssets { get; set; }
        public CrossSceneAssets CrossSceneAssets { get; set; }

        [VContainer.Inject]
                public void Construct(IAddressablesManager addressablesManager)
                {
                    _addressablesManager = addressablesManager;
                }

                private void Awake()
                {
                    if (m_autoPreloadOnAwake && !string.IsNullOrEmpty(m_preloadGroupKey))
                    {
                        _addressablesManager.StartPreloadGroupAssets(new[] { m_preloadGroupKey }).GetAwaiter().GetResult();
                    }
                }
        
        public Player CreateNewPlayer()
        {
            // Not supported in this simplified Addressable variant; return null to allow other managers to provide the player prefab.
            return null;
        }
 
        public GameObject CreateNewObject(LevelObjectType type)
        {
            // Addressable variant does not provide per-type prefab instantiation in this simplified implementation.
            return null;
        }
 
        public void ResetInstantiatedPlayerCount()
        {
        }

        public void InitCrossSceneAssets()
        {
            throw new System.NotImplementedException();
        }

        public void PinKeys(System.Collections.Generic.IEnumerable<string> keys)
        {
            if (keys == null) return;
            foreach (var k in keys) _addressablesManager.PinKey(k);
        }

        public void UnpinKeys(System.Collections.Generic.IEnumerable<string> keys)
        {
            if (keys == null) return;
            foreach (var k in keys) _addressablesManager.UnpinKey(k);
        }

        private void OnDestroy()
        {
 
        }
    }
}


