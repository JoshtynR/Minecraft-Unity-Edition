using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ChunkRenderer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;
    public bool showGizmo = false;

    public ChunkData ChunkData { get; private set; }

    public bool ModifiedByPlayer
    {
        get => ChunkData.modifiedByPlayer;
        set => ChunkData.modifiedByPlayer = value;
    }

    public void Initialize(ChunkData data)
    {
        meshFilter = GetComponent<MeshFilter>();
        if (mesh == null) mesh = new Mesh();
        meshFilter.sharedMesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.ChunkData = data;
    }

    public void RenderMesh(MeshData meshData)
    {
        if (meshData.sourceChunk != null && !ReferenceEquals(meshData.sourceChunk, ChunkData)) return;
        meshData.PrepareForRendering();
        mesh.Clear();
        mesh.MarkDynamic();
        mesh.subMeshCount = 2;
        mesh.SetVertices(meshData.vertices);
        mesh.SetUVs(0, meshData.uv);
        mesh.SetUVs(1, meshData.lightingUVs);
        mesh.SetUVs(2, meshData.sides);
        mesh.SetUVs(3, meshData.smoothLightCorners);
        mesh.SetColors(meshData.colors);
        mesh.SetTriangles(meshData.triangles, 0);
        if (meshData.transparentMesh != null)
            mesh.SetTriangles(meshData.transparentMesh.triangles, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        // MeshData is staging data; retaining it here doubles chunk geometry memory.
    }

    public void UpdateChunkAsync()
    {
        World.Instance.AddChunkToUpdate(this);
    }

    private void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        mesh = null;
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
