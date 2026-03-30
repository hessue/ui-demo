using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using BlockAndDagger.Core;

namespace BlockAndDagger.Setup
{
    public class EnsureAddressablesLoader : MonoBehaviour
    {
        // Perform synchronous initialization and loading so callers that need assets available immediately
        public static void Start(IAddressablesManager mgr, string[] keys)
        {
            if (keys == null || keys.Length == 0)
            {
                return;
            }
            
            var initHandle = UnityEngine.AddressableAssets.Addressables.InitializeAsync();
            initHandle.WaitForCompletion();
            if (!initHandle.IsValid() || initHandle.Status != AsyncOperationStatus.Succeeded)
            {
                return;
            }

            var pm = FindFirstObjectByType<PrefabManager>();
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                try
                {
                    var handle = mgr.LoadAndPinKey(key);
                    if (handle.IsValid() && handle.Result != null)
                    {
                        // If prefab, instantiate so Awake/singletons run and object exists in scene.
                        if (handle.Result is GameObject prefabGo)
                        {
                            var instance = Instantiate(prefabGo);
                            instance.name = prefabGo.name; // preserve name for lookups
                            mgr.RegisterAddressablesInstancedObject(instance);
                            DontDestroyOnLoad(instance);
                            continue;
                        }

                        // Try to assign typed assets to PrefabManager. Re-query PrefabManager in case it was instantiated by a previously-loaded prefab.
                        if (pm == null)
                        {
                            pm = FindFirstObjectByType<PrefabManager>();
                        }

                        if (pm != null)
                        {
                            if (handle.Result is LevelSelectionAssets levelAssets)
                            {
                                pm.LevelSelectionAssets = levelAssets;
                                continue;
                            }

                            if (handle.Result is CrossSceneAssets cross)
                            {
                                pm.CrossSceneAssets = cross;
                                continue;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"EnsureAddressablesLoader: exception loading key='{key}' synchronously: {e}");
                }
            }
        }
    }
}
