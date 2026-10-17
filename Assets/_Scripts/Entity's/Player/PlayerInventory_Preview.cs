using UnityEngine;

public partial class PlayerInventory
{
    private GameObject inventoryPreview;
    private Mesh inventoryPreviewMesh;
    private Material inventoryPreviewMaterial;
    private GameObject inventoryPreviewHelmet;
    private RectTransform inventoryPreviewRect;

    private void BuildInventoryPreview(Transform parent,PlayerHudAssets assets)
    {
        var template=Resources.Load<Material>("InventoryPlayerMaterial");
        if(template==null)return;
        inventoryPreviewRect=parent.GetComponent<RectTransform>();
        inventoryPreview=new GameObject("InventoryPlayer",typeof(MeshFilter),typeof(MeshRenderer));
        inventoryPreview.layer=parent.gameObject.layer;
        inventoryPreview.transform.SetParent(parent,false);
        inventoryPreview.transform.localPosition=new Vector3(0,-29,-20);
        inventoryPreview.transform.localScale=Vector3.one*29;
        inventoryPreviewMaterial=new Material(template){name="Owned inventory player material"};
        inventoryPreview.GetComponent<MeshRenderer>().sharedMaterial=inventoryPreviewMaterial;
        var data=new MeshData(false);
        SkinBox(data,-4,24,-4,4,32,4,8,0,8,8);
        SkinBox(data,-4,12,-2,4,24,2,20,16,8,12);
        SkinBox(data,-8,12,-2,-4,24,2,44,16,4,12);
        SkinBox(data,4,12,-2,8,24,2,44,16,4,12);
        SkinBox(data,-4,0,-2,0,12,2,4,16,4,12);
        SkinBox(data,0,0,-2,4,12,2,4,16,4,12);
        inventoryPreviewMesh=new Mesh{name="Owned inventory player mesh"};
        inventoryPreviewMesh.SetVertices(data.vertices);
        var uv=new System.Collections.Generic.List<Vector2>();
        foreach(var p in data.uv)uv.Add(new Vector2(p.x,p.y));
        inventoryPreviewMesh.SetUVs(0,uv);inventoryPreviewMesh.SetTriangles(data.triangles,0);
        inventoryPreviewMesh.RecalculateNormals();inventoryPreviewMesh.RecalculateBounds();
        inventoryPreview.GetComponent<MeshFilter>().sharedMesh=inventoryPreviewMesh;
    }
    private void SkinBox(MeshData data,float x0,float y0,float z0,float x1,float y1,float z1,int u,int v,int width,int height)
    {
        var box=new BlockShape(x0,y0,z0,x1,y1,z1);
        float depth=z1-z0;
        float skinHeight=inventoryPreviewMaterial.mainTexture!=null?inventoryPreviewMaterial.mainTexture.height:32;
        foreach(var direction in BlockHelper.directions)
        {
            box.SetFaceVertices(direction,Vector3Int.zero,data);data.AddQuadTriangles();
            float sx=u,sy=v+depth,sw=width,sh=height;
            if(direction==Direction.up){sy=v;}
            else if(direction==Direction.down){sx=u+width;sy=v;}
            else if(direction==Direction.left){sx=u-depth;sw=depth;}
            else if(direction==Direction.right){sx=u+width;sw=depth;}
            else if(direction==Direction.forwards){sx=u+width+depth;}
            if(direction==Direction.up||direction==Direction.down)sh=depth;
            data.AddFaceUVs(0,new Vector4(sw/64f,sh/skinHeight,sx/64f,1-(sy+sh)/skinHeight));
        }
    }
    private void UpdateInventoryPreview()
    {
        if(inventoryPreview==null)return;
        Vector2 point;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(inventoryPreviewRect,Input.mousePosition,
            inventoryCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:inventoryCanvas.worldCamera,out point))
            inventoryPreview.transform.localRotation=Quaternion.Euler(Mathf.Clamp(point.y*.1f,-12,12),Mathf.Clamp(-point.x*.2f,-30,30),0);
    }
    private void RefreshPreviewEquipment()
    {
        if(inventoryPreview==null)return;
        bool pumpkin=equipmentSlots[0]!=null&&equipmentSlots[0].type==ItemType.Pumpkin&&equipmentSlots[0].count>0;
        if(pumpkin&&inventoryPreviewHelmet==null)
        {
            CreateBlockMesh(BlockType.Pumpkin,inventoryPreview.transform);
            inventoryPreviewHelmet=inventoryPreview.transform.GetChild(inventoryPreview.transform.childCount-1).gameObject;
            inventoryPreviewHelmet.transform.localScale=Vector3.one*.56f;
            inventoryPreviewHelmet.transform.localPosition=new Vector3(.28f,1.47f,.28f);
            inventoryPreviewHelmet.transform.localRotation=Quaternion.Euler(0,180,0);
        }
        if(inventoryPreviewHelmet!=null)inventoryPreviewHelmet.SetActive(pumpkin);
    }
    private void DisposeInventoryPreview()
    {
        if(inventoryPreviewMesh!=null)Destroy(inventoryPreviewMesh);
        if(inventoryPreviewMaterial!=null)Destroy(inventoryPreviewMaterial);
        inventoryPreviewMesh=null;inventoryPreviewMaterial=null;
    }
}
