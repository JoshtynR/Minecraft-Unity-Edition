using System.Collections.Generic;
using UnityEngine;

// Visuals own only their hierarchy; the world shares/owns the nine meshes and skin material.
public sealed class CowVisual
{
    public readonly GameObject root;
    private readonly Transform[] parts = new Transform[CowModelGeometry.PartCount];
    private readonly MeshRenderer[] renderers = new MeshRenderer[CowModelGeometry.PartCount];
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private Vector3 previous;
    private float walk, amplitude, deathTime, yaw;
    public CowVisual(Transform parent,CowState cow,Mesh[] meshes,Material material)
    {
        root=new GameObject("Cow "+cow.id);root.transform.SetParent(parent,false);
        root.transform.position=previous=cow.position;root.transform.rotation=Quaternion.Euler(0,cow.yaw,0);
        for(int i=0;i<parts.Length;i++)
        {
            var child=new GameObject(i==0?"Body":i==1?"Head":i<4?"Horn":i==4?"Udder":"Leg",typeof(MeshFilter),typeof(MeshRenderer));
            parts[i]=child.transform;parts[i].SetParent(root.transform,false);parts[i].localPosition=CowModelGeometry.Pivot(i);
            if(i==0||i==4)parts[i].localRotation=Quaternion.Euler(90,0,0);
            child.GetComponent<MeshFilter>().sharedMesh=meshes[i];
            renderers[i]=child.GetComponent<MeshRenderer>();renderers[i].sharedMaterial=material;
            renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    public void Update(CowState cow,World world,float delta)
    {
        float t=1-Mathf.Exp(-15*delta);
        root.transform.position=Vector3.Lerp(root.transform.position,cow.position,t);
        yaw=Mathf.LerpAngle(root.transform.eulerAngles.y,cow.yaw,t);root.transform.rotation=Quaternion.Euler(0,yaw,0);
        float moved=new Vector2(root.transform.position.x-previous.x,root.transform.position.z-previous.z).magnitude;
        previous=root.transform.position;walk+=moved*4;
        amplitude=Mathf.Lerp(amplitude,Mathf.Clamp01(moved/Mathf.Max(.0001f,delta)*.2f),t);
        float angle=Mathf.Cos(walk*.6662f)*1.4f*amplitude*Mathf.Rad2Deg;
        parts[5].localRotation=parts[8].localRotation=Quaternion.Euler(angle,0,0);
        parts[6].localRotation=parts[7].localRotation=Quaternion.Euler(-angle,0,0);
        int sky=0,light=0;
        var p=Vector3Int.FloorToInt(cow.position+Vector3.up);
        foreach(var direction in BlockHelper.directions)
        {
            var b=world.GetBlock(p+direction.GetVector());
            if(b==null)continue;sky=Mathf.Max(sky,b.GetSkyLight());light=Mathf.Max(light,b.GetBlockLight());
        }
        properties.SetFloat("_Brightness",Beta173Lighting.Brightness(sky,light,world.betaSkylightSubtracted,0));
        properties.SetFloat("_Hurt",cow.hurtTicks>0?1:0);
        foreach(var renderer in renderers)renderer.SetPropertyBlock(properties);
    }
    public bool AnimateDeath(float delta)
    {
        deathTime+=delta;
        root.transform.rotation=Quaternion.Euler(0,yaw,Mathf.Min(90,deathTime*180));
        return deathTime<1;
    }
    public void Dispose(){if(root!=null)Object.Destroy(root);}
}
