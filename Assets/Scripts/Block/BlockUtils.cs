using UnityEngine;

namespace BlockAndDagger
{
    public static class BlockUtils
    {
        public static Vector3 GetPos(this IBlock block)
        {
            return new Vector3(block.Data.x, block.Data.y, block.Data.z);
        }
        
        /// <summary>
        /// DO NOT FORGET to sync pos and xyz location
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public static Vector3Int GetXYZGridLocation(this IBlock block)
        {
            return TileDataTracker.ToGridPosition(new Vector3(block.Data.x, block.Data.y, block.Data.z));
        }
    }
}
