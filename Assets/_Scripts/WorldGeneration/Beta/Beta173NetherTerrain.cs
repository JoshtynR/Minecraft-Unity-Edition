using System;

// Chunk-local Beta Nether terrain. All coordinates here are canonical Beta coordinates.
public sealed class Beta173NetherTerrain
{
    public const int TerrainHeight = 128, SeaLevel = 32;
    private readonly BetaOctaveNoise minLimit, maxLimit, selector, surface, thickness;
    private readonly Beta173NetherCaves caves;
    [ThreadStatic] private static NetherScratch threadScratch;
    private static NetherScratch Scratch => threadScratch ?? (threadScratch=new NetherScratch());

    private sealed class NetherScratch
    {
        public readonly double[] low=new double[425],high=new double[425],select=new double[425],vertical=new double[17];
        public readonly double[] sand=new double[256],gravel=new double[256],depth=new double[256];
        public readonly double[,,] density=new double[5,17,5];
        public readonly BlockType[,,] blocks=new BlockType[16,TerrainHeight,16];
    }
    public Beta173NetherTerrain(long seed)
    {
        var random = new JavaRandom(seed);
        minLimit = new BetaOctaveNoise(random,16); maxLimit = new BetaOctaveNoise(random,16);
        selector = new BetaOctaveNoise(random,8); surface = new BetaOctaveNoise(random,4);
        thickness = new BetaOctaveNoise(random,4);
        // These unused density terms still consume the original constructor RNG.
        new BetaOctaveNoise(random,10); new BetaOctaveNoise(random,16);
        caves = new Beta173NetherCaves(seed);
    }
    public BlockType[,,] Generate(int chunkX,int chunkZ)
    {
        var blocks=GenerateRaw(chunkX,chunkZ);
        Surface(blocks,chunkX,chunkZ); caves.Generate(blocks,chunkX,chunkZ); return blocks;
    }

    // Streaming consumes the result before the worker takes another chunk, so
    // its large noise/density/block buffers can safely be reused per worker.
    internal BlockType[,,] GenerateForChunk(int chunkX,int chunkZ)
    {
        var scratch=Scratch;
        var blocks=GenerateRawCore(chunkX,chunkZ,scratch.blocks,scratch);
        SurfaceCore(blocks,chunkX,chunkZ,scratch);caves.Generate(blocks,chunkX,chunkZ);return blocks;
    }

    public BlockType[,,] GenerateRaw(int chunkX,int chunkZ)
    {
        return GenerateRawCore(chunkX,chunkZ,new BlockType[16,TerrainHeight,16],null);
    }

