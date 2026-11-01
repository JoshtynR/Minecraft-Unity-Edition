using UnityEngine;

public partial class ChunkData
{
    // Only village chunks allocate the 4 KiB bit mask. It protects structure
    // blocks/air from later Beta population, including resumed edge populations.
    // Gameplay changes are intentionally not restricted by this mask.
    public byte[] betaVillageProtection;
    public void ApplyVillageBlock(int betaLocalX,int y,int betaLocalZ,BlockType type,byte state)
    {
        if(betaLocalX<0||betaLocalX>=16||betaLocalZ<0||betaLocalZ>=16||y<=0||y>=128)return;
        int x=15-betaLocalX,z=betaLocalZ;
        var pos=new Vector3Int(x,y,z);SetBlock(pos,type);
        var cell=GetBlock(pos);cell.waterLevel=state; // shared packed metadata, not just fluids
        if(betaVillageProtection==null)betaVillageProtection=new byte[4096];
        int index=x+z*16+y*256;betaVillageProtection[index>>3]|=(byte)(1<<(index&7));
    }
    public bool VillageProtected(int worldX,int y,int worldZ)
    {
        if(betaVillageProtection==null||betaVillageProtection.Length!=4096||y<0||y>=128)return false;
        int x=worldX-worldPos.x,z=worldZ-worldPos.z;
        if(x<0||x>=16||z<0||z>=16)return false;
        int index=x+z*16+y*256;return (betaVillageProtection[index>>3]&(1<<(index&7)))!=0;
    }
    public byte[] CaptureVillageProtection()=>betaVillageProtection==null?null:(byte[])betaVillageProtection.Clone();
    public void RestoreVillageProtection(byte[] saved)
    {betaVillageProtection=saved!=null&&saved.Length==4096?(byte[])saved.Clone():null;}
}
