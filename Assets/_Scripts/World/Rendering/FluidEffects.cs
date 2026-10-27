using System.Collections.Generic;
using UnityEngine;

// Beta renders particles as hand-ticked entities. Unity's ParticleSystem has
// different billboard sizing, sheet animation, motion and lifetime semantics,
// so these are simulated at 20 Hz and drawn as batched camera-facing quads.
public sealed class FluidEffects : MonoBehaviour
{
    private const float TickLength=.05f;
    private const int EffectLayerCount=6,BreakLayerCount=8;
    private enum Motion : byte {Smoke,Explosion,Gravity,Bubble,Lava}
    private sealed class Particle
    {
        public Vector3 position,previous,velocity;
        public Color color;
        public Rect uv;
        public float scale,originalScale,gravity=1;
        public int age,maxAge;
        public Motion motion;
        public bool collide=true;
    }
    private sealed class Layer
    {
        public readonly List<Particle> particles;
        public readonly GameObject root;
        public readonly Mesh mesh;
        public readonly Material material;
        public Texture texture;
        private readonly Vector3[] vertices;
        private readonly Vector2[] uvs;
        private readonly Color[] colors;
        private readonly int[] triangles;
        public int Count=>particles.Count;
        public Layer(Transform parent,string name,Material template,Texture image,int capacity)
        {
            particles=new List<Particle>(capacity);vertices=new Vector3[capacity*4];uvs=new Vector2[capacity*4];
            colors=new Color[capacity*4];triangles=new int[capacity*6];
            for(int i=0;i<capacity;i++){int v=i*4,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+2;triangles[t+4]=v+3;triangles[t+5]=v;}
            root=new GameObject(name);root.transform.SetParent(parent,false);
            var filter=root.AddComponent<MeshFilter>();var renderer=root.AddComponent<MeshRenderer>();
            mesh=new Mesh{name=name+"Mesh"};mesh.MarkDynamic();filter.sharedMesh=mesh;
            if(template!=null){material=new Material(template);material.mainTexture=image;renderer.sharedMaterial=material;}
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;texture=image;
        }
        public void SetTexture(Texture image){texture=image;if(material!=null)material.mainTexture=image;}
        public void Add(Particle p){if(particles.Count==vertices.Length/4)particles.RemoveAt(0);particles.Add(p);}
        public void Clear(){particles.Clear();mesh.Clear();}
        public void Draw(Camera camera,float interpolation)
        {
            if(camera==null||particles.Count==0){mesh.Clear();return;}
            Vector3 right=camera.transform.right,up=camera.transform.up;
            for(int i=0;i<particles.Count;i++)
            {
                var p=particles[i];Vector3 center=Vector3.Lerp(p.previous,p.position,interpolation);float radius=.1f*p.scale;
                Vector3 x=right*radius,y=up*radius;int v=i*4;
                vertices[v]=center-x-y;vertices[v+1]=center-x+y;vertices[v+2]=center+x+y;vertices[v+3]=center+x-y;
                float u0=p.uv.xMin,u1=p.uv.xMax,v0=p.uv.yMin,v1=p.uv.yMax;
                uvs[v]=new Vector2(u0,v0);uvs[v+1]=new Vector2(u0,v1);uvs[v+2]=new Vector2(u1,v1);uvs[v+3]=new Vector2(u1,v0);
                colors[v]=colors[v+1]=colors[v+2]=colors[v+3]=p.color;
            }
            int vertexCount=particles.Count*4,indexCount=particles.Count*6;mesh.Clear();
            mesh.SetVertices(vertices,0,vertexCount);mesh.SetUVs(0,uvs,0,vertexCount);mesh.SetColors(colors,0,vertexCount);
            mesh.SetTriangles(triangles,0,indexCount,0,false);mesh.RecalculateBounds();
        }
        public void Dispose(){Object.Destroy(root);Object.Destroy(mesh);if(material!=null)Object.Destroy(material);}
    }

