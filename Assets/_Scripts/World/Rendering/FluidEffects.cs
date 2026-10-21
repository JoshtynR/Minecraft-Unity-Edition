using UnityEngine;

// Four bounded reusable emitters; no objects/materials are allocated per splash.
public sealed class FluidEffects : MonoBehaviour
{
    private readonly ParticleSystem[] emitters=new ParticleSystem[4];
    private readonly Material[] materials=new Material[4];
    private AudioClip fizz,splash;
    private AudioClip flow;
    private bool ownsFizz,ownsSplash;
    private float displayTime;
    private float nextFireSound,nextLavaSound,nextFlowSound;
    private readonly System.Random cosmetic=new System.Random();
    private void Awake()
    {
        var template=Resources.Load<Material>("FluidParticleMaterial");
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        for(int i=0;i<4;i++)
        {
            var go=new GameObject("FluidParticles"+i);go.transform.SetParent(transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=256;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=i==2?.8f:0;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            if(template!=null)
            {
                materials[i]=new Material(template);renderer.sharedMaterial=materials[i];
                Texture image=assets==null?null:i==0?assets.smokeParticle:i==1?assets.splashParticle:i==2?assets.lavaParticle:assets.bubbleParticle;
                if(image!=null)materials[i].mainTexture=image;
            }
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            emitters[i]=ps;
            ps.Play();
        }
        fizz=assets!=null&&assets.fluidFizz!=null?assets.fluidFizz:GameAudio.Find("random/fizz");
        splash=assets!=null&&assets.fluidSplash!=null?assets.fluidSplash:GameAudio.Find("liquid/splash");
        ownsFizz=fizz==null;ownsSplash=splash==null;
        if(ownsFizz)fizz=Noise("FluidFizz",false);
        if(ownsSplash)splash=Noise("FluidSplash",true);
        flow=assets!=null&&assets.fluidFlow!=null?assets.fluidFlow:GameAudio.Find("liquid/water");
        if(flow==null)flow=splash;
    }
    private AudioClip Noise(string name,bool low)
    {
        const int rate=22050;var samples=new float[rate/3];float previous=0;
        for(int i=0;i<samples.Length;i++)
        {float noise=(float)(cosmetic.NextDouble()*2-1);previous=low?previous*.93f+noise*.07f:noise;samples[i]=previous*(1-i/(float)samples.Length)*.2f;}
        var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    public void Emit(Vector3 position,byte kind)
    {
        int index=kind==0||kind==4?0:kind==1?1:kind==2?2:3;
        for(int i=0;i<(kind==2?1:8);i++)
        {
            var emit=new ParticleSystem.EmitParams();
            emit.position=position+new Vector3((float)cosmetic.NextDouble()-.5f,.1f,(float)cosmetic.NextDouble()-.5f);
            emit.velocity=new Vector3(((float)cosmetic.NextDouble()-.5f)*.5f,index==0?.8f:index==2?2:1,((float)cosmetic.NextDouble()-.5f)*.5f);
            emit.startLifetime=index==0?1.2f:.6f;emit.startSize=index==0?.2f:.1f;
            emit.startColor=index==0?new Color(.55f,.55f,.55f,.7f):index==2?new Color(1,.45f,.05f,1):new Color(.45f,.65f,1,.8f);
            emitters[index].Emit(emit,1);
        }
        if(kind<2)GameAudio.PlayClip(kind==0?fizz:splash,position,kind==0?.5f:.35f);
        if(kind==2&&Time.unscaledTime>=nextLavaSound)
        {nextLavaSound=Time.unscaledTime+GameAudio.Variation(1,3);GameAudio.Play("liquid/lavapop",position,.25f);}
        if(kind==4&&Time.unscaledTime>=nextFireSound)
        {nextFireSound=Time.unscaledTime+GameAudio.Variation(3,5);GameAudio.Play("fire/fire",position,.2f);}
    }
    private void Update()
    {
        displayTime+=Time.deltaTime;if(displayTime<.05f)return;displayTime=0;
        var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;
        if(player==null||player.CurrentWorld==null)return;
        for(int i=0;i<16;i++)
        {
            var p=Vector3Int.FloorToInt(player.transform.position)+new Vector3Int(cosmetic.Next(-8,9),cosmetic.Next(-4,5),cosmetic.Next(-8,9));
            var b=player.CurrentWorld.GetBlock(p);if(b==null)continue;
            if(b.type==BlockType.Lava&&cosmetic.Next(100)==0)
            {var above=player.CurrentWorld.GetBlock(p+Vector3Int.up);if(above!=null&&above.type==BlockType.Air)Emit((Vector3)p+Vector3.one*.5f,2);}
            else if(b.type==BlockType.Fire&&cosmetic.Next(8)==0)Emit((Vector3)p+Vector3.up*.5f,4);
            else if(b.type==BlockType.Water&&b.waterLevel>0&&b.waterLevel<8&&cosmetic.Next(64)==0&&Time.unscaledTime>=nextFlowSound)
            {nextFlowSound=Time.unscaledTime+GameAudio.Variation(1,2);GameAudio.PlayClip(flow,p,.12f);}
        }
    }
    private void OnDestroy()
    {
        foreach(var ps in emitters)if(ps!=null)Destroy(ps.gameObject);
        foreach(var material in materials)if(material!=null)Destroy(material);
        if(fizz!=null&&ownsFizz)Destroy(fizz);
        if(splash!=null&&ownsSplash)Destroy(splash);
    }
}
