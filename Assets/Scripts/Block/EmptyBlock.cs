using System;
using UnityEngine;

namespace BlockAndDagger
{
    public sealed class EmptyBlock : IBlock
    {
        private JsonBlock _data;
        private BlockDepth _blockDepth;

        public EmptyBlock(BlockDepth blockDepth, Vector3 focusedPosition)
        {
            IsEmptyNew = true;
            _blockDepth = blockDepth;
            _data = new JsonBlock()
            {
                x = focusedPosition.x,
                y = focusedPosition.y,
                z = focusedPosition.z
            };
        }

        public bool IsEmptyNew { get; set; }
        public JsonBlock Data => _data;
        public TileType TileType => _data.type;
        public bool IsGoal { get; set; }
        
        [field: SerializeField] public MetadataLabel[] MetadataField { get; private set; }

        public BlockDepth GetGridFloor()
        {
            return _blockDepth;
        }
        
        public Transform transform => throw new NotSupportedException("EmptyBlock will not have transform");

        public INeighbourData NeighbourData { get; set; }
        
        public void SetNeighbours(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under, IBlock eastUnder, IBlock westUnder)
        {
            NeighbourData = new ExtendedNeighbourData( north, east, south, west, above, under, eastUnder, westUnder);
        }

        public bool IsBuild { get; set; }

        public float BuildTime => throw new NotSupportedException();

        public void SetBuildState(bool completed)
        {
            throw new NotSupportedException();
        }
    }
}