    private readonly Layer[] effects=new Layer[EffectLayerCount];
    private readonly Layer[] blocks=new Layer[BreakLayerCount];
    private readonly Texture[] blockTextures=new Texture[BreakLayerCount];
    private Texture2D atlas;
    private World world;
    private int nextBlockLayer;
    private float tickAccumulator,displayTime,nextFireSound,nextLavaSound,nextFlowSound;
    private AudioClip fizz,splash,flow;
    private bool ownsFizz,ownsSplash;
    private readonly System.Random cosmetic=new System.Random();

    private void Awake()
    {
        var template=Resources.Load<Material>("FluidParticleMaterial");var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");atlas=assets==null?null:assets.particleAtlas;
        for(int i=0;i<EffectLayerCount;i++)
        {
            Texture image=atlas!=null?atlas:assets==null?null:i==0||i==4||i==5?assets.smokeParticle:i==1?assets.splashParticle:i==2?assets.lavaParticle:assets.bubbleParticle;
            effects[i]=new Layer(transform,"MinecraftParticles"+i,template,image,256);
        }
        for(int i=0;i<BreakLayerCount;i++)blocks[i]=new Layer(transform,"MinecraftBlockParticles"+i,template,null,192);
        var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;world=player==null?World.Instance:player.CurrentWorld;
        fizz=assets!=null&&assets.fluidFizz!=null?assets.fluidFizz:GameAudio.Find("random/fizz");splash=assets!=null&&assets.fluidSplash!=null?assets.fluidSplash:GameAudio.Find("liquid/splash");
        ownsFizz=fizz==null;ownsSplash=splash==null;if(ownsFizz)fizz=Noise("FluidFizz",false);if(ownsSplash)splash=Noise("FluidSplash",true);
        flow=assets!=null&&assets.fluidFlow!=null?assets.fluidFlow:GameAudio.Find("liquid/water");if(flow==null)flow=splash;
    }
    private AudioClip Noise(string name,bool low)
    {
        const int rate=22050;var samples=new float[rate/3];float previous=0;
        for(int i=0;i<samples.Length;i++){float noise=(float)(cosmetic.NextDouble()*2-1);previous=low?previous*.93f+noise*.07f:noise;samples[i]=previous*(1-i/(float)samples.Length)*.2f;}
        var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    private Rect AtlasCell(int x,int top,int width=8,int height=8)
    {
        if(atlas==null)return new Rect(0,0,1,1);
        return new Rect(x/(float)atlas.width,1-(top+height)/(float)atlas.height,width/(float)atlas.width,height/(float)atlas.height);
    }
    private Rect TextureChip(){return new Rect(RandomRange(0,.75f),RandomRange(0,.75f),.25f,.25f);}
    private int Lifetime(float numerator,float span,float floor)=>Mathf.Max(1,Mathf.FloorToInt(numerator/RandomRange(floor,floor+span)));
    private Particle NewParticle(Vector3 position,Vector3 velocity,float scale,int lifetime,Motion motion,Rect uv,Color color)
    {return new Particle{position=position,previous=position,velocity=velocity,scale=scale,originalScale=scale,maxAge=lifetime,motion=motion,uv=uv,color=color};}

    public void Emit(Vector3 position,byte kind)
    {
        int layer=kind==0?0:kind==1?1:kind==2?2:kind==4?4:3,count=kind==0||kind==1?8:1;
        for(int i=0;i<count;i++)
        {
            float x=RandomSigned(.5f),z=RandomSigned(.5f);Particle p;
            if(kind==0||kind==4)
            {
                float shade=RandomRange(.3f,.6f),scale=RandomRange(.5f,1)*(kind==4?1.5f:1);
                p=NewParticle(position+new Vector3(x,.7f,z),new Vector3(RandomSigned(.02f),RandomRange(.01f,.03f),RandomSigned(.02f)),scale,Lifetime(8,.8f,.2f),Motion.Smoke,AtlasCell(56,0),new Color(shade,shade,shade,1));
            }
            else if(kind==1){p=NewParticle(position+new Vector3(x,0,z),new Vector3(RandomSigned(.06f),RandomRange(.1f,.2f),RandomSigned(.06f)),RandomRange(.5f,1),Lifetime(8,.8f,.2f),Motion.Gravity,AtlasCell(40,8),Color.white);p.gravity=.04f;}
            else if(kind==2){p=NewParticle(position+new Vector3(x,.5f,z),new Vector3(0,RandomRange(.05f,.45f),0),RandomRange(.5f,1)*RandomRange(.2f,2.2f),Lifetime(16,.8f,.2f),Motion.Lava,AtlasCell(8,24),Color.white);p.gravity=0;}
            else{p=NewParticle(position+new Vector3(RandomSigned(.2f),RandomSigned(.1f),RandomSigned(.2f)),new Vector3(RandomSigned(.01f),RandomRange(.075f,.15f),RandomSigned(.01f)),RandomRange(.4f,.8f),Lifetime(8,.8f,.2f),Motion.Bubble,AtlasCell(0,16),Color.white);p.collide=false;}
            effects[layer].Add(p);
        }
        if(kind<2)GameAudio.PlayClip(kind==0?fizz:splash,position,kind==0?.5f:.35f);
        if(kind==2&&Time.unscaledTime>=nextLavaSound){nextLavaSound=Time.unscaledTime+GameAudio.Variation(1,3);GameAudio.Play("liquid/lavapop",position,.25f);}
        if(kind==4&&Time.unscaledTime>=nextFireSound){nextFireSound=Time.unscaledTime+GameAudio.Variation(3,5);GameAudio.Play("fire/fire",position,.2f);}
    }
    public void EmitMobDeath(Vector3 position,float radius,float height)
    {
        for(int i=0;i<20;i++)
        {
            var velocity=new Vector3(Gaussian()*.02f+RandomSigned(.05f),Gaussian()*.02f+RandomSigned(.05f),Gaussian()*.02f+RandomSigned(.05f));float shade=RandomRange(.7f,1);
            effects[5].Add(NewParticle(position+new Vector3(RandomSigned(radius),RandomRange(0,height),RandomSigned(radius)),velocity,RandomRange(.4f,1),Lifetime(4,.9f,.1f),Motion.Explosion,AtlasCell(56,0),new Color(shade,shade,shade,1)));
        }
    }
    private float RandomSigned(float radius)=>((float)cosmetic.NextDouble()*2-1)*radius;
    private float RandomRange(float minimum,float maximum)=>minimum+(float)cosmetic.NextDouble()*(maximum-minimum);
    private float Gaussian(){float a=Mathf.Max(.000001f,(float)cosmetic.NextDouble());return Mathf.Sqrt(-2*Mathf.Log(a))*Mathf.Cos((float)cosmetic.NextDouble()*Mathf.PI*2);}
    private Vector3 ClassicChipVelocity(Vector3 baseMotion)
    {
        var motion=baseMotion+new Vector3(RandomSigned(.4f),RandomSigned(.4f),RandomSigned(.4f));float length=motion.magnitude;
        if(length<.00001f)motion=Vector3.up;else motion*=1/length;
        return motion*(((float)cosmetic.NextDouble()+(float)cosmetic.NextDouble()+1)*.15f*.4f)+Vector3.up*.1f;
    }
    private int BlockLayer(Texture texture)
    {
        for(int i=0;i<BreakLayerCount;i++)if(blockTextures[i]==texture)return i;
        for(int i=0;i<BreakLayerCount;i++)if(blocks[i].Count==0){blockTextures[i]=texture;blocks[i].SetTexture(texture);return i;}
        int index=nextBlockLayer++%BreakLayerCount;blocks[index].Clear();blockTextures[index]=texture;blocks[index].SetTexture(texture);return index;
    }
    private bool BlockParticle(World target,Vector3Int cell,BlockType type,Texture preferred,out Layer layer,out Color color)
    {
        layer=null;color=Color.white;if(target==null||type==BlockType.Air||type==BlockType.Nothing)return false;
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length)return false;
        var data=definitions[typeIndex];if(data==null)return false;var texture=preferred!=null?preferred:data.textureData.side!=null?data.textureData.side:data.textureData.top!=null?data.textureData.top:data.textureData.bottom;if(texture==null)return false;
        layer=blocks[BlockLayer(texture)];var sample=target.GetBlock(cell);int sky=sample==null?15:sample.GetSkyLight(),block=sample==null?0:sample.GetBlockLight();
        var light=LightmapLighting.Sample(sky,block,LightmapLighting.SunBrightness(SkyClock.Angle(target.CurrentWorldTicks)),target.gamma,target.dimension==WorldDimension.Nether);var tint=data.albedoTint;
        color=new Color(tint.r*light.r*.6f,tint.g*light.g*.6f,tint.b*light.b*.6f,tint.a);return true;
    }
    private Particle BlockChip(Vector3 position,Vector3 velocity,Color color,float scale)
    {return NewParticle(position,velocity,scale,Lifetime(4,.9f,.1f),Motion.Gravity,TextureChip(),color);}
    public void EmitBlockBreak(World target,Vector3Int cell,BlockType type)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];var texture=data.textureData.bottom!=null?data.textureData.bottom:data.textureData.side!=null?data.textureData.side:data.textureData.top;
        if(!BlockParticle(target,cell,type,texture,out var layer,out var color))return;var center=(Vector3)cell+Vector3.one*.5f;
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++){var position=(Vector3)cell+new Vector3((x+.5f)/4,(y+.5f)/4,(z+.5f)/4);layer.Add(BlockChip(position,ClassicChipVelocity(position-center),color,RandomRange(.3f,.6f)));}
    }
    public void EmitBlockHit(World target,Vector3Int cell,BlockType type,Vector3 hitPoint)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];var local=hitPoint-(Vector3)cell;float bottom=Mathf.Abs(local.y),top=Mathf.Abs(1-local.y);Texture texture=bottom<top?data.textureData.bottom:data.textureData.top;if(Mathf.Min(bottom,top)>.01f)texture=data.textureData.side;
        if(!BlockParticle(target,cell,type,texture,out var layer,out var color))return;
        var p=(Vector3)cell+new Vector3(RandomRange(.1f,.9f),RandomRange(.1f,.9f),RandomRange(.1f,.9f));float edge=bottom<top?0:1;
        if(Mathf.Min(bottom,top)<=.01f)p.y=cell.y+edge+(edge==0?-.1f:.1f);else{float left=Mathf.Abs(local.x),right=Mathf.Abs(1-local.x),back=Mathf.Abs(local.z),front=Mathf.Abs(1-local.z);float nearest=Mathf.Min(Mathf.Min(left,right),Mathf.Min(back,front));if(nearest==left)p.x=cell.x-.1f;else if(nearest==right)p.x=cell.x+1.1f;else if(nearest==back)p.z=cell.z-.1f;else p.z=cell.z+1.1f;}
        layer.Add(BlockChip(p,ClassicChipVelocity(Vector3.zero)*.2f,color,RandomRange(.18f,.36f)));
    }
    public void EmitSprintDust(World target,Vector3 position,Vector3 horizontalVelocity,Block block)
    {
        if(block==null)return;int typeIndex=(int)block.type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var texture=definitions[typeIndex].textureData.top!=null?definitions[typeIndex].textureData.top:definitions[typeIndex].textureData.side;
        if(!BlockParticle(target,block.globalWorldPosition,block.type,texture,out var layer,out var color))return;
        layer.Add(BlockChip(position+new Vector3(RandomSigned(.3f),.08f,RandomSigned(.3f)),new Vector3(-horizontalVelocity.x*.01f,.075f,-horizontalVelocity.z*.01f),color,RandomRange(.2f,.4f)));
    }
    private bool Solid(Vector3 position)
    {if(world==null)return false;var block=world.GetBlock(Vector3Int.FloorToInt(position));return block!=null&&World.FluidHasSolidMaterial(block.type);}
    private bool Move(Particle p)
    {
        bool grounded=false;Vector3 next=p.position;next.y+=p.velocity.y;if(!p.collide||!Solid(next))p.position.y=next.y;else{grounded=p.velocity.y<0;p.velocity.y=0;}
        next=p.position;next.x+=p.velocity.x;if(!p.collide||!Solid(next))p.position.x=next.x;else p.velocity.x=0;
        next=p.position;next.z+=p.velocity.z;if(!p.collide||!Solid(next))p.position.z=next.z;else p.velocity.z=0;return grounded;
    }
    private void TickParticle(Particle p)
    {
        p.previous=p.position;
        if(p.motion==Motion.Smoke){p.velocity.y+=.004f;p.scale=p.originalScale*(1-p.age/(float)p.maxAge*.5f);}else if(p.motion==Motion.Gravity)p.velocity.y-=.04f*p.gravity;else if(p.motion==Motion.Lava){p.scale=p.originalScale*(1-p.age/(float)p.maxAge);p.velocity.y-=.03f;}
        bool grounded=Move(p);float drag=p.motion==Motion.Explosion?.9f:p.motion==Motion.Smoke?.96f:p.motion==Motion.Bubble?.85f:p.motion==Motion.Lava?.999f:.98f;p.velocity*=drag;
        if(grounded){p.velocity.x*=.7f;p.velocity.z*=.7f;}if((p.motion==Motion.Smoke||p.motion==Motion.Explosion)&&atlas!=null)p.uv=AtlasCell(Mathf.Max(0,7-p.age*8/p.maxAge)*8,0);
    }
    private void TickLayer(Layer layer)
    {
        for(int i=layer.particles.Count-1;i>=0;i--)
        {
            var p=layer.particles[i];if(p.age++>=p.maxAge){layer.particles.RemoveAt(i);continue;}
            if(p.motion==Motion.Bubble&&world!=null){var b=world.GetBlock(Vector3Int.FloorToInt(p.position));if(b==null||b.type!=BlockType.Water){layer.particles.RemoveAt(i);continue;}}TickParticle(p);
            if(p.motion==Motion.Lava&&cosmetic.NextDouble()>p.age/(double)p.maxAge)
            {float shade=RandomRange(.3f,.6f);effects[0].Add(NewParticle(p.position,Vector3.zero,RandomRange(.5f,1),Lifetime(8,.8f,.2f),Motion.Smoke,AtlasCell(56,0),new Color(shade,shade,shade,1)));}
        }
    }
    private Camera ActiveCamera()
    {var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;return player!=null&&player.cam!=null?player.cam.GetComponent<Camera>():Camera.main;}
    private void Update()
    {
        var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;if(player!=null&&player.CurrentWorld!=null)world=player.CurrentWorld;
        tickAccumulator+=Time.deltaTime;int steps=0;while(tickAccumulator>=TickLength&&steps<4){tickAccumulator-=TickLength;steps++;for(int i=0;i<effects.Length;i++)TickLayer(effects[i]);for(int i=0;i<blocks.Length;i++)TickLayer(blocks[i]);}if(steps==4&&tickAccumulator>=TickLength)tickAccumulator=0;
        displayTime+=Time.deltaTime;if(displayTime<.05f||player==null||player.CurrentWorld==null)return;displayTime=0;
        for(int i=0;i<128;i++)
        {
            var pos=Vector3Int.FloorToInt(player.transform.position)+new Vector3Int(cosmetic.Next(-8,9),cosmetic.Next(-4,5),cosmetic.Next(-8,9));var b=player.CurrentWorld.GetBlock(pos);if(b==null)continue;
            if(b.type==BlockType.Lava&&cosmetic.Next(100)==0){var above=player.CurrentWorld.GetBlock(pos+Vector3Int.up);if(above!=null&&above.type==BlockType.Air)Emit((Vector3)pos+Vector3.one*.5f,2);}else if(b.type==BlockType.Fire)Emit((Vector3)pos+Vector3.one*.5f,4);
            else if(b.type==BlockType.Water&&b.waterLevel>0&&b.waterLevel<8&&cosmetic.Next(64)==0&&Time.unscaledTime>=nextFlowSound){nextFlowSound=Time.unscaledTime+GameAudio.Variation(1,2);GameAudio.PlayClip(flow,pos,.12f);}
        }
    }
    private void LateUpdate()
    {var camera=ActiveCamera();float interpolation=Mathf.Clamp01(tickAccumulator/TickLength);for(int i=0;i<effects.Length;i++)effects[i].Draw(camera,interpolation);for(int i=0;i<blocks.Length;i++)blocks[i].Draw(camera,interpolation);}
    private void OnDestroy()
    {foreach(var layer in effects)if(layer!=null)layer.Dispose();foreach(var layer in blocks)if(layer!=null)layer.Dispose();if(fizz!=null&&ownsFizz)Destroy(fizz);if(splash!=null&&ownsSplash)Destroy(splash);}
}
