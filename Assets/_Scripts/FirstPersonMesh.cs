using System.Collections.Generic;
using UnityEngine;

// Pixel-edge extrusion samples the sprite's alpha in the shader: no readable texture
// imports or GPU readback are required, including for resource-pack item textures.
public static class FirstPersonMesh
{
    private sealed class Builder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            uv.AddRange(new[] { ua, ub, uc, ud });
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }

    public static Mesh Sprite(int width, int height)
    {
        width = Mathf.Clamp(width, 1, 256); height = Mathf.Clamp(height, 1, 256);
        var b = new Builder(); const float d = 1f / 16;
        b.Quad(new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(1,0,0),
            new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0));
        b.Quad(new Vector3(1,0,d), new Vector3(1,1,d), new Vector3(0,1,d), new Vector3(0,0,d),
            new Vector2(1,0), new Vector2(1,1), new Vector2(0,1), new Vector2(0,0));
        for (int i = 0; i < width; i++)
        {
            float left = (float)i / width, right = (float)(i + 1) / width, u = (i + .5f) / width;
            b.Quad(new Vector3(left,0,d), new Vector3(left,1,d), new Vector3(left,1,0), new Vector3(left,0,0),
                new Vector2(u,0), new Vector2(u,1), new Vector2(u,1), new Vector2(u,0));
            b.Quad(new Vector3(right,0,0), new Vector3(right,1,0), new Vector3(right,1,d), new Vector3(right,0,d),
                new Vector2(u,0), new Vector2(u,1), new Vector2(u,1), new Vector2(u,0));
        }
        for (int i = 0; i < height; i++)
        {
            float bottom = (float)i / height, top = (float)(i + 1) / height, v = (i + .5f) / height;
            b.Quad(new Vector3(0,top,0), new Vector3(0,top,d), new Vector3(1,top,d), new Vector3(1,top,0),
                new Vector2(0,v), new Vector2(0,v), new Vector2(1,v), new Vector2(1,v));
            b.Quad(new Vector3(0,bottom,d), new Vector3(0,bottom,0), new Vector3(1,bottom,0), new Vector3(1,bottom,d),
                new Vector2(0,v), new Vector2(0,v), new Vector2(1,v), new Vector2(1,v));
        }
        return b.Finish("First person extruded item");
    }

    public static Mesh Arm(int textureWidth, int textureHeight)
    {
        var b = new Builder();
        float h = 64f * Mathf.Max(1, textureHeight) / Mathf.Max(1, textureWidth);
        float x = .125f, y = -.75f, z = .125f;
        ArmFace(b, new Vector3(-x,y,-z), new Vector3(-x,0,-z), new Vector3(x,0,-z), new Vector3(x,y,-z), 44,20,4,12,h);
        ArmFace(b, new Vector3(x,y,z), new Vector3(x,0,z), new Vector3(-x,0,z), new Vector3(-x,y,z), 52,20,4,12,h);
        ArmFace(b, new Vector3(-x,y,z), new Vector3(-x,0,z), new Vector3(-x,0,-z), new Vector3(-x,y,-z), 40,20,4,12,h);
        ArmFace(b, new Vector3(x,y,-z), new Vector3(x,0,-z), new Vector3(x,0,z), new Vector3(x,y,z), 48,20,4,12,h);
        ArmFace(b, new Vector3(-x,0,z), new Vector3(x,0,z), new Vector3(x,0,-z), new Vector3(-x,0,-z), 44,16,4,4,h);
        ArmFace(b, new Vector3(-x,y,-z), new Vector3(x,y,-z), new Vector3(x,y,z), new Vector3(-x,y,z), 48,16,4,4,h);
        return b.Finish("First person right arm");
    }
    private static void ArmFace(Builder b, Vector3 a, Vector3 c, Vector3 d, Vector3 e, float u, float v, float w, float h, float skinHeight)
    {
        b.Quad(a,c,d,e,new Vector2(u/64,1-(v+h)/skinHeight),new Vector2(u/64,1-v/skinHeight),
            new Vector2((u+w)/64,1-v/skinHeight),new Vector2((u+w)/64,1-(v+h)/skinHeight));
    }
}
