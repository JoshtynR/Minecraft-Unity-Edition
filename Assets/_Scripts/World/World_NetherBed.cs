using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public partial class World
{
    public void ExplodeNetherBed(Vector3Int position)
    {
        if(!DropAuthority||dimension!=WorldDimension.Nether)return;
        var block=GetBlock(position);if(block.type!=BlockType.Bed)return;
        byte state=SpecialBlockGeometry.State(block.type,block.blockShape);
        var partner=position+SpecialBlockGeometry.PartnerOffset(block.type,state);
        var center=(Vector3)position+Vector3.one*.5f;
        // Classic strength-five ray blast, with material resistance and permanent bedrock.
        var destroyed=new HashSet<Vector3Int>{position,partner};
        var random=new JavaRandom(unchecked(betaWorldSeed^position.GetHashCode()^(long)(Time.time*20)));
        for(int x=0;x<16;x++)for(int y=0;y<16;y++)for(int z=0;z<16;z++)
        {
            if(x!=0&&x!=15&&y!=0&&y!=15&&z!=0&&z!=15)continue;
            var direction=new Vector3(x/15f*2-1,y/15f*2-1,z/15f*2-1).normalized;
            float energy=5*(.7f+random.NextFloat()*.6f);var ray=center;
            while(energy>0)
            {
                var p=Vector3Int.FloorToInt(ray);var type=GetBlock(p).type;
                if(type==BlockType.Nothing||type==BlockType.Bedrock)break;
                if(type!=BlockType.Air)energy-=(BedBlastResistance(type)+.3f)*.3f;
                if(energy>0&&type!=BlockType.Air)destroyed.Add(p);
                ray+=direction*.3f;energy-=.225f;
            }
        }
        foreach(var p in destroyed)
        {
            var type=GetBlock(p).type;if(type==BlockType.Nothing||type==BlockType.Bedrock)continue;
            SetFarmBlock(p,BlockType.Air);
        }
        if(GameManager.Instance!=null)
            foreach(var player in GameManager.Instance.players)
            {
                if(player==null||player.dimension!=dimension||player.health<=0)continue;
                float distance=(player.transform.position-center).magnitude/10;if(distance>1)continue;
                float exposure=0;
                for(int i=0;i<8;i++)
                {
                    var point=player.transform.position+new Vector3((i&1)==0?-.3f:.3f,(i&2)==0?.1f:1.7f,(i&4)==0?-.3f:.3f);
                    bool clear=true;int steps=Mathf.Max(1,Mathf.CeilToInt((point-center).magnitude/.3f));
                    for(int j=1;j<steps;j++)if(SpecialBlockGeometry.NormalCube(GetBlock(Vector3.Lerp(center,point,(float)j/steps)).type)){clear=false;break;}
                    if(clear)exposure+=.125f;
                }
                float impact=(1-distance)*exposure;
                player.ApplyArmouredDamage((int)((impact*impact+impact)/2*8*10+1));
            }
        GameAudio.Broadcast("random/explode",center,1,1,this);
    }
    private static float BedBlastResistance(BlockType type)
    {
        if(type==BlockType.Obsidian)return 1200;
        if(type==BlockType.Water||type==BlockType.Lava)return 100;
        if(type==BlockType.Netherrack)return .4f;
        if(type==BlockType.Glass||type==BlockType.GlowStone)return .3f;
        if(type==BlockType.Dirt||type==BlockType.SoulSand||type==BlockType.Gravel)return .5f;
        if(type==BlockType.Bed||BlockShapesExtensions.IsPlant(type)||SpecialBlockGeometry.NoCollision(type))return 0;
        BetaBlockCatalog.Entry entry;
        return BetaBlockCatalog.TryGet(type,out entry)?Math.Max(0,entry.Hardness)*3:3;
    }
}
