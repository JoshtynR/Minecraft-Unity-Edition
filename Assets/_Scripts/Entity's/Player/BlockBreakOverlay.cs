using System.Collections.Generic;
using UnityEngine;

public sealed class BlockBreakOverlay : System.IDisposable
{
    private GameObject root;
    private Mesh mesh;
    private Material material;
    private Vector3Int position;
    private BlockShape shape;
    private BlockType blockType;
    private byte stairFacing;
    private int columns, rows;
    private bool warned;

    public void Show(Block block, int stage)
    {
        if (root == null && !Create()) return;
        var nextShape = block.blockShape.GetShape();
        if (shape == null || position != block.globalWorldPosition || blockType != block.type || !ReferenceEquals(shape, nextShape) || (StairGeometry.IsStair(block.type) && stairFacing != StairGeometry.Facing(block.blockShape)))
        {
            position = block.globalWorldPosition; shape = nextShape;
            blockType = block.type;
            stairFacing = StairGeometry.Facing(block.blockShape);
            root.transform.position = position;
            var data = new MeshData(false);
            var baseUvs = new List<Vector3>();
            foreach (var dir in BlockHelper.directions)
            {
                int parts = StairGeometry.IsStair(block.type) ? 2 : 1;
                for (int part=0; part<parts; part++)
                {
                    var face = parts == 2 ? StairGeometry.RenderFace(stairFacing, part, dir) : shape;
                    if (face == null) continue;
                    face.SetFaceVertices(dir, Vector3Int.zero, data);
                    var mapping = face.FaceUVTransform(dir);
                    AddBaseUVs(baseUvs,block.BlockData.textureData.FaceIndex(block.type,dir),mapping);
                    data.AddFaceUVs(0, face.FaceUVTransform(dir)); data.AddQuadTriangles();
                }
            }
            mesh.Clear(); mesh.SetVertices(data.vertices); mesh.SetUVs(0, data.uv); mesh.SetUVs(1,baseUvs); mesh.SetTriangles(data.triangles, 0); mesh.RecalculateBounds();
        }
        int column = stage % columns, row = stage / columns;
        material.SetVector("_Frame", new Vector4(1f / columns, 1f / rows, (float)column / columns, 1 - (row + 1f) / rows));
        root.SetActive(true);
    }

    private static void AddBaseUVs(List<Vector3> uvs,int layer,Vector4 mapping)
    {
        uvs.Add(new Vector3(mapping.z,mapping.w,layer));
        uvs.Add(new Vector3(mapping.z,mapping.w+mapping.y,layer));
        uvs.Add(new Vector3(mapping.z+mapping.x,mapping.w+mapping.y,layer));
        uvs.Add(new Vector3(mapping.z+mapping.x,mapping.w,layer));
    }

    private bool Create()
    {
        var assets = Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if (assets == null || assets.blockBreak == null || assets.blockBreakMaterial == null)
        {
            if (!warned) Debug.LogWarning("Block cracks need Assets/Textures/Minecraft/block_break.png; import it to bind automatically.");
            warned = true; return false;
        }
        columns = assets.breakColumns; rows = assets.breakRows;
        if (columns <= 0 || rows <= 0)
        {
            for (int c = 1; c <= 10; c++)
                if (10 % c == 0 && assets.blockBreak.width % c == 0 && assets.blockBreak.height % (10 / c) == 0 && assets.blockBreak.width / c == assets.blockBreak.height / (10 / c))
                { columns = c; rows = 10 / c; break; }
        }
        if (columns <= 0 || rows <= 0 || columns * rows < 10)
        {
            if (!warned) Debug.LogWarning("block_break.png needs ten square frames. Set Break Columns/Rows on PlayerHudAssets for another grid layout.");
            warned = true; return false;
        }
        root = new GameObject("Block breaking cracks", typeof(MeshFilter), typeof(MeshRenderer));
        mesh = new Mesh { name = "Owned block crack mesh" };
        material = new Material(assets.blockBreakMaterial) { name = "Owned block crack material" };
        material.mainTexture = assets.blockBreak;
        root.GetComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = root.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return true;
    }
    public void Hide() { if (root != null) root.SetActive(false); }
    public void Dispose()
    {
        if (mesh != null) Object.Destroy(mesh);
        if (material != null) Object.Destroy(material);
        if (root != null) Object.Destroy(root);
        mesh = null; material = null; root = null;
    }
}
