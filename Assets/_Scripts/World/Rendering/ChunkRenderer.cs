using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkRenderer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    public bool showGizmo = false;

    public ChunkData ChunkData { get; private set; }
    public MeshData MeshData;

    public bool ModifiedByPlayer
    {
        get => ChunkData.modifiedByPlayer;
        set => ChunkData.modifiedByPlayer = value;
    }

    public void Initialize(ChunkData data)
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter.sharedMesh = new Mesh();
        mesh = meshFilter.sharedMesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.ChunkData = data;
    }

    public void RenderMesh(MeshData meshData)
    {
        // NOTE: using AddRange instead of concat and then ToArray is a lot faster.
        this.MeshData = meshData;

        mesh.Clear();
        mesh.MarkDynamic();
        mesh.subMeshCount = 2;

        // --- Vertices (opaque first, then transparent) ---
        meshData.vertices.AddRange(meshData.transparentMesh.vertices);
        mesh.SetVertices(meshData.vertices);

        // --- Build UV2 from per-vertex sky/block floats (0..15) ---
        meshData.skyLight.AddRange(meshData.transparentMesh.skyLight);
        meshData.blockLight.AddRange(meshData.transparentMesh.blockLight);

        int vCount = meshData.vertices.Count;
        if (meshData.skyLight.Count != vCount || meshData.blockLight.Count != vCount)
        {
            Debug.LogError($"[ChunkRenderer] UV2 counts don't match vertices. " +
                           $"verts={vCount}, sky={meshData.skyLight.Count}, block={meshData.blockLight.Count}");
        }

            if (meshData.skyLight.Count >= 4) {
        Debug.Log($"First quad sky: {meshData.skyLight[0]}, {meshData.skyLight[1]}, {meshData.skyLight[2]}, {meshData.skyLight[3]}");
        Debug.Log($"First quad blk: {meshData.blockLight[0]}, {meshData.blockLight[1]}, {meshData.blockLight[2]}, {meshData.blockLight[3]}");
    }

        // Pack (sky, block) into Vector2 for TEXCOORD1; keep as floats (do NOT round)
        var uv2Array = new Vector2[meshData.skyLight.Count];
        for (int i = 0; i < uv2Array.Length; i++)
            uv2Array[i] = new Vector2(meshData.skyLight[i], meshData.blockLight[i]);

        // Assign UV2 (TEXCOORD1). Using property accepts Vector2[] directly.
        mesh.uv2 = uv2Array;

        // --- AO / sides data into UV3 (TEXCOORD2) as before ---
        MeshData.sides.AddRange(MeshData.transparentMesh.sides);
        mesh.SetUVs(2, MeshData.sides);

        // --- Triangles (submesh 0 = opaque, submesh 1 = transparent with offset) ---
        mesh.SetTriangles(meshData.triangles, 0);
        int transparentOffset = meshData.vertices.Count - meshData.transparentMesh.vertices.Count;
        mesh.SetTriangles(
            meshData.transparentMesh.triangles.Select(val => val + transparentOffset).ToList(),
            1
        );

        // --- UV0 for albedo ---
        meshData.uv.AddRange(meshData.transparentMesh.uv);
        meshData.colors.AddRange(meshData.transparentMesh.colors);
        if (meshData.colors.Count == meshData.vertices.Count)
            mesh.SetUVs(0, meshData.uv);
            mesh.SetColors(meshData.colors);


        // --- Finalize ---
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        #if UNITY_2020_2_OR_NEWER
                // mesh.Optimize() is deprecated in recent Unity; skip or use MeshUtility.Optimize in editor if desired.
        #else
                mesh.Optimize();
        #endif
    }

    public async void UpdateChunkAsync()
    {
        var data = await World.Instance.CreateMeshDataAsync(new List<ChunkData> { ChunkData });
        RenderMesh(data.Values.First());
    }

    public void UpdateChunk(MeshData meshData)
    {
        RenderMesh(meshData);
    }

    public struct MeshChunkObject
    {
        public ChunkRenderer chunk;
        public MeshData meshData;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        if (ChunkData != null)
        {
            Gizmos.color = (Selection.activeObject == gameObject)
                ? new Color(0, 1, 0, 0.4f)
                : new Color(1, 0, 0, 0.4f);

            Gizmos.DrawCube(
                transform.position + new Vector3(ChunkData.chunkSize / 2f - 0.5f,
                                                 ChunkData.worldRef.worldHeight / 2f - 0.5f,
                                                 ChunkData.chunkSize / 2f - 0.5f),
                new Vector3(ChunkData.chunkSize, ChunkData.worldRef.worldHeight, ChunkData.chunkSize)
            );
        }
    }
#endif
}
