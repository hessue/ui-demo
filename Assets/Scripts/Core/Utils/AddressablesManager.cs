using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace BlockAndDagger.Utils
{
    /// <summary>
    /// This uses WaitForCompletion() to block until instantiation completes.
    /// </summary>
    public static class AddressablesManager
    {
        private static TaskCompletionSource<bool> _sPreloadTcs;
        private static AsyncOperationHandle<IList<GameObject>> _sPreloadHandle;
        private static IList<GameObject> _sPreloadedGroup = new List<GameObject>();
        public static Task PreloadCompleted => _sPreloadTcs?.Task ?? Task.CompletedTask;
        public static IList<GameObject> GetPreloadedGroup() => _sPreloadedGroup;
        public static AsyncOperationHandle<IList<GameObject>> GetPreloadHandle() => _sPreloadHandle;

        public static async Task<AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> LoadSceneAsync(
            string addressKey, 
            UnityEngine.SceneManagement.LoadSceneMode mode = UnityEngine.SceneManagement.LoadSceneMode.Single,
            bool activateOnLoad = true)
        {
            if (string.IsNullOrEmpty(addressKey))
            {
                Debug.LogError("LoadSceneAsync: addressKey is null or empty");
                return default;
            }
        
            var locationsHandle = Addressables.LoadResourceLocationsAsync(addressKey);
            locationsHandle.WaitForCompletion();
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null || locationsHandle.Result.Count == 0)
            {
                Debug.LogError($"Addressables: No Location found for Key={addressKey}");
                return default;
            }
        
            var handle = Addressables.LoadSceneAsync(addressKey, mode, activateOnLoad);
            try
            {
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log($"Loaded scene {addressKey}");
                }
                else
                {
                    Debug.LogError($"Failed to load scene {addressKey}: {handle.OperationException}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception while loading scene {addressKey}: {e}");
            }
        
            return handle;
        }
        
        public static GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent, Quaternion? rotation = null)
        {
            var rot = rotation ?? Quaternion.identity;
            var locationsHandle = Addressables.LoadResourceLocationsAsync(addressKey);
            locationsHandle.WaitForCompletion();
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null || locationsHandle.Result.Count == 0)
            {
                Debug.LogError($"Addressables: No Location found for Key={addressKey}");
                return null;
            }

            var handle = Addressables.InstantiateAsync(addressKey, position, rot, parent);
            return handle.WaitForCompletion();
        }

        //TODO:load x number of level folders "level packages"
        public static async Task StartPreloadGroupAssets(string folderAddressKey)
        {
            _sPreloadTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (string.IsNullOrEmpty(folderAddressKey))
            {
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadTcs.TrySetResult(false);
                return;
            }

            try
            {
                _sPreloadHandle = Addressables.LoadAssetsAsync<GameObject>(folderAddressKey);
                await _sPreloadHandle.Task;

                _sPreloadedGroup = _sPreloadHandle.IsValid() && _sPreloadHandle.Result != null
                    ? _sPreloadHandle.Result
                    : new List<GameObject>();

                Debug.Log($"Preloaded {_sPreloadedGroup.Count} assets from folder {folderAddressKey}:{System.Environment.NewLine}" +
                          $"{string.Join(System.Environment.NewLine, _sPreloadedGroup).Replace("(UnityEngine.GameObject)", "")}");

                _sPreloadTcs.TrySetResult(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StartPreloadWarmGroup failed for {folderAddressKey}: {e}");
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadTcs.TrySetResult(false);
            }
        }

        // Release the stored handle and clear cached data
        public static void ReleasePreloadedGroup()
        {
            if (_sPreloadHandle.IsValid())
            {
                Addressables.Release(_sPreloadHandle);
            }

            var count = _sPreloadedGroup?.Count ?? 0;
            Debug.Log($"Releasing preloaded group content {count}.");

            _sPreloadHandle = default;
            _sPreloadedGroup = new List<GameObject>();
            _sPreloadTcs = null;
        }

        public static GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            var prefab = _sPreloadedGroup.FirstOrDefault(x => x != null && x.name == prefabName);
            if (prefab == null)
            {
                Debug.LogError($"{prefabName} not found!");
                return null;
            }

            var newObj = InstantiatePreloadedGameObject(prefab, new Vector3(pos.x, pos.y, pos.z), parent, rot);
            if (newObj == null)
            {
                return null;
            }

            newObj.name = prefabName;
            if (parent != null)
            {
                newObj.transform.SetParent(parent);
            }

            return newObj;
        }

        private static GameObject InstantiatePreloadedGameObject(GameObject prefab, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            var newObj = Object.Instantiate(prefab, pos, rot ?? Quaternion.identity);
            if (newObj == null)
            {
                return null;
            }

            newObj.name = prefab.name;
            if (parent != null)
            {
                newObj.transform.SetParent(parent);
            }

            return newObj;
        }
    }
}