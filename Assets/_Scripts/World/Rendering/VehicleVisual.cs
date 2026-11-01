using UnityEngine;

// Textured classic entity models; no prefab/scene wiring needed.
// Collision is handled by the voxel sweep, not these presentation objects.
public sealed class VehicleVisual : MonoBehaviour
{
    private Material material;
    private Mesh mesh;
    private MeshRenderer hullRenderer;
    private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    private Vector3Int lastLightCell=new Vector3Int(int.MinValue,int.MinValue,int.MinValue);
    private int lastSky=-1,lastBlock=-1;
    private float nextLightSample;
    private Vector3 target;
    private float yaw;
    private float rock,pitch;
    private World world;
    private bool boat;
    private Vector3 velocity;
    private float wakeClock;
    public void Initialize(VehicleState state,World owner=null)
    {
        world=owner;boat=state.kind==VehicleKind.Boat;
        var template=Resources.Load<Material>("Materials/CowMaterial");
        material=template!=null?new Material(template):new Material(Shader.Find("Voxel/Mob"));
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        var skin=assets==null?null:state.kind==VehicleKind.Boat?assets.boatSkin:assets.minecartSkin;
        if(material.HasProperty("_MainTex"))material.mainTexture=skin!=null?skin:Texture2D.whiteTexture;
        var tint=skin!=null?new Color(1,1,1):state.kind==VehicleKind.Boat?new Color(.48f,.3f,.13f):new Color(.55f,.55f,.57f);
        if(material.HasProperty("_Tint"))material.SetColor("_Tint",tint);else material.color=tint;
        if(material.HasProperty("_Unlit"))material.SetFloat("_Unlit",0);
        var data=VehicleModelGeometry.Build(state.kind);
        mesh=new Mesh{name=state.kind+" classic hull"};
        mesh.SetVertices(data.vertices);mesh.SetUVs(0,data.uv);mesh.SetTriangles(data.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        hullRenderer=gameObject.AddComponent<MeshRenderer>();hullRenderer.sharedMaterial=material;
        target=state.position;yaw=state.yaw;transform.SetPositionAndRotation(target,Quaternion.Euler(0,yaw,0));
        UpdateLighting();
    }
    public void SetTarget(VehicleState state){target=state.position;velocity=state.velocity;yaw=state.yaw;rock=state.kind==VehicleKind.Boat?VehiclePhysics.BoatRock(state):VehiclePhysics.CartRock(state);pitch=world!=null?world.VehiclePitch(state):0;}
    private void LateUpdate()
    {
        float blend=1-Mathf.Exp(-20*Time.deltaTime);
        transform.position=Vector3.Lerp(transform.position,target,blend);
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(pitch,yaw,rock),blend);
        UpdateLighting();
        wakeClock=Mathf.Min(wakeClock+Time.deltaTime,.1f);
        while(wakeClock>=VehiclePhysics.Step)
        {
            wakeClock-=VehiclePhysics.Step;
            if(boat&&world!=null&&velocity.x*velocity.x+velocity.z*velocity.z>9)
                world.PlayBoatWake(target,yaw,velocity);
        }
    }
    private void UpdateLighting()
    {
        if(world==null||hullRenderer==null)return;
        // Follow the rendered hull rather than the next network snapshot. A
        // periodic refresh also catches torch edits while a vehicle is parked.
        var cell=Vector3Int.FloorToInt(transform.position+Vector3.up*.3f);
        if(cell==lastLightCell&&Time.unscaledTime<nextLightSample)return;
        lastLightCell=cell;nextLightSample=Time.unscaledTime+.2f;
        var sample=world.GetBlock(cell);
        if(sample==null||sample.type==BlockType.Nothing)return;
        int sky=sample.GetSkyLight(),block=sample.GetBlockLight();
        if(sky==lastSky&&block==lastBlock)return;
        lastSky=sky;lastBlock=block;
        properties.SetVector("_LightLevels",new Vector4(sky,block,1,0));
        hullRenderer.SetPropertyBlock(properties);
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
