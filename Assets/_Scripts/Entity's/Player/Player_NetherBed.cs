using Mirror;
using UnityEngine;

public partial class Player
{
    private bool TryUseNetherBed(Block block)
    {
        if(dimension!=WorldDimension.Nether||block.type!=BlockType.Bed)return false;
        if(networkStarted&&!isServer)CmdUseNetherBed(block.globalWorldPosition);
        else ExplodeNetherBed(block.globalWorldPosition);
        return true;
    }
    [Command] private void CmdUseNetherBed(Vector3Int p){ExplodeNetherBed(p);}
    private void ExplodeNetherBed(Vector3Int position)
    {
        if(dimensionTransfer||dimension!=WorldDimension.Nether||health<=0||world==null||world.isSaving||!InMiningReach(position))return;
        if(world.GetBlock(position).type!=BlockType.Bed)return;
        world.ExplodeNetherBed(position);
    }
}
