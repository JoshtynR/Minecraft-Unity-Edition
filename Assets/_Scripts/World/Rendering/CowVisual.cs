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
    private float soundTime=GameAudio.Variation(4,12),stepDistance;
    public CowVisual(Transform parent,CowState cow,Mesh[] meshes,Material material)
    {
        root=new GameObject(cow.species+" "+cow.id);root.transform.SetParent(parent,false);
        root.transform.position=previous=cow.position;root.transform.rotation=Quaternion.Euler(0,cow.yaw,0);
        for(int i=0;i<parts.Length;i++)
        {
            var child=new GameObject(i==0?"Body":i==1?"Head":i<4?"Horn":i==4?"Udder":"Leg",typeof(MeshFilter),typeof(MeshRenderer));
            parts[i]=child.transform;parts[i].SetParent(root.transform,false);parts[i].localPosition=CowModelGeometry.Pivot(i,cow.species);
            if(i==0||(i==4&&cow.species==PassiveAnimal.Cow))parts[i].localRotation=Quaternion.Euler(90,0,0);
            child.GetComponent<MeshFilter>().sharedMesh=meshes[i];
            renderers[i]=child.GetComponent<MeshRenderer>();renderers[i].sharedMaterial=material;
            if(cow.species==PassiveAnimal.Sheep&&(i==2||i==3))
            {
                var fur=Resources.Load<Material>("SheepFurMaterial");if(fur!=null)renderers[i].sharedMaterial=fur;
                if(i==2)parts[i].localRotation=Quaternion.Euler(90,0,0);
            }
            renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    public void Update(CowState cow,World world,float delta)
    {
        root.transform.localScale=Vector3.one*(cow.ageTicks>0?.5f:1);
        if(cow.species==PassiveAnimal.Sheep){parts[2].gameObject.SetActive(!cow.sheared);parts[3].gameObject.SetActive(!cow.sheared);}
        float t=1-Mathf.Exp(-15*delta);
        root.transform.position=Vector3.Lerp(root.transform.position,cow.position,t);
        yaw=Mathf.LerpAngle(root.transform.eulerAngles.y,cow.yaw,t);root.transform.rotation=Quaternion.Euler(0,yaw,0);
        float moved=new Vector2(root.transform.position.x-previous.x,root.transform.position.z-previous.z).magnitude;
        previous=root.transform.position;walk+=moved*4;
        soundTime-=delta;
        string family="mob/"+cow.species.ToString().ToLowerInvariant();
        if(soundTime<=0){soundTime=GameAudio.Variation(8,20);GameAudio.Play(family+"/say",root.transform.position,.4f,GameAudio.Variation(.9f,1.1f)*(cow.ageTicks>0?1.5f:1));}
        if(cow.grounded)
        {
            stepDistance+=moved;
            if(stepDistance>=1.67f){stepDistance%=1.67f;GameAudio.Play(family+"/step",root.transform.position,.15f);}
        }
        else stepDistance=0;
        amplitude=Mathf.Lerp(amplitude,Mathf.Clamp01(moved/Mathf.Max(.0001f,delta)*.2f),t);
        float angle=Mathf.Cos(walk*.6662f)*1.4f*amplitude*Mathf.Rad2Deg;
        parts[5].localRotation=parts[8].localRotation=Quaternion.Euler(angle,0,0);
        parts[6].localRotation=parts[7].localRotation=Quaternion.Euler(-angle,0,0);
        if(cow.species==PassiveAnimal.Chicken)
        {
            parts[7].localRotation=Quaternion.Euler(0,0,cow.grounded?0:Mathf.Sin(Time.time*20)*45);
            parts[4].localRotation=Quaternion.Euler(0,0,cow.grounded?0:-Mathf.Sin(Time.time*20)*45);
        }
        int sky=0,light=0;
        var p=Vector3Int.FloorToInt(cow.position+Vector3.up);
        foreach(var direction in BlockHelper.directions)
        {
            var b=world.GetBlock(p+direction.GetVector());
            if(b==null)continue;sky=Mathf.Max(sky,b.GetSkyLight());light=Mathf.Max(light,b.GetBlockLight());
        }
        properties.SetFloat("_Brightness",Beta173Lighting.Brightness(sky,light,world.betaSkylightSubtracted,0,world.dimension == WorldDimension.Nether));
        properties.SetFloat("_Hurt",cow.hurtTicks>0?1:0);
        for(int i=0;i<renderers.Length;i++)
        {
            properties.SetColor("_Tint",cow.species==PassiveAnimal.Sheep&&(i==2||i==3)?CowRules.FleeceTint(cow.fleeceColor):Color.white);
            renderers[i].SetPropertyBlock(properties);
        }
    }
    public bool AnimateDeath(float delta)
    {
        deathTime+=delta;
        root.transform.rotation=Quaternion.Euler(0,yaw,Mathf.Min(90,deathTime*180));
        return deathTime<1;
    }
    public void Dispose(){if(root!=null)Object.Destroy(root);}
}
