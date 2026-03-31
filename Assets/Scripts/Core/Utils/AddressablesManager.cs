using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;

namespace BlockAndDagger.Core
{
    /// <summary>
    /// This uses WaitForCompletion() to block until instantiation completes.
    /// </summary>
    public static class AddressablesManager
    {
        private static TaskCompletionSource<bool> _sPreloadTcs;
        private static AsyncOperationHandle<IList<GameObject>> _sPreloadHandle;
        private static IList<GameObject> _sPreloadedGroup = new List<GameObject>();
        private static int _sPreloadGeneration = 0;
        private static HashSet<string> _sPreloadedGroupKeys = new();
        private static bool _sTestMode = false;
        public static Task PreloadCompleted => _sPreloadTcs?.Task ?? Task.CompletedTask;
        public static IList<GameObject> GetPreloadedGroup() => _sPreloadedGroup;
        public static AsyncOperationHandle<IList<GameObject>> GetPreloadHandle() => _sPreloadHandle;

        public static async Task<bool> HasResourceLocationsAsync(string key, System.Type type = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            var locationsHandle = type == null
                ? Addressables.LoadResourceLocationsAsync(key)
                : Addressables.LoadResourceLocationsAsync(key, type);

            await locationsHandle.Task;
            return locationsHandle.Status == AsyncOperationStatus.Succeeded && locationsHandle.Result != null &&
                   locationsHandle.Result.Count > 0;
        }

        public static async Task<AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>>
            LoadSceneAsync(
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
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null ||
                locationsHandle.Result.Count == 0)
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

        public static GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent,
            Quaternion? rotation = null)
        {
            var rot = rotation ?? Quaternion.identity;
            var locationsHandle = Addressables.LoadResourceLocationsAsync(addressKey);
            locationsHandle.WaitForCompletion();
            if (locationsHandle.Status != AsyncOperationStatus.Succeeded || locationsHandle.Result == null ||
                locationsHandle.Result.Count == 0)
            {
                Debug.LogError($"Addressables: No Location found for Key={addressKey}");
                return null;
            }

            var handle = Addressables.InstantiateAsync(addressKey, position, rot, parent);
            return handle.WaitForCompletion();
        }

        public static async Task StartPreloadGroupAssets(IEnumerable<string> folderAddressKeys)
        {
            _sPreloadTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (folderAddressKeys == null)
            {
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadTcs.TrySetResult(false);
                return;
            }

            folderAddressKeys = folderAddressKeys.Select(x => x.ToLower()).ToArray();

            var requestedKeys = folderAddressKeys.ToArray();
            var requestedSet = new HashSet<string>(requestedKeys);

            if (_sPreloadedGroupKeys != null && _sPreloadedGroupKeys.Count > 0 &&
                _sPreloadedGroupKeys.IsSupersetOf(requestedSet))
            {
                _sPreloadTcs.TrySetResult(true);
                return;
            }

#if UNITY_EDITOR
            if (_sTestMode)
            {
                _sPreloadedGroupKeys = new HashSet<string>(requestedSet);
                if (_sPreloadedGroup != null)
                {
                    foreach (var go in _sPreloadedGroup)
                    {
                        if (go != null)
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                }

                _sPreloadedGroup = _sPreloadedGroupKeys.Select(k => new GameObject(k)).ToList();
                _sPreloadHandle = default;
                _sPreloadTcs.TrySetResult(true);
                return;
            }
#endif

            try
            {
                var myGeneration = ++_sPreloadGeneration;
                var handle = Addressables.LoadAssetsAsync<GameObject>(folderAddressKeys, null,
                    Addressables.MergeMode.Union);
                _sPreloadHandle = handle;
                await handle.Task;

                if (myGeneration != _sPreloadGeneration)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    _sPreloadHandle = default;
                    _sPreloadedGroup = new List<GameObject>();
                    _sPreloadTcs.TrySetResult(false);
                    return;
                }

                _sPreloadedGroup = handle.IsValid() && handle.Result != null
                    ? handle.Result
                    : new List<GameObject>();

                _sPreloadedGroupKeys = folderAddressKeys != null
                    ? new HashSet<string>(folderAddressKeys)
                    : new HashSet<string>();

                Debug.Log($"Preloaded {_sPreloadedGroup.Count} assets from keys list:{System.Environment.NewLine}" +
                          $"{string.Join(System.Environment.NewLine, _sPreloadedGroup).Replace("(UnityEngine.GameObject)", "")} ");

                _sPreloadTcs.TrySetResult(true);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StartPreloadGroupAssets (multiple keys) failed: {e}");
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadTcs.TrySetResult(false);
            }
        }

