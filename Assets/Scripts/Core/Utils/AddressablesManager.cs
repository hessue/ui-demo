using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.IO;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

namespace BlockAndDagger.Core
{
    public class AddressablesManager : IAddressablesManager
    {
        private TaskCompletionSource<bool> _preloadTcs;
        private AsyncOperationHandle<IList<GameObject>> _preloadHandle;
        private IList<GameObject> _preloadedGroup = new List<GameObject>();
        private int _preloadGeneration = 0;
        private HashSet<string> _preloadedGroupKeys = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, IResourceLocation> _preloadedGroupLocations = new();
        
        public System.Threading.Tasks.Task PreloadCompleted => _preloadTcs?.Task ?? System.Threading.Tasks.Task.CompletedTask;
        public virtual IList<GameObject> GetPreloadedGroup() => _preloadedGroup;
        public virtual AsyncOperationHandle<IList<GameObject>> GetPreloadHandle() => _preloadHandle;
        public event Action<bool> AddressablePackedReady;

        private readonly Dictionary<string, int> _pinnedKeyRefCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<UnityEngine.Object>> _pinnedHandles = new Dictionary<string, AsyncOperationHandle<UnityEngine.Object>>(StringComparer.OrdinalIgnoreCase);

        // Track Addressables-instanced runtime objects so we can call ReleaseInstance when appropriate
        private readonly HashSet<GameObject> _addressablesInstancedObjects = new();

        public AddressablesManager()
        {
        }

        public async System.Threading.Tasks.Task<bool> HasResourceLocationsAsync(string key, System.Type type = null)
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

        public async System.Threading.Tasks.Task<AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance>> LoadSceneAsync(
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
                if (handle.Status != AsyncOperationStatus.Succeeded)
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

        public virtual GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent, Quaternion? rotation = null)
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

        // Start preload now returns a result to help tests inspect what was loaded
        public virtual async System.Threading.Tasks.Task<BlockAndDagger.Core.AddressablesPreloadResult> StartPreloadGroupAssets(System.Collections.Generic.IEnumerable<string> folderAddressKeys)
        {
            _preloadTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (folderAddressKeys == null)
            {
                _preloadedGroup = new System.Collections.Generic.List<GameObject>();
                _preloadTcs.TrySetResult(false);
                AddressablePackedReady?.Invoke(false);

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }

            // Keep key casing as provided; Addressables keys may be case-sensitive. Use case-insensitive set comparisons below where needed.
            var requestedKeys = folderAddressKeys.ToArray();
            var requestedSet = new HashSet<string>(requestedKeys, StringComparer.OrdinalIgnoreCase);

            if (_preloadedGroupKeys != null && _preloadedGroupKeys.Count > 0 && _preloadedGroupKeys.IsSupersetOf(requestedSet))
            {
                _preloadTcs.TrySetResult(true);
                AddressablePackedReady?.Invoke(true);
                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }

            try
            {
                var myGeneration = ++_preloadGeneration;
                var allLocations = new System.Collections.Generic.List<IResourceLocation>();

                foreach (var key in folderAddressKeys)
                {
                    var locHandle = Addressables.LoadResourceLocationsAsync(key);
                    await locHandle.Task;
                    if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result != null)
                    {
                        allLocations.AddRange(locHandle.Result);
                    }
                }

                if (allLocations.Count == 0)
                {
                    Debug.Log($"StartPreloadGroupAssets: no locations found. Clearing preloaded group. Stack:\n{System.Environment.StackTrace}");
                    _preloadedGroup = new System.Collections.Generic.List<GameObject>();
                    _preloadedGroupKeys = new System.Collections.Generic.HashSet<string>();
                    _preloadHandle = default;
                    _preloadTcs.TrySetResult(false);

                    return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
                }

                var keys = allLocations.Select(l => l.PrimaryKey).Where(k => !string.IsNullOrEmpty(k)).Distinct();
                var handle = Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Union);
                _preloadHandle = handle;
                await handle.Task;

