using System.Collections.Generic;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class TileData
    {
        public readonly Vector3Int PosInGrid;
        public IBlock Block { get; private set; }
        public bool IsOccupied => Block != null && !Block.IsEmptyNew;

        public TileData(Vector3Int posInGrid, IBlock block)
        {
            PosInGrid = posInGrid;
            Block = block;
        }

        public void SetBlock(IBlock block)
        {
            Block = block;
        }

        public void SetEmpty()
        {
            Block = null;
        }
    }

    public sealed class TileDataTracker //TODO: create singleton
    {
        public const float GridInverseCellSize = 2f; //GridCellSize is now 0.5
        public static int GridStep => Mathf.RoundToInt(GridInverseCellSize);
        public static float VectorComparisonTolerance = 0.20f;

        public readonly Dictionary<Vector3Int, TileData> TileDataList = new();

        public static Vector3Int ToGridPosition(Vector3 pos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(pos.x * GridInverseCellSize),
                Mathf.RoundToInt(pos.y * GridInverseCellSize),
                Mathf.RoundToInt(pos.z * GridInverseCellSize));
        }

        public void AddBlocksToList(Block[] blocks)
        {
            foreach (var block in blocks)
            {
                AddOrReplaceTile(block.GetXYZGridLocation(), block);
            }
        }

        public void AddOrReplaceTile(Vector3 pos, IBlock block)
        {
            AddOrReplaceTile(ToGridPosition(pos), block);
        }

        public void AddOrReplaceTile(Vector3Int pos, IBlock block)
        {
            if (block == null)
            {
                return;
            }

            if (TileDataList.TryGetValue(pos, out var tileData))
            {
                tileData.SetBlock(block);
            }
            else
            {
                TileDataList[pos] = new TileData(pos, block);
            }

            block.IsEmptyNew = false;
        }

        public void MarkKnownEmpty(Vector3 pos)
        {
            MarkKnownEmpty(ToGridPosition(pos));
        }

        public void MarkKnownEmpty(Vector3Int pos)
        {
            if (TileDataList.TryGetValue(pos, out var tileData))
            {
                tileData.SetEmpty();
            }
            else
            {
                TileDataList[pos] = new TileData(pos, null);
            }
        }

        public void OnlyDetach(Vector3 pos)
        {
            OnlyDetach(ToGridPosition(pos));
        }

        public void OnlyDetach(Vector3Int pos)
        {
            if (!TileDataList.Remove(pos))
            {
                Debug.LogWarning($"Could not remove block from tracker at :{pos}");
            }
        }

        public void RemoveTile(IBlock block)
        {
            if (TileDataList.Count == 0 || block == null)
            {
                return;
            }

            if (block.transform != null && block.transform.gameObject != null)
            {
                GameManager.Instance?.AddressablesManager?.ReleaseInstance(block.transform.gameObject);
            }

            MarkKnownEmpty(block.GetXYZGridLocation());
        }
    }
}