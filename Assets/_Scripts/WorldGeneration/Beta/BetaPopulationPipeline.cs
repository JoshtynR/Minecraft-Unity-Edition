using System;

public sealed partial class Beta173Population
{
    public void PopulateComplete(ChunkData owner, Beta173Terrain terrain, int chunkX, int chunkZ)
    {
        var world = new TreeWorld(owner);
        PopulateComplete(world, chunkX, chunkZ, terrain.GetBiome(chunkX*16+16,chunkZ*16+16),
            terrain.GetTreeCountNoise(chunkX*16,chunkZ*16),terrain.GetPopulationTemperaturesForChunk(chunkX*16+8,chunkZ*16+8));
        world.MarkDirtyChunks();
    }
    public void PopulateComplete(IBetaPopulationWorld w,int chunkX,int chunkZ,BetaBiomeType biome,double treeNoise,
        double[] temperatures,Action<string,long> observe=null)
    {
        var r=PopulationRandom(chunkX,chunkZ);int ox=chunkX*16,oz=chunkZ*16;
        PopulateLakes(w,r,ox,oz);observe?.Invoke("lakes",r.State);
        for(int i=0;i<8;i++){int x=ox+r.NextInt(16)+8,y=r.NextInt(128),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Dungeon(w,r,x,y,z);}
        observe?.Invoke("dungeons",r.State);
        Veins(w,r,ox,oz,10,128,BlockType.Clay,32,true);observe?.Invoke("clay",r.State);
        Veins(w,r,ox,oz,20,128,BlockType.Dirt,32);observe?.Invoke("dirt",r.State);
        Veins(w,r,ox,oz,10,128,BlockType.Gravel,32);observe?.Invoke("gravel",r.State);
        Veins(w,r,ox,oz,20,128,BlockType.CoalOre,16);observe?.Invoke("coal",r.State);
        Veins(w,r,ox,oz,20,64,BlockType.IronOre,8);observe?.Invoke("iron",r.State);
        Veins(w,r,ox,oz,2,32,BlockType.GoldOre,8);observe?.Invoke("gold",r.State);
        Veins(w,r,ox,oz,8,16,BlockType.RedstoneOre,7);observe?.Invoke("redstone",r.State);
        Veins(w,r,ox,oz,1,16,BlockType.DiamondOre,7);observe?.Invoke("diamond",r.State);
        int lx=ox+r.NextInt(16),ly=r.NextInt(16)+r.NextInt(16),lz=oz+r.NextInt(16);
        BetaPopulationFeatures.Vein(w,r,lx,ly,lz,BlockType.LapisOre,6);observe?.Invoke("lapis",r.State);
        int trees=Beta173Trees.AttemptCount(biome,treeNoise,r);
        for(int i=0;i<trees;i++)
        {
            int x=ox+r.NextInt(16)+8,z=oz+r.NextInt(16)+8;
            var kind=Beta173Trees.Select(biome,r);
            Beta173Trees.Generate(w,r,kind,x,w.Height(x,z),z);
        }
        observe?.Invoke("trees",r.State);
        int flowers=biome==BetaBiomeType.Forest?2:biome==BetaBiomeType.SeasonalForest?4:biome==BetaBiomeType.Taiga?2:biome==BetaBiomeType.Plains?3:0;
        for(int i=0;i<flowers;i++)PlantAttempt(w,r,ox,oz,BlockType.Dandelion);
        int grass=biome==BetaBiomeType.Rainforest||biome==BetaBiomeType.Plains?10:biome==BetaBiomeType.Forest||biome==BetaBiomeType.SeasonalForest?2:biome==BetaBiomeType.Taiga?1:0;
        for(int i=0;i<grass;i++)
        {
            var type=biome==BetaBiomeType.Rainforest&&r.NextInt(3)!=0?BlockType.Fern:BlockType.TallGrass;
            PlantAttempt(w,r,ox,oz,type);
        }
        if(biome==BetaBiomeType.Desert)for(int i=0;i<2;i++)PlantAttempt(w,r,ox,oz,BlockType.DeadBush);
        if(r.NextInt(2)==0)PlantAttempt(w,r,ox,oz,BlockType.Rose);
        if(r.NextInt(4)==0)PlantAttempt(w,r,ox,oz,BlockType.BrownMushroom);
        if(r.NextInt(8)==0)PlantAttempt(w,r,ox,oz,BlockType.RedMushroom);
        observe?.Invoke("plants",r.State);
        for(int i=0;i<10;i++){int x=ox+r.NextInt(16)+8,y=r.NextInt(128),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Reeds(w,r,x,y,z);}
        if(r.NextInt(32)==0){int x=ox+r.NextInt(16)+8,y=r.NextInt(128),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Pumpkins(w,r,x,y,z);}
        if(biome==BetaBiomeType.Desert)for(int i=0;i<10;i++){int x=ox+r.NextInt(16)+8,y=r.NextInt(128),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Cacti(w,r,x,y,z);}
        observe?.Invoke("reeds-pumpkins-cacti",r.State);
        for(int i=0;i<50;i++){int x=ox+r.NextInt(16)+8,y=r.NextInt(r.NextInt(120)+8),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Spring(w,r,x,y,z,BlockType.Water);}
        for(int i=0;i<20;i++){int x=ox+r.NextInt(16)+8,y=r.NextInt(r.NextInt(r.NextInt(112)+8)+8),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Spring(w,r,x,y,z,BlockType.Lava);}
        observe?.Invoke("springs",r.State);
        for(int x=ox+8;x<ox+24;x++)for(int z=oz+8;z<oz+24;z++)
        {
            int top=-1;
            for(int y=127;y>0;y--)if(BetaPopulationFeatures.Solid(w.Get(x,y,z))||BetaPopulationFeatures.Liquid(w.Get(x,y,z))){top=y+1;break;}
            if(top>0&&top<128&&temperatures[(x-ox-8)*16+z-oz-8]-(top-64)/64.0*.3<.5&&w.Get(x,top,z)==BlockType.Air&&
                BetaPopulationFeatures.Solid(w.Get(x,top-1,z))&&w.Get(x,top-1,z)!=BlockType.Ice)w.Set(x,top,z,BlockType.SnowLayer);
        }
        observe?.Invoke("snow",r.State);
    }
    private static void Veins(IBetaPopulationWorld w,JavaRandom r,int ox,int oz,int attempts,int height,BlockType type,int size,bool clay=false)
    {
        for(int i=0;i<attempts;i++){int x=ox+r.NextInt(16),y=r.NextInt(height),z=oz+r.NextInt(16);BetaPopulationFeatures.Vein(w,r,x,y,z,type,size,clay);}
    }
    private static void PlantAttempt(IBetaPopulationWorld w,JavaRandom r,int ox,int oz,BlockType type)
    {
        int x=ox+r.NextInt(16)+8,y=r.NextInt(128),z=oz+r.NextInt(16)+8;BetaPopulationFeatures.Plants(w,r,x,y,z,type);
    }
}
