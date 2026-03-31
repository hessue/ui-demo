using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class TileData
    {
        public readonly Vector3 PosInGrid;
        public IBlock Block;

        public TileData(Vector3 posInGrid, IBlock block)
        {
            PosInGrid = posInGrid;
            Block = block;
        }
    }

    /// <summary>
    /// NOT Grid and Tilemap compatible which use Vector3Int!
    /// </summary>
    /// TODO it needs the transformation from world to grid and vica-versa(tilemap.WorldToCell and tilemap.GetCellCenterWorld) which is currently not included
    public sealed class TileDataTracker //TODO: create singleton
    {
        public static float VectorComparisonTolerance = 0.20f;

        public readonly Dictionary<Vector3, TileData> TileDataList = new();

        /*public TileData GetData(Vector3 pos)
        {
            // a totally new approach is needed If this ever fails
            return TileDataList.First(x =>
                Math.Abs(x.Key.x - pos.x) < VectorComparisonTolerance &&
                Math.Abs(x.Key.y - pos.y) < VectorComparisonTolerance &&
                Math.Abs(x.Key.z - pos.z) < VectorComparisonTolerance).Value;
        }*/

        public void AddBlocksToList(Block[] blocks)
        {
            foreach (var block in blocks)
            {
                AddOrReplaceTile(new Vector3(block.Data.x, block.Data.y, block.Data.z), block);
            }
        }

        public void AddOrReplaceTile(Vector3 pos
            , IBlock block) //TODO: split removing separate
        {
            //a totally new approach is needed If this ever fails
            var exists = TileDataList.FirstOrDefault(x =>
                Math.Abs(x.Key.x - pos.x) < VectorComparisonTolerance &&
                Math.Abs(x.Key.y - pos.y) < VectorComparisonTolerance &&
                Math.Abs(x.Key.z - pos.z) < VectorComparisonTolerance).Value;

            if (exists != null)
            {
                exists.Block = block;
            }
            else
            {
                var id = pos;
                TileDataList.Add(id, new TileData(id, block));
            }

            block.IsEmptyNew = false;
        }

        ///TODO: GameObject must be destroyed also
        public void OnlyDetach(Vector3 pos)
        {
            if (!TileDataList.Remove(pos))
            {
                Debug.LogWarning($"Could not remove block from tracker at :{pos}");
            }
        }

        public void RemoveTile(IBlock block)
        {
            if (TileDataList.Count == 0 || block == null)
                return;
            
            var pos = block.GetPos(); //TODO: get Vector3Int from tilemap
            /*if (!TileDataList.Remove(pos))
            {
               // Debug.LogWarning($"Could not remove block '{block.TileType}' from tracker at :{pos}");
            }
            else
            {
                GameObject.Destroy(block.transform.gameObject);
            }*/

            try
            {
                if (block.transform != null && block.transform.gameObject != null)
                {
                    GameObject.Destroy(block.transform.gameObject);
                }
                TileDataList.Remove(pos);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                //throw;
            }
        }
    }
}