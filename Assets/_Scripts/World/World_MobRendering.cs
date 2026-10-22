using UnityEngine;

public partial class World
{
    private Mesh mobHeartMesh,mobFireMesh;
    internal Mesh MobHeartMesh()
    {
        if(mobHeartMesh!=null)return mobHeartMesh;
        mobHeartMesh=new Mesh{name="Owned mob heart"};
        var points=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
        mobHeartMesh.vertices=points;mobHeartMesh.uv=new[]{new Vector2(52/256f,1-9/256f),new Vector2(52/256f,1),new Vector2(61/256f,1),new Vector2(61/256f,1-9/256f)};
        mobHeartMesh.triangles=new[]{0,1,2,0,2,3};mobHeartMesh.RecalculateNormals();mobHeartMesh.RecalculateBounds();return mobHeartMesh;
    }
    internal Mesh MobFireMesh()
    {
        if(mobFireMesh!=null)return mobFireMesh;
        mobFireMesh=ItemVisualMesh.Build(BlockType.Fire);
        var light=new Vector2[mobFireMesh.vertexCount];for(int i=0;i<light.Length;i++)light[i]=new Vector2(15,15);
        mobFireMesh.SetUVs(1,light);return mobFireMesh;
    }
    private void ResetMobEffects()
    {if(mobHeartMesh!=null)Destroy(mobHeartMesh);if(mobFireMesh!=null)Destroy(mobFireMesh);mobHeartMesh=mobFireMesh=null;}
}