                if (myGeneration != _preloadGeneration)
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    _preloadHandle = default;
                    _preloadedGroup = new System.Collections.Generic.List<GameObject>();
                    _preloadTcs.TrySetResult(false);

                    return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
                }

                _preloadedGroup = handle.IsValid() && handle.Result != null ? handle.Result : new System.Collections.Generic.List<GameObject>();

                _preloadedGroupKeys = folderAddressKeys != null ? new System.Collections.Generic.HashSet<string>(folderAddressKeys) : new System.Collections.Generic.HashSet<string>();

                // Build mapping from prefab name to its resource location for Addressables-controlled instantiation
                _preloadedGroupLocations = new System.Collections.Generic.Dictionary<string, IResourceLocation>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    // First pass: map by filename / primary key
                    foreach (var loc in allLocations)
                    {
                        if (loc == null)
                        {
                            continue;
                        }

                        var name = Path.GetFileNameWithoutExtension(loc.InternalId);
                        if (string.IsNullOrEmpty(name))
                        {
                            name = loc.PrimaryKey;
                        }

                        if (!string.IsNullOrEmpty(name) && !_preloadedGroupLocations.ContainsKey(name))
                        {
                            _preloadedGroupLocations[name] = loc;
                        }
                    }

                    // Second pass: ensure loaded GameObject names are also present in the mapping by trying to match InternalId
                    if (_preloadedGroup != null && _preloadedGroup.Count > 0)
                    {
                        foreach (var go in _preloadedGroup)
                        {
                            if (go == null || string.IsNullOrEmpty(go.name))
                            {
                                continue;
                            }

                            if (_preloadedGroupLocations.ContainsKey(go.name))
                            {
                                continue;
                            }

                            var locMatch = allLocations.FirstOrDefault(l =>
                                (!string.IsNullOrEmpty(l.PrimaryKey) && string.Equals(l.PrimaryKey, go.name, StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(l.InternalId) && l.InternalId.IndexOf(go.name, StringComparison.OrdinalIgnoreCase) >= 0));

                            if (locMatch != null)
                            {
                                _preloadedGroupLocations[go.name] = locMatch;
                            }
                        }
                    }
                }
                catch
                {
                    _preloadedGroupLocations = new System.Collections.Generic.Dictionary<string, IResourceLocation>(StringComparer.OrdinalIgnoreCase);
                }

                Debug.Log($"Preloaded {_preloadedGroup.Count} assets from keys list:{System.Environment.NewLine}" +
                          $"{string.Join(System.Environment.NewLine, _preloadedGroup).Replace("(UnityEngine.GameObject)", "")} ");

