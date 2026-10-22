using UnityEngine;

// Local camera visuals only. The world clock is shared by the server and saved
// with metadata; cosmetic cloud generation never consumes the terrain RNG.
[RequireComponent(typeof(Camera))]
public sealed class OverworldAtmosphere : MonoBehaviour
{
    public Player player;
    private Camera view;
    private GameObject root,sun,moon,clouds;
    private Mesh quad,cloudQuad;
    private Material sunMaterial,moonMaterial,cloudMaterial;
    private Texture2D ownedSun,ownedMoon,ownedClouds;
    private Vector4 previousFog,previousRange;
    private bool fogApplied;
    private void Awake(){view=GetComponent<Camera>();}
    private static Mesh Quad(bool horizontal)
    {
        var mesh=new Mesh{name="Owned sky quad"};
        mesh.vertices=horizontal?new[]{new Vector3(-1,0,-1),new Vector3(-1,0,1),new Vector3(1,0,1),new Vector3(1,0,-1)}:
            new[]{new Vector3(-1,-1,0),new Vector3(-1,1,0),new Vector3(1,1,0),new Vector3(1,-1,0)};
        mesh.uv=new[]{new Vector2(0,0),new Vector2(0,1),new Vector2(1,1),new Vector2(1,0)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
    }
    private GameObject Surface(string name,Material material,Mesh mesh)
    {
        var go=new GameObject(name);go.transform.SetParent(root.transform,false);
        // The first-person and UI cameras exclude the normal world layer.
        go.layer=0;go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
    }
    private Texture2D Fallback(int kind)
    {
        int size=kind==2?32:16;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
        texture.name="Generated sky fallback";texture.filterMode=FilterMode.Point;
        texture.wrapMode=kind==2?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        var pixels=new Color[size*size];
        for(int z=0;z<size;z++)for(int x=0;x<size;x++)
        {
            if(kind==2)
            {
                int hash=unchecked((x/3*73428767)^(z/2*912931));
                pixels[z*size+x]=(hash&15)<6?new Color(1,1,1,.8f):Color.clear;
            }
            else
            {
                bool rim=x<2||z<2||x>=size-2||z>=size-2;
                pixels[z*size+x]=kind==0?(rim?new Color(1,.85f,.45f,.65f):Color.white):
                    ((x/3+z/4)%4==0?new Color(.55f,.55f,.6f,1):new Color(.85f,.87f,.9f,1));
            }
        }
        texture.SetPixels(pixels);texture.Apply(false,true);return texture;
    }
    private bool Build()
    {
        var template=Resources.Load<Material>("SkyMaterial");
        if(template==null){Debug.LogError("SkyMaterial resource is missing.");enabled=false;return false;}
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        sunMaterial=new Material(template);moonMaterial=new Material(template);cloudMaterial=new Material(template);
        sunMaterial.mainTexture=assets!=null&&assets.sunTexture!=null?assets.sunTexture:ownedSun=Fallback(0);
        moonMaterial.mainTexture=assets!=null&&assets.moonTexture!=null?assets.moonTexture:ownedMoon=Fallback(1);
        cloudMaterial.mainTexture=assets!=null&&assets.cloudTexture!=null?assets.cloudTexture:ownedClouds=Fallback(2);
        sunMaterial.SetFloat("_BlackTransparent",1);
        moonMaterial.SetFloat("_BlackTransparent",1);
        cloudMaterial.renderQueue=2900;cloudMaterial.SetFloat("_FogOn",1);
        root=new GameObject("Overworld sky visuals");quad=Quad(false);cloudQuad=Quad(true);
        sun=Surface("Sun",sunMaterial,quad);moon=Surface("Moon",moonMaterial,quad);clouds=Surface("Clouds",cloudMaterial,cloudQuad);
        return true;
    }
    private void LateUpdate()
    {
        var world=player!=null?player.CurrentWorld:null;
        bool visible=world!=null&&world.IsWorldCreated&&world.dimension==WorldDimension.Overworld&&view.enabled;
        if(!visible){if(root!=null)root.SetActive(false);return;}
        if(root==null&&!Build())return;
        root.SetActive(true);
        double ticks=world.CurrentWorldTicks;float daylight=SkyClock.Daylight(ticks);
        view.clearFlags=CameraClearFlags.SolidColor;
        // Beta 1.7.3's clear sky is a saturated but not neon blue. Keep the
        // same palette for the camera clear color and the distance fog.
        view.backgroundColor=Color.Lerp(new Color(.015f,.02f,.045f),new Color(.50f,.70f,1f),daylight);
        float angle=SkyClock.Angle(ticks)*Mathf.Deg2Rad;
        var direction=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0);
        float radius=Mathf.Min(200,view.farClipPlane*.75f);
        sun.transform.position=transform.position+direction*radius;
        sun.transform.rotation=Quaternion.LookRotation(direction,Vector3.forward);
        sun.transform.localScale=Vector3.one*radius*.10f;sun.SetActive(direction.y>-.1f);
        moon.transform.position=transform.position-direction*radius;
        moon.transform.rotation=Quaternion.LookRotation(-direction,Vector3.forward);
        moon.transform.localScale=Vector3.one*radius*.075f;moon.SetActive(direction.y<.1f);
        var moonTexture=moonMaterial.mainTexture;
        if(moonTexture!=null&&moonTexture.width==moonTexture.height*2)
        {
            int phase=(int)(System.Math.Floor(ticks/24000)%8+8)%8;
            moonMaterial.mainTextureScale=new Vector2(.25f,.5f);
            moonMaterial.mainTextureOffset=new Vector2((phase%4)*.25f,phase<4?.5f:0);
        }
        clouds.SetActive(world.showClouds);
        // EntityRenderer's classic cloud layer is a large, horizontal tiled
        // sheet at Y=128. The texture supplies the pixel-stepped silhouettes;
        // only its world offset moves over time.
        float drift=(float)((ticks*.03)%2048.0);
        clouds.transform.position=new Vector3(transform.position.x+drift,128,transform.position.z);
        clouds.transform.localScale=new Vector3(1024,1,1024);
        float tileSize=Mathf.Max(256,cloudMaterial.mainTexture.width*8);
        cloudMaterial.mainTextureScale=new Vector2(2048/tileSize,2048/tileSize);
        cloudMaterial.mainTextureOffset=new Vector2(transform.position.x/tileSize,transform.position.z/tileSize);
        cloudMaterial.SetColor("_Tint",Color.Lerp(new Color(.15f,.18f,.25f,1),Color.white,daylight));
    }
    private void OnPreCull()
    {
        RestoreFog();var world=player!=null?player.CurrentWorld:null;
        previousFog=Shader.GetGlobalVector("_WorldFogColor");previousRange=Shader.GetGlobalVector("_WorldFogRange");
        fogApplied=true;
        bool active=world!=null&&world.dimension==WorldDimension.Overworld&&world.overworldFog;
        if(!active){Shader.SetGlobalVector("_WorldFogRange",Vector4.zero);return;}
        var color=Color.Lerp(new Color(.025f,.03f,.06f),new Color(.50f,.70f,.96f),SkyClock.Daylight(world.CurrentWorldTicks));
        float end=Mathf.Max(16,Mathf.Min(view.farClipPlane*.9f,(world.renderDistance-.5f)*world.chunkSize));
        Shader.SetGlobalVector("_WorldFogColor",new Vector4(color.r,color.g,color.b,1));
        // Beta's overworld haze is subtle; it should only soften the far edge,
        // not wash the playable terrain in blue-gray.
        Shader.SetGlobalVector("_WorldFogRange",new Vector4(end*.55f,end,.62f,0));
    }
    private void OnPostRender(){RestoreFog();}
    private void RestoreFog()
    {
        if(!fogApplied)return;fogApplied=false;
        Shader.SetGlobalVector("_WorldFogColor",previousFog);Shader.SetGlobalVector("_WorldFogRange",previousRange);
    }
    private void OnDisable(){RestoreFog();if(root!=null)root.SetActive(false);}
    private void OnDestroy()
    {
        RestoreFog();if(root!=null)Destroy(root);if(quad!=null)Destroy(quad);if(cloudQuad!=null)Destroy(cloudQuad);
        if(sunMaterial!=null)Destroy(sunMaterial);if(moonMaterial!=null)Destroy(moonMaterial);if(cloudMaterial!=null)Destroy(cloudMaterial);
        if(ownedSun!=null)Destroy(ownedSun);if(ownedMoon!=null)Destroy(ownedMoon);if(ownedClouds!=null)Destroy(ownedClouds);
    }
}
