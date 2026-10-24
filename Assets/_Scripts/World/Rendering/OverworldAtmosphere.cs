using UnityEngine;
using System.Collections.Generic;

public static class CloudMeshGeometry
{
    private static void Quad(List<Vector3> vertices,List<Vector2> uvs,List<Color> colors,List<int> triangles,
        Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 uv,Color shade)
    {
        int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
        uvs.Add(uv);uvs.Add(uv);uvs.Add(uv);uvs.Add(uv);
        colors.Add(shade);colors.Add(shade);colors.Add(shade);colors.Add(shade);
        triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
    }
    public static Mesh Build(Texture2D texture,float cellSize=12,float thickness=4)
    {
        if(texture==null)throw new System.ArgumentNullException("texture");
        int width=texture.width,height=texture.height;var pixels=texture.GetPixels();
        var solid=new bool[width*height];
        for(int i=0;i<solid.Length;i++)solid[i]=pixels[i].a>.1f;
        System.Func<int,int,bool> filled=(x,z)=>solid[(z%height+height)%height*width+(x%width+width)%width];
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
        float ox=-width*cellSize*.5f,oz=-height*cellSize*.5f,bottom=-thickness*.5f,top=thickness*.5f;
        var topShade=Color.white;var sideShade=new Color(.8f,.8f,.8f,1);var bottomShade=new Color(.65f,.65f,.65f,1);
        for(int z=0;z<height;z++)for(int x=0;x<width;x++)
        {
            if(!filled(x,z))continue;
            float x0=ox+x*cellSize,x1=x0+cellSize,z0=oz+z*cellSize,z1=z0+cellSize;
            var sample=new Vector2((x+.5f)/width,(z+.5f)/height);
            Quad(vertices,uvs,colors,triangles,new Vector3(x0,top,z0),new Vector3(x0,top,z1),new Vector3(x1,top,z1),new Vector3(x1,top,z0),sample,topShade);
            Quad(vertices,uvs,colors,triangles,new Vector3(x1,bottom,z0),new Vector3(x1,bottom,z1),new Vector3(x0,bottom,z1),new Vector3(x0,bottom,z0),sample,bottomShade);
            if(!filled(x-1,z))Quad(vertices,uvs,colors,triangles,new Vector3(x0,bottom,z0),new Vector3(x0,bottom,z1),new Vector3(x0,top,z1),new Vector3(x0,top,z0),sample,sideShade);
            if(!filled(x+1,z))Quad(vertices,uvs,colors,triangles,new Vector3(x1,bottom,z1),new Vector3(x1,bottom,z0),new Vector3(x1,top,z0),new Vector3(x1,top,z1),sample,sideShade);
            if(!filled(x,z-1))Quad(vertices,uvs,colors,triangles,new Vector3(x1,bottom,z0),new Vector3(x0,bottom,z0),new Vector3(x0,top,z0),new Vector3(x1,top,z0),sample,sideShade);
            if(!filled(x,z+1))Quad(vertices,uvs,colors,triangles,new Vector3(x0,bottom,z1),new Vector3(x1,bottom,z1),new Vector3(x1,top,z1),new Vector3(x0,top,z1),sample,sideShade);
        }
        var mesh=new Mesh{name="Owned extruded cloud tile"};
        if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
}

public static class HorizonMeshGeometry
{
    public static Mesh Build(int segments=64)
    {
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
        for(int i=0;i<segments;i++)
        {
            float a0=i*Mathf.PI*2/segments,a1=(i+1)*Mathf.PI*2/segments;
            int first=vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(a0),-.14f,Mathf.Sin(a0)));vertices.Add(new Vector3(Mathf.Cos(a0),.34f,Mathf.Sin(a0)));
            vertices.Add(new Vector3(Mathf.Cos(a1),.34f,Mathf.Sin(a1)));vertices.Add(new Vector3(Mathf.Cos(a1),-.14f,Mathf.Sin(a1)));
            uvs.Add(new Vector2((float)i/segments,0));uvs.Add(new Vector2((float)i/segments,1));
            uvs.Add(new Vector2((float)(i+1)/segments,1));uvs.Add(new Vector2((float)(i+1)/segments,0));
            triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);triangles.Add(first);triangles.Add(first+2);triangles.Add(first+3);
        }
        var mesh=new Mesh{name="Owned sunset horizon band"};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
    }
}

