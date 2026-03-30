using BlockAndDagger;
using UnityEngine;

public interface IFocusableBlock
{
    public IBlock FocusedBlock { get; set; }

    public TileType[] UnlockedBlockTypes { get; }
}
