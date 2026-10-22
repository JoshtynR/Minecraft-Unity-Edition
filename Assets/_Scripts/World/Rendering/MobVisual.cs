using System.Collections.Generic;
using UnityEngine;

// The renderer owns its hierarchy; the world caches geometry/materials by species.
public sealed class MobVisual
{
    public readonly GameObject root;
    private readonly Transform[] parts;
    private readonly MeshRenderer[] renderers;
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private Vector3 previous;
    private float walk, amplitude, deathTime, yaw;
    private float soundTime=GameAudio.Variation(4,12),stepDistance;
    private GameObject heart,flames;
    public MobVisual(Transform parent,MobState mob,Mesh[] meshes,Material material)
    {
        parts=new Transform[meshes.Length];renderers=new MeshRenderer[meshes.Length];
        root=new GameObject(mob.species+" "+mob.id);root.transform.SetParent(parent,false);
        root.transform.position=previous=mob.position;root.transform.rotation=Quaternion.Euler(0,mob.yaw,0);
        for(int i=0;i<parts.Length;i++)
        {
            if(meshes[i]==null)continue;
            var child=new GameObject("Part "+i,typeof(MeshFilter),typeof(MeshRenderer));
            parts[i]=child.transform;parts[i].SetParent(root.transform,false);parts[i].localPosition=MobModelGeometry.Pivot(i,mob.species);
            if((i==0&&!MobRules.Hostile(mob.species))||(i==4&&mob.species==MobType.Cow))parts[i].localRotation=Quaternion.Euler(90,0,0);
            child.GetComponent<MeshFilter>().sharedMesh=meshes[i];
            renderers[i]=child.GetComponent<MeshRenderer>();renderers[i].sharedMaterial=material;
            if(mob.species==MobType.Skeleton&&i==4)renderers[i].sharedMaterial=Resources.Load<Material>("SkeletonBowMaterial");
            if(mob.species==MobType.Sheep&&(i==2||i==3||i>=9))
            {
                var fur=Resources.Load<Material>("SheepFurMaterial");if(fur!=null)renderers[i].sharedMaterial=fur;
                if(i==2)parts[i].localRotation=Quaternion.Euler(90,0,0);
            }
            renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    public void Update(MobState mob,World world,float delta)
    {
        root.transform.localScale=Vector3.one*(mob.ageTicks>0?.5f:1);
        if(mob.species==MobType.Sheep)for(int i=0;i<parts.Length;i++)if(i==2||i==3||i>=9)parts[i].gameObject.SetActive(!mob.sheared);
        float t=1-Mathf.Exp(-15*delta);
        root.transform.position=Vector3.Lerp(root.transform.position,mob.position,t);
        yaw=Mathf.LerpAngle(root.transform.eulerAngles.y,mob.yaw,t);root.transform.rotation=Quaternion.Euler(0,yaw,0);
        float moved=new Vector2(root.transform.position.x-previous.x,root.transform.position.z-previous.z).magnitude;
        previous=root.transform.position;walk+=moved*4;
        soundTime-=delta;
        var definition=MobDefinitions.Get(mob.species);
        if(soundTime<=0){soundTime=GameAudio.Variation(8,20);if(definition.AmbientSound!=null)GameAudio.Play(definition.AmbientSound,root.transform.position,.4f,GameAudio.Variation(.9f,1.1f)*(mob.ageTicks>0?1.5f:1));}
        if(mob.grounded)
        {
            stepDistance+=moved;
            if(stepDistance>=1.67f)
            {
                stepDistance%=1.67f;var floor=world.GetBlock(mob.position+Vector3.down*.05f);
                string sound=definition.StepSound??(floor==null?null:BlockSoundRules.Step(floor.type));
                if(sound!=null)GameAudio.Play(sound,root.transform.position,.15f);
            }
        }
        else stepDistance=0;
        amplitude=Mathf.Lerp(amplitude,Mathf.Clamp01(moved/Mathf.Max(.0001f,delta)*.2f),t);
        MobAnimation.Apply(parts,mob,walk,amplitude);
        if(mob.species==MobType.Creeper)root.transform.localScale=Vector3.one*(1+mob.fuseTicks/30f*.15f);
        var sample=world.GetBlock(mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.66f));
        properties.SetVector("_LightLevels",new Vector4(sample==null?15:sample.GetSkyLight(),sample==null?0:sample.GetBlockLight(),1,0));
        properties.SetFloat("_Brightness",sample==null?1:Beta173Lighting.Brightness(sample.GetSkyLight(),sample.GetBlockLight(),world.betaSkylightSubtracted,0,world.dimension == WorldDimension.Nether));
        properties.SetFloat("_Hurt",mob.hurtTicks>0?1:0);
        properties.SetFloat("_Flash",mob.species==MobType.Creeper&&mob.fuseTicks>0&&(mob.fuseTicks/3)%2==0?.8f:0);
        for(int i=0;i<renderers.Length;i++)
        {
            if(renderers[i]==null)continue;
            properties.SetColor("_Tint",mob.species==MobType.Sheep&&(i==2||i==3||i>=9)?MobRules.FleeceTint(mob.fleeceColor):Color.white);
            renderers[i].SetPropertyBlock(properties);
        }
        UpdateEffects(mob,world);
    }
    public bool AnimateDeath(float delta)
    {
        deathTime+=delta;
        float progress=Mathf.Sqrt(Mathf.Max(0,(deathTime*20-1)/20*1.6f));
        root.transform.rotation=Quaternion.Euler(0,yaw,Mathf.Min(1,progress)*90);
        return deathTime<1;
    }
    private GameObject Effect(string name,Mesh mesh,Material material)
    {
        var child=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
        child.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return child;
    }
    private void UpdateEffects(MobState mob,World world)
    {
        var definition=MobDefinitions.Get(mob.species);
        if(mob.loveTicks>0&&heart==null)heart=Effect("Breeding heart",world.MobHeartMesh(),Resources.Load<Material>("MobHeartMaterial"));
        if(heart!=null)
        {
            heart.SetActive(mob.loveTicks>0);float phase=(Time.time*1.5f+mob.id*.17f)%1;
            heart.transform.localPosition=new Vector3(Mathf.Sin(mob.id+phase*6)*.2f,definition.Height+.2f+phase*.5f,0);
            heart.transform.localScale=Vector3.one*.2f;var camera=Camera.main;if(camera!=null)heart.transform.rotation=camera.transform.rotation;
        }
        if(mob.fireTicks>0&&flames==null)flames=Effect("Burning mob",world.MobFireMesh(),Resources.Load<Material>("DroppedBlockMaterial"));
        if(flames!=null)
        {flames.SetActive(mob.fireTicks>0);flames.transform.localPosition=new Vector3(-definition.Radius,0,-definition.Radius);flames.transform.localScale=new Vector3(definition.Radius*2,definition.Height,definition.Radius*2);}
    }
    public void Dispose(){if(root!=null)Object.Destroy(root);}
}
