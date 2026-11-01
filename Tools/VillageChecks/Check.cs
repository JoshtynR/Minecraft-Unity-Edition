using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

// Storage doubles are shared with the production terrain-boundary suite.
public class ChunkSection {}
public class ChunkSectionSaveData { public ChunkSectionSaveData(ChunkSection section){} }
public partial class ChunkData {public ChunkSection[] sections=new ChunkSection[0];}
public static class VillageCheck
{
    private static int checks;
    private static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    private sealed class Positions:IEqualityComparer<Vector3Int>
    {
        public bool Equals(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;
        public int GetHashCode(Vector3Int p)=>unchecked((p.x*397^p.y)*397^p.z);
    }
    private sealed class Grade : IBetaVillageTerrain
    {
        public int center;
        public BetaVillageGround Ground(int x,int z)=>new BetaVillageGround{height=64+Math.Max(-6,Math.Min(6,Beta18Villages.FloorDiv(x-center,8))),top=BlockType.Grass,biome=BetaBiomeType.Plains};
    }
    private struct Cell {public BlockType type;public byte state;}
    private sealed class Flat : IBetaVillageTerrain
    {
        public BlockType top=BlockType.Grass;public BetaBiomeType biome=BetaBiomeType.Plains;public bool steep;
        public BetaVillageGround Ground(int x,int z)=>new BetaVillageGround{height=steep?64+(int)(Math.Abs((long)x*5+z*7)%24):64,top=top,biome=biome};
    }
    private static Dictionary<Vector3Int,Cell> Stamp(Beta18Villages villages,int x,int z,bool reverse,HashSet<BetaVillagePiece> pieces=null)
    {
        var result=new Dictionary<Vector3Int,Cell>(new Positions());int cx=Beta18Villages.FloorDiv(x,16),cz=Beta18Villages.FloorDiv(z,16);
        for(int a=0;a<9;a++)for(int b=0;b<9;b++)
        {
            int xx=cx+(reverse?4-a:a-4),zz=cz+(reverse?4-b:b-4);
            foreach(var piece in villages.ForChunk(xx,zz))
            {
                pieces?.Add(piece);
                piece.Emit(xx,zz,(u,y,v,type,state)=>
                {
                    Check(u>=0&&u<16&&v>=0&&v<16&&y>0&&y<128,"structure writes stay inside their owning chunk and height bounds");
                    result[new Vector3Int(xx*16+u,y,zz*16+v)]=new Cell{type=type,state=state};
                });
            }
        }
        return result;
    }
    private static void Equal(Dictionary<Vector3Int,Cell> a,Dictionary<Vector3Int,Cell> b)
    {
        Check(a.Count==b.Count,"chunk visitation order keeps structure footprint");
        foreach(var row in a)Check(b.TryGetValue(row.Key,out var cell)&&row.Value.type==cell.type&&row.Value.state==cell.state,"chunk visitation order keeps every structure block and metadata");
    }
    private static void Layouts()
    {
        Check(Beta18Villages.FloorDiv(-1,32)==-1&&Beta18Villages.FloorDiv(-32,32)==-1&&Beta18Villages.FloorDiv(-33,32)==-2,"negative region boundaries use floor division");
        var kinds=new HashSet<BetaVillageKind>();var rotations=new HashSet<int>();
        for(int trial=0;trial<5;trial++)
        {
            long seed=trial==4?long.MinValue:trial*341L;int rx=trial%2==0?-1:0,rz=trial%2==0?0:-1;
            var villages=new Beta18Villages(seed,new Flat());villages.Candidate(rx,rz,out int x,out int z);
            Check(Beta18Villages.FloorDiv(x/16,32)==rx&&Beta18Villages.FloorDiv(z/16,32)==rz,"candidate remains in signed region");
            var pieces=new HashSet<BetaVillagePiece>();var forward=Stamp(villages,x,z,false,pieces);
            Equal(forward,Stamp(new Beta18Villages(seed,new Flat()),x,z,true));
            Check(pieces.Count>=8,"flat terrain produces connected, populated layout");
            foreach(var p in pieces)
            {
                kinds.Add(p.kind);rotations.Add(p.rotation);
                p.Bounds(out int a,out int b,out int c,out int d);Check(a>=x-Beta18Villages.Radius&&c<=x+Beta18Villages.Radius&&b>=z-Beta18Villages.Radius&&d<=z+Beta18Villages.Radius,"piece envelope fits candidate discovery radius");
                if(p.kind==BetaVillageKind.House||p.kind==BetaVillageKind.Hut||p.kind==BetaVillageKind.LargeHouse||p.kind==BetaVillageKind.Library||p.kind==BetaVillageKind.Church||p.kind==BetaVillageKind.Blacksmith)
                {
                    p.Position(p.width/2,0,out int doorX,out int doorZ);
                    var low=forward[new Vector3Int(doorX,p.floor+1,doorZ)];var high=forward[new Vector3Int(doorX,p.floor+2,doorZ)];
                    Check(low.type==BlockType.WoodenDoor&&high.type==BlockType.WoodenDoor&&low.state==((p.rotation+1)&3)&&high.state==(low.state|8),"every entrance retains paired reflected door states");
                    p.Position(p.width/2,1,out int torchX,out int torchZ);
                    var torch=forward[new Vector3Int(torchX,p.floor+3,torchZ)];
                    Check(torch.type==BlockType.Torch&&torch.state==new byte[]{3,1,4,2}[p.rotation],"interior torches attach to their front wall in every rotation");
                }
            }
            foreach(var row in forward)if(row.Value.type==BlockType.WheatCrop0)
            {
                var below=forward[new Vector3Int(row.Key.x,row.Key.y-1,row.Key.z)];
                Check(below.type==BlockType.MoistFarmland&&row.Value.state>=2&&row.Value.state<=7,"crops have hydrated soil and valid growth metadata");
            }
        }
        foreach(BetaVillageKind kind in Enum.GetValues(typeof(BetaVillageKind)))Check(kinds.Contains(kind),"seeded layout coverage includes "+kind);
        Check(rotations.Count==4,"layouts cover all four building orientations");
        var slope=new Grade();var graded=new Beta18Villages(17,slope);graded.Candidate(0,0,out int gx,out int gz);slope.center=gx;
        var gradedPieces=new HashSet<BetaVillagePiece>();Stamp(graded,gx,gz,false,gradedPieces);var floors=new HashSet<int>();
        foreach(var piece in gradedPieces)if(piece.kind!=BetaVillageKind.Road)floors.Add(piece.floor);
        Check(floors.Count>1,"gentle Beta hills retain stepped building elevations rather than flattening the village");
        var bounded=new Beta18Villages(17,new Flat());bounded.Candidate(0,0,out int bx,out int bz);var before=Stamp(bounded,bx,bz,false);
        for(int region=1;region<70;region++){bounded.Candidate(region,0,out int wx,out int wz);bounded.ForChunk(Beta18Villages.FloorDiv(wx,16),Beta18Villages.FloorDiv(wz,16));}
        Equal(before,Stamp(bounded,bx,bz,false));
        foreach(var terrain in new[]{new Flat{top=BlockType.Water},new Flat{top=BlockType.Lava},new Flat{biome=BetaBiomeType.Taiga},new Flat{steep=true}})
        {
            var v=new Beta18Villages(0,terrain);v.Candidate(0,0,out int x,out int z);
            Check(v.ForChunk(Beta18Villages.FloorDiv(x,16),Beta18Villages.FloorDiv(z,16)).Length==0,"ocean/lava/closed-biome/extreme-slope sites rejected");
        }
    }
    private static bool Locate(long seed,int radius,out int x,out int z)
    {
        var t=new Beta173Terrain(seed);var v=new Beta18Villages(seed,new BetaVillageTerrain(t,new Beta173Caves(seed)));
        for(int rx=-radius;rx<=radius;rx++)for(int rz=-radius;rz<=radius;rz++)
        {
            v.Candidate(rx,rz,out int wx,out int wz);var pieces=v.ForChunk(Beta18Villages.FloorDiv(wx,16),Beta18Villages.FloorDiv(wz,16));
            foreach(var piece in pieces)if(piece.kind==BetaVillageKind.Well)
            {x=wx;z=wz;Console.WriteLine("Village seed="+seed+" Unity X="+unchecked(-x-1)+" Z="+z+" (Beta X="+x+")");return true;}
        }
        x=z=0;return false;
    }
    private static ChunkData Generate(TerrainGenerator generator,long seed,int cx,int cz,bool villages)
    {
        var world=new World{generateVillages=villages,betaWorldSeed=seed};
        var data=new ChunkData{worldRef=world,worldPos=new Vector3Int(unchecked((-cx-1)*16),0,cz*16)};
        world.chunks[data.worldPos]=data;generator.GenerateChunkData(data);return data;
    }
    private static void Production()
    {
        var climateTerrain=new Beta173Terrain(17);var groundSampler=new BetaVillageTerrain(climateTerrain,new Beta173Caves(17));
        foreach(var pos in new[]{new Vector3Int(3,0,13),new Vector3Int(-1,0,21),new Vector3Int(47,0,-36)})
            Check(groundSampler.Ground(pos.x,pos.z).biome==climateTerrain.GetBiome(pos.x,pos.z),"ground sample uses X-major climate indexing at asymmetric and negative coordinates");
        Check(Locate(0,2,out int x,out int z),"real Beta terrain has at least one viable village in search fixture");
        int cx=Beta18Villages.FloorDiv(x,16),cz=Beta18Villages.FloorDiv(z,16);var generator=new TerrainGenerator();
        var generated=Generate(generator,0,cx,cz,true);var baseline=Generate(new TerrainGenerator(),0,cx,cz,false);int changed=0;
        Check(generated.betaVillageProtection!=null,"production adapter stores village ownership mask");
        for(int u=0;u<16;u++)for(int v=0;v<16;v++)for(int y=0;y<128;y++)
        {
            if(generated.blocks[u,y,v]!=baseline.blocks[u,y,v])changed++;
            if(!generated.VillageProtected(generated.worldPos.x+u,y,generated.worldPos.z+v))
                Check(generated.blocks[u,y,v]==baseline.blocks[u,y,v],"village planning never changes underlying terrain outside structure footprint (scratch safety)");
        }
        Check(changed>50,"production village actually writes buildings/roads into target terrain");
        var expected=new ChunkData[4];for(int i=0;i<4;i++)expected[i]=Generate(generator,0,cx+i%2,cz+i/2,true);
        var cold=new TerrainGenerator();Parallel.For(0,4,i=>
        {
            var actual=Generate(cold,0,cx+i%2,cz+i/2,true);
            for(int u=0;u<16;u++)for(int v=0;v<16;v++)for(int y=0;y<128;y++)
                if(actual.blocks[u,y,v]!=expected[i].blocks[u,y,v]||actual.states[u,y,v]!=expected[i].states[u,y,v])throw new Exception("parallel/cold village generation changed blocks or metadata");
        });Check(true,"cold parallel generation agrees with sequential chunk generation");
        var boundary=new ChunkData{worldPos=new Vector3Int(-16,0,-16),worldRef=new World()};
        boundary.ApplyVillageBlock(0,65,15,BlockType.WoodenDoor,9);
        Check(boundary.blocks[15,65,15]==BlockType.WoodenDoor&&boundary.states[15,65,15]==9&&boundary.VillageProtected(-1,65,-1),"canonical chunk edge reflects once and preserves upper-door metadata");
        boundary.ApplyVillageBlock(15,66,0,BlockType.WheatCrop0,7);
        Check(boundary.blocks[0,66,0]==BlockType.WheatCrop0&&boundary.states[0,66,0]==7,"crop age reaches packed generation storage");
        var snapshot=ChunkSaveData.Serialize(generated);var restored=new ChunkData{worldPos=generated.worldPos};restored.RestoreVillageProtection(snapshot.betaVillageProtection);
        Check(snapshot.betaVillageProtection.Length==4096&&!ReferenceEquals(snapshot.betaVillageProtection,generated.betaVillageProtection),"save snapshot owns its protection buffer");
        snapshot.betaVillageProtection[0]^=255;Check(restored.betaVillageProtection[0]!=snapshot.betaVillageProtection[0],"reload does not share mutable snapshot ownership");
        restored.RestoreVillageProtection(null);Check(!restored.VillageProtected(restored.worldPos.x,64,restored.worldPos.z),"legacy chunk without village mask remains unprotected");
        restored.RestoreVillageProtection(new byte[7]);Check(restored.betaVillageProtection==null,"malformed ownership mask falls back safely");
        var nether=new ChunkData{worldRef=new World{generateVillages=true,dimension=WorldDimension.Nether}};generator.GenerateChunkData(nether);
        Check(nether.betaVillageProtection==null,"enabled villages never run in the Nether");
        var treeType=typeof(Beta173Population).GetNestedType("TreeWorld",BindingFlags.NonPublic);
        var writer=(IBetaTreeWorld)Activator.CreateInstance(treeType,generated);
        bool tested=false;var protectedAir=new Vector3Int();
        for(int u=0;u<16&&!tested;u++)for(int v=0;v<16&&!tested;v++)for(int y=64;y<110&&!tested;y++)
        {
            int wx=generated.worldPos.x+u,wz=generated.worldPos.z+v;
            if(!generated.VillageProtected(wx,y,wz)||generated.blocks[u,y,v]!=BlockType.Air)continue;
            writer.Set(unchecked(-wx-1),y,wz,BlockType.Log);Check(generated.blocks[u,y,v]==BlockType.Air,"population cannot place a tree inside protected village air");protectedAir=new Vector3Int(unchecked(-wx-1),y,wz);tested=true;
        }
        Check(tested,"interior protection exercised through actual population adapter");
        var records=(IBetaPopulationWorld)writer;int recordCount=generated.betaPopulationRecords.Count;
        records.Metadata(protectedAir.x,protectedAir.y,protectedAir.z,7);records.Loot(protectedAir.x,protectedAir.y,protectedAir.z,0,1,1,0);records.Spawner(protectedAir.x,protectedAir.y,protectedAir.z,"Zombie");
        Check(generated.betaPopulationRecords.Count==recordCount,"blocked population placement cannot leave phantom tile metadata, loot or spawners");
        writer.Set(unchecked(-generated.worldPos.x-1),20,generated.worldPos.z,BlockType.Dirt);
        Check(generated.blocks[0,20,0]==BlockType.Dirt,"population continues outside protected village voxels");
    }
    public static int Main(string[] args)
    {
        if(args.Length>0&&args[0]=="--locate")return Locate(long.Parse(args[1]),int.Parse(args[2]),out _,out _)?0:1;
        Layouts();Production();Console.WriteLine(checks+" village layout/clipping/metadata/terrain/parallel/protection/save checks passed (Unity storage mocked).");return 0;
    }
}
