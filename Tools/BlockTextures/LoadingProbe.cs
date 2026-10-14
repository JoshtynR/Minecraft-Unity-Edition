using System.Collections.Generic;

// Compile the unchanged production Block.Loaded method against the real
// BlockDataManager lookup; only section lighting/storage are replaced.
public partial class LoadingProbe
{
    public BlockType type;
    public int light;
    public BlockTypeData BlockData => BlockDataManager.blockTypeDataDictionary[(int)type];
    public ProbeChunk chunkData = new ProbeChunk();
    public void SetBlockLight(int value) { light = value; }
}
public class ProbeChunk { public Queue<BlockLightNode> blockLightUpdateQueue = new Queue<BlockLightNode>(); }
public struct BlockLightNode
{
    public LoadingProbe block;
    public byte lightLevel;
    public BlockLightNode(LoadingProbe block, byte lightLevel) { this.block = block; this.lightLevel = lightLevel; }
}