// Local camera visuals only. The world clock is shared by the server and saved
// with metadata; cosmetic cloud generation never consumes the terrain RNG.
[RequireComponent(typeof(Camera))]
public sealed class OverworldAtmosphere : MonoBehaviour
{
    public Player player;
    private Camera view;
    private GameObject root,sun,moon,sunset,clouds;
    private readonly List<Transform> cloudTiles=new List<Transform>();
    private Mesh quad,cloudMesh,horizonMesh;
    private float cloudTileWidth,cloudTileDepth;
    private int cloudTileRadius;
    private Material sunMaterial,moonMaterial,sunsetMaterial,cloudMaterial;
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
        texture.SetPixels(pixels);texture.Apply(false,kind!=2);return texture;
    }
    private bool Build()
    {
        var template=Resources.Load<Material>("SkyMaterial");
        if(template==null){Debug.LogError("SkyMaterial resource is missing.");enabled=false;return false;}
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        sunMaterial=new Material(template);moonMaterial=new Material(template);sunsetMaterial=new Material(template);cloudMaterial=new Material(template);
        sunMaterial.mainTexture=assets!=null&&assets.sunTexture!=null?assets.sunTexture:ownedSun=Fallback(0);
        moonMaterial.mainTexture=assets!=null&&assets.moonTexture!=null?assets.moonTexture:ownedMoon=Fallback(1);
        cloudMaterial.mainTexture=assets!=null&&assets.cloudTexture!=null?assets.cloudTexture:ownedClouds=Fallback(2);
        var sunPixels=sunMaterial.mainTexture as Texture2D;if(sunPixels!=null){sunPixels.filterMode=FilterMode.Point;sunPixels.wrapMode=TextureWrapMode.Clamp;}
        var moonPixels=moonMaterial.mainTexture as Texture2D;if(moonPixels!=null){moonPixels.filterMode=FilterMode.Point;moonPixels.wrapMode=TextureWrapMode.Clamp;}
        // This sheet stores its glow as 1..40 RGB over a fully opaque black
        // background. Additive blending makes black contribute nothing while
        // preserving every dim, pixel-stepped yellow halo value.
        sunMaterial.SetFloat("_BlackTransparent",0);
        sunMaterial.SetFloat("_Additive",1);
        sunMaterial.SetFloat("_SrcBlend",1); // One
        sunMaterial.SetFloat("_DstBlend",1); // One
        moonMaterial.SetFloat("_BlackTransparent",1);
        moonMaterial.SetFloat("_BlackCutoff",.005f);
        sunsetMaterial.SetFloat("_HorizonBand",1);sunsetMaterial.renderQueue=1005;
        cloudMaterial.renderQueue=2900;cloudMaterial.SetFloat("_FogOn",1);cloudMaterial.SetFloat("_VertexColorOn",1);cloudMaterial.SetFloat("_ZWrite",1);
        root=new GameObject("Overworld sky visuals");quad=Quad(false);
        horizonMesh=HorizonMeshGeometry.Build();sunset=Surface("Sunset horizon",sunsetMaterial,horizonMesh);
        sun=Surface("Sun",sunMaterial,quad);moon=Surface("Moon",moonMaterial,quad);
        clouds=new GameObject("Clouds");clouds.layer=0;clouds.transform.SetParent(root.transform,false);
        var cloudTexture=(Texture2D)cloudMaterial.mainTexture;cloudMesh=CloudMeshGeometry.Build(cloudTexture);
        cloudTileWidth=cloudTexture.width*12;cloudTileDepth=cloudTexture.height*12;
        cloudTileRadius=Mathf.Min(4,Mathf.Max(2,(int)System.Math.Ceiling(Mathf.Min(view.farClipPlane,1024)/Mathf.Min(cloudTileWidth,cloudTileDepth))+1));
        for(int z=-cloudTileRadius;z<=cloudTileRadius;z++)for(int x=-cloudTileRadius;x<=cloudTileRadius;x++)
        {
            var tile=Surface("Cloud tile "+x+","+z,cloudMaterial,cloudMesh);tile.transform.SetParent(clouds.transform,false);cloudTiles.Add(tile.transform);
        }
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
        float horizonGlow=SkyClock.HorizonGlow(ticks);
        var clear=Color.Lerp(new Color(.015f,.02f,.045f),new Color(.50f,.70f,1f),daylight);
        view.backgroundColor=clear;
        float angle=SkyClock.Angle(ticks)*Mathf.Deg2Rad;
        var direction=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0);
        float radius=Mathf.Min(200,view.farClipPlane*.75f);
        sunset.transform.position=transform.position;sunset.transform.rotation=Quaternion.identity;
        sunset.transform.localScale=Vector3.one*radius*.98f;
        sunsetMaterial.SetColor("_Tint",new Color(1f,.28f,.08f,horizonGlow*.58f));
        sunsetMaterial.SetVector("_SunsetDirection",new Vector4(direction.x,0,direction.z,0));
        sunset.SetActive(horizonGlow>.001f);
        sun.transform.position=transform.position+direction*radius;
        sun.transform.rotation=Quaternion.LookRotation(direction,Vector3.forward);
        // Classic Minecraft renders these at half-sizes 30 and 20 on a
        // 100-unit celestial plane. Preserve those angular proportions.
        sun.transform.localScale=Vector3.one*radius*.30f;sun.SetActive(direction.y>-.1f);
        moon.transform.position=transform.position-direction*radius;
        moon.transform.rotation=Quaternion.LookRotation(-direction,Vector3.forward);
        moon.transform.localScale=Vector3.one*radius*.20f;moon.SetActive(direction.y<.1f);
        var moonTexture=moonMaterial.mainTexture;
        if(moonTexture!=null&&moonTexture.width==moonTexture.height*2)
        {
            int phase=(int)(System.Math.Floor(ticks/24000)%8+8)%8;
            moonMaterial.mainTextureScale=new Vector2(.25f,.5f);
            moonMaterial.mainTextureOffset=new Vector2((phase%4)*.25f,phase<4?.5f:0);
        }
        clouds.SetActive(world.showClouds);
        // Recycle a square of shared extruded tiles around the camera. The
        // lattice itself drifts, so crossing a sheet boundary is seamless and
        // never exposes the end of the cloud texture.
        float drift=(float)((ticks*.03)%cloudTileWidth);
        int centerX=(int)System.Math.Floor((transform.position.x-drift)/cloudTileWidth);
        int centerZ=(int)System.Math.Floor(transform.position.z/cloudTileDepth);
        int tileIndex=0;
        for(int z=-cloudTileRadius;z<=cloudTileRadius;z++)for(int x=-cloudTileRadius;x<=cloudTileRadius;x++)
            cloudTiles[tileIndex++].position=new Vector3((centerX+x)*cloudTileWidth+drift,128,(centerZ+z)*cloudTileDepth);
        cloudMaterial.SetColor("_Tint",Color.Lerp(new Color(.15f,.18f,.25f,1),Color.white,daylight));
    }
    private void OnPreCull()
    {
        RestoreFog();var world=player!=null?player.CurrentWorld:null;
        previousFog=Shader.GetGlobalVector("_WorldFogColor");previousRange=Shader.GetGlobalVector("_WorldFogRange");
        fogApplied=true;
        bool active=world!=null&&world.dimension==WorldDimension.Overworld&&world.overworldFog;
        if(!active){Shader.SetGlobalVector("_WorldFogRange",Vector4.zero);return;}
        double ticks=world.CurrentWorldTicks;
        var color=Color.Lerp(new Color(.025f,.03f,.06f),new Color(.50f,.70f,.96f),SkyClock.Daylight(ticks));
        color=Color.Lerp(color,new Color(.78f,.34f,.16f),SkyClock.HorizonGlow(ticks)*.62f);
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
        RestoreFog();if(root!=null)Destroy(root);if(quad!=null)Destroy(quad);if(cloudMesh!=null)Destroy(cloudMesh);if(horizonMesh!=null)Destroy(horizonMesh);
        if(sunMaterial!=null)Destroy(sunMaterial);if(moonMaterial!=null)Destroy(moonMaterial);if(sunsetMaterial!=null)Destroy(sunsetMaterial);if(cloudMaterial!=null)Destroy(cloudMaterial);
        if(ownedSun!=null)Destroy(ownedSun);if(ownedMoon!=null)Destroy(ownedMoon);if(ownedClouds!=null)Destroy(ownedClouds);
    }
}
