using System.Collections.Generic;
using UnityEngine;

// Beta renders particles as hand-ticked entities. Unity's ParticleSystem has
// different billboard sizing, sheet animation, motion and lifetime semantics,
// so these are simulated at 20 Hz and drawn as batched camera-facing quads.
public sealed class FluidEffects : MonoBehaviour
{
    private const float TickLength=.05f;
    private const int EffectLayerCount=7,BreakLayerCount=8;
    private enum Motion : byte {Smoke,Explosion,Digging,Bubble,Lava,Rain,Portal}
    private sealed class Particle
    {
        public Vector3 position,previous,velocity,origin;
        public Color color;
        public Rect uv;
        public float scale,originalScale,gravity=1;
        public int age,maxAge;
        public Motion motion;
        public bool collide=true,fullBright;
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
        public void Draw(Camera camera,World world,float interpolation)
        {
            if(camera==null||particles.Count==0){mesh.Clear();return;}
            Vector3 right=camera.transform.right,up=camera.transform.up;
            for(int i=0;i<particles.Count;i++)
            {
                var p=particles[i];Vector3 center=Vector3.Lerp(p.previous,p.position,interpolation);float progress=(p.age+interpolation)/p.maxAge;
                float scale=p.originalScale;
                if(p.motion==Motion.Smoke)scale*=Mathf.Clamp01(progress*32);
                else if(p.motion==Motion.Lava)scale*=1-progress*progress;
                else if(p.motion==Motion.Portal)scale*=1-(1-progress)*(1-progress);
                float radius=.1f*scale;
                Vector3 x=right*radius,y=up*radius;int v=i*4;
                vertices[v]=center-x-y;vertices[v+1]=center-x+y;vertices[v+2]=center+x+y;vertices[v+3]=center+x-y;
                float u0=p.uv.xMin,u1=p.uv.xMax,v0=p.uv.yMin,v1=p.uv.yMax;
                uvs[v]=new Vector2(u0,v0);uvs[v+1]=new Vector2(u0,v1);uvs[v+2]=new Vector2(u1,v1);uvs[v+3]=new Vector2(u1,v0);
                Color light=Color.white;
                if(!p.fullBright&&world!=null)
                {
                    var sample=world.GetBlock(Vector3Int.FloorToInt(center));int sky=sample==null?15:sample.GetSkyLight(),block=sample==null?0:sample.GetBlockLight();
                    var sampled=LightmapLighting.Sample(sky,block,LightmapLighting.SunBrightness(SkyClock.Angle(world.CurrentWorldTicks)),world.gamma,world.dimension==WorldDimension.Nether);
                    light=new Color(sampled.r,sampled.g,sampled.b,1);
                }
                if(p.motion==Motion.Portal){float glow=p.age/(float)p.maxAge;glow*=glow;glow*=glow;light=Color.Lerp(light,Color.white,glow);}
                var color=new Color(p.color.r*light.r,p.color.g*light.g,p.color.b*light.b,p.color.a);
                colors[v]=colors[v+1]=colors[v+2]=colors[v+3]=color;
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
    private float tickAccumulator,displayTime;
    private AudioClip fizz,splash,flow;
    private bool ownsFizz,ownsSplash;
    private readonly System.Random cosmetic=new System.Random();

    private void Awake()
    {
        var template=Resources.Load<Material>("FluidParticleMaterial");var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");atlas=assets==null?null:assets.particleAtlas;
        for(int i=0;i<EffectLayerCount;i++)
        {
            Texture image=atlas!=null?atlas:assets==null?null:i==0||i==4||i==5||i==6?assets.smokeParticle:i==1?assets.splashParticle:i==2?assets.lavaParticle:assets.bubbleParticle;
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
    private Rect TextureChip(){return new Rect(RandomRange(0,.75f),RandomRange(0,.75f),.24975f,.24975f);}
    private int Lifetime(float numerator,float span,float floor)=>Mathf.Max(1,Mathf.FloorToInt(numerator/RandomRange(floor,floor+span)));
    private float BaseScale()=>RandomRange(.5f,1)*2;
    private Particle NewParticle(Vector3 position,Vector3 velocity,float scale,int lifetime,Motion motion,Rect uv,Color color)
    {return new Particle{position=position,previous=position,origin=position,velocity=velocity,scale=scale,originalScale=scale,maxAge=lifetime,motion=motion,uv=uv,color=color};}
    private Particle NewSmoke(Vector3 position,Vector3 input,float size=1)
    {
        float shade=RandomRange(0,.3f),scale=BaseScale()*.75f*size;
        return NewParticle(position,ClassicChipVelocity(Vector3.zero)*.1f+input,scale,Mathf.Max(1,Mathf.FloorToInt(Lifetime(8,.8f,.2f)*size)),Motion.Smoke,AtlasCell(56,0),new Color(shade,shade,shade,1));
    }
    private Particle NewExplosion(Vector3 position,Vector3 input)
    {
        float shade=RandomRange(.7f,1),scale=(float)cosmetic.NextDouble()*(float)cosmetic.NextDouble()*6+1;
        return NewParticle(position,input+new Vector3(RandomSigned(.05f),RandomSigned(.05f),RandomSigned(.05f)),scale,Lifetime(16,.8f,.2f)+2,Motion.Explosion,AtlasCell(56,0),new Color(shade,shade,shade,1));
    }
    private Particle NewBubble(Vector3 position,Vector3 input)
    {
        var velocity=input*.2f+new Vector3(RandomSigned(.02f),RandomSigned(.02f),RandomSigned(.02f));
        var p=NewParticle(position,velocity,BaseScale()*RandomRange(.2f,.8f),Lifetime(8,.8f,.2f),Motion.Bubble,AtlasCell(0,16),Color.white);p.collide=false;return p;
    }
    private Particle NewLava(Vector3 position)
    {
        var velocity=ClassicChipVelocity(Vector3.zero)*.8f;velocity.y=RandomRange(.05f,.45f);
        var p=NewParticle(position,velocity,BaseScale()*RandomRange(.2f,2.2f),Lifetime(16,.8f,.2f),Motion.Lava,AtlasCell(8,24),Color.white);p.fullBright=true;return p;
    }
    private Particle NewSplash(Vector3 position,Vector3 input)
    {
        var velocity=ClassicChipVelocity(Vector3.zero);velocity.x*=.3f;velocity.y=RandomRange(.1f,.3f);velocity.z*=.3f;
        if(input.y==0&&(input.x!=0||input.z!=0)){velocity=input;velocity.y=.1f;}
        int texture=20+cosmetic.Next(4);var p=NewParticle(position,velocity,BaseScale(),Lifetime(8,.8f,.2f),Motion.Rain,AtlasCell(texture%16*8,texture/16*8),Color.white);p.gravity=.04f;return p;
    }
    private Particle NewPortal(Vector3 position,Vector3 velocity)
    {
        float shade=RandomRange(.4f,1);var p=NewParticle(position,velocity,RandomRange(.5f,.7f),cosmetic.Next(10)+40,Motion.Portal,AtlasCell(cosmetic.Next(8)*8,0),new Color(shade*.9f,shade*.3f,shade,1));p.collide=false;return p;
    }

    public void Emit(Vector3 position,byte kind)
    {
        int layer=kind==0||kind==4?4:kind==1?1:kind==2?2:3,count=kind==0||kind==1?8:1;
        for(int i=0;i<count;i++)
        {
            float x=RandomSigned(.5f),z=RandomSigned(.5f);Particle p;
            if(kind==0)p=NewSmoke(position+new Vector3(x,.7f,z),Vector3.zero,2.5f);
            else if(kind==4)p=NewSmoke(position,Vector3.zero,2.5f);
            else if(kind==1)p=NewSplash(position+new Vector3(x,0,z),new Vector3(RandomSigned(.06f),0,RandomSigned(.06f)));
            else if(kind==2)p=NewLava(position+new Vector3(x,.5f,z));
            else p=NewBubble(position+new Vector3(RandomSigned(.2f),RandomSigned(.1f),RandomSigned(.2f)),Vector3.zero);
            effects[layer].Add(p);
        }
        if(kind==0)GameAudio.PlayClip(fizz,position,.5f,true,2.6f+(RandomRange(0,1)-RandomRange(0,1))*.8f);
        else if(kind==1)GameAudio.PlayClip(splash,position,.35f);
    }
    public void EmitMobDeath(Vector3 position,float radius,float height)
    {
        for(int i=0;i<20;i++)
        {
            var velocity=new Vector3(Gaussian()*.02f,Gaussian()*.02f,Gaussian()*.02f);
            effects[5].Add(NewExplosion(position+new Vector3(RandomSigned(radius),RandomRange(0,height),RandomSigned(radius)),velocity));
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
        layer=blocks[BlockLayer(texture)];var tint=type==BlockType.Grass?Color.white:data.albedoTint;
        color=new Color(tint.r*.6f,tint.g*.6f,tint.b*.6f,tint.a);return true;
    }
    private Particle BlockChip(Vector3 position,Vector3 velocity,Color color,float scale)
    {return NewParticle(position,velocity,scale,Lifetime(4,.9f,.1f),Motion.Digging,TextureChip(),color);}
    public void EmitBlockBreak(World target,Vector3Int cell,BlockType type)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];var texture=data.textureData.bottom!=null?data.textureData.bottom:data.textureData.side!=null?data.textureData.side:data.textureData.top;
        if(!BlockParticle(target,cell,type,texture,out var layer,out var color))return;var center=(Vector3)cell+Vector3.one*.5f;
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++){var position=(Vector3)cell+new Vector3((x+.5f)/4,(y+.5f)/4,(z+.5f)/4);layer.Add(BlockChip(position,ClassicChipVelocity(position-center),color,BaseScale()*.5f));}
    }
    public void EmitBlockHit(World target,Vector3Int cell,BlockType type,Vector3 hitPoint)
    {
        int typeIndex=(int)type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var data=definitions[typeIndex];var local=hitPoint-(Vector3)cell;float bottom=Mathf.Abs(local.y),top=Mathf.Abs(1-local.y);Texture texture=bottom<top?data.textureData.bottom:data.textureData.top;if(Mathf.Min(bottom,top)>.01f)texture=data.textureData.side;
        if(!BlockParticle(target,cell,type,texture,out var layer,out var color))return;
        var p=(Vector3)cell+new Vector3(RandomRange(.1f,.9f),RandomRange(.1f,.9f),RandomRange(.1f,.9f));float edge=bottom<top?0:1;
        if(Mathf.Min(bottom,top)<=.01f)p.y=cell.y+edge+(edge==0?-.1f:.1f);else{float left=Mathf.Abs(local.x),right=Mathf.Abs(1-local.x),back=Mathf.Abs(local.z),front=Mathf.Abs(1-local.z);float nearest=Mathf.Min(Mathf.Min(left,right),Mathf.Min(back,front));if(nearest==left)p.x=cell.x-.1f;else if(nearest==right)p.x=cell.x+1.1f;else if(nearest==back)p.z=cell.z-.1f;else p.z=cell.z+1.1f;}
        layer.Add(BlockChip(p,ClassicChipVelocity(Vector3.zero)*.2f,color,BaseScale()*.5f*.6f));
    }
    public void EmitSprintDust(World target,Vector3 position,Vector3 horizontalVelocity,Block block)
    {
        if(block==null)return;int typeIndex=(int)block.type;var definitions=BlockDataManager.blockTypeDataDictionary;if(definitions==null||typeIndex<0||typeIndex>=definitions.Length||definitions[typeIndex]==null)return;
        var texture=definitions[typeIndex].textureData.top!=null?definitions[typeIndex].textureData.top:definitions[typeIndex].textureData.side;
        if(!BlockParticle(target,block.globalWorldPosition,block.type,texture,out var layer,out var color))return;
        layer.Add(BlockChip(position+new Vector3(RandomSigned(.3f),.08f,RandomSigned(.3f)),new Vector3(-horizontalVelocity.x*.01f,.075f,-horizontalVelocity.z*.01f),color,BaseScale()*.5f));
    }
    private bool Solid(Vector3 position)
    {if(world==null)return false;var block=world.GetBlock(Vector3Int.FloorToInt(position));return block!=null&&World.FluidHasSolidMaterial(block.type);}
    private bool Move(Particle p,out bool verticalBlocked)
    {
        bool grounded=false;verticalBlocked=false;Vector3 next=p.position;next.y+=p.velocity.y;if(!p.collide||!Solid(next))p.position.y=next.y;else{verticalBlocked=true;grounded=p.velocity.y<0;p.velocity.y=0;}
        next=p.position;next.x+=p.velocity.x;if(!p.collide||!Solid(next))p.position.x=next.x;else p.velocity.x=0;
        next=p.position;next.z+=p.velocity.z;if(!p.collide||!Solid(next))p.position.z=next.z;else p.velocity.z=0;return grounded;
    }
    private bool TickParticle(Particle p)
    {
        p.previous=p.position;
        if(p.motion==Motion.Portal)
        {
            float age=p.age/(float)p.maxAge,curve=1+age-age*age*2;p.position=p.origin+p.velocity*curve;p.position.y+=1-age;
            return p.age++>=p.maxAge;
        }
        if(p.motion==Motion.Bubble)
        {
            p.velocity.y+=.002f;Move(p,out _);p.velocity*=.85f;
            if(world==null)return true;var block=world.GetBlock(Vector3Int.FloorToInt(p.position));return block==null||block.type!=BlockType.Water||p.age++>=p.maxAge;
        }
        if(p.motion==Motion.Rain)
        {
            p.velocity.y-=p.gravity;bool grounded=Move(p,out _);p.velocity*=.98f;bool dead=p.age++>=p.maxAge;
            if(grounded){if(cosmetic.NextDouble()<.5)dead=true;p.velocity.x*=.7f;p.velocity.z*=.7f;}
            if(world!=null){var block=world.GetBlock(Vector3Int.FloorToInt(p.position));if(block!=null&&(block.type==BlockType.Water||block.type==BlockType.Lava))dead=true;}return dead;
        }
        if(p.age++>=p.maxAge)return true;
        if((p.motion==Motion.Smoke||p.motion==Motion.Explosion)&&atlas!=null)p.uv=AtlasCell(Mathf.Max(0,7-p.age*8/p.maxAge)*8,0);
        if(p.motion==Motion.Smoke)
        {
            p.velocity.y+=.004f;Move(p,out _);if(p.position.y==p.previous.y){p.velocity.x*=1.1f;p.velocity.z*=1.1f;}p.velocity*=.96f;
        }
        else if(p.motion==Motion.Explosion){p.velocity.y+=.004f;bool grounded=Move(p,out _);p.velocity*=.9f;if(grounded){p.velocity.x*=.7f;p.velocity.z*=.7f;}}
        else
        {
            if(p.motion==Motion.Digging)p.velocity.y-=.04f*p.gravity;
            else if(p.motion==Motion.Lava)
            {
                if(cosmetic.NextDouble()>p.age/(double)p.maxAge){var smoke=NewSmoke(p.position,p.velocity);TickParticle(smoke);effects[0].Add(smoke);}p.velocity.y-=.03f;
            }
            bool grounded=Move(p,out _);p.velocity*=p.motion==Motion.Lava?.999f:.98f;if(grounded){p.velocity.x*=.7f;p.velocity.z*=.7f;}
        }
        return false;
    }
    private void TickLayer(Layer layer)
    {
        for(int i=layer.particles.Count-1;i>=0;i--)
        {
            if(TickParticle(layer.particles[i]))layer.particles.RemoveAt(i);
        }
    }
    private Camera ActiveCamera()
    {var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;return player!=null&&player.cam!=null?player.cam.GetComponent<Camera>():Camera.main;}
    private bool Flammable(Vector3Int position)
    {if(world==null)return false;var block=world.GetBlock(position);return block!=null&&BetaFireSimulation.Flammability(block.type)>0;}
    private void LargeSmoke(Vector3 position)=>effects[4].Add(NewSmoke(position,Vector3.zero,2.5f));
    private void EmitFire(Vector3Int cell)
    {
        if(cosmetic.Next(24)==0)GameAudio.Play("fire/fire",(Vector3)cell+Vector3.one*.5f,RandomRange(1,2),RandomRange(.3f,1));
        bool supported=Solid((Vector3)cell+new Vector3(.5f,-.01f,.5f))||Flammable(cell+Vector3Int.down);
        if(supported)
        {
            for(int i=0;i<3;i++)LargeSmoke((Vector3)cell+new Vector3(RandomRange(0,1),RandomRange(.5f,1),RandomRange(0,1)));
            return;
        }
        for(int i=0;i<2;i++)
        {
            if(Flammable(cell+Vector3Int.left))LargeSmoke((Vector3)cell+new Vector3(RandomRange(0,.1f),RandomRange(0,1),RandomRange(0,1)));
            if(Flammable(cell+Vector3Int.right))LargeSmoke((Vector3)cell+new Vector3(1-RandomRange(0,.1f),RandomRange(0,1),RandomRange(0,1)));
            if(Flammable(cell+new Vector3Int(0,0,-1)))LargeSmoke((Vector3)cell+new Vector3(RandomRange(0,1),RandomRange(0,1),RandomRange(0,.1f)));
            if(Flammable(cell+new Vector3Int(0,0,1)))LargeSmoke((Vector3)cell+new Vector3(RandomRange(0,1),RandomRange(0,1),1-RandomRange(0,.1f)));
            if(Flammable(cell+Vector3Int.up))LargeSmoke((Vector3)cell+new Vector3(RandomRange(0,1),1-RandomRange(0,.1f),RandomRange(0,1)));
        }
    }
    private void EmitPortal(Vector3Int cell,Block block)
    {
        if(cosmetic.Next(100)==0)GameAudio.Play("portal/portal",(Vector3)cell+Vector3.one*.5f,1,RandomRange(.8f,1.2f));
        byte state=SpecialBlockGeometry.State(block.type,block.blockShape);
        for(int i=0;i<4;i++)
        {
            var position=(Vector3)cell+new Vector3(RandomRange(0,1),RandomRange(0,1),RandomRange(0,1));
            var velocity=new Vector3(RandomSigned(.25f),RandomSigned(.25f),RandomSigned(.25f));float sign=cosmetic.Next(2)*2-1;
            if((state&1)==0){position.z=cell.z+.5f+.25f*sign;velocity.z=RandomRange(0,2)*sign;}
            else{position.x=cell.x+.5f+.25f*sign;velocity.x=RandomRange(0,2)*sign;}
            effects[6].Add(NewPortal(position,velocity));
        }
    }
    private void Update()
    {
        var player=GameManager.Instance==null?null:GameManager.Instance.localPlayer;if(player!=null&&player.CurrentWorld!=null)world=player.CurrentWorld;
        tickAccumulator+=Time.deltaTime;int steps=0;while(tickAccumulator>=TickLength&&steps<4){tickAccumulator-=TickLength;steps++;for(int i=0;i<effects.Length;i++)TickLayer(effects[i]);for(int i=0;i<blocks.Length;i++)TickLayer(blocks[i]);}if(steps==4&&tickAccumulator>=TickLength)tickAccumulator=0;
        displayTime+=Time.deltaTime;if(displayTime<.05f||player==null||player.CurrentWorld==null)return;displayTime=0;
        // World.randomDisplayUpdates uses 1000 triangular samples within 16
        // blocks of the view entity every client tick.
        for(int i=0;i<1000;i++)
        {
            var pos=Vector3Int.FloorToInt(player.transform.position)+new Vector3Int(cosmetic.Next(16)-cosmetic.Next(16),cosmetic.Next(16)-cosmetic.Next(16),cosmetic.Next(16)-cosmetic.Next(16));var b=player.CurrentWorld.GetBlock(pos);if(b==null)continue;
            if(b.type==BlockType.Lava&&cosmetic.Next(100)==0){var above=player.CurrentWorld.GetBlock(pos+Vector3Int.up);if(above!=null&&above.type==BlockType.Air)effects[2].Add(NewLava((Vector3)pos+new Vector3(RandomRange(0,1),1,RandomRange(0,1))));}
            else if(b.type==BlockType.Fire)EmitFire(pos);
            else if(b.type==BlockType.NetherPortal)EmitPortal(pos,b);
            else if(b.type==BlockType.Water&&b.waterLevel>0&&b.waterLevel<8&&cosmetic.Next(64)==0)GameAudio.PlayClip(flow,(Vector3)pos+Vector3.one*.5f,RandomRange(.75f,1),true,RandomRange(.5f,1.5f));
        }
    }
    private void LateUpdate()
    {var camera=ActiveCamera();float interpolation=Mathf.Clamp01(tickAccumulator/TickLength);for(int i=0;i<effects.Length;i++)effects[i].Draw(camera,world,interpolation);for(int i=0;i<blocks.Length;i++)blocks[i].Draw(camera,world,interpolation);}
    private void OnDestroy()
    {foreach(var layer in effects)if(layer!=null)layer.Dispose();foreach(var layer in blocks)if(layer!=null)layer.Dispose();if(fizz!=null&&ownsFizz)Destroy(fizz);if(splash!=null&&ownsSplash)Destroy(splash);}
}
