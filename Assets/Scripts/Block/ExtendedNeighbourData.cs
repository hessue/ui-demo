using System;
using UnityEngine;

namespace BlockAndDagger
{
    //Experimental class, might ditch this
    [Serializable]
    public class ExtendedNeighbourData : NeighbourData
    {
        [SerializeField] private Transform _eastUnderTransform;
        [SerializeField] private Transform _westUnderTransform;
        
        private IBlock _eastUnder;
        private IBlock _westUnder;
        public IBlock EastUnder => _eastUnder;
        public IBlock WestUnder => _westUnder;

        public ExtendedNeighbourData(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under, IBlock eastUnder, IBlock westUnder)
            : base(north, east, south, west, above, under)
        {
            _eastUnder = eastUnder;
            _westUnder = westUnder;
            _eastUnderTransform = eastUnder?.transform;
            _westUnderTransform = westUnder?.transform;
        }
    }
}
