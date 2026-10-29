using System.Collections.Generic;
using UnityEngine;

// The renderer owns its hierarchy; the world caches geometry/materials by species.
public sealed class MobVisual
{
    public readonly GameObject root;
    private readonly Transform[] parts;
    private readonly MeshRenderer[] renderers;
    private readonly Color[] tints;
    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
    private Vector3 previous;
    private readonly EntityVisualMotion motion=new EntityVisualMotion();
    private float walk, amplitude, deathTime, yaw;
    private readonly float deathRadius,deathHeight;
    private bool deathSmoke,deathPropertiesApplied,shearedInitialized,lastSheared;
    private int lastSky=-1,lastBlock=-1,lastHurt=-1,lastFleece=-1;
    private Vector3Int lastLightCell=new Vector3Int(int.MinValue,int.MinValue,int.MinValue);
    private float nextLightSample;
    private float lastFlash=-1;
    private float soundTime=GameAudio.Variation(4,12),stepDistance;
    private GameObject heart,flames;
    public MobVisual(Transform parent,MobState mob,Mesh[] meshes,Material material)
    {
        parts=new Transform[meshes.Length];renderers=new MeshRenderer[meshes.Length];tints=new Color[meshes.Length];
        deathRadius=MobRules.Width(mob);deathHeight=MobRules.BodyHeight(mob);
        root=new GameObject(mob.species+" "+mob.id);root.transform.SetParent(parent,false);
        root.transform.position=previous=mob.position;root.transform.rotation=Quaternion.Euler(0,mob.yaw,0);
        motion.Step(mob.position,0,.05f);
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
        if(mob.species==MobType.Sheep&&(!shearedInitialized||lastSheared!=mob.sheared))
        {
            shearedInitialized=true;lastSheared=mob.sheared;
            for(int i=0;i<parts.Length;i++)if(i==2||i==3||i>=9)parts[i].gameObject.SetActive(!mob.sheared);
        }
        float t=1-Mathf.Exp(-15*delta);
        root.transform.position=motion.Step(mob.position,delta,world.DropAuthority?.05f:.2f);
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
        var lightCell=Vector3Int.FloorToInt(mob.position+Vector3.up*(MobRules.BodyHeight(mob)*.66f));
        bool sampleLight=lightCell!=lastLightCell||Time.unscaledTime>=nextLightSample;
        int sky=lastSky<0?15:lastSky,block=lastBlock<0?0:lastBlock,hurt=mob.hurtTicks>0?1:0;
        if(sampleLight)
        {
            lastLightCell=lightCell;nextLightSample=Time.unscaledTime+.25f;
            var sample=world.GetBlock(lightCell);
            sky=sample==null?15:sample.GetSkyLight();block=sample==null?0:sample.GetBlockLight();
        }
        float flash=mob.species==MobType.Creeper&&mob.fuseTicks>0&&(mob.fuseTicks/3)%2==0?.8f:0;
        int fleece=mob.species==MobType.Sheep?mob.fleeceColor:0;
        if(sky!=lastSky||block!=lastBlock||hurt!=lastHurt||flash!=lastFlash||fleece!=lastFleece)
        {
            lastSky=sky;lastBlock=block;lastHurt=hurt;lastFlash=flash;lastFleece=fleece;
            properties.SetVector("_LightLevels",new Vector4(sky,block,1,0));properties.SetFloat("_Hurt",hurt);properties.SetFloat("_Flash",flash);
            for(int i=0;i<renderers.Length;i++)
            {
                if(renderers[i]==null)continue;
                tints[i]=mob.species==MobType.Sheep&&(i==2||i==3||i>=9)?MobRules.FleeceTint(fleece):Color.white;
                properties.SetColor("_Tint",tints[i]);renderers[i].SetPropertyBlock(properties);
            }
        }
        UpdateEffects(mob,world);
    }
    public bool AnimateDeath(float delta,World world)
    {
        deathTime+=delta;
        float progress=Mathf.Sqrt(Mathf.Max(0,(deathTime*20-1)/20*1.6f));
        root.transform.rotation=Quaternion.Euler(0,yaw,Mathf.Min(1,progress)*90);
        if(!deathPropertiesApplied)
        {
            deathPropertiesApplied=true;properties.SetFloat("_Hurt",1);properties.SetFloat("_Flash",0);
            for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)
            {properties.SetColor("_Tint",tints[i]);renderers[i].SetPropertyBlock(properties);}
        }
        if(deathTime>=1&&!deathSmoke)
        {deathSmoke=true;if(world!=null)world.PlayMobDeathSmoke(root.transform.position,deathRadius,deathHeight);}
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