    private BlockType[,,] GenerateRawCore(int chunkX,int chunkZ,BlockType[,,] blocks,NetherScratch scratch)
    {
        int cx=chunkX*4,cz=chunkZ*4;
        double[] low=minLimit.GenerateNoiseOctaves(scratch?.low,cx,0,cz,5,17,5,684.412,2053.236,684.412);
        double[] high=maxLimit.GenerateNoiseOctaves(scratch?.high,cx,0,cz,5,17,5,684.412,2053.236,684.412);
        double[] select=selector.GenerateNoiseOctaves(scratch?.select,cx,0,cz,5,17,5,684.412/80,2053.236/60,684.412/80);
        var vertical=scratch?.vertical??new double[17];
        for(int y=0;y<17;y++)
        {
            vertical[y]=Math.Cos(y*Math.PI*6/17)*2;
            double d=y>17/2?16-y:y;
            if(d<4){d=4-d;vertical[y]-=d*d*d*10;}
        }
        var density=scratch?.density??new double[5,17,5];int index=0;
        for(int x=0;x<5;x++)for(int z=0;z<5;z++)for(int y=0;y<17;y++)
        {
            double a=low[index]/512,b=high[index]/512,t=(select[index]/10+1)/2;
            double value=(t<0?a:t>1?b:a+(b-a)*t)-vertical[y];
            if(y>13){double fade=(double)((float)(y-13)/3f);value=value*(1-fade)+-10*fade;}
            density[x,y,z]=value;index++;
        }
        for (int cellX = 0; cellX < 4; cellX++)
        for (int cellZ = 0; cellZ < 4; cellZ++)
        for (int cellY = 0; cellY < 16; cellY++)
        {
            double d000 = density[cellX, cellY, cellZ], d001 = density[cellX, cellY, cellZ + 1];
            double d100 = density[cellX + 1, cellY, cellZ], d101 = density[cellX + 1, cellY, cellZ + 1];
            double dy000 = (density[cellX, cellY + 1, cellZ] - d000) * 0.125;
            double dy001 = (density[cellX, cellY + 1, cellZ + 1] - d001) * 0.125;
            double dy100 = (density[cellX + 1, cellY + 1, cellZ] - d100) * 0.125;
            double dy101 = (density[cellX + 1, cellY + 1, cellZ + 1] - d101) * 0.125;
            for (int subY = 0; subY < 8; subY++)
            {
                double x0z0 = d000, x0z1 = d001;
                double dxz0 = (d100 - d000) * 0.25, dxz1 = (d101 - d001) * 0.25;
                for (int subX = 0; subX < 4; subX++)
                {
                    double current = x0z0, dz = (x0z1 - x0z0) * 0.25;
                    for (int subZ = 0; subZ < 4; subZ++)
                    {
                        int x = cellX * 4 + subX, y = cellY * 8 + subY, z = cellZ * 4 + subZ;
                        BlockType block = BlockType.Air;
                        if (y < SeaLevel)
                            block = BlockType.Lava;
                        if (current > 0.0) block = BlockType.Netherrack;
                        blocks[x, y, z] = block; current += dz;
                    }
                    x0z0 += dxz0; x0z1 += dxz1;
                }
                d000 += dy000; d001 += dy001; d100 += dy100; d101 += dy101;
            }
        }
        return blocks;
    }

    public void Surface(BlockType[,,] blocks,int chunkX,int chunkZ)
    {
        SurfaceCore(blocks,chunkX,chunkZ,null);
    }

    private void SurfaceCore(BlockType[,,] blocks,int chunkX,int chunkZ,NetherScratch scratch)
    {
        const double scale=.03125;
        var sand=surface.GenerateNoiseOctaves(scratch?.sand,chunkX*16,chunkZ*16,0,16,16,1,scale,scale,1);
        var gravel=surface.GenerateNoiseOctaves(scratch?.gravel,chunkX*16,109.0134,chunkZ*16,16,1,16,scale,1,scale);
        var depth=thickness.GenerateNoiseOctaves(scratch?.depth,chunkX*16,chunkZ*16,0,16,16,1,scale*2,scale*2,scale*2);
        var random=new JavaRandom(unchecked((long)chunkX*341873128712L+(long)chunkZ*132897987541L));
        for(int k=0;k<16;k++)for(int l=0;l<16;l++)
        {
            int i=k+l*16;
            bool soul=sand[i]+random.NextDouble()*.2>0;
            bool coarse=gravel[i]+random.NextDouble()*.2>0;
            int thick=(int)(depth[i]/3+3+random.NextDouble()*.25),remaining=-1;
            BlockType top=BlockType.Netherrack,fill=BlockType.Netherrack;
            for(int y=127;y>=0;y--)
            {
                if(y>=127-random.NextInt(5)){blocks[l,y,k]=BlockType.Bedrock;continue;}
                if(y<=random.NextInt(5)){blocks[l,y,k]=BlockType.Bedrock;continue;}
                var block=blocks[l,y,k];
                if(block==BlockType.Air)remaining=-1;
                else if(block==BlockType.Netherrack)
                {
                    if(remaining==-1)
                    {
                        if(thick<=0){top=BlockType.Air;fill=BlockType.Netherrack;}
                        else if(y>=60&&y<=65)
                        {
                            top=coarse?BlockType.Gravel:BlockType.Netherrack;fill=BlockType.Netherrack;
                            if(soul)top=fill=BlockType.SoulSand;
                        }
                        if(y<64&&top==BlockType.Air)top=BlockType.Lava;
                        remaining=thick;blocks[l,y,k]=y>=63?top:fill;
                    }
                    else if(remaining>0){remaining--;blocks[l,y,k]=fill;}
                }
            }
        }
    }
}
