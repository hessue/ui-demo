using UnityEngine;

namespace BlockAndDagger
{
    /// <summary>
    /// Uses different inputActions asset than in game Player (MenuInputActions vs PlayerInputActions)
    /// </summary>
    public sealed class LevelBuilderPlayer : IFocusableBlock
    {
        [field: SerializeField] public IBlock FocusedBlock { get; set; }

        public TileType[] UnlockedBlockTypes
        {
            get;
            private set;
        } = new[] { TileType.Barrel, TileType.Crate, TileType.Slope, TileType.Wall, TileType.Wall, TileType.Wall, TileType.Fence, TileType.Fence, TileType.Fence, TileType.Fence};
    }
}
