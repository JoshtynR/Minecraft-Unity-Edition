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
        }
    }
}
