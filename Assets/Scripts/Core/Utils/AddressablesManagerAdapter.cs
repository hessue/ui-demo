using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlockAndDagger.Core
{
    /*// Adapter that wraps an instance of AddressablesManager and exposes an IAddressablesManager
    public class AddressablesManagerAdapter : IAddressablesManager
    {
        private readonly AddressablesManager _impl;

        public AddressablesManagerAdapter()
        {
            _impl = new AddressablesManager();
        }

        public System.Threading.Tasks.Task PreloadCompleted => _impl.PreloadCompleted;
        public IList<GameObject> GetPreloadedGroup() => _impl.GetPreloadedGroup();
        public AsyncOperationHandle<IList<GameObject>> GetPreloadHandle() => _impl.GetPreloadHandle();
        public event System.Action<bool> AddressablePackedReady
        {
            add { _impl.AddressablePackedReady += value; }
            remove { _impl.AddressablePackedReady -= value; }
        }

        public System.Threading.Tasks.Task<bool> HasResourceLocationsAsync(string key, System.Type type = null) => _impl.HasResourceLocationsAsync(key, type);
        public System.Threading.Tasks.Task<AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> LoadSceneAsync(string addressKey, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true)
            => _impl.LoadSceneAsync(addressKey, mode, activateOnLoad);

        public GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent, Quaternion? rotation = null)
            => _impl.InstantiatePrefab(addressKey, position, parent, rotation);

        public System.Threading.Tasks.Task<AddressablesPreloadResult> StartPreloadGroupAssets(IEnumerable<string> folderAddressKeys)
        {
            return _impl.StartPreloadGroupAssets(folderAddressKeys);
        }

        public System.Threading.Tasks.Task<AddressablesPreloadResult> ReleasePreloadedGroupAsync(System.Collections.Generic.List<string> newAssetGroups = null)
        {
            return _impl.ReleasePreloadedGroupAsync(newAssetGroups);
        }

        public void ReleasePreloadedGroup() => _impl.ReleasePreloadedGroup();
        public void PinKey(string key) => _impl.PinKey(key);
        public AsyncOperationHandle<UnityEngine.Object> LoadAndPinKey(string key) => _impl.LoadAndPinKey(key);
        public System.Threading.Tasks.Task<AsyncOperationHandle<UnityEngine.Object>> LoadAndPinKeyAsync(string key) => _impl.LoadAndPinKeyAsync(key);
        public void UnpinKey(string key) => _impl.UnpinKey(key);
        public bool IsPinned(string key) => _impl.IsPinned(key);
        public void RegisterAddressablesInstancedObject(GameObject go) => _impl.RegisterAddressablesInstancedObject(go);
        public void ReleaseInstance(UnityEngine.Object obj) => _impl.ReleaseInstance(obj);
        public GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos, Transform parent = null, Quaternion? rot = null)
            => _impl.FindFromCacheAndInstantiatePrefab(prefabName, pos, parent, rot);
        public System.Collections.Generic.List<ManifestEntry> GetNeededAddressGroups(System.Collections.Generic.List<TileType> distinctTileTypes) => _impl.GetNeededAddressGroups(distinctTileTypes);
    }*/
}