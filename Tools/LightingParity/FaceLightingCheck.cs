using System;
using UnityEngine;
public static partial class LightingCheck
{
    static void CheckFaceLighting()
    {
        foreach(Direction direction in Enum.GetValues(typeof(Direction)))
        {
            var chunk=new ChunkData();
            var p=new Vector3Int(0,64,0);var normal=direction.GetVector();
            ChunkSection.ChooseFaceAxes(normal,out var axisA,out var axisB);
            for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)for(int z=-2;z<=2;z++)
            {var air=Add(chunk,p+new Vector3Int(x,y,z));air.sky=15;air.light=2;}
            var block=chunk.GetBlock(p);block.type=BlockType.Stone;block.BlockData.opacity=15;
            foreach(var side in new[]{p+normal+axisA*-1,p+normal+axisB*-1})
            {var wall=chunk.GetBlock(side);wall.type=BlockType.Stone;wall.BlockData.opacity=15;wall.sky=wall.light=0;}
            var mesh=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,mesh,block,block.BlockData,chunk);
            bool shadow=false,open=false;
            for(int i=0;i<4;i++)
            {
                Check(mesh.skyLight[i]==15&&mesh.blockLight[i]==2,"AO retains independent raw sky/block levels");
                Check(mesh.sides[i].z==4,"mesh marks separate AO/light streams");
                if(Math.Abs(mesh.sides[i].x-.4f)<.00001f)shadow=true;
                if(Math.Abs(mesh.sides[i].x-1)<.00001f)open=true;
                Check(mesh.smoothLightCorners[i].x==15&&mesh.smoothLightCorners[i].y==2,"AO-off retains face light levels");
            }
            Check(shadow&&open,"each face has a .4 occluded corner and an unoccluded corner");
            chunk.worldRef.skyLightReduction=11;
            var night=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,night,block,block.BlockData,chunk);
            for(int i=0;i<4;i++)Check(night.skyLight[i]==mesh.skyLight[i]&&night.sides[i].x==mesh.sides[i].x,"daylight never bakes into chunk meshes");
            block.BlockData.lightEmission=15;
            var glow=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,glow,block,block.BlockData,chunk);
            for(int i=0;i<4;i++)Check(glow.blockLight[i]==15&&glow.sides[i].x==1,"emitting faces remain bright without corner AO");
            block.BlockData.lightEmission=0;
            block.type=BlockType.Water;
            var fluid=new MeshData(false);
            fluid.vertices.AddRange(mesh.vertices);
            BlockHelper.GetFaceLightingAndAO(direction,p,fluid,chunk);
            var front=chunk.GetBlock(direction==Direction.up?p:p+normal);
            for(int i=0;i<4;i++)
            {
                Check(fluid.skyLight[i]==front.sky&&fluid.blockLight[i]==front.light,"liquids retain flat face light beside occluders");
                Check(fluid.sides[i].x==1,"liquid lighting never includes corner AO");
            }
        }
    }
    static void CheckLeafAO()
    {
        var species=new[]{BlockType.Leaves,BlockType.SpruceLeaves,BlockType.BirchLeaves};
        for(int kind=0;kind<species.Length;kind++)foreach(Direction direction in Enum.GetValues(typeof(Direction)))
        {
            var chunk=new ChunkData();var p=new Vector3Int(0,64,0);var normal=direction.GetVector();
            ChunkSection.ChooseFaceAxes(normal,out var a,out var b);
            for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)for(int z=-2;z<=2;z++)
            {var air=Add(chunk,p+new Vector3Int(x,y,z));air.sky=15;air.light=4;}
            var leaf=Add(chunk,p,species[kind],1);leaf.sky=15;leaf.light=4;
            var open=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,open,leaf,leaf.BlockData,chunk);
            for(int v=0;v<4;v++)Check(open.sides[v].x==1,"isolated "+species[kind]+" has no blanket AO darkening");
            var first=chunk.GetBlock(p+normal+a*-1);first.type=species[(kind+1)%3];first.BlockData.opacity=1;
            var second=chunk.GetBlock(p+normal+b*-1);second.type=species[(kind+2)%3];second.BlockData.opacity=1;
            var diagonal=chunk.GetBlock(p+normal+a*-1+b*-1);diagonal.sky=7;diagonal.light=1;
            var canopy=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,canopy,leaf,leaf.BlockData,chunk);
            bool shaded=false,unshaded=false;
            for(int v=0;v<4;v++)
            {
                if(Math.Abs(canopy.sides[v].x-.8f)<.00001f)
                {
                    shaded=true;Check(Math.Abs(canopy.skyLight[v]-13)<.00001f&&Math.Abs(canopy.blockLight[v]-3.25f)<.00001f,"soft foliage keeps the diagonal sky/block sample");
                }
                if(canopy.sides[v].x==1)unshaded=true;
                Check(canopy.sides[v].z==4&&canopy.smoothLightCorners[v].x==15&&canopy.smoothLightCorners[v].y==4,"leaf AO reaches the world shader while retaining AO-off light");
            }
            Check(shaded&&unshaded,"mixed-species canopy creates corner AO on every face");
            first.type=BlockType.Stone;first.BlockData.opacity=15;second.type=BlockType.Stone;second.BlockData.opacity=15;
            var trunk=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,trunk,leaf,leaf.BlockData,chunk);
            bool hard=false;foreach(var side in trunk.sides)if(Math.Abs(side.x-.4f)<.00001f)hard=true;
            Check(hard,"solid blocks still occlude leaves more strongly than foliage");
            first.type=BlockType.Glass;first.BlockData.opacity=0;second.type=BlockType.Glass;second.BlockData.opacity=0;
            var glass=new MeshData(false);BlockHelper.GetFaceDataIn(direction,p,glass,leaf,leaf.BlockData,chunk);
            foreach(var side in glass.sides)Check(side.x==1,"glass does not acquire leaf-specific occlusion");
            Check(first.BlockData.opacity==0&&leaf.BlockData.opacity==1,"leaf AO does not change light transport opacity");
        }
        // Exercise normal cube routing, not just the face-light helper: foliage
        // stays in the cutout submesh carrying per-corner AO values.
        var routed=new ChunkData();var at=new Vector3Int(0,64,0);
        for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)for(int z=-2;z<=2;z++)Add(routed,at+new Vector3Int(x,y,z)).sky=15;
        Add(routed,at,BlockType.Leaves,1);Add(routed,at+new Vector3Int(-1,1,0),BlockType.SpruceLeaves,1).sky=15;
        var mesh=BlockHelper.GetMeshData(routed,at,new MeshData(true){greedyMeshing=true},BlockType.Leaves);
        bool receives=false;foreach(var side in mesh.transparentMesh.sides)if(side.x<1)receives=true;
        Check(receives&&mesh.vertices.Count==0&&mesh.transparentMesh.vertices.Count>0,"production foliage cutout geometry carries canopy AO with greedy meshing enabled");
    }

}
