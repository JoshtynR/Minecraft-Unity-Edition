using UnityEngine;

public static class WaterBlock
{
    public static Block Create(Vector3Int position, ChunkSection section) => new Block(BlockType.Water, position, section);
}
