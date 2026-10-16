using System.Collections.Generic;
using UnityEngine;

public class TargetWall : MonoBehaviour
{
    [SerializeField] private WallBlock[] blocks;

    public IReadOnlyList<WallBlock> Blocks => blocks;

    public void Restore()
    {
        foreach (WallBlock block in blocks) block.Restore();
        Physics.SyncTransforms();
    }
}
