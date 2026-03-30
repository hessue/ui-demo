using System;
using System.Linq;
using UnityEngine;

namespace BlockAndDagger
{
    //Should be replaced with NeighbourData
    public class LegacyLookups
    {
        
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

        // Robust grid-based lookup using rounded integer grid coordinates from GetXYZGridLocation
        private Block FindBlockByGridPosition(Block[] ground, Vector3Int gridPos)
        {
            return ground.FirstOrDefault(b => b.GetXYZGridLocation() == gridPos);
        }
        
#endregion
        
    }
}
