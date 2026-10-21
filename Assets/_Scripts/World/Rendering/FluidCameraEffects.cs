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
    private float previousStart,previousEnd;
    private FogMode previousMode;
    private CameraClearFlags previousClearFlags;
    private BlockType medium;
    private Texture2D deathGradient;
    private GUIStyle deathTitleStyle,deathTextStyle;
    private float deathShownAt=-1;
    private bool confirmTitle;
    private float deathGuiScale=1;
    private void Awake(){view=GetComponent<Camera>();assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");}
    private void OnPreCull()
    {
        Restore();if(player==null||player.CurrentWorld==null)return;
        medium=FluidWorldQueries.AtEye(player.CurrentWorld,transform.position);
        previousFog=Shader.GetGlobalVector("_FluidFog");previousBackground=view.backgroundColor;
        previousClearFlags=view.clearFlags;
        previousFogEnabled=RenderSettings.fog;previousFogColor=RenderSettings.fogColor;
        previousDensity=RenderSettings.fogDensity;previousMode=RenderSettings.fogMode;applied=true;
        previousStart=RenderSettings.fogStartDistance;previousEnd=RenderSettings.fogEndDistance;
        Vector4 fog=medium==BlockType.Water?new Vector4(.02f,.02f,.2f,.1f):medium==BlockType.Lava?new Vector4(.6f,.1f,0,2):Vector4.zero;
        if (fog.w<=0 && player.CurrentWorld.dimension==WorldDimension.Nether) fog=new Vector4(.2f,.03f,.03f,.025f);
        Shader.SetGlobalVector("_FluidFog",fog);
        if(fog.w<=0)
        {
            var world=player.CurrentWorld;
            if(world.overworldFog&&world.dimension==WorldDimension.Overworld)
            {
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
                RenderSettings.fogColor=Color.Lerp(new Color(.025f,.03f,.06f),new Color(.65f,.77f,.9f),SkyClock.Daylight(world.CurrentWorldTicks));
                RenderSettings.fogEndDistance=Mathf.Max(16,Mathf.Min(view.farClipPlane*.9f,(world.renderDistance-.5f)*world.chunkSize));
                RenderSettings.fogStartDistance=RenderSettings.fogEndDistance*.35f;
            }
            return;
        }
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
        RenderSettings.fogStartDistance=previousStart;RenderSettings.fogEndDistance=previousEnd;
    }
    private void OnGUI()
    {
        if(player!=null&&player.health<=0)
        {
            DrawDeathScreen();return;
        }
        deathShownAt=-1;confirmTitle=false;
        if(player==null||assets==null||Event.current.type!=EventType.Repaint)return;
        var old=GUI.color;int depth=GUI.depth;GUI.depth=100;
        if(player.PortalOverlay>0)
        {
            var data=BlockDataManager.blockTypeDataDictionary[(int)BlockType.NetherPortal].textureData;
            var texture=data.side!=null?data.side:data.top;
            if(texture!=null)
            {
                int rows=Mathf.Max(1,texture.height/texture.width);
                int frame=Mathf.FloorToInt(Time.time*20)%rows;
                GUI.color=new Color(1,1,1,.15f+player.PortalOverlay*.65f);
                GUI.DrawTextureWithTexCoords(new Rect(0,0,Screen.width,Screen.height),texture,new Rect(0,1-(frame+1f)/rows,1,1f/rows));
            }
        }
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

    private void DrawDeathScreen()
    {
        if(deathShownAt<0)deathShownAt=Time.unscaledTime;
        if(deathGradient==null)
        {
            deathGradient=new Texture2D(1,2,TextureFormat.RGBA32,false);
            deathGradient.SetPixels(new[]{new Color(128/255f,48/255f,48/255f,160/255f),new Color(80/255f,0,0,96/255f)});
            deathGradient.filterMode=FilterMode.Bilinear;deathGradient.wrapMode=TextureWrapMode.Clamp;deathGradient.Apply();
            deathTextStyle=new GUIStyle(GUI.skin.label){fontSize=8,alignment=TextAnchor.UpperCenter,richText=true,
                padding=new RectOffset(),margin=new RectOffset(),contentOffset=Vector2.zero};
            if(assets!=null&&assets.guiFont!=null)deathTextStyle.font=assets.guiFont;
            deathTitleStyle=new GUIStyle(deathTextStyle){fontSize=16};
        }
        int oldDepth=GUI.depth;var oldMatrix=GUI.matrix;var oldColor=GUI.color;
        try
        {
            GUI.depth=-100;GUI.color=Color.white;
            int scale=Mathf.Max(1,Mathf.Min(Screen.width/320,Screen.height/240));
            deathGuiScale=scale;
            float width=Screen.width/(float)scale,height=Screen.height/(float)scale;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            GUI.DrawTexture(new Rect(0,0,width,height),deathGradient);
            bool ready=Time.unscaledTime-deathShownAt>=1;
            if(confirmTitle)
            {
                DrawDeathText(new Rect(0,height/6+50,width,20),"Are you sure you want to quit?",deathTextStyle,Color.white);
                if(DeathButton(new Rect(width/2-155,height/6+96,150,20),"Title screen",ready))EscapeMenuManager.ExitToTitle();
                if(DeathButton(new Rect(width/2+5,height/6+96,150,20),"Respawn",ready))player.RequestFluidRespawn();
                return;
            }
            DrawDeathText(new Rect(0,60,width,20),"You died!",deathTitleStyle,Color.white);
            // The project does not yet award vanilla score points.
            DrawDeathText(new Rect(0,100,width,12),"Score: <color=#ffff55>0</color>",deathTextStyle,Color.white);
            if(player.fluidRespawnUnavailable)DrawDeathText(new Rect(0,115,width,12),"No safe loaded spawn. Try again shortly.",deathTextStyle,new Color(1,.4f,.4f,1));
            if(DeathButton(new Rect(width/2-100,height/4+72,200,20),"Respawn",ready))player.RequestFluidRespawn();
            if(DeathButton(new Rect(width/2-100,height/4+96,200,20),"Title screen",ready))
            {confirmTitle=true;deathShownAt=Time.unscaledTime;}
        }
        finally{GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.depth=oldDepth;}
    }
    private bool DeathButton(Rect rect,string text,bool enabled)
    {
        var mouse=Input.mousePosition;
        bool hover=enabled&&rect.Contains(new Vector2(mouse.x/deathGuiScale,(Screen.height-mouse.y)/deathGuiScale));
        int row=!enabled?46:hover?86:66;
        var widgets=assets!=null?assets.guiWidgets:null;
        if(widgets==null)widgets=Resources.Load<Texture2D>("Sprites/UI/widgets");
        if(widgets!=null)
        {
            // Native buttons preserve both 100px end caps when resized.
            var left=new Rect(rect.x,rect.y,rect.width/2,rect.height);
            var right=new Rect(rect.x+rect.width/2,rect.y,rect.width/2,rect.height);
            GUI.DrawTextureWithTexCoords(left,widgets,new Rect(0,1-(row+20)/256f,left.width/256f,20/256f));
            GUI.DrawTextureWithTexCoords(right,widgets,new Rect((200-right.width)/256f,1-(row+20)/256f,right.width/256f,20/256f));
        }
        else
        {
            var old=GUI.color;GUI.color=!enabled?new Color(.25f,.25f,.25f,1):hover?new Color(.45f,.45f,.6f,1):new Color(.4f,.4f,.4f,1);
            GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;
        }
        var label=new Rect(rect.x,rect.y+6,rect.width,8);
        DrawDeathText(label,text,deathTextStyle,!enabled?new Color(160/255f,160/255f,160/255f,1):hover?new Color(1,1,160/255f,1):new Color(224/255f,224/255f,224/255f,1));
        return GUI.Button(rect,"",GUIStyle.none)&&enabled;
    }
    private static void DrawDeathText(Rect rect,string text,GUIStyle style,Color color)
    {
        // Strip inline colors only for the shadow, otherwise the score draws
        // a second yellow number instead of a black lower-right pixel edge.
        bool rich=style.richText;var previous=style.normal.textColor;
        try
        {
            string plain=text.IndexOf('<')<0?text:System.Text.RegularExpressions.Regex.Replace(text,"<[^>]*>","");
            float offset=style.fontSize>=16?2:1;
            style.richText=false;style.normal.textColor=new Color(0,0,0,color.a);
            GUI.Label(new Rect(rect.x+offset,rect.y+offset,rect.width,rect.height),plain,style);
            style.richText=rich;style.normal.textColor=color;GUI.Label(rect,text,style);
        }
        finally{style.richText=rich;style.normal.textColor=previous;}
    }
    private void OnDestroy(){Restore();if(deathGradient!=null)Destroy(deathGradient);}
}
