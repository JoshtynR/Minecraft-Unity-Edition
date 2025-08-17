using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices   = new List<Vector3>();
    public List<int>     triangles  = new List<int>();
    public List<Vector2> uv         = new List<Vector2>();
    public List<Color>   colors     = new List<Color>();   // ← keep only this ONE
    public List<float>   skyLight   = new List<float>();
    public List<float>   blockLight = new List<float>();
    public List<Vector3> sides      = new List<Vector3>();

    public int subMeshCount;

    public MeshData transparentMesh;   // secondary container for transparent faces (if you use it)

    public MeshData(bool isMainMesh)
    {
        if (isMainMesh)
            transparentMesh = new MeshData(false);
    }

    public void AddVertex(Vector3 v)
    {
        vertices.Add(v);
    }

    public void AddColor(Color c)
    {
        colors.Add(c);
    }

    // Add one color per vertex in the face (4 verts per quad)
    public void AddQuadColor(Color c)
    {
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
        colors.Add(c);
    }

    public void AddQuadTriangles()
    {
        // assumes the last 4 added vertices belong to this face
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);

        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);
    }
}
