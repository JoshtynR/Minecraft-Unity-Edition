using UnityEngine;

// Simple open hulls: deliberately code-defined, no prefab/scene wiring needed.
// Collision is handled by the voxel sweep, not these presentation objects.
public sealed class VehicleVisual : MonoBehaviour
{
    private Material material;
    private Vector3 target;
    private float yaw;
    public void Initialize(VehicleState state)
    {
        var template=Resources.Load<Material>("CowMaterial");
        material=template!=null?new Material(template):new Material(Shader.Find("Unlit/Color"));
        if(material.HasProperty("_MainTex"))material.mainTexture=Texture2D.whiteTexture;
        var tint=state.kind==VehicleKind.Boat?new Color(.48f,.3f,.13f):new Color(.55f,.55f,.57f);
        if(material.HasProperty("_Tint"))material.SetColor("_Tint",tint);else material.color=tint;
        if(material.HasProperty("_Unlit"))material.SetFloat("_Unlit",1);
        float width=state.kind==VehicleKind.Boat?1.4f:.98f,length=state.kind==VehicleKind.Boat?1.5f:.98f;
        Box(new Vector3(0,.08f,0),new Vector3(width,.16f,length));
        Box(new Vector3(-width/2+.06f,.35f,0),new Vector3(.12f,.45f,length));
        Box(new Vector3(width/2-.06f,.35f,0),new Vector3(.12f,.45f,length));
        Box(new Vector3(0,.35f,-length/2+.06f),new Vector3(width-.24f,.45f,.12f));
        Box(new Vector3(0,.35f,length/2-.06f),new Vector3(width-.24f,.45f,.12f));
        target=state.position;yaw=state.yaw;transform.SetPositionAndRotation(target,Quaternion.Euler(0,yaw,0));
    }
    private void Box(Vector3 position,Vector3 scale)
    {
        var part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name="Hull";part.transform.SetParent(transform,false);
        part.transform.localPosition=position;part.transform.localScale=scale;
        var collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
        part.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
    public void SetTarget(VehicleState state){target=state.position;yaw=state.yaw;}
    private void LateUpdate()
    {
        float blend=1-Mathf.Exp(-20*Time.deltaTime);
        transform.position=Vector3.Lerp(transform.position,target,blend);
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.Euler(0,yaw,0),blend);
    }
    private void OnDestroy(){if(material!=null)Destroy(material);}
}
