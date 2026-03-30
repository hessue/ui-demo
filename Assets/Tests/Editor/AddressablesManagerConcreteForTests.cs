using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlockAndDagger.Core;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tests
{
    public class AddressablesManagerConcreteForTests : AddressablesManager
    {
        private HashSet<string> _preloadedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<GameObject> _preloadedGroup = new List<GameObject>();
        private readonly Dictionary<int, int> _trackedInstanceRefs = new Dictionary<int, int>();

        public IReadOnlyDictionary<int, int> TrackedInstanceRefs => _trackedInstanceRefs;

        public override IList<GameObject> GetPreloadedGroup()
        {
            return _preloadedGroup;
        }

        public override AsyncOperationHandle<IList<GameObject>> GetPreloadHandle()
        {
            return default;
        }

        public override Task<AddressablesPreloadResult> StartPreloadGroupAssets(IEnumerable<string> folderAddressKeys)
        {
            var result = new AddressablesPreloadResult();
            if (folderAddressKeys == null)
            {
                _preloadedGroup.Clear();
                _preloadedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result.PreloadedGroup = _preloadedGroup;
                result.PreloadedGroupKeys = _preloadedGroupKeys;
                return Task.FromResult(result);
            }

            var requestedSet = new HashSet<string>(folderAddressKeys, StringComparer.OrdinalIgnoreCase);
            if (_preloadedGroupKeys.Count > 0 && _preloadedGroupKeys.IsSupersetOf(requestedSet))
            {
                result.PreloadedGroup = _preloadedGroup;
                result.PreloadedGroupKeys = _preloadedGroupKeys;
                return Task.FromResult(result);
            }

            _preloadedGroup.Clear();
            _preloadedGroupKeys = requestedSet;
            result.PreloadedGroup = _preloadedGroup;
            result.PreloadedGroupKeys = _preloadedGroupKeys;
            return Task.FromResult(result);
        }

        public override Task<AddressablesPreloadResult> ReleasePreloadedGroupAsync(List<string> newAssetGroups = null)
        {
            var result = new AddressablesPreloadResult();
            if (newAssetGroups == null || newAssetGroups.Count == 0 || _preloadedGroupKeys.Count == 0)
            {
                _preloadedGroup.Clear();
                _preloadedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result.PreloadedGroup = _preloadedGroup;
                result.PreloadedGroupKeys = _preloadedGroupKeys;
                return Task.FromResult(result);
            }

            var newKeys = new HashSet<string>(newAssetGroups, StringComparer.OrdinalIgnoreCase);
            _preloadedGroupKeys.IntersectWith(newKeys);
            _preloadedGroup.Clear();
            result.PreloadedGroup = _preloadedGroup;
            result.PreloadedGroupKeys = new HashSet<string>(_preloadedGroupKeys, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(result);
        }

        public override void ReleasePreloadedGroup()
        {
            _preloadedGroup.Clear();
            _preloadedGroupKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public override GameObject InstantiatePrefab(string addressKey, Vector3 position, Transform parent, Quaternion? rotation = null)
        {
            return null;
        }

        public override GameObject FindFromCacheAndInstantiatePrefab(string prefabName, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            return null;
        }

        protected override GameObject InstantiatePreloadedGameObject(GameObject prefab, Vector3 pos, Transform parent = null, Quaternion? rot = null)
        {
            return null;
        }

        public override void RegisterAddressablesInstancedObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var id = go.GetInstanceID();
            if (_trackedInstanceRefs.ContainsKey(id))
            {
                _trackedInstanceRefs[id]++;
            }
            else
            {
                _trackedInstanceRefs[id] = 1;
            }
        }

        public override void ReleaseInstance(UnityEngine.Object obj)
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

            if (obj is Component component && component.gameObject != null)
            {
                ReleaseInstance(component.gameObject);
            }
        }

        public override void ReleaseInstance(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var id = go.GetInstanceID();
            if (!_trackedInstanceRefs.ContainsKey(id))
            {
                return;
            }

            _trackedInstanceRefs[id]--;
            if (_trackedInstanceRefs[id] <= 0)
            {
                _trackedInstanceRefs.Remove(id);
            }
        }
    }
}
