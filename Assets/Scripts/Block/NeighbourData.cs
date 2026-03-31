using System;
using UnityEngine;

namespace BlockAndDagger
{
    public interface INeighbourData
    {
        IBlock North { get; }
        IBlock East { get; }
        IBlock South { get; }
        IBlock West { get; }
        IBlock Above { get; }
        IBlock Under { get; }
    }

    [Serializable]
    public class NeighbourData : INeighbourData
    {
        [SerializeField] private Transform _northTransform;
        [SerializeField] private Transform _eastTransform;
        [SerializeField] private Transform _southTransform;
        [SerializeField] private Transform _westTransform;
        [SerializeField] private Transform _aboveTransform;
        [SerializeField] private Transform _underTransform;

        private IBlock _north;
        private IBlock _east;
        private IBlock _south;
        private IBlock _west;
        private IBlock _above;
        private IBlock _under;

        public IBlock North => _north;
        public IBlock East => _east;
        public IBlock South => _south;
        public IBlock West => _west;
        public IBlock Above => _above;
        public IBlock Under => _under;

        public NeighbourData(IBlock north, IBlock east, IBlock south, IBlock west, IBlock above, IBlock under)
        {
            _north = north;
            _east = east;
            _south = south;
            _west = west;
            _above = above;
            _under = under;

            _northTransform = north?.transform;
            _eastTransform = east?.transform;
            _southTransform = south?.transform;
            _westTransform = west?.transform;
            _aboveTransform = above?.transform;
            _underTransform = under?.transform;
        }
    }
}
