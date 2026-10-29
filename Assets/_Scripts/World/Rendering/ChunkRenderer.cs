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
    private readonly Dictionary<int,ChunkRenderer> sectionRenderers=new Dictionary<int,ChunkRenderer>();
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
        if(!ReferenceEquals(ChunkData,data))ReleaseSectionRenderers();
        meshFilter = GetComponent<MeshFilter>();
        this.ChunkData = data;
    }

    public void RenderMesh(MeshData meshData)
    {
        if (meshData.sourceChunk != null && !ReferenceEquals(meshData.sourceChunk, ChunkData))
        {meshData.ReleaseAfterUpload();return;}
        if(meshData.renderSections!=null)
        {
            if(mesh!=null)
            {
                if(meshFilter==null)meshFilter=GetComponent<MeshFilter>();
                meshFilter.sharedMesh=null;Destroy(mesh);mesh=null;
            }
            try
            {
                for(int index=0;index<meshData.renderSections.Length;index++)
                {
                    var section=meshData.renderSections[index];if(section==null)continue;
                    if(!sectionRenderers.TryGetValue(index,out var renderer))
                    {
                        var child=new GameObject("RenderSection "+index);child.layer=gameObject.layer;
                        child.transform.SetParent(transform,false);
                        renderer=child.AddComponent<ChunkRenderer>();
                        child.GetComponent<MeshRenderer>().sharedMaterials=GetComponent<MeshRenderer>().sharedMaterials;
                        renderer.Initialize(ChunkData);sectionRenderers.Add(index,renderer);
                    }
                    renderer.RenderMesh(section);
                }
            }
            finally {meshData.ReleaseAfterUpload();}
            return;
        }
        meshData.PrepareForRendering();
        var uploaded=new Mesh{name="Chunk "+(ChunkData==null?Vector3Int.zero:ChunkData.worldPos),
            indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,subMeshCount=4};
        try
        {
            PackedChunkVertex.Upload(uploaded,meshData);
            uploaded.SetTriangles(meshData.triangles, 0, false);
            if (meshData.transparentMesh != null)
                uploaded.SetTriangles(meshData.transparentMesh.triangles, 1, false);
            if (meshData.waterMesh != null)
                uploaded.SetTriangles(meshData.waterMesh.triangles, 2, false);
            if (meshData.portalMesh != null)
                uploaded.SetTriangles(meshData.portalMesh.triangles, 3, false);
            uploaded.bounds = new Bounds(meshData.boundsCenter, meshData.boundsSize);
            // Chunk meshes are rebuilt rather than edited in place. Discarding
            // Unity's readable CPU copy leaves only the GPU data after upload.
            uploaded.UploadMeshData(true);
            var previous=mesh;mesh=uploaded;uploaded=null;
            if(meshFilter==null)meshFilter=GetComponent<MeshFilter>();
            meshFilter.sharedMesh=mesh;
            if(previous!=null)Destroy(previous);
            if(meshData.chests!=null||chestVisuals.Count>0)RefreshChestVisuals(meshData.chests);
        }
        finally
        {
            if(uploaded!=null)Destroy(uploaded);
            meshData.ReleaseAfterUpload();
        }
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

    public void ReleaseForPool()
    {
        ReleaseSectionRenderers();
        if(chestVisuals.Count>0)RefreshChestVisuals(null);
        ChunkData=null;
        if(meshFilter==null)meshFilter=GetComponent<MeshFilter>();
        if(meshFilter!=null)meshFilter.sharedMesh=null;
        if(mesh!=null)Destroy(mesh);
        mesh=null;
    }

    private void OnDestroy()
    {
        ReleaseSectionRenderers();
        ChunkData=null;
        if (mesh != null) Destroy(mesh);
        mesh = null;
    }

    private void ReleaseSectionRenderers()
    {
        foreach(var renderer in sectionRenderers.Values)
            if(renderer!=null){renderer.ReleaseForPool();renderer.gameObject.SetActive(false);Destroy(renderer.gameObject);}
        sectionRenderers.Clear();
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
