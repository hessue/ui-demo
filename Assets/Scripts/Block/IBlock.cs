using UnityEngine;

namespace BlockAndDagger
{
    public interface IBlock : IBuildable
    {
        /// <summary>
        /// IBlock(EmptyBlock class) which has no transform until its actually instantiated "added" 
        /// </summary>
        public bool IsEmptyNew { get; set; }
        public JsonBlock Data { get; }
        public TileType TileType { get; }
        public BlockDepth GetGridFloor();
        public bool IsGoal { get; set; }
        
        public MetadataLabel[] MetadataField { get; }

        public Transform transform { get; } //or MonoBehaviour later on

        public INeighbourData NeighbourData { get; }

        public void SetNeighbours(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under, IBlock eastUnder, IBlock westUnder);
    }
}
