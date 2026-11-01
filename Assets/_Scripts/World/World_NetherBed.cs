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
        // BlockBed removes both halves before creating its explosion.
        SetFarmBlock(position,BlockType.Air);
        if(GetBlock(partner).type==BlockType.Bed)SetFarmBlock(partner,BlockType.Air);
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
                if(energy>0)destroyed.Add(p);
                ray+=direction*.3f;energy-=.225f;
            }
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
        foreach(var p in destroyed)
        {
            var type=GetBlock(p).type;if(type==BlockType.Nothing||type==BlockType.Bedrock||type==BlockType.Air)continue;
            if(type==BlockType.TNT){PrimeTnt(p,true);continue;}
            SetFarmBlock(p,BlockType.Air);
        }
        GameAudio.Broadcast("random/explode",center,4,(1+(random.NextFloat()-random.NextFloat())*.2f)*.7f,this);
        EmitExplosionEffects(center,5,new List<Vector3Int>(destroyed).ToArray());
    }
    private static float BedBlastResistance(BlockType type)
    {
        if(type==BlockType.Obsidian)return 1200;
        if(type==BlockType.Water||type==BlockType.Lava)return 100;
        if(type==BlockType.Netherrack)return .4f;
        if(type==BlockType.Glass||type==BlockType.GlowStone)return .3f;
        if(type==BlockType.Dirt||type==BlockType.SoulSand||type==BlockType.Gravel)return .5f;
        BetaBlockCatalog.Entry entry;
        if(!BetaBlockCatalog.TryGet(type,out entry))return 0;
        // Block.setResistance multiplies by three; getExplosionResistance
        // divides by five. Most other blocks inherit their hardness directly.
        switch(entry.SourceId)
        {
            case 1:case 4:case 41:case 42:case 43:case 44:case 45:case 48:case 57:case 67:case 84:return 6;
            case 5:case 14:case 15:case 16:case 21:case 22:case 53:case 56:case 73:case 74:case 85:return 3;
            default:return Math.Max(0,entry.Hardness);
        }
    }
}
