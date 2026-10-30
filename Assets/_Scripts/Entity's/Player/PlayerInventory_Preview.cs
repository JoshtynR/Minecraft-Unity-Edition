using UnityEngine;

public partial class PlayerInventory
{
    private GameObject inventoryPreview, inventoryPreviewHelmet;
    private MeshData inventoryPreviewData;
    private Texture inventoryPreviewSkin;
    private InventoryPlayerGraphic inventoryPreviewGraphic;
    private InventoryPlayerGraphic[] inventoryHelmetGraphics;
    private RectTransform inventoryPreviewRect;
    private PlayerHudAssets inventoryPreviewAssets;
    private readonly InventoryPlayerGraphic[] armourPreviewGraphics=new InventoryPlayerGraphic[4],leatherPreviewGraphics=new InventoryPlayerGraphic[4];
    private readonly MeshData[] armourPreviewData=new MeshData[4];
    private readonly ItemType[] armourPreviewTypes=new ItemType[4];

    private void BuildInventoryPreview(Transform parent,PlayerHudAssets assets)
    {
        inventoryPreviewAssets=assets;
        inventoryPreviewSkin=assets!=null?assets.inventoryPlayerSkin:null;
        if(inventoryPreviewSkin==null)
        {
            var template=Resources.Load<Material>("InventoryPlayerMaterial");
            if(template!=null)inventoryPreviewSkin=template.mainTexture;
        }
        if(inventoryPreviewSkin==null)
        {
            // A missing optional asset must not leave an entirely empty preview window.
            inventoryPreviewSkin=Texture2D.whiteTexture;
            Debug.LogWarning("Inventory player skin is missing. Assign Inventory Player Skin on Resources/PlayerHudAssets, or run Minecraft > Textures > Refresh GUI and Chest Assets.");
        }
        inventoryPreviewRect=parent.GetComponent<RectTransform>();
        var rect=CreativeRect("InventoryPlayer",parent,0,0,50,70);
        inventoryPreview=rect.gameObject;
        inventoryPreviewGraphic=inventoryPreview.AddComponent<InventoryPlayerGraphic>();
        var data=new MeshData(false);
        SkinBox(data,-4,24,-4,4,32,4,8,0,8,8);
        SkinBox(data,-4,12,-2,4,24,2,20,16,8,12);
        SkinBox(data,-8,12,-2,-4,24,2,44,16,4,12);
        SkinBox(data,4,12,-2,8,24,2,44,16,4,12);
        SkinBox(data,-4,0,-2,0,12,2,4,16,4,12);
        SkinBox(data,0,0,-2,4,12,2,4,16,4,12);
        inventoryPreviewData=data;
        inventoryPreviewGraphic.Initialize(data,inventoryPreviewSkin);
        // Draw inner leggings before the outer chestplate/boots/helmet.
        foreach(int part in new[]{2,3,1,0})
        {
            armourPreviewGraphics[part]=CreativeRect("PreviewArmour"+part,parent,0,0,50,70).gameObject.AddComponent<InventoryPlayerGraphic>();
            leatherPreviewGraphics[part]=CreativeRect("PreviewLeatherDetails"+part,parent,0,0,50,70).gameObject.AddComponent<InventoryPlayerGraphic>();
            armourPreviewGraphics[part].gameObject.SetActive(false);leatherPreviewGraphics[part].gameObject.SetActive(false);
        }
    }
    private void SkinBox(MeshData data,float x0,float y0,float z0,float x1,float y1,float z1,int u,int v,int width,int height,float inflate=0,float atlasHeight=0)
    {
        var box=new BlockShape(x0-inflate,y0-inflate,z0-inflate,x1+inflate,y1+inflate,z1+inflate);
        float depth=z1-z0;
        float skinHeight=atlasHeight>0?atlasHeight:inventoryPreviewSkin.height*64f/inventoryPreviewSkin.width;
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
        if(inventoryPreviewGraphic==null)return;
        Vector2 point;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(inventoryPreviewRect,Input.mousePosition,
            inventoryCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:inventoryCanvas.worldCamera,out point))
        {
            float pitch=Mathf.Clamp(point.y*.1f,-12,12),yaw=Mathf.Clamp(-point.x*.2f,-30,30);
            inventoryPreviewGraphic.SetLook(pitch,yaw);
            for(int i=0;i<4;i++)
            {if(armourPreviewGraphics[i]!=null)armourPreviewGraphics[i].SetLook(pitch,yaw);if(leatherPreviewGraphics[i]!=null)leatherPreviewGraphics[i].SetLook(pitch,yaw);}
            if(inventoryHelmetGraphics!=null)
                foreach(var face in inventoryHelmetGraphics)face.SetLook(pitch,yaw);
        }
    }
    private void RefreshPreviewEquipment()
    {
        if(inventoryPreview==null)return;
        RefreshArmourPreview();
        bool pumpkin=equipmentSlots[0]!=null&&equipmentSlots[0].type==ItemType.Pumpkin&&equipmentSlots[0].count>0;
        if(pumpkin&&inventoryPreviewHelmet==null)
        {
            inventoryPreviewHelmet=CreativeRect("PreviewPumpkin",inventoryPreviewRect,0,0,50,70).gameObject;
            inventoryHelmetGraphics=new InventoryPlayerGraphic[BlockHelper.directions.Length];
            var textures=BlockDataManager.blockTypeDataDictionary[(int)BlockType.Pumpkin].textureData;
            for(int i=0;i<inventoryHelmetGraphics.Length;i++)
            {
                var direction=BlockHelper.directions[i];
                Texture texture=direction==Direction.up?textures.top:direction==Direction.down?textures.bottom:
                    direction==Direction.backwards?textures.front:direction==Direction.forwards?textures.back:
                    direction==Direction.left?textures.left:textures.right;
                if(texture==null)texture=textures.side;
                var face=CreativeRect(direction.ToString(),inventoryPreviewHelmet.transform,0,0,50,70).gameObject.AddComponent<InventoryPlayerGraphic>();
                var data=new MeshData(false);
                new BlockShape(-4.5f,23.5f,-4.5f,4.5f,32.5f,4.5f).SetFaceVertices(direction,Vector3Int.zero,data);
                data.AddQuadTriangles();data.AddFaceUVs(0);
                face.Initialize(data,texture);inventoryHelmetGraphics[i]=face;
            }
        }
        if(inventoryPreviewHelmet!=null)inventoryPreviewHelmet.SetActive(pumpkin);
    }
    private MeshData BuildArmourPreview(int part)
    {
        var data=new MeshData(false);float inflate=part==2?.5f:1;
        if(part==0)SkinBox(data,-4,24,-4,4,32,4,8,0,8,8,inflate,32);
        if(part==1||part==2)SkinBox(data,-4,12,-2,4,24,2,20,16,8,12,inflate,32);
        if(part==1)
        {
            SkinBox(data,-8,12,-2,-4,24,2,44,16,4,12,inflate,32);
            SkinBox(data,4,12,-2,8,24,2,44,16,4,12,inflate,32);
        }
        if(part==2||part==3)
        {
            SkinBox(data,-4,0,-2,0,12,2,4,16,4,12,inflate,32);
            SkinBox(data,0,0,-2,4,12,2,4,16,4,12,inflate,32);
        }
        return data;
    }
    private void RefreshArmourPreview()
    {
        for(int part=0;part<4;part++)
        {
            var graphic=armourPreviewGraphics[part];if(graphic==null)continue;
            var stack=Stack(equipmentSlots[part]);var type=stack.Empty?ItemType.Nothing:stack.type;
            if(type==armourPreviewTypes[part])continue;
            armourPreviewTypes[part]=type;graphic.gameObject.SetActive(false);leatherPreviewGraphics[part].gameObject.SetActive(false);
            if(ItemDurability.ArmorPart(type)!=part||inventoryPreviewAssets==null)continue;
            int material=((int)type-(int)ItemType.LeatherHelmet)/4,layer=part==2?1:0,index=material*2+layer;
            var textures=inventoryPreviewAssets.armourLayers;
            if(textures==null||index>=textures.Length||textures[index]==null)continue;
            if(armourPreviewData[part]==null)armourPreviewData[part]=BuildArmourPreview(part);
            graphic.color=material==0?new Color(160/255f,101/255f,64/255f,1):Color.white;
            graphic.Initialize(armourPreviewData[part],textures[index]);graphic.gameObject.SetActive(true);
            var overlays=inventoryPreviewAssets.leatherArmourOverlays;
            if(material==0&&overlays!=null&&overlays.Length>layer&&overlays[layer]!=null)
            {var detail=leatherPreviewGraphics[part];detail.Initialize(armourPreviewData[part],overlays[layer]);detail.gameObject.SetActive(true);}
        }
    }
    private void DisposeInventoryPreview()
    {
        // Canvas graphics own their internal meshes. Skins/materials remain shared assets.
        inventoryPreviewData=null;inventoryPreviewSkin=null;
        inventoryPreviewGraphic=null;inventoryHelmetGraphics=null;
        inventoryPreviewAssets=null;
        for(int i=0;i<4;i++){armourPreviewData[i]=null;armourPreviewGraphics[i]=null;leatherPreviewGraphics[i]=null;}
    }
}
