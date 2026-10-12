using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public List<Vector3> vertices   = new List<Vector3>();
    public List<int>     triangles  = new List<int>();
    public List<Vector3> uv         = new List<Vector3>(); // xy = face UV, z = image layer
    public List<Color>   colors     = new List<Color>();   // ← keep only this ONE
    public List<float>   skyLight   = new List<float>();
    public List<float>   blockLight = new List<float>();
    public List<Vector3> sides      = new List<Vector3>();

    public int subMeshCount;
    public ChunkData sourceChunk;

    public MeshData transparentMesh;   // secondary container for transparent faces (if you use it)

    public MeshData(bool isMainMesh)
    {
        if (isMainMesh)
            transparentMesh = new MeshData(false);
    }

    private bool prepared;
    public readonly List<Vector2> lightingUVs = new List<Vector2>();

    // Upload preparation is idempotent: a repeated upload must not duplicate
    // transparent geometry or offset its indices for a second time.
    public void PrepareForRendering()
    {
        if (prepared) return;
        if (transparentMesh != null)
        {
            int offset = vertices.Count;
            for (int i = 0; i < transparentMesh.triangles.Count; i++)
                transparentMesh.triangles[i] += offset;
            vertices.AddRange(transparentMesh.vertices);
            uv.AddRange(transparentMesh.uv);
            colors.AddRange(transparentMesh.colors);
            skyLight.AddRange(transparentMesh.skyLight);
            blockLight.AddRange(transparentMesh.blockLight);
            sides.AddRange(transparentMesh.sides);
        }
        for (int i = 0; i < skyLight.Count; i++)
            lightingUVs.Add(new Vector2(skyLight[i], blockLight[i]));
        prepared = true;
    }

    public void AddFaceUVs(int layer)
    {
        uv.Add(new Vector3(0, 0, layer));
        uv.Add(new Vector3(0, 1, layer));
        uv.Add(new Vector3(1, 1, layer));
        uv.Add(new Vector3(1, 0, layer));
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
