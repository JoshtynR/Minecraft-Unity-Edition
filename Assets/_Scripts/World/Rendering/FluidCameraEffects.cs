using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class FluidCameraEffects : MonoBehaviour
{
    public Player player;
    private PlayerHudAssets assets;
    private Camera view;
    private Vector4 previousFog;
    private Color previousBackground,previousFogColor;
    private bool previousFogEnabled,applied;
    private float previousDensity;
    private FogMode previousMode;
    private CameraClearFlags previousClearFlags;
    private BlockType medium;
    private void Awake(){view=GetComponent<Camera>();assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");}
    private void OnPreCull()
    {
        Restore();if(player==null||World.Instance==null)return;
        medium=FluidWorldQueries.AtEye(World.Instance,transform.position);
        previousFog=Shader.GetGlobalVector("_FluidFog");previousBackground=view.backgroundColor;
        previousClearFlags=view.clearFlags;
        previousFogEnabled=RenderSettings.fog;previousFogColor=RenderSettings.fogColor;
        previousDensity=RenderSettings.fogDensity;previousMode=RenderSettings.fogMode;applied=true;
        Vector4 fog=medium==BlockType.Water?new Vector4(.02f,.02f,.2f,.1f):medium==BlockType.Lava?new Vector4(.6f,.1f,0,2):Vector4.zero;
        Shader.SetGlobalVector("_FluidFog",fog);
        if(fog.w<=0)return;
        view.backgroundColor=new Color(fog.x,fog.y,fog.z,1);
        view.clearFlags=CameraClearFlags.SolidColor;
        RenderSettings.fog=true;RenderSettings.fogColor=view.backgroundColor;
        RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=fog.w;
    }
    private void OnPostRender(){Restore();}
    private void OnDisable(){Restore();}
    private void Restore()
    {
        if(!applied)return;
        Shader.SetGlobalVector("_FluidFog",previousFog);view.backgroundColor=previousBackground;
        view.clearFlags=previousClearFlags;
        RenderSettings.fog=previousFogEnabled;RenderSettings.fogColor=previousFogColor;
        RenderSettings.fogDensity=previousDensity;RenderSettings.fogMode=previousMode;applied=false;
    }
    private void OnGUI()
    {
        if(player!=null&&player.health<=0)
        {
            int previousDepth=GUI.depth;GUI.depth=-100;
            float x=Screen.width*.5f-160,y=Screen.height*.5f-70;
            GUI.Box(new Rect(x,y,320,140),"You died");
            GUI.Label(new Rect(x+20,y+35,280,35),player.fluidRespawnUnavailable?"No safe loaded spawn yet. Try again shortly.":"Respawn at your last safe position.");
            if(GUI.Button(new Rect(x+60,y+85,200,35),"Respawn"))player.RequestFluidRespawn();
            GUI.depth=previousDepth;return;
        }
        if(player==null||assets==null||Event.current.type!=EventType.Repaint)return;
        var old=GUI.color;int depth=GUI.depth;GUI.depth=100;
        if(medium==BlockType.Water&&assets.waterOverlay!=null)
        {
            GUI.color=new Color(1,1,1,.1f);var offset=player.transform.eulerAngles;
            GUI.DrawTextureWithTexCoords(new Rect(0,0,Screen.width,Screen.height),assets.waterOverlay,new Rect(-offset.y/64,offset.x/64,4,4));
        }
        if(player.burning&&assets.fireOverlay!=null&&!player.isFlying)
        {
            GUI.color=new Color(1,1,1,.9f);
            int rows=Mathf.Max(1,assets.fireOverlay.height/assets.fireOverlay.width);
            int frame=Mathf.FloorToInt(Time.time*20)%rows;var uv=new Rect(0,1-(frame+1f)/rows,1,1f/rows);
            GUI.DrawTextureWithTexCoords(new Rect(-Screen.width*.15f,Screen.height*.45f,Screen.width*.65f,Screen.height*.7f),assets.fireOverlay,uv);
            GUI.DrawTextureWithTexCoords(new Rect(Screen.width*.5f,Screen.height*.45f,Screen.width*.65f,Screen.height*.7f),assets.fireOverlay,uv);
        }
        GUI.color=old;GUI.depth=depth;
    }
}
