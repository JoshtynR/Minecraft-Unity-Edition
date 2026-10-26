using UnityEngine;

public sealed class DroppedItemVisual
{
    public readonly GameObject root;
    private readonly Transform[] copies=new Transform[4];
    private readonly MeshRenderer[] renderers=new MeshRenderer[4];
    private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
    private readonly bool flat;
    private bool placed,collecting;
    private float collectionTime;
    private Vector3 collectionStart,collectionTarget;
    private int lastAge=-1;
    private int lastCopies=-1,lastSky=-1,lastBlock=-1;
    private float ageTime;
    public DroppedItemVisual(Transform parent,InventoryStack stack,Mesh mesh,Material material,bool flat)
    {
        this.flat=flat;root=new GameObject("Dropped "+stack.type);root.transform.SetParent(parent,false);
        var random=new System.Random(187);
        for(int i=0;i<4;i++)
        {
            var go=new GameObject("Copy "+i,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);
            copies[i]=go.transform;
            Vector3 offset=i==0?Vector3.zero:new Vector3((float)random.NextDouble()*2-1,(float)random.NextDouble()*2-1,(float)random.NextDouble()*2-1)*(flat?.15f:.2f);
            copies[i].localPosition=offset+(flat?Vector3.zero:new Vector3(-.125f,0,-.125f));
            copies[i].localScale=Vector3.one*(flat?.5f:.25f);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            renderers[i]=go.GetComponent<MeshRenderer>();renderers[i].sharedMaterial=material;
            renderers[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
    public void Update(DroppedItemState state,World world,Camera camera)
    {
        if(lastAge!=state.age){lastAge=state.age;ageTime=Time.unscaledTime;}
        float age=state.age+(Time.unscaledTime-ageTime)*20;
        Vector3 destination=state.position+Vector3.up*DroppedItemRules.Bob(age,state.hover);
        root.transform.position=placed?Vector3.Lerp(root.transform.position,destination,Mathf.Clamp01(Time.deltaTime*20)):destination;
        placed=true;
        root.transform.rotation=Quaternion.Euler(0,flat&&camera!=null?camera.transform.eulerAngles.y:DroppedItemRules.Rotation(age,state.hover),0);
        int visible=DroppedItemRules.Copies(state.stack.count);
        var block=world.GetBlock(Vector3Int.FloorToInt(state.position+Vector3.up*.25f));
        int sky=block==null?15:block.GetSkyLight(),blockLight=block==null?0:block.GetBlockLight();
        bool lightingChanged=sky!=lastSky||blockLight!=lastBlock;
        if(lightingChanged){lastSky=sky;lastBlock=blockLight;properties.SetVector("_LightLevels",new Vector4(sky,blockLight,1,0));}
        for(int i=0;i<4;i++)
        {
            if(lastCopies!=visible)copies[i].gameObject.SetActive(i<visible);
            if(lightingChanged&&i<visible)renderers[i].SetPropertyBlock(properties);
        }
        lastCopies=visible;
    }
    public void BeginPickup(Vector3 target)
    {collecting=true;collectionTime=0;collectionStart=root.transform.position;collectionTarget=target;}
    public bool AnimatePickup()
    {
        if(!collecting)return false;
        collectionTime+=Time.deltaTime;float t=Mathf.Clamp01(collectionTime/.15f);
        root.transform.position=Vector3.Lerp(collectionStart,collectionTarget,t*t);
        return t<1;
    }
    public void Dispose(){Object.Destroy(root);}
}
