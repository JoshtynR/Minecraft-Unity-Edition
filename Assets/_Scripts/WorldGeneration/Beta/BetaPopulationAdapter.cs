using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Beta173Population
{
    private sealed partial class TreeWorld
    {
        private readonly Dictionary<Vector3Int,int[]> skyColumns = new Dictionary<Vector3Int,int[]>();
        private static Vector3Int UnityPosition(int x,int y,int z) => new Vector3Int(unchecked(-x-1),y,z);
        private void Record(int x,int y,int z,string kind,int slot=0,int item=0,int count=0,int damage=0,int metadata=0,string mob=null)
        {
            var pos=UnityPosition(x,y,z);
            var block=owner.worldRef.GetBlock(pos);
            var records=block.chunkData.betaPopulationRecords;
            // Replace complete records rather than mutating objects that a
            // background save may already be serializing.
            var record=new BetaPopulationRecord{position=pos,kind=kind,slot=slot,item=item,count=count,damage=damage,metadata=metadata,mob=mob};
            lock(records)
            {
                int index=records.FindIndex(e=>e.position.Equals(pos)&&e.kind==kind&&e.slot==slot);
                if(index<0)records.Add(record);else records[index]=record;
            }
            block.chunkData.modifiedAfterSave=true;
        }
        public void Metadata(int x,int y,int z,int value) { Record(x,y,z,"metadata",metadata:value); }
        public void Loot(int x,int y,int z,int slot,int item,int count,int damage)
        { Record(x,y,z,"loot",slot,item,count,damage); }
        public void Spawner(int x,int y,int z,string mob) { Record(x,y,z,"spawner",mob:mob); }
        public bool SeesSky(int x,int y,int z) => y>=Height(x,z);
        private int DirectSky(int x,int y,int z)
        {
            var column=new Vector3Int(x,0,z);int[] values;
            if(!skyColumns.TryGetValue(column,out values))
            {
                values=new int[128];int light=15;
                for(int yy=127;yy>=0;yy--)
                {
                    light=Math.Max(0,light-BetaPopulationFeatures.Opacity(Get(x,yy,z)));
                    values[yy]=light;
                    if(light==0)break;
                }
                skyColumns[column]=values;
            }
            return y>=128?15:y<0?0:values[y];
        }

        private static int Emission(BlockType b)
        {
            if(b==BlockType.Lava||b==BlockType.GlowStone||b==BlockType.Fire||b==BlockType.JackOLantern)return 15;
            if(b==BlockType.Torch)return 14;
            if(b==BlockType.BrownMushroom)return 1;
            return 0;
        }
        public int SkyLight(int x,int y,int z) => SampleLight(x,y,z,false);
        public int Light(int x,int y,int z) => SampleLight(x,y,z,true);
        private int SampleLight(int x,int y,int z,bool emissions)
        {
            if(y>=128)return 15;if(y<0)return 0;
            int direct=DirectSky(x,y,z);
            if(direct==15)return 15;
            // Reverse light propagation: find the brightest source reachable
            // through the same opacity costs as Beta, within light's 15-cell range.
            var queue=new Queue<Vector3Int>();var costs=new Dictionary<Vector3Int,int>();
            var start=new Vector3Int(x,y,z);queue.Enqueue(start);costs[start]=0;int best=direct;
            int[] dx={-1,1,0,0,0,0},dy={0,0,-1,1,0,0},dz={0,0,0,0,-1,1};
            while(queue.Count>0)
            {
                var p=queue.Dequeue();int cost=costs[p];
                int source=DirectSky(p.x,p.y,p.z);
                if(emissions)source=Math.Max(source,Emission(Get(p.x,p.y,p.z)));
                best=Math.Max(best,source-cost);if(best==15)return 15;
                int nextCost=cost+Math.Max(1,BetaPopulationFeatures.Opacity(Get(p.x,p.y,p.z)));
                if(nextCost>=15-best)continue;
                for(int i=0;i<6;i++)
                {
                    var next=new Vector3Int(p.x+dx[i],p.y+dy[i],p.z+dz[i]);
                    if(next.y<0||next.y>=128)continue;
                    int old;if(costs.TryGetValue(next,out old)&&old<=nextCost)continue;
                    costs[next]=nextCost;queue.Enqueue(next);
                }
            }
            return best;
        }
        public void Spring(int x,int y,int z,BlockType liquid,JavaRandom random)
        {
            Set(x,y,z,liquid);
            // The source placement belongs to population; subsequent scheduled
            // water ticks are handled by the existing authoritative simulation.
            // Both water and lava have runtime flow/reactions. Beta's immediate
            // generation-time fluid updates are still separate from this adapter.
        }
    }
}
