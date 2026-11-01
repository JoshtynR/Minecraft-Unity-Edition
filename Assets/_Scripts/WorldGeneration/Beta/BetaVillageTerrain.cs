using System.Collections.Generic;

// Called only while Beta18Villages holds its plan lock. Samples unmodified
// terrain, never loaded chunks, so plans cannot depend on streaming/player edits.
public sealed class BetaVillageTerrain : IBetaVillageTerrain
{
    private readonly Beta173Terrain terrain;
    private readonly Beta173Caves caves;
    private readonly Dictionary<long,BetaVillageGround[]> columns=new Dictionary<long,BetaVillageGround[]>();
    private readonly Queue<long> order=new Queue<long>();
    public BetaVillageTerrain(Beta173Terrain terrain,Beta173Caves caves){this.terrain=terrain;this.caves=caves;}
    public BetaVillageGround Ground(int x,int z)
    {
        int cx=Beta18Villages.FloorDiv(x,16),cz=Beta18Villages.FloorDiv(z,16);
        long key=unchecked(((long)cx<<32)|(uint)cz);
        if(!columns.TryGetValue(key,out var ground))
        {
            // GenerateRawTerrainForChunk returns a thread scratch array. Copy
            // the ground samples before another chunk request can reuse it.
            var raw=terrain.GenerateRawTerrainForChunk(cx*16,cz*16);
            BetaSurfaceDecorator.DecorateChunk(raw,terrain,cx,cz);caves.Generate(raw,cx,cz);
            var biomes=terrain.GenerateBiomeRegion(cx*16,cz*16,16,16);
            ground=new BetaVillageGround[256];
            for(int lx=0;lx<16;lx++)for(int lz=0;lz<16;lz++)
            {
                int y=127;while(y>0&&raw[lx,y,lz]==BlockType.Air)y--;
                ground[lx+lz*16]=new BetaVillageGround{height=y+1,top=raw[lx,y,lz],biome=biomes[lx*16+lz]};
            }
            if(columns.Count>=128)columns.Remove(order.Dequeue());columns.Add(key,ground);order.Enqueue(key);
        }
        return ground[x-cx*16+(z-cz*16)*16];
    }
}
