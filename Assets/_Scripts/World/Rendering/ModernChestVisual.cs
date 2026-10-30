using System.Collections.Generic;
using UnityEngine;

public class ModernChestVisual : MonoBehaviour
{
    public static bool Enabled {get;private set;}
    private World world;
    private Vector3Int position;
    private Transform lid;
    private readonly List<Mesh> meshes=new List<Mesh>();
    private readonly List<MeshRenderer> renderers=new List<MeshRenderer>();
    private Material material;
    private MaterialPropertyBlock properties;
    private float progress,lightTime;
    private byte savedFacing,renderFacing;
    private int modelHalf=int.MinValue;
    public static void Prepare()
    {
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        Enabled=assets!=null&&assets.chestSkin!=null&&Resources.Load<Material>("ModernChestMaterial")!=null;
        if(!Enabled)Debug.LogWarning("Modern chest skin/material missing. Chest containers work; closed block meshes remain until chest assets are assigned.");
    }
    public void Initialize(World source,Vector3Int pos,byte facing)
    {
        world=source;position=pos;savedFacing=facing;
        RefreshModel();
    }
    private void RefreshModel()
    {
        byte facing=world.ChestRenderFacing(position,savedFacing);
        var partner=world.ChestPartner(position);int half=0;
        if(partner.HasValue)
        {
            var right=facing==3?Vector3Int.left:facing==4?Vector3Int.back:facing==5?Vector3Int.forward:Vector3Int.right;
            half=partner.Value==position+right?-1:1;
        }
        transform.rotation=Quaternion.Euler(0,ChestModelGeometry.Facing(facing),0);
        if(modelHalf==half&&renderFacing==facing&&material!=null)return;
        modelHalf=half;renderFacing=facing;
        foreach(var renderer in renderers)if(renderer!=null){renderer.gameObject.SetActive(false);Destroy(renderer.gameObject);}
        foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);
        renderers.Clear();meshes.Clear();lid=null;
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        if(material==null)material=new Material(Resources.Load<Material>("ModernChestMaterial"));
        material.mainTexture=half<0&&assets.chestRightSkin!=null?assets.chestRightSkin:half>0&&assets.chestLeftSkin!=null?assets.chestLeftSkin:assets.chestSkin;
        if(properties==null)properties=new MaterialPropertyBlock();
        for(int part=0;part<3;part++)
        {
            Transform parent=part==2?lid:transform;
            var child=new GameObject(part==0?"ChestBody":part==1?"ChestLid":"ChestLatch",typeof(MeshFilter),typeof(MeshRenderer));
            child.layer=gameObject.layer;child.transform.SetParent(parent,false);
            if(part==1){lid=child.transform;lid.localPosition=new Vector3(0,9/16f,7/16f);}
            var data=ChestModelGeometry.Build(part,half);var mesh=new Mesh{name=child.name};
            mesh.SetVertices(data.vertices);mesh.SetTriangles(data.triangles,0);
            var uv=new List<Vector2>();foreach(var p in data.uv)uv.Add(new Vector2(p.x,p.y));
            mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            child.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderers.Add(renderer);
        }
    }
    private void Update()
    {
        if(world==null||lid==null)return;
        progress=Mathf.MoveTowards(progress,world.openChestLids.Contains(position)?1:0,Time.deltaTime*2);
        lid.localRotation=Quaternion.Euler(ChestModelGeometry.LidAngle(progress),0,0);
        if(Time.unscaledTime<lightTime)return;lightTime=Time.unscaledTime+.2f;
        RefreshModel();
        int sky=0,light=0;
        foreach(var direction in BlockHelper.directions)
        {
            var block=world.GetBlock(position+direction.GetVector());if(block==null)continue;
            sky=Mathf.Max(sky,block.GetSkyLight());light=Mathf.Max(light,block.GetBlockLight());
        }
        properties.SetVector("_LightLevels",new Vector4(sky,light,1,0));
        foreach(var renderer in renderers)renderer.SetPropertyBlock(properties);
    }
    private void OnDestroy()
    {foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