                _preloadTcs.TrySetResult(true);
                AddressablePackedReady?.Invoke(true);

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"StartPreloadGroupAssets (multiple keys) failed: {e}");
                _preloadedGroup = new List<GameObject>();
                _preloadTcs.TrySetResult(false);
                AddressablePackedReady?.Invoke(false);

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }
        }

        // Release the stored handle and clear cached data
        public virtual void ReleasePreloadedGroup()
        {
            // bump generation so any in-flight preload will not repopulate the cache
            _preloadGeneration++;

            var hasPinnedPreloadedGroup = false;
            if (_preloadedGroupKeys != null)
            {
                foreach (var key in _preloadedGroupKeys)
                {
                    if (IsPinned(key))
                    {
                        hasPinnedPreloadedGroup = true;
                        break;
                    }
                }
            }

            if (hasPinnedPreloadedGroup)
            {
                var preservedCount = _preloadedGroup?.Count ?? 0;
                Debug.Log($"Preserving preloaded group content {preservedCount} because at least one preloaded group key is pinned.");
                return;
            }

            if (_preloadHandle.IsValid())
            {
                Addressables.Release(_preloadHandle);
            }

            // Do not release pinned handles here. Pinned handles will be released when UnpinKey is called.
            // Rebuild _preloadedGroup to include only pinned GameObjects so FindFromCache can still work for them.
            var pinnedGameObjects = new List<GameObject>();
            foreach (var kv in _pinnedHandles)
            {
                var handle = kv.Value;
                if (handle.IsValid() && handle.Result is GameObject go)
                {
                    pinnedGameObjects.Add(go);
                }
            }

            var count = pinnedGameObjects?.Count ?? 0;
            Debug.Log($"Releasing preloaded group content {count}. Called from:\n{System.Environment.StackTrace}");

            _preloadHandle = default;
            _preloadedGroup = pinnedGameObjects;
            _preloadedGroupLocations = new Dictionary<string, IResourceLocation>();
            _preloadTcs = null;
        }

        // Pin/unpin operations
        public void PinKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_pinnedKeyRefCount.ContainsKey(key))
            {
                _pinnedKeyRefCount[key]++;
                Debug.Log($"AddressablesManager.PinKey: incremented pin for key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
                return;
            }

            _pinnedKeyRefCount[key] = 1;
            Debug.Log($"AddressablesManager.PinKey: pinned key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");

            // Note: PinKey only updates ref-count. Use LoadAndPinKey when you need Addressables to keep the asset loaded.
        }

        // Load the asset and keep its handle while pinned. This is the heavier operation and should be used
        // only when the caller requires the asset to remain loaded regardless of other releases.
        public AsyncOperationHandle<UnityEngine.Object> LoadAndPinKey(string key)
        {
            var defaultHandle = new AsyncOperationHandle<UnityEngine.Object>();
            if (string.IsNullOrEmpty(key)) return defaultHandle;

            if (_pinnedKeyRefCount.ContainsKey(key))
            {
                _pinnedKeyRefCount[key]++;
                Debug.Log($"AddressablesManager.LoadAndPinKey: incremented pin for key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
            }
            else
            {
                _pinnedKeyRefCount[key] = 1;
                Debug.Log($"AddressablesManager.LoadAndPinKey: pinned key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
            }

            // If we already have a loaded handle, return it.
            if (_pinnedHandles.ContainsKey(key) && _pinnedHandles[key].IsValid())
            {
                Debug.Log($"AddressablesManager.LoadAndPinKey: handle already present for key='{key}'");
                return _pinnedHandles[key];
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
                handle.WaitForCompletion();
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    _pinnedHandles[key] = handle;
                    Debug.Log($"AddressablesManager.LoadAndPinKey: loaded and pinned handle for key='{key}'");
                    return handle;
                }
                else
                {
                    Debug.LogWarning($"AddressablesManager.LoadAndPinKey: failed to load asset for key='{key}' - status={handle.Status}");
                    // Cleanup if load failed
                    if (_pinnedKeyRefCount.ContainsKey(key)) _pinnedKeyRefCount[key]--;
                    if (_pinnedKeyRefCount.ContainsKey(key) && _pinnedKeyRefCount[key] <= 0) _pinnedKeyRefCount.Remove(key);
                    if (handle.IsValid()) Addressables.Release(handle);
                    return defaultHandle;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AddressablesManager.LoadAndPinKey: exception while loading key='{key}': {e}");
                if (_pinnedKeyRefCount.ContainsKey(key)) _pinnedKeyRefCount.Remove(key);
                return defaultHandle;
            }
        }

        // Async variant that does not block the main thread. Use from coroutines or async Start/Awake.
        public async System.Threading.Tasks.Task<AsyncOperationHandle<UnityEngine.Object>> LoadAndPinKeyAsync(string key)
        {
            var defaultHandle = new AsyncOperationHandle<UnityEngine.Object>();
            if (string.IsNullOrEmpty(key)) return defaultHandle;

            if (_pinnedKeyRefCount.ContainsKey(key))
            {
                _pinnedKeyRefCount[key]++;
                Debug.Log($"AddressablesManager.LoadAndPinKeyAsync: incremented pin for key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
            }
            else
            {
                _pinnedKeyRefCount[key] = 1;
                Debug.Log($"AddressablesManager.LoadAndPinKeyAsync: pinned key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
            }

            // If we already have a loaded handle, return it.
            if (_pinnedHandles.ContainsKey(key) && _pinnedHandles[key].IsValid())
            {
                Debug.Log($"AddressablesManager.LoadAndPinKeyAsync: handle already present for key='{key}'");
                return _pinnedHandles[key];
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(key);
                await handle.Task;
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    _pinnedHandles[key] = handle;
                    Debug.Log($"AddressablesManager.LoadAndPinKeyAsync: loaded and pinned handle for key='{key}'");
                    return handle;
                }
                else
                {
                    Debug.LogWarning($"AddressablesManager.LoadAndPinKeyAsync: failed to load asset for key='{key}' - status={handle.Status}");
                    // Cleanup if load failed
                    if (_pinnedKeyRefCount.ContainsKey(key)) _pinnedKeyRefCount[key]--;
                    if (_pinnedKeyRefCount.ContainsKey(key) && _pinnedKeyRefCount[key] <= 0) _pinnedKeyRefCount.Remove(key);
                    if (handle.IsValid()) Addressables.Release(handle);
                    return defaultHandle;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AddressablesManager.LoadAndPinKeyAsync: exception while loading key='{key}': {e}");
                if (_pinnedKeyRefCount.ContainsKey(key)) _pinnedKeyRefCount.Remove(key);
                return defaultHandle;
            }
        }

        public void UnpinKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!_pinnedKeyRefCount.ContainsKey(key))
            {
                Debug.LogWarning($"AddressablesManager.UnpinKey: key not pinned='{key}'");
                return;
            }

            _pinnedKeyRefCount[key]--;
            Debug.Log($"AddressablesManager.UnpinKey: decremented pin for key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");
            if (_pinnedKeyRefCount[key] > 0) return;

            _pinnedKeyRefCount.Remove(key);
            Debug.Log($"AddressablesManager.UnpinKey: fully unpinned key='{key}', totalPinnedRefCount={_pinnedKeyRefCount.Values.Sum()}");

            if (_pinnedHandles.TryGetValue(key, out var handle))
            {
                // Debug: log what we're about to release and detect if it matches the PrefabManager's material
                var res = handle.IsValid() ? handle.Result : null as UnityEngine.Object;
                var resName = res != null ? res.name : "(null)";
                var resId = res != null ? res.GetInstanceID() : 0;
                Debug.Log($"AddressablesManager.UnpinKey: releasing handle for key='{key}', result={resName}, instanceID={resId}");

                if (handle.IsValid())
                {
                    try
                    {
                        Addressables.Release(handle);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"AddressablesManager.UnpinKey: failed to release handle for key='{key}': {e}");
                    }
                }

                _pinnedHandles.Remove(key);
            }
        }

        public bool IsPinned(string key)
        {
            return !string.IsNullOrEmpty(key) && _pinnedKeyRefCount.ContainsKey(key) && _pinnedKeyRefCount[key] > 0;
        }

        // Release a runtime instance that may have been created via Addressables.InstantiateAsync.
        // If the instance was created via Addressables we call Addressables.ReleaseInstance, otherwise fall back to UnityEngine.Object.Destroy.
        
        public virtual void RegisterAddressablesInstancedObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            lock (_addressablesInstancedObjects)
            {
                _addressablesInstancedObjects.Add(go);
            }
        }

        public virtual void ReleaseInstance(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj is GameObject go)
            {
                ReleaseInstance(go);
                return;
            }

            if (obj is Component comp && comp.gameObject != null)
            {
                ReleaseInstance(comp.gameObject);
                return;
            }

            UnityEngine.Object.Destroy(obj);
        }

        public virtual void ReleaseInstance(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var removed = false;
            lock (_addressablesInstancedObjects)
            {
                if (_addressablesInstancedObjects.Contains(go))
                {
                    _addressablesInstancedObjects.Remove(go);
                    removed = true;
                }
            }

            if (removed)
            {
                Addressables.ReleaseInstance(go);
                return;
            }

            UnityEngine.Object.Destroy(go);
        }

        public virtual async System.Threading.Tasks.Task<BlockAndDagger.Core.AddressablesPreloadResult> ReleasePreloadedGroupAsync(System.Collections.Generic.List<string> newAssetGroups = null)
        {
            var tcs = _preloadTcs;

            _preloadGeneration++;


            if (newAssetGroups == null || newAssetGroups.Count == 0 || _preloadedGroupKeys == null || _preloadedGroupKeys.Count == 0)
            {
                if (_preloadHandle.IsValid())
                {
                    Addressables.Release(_preloadHandle);
                }

                var count = _preloadedGroup?.Count ?? 0;
                Debug.Log($"Releasing preloaded group content {count}. Called from:\n{System.Environment.StackTrace}");

                _preloadHandle = default;
                _preloadedGroup = new List<GameObject>();
                _preloadedGroupKeys = new HashSet<string>();
                _preloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"ReleasePreloadedGroupAsync: waiting preload failed: {e}");
                    }
                }

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }

            var newKeys = newAssetGroups.ToArray();
            var newSet = new HashSet<string>(newKeys, StringComparer.OrdinalIgnoreCase);

            var keepKeys = new HashSet<string>(_preloadedGroupKeys, StringComparer.OrdinalIgnoreCase);
            keepKeys.IntersectWith(newSet);

            if (keepKeys.SetEquals(new HashSet<string>(_preloadedGroupKeys, StringComparer.OrdinalIgnoreCase)))
            {
                _preloadTcs = null;
                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"ReleasePreloadedGroupAsync: waiting preload failed: {e}");
                    }
                }

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }

            if (keepKeys.Count == 0)
            {
                if (_preloadHandle.IsValid())
                {
                    Addressables.Release(_preloadHandle);
                }

                var count = _preloadedGroup?.Count ?? 0;
                Debug.Log($"Releasing preloaded group content {count}. Called from:\n{System.Environment.StackTrace}");

                _preloadHandle = default;
                _preloadedGroup = new List<GameObject>();
                _preloadedGroupKeys = new HashSet<string>();
                _preloadTcs = null;

                if (tcs != null)
                {
                    try
                    {
                        await tcs.Task;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"ReleasePreloadedGroupAsync: waiting preload failed: {e}");
                    }
                }

                return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
            }

            try
            {
                var allLocations = new List<IResourceLocation>();
                foreach (var key in keepKeys)
                {
                    var locHandle = Addressables.LoadResourceLocationsAsync(key);
                    await locHandle.Task;
                    if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result != null)
                    {
                        allLocations.AddRange(locHandle.Result);
                    }
                }

                if (allLocations.Count == 0)
                {
                    _preloadHandle = default;
                    _preloadedGroup = new List<GameObject>();
                    _preloadedGroupKeys = new HashSet<string>();
                    _preloadedGroupLocations = new Dictionary<string, IResourceLocation>();
                    _preloadTcs = null;
                    return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
                }

                var keys = allLocations.Select(l => l.PrimaryKey).Where(k => !string.IsNullOrEmpty(k)).Distinct();
                var handle = Addressables.LoadAssetsAsync<GameObject>(keys, null, Addressables.MergeMode.Union);
                _preloadHandle = handle;
                await handle.Task;

                if (handle.IsValid() && handle.Result != null)
                {
                    _preloadedGroup = handle.Result;
                    _preloadedGroupKeys = new HashSet<string>(keepKeys);

                    // rebuild mapping
                    _preloadedGroupLocations = new Dictionary<string, IResourceLocation>();
                    try
                    {
                        foreach (var loc in allLocations)
                        {
                            if (loc == null)
                            {
                                continue;
                            }

                            var name = Path.GetFileNameWithoutExtension(loc.InternalId);
                            if (string.IsNullOrEmpty(name))
                            {
                                name = loc.PrimaryKey;
                            }

                            if (!string.IsNullOrEmpty(name) && !_preloadedGroupLocations.ContainsKey(name))
                            {
                                _preloadedGroupLocations[name] = loc;
                            }
                        }
                    }
                    catch
                    {
                        _preloadedGroupLocations = new Dictionary<string, IResourceLocation>();
                    }

                    Debug.Log($"Kept {_preloadedGroup.Count} preloaded assets from groups: {string.Join(", ", keepKeys)}");
                }
                else
                {
                    if (_preloadHandle.IsValid())
                    {
                        Addressables.Release(_preloadHandle);
                    }

                    _preloadHandle = default;
                    _preloadedGroup = new List<GameObject>();
                    _preloadedGroupKeys = new HashSet<string>();
                    _preloadedGroupLocations = new Dictionary<string, IResourceLocation>();
                    _preloadTcs = null;
                }
            }
            catch (System.Exception)
            {
                if (_preloadHandle.IsValid())
                {
                    Addressables.Release(_preloadHandle);
                }

                _preloadHandle = default;
                _preloadedGroup = new List<GameObject>();
                _preloadedGroupKeys = new HashSet<string>();
                _preloadTcs = null;
            }

            if (tcs != null)
            {
                try
                {
                    await tcs.Task;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"ReleasePreloadedGroupAsync: waiting preload failed: {e}");
                }
            }

            return new BlockAndDagger.Core.AddressablesPreloadResult { PreloadedGroup = _preloadedGroup, PreloadedGroupKeys = new System.Collections.Generic.HashSet<string>(_preloadedGroupKeys) };
        }

        public virtual GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            var prefab = _preloadedGroup.FirstOrDefault(x => x != null && string.Equals(x.name, prefabName, StringComparison.OrdinalIgnoreCase));
            if (prefab != null)
            {
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

            try
            {
                var names = _preloadedGroup?.Select(g => g != null ? g.name : "(null)") ?? Enumerable.Empty<string>();
                var locKeys = _preloadedGroupLocations?.Keys ?? new System.Collections.Generic.Dictionary<string,IResourceLocation>().Keys;
                Debug.LogError($"{prefabName} not found! PreloadedGroupCount={_preloadedGroup?.Count ?? 0}, PreloadedGroupNames=[{string.Join(",", names)}], PreloadedLocationKeysCount={_preloadedGroupLocations?.Count ?? 0}, PreloadedLocationKeys=[{string.Join(",", locKeys)}]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{prefabName} not found! Additionally failed to build diagnostic info: {ex}");
            }

            return null;
        }

        protected virtual GameObject InstantiatePreloadedGameObject(GameObject prefab, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            if (_preloadedGroupLocations != null && _preloadedGroupLocations.TryGetValue(prefab.name, out var loc) && loc != null)
            {
                var handle = Addressables.InstantiateAsync(loc, pos, rot ?? Quaternion.identity, parent);
                var instance = handle.WaitForCompletion();
                if (instance != null)
                {
                    RegisterAddressablesInstancedObject(instance);
                    instance.name = prefab.name;
                    if (parent != null)
                    {
                        instance.transform.SetParent(parent);
                        instance.name = prefab.name;
                    }

                    return instance;
                }
            }

            Debug.LogWarning("NOOOOOOT WORKING");
            return null;
        }
        
        public List<ManifestEntry> GetNeededAddressGroups(List<TileType> distinctTileTypes)
        {
#if UNITY_EDITOR
            var folderPath = "Assets/Prefabs/Blocks";
            var distinctManifestEntries = new List<ManifestEntry>();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var tile in distinctTileTypes)
            {
                var name = tile.ToString();
                var guids = AssetDatabase.FindAssets(name + " t:Prefab", new[] { folderPath });
                if (guids != null && guids.Length > 0)
                {
                    var guid = guids[0];
                    var entry = settings?.FindAssetEntry(guid);
                    var groupName = entry?.parentGroup?.name ?? string.Empty;
                    distinctManifestEntries.Add(new ManifestEntry
                    {
                        guid = guid,
                        tileType = name,
                        group = groupName
                    });
                }
            }

            return distinctManifestEntries;
#else
            return new List<ManifestEntry>();
#endif
        }

    }
}