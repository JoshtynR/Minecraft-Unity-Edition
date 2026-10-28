using System.Collections.Generic;
using UnityEngine;

public partial class World : IFireWorld
{
    private BetaFireSimulation fluidFire;
    [System.NonSerialized] private List<ChunkData> runtimeFluidRuntimeChunks;
    private List<ChunkData> fluidRuntimeChunks => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeFluidRuntimeChunks, () => new List<ChunkData>());
    private void InitializeFluidRuntime()
    {
        fluidFire=new BetaFireSimulation(this,bound=>fluidRandom.NextInt(bound));
        fluidTicks.Bind(2,p=>fluidFire.Tick(p));
    }
    private void ResetFluidRuntime(){fluidRuntimeChunks.Clear();fluidFire=null;DisposeFluidEffects();}
    private void RegisterFluidRuntimeChunk(ChunkData chunk)
    {if(!fluidRuntimeChunks.Contains(chunk))fluidRuntimeChunks.Add(chunk);}
    private void NotifyFireChange(WaterPosition p)
    {
        for(int d=0;d<7;d++)
        {
            var q=d==0?p:p.Offset(d==1?-1:d==2?1:0,d==3?-1:d==4?1:0,d==5?-1:d==6?1:0);
            if(((IFireWorld)this).Type(q)!=BlockType.Fire)continue;
            if(!fluidFire.Supported(q))((IFireWorld)this).SetFire(q,-1);
            else fluidTicks.Schedule(q,2,40);
        }
    }
    private void RandomFluidTicks()
    {
        // Beta chooses 80 positions in each active 16x128x16 chunk per tick
        // with a separate LCG. Keep the LCG and gameplay Java RNG in saves.
        for(int c=fluidRuntimeChunks.Count-1;c>=0;c--)
        {
            var chunk=fluidRuntimeChunks[c];
            if(!worldData.chunkDict.ContainsKey(chunk.worldPos)||!worldData.chunkDataDict.TryGetValue(chunk.worldPos,out var live)||live!=chunk)
            {fluidRuntimeChunks.RemoveAt(c);continue;}
            for(int n=0;n<80;n++)
            {
                fluidRandomTickState=unchecked(fluidRandomTickState*3+1013904223);
                int bits=fluidRandomTickState>>2;
                var p=new WaterPosition(chunk.worldPos.x+(bits&15),bits>>16&127,chunk.worldPos.z+(bits>>8&15));
                if(!TryGetWaterBlock(BlockPos(p),out var block))continue;
                if(block.type==BlockType.Lava)fluidFire.IgniteFromLava(p);
                else if(block.type==BlockType.Fire)fluidFire.Tick(p);
                else if(block.type==BlockType.Grass)TickGrass(p);
                else if(WheatCropRules.IsCrop(block.type)||WheatCropRules.IsSoil(block.type))TickFarming(p,block);
            }
        }
    }
    BlockType IFireWorld.Type(WaterPosition p)
    {return TryGetWaterBlock(BlockPos(p),out var b)?b.type:BlockType.Nothing;}
    int IFireWorld.Age(WaterPosition p)=>TryGetWaterBlock(BlockPos(p),out var b)?b.waterLevel:0;
    bool IFireWorld.Solid(WaterPosition p)=>TryGetWaterBlock(BlockPos(p),out var b)&&SpecialBlockGeometry.NormalCube(b.type);
    void IFireWorld.ScheduleFire(WaterPosition p)=>fluidTicks.Schedule(p,2,40);
    void IFireWorld.SetFire(WaterPosition p,int age)
    {
        if(!TryGetWaterBlock(BlockPos(p),out var old)||old.type==BlockType.Nothing)return;
        if(age<0&&old.type==BlockType.Air)return;
        var type=age<0?BlockType.Air:BlockType.Fire;
        if(!CanReplaceChest(BlockPos(p),type))return;
        if(ApplyFluidState(BlockPos(p),type,age<0?0:age))
        {
            waterChanges[BlockPos(p)]=age<0?byte.MaxValue:(byte)(80+age);
            if(age>=0)fluidTicks.Schedule(p,2,40);
        }
    }
}
