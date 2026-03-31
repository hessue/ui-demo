using System;
using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    //Should be replaced with NeighbourData
    public class LegacyLookups
    {
        private Block FindBlockAbove(IBlock block)
        {
            BlockDepth blockTypeAbove = block.GetGridFloor() + 1;
            //TODO: smarter logic with just arrays index
            var upperBlocks = GameManager.Instance.LevelMaker.m_activeLevel.LevelData.GetGroundBlocks(blockTypeAbove);
            var higherY = block.Data.y + 1;

            const float tolerance = 0.01f; //A bit overkill because we are just playing with few decimals (0,00)

            var found = upperBlocks.FirstOrDefault(x => Math.Abs(x.Data.x - block.Data.x) < tolerance
                                                        && Math.Abs(x.Data.y - higherY) < tolerance
                                                        && Math.Abs(x.Data.z - block.Data.z) < tolerance);

            return found;
        }
        
        //Ideally to replace with targetToReplace.NeighbourData.Above but not populated for the newly created blocks at the moment
        public Block FindMatchingBlockInArray(Block[] array, IBlock target, float higherY)
        {
            if (array == null)
            { return null; }

            foreach (var b in array)
            {
                if (b == null || b.Data == null)
                { continue; }

                if (Math.Abs(b.Data.x - target.Data.x) < TileDataTracker.VectorComparisonTolerance &&
                    Math.Abs(b.Data.y - higherY) < TileDataTracker.VectorComparisonTolerance &&
                    Math.Abs(b.Data.z - target.Data.z) < TileDataTracker.VectorComparisonTolerance)
                {
                    return b;
                }
            }

            return null;
        }
        
#region Level
        
        private Block FindBlockByPos(Block[] ground, Vector3 targetPos, float offsetX = 0)
        {
            return ground.FirstOrDefault(x =>
                Math.Abs(x.Data.x - (targetPos.x + offsetX)) < TileDataTracker.VectorComparisonTolerance &&
                Math.Abs(x.Data.y - targetPos.y) < TileDataTracker.VectorComparisonTolerance &&
                Math.Abs(x.Data.z - targetPos.z) < TileDataTracker.VectorComparisonTolerance);
        }

        // Robust grid-based lookup using rounded integer grid coordinates from GetXYZGridLocation
        private Block FindBlockByGridPosition(Block[] ground, Vector3Int gridPos)
        {
            return ground.FirstOrDefault(b => b.GetXYZGridLocation() == gridPos);
        }
        
#endregion
        
    }
}
