using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlockAndDagger.Core
{
    public interface IAddressablesManager
    {
        Task PreloadCompleted { get; }
        IList<GameObject> GetPreloadedGroup();
        AsyncOperationHandle<IList<GameObject>> GetPreloadHandle();

        Task<bool> HasResourceLocationsAsync(string key, System.Type type = null);
        Task<AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> LoadSceneAsync(string addressKey, UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single, bool activateOnLoad = true);
        GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent, Quaternion? rotation = null);

        System.Threading.Tasks.Task<BlockAndDagger.Core.AddressablesPreloadResult> StartPreloadGroupAssets(IEnumerable<string> folderAddressKeys);
        System.Threading.Tasks.Task<BlockAndDagger.Core.AddressablesPreloadResult> ReleasePreloadedGroupAsync(System.Collections.Generic.List<string> newAssetGroups = null);

        void ReleasePreloadedGroup();

        void PinKey(string key);
        AsyncOperationHandle<UnityEngine.Object> LoadAndPinKey(string key);
        Task<AsyncOperationHandle<UnityEngine.Object>> LoadAndPinKeyAsync(string key);
        void UnpinKey(string key);
        bool IsPinned(string key);

        void RegisterAddressablesInstancedObject(GameObject go);
        void ReleaseInstance(UnityEngine.Object obj);

        GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos, Transform parent = null, Quaternion? rot = null);

        List<ManifestEntry> GetNeededAddressGroups(List<TileType> distinctTileTypes);
    }
}