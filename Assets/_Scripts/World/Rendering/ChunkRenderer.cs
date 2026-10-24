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
    private readonly Dictionary<Vector3Int,ModernChestVisual> chestVisuals=new Dictionary<Vector3Int,ModernChestVisual>();
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
        mesh.subMeshCount = 3;
        mesh.SetVertices(meshData.vertices);
        mesh.SetUVs(0, meshData.uv);
        mesh.SetUVs(1, meshData.lightingUVs);
        mesh.SetUVs(2, meshData.sides);
        mesh.SetUVs(3, meshData.smoothLightCorners);
        mesh.SetUVs(4, meshData.faceCoordinates);
        mesh.SetColors(meshData.colors);
        mesh.SetNormals(meshData.normals);
        mesh.SetTriangles(meshData.triangles, 0, false);
        if (meshData.transparentMesh != null)
            mesh.SetTriangles(meshData.transparentMesh.triangles, 1, false);
        if (meshData.waterMesh != null)
            mesh.SetTriangles(meshData.waterMesh.triangles, 2, false);
        mesh.bounds = new Bounds(meshData.boundsCenter, meshData.boundsSize);
        if(meshData.chests!=null||chestVisuals.Count>0)RefreshChestVisuals(meshData.chests);
        meshData.ReleaseAfterUpload();
    }

    private void RefreshChestVisuals(List<Vector3Int> positions)
    {
        var wanted=new HashSet<Vector3Int>();
        if(positions!=null)foreach(var local in positions)
        {
            var block=ChunkData.GetBlock(local);if(block.type!=BlockType.Chest)continue;
            var position=ChunkData.worldPos+local;wanted.Add(position);
            ModernChestVisual visual;
            if(!chestVisuals.TryGetValue(position,out visual))
            {
                var child=new GameObject("ModernChest");child.layer=gameObject.layer;child.transform.SetParent(transform,false);
                child.transform.localPosition=(Vector3)local+new Vector3(.5f,0,.5f);
                visual=child.AddComponent<ModernChestVisual>();chestVisuals.Add(position,visual);
            }
            visual.Initialize(ChunkData.worldRef,position,SpecialBlockGeometry.State(block.type,block.blockShape));
        }
        var removed=new List<Vector3Int>();
        foreach(var pair in chestVisuals)if(!wanted.Contains(pair.Key)){pair.Value.gameObject.SetActive(false);Destroy(pair.Value.gameObject);removed.Add(pair.Key);}
        foreach(var position in removed)chestVisuals.Remove(position);
    }

    public void UpdateChunkAsync()
    {
        ChunkData.worldRef.AddChunkToUpdate(this);
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
