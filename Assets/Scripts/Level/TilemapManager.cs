using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BlockAndDagger
{
    ///  Utilized only with the static grid currently
    public sealed class TilemapManager : MonoBehaviour
    {
        public static  Dictionary<string, Transform> _tilemaps = new();
        private Transform _grid;
        private readonly List<IBlock> _iBlockBuffer = new List<IBlock>(128);

        void Awake()
        {
            _grid = transform;
            _tilemaps = new Dictionary<string, Transform>();
            var tilemaps = _grid.GetComponentsInChildren<Tilemap>();
            
            //Most level blocks are in the 'NashMeshSurface' layer. Exclude the static ones from the process 
            int ExcludeFromNavMeshBaking = LayerMask.NameToLayer("Default"); 
            
            foreach (var tilemap in tilemaps)
            {
                tilemap.gameObject.layer = ExcludeFromNavMeshBaking;
                AddExistingGameObjectTilemap(tilemap, tilemap.transform.position.y, true);
            }
        }
        
        public void AddNewTilemap(string transformName, float posY, bool isStaticLayer = false)
        {
            throw new Exception("NOT TESTED YET");
            /*var tilemap = new GameObject(transformName).AddComponent<Tilemap>();
            tilemap.transform.position = new Vector3(0, posY, 0);
            _tilemaps.TryAdd(name, tilemap.transform);*/
        }

        public void AddExistingGameObjectTilemap(Tilemap tilemap, float posY, bool isStaticLayer = false)
        {
            _tilemaps.TryAdd(tilemap.name, tilemap.transform);
        }

        public Transform GetTileMap(string name)
        {
            if (_tilemaps.TryGetValue(name, out var t))
            {
                return t;
            }

            throw new InvalidOperationException($"Tilemap not found: {name}");
        }

        public void ClearAll()
        {
            if (_tilemaps == null || _tilemaps.Count == 0)
            {
                return;
            }

            foreach (var tilemap in _tilemaps)
            {
                var tm = tilemap.Value;
                if (tm == null)
                {
                    continue;
                }

                _iBlockBuffer.Clear();
                tm.GetComponentsInChildren<IBlock>(_iBlockBuffer);
                if (_iBlockBuffer.Count == 0)
                {
                    continue;
                }

                for (int i = 0, n = _iBlockBuffer.Count; i < n; ++i)
                {
                    GameManager.Instance?.AddressablesManager?.ReleaseInstance(_iBlockBuffer[i].transform.gameObject);
                }
            }
        }
        

    }
}