using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public sealed class FixtureWorld : IBetaPopulationWorld
{
    public readonly int env;
    public readonly SortedDictionary<string,int> writes=new SortedDictionary<string,int>(StringComparer.Ordinal);
    public readonly SortedDictionary<string,string> records=new SortedDictionary<string,string>(StringComparer.Ordinal);
    public FixtureWorld(int env){this.env=env;}
    static string Key(int x,int y,int z)=>x+","+y+","+z;
    int Base(int x,int y,int z)
    {
        if(y<0||y>=128)return 0;
        if(env==4){if(x==10&&y==62&&z==10)return 9;return y<70?12:0;}
        if(env==5){if(y==63)return ((z%8+8)%8)<4?9:2;return y<63?1:0;}
        if(env==2&&y==39&&z==0&&x==4)return 54;
        if(env==2&&y==39&&z==0&&x==5)return 52;
        if(env==2&&y>=40&&y<=41&&z==0&&x>=3&&x<=8)return 0;
        if(env==1){if(y==62&&((z%8+8)%8)<4)return 9;if(y==61)return 12;if(y==62)return 2;return y<61?1:0;}
        if(y==63)return env==3?12:2;return y<63?1:0;
    }
    public static int Native(BlockType type)
    {
        BetaBlockCatalog.Entry e;BetaBlockCatalog.TryGet(type,out e);return e.SourceId<0?0:e.SourceId*16+e.Metadata;
    }
    static BlockType Type(int state)
    {
        int id=state/16,meta=state%16;
        foreach(var e in BetaBlockCatalog.Entries)if(e.SourceId==id&&e.Metadata==meta)return e.Type;
        foreach(var e in BetaBlockCatalog.Entries)if(e.SourceId==id)return e.Type;
        throw new Exception("Unknown native state "+state);
    }
    int State(int x,int y,int z){int s;return writes.TryGetValue(Key(x,y,z),out s)?s:Base(x,y,z)*16;}
    public BlockType Get(int x,int y,int z)=>Type(State(x,y,z));
    void State(int x,int y,int z,int value){if(y<0||y>=128)return;var k=Key(x,y,z);if(value==Base(x,y,z)*16)writes.Remove(k);else writes[k]=value;}
    public void Set(int x,int y,int z,BlockType type)=>State(x,y,z,Native(type));
    public bool IsOpaque(BlockType type)=>BetaPopulationFeatures.Opaque(type);
    public int Height(int x,int z){for(int y=127;y>=0;y--)if(BetaPopulationFeatures.Opacity(Get(x,y,z))>0)return y+1;return 0;}
    public bool SeesSky(int x,int y,int z)=>y>=Height(x,z);
    public int Light(int x,int y,int z)=>SeesSky(x,y,z)?15:0;
    public int SkyLight(int x,int y,int z)=>Light(x,y,z);
    public void Metadata(int x,int y,int z,int value)=>State(x,y,z,State(x,y,z)/16*16+value);
    public void Loot(int x,int y,int z,int slot,int item,int count,int damage){records[Key(x,y,z)+",loot,"+slot]=item+","+count+","+damage;}
    public void Spawner(int x,int y,int z,string mob){records[Key(x,y,z)+",spawner,0"]=mob;}
    public void Spring(int x,int y,int z,BlockType liquid,JavaRandom r)=>Set(x,y,z,liquid);
    public string Hash()
    {
        var text=new StringBuilder();foreach(var e in writes)text.Append(e.Key).Append(',').Append(e.Value).Append('\n');foreach(var e in records)text.Append(e.Key).Append(',').Append(e.Value).Append('\n');
        using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant();
    }
}
public static class PopulationCheck
{
    public static void Main(string[] args)
    {
        int checks=0,failures=0;
        foreach(var line in File.ReadLines(args[0]))
        {
            var p=line.Split('\t');var r=new JavaRandom(long.Parse(p[1],CultureInfo.InvariantCulture));var w=new FixtureWorld(int.Parse(p[2]));
            int x=int.Parse(p[3]),y=int.Parse(p[4]),z=int.Parse(p[5]);bool result=true;
            switch(p[0])
            {
                case "ore":result=BetaPopulationFeatures.Vein(w,r,x,y,z,BlockType.DiamondOre,7);break;
                case "clay":result=BetaPopulationFeatures.Vein(w,r,x,y,z,BlockType.Clay,32,true);break;
                case "lake":result=Beta173Population.GenerateLake(w,r,x,y,z,BlockType.Water,false);break;
                case "lava":result=Beta173Population.GenerateLake(w,r,x,y,z,BlockType.Lava,true);break;
                case "dungeon":result=BetaPopulationFeatures.Dungeon(w,r,x,y,z);break;
                case "flower":BetaPopulationFeatures.Plants(w,r,x,y,z,BlockType.Dandelion);break;
                case "mushroom":BetaPopulationFeatures.Plants(w,r,x,y,z,BlockType.BrownMushroom);break;
                case "grass":BetaPopulationFeatures.Plants(w,r,x,y,z,BlockType.TallGrass);break;
                case "fern":BetaPopulationFeatures.Plants(w,r,x,y,z,BlockType.Fern);break;
                case "bush":BetaPopulationFeatures.Plants(w,r,x,y,z,BlockType.DeadBush);break;
                case "reeds":BetaPopulationFeatures.Reeds(w,r,x,y,z);break;
                case "cactus":BetaPopulationFeatures.Cacti(w,r,x,y,z);break;
                case "pumpkin":BetaPopulationFeatures.Pumpkins(w,r,x,y,z);break;
                case "spring":result=BetaPopulationFeatures.Spring(w,r,x,y,z,BlockType.Water);break;
                default:
                    int biome=int.Parse(p[0].Substring(8));
                    // Java biome table indices above are explicit reference identities.
                    var type=(BetaBiomeType)biome;
                    var temperatures=new double[256];for(int i=0;i<256;i++)temperatures[i]=w.env==3?.2:.8;
                    long last=0;new Beta173Population(long.Parse(p[1])).PopulateComplete(w,x,z,type,0,temperatures,(stage,state)=>last=state);
                    // The observer exposes the last RNG state without advancing it.
                    r.SetSeed(last^0x5DEECE66DL);break;
            }
            long next=r.NextLong();checks+=4;
            if(result.ToString().ToLowerInvariant()!=p[6]||next.ToString(CultureInfo.InvariantCulture)!=p[7]||w.writes.Count.ToString()!=p[8]||w.Hash()!=p[9])
            {
                failures++;if(failures<15)Console.WriteLine("FAIL "+string.Join(" ",p,0,6)+" result="+result+" next="+next+"/"+p[7]+" writes="+w.writes.Count+"/"+p[8]+" hash="+w.Hash()+"/"+p[9]);
            }
        }
        Console.WriteLine(checks+" population feature/output/loot/RNG checks, "+failures+" failures (Java world services mocked).");
        if(failures>0)Environment.Exit(1);
    }
}
