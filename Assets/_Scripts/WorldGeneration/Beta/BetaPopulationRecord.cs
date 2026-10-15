using System;
using UnityEngine;

// Generation payload retained even before chest/spawner gameplay is implemented.
// Item IDs and damage use the Beta registry, not BlockType local IDs.
[Serializable]
public sealed class BetaPopulationRecord
{
    public Vector3Int position;
    public string kind;
    public int slot, item, count, damage, metadata;
    public string mob;
}
