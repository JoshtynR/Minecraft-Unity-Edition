using UnityEngine;
using System.Collections.Generic;

// Beta's Nether populator reused the provider RNG, so its results depended on the
// chunk request order. Use a documented per-owner seed here for stable streaming.
public static class Beta173NetherPopulation
{
    public const string Randomness = "Deterministic per-chunk; historical Beta Nether population is load-order dependent.";
    public static void Populate(ChunkData owner,int cx,int cz)
    {
        var random=new JavaRandom(owner.worldRef.betaWorldSeed);
        long a=unchecked(random.NextLong()/2*2+1),b=unchecked(random.NextLong()/2*2+1);
        random.SetSeed(unchecked((cx*a+cz*b)^owner.worldRef.betaWorldSeed));
        var world=new View(owner);
        for(int i=0;i<8;i++)
        {
            int x=cx*16+random.NextInt(16)+8,y=random.NextInt(120)+4,z=cz*16+random.NextInt(16)+8;
            if(world.Get(x,y+1,z)!=BlockType.Netherrack)continue;
            var cell=world.Get(x,y,z);if(cell!=BlockType.Air&&cell!=BlockType.Netherrack)continue;
            int stone=0,air=0;
            foreach(var d in LavaNeighbors)
            {var n=world.Get(x+d.x,y+d.y,z+d.z);if(n==BlockType.Netherrack)stone++;if(n==BlockType.Air)air++;}
            if(stone==4&&air==1)world.Set(x,y,z,BlockType.Lava);
        }
        int count=random.NextInt(random.NextInt(10)+1)+1;
        for(int i=0;i<count;i++)
        {
            int x=cx*16+random.NextInt(16)+8,y=random.NextInt(120)+4,z=cz*16+random.NextInt(16)+8;
            for(int n=0;n<64;n++)
            {
                int xx=x+random.NextInt(8)-random.NextInt(8),yy=y+random.NextInt(4)-random.NextInt(4),zz=z+random.NextInt(8)-random.NextInt(8);
                if(world.Get(xx,yy,zz)==BlockType.Air&&world.Get(xx,yy-1,zz)==BlockType.Netherrack)world.Set(xx,yy,zz,BlockType.Fire);
            }
        }
        count=random.NextInt(random.NextInt(10)+1);
        for(int i=0;i<count;i++)
        {int x=cx*16+random.NextInt(16)+8,y=random.NextInt(120)+4,z=cz*16+random.NextInt(16)+8;Glow(world,random,x,y,z);}
        for(int i=0;i<10;i++)
        {int x=cx*16+random.NextInt(16)+8,y=random.NextInt(128),z=cz*16+random.NextInt(16)+8;Glow(world,random,x,y,z);}
        for(int kind=0;kind<2;kind++)
        {
            random.NextInt(1);
            int x=cx*16+random.NextInt(16)+8,y=random.NextInt(128),z=cz*16+random.NextInt(16)+8;
            for(int i=0;i<64;i++)
            {
                int xx=x+random.NextInt(8)-random.NextInt(8),yy=y+random.NextInt(4)-random.NextInt(4),zz=z+random.NextInt(8)-random.NextInt(8);
                // Roofed Nether has no direct skylight. Mushroom support follows solid blocks.
                if(yy>=0&&yy<128&&world.Get(xx,yy,zz)==BlockType.Air&&BetaPopulationFeatures.Opaque(world.Get(xx,yy-1,zz))&&world.DarkEnough(xx,yy,zz))
                    world.Set(xx,yy,zz,kind==0?BlockType.BrownMushroom:BlockType.RedMushroom);
            }
        }
    }
    private static readonly Vector3Int[] LavaNeighbors={Vector3Int.left,Vector3Int.right,Vector3Int.down,new Vector3Int(0,0,-1),new Vector3Int(0,0,1)};
    private static readonly Vector3Int[] Neighbors={Vector3Int.left,Vector3Int.right,Vector3Int.down,Vector3Int.up,new Vector3Int(0,0,-1),new Vector3Int(0,0,1)};
    private static void Glow(View world,JavaRandom random,int x,int y,int z)
    {
        if(world.Get(x,y,z)!=BlockType.Air||world.Get(x,y+1,z)!=BlockType.Netherrack)return;
        world.Set(x,y,z,BlockType.GlowStone);
        for(int i=0;i<1500;i++)
        {
            int xx=x+random.NextInt(8)-random.NextInt(8),yy=y-random.NextInt(12),zz=z+random.NextInt(8)-random.NextInt(8);
            if(world.Get(xx,yy,zz)!=BlockType.Air)continue;
            int adjacent=0;foreach(var d in Neighbors)if(world.Get(xx+d.x,yy+d.y,zz+d.z)==BlockType.GlowStone)adjacent++;
            if(adjacent==1)world.Set(xx,yy,zz,BlockType.GlowStone);
        }
    }
    private sealed class View
    {
        private readonly World world;
        public View(ChunkData owner){world=owner.worldRef;}
        public BlockType Get(int x,int y,int z)=>y<0||y>=128?BlockType.Nothing:world.GetBlock(new Vector3Int(unchecked(-x-1),y,z)).type;
        public bool DarkEnough(int x,int y,int z)
        {
            // For the <13 threshold, only level-15 emitters within two steps,
            // or level-14 emitters within one step can prevent mushroom placement.
            var queue=new Queue<Vector3Int>();var seen=new HashSet<Vector3Int>();
            var start=new Vector3Int(x,y,z);queue.Enqueue(start);seen.Add(start);
            int distance=0;
            while(queue.Count>0&&distance<=2)
            {
                int count=queue.Count;
                for(int i=0;i<count;i++)
                {
                    var p=queue.Dequeue();var t=Get(p.x,p.y,p.z);
                    int emission=t==BlockType.Lava||t==BlockType.Fire||t==BlockType.GlowStone||t==BlockType.JackOLantern?15:t==BlockType.Torch?14:0;
                    if(emission-distance>=13)return false;
                    if(BetaPopulationFeatures.Opacity(t)>1)continue;
                    foreach(var d in Neighbors){var n=p+d;if(seen.Add(n))queue.Enqueue(n);}
                }
                distance++;
            }
            return true;
        }
        public void Set(int x,int y,int z,BlockType block)
        {if(y>=0&&y<128)WorldDataHelper.SetBlock(world,new Vector3Int(unchecked(-x-1),y,z),block);}
    }
}
