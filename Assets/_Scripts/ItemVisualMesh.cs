using System.Collections.Generic;
using UnityEngine;

// Shared inventory/drop mesh builder. Each owner caches its meshes and disposes
// them on teardown; source textures and materials remain shared.
public static class ItemVisualMesh
{
    public static Mesh Build(BlockType type)
    {
        var data=new MeshData(true);
        var block=BlockMapping.MapTypeToBlock(type,new Block(type,Vector3Int.zero,null));
        foreach(var direction in BlockHelper.directions)
        {
            int first=data.vertices.Count;
            BlockHelper.GetFaceDataIn(direction,Vector3Int.zero,data,block,BlockDataManager.blockTypeDataDictionary[(int)type],null);
            float light=direction==Direction.backwards?13:direction==Direction.right?10:14;
            bool plant=BlockShapesExtensions.IsPlant(type)||SpecialBlockGeometry.FlatShading(type);
            for(int i=first;i<data.vertices.Count;i++)
            {data.skyLight.Add(light);data.sides.Add(plant?new Vector3(1,1,3):Vector3.zero);}
        }
        var mesh=new Mesh{name="Item "+type};
        mesh.SetVertices(data.vertices);mesh.SetTriangles(data.triangles,0);mesh.SetUVs(0,data.uv);mesh.SetColors(data.colors);
        var lightUV=new Vector2[data.skyLight.Count];for(int i=0;i<lightUV.Length;i++)lightUV[i]=new Vector2(data.skyLight[i],0);
        mesh.SetUVs(1,lightUV);mesh.SetUVs(2,data.sides);mesh.SetUVs(4,data.faceCoordinates);
        mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static Mesh Billboard()
    {
        var mesh=new Mesh{name="Dropped item billboard"};
        mesh.SetVertices(new List<Vector3>{new Vector3(-.5f,0,0),new Vector3(-.5f,1,0),new Vector3(.5f,1,0),new Vector3(.5f,0,0)});
        mesh.SetUVs(0,new List<Vector2>{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)});
        mesh.SetTriangles(new List<int>{0,1,2,0,2,3},0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
