using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BlockAndDagger
{
    ///  Utilized only with the static grid currently
    public sealed class TilemapManager : MonoBehaviour
    {
        public static  Dictionary<string, Transform> _tilemaps = new();
        private Transform _grid;

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
            return _tilemaps.First(x => x.Key == name).Value;
        }

        public void ClearAll()
        {
            if (_tilemaps == null || _tilemaps.Count == 0)
                return;

            foreach (var tilemap in _tilemaps)
            {
                //Note! the StaticGrid has other objects as well currently, like water plane
                var transforms = tilemap.Value.GetComponentsInChildren<IBlock>();
                foreach (var t in transforms)
                {
                    Destroy(t.transform.gameObject);
                }
            }
        }
        

    }
}