using UnityEngine;

// Textured classic entity models; no prefab/scene wiring needed.
// Collision is handled by the voxel sweep, not these presentation objects.
public sealed class VehicleVisual : MonoBehaviour
{
    private Material material;
    private Mesh mesh;
    private Vector3 target;
    private float yaw;
    public void Initialize(VehicleState state)
    {
        var template=Resources.Load<Material>("Materials/CowMaterial");
        material=template!=null?new Material(template):new Material(Shader.Find("Unlit/Color"));
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        var skin=assets==null?null:state.kind==VehicleKind.Boat?assets.boatSkin:assets.minecartSkin;
        if(material.HasProperty("_MainTex"))material.mainTexture=skin!=null?skin:Texture2D.whiteTexture;
        var tint=skin!=null?new Color(1,1,1):state.kind==VehicleKind.Boat?new Color(.48f,.3f,.13f):new Color(.55f,.55f,.57f);
        if(material.HasProperty("_Tint"))material.SetColor("_Tint",tint);else material.color=tint;
        if(material.HasProperty("_Unlit"))material.SetFloat("_Unlit",1);
        var data=VehicleModelGeometry.Build(state.kind);
        mesh=new Mesh{name=state.kind+" classic hull"};
        mesh.SetVertices(data.vertices);mesh.SetUVs(0,data.uv);mesh.SetTriangles(data.triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        target=state.position;yaw=state.yaw;transform.SetPositionAndRotation(target,Quaternion.Euler(0,yaw,0));
    }
    public void SetTarget(VehicleState state){target=state.position;yaw=state.yaw;}
    private void LateUpdate()
    {
        float blend=1-Mathf.Exp(-20*Time.deltaTime);
        transform.position=Vector3.Lerp(transform.position,target,blend);
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,yaw,0),blend);
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
