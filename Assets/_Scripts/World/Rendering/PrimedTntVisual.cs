using UnityEngine;

public sealed class PrimedTntVisual : MonoBehaviour
{
    private Mesh mesh;
    private Material material;
    private MeshRenderer rendererComponent;
    private MaterialPropertyBlock properties=new MaterialPropertyBlock();
    private EntityVisualMotion motion=new EntityVisualMotion();
    private bool initialized;
    private float smokeAt;
    public void Initialize()
    {
        var template=Resources.Load<Material>("Materials/DroppedBlockMaterial");
        if(template==null)return;
        mesh=ItemVisualMesh.Build(BlockType.TNT);material=new Material(template);
        var hull=new GameObject("TNT hull");hull.AddComponent<MeshFilter>();hull.AddComponent<MeshRenderer>();hull.transform.SetParent(transform,false);
        hull.transform.localPosition=new Vector3(-.5f,0,-.5f);
        hull.GetComponent<MeshFilter>().sharedMesh=mesh;
        rendererComponent=hull.GetComponent<MeshRenderer>();rendererComponent.sharedMaterial=material;
    }
    public void Step(World world,PrimedTntState item)
    {
        if(!initialized){transform.position=item.position;initialized=true;}
        transform.position=motion.Step(item.position,Time.deltaTime,.1f);
        if(rendererComponent!=null)
        {
            properties.SetFloat("_FuseFlash",item.fuse/5%2==0?1:0);
            var block=world.GetBlock(Vector3Int.FloorToInt(item.position+Vector3.up*.5f));
            properties.SetVector("_LightLevels",new Vector4(block.GetSkyLight(),block.GetBlockLight(),1,0));
            rendererComponent.SetPropertyBlock(properties);
        }
        if(Time.unscaledTime>=smokeAt&&item.fuse>0)
        {smokeAt=Time.unscaledTime+.05f;world.PlayTntSmoke(item.position+Vector3.up*.5f);}
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
