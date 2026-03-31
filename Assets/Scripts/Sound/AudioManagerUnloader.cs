using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BlockAndDagger.Sound
{
    public sealed class AudioManagerUnloader
    {
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _handles = new();
        public void AddHandle(string key, AsyncOperationHandle<AudioClip> handle)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (!handle.IsValid()) return;
            _handles[key] = handle;
        }

        public bool TryGetHandle(string key, out AsyncOperationHandle<AudioClip> handle)
        {
            return _handles.TryGetValue(key, out handle);
        }

        public bool Contains(string key)
        {
            return _handles.ContainsKey(key);
        }

        public void UnloadClip(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (_handles.TryGetValue(key, out var h))
            {
                if (h.IsValid()) Addressables.Release(h);
                _handles.Remove(key);
            }
        }

        public void UnloadAll()
        {
            foreach (var kv in _handles)
            {
                if (kv.Value.IsValid()) Addressables.Release(kv.Value);
            }
            _handles.Clear();
        }
    }
}