        // Release the stored handle and clear cached data
        public static void ReleasePreloadedGroup()
        {
            // bump generation so any in-flight preload will not repopulate the cache
            _sPreloadGeneration++;

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

        public static async Task ReleasePreloadedGroupAsync(List<string> newAssetGroups = null)
        {
            var tcs = _sPreloadTcs;

            _sPreloadGeneration++;

#if UNITY_EDITOR
            if (_sTestMode)
            {
                //TODO: replace with DI approach
                await ReleasePreloadedGroupForTests(newAssetGroups);
                return;
            }
#endif

            if (newAssetGroups == null || newAssetGroups.Count == 0 || _sPreloadedGroupKeys == null ||
                _sPreloadedGroupKeys.Count == 0)
            {
                if (_sPreloadHandle.IsValid())
                {
                    Addressables.Release(_sPreloadHandle);
                }

                var count = _sPreloadedGroup?.Count ?? 0;
                Debug.Log($"Releasing preloaded group content {count}.");

                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            var newKeys = newAssetGroups.Select(x => x.ToLower()).ToArray();
            var newSet = new HashSet<string>(newKeys);

            var keepKeys = new HashSet<string>(_sPreloadedGroupKeys);
            keepKeys.IntersectWith(newSet);

            if (keepKeys.SetEquals(_sPreloadedGroupKeys))
            {
                _sPreloadTcs = null;
                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            if (keepKeys.Count == 0)
            {
                if (_sPreloadHandle.IsValid())
                {
                    Addressables.Release(_sPreloadHandle);
                }

                var count = _sPreloadedGroup?.Count ?? 0;
                Debug.Log($"Releasing preloaded group content {count}.");

                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            try
            {
                var handle =
                    Addressables.LoadAssetsAsync<GameObject>(keepKeys, null,
                        Addressables.MergeMode.Union);
                _sPreloadHandle = handle;
                await handle.Task;

                if (handle.IsValid() && handle.Result != null)
                {
                    _sPreloadedGroup = handle.Result;
                    _sPreloadedGroupKeys = new HashSet<string>(keepKeys);
                    Debug.Log(
                        $"Kept {_sPreloadedGroup.Count} preloaded assets from groups: {string.Join(", ", keepKeys)}");
                }
                else
                {
                    if (_sPreloadHandle.IsValid())
                    {
                        Addressables.Release(_sPreloadHandle);
                    }

                    _sPreloadHandle = default;
                    _sPreloadedGroup = new List<GameObject>();
                    _sPreloadedGroupKeys = new HashSet<string>();
                    _sPreloadTcs = null;
                }
            }
            catch (System.Exception)
            {
                if (_sPreloadHandle.IsValid())
                {
                    Addressables.Release(_sPreloadHandle);
                }

                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;
            }

            if (tcs != null)
            {
                try
                {
                    await tcs.Task;
                }
                catch
                {
                }
            }
        }

        public static GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos,
            Transform parent = null, Quaternion? rot = null)
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

        private static GameObject InstantiatePreloadedGameObject(GameObject prefab, Vector3 pos,
            Transform parent = null, Quaternion? rot = null)
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

#region testing
#if UNITY_EDITOR

        //TODO: replace with mock etc at somepoint
        public static HashSet<string> GetPreloadedGroupKeysForTests()
        {
            return new HashSet<string>(_sPreloadedGroupKeys);
        }

        public static void SetTestModeForEditor(bool enabled)
        {
            _sTestMode = enabled;
        }

        public static async Task ReleasePreloadedGroupForTests(List<string> newAssetGroups = null)
        {
            var tcs = _sPreloadTcs;

            _sPreloadGeneration++;

            if (newAssetGroups == null || newAssetGroups.Count == 0 || _sPreloadedGroupKeys == null ||
                _sPreloadedGroupKeys.Count == 0)
            {
                if (_sPreloadedGroup != null)
                {
                    foreach (var go in _sPreloadedGroup)
                    {
                        if (go != null)
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                }

                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            var newKeys = newAssetGroups.Select(x => x.ToLower()).ToArray();
            var newSet = new HashSet<string>(newKeys);

            var keepKeys = new HashSet<string>(_sPreloadedGroupKeys);
            keepKeys.IntersectWith(newSet);

            if (keepKeys.SetEquals(_sPreloadedGroupKeys))
            {
                _sPreloadTcs = null;
                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            if (keepKeys.Count == 0)
            {
                if (_sPreloadedGroup != null)
                {
                    foreach (var go in _sPreloadedGroup)
                    {
                        if (go != null)
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                }

                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch
                    {
                    }
                }

                return;
            }

            try
            {
                var keptList = new List<GameObject>();
                foreach (var key in keepKeys)
                {
                    var existing = _sPreloadedGroup.FirstOrDefault(g => g != null && g.name == key);
                    if (existing != null)
                    {
                        keptList.Add(existing);
                    }
                    else
                    {
                        keptList.Add(new GameObject(key));
                    }
                }

                if (_sPreloadedGroup != null)
                {
                    foreach (var go in _sPreloadedGroup)
                    {
                        if (go != null && !keepKeys.Contains(go.name))
                        {
                            Object.DestroyImmediate(go);
                        }
                    }
                }

                _sPreloadedGroup = keptList;
                _sPreloadedGroupKeys = new HashSet<string>(keepKeys);
                _sPreloadHandle = default;
                _sPreloadTcs = null;
            }
            catch (System.Exception)
            {
                _sPreloadHandle = default;
                _sPreloadedGroup = new List<GameObject>();
                _sPreloadedGroupKeys = new HashSet<string>();
                _sPreloadTcs = null;
            }

            if (tcs != null)
            {
                try
                {
                    await tcs.Task;
                }
                catch
                {
                }
            }
        }
#endif
#endregion
    }
}