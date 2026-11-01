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
    private int lastFuse=-1;
    private float fuseSampleAt;
    public void Initialize()
    {
        var template=Resources.Load<Material>("Materials/DroppedBlockMaterial");
        if(template==null)return;
        mesh=ItemVisualMesh.Build(BlockType.TNT);material=new Material(template);
        var hull=new GameObject("TNT hull");hull.AddComponent<MeshFilter>();hull.AddComponent<MeshRenderer>();hull.transform.SetParent(transform,false);
        hull.transform.localPosition=new Vector3(-.5f,-.5f,-.5f);
        hull.GetComponent<MeshFilter>().sharedMesh=mesh;
        rendererComponent=hull.GetComponent<MeshRenderer>();rendererComponent.sharedMaterial=material;
    }
    public void Step(World world,PrimedTntState item)
    {
        var center=item.position+Vector3.up*.49f;
        if(!initialized){transform.position=center;initialized=true;}
        transform.position=motion.Step(center,Time.deltaTime,.1f);
        if(rendererComponent!=null)
        {
            if(lastFuse!=item.fuse){lastFuse=item.fuse;fuseSampleAt=Time.unscaledTime;}
            float partial=Mathf.Clamp01((Time.unscaledTime-fuseSampleAt)*20);
            transform.localScale=Vector3.one*TntRules.Swell(item.fuse-partial);
            properties.SetFloat("_FuseFlash",TntRules.Flash(item.fuse,partial));
            var block=world.GetBlock(Vector3Int.FloorToInt(item.position+Vector3.up*.5f));
            properties.SetVector("_LightLevels",new Vector4(block.GetSkyLight(),block.GetBlockLight(),1,0));
            rendererComponent.SetPropertyBlock(properties);
        }
        if(Time.unscaledTime>=smokeAt&&item.fuse>0)
        {smokeAt=Time.unscaledTime+.05f;world.PlayTntSmoke(item.position+Vector3.up*.99f);}
    }
    private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
}
