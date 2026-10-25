using UnityEngine;

// Legacy factory kept so old editor references still resolve. Runtime cells are
// packed and no longer use per-type Block subclasses.
public static class AirBlock
{
    public static Block Create(Vector3Int position, ChunkSection section = null) => new Block(BlockType.Air, position, section);
}
