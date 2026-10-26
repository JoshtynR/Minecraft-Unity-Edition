using System.Collections.Generic;
using UnityEngine;

// Bounded reusable emitters; no objects or materials are allocated per burst.
public sealed class FluidEffects : MonoBehaviour
{
    private const float MinecraftTicksPerSecond=20;
    private const int BreakEmitterCount=8;
    private const int EffectEmitterCount=6;
    private readonly ParticleSystem[] emitters=new ParticleSystem[EffectEmitterCount];
    private readonly Material[] materials=new Material[EffectEmitterCount];
    private readonly List<Sprite> atlasSprites=new List<Sprite>(48);
    private readonly ParticleSystem[] breakEmitters=new ParticleSystem[BreakEmitterCount];
    private readonly Material[] breakMaterials=new Material[BreakEmitterCount];
    private readonly Texture[] breakTextures=new Texture[BreakEmitterCount];
    private int nextBreakEmitter;
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
        var atlas=assets==null?null:assets.particleAtlas;
        for(int i=0;i<EffectEmitterCount;i++)
        {
            var go=new GameObject("FluidParticles"+i);go.transform.SetParent(transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.playOnAwake=false;main.maxParticles=256;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.gravityModifier=i==1?1.63f:i==2?1.22f:0;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            if(template!=null)
            {
                materials[i]=new Material(template);renderer.sharedMaterial=materials[i];
                Texture image=atlas!=null?atlas:assets==null?null:i==0||i==4||i==5?assets.smokeParticle:i==1?assets.splashParticle:i==2?assets.lavaParticle:assets.bubbleParticle;
                if(image!=null)materials[i].mainTexture=image;
            }
            if(atlas!=null)ConfigureAtlasAnimation(ps,atlas,i);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            emitters[i]=ps;
            ps.Play();
        }
        for(int i=0;i<BreakEmitterCount;i++)
        {
            var go=new GameObject("BlockBreakParticles"+i);go.transform.SetParent(transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=false;main.playOnAwake=false;main.maxParticles=128;
            main.simulationSpace=ParticleSystemSimulationSpace.World;main.gravityModifier=1.63f;
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
            var animation=ps.textureSheetAnimation;animation.enabled=true;animation.mode=ParticleSystemAnimationMode.Grid;
            animation.numTilesX=4;animation.numTilesY=4;animation.animation=ParticleSystemAnimationType.WholeSheet;
            animation.frameOverTime=new ParticleSystem.MinMaxCurve(0);animation.startFrame=new ParticleSystem.MinMaxCurve(0,1);
            var renderer=ps.GetComponent<ParticleSystemRenderer>();
            if(template!=null){breakMaterials[i]=new Material(template);renderer.sharedMaterial=breakMaterials[i];}
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            breakEmitters[i]=ps;ps.Play();
        }
        fizz=assets!=null&&assets.fluidFizz!=null?assets.fluidFizz:GameAudio.Find("random/fizz");
        splash=assets!=null&&assets.fluidSplash!=null?assets.fluidSplash:GameAudio.Find("liquid/splash");
        ownsFizz=fizz==null;ownsSplash=splash==null;
        if(ownsFizz)fizz=Noise("FluidFizz",false);
        if(ownsSplash)splash=Noise("FluidSplash",true);
        flow=assets!=null&&assets.fluidFlow!=null?assets.fluidFlow:GameAudio.Find("liquid/water");
        if(flow==null)flow=splash;
    }
    private void ConfigureAtlasAnimation(ParticleSystem ps,Texture2D atlas,int effect)
    {
        var animation=ps.textureSheetAnimation;animation.enabled=true;animation.mode=ParticleSystemAnimationMode.Sprites;
        // Beta's generic/explode particles count texture indices down from 7.
        if(effect==0||effect==4||effect==5)for(int i=7;i>=0;i--)AddAtlasSprite(animation,atlas,i*8,0,8,8);
        // Splash/rain is texture index 21, not the later four-frame animation.
        else if(effect==1)AddAtlasSprite(animation,atlas,40,8,8,8);
        else if(effect==2)AddAtlasSprite(animation,atlas,8,24,8,8);
        else if(effect==3)AddAtlasSprite(animation,atlas,0,16,8,8);
        animation.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));
        animation.startFrame=new ParticleSystem.MinMaxCurve(0);
    }
    private void AddAtlasSprite(ParticleSystem.TextureSheetAnimationModule animation,Texture2D atlas,int x,int top,int width,int height)
    {
        var sprite=Sprite.Create(atlas,new Rect(x,atlas.height-top-height,width,height),new Vector2(.5f,.5f),width);
        sprite.name="ParticleAtlas_"+x+"_"+top;atlasSprites.Add(sprite);animation.AddSprite(sprite);
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
        int index=kind==0?0:kind==1?1:kind==2?2:kind==4?4:3;
        int count=kind==0||kind==1?8:1;
        for(int i=0;i<count;i++)
        {
            var emit=new ParticleSystem.EmitParams();
            float x=RandomSigned(.5f),z=RandomSigned(.5f);
            if(kind==0||kind==4)
            {
                float shade=RandomRange(.3f,.6f);
                emit.position=position+new Vector3(x,.7f,z);
                emit.velocity=new Vector3(RandomSigned(.4f),RandomRange(.2f,.6f),RandomSigned(.4f));
                emit.startLifetime=MinecraftLifetime(8,.8f,.2f)*(kind==0?2.5f:1.5f);
                emit.startSize=RandomRange(.35f,.55f);emit.startColor=new Color(shade,shade,shade,1);
            }
            else if(kind==1)
            {
                emit.position=position+new Vector3(x,0,z);
                emit.velocity=new Vector3(RandomSigned(1.2f),RandomRange(2,4),RandomSigned(1.2f));
                emit.startLifetime=MinecraftLifetime(8,.8f,.2f);emit.startSize=RandomRange(.08f,.16f);emit.startColor=Color.white;
            }
            else if(kind==2)
            {
                emit.position=position+new Vector3(x,.5f,z);
                emit.velocity=new Vector3(RandomSigned(.4f)*MinecraftTicksPerSecond,RandomRange(.05f,.45f)*MinecraftTicksPerSecond,RandomSigned(.4f)*MinecraftTicksPerSecond);
                emit.startLifetime=MinecraftLifetime(16,.8f,.2f);emit.startSize=RandomRange(.16f,.24f);emit.startColor=Color.white;
            }
            else
            {
                emit.position=position+new Vector3(RandomSigned(.2f),RandomSigned(.1f),RandomSigned(.2f));
                emit.velocity=new Vector3(RandomSigned(.2f),RandomRange(1.5f,3),RandomSigned(.2f));
                emit.startLifetime=MinecraftLifetime(8,.8f,.2f);emit.startSize=RandomRange(.08f,.14f);emit.startColor=Color.white;
            }
            emitters[index].Emit(emit,1);
        }
        if(kind<2)GameAudio.PlayClip(kind==0?fizz:splash,position,kind==0?.5f:.35f);
        if(kind==2&&Time.unscaledTime>=nextLavaSound)
        {nextLavaSound=Time.unscaledTime+GameAudio.Variation(1,3);GameAudio.Play("liquid/lavapop",position,.25f);}
        if(kind==4&&Time.unscaledTime>=nextFireSound)
        {nextFireSound=Time.unscaledTime+GameAudio.Variation(3,5);GameAudio.Play("fire/fire",position,.2f);}
    }
    public void EmitMobDeath(Vector3 position,float radius,float height)
    {
        // Classic entity death uses twenty expanding smoke/explode particles.
        // Reuse the bounded smoke emitter so deaths allocate no GameObjects.
        for(int i=0;i<20;i++)
        {
            var emit=new ParticleSystem.EmitParams();
            emit.position=position+new Vector3(((float)cosmetic.NextDouble()*2-1)*radius,(float)cosmetic.NextDouble()*height,((float)cosmetic.NextDouble()*2-1)*radius);
            emit.velocity=new Vector3((Gaussian()*.02f+RandomSigned(.05f))*MinecraftTicksPerSecond,
                (Gaussian()*.02f+RandomSigned(.05f))*MinecraftTicksPerSecond,
                (Gaussian()*.02f+RandomSigned(.05f))*MinecraftTicksPerSecond);
            emit.startLifetime=MinecraftLifetime(4,.9f,.1f);
            emit.startSize=RandomRange(.1f,.14f);
            float shade=RandomRange(.7f,1);emit.startColor=new Color(shade,shade,shade,1);
            emitters[5].Emit(emit,1);
        }
    }
    private float RandomSigned(float radius){return ((float)cosmetic.NextDouble()*2-1)*radius;}
    private float RandomRange(float minimum,float maximum){return minimum+(float)cosmetic.NextDouble()*(maximum-minimum);}
    private float MinecraftLifetime(float numeratorTicks,float span,float floor){return numeratorTicks/(RandomRange(0,span)+floor)/MinecraftTicksPerSecond;}
    private float Gaussian()
    {
        float a=Mathf.Max(.000001f,(float)cosmetic.NextDouble());
        return Mathf.Sqrt(-2*Mathf.Log(a))*Mathf.Cos((float)cosmetic.NextDouble()*Mathf.PI*2);
    }
    private Vector3 ClassicChipVelocity(Vector3 baseMotion)
    {
        var motion=baseMotion+new Vector3(RandomSigned(.4f),RandomSigned(.4f),RandomSigned(.4f));
        float length=motion.magnitude;if(length<.00001f)motion=Vector3.up;else motion*=1/length;
        float speed=((float)cosmetic.NextDouble()+(float)cosmetic.NextDouble()+1)*.15f;
        return motion*(speed*.4f*MinecraftTicksPerSecond)+Vector3.up*(.1f*MinecraftTicksPerSecond);
    }
    private int BreakEmitter(Texture texture)
    {
        for(int i=0;i<BreakEmitterCount;i++)if(breakTextures[i]==texture)return i;
        for(int i=0;i<BreakEmitterCount;i++)if(breakEmitters[i].particleCount==0)
        {breakTextures[i]=texture;if(breakMaterials[i]!=null)breakMaterials[i].mainTexture=texture;return i;}
        int index=nextBreakEmitter++%BreakEmitterCount;
        breakEmitters[index].Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        breakTextures[index]=texture;if(breakMaterials[index]!=null)breakMaterials[index].mainTexture=texture;breakEmitters[index].Play();return index;
    }
    private bool BlockParticle(World world,Vector3Int cell,BlockType type,Texture preferred,out ParticleSystem ps,out Color color)
    {
        ps=null;color=Color.white;
        if(world==null||type==BlockType.Air||type==BlockType.Nothing)return false;
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;
        if(definitions==null||typeIndex<0||typeIndex>=definitions.Length)return false;
        var data=definitions[typeIndex];if(data==null)return false;
        var texture=preferred!=null?preferred:data.textureData.side!=null?data.textureData.side:data.textureData.top!=null?data.textureData.top:data.textureData.bottom;
        if(texture==null)return false;
        ps=breakEmitters[BreakEmitter(texture)];
        var sample=world.GetBlock(cell);int sky=sample==null?15:sample.GetSkyLight(),block=sample==null?0:sample.GetBlockLight();
        var light=LightmapLighting.Sample(sky,block,LightmapLighting.SunBrightness(SkyClock.Angle(world.CurrentWorldTicks)),world.gamma,world.dimension==WorldDimension.Nether);
        var tint=data.albedoTint;color=new Color(tint.r*light.r*.6f,tint.g*light.g*.6f,tint.b*light.b*.6f,tint.a);return true;
    }
    public void EmitBlockBreak(World world,Vector3Int cell,BlockType type)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;
        if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];
        // EntityDiggingFX asks the block for side zero, which is the bottom
        // face. This notably makes grass debris use dirt rather than grass-side.
        var texture=data.textureData.bottom!=null?data.textureData.bottom:data.textureData.side!=null?data.textureData.side:data.textureData.top;
        if(!BlockParticle(world,cell,type,texture,out var ps,out var color))return;
        var center=(Vector3)cell+Vector3.one*.5f;
        // Classic destroy effects divide the block into a 4x4x4 grid. The
        // texture-sheet module independently chooses one of sixteen face chips.
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
        {
            var position=(Vector3)cell+new Vector3((x+.5f)/4,(y+.5f)/4,(z+.5f)/4);
            var outward=position-center;
            var emit=new ParticleSystem.EmitParams{position=position,velocity=ClassicChipVelocity(outward),
                startLifetime=MinecraftLifetime(4,.9f,.1f),startSize=RandomRange(.12f,.24f),startColor=color};
            ps.Emit(emit,1);
        }
    }
    public void EmitBlockHit(World world,Vector3Int cell,BlockType type,Vector3 hitPoint)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;
        if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];var local=hitPoint-(Vector3)cell;
        float bottom=Mathf.Abs(local.y),top=Mathf.Abs(1-local.y);
        Texture texture=bottom<top?data.textureData.bottom:data.textureData.top;
        if(Mathf.Min(bottom,top)>.01f)texture=data.textureData.side;
        if(!BlockParticle(world,cell,type,texture,out var ps,out var color))return;
        var p=(Vector3)cell+new Vector3(RandomRange(.1f,.9f),RandomRange(.1f,.9f),RandomRange(.1f,.9f));
        float edge=bottom<top?0:1;if(Mathf.Min(bottom,top)<=.01f)p.y=cell.y+edge+(edge==0?-.1f:.1f);
        else
        {
            float left=Mathf.Abs(local.x),right=Mathf.Abs(1-local.x),back=Mathf.Abs(local.z),front=Mathf.Abs(1-local.z);
            float nearest=Mathf.Min(Mathf.Min(left,right),Mathf.Min(back,front));
            if(nearest==left)p.x=cell.x-.1f;else if(nearest==right)p.x=cell.x+1.1f;else if(nearest==back)p.z=cell.z-.1f;else p.z=cell.z+1.1f;
        }
        var emit=new ParticleSystem.EmitParams{position=p,velocity=ClassicChipVelocity(Vector3.zero)*.2f,
            startLifetime=MinecraftLifetime(4,.9f,.1f),startSize=RandomRange(.07f,.14f),startColor=color};
        ps.Emit(emit,1);
    }
    public void EmitSprintDust(World world,Vector3 position,Vector3 horizontalVelocity,Block block)
    {
        if(block==null)return;int typeIndex=(int)block.type;var definitions=BlockDataManager.blockTypeDataDictionary;
        if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var texture=definitions[typeIndex].textureData.top!=null?definitions[typeIndex].textureData.top:definitions[typeIndex].textureData.side;
        var cell=block.globalWorldPosition;
        if(!BlockParticle(world,cell,block.type,texture,out var ps,out var color))return;
        var emit=new ParticleSystem.EmitParams{position=position+new Vector3(RandomSigned(.3f),.08f,RandomSigned(.3f)),
            velocity=new Vector3(-horizontalVelocity.x*.2f,1.5f,-horizontalVelocity.z*.2f),
            startLifetime=RandomRange(.25f,.5f),startSize=RandomRange(.08f,.14f),startColor=color};
        ps.Emit(emit,1);
    }
    private void Update()
    {
        displayTime+=Time.deltaTime;if(displayTime<.05f)return;displayTime=0;
        var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;
        if(player==null||player.CurrentWorld==null)return;
        for(int i=0;i<128;i++)
        {
            var p=Vector3Int.FloorToInt(player.transform.position)+new Vector3Int(cosmetic.Next(-8,9),cosmetic.Next(-4,5),cosmetic.Next(-8,9));
            var b=player.CurrentWorld.GetBlock(p);if(b==null)continue;
            if(b.type==BlockType.Lava&&cosmetic.Next(100)==0)
            {var above=player.CurrentWorld.GetBlock(p+Vector3Int.up);if(above!=null&&above.type==BlockType.Air)Emit((Vector3)p+Vector3.one*.5f,2);}
            else if(b.type==BlockType.Fire)Emit((Vector3)p+Vector3.one*.5f,4);
            else if(b.type==BlockType.Water&&b.waterLevel>0&&b.waterLevel<8&&cosmetic.Next(64)==0&&Time.unscaledTime>=nextFlowSound)
            {nextFlowSound=Time.unscaledTime+GameAudio.Variation(1,2);GameAudio.PlayClip(flow,p,.12f);}
        }
    }
    private void OnDestroy()
    {
        foreach(var ps in emitters)if(ps!=null)Destroy(ps.gameObject);
        foreach(var material in materials)if(material!=null)Destroy(material);
        foreach(var sprite in atlasSprites)if(sprite!=null)Destroy(sprite);
        foreach(var ps in breakEmitters)if(ps!=null)Destroy(ps.gameObject);
        foreach(var material in breakMaterials)if(material!=null)Destroy(material);
        if(fizz!=null&&ownsFizz)Destroy(fizz);
        if(splash!=null&&ownsSplash)Destroy(splash);
    }
}
