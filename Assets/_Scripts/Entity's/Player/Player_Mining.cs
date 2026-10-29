using Mirror;
using UnityEngine;

public partial class Player
{
    private bool localMining, serverMining;
    private Vector3Int localMinePosition, serverMinePosition;
    private BlockType localMineType, serverMineType;
    private Vector3 targetHitPoint, serverMineHitPoint;
    private float localMineProgress, localMineClock, serverMineProgress, serverMineClock;
    private float nextLocalMineTime;
    private float nextMineSoundTime;
    private float nextMineParticleTime;
    private double nextServerMineTime;
    private BlockBreakOverlay crackOverlay;
    private int localMineSlot,serverMineSlot;
    private InventoryStack localMineItem,serverMineItem;
    private static bool SameMiningItem(InventoryStack a,InventoryStack b)=>a.Empty&&b.Empty||!a.Empty&&!b.Empty&&a.Matches(b);

    private bool HeadInWater()
    {
        var block = world.GetBlock(transform.position + Vector3.up * eyeHeight);
        return block != null && block.type == BlockType.Water;
    }

    private void UpdateMining()
    {
        if (world == null || health <= 0 || inventory.isOpen || inventory.DropRequestPending || escapeMenuOpen || !Input.GetMouseButton(0) || Time.time < nextLocalMineTime)
        { CancelMining(); return; }
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, reach, LayerMask.GetMask("Default")) && hit.transform.GetComponentInParent<Player>() != null)
        { CancelMining(); return; }
        var block = TargetedBlock(reach, out _);
        if (block == null || HandMining.Hardness(block.type) < 0) { CancelMining(); return; }
        var position = block.globalWorldPosition;
        var held=inventory.HeldItem();
        if (!localMining || localMinePosition != position || localMineType != block.type || localMineSlot!=inventory.selectedSlotIndex || !SameMiningItem(localMineItem,held))
        {
            CancelMining();
            localMining = true; localMinePosition = position; localMineType = block.type;
            localMineSlot=inventory.selectedSlotIndex;localMineItem=held;
            localMineProgress = localMineClock = 0;
            nextMineParticleTime=Time.time;
            if (networkStarted && !isServer) CmdBeginMining(position, block.type, targetHitPoint,localMineSlot,inventory.DropInventorySnapshot(),knownItemWearRevision);
            else BeginServerMining(position, block.type, targetHitPoint);
        }
        localMineClock += Time.deltaTime;
        if(Time.time>=nextMineSoundTime)
        {
            nextMineSoundTime=Time.time+.25f;
            GameAudio.Play(BlockSoundRules.Step(localMineType),targetHitPoint,.25f,.5f);
        }
        if(Time.time>=nextMineParticleTime)
        {
            nextMineParticleTime=Time.time+.1f;
            world.PlayBlockHitParticles(localMinePosition,localMineType,targetHitPoint);
        }
        while (localMineClock >= 0.05f)
        {
            localMineClock -= 0.05f;
            localMineProgress = Mathf.Min(1, localMineProgress + ToolMining.DamagePerTick(localMineType, localMineItem.type, isGrounded, HeadInWater()));
        }
        if (localMineProgress > 0)
        {
            if (crackOverlay == null) crackOverlay = new BlockBreakOverlay();
            crackOverlay.Show(block, HandMining.CrackStage(localMineProgress));
        }
    }

    private void CancelMining()
    {
        if (localMining)
        {
            if (networkStarted && !isServer) CmdCancelMining();
            else serverMining = false;
        }
        localMining = false;
        localMineProgress = localMineClock = 0;
        if (crackOverlay != null) crackOverlay.Hide();
    }

    [Command] private void CmdBeginMining(Vector3Int position, BlockType type, Vector3 hitPoint,int slot,InventoryStack[] snapshot,int revision)
    {
        if(!AcceptsItemRevision(revision)){TargetRejectMining(connectionToClient);return;}
        if(slot<0||slot>=9||!ChestTransactions.Valid(snapshot,new InventoryStack()))return;
        inventory.ApplyDropInventory(snapshot);inventory.selectedSlotIndex=slot;
        BeginServerMining(position,type,hitPoint);
    }
    [TargetRpc] private void TargetRejectMining(NetworkConnection target)
    {localMining=false;localMineProgress=localMineClock=0;if(crackOverlay!=null)crackOverlay.Hide();}
    [Command] private void CmdCancelMining() { serverMining = false; }

    private bool InMiningReach(Vector3Int position)
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        var nearest = new Vector3(Mathf.Clamp(eye.x, position.x, position.x + 1), Mathf.Clamp(eye.y, position.y, position.y + 1), Mathf.Clamp(eye.z, position.z, position.z + 1));
        return (eye - nearest).sqrMagnitude <= reach * reach;
    }

    private bool MiningLineOfSight(Vector3Int target, Vector3 destination)
    {
        var eye = transform.position + Vector3.up * eyeHeight;
        float distance = (destination - eye).magnitude;
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 0.05f));
        for (int i = 1; i <= steps; i++)
        {
            var point = Vector3.Lerp(eye, destination, (float)i / steps);
            var block = world.GetBlock(point);
            if (block == null || !HandMining.IsTarget(block.type)) continue;
            if (block.globalWorldPosition == target) return true;
            var shape = block.blockShape.GetShape();
            var local = point - (Vector3)block.globalWorldPosition;
            if (local.x >= shape.xMin / 16f && local.x <= shape.xMax / 16f &&
                local.y >= shape.yMin / 16f && local.y <= shape.yMax / 16f &&
                local.z >= shape.zMin / 16f && local.z <= shape.zMax / 16f) return false;
        }
        return false;
    }

    private void BeginServerMining(Vector3Int position, BlockType type, Vector3 hitPoint)
    {
        serverMining = false;
        if (float.IsNaN(hitPoint.x) || float.IsNaN(hitPoint.y) || float.IsNaN(hitPoint.z) ||
            float.IsInfinity(hitPoint.x) || float.IsInfinity(hitPoint.y) || float.IsInfinity(hitPoint.z)) return;
        if (world == null || health <= 0 || NetworkTime.time < nextServerMineTime || !InMiningReach(position)) return;
        var block = world.GetBlock(position);
        if (block == null || block.type != type || HandMining.Hardness(type) < 0) return;
        if(!world.CanReplaceChest(position,BlockType.Air))return;
        var shape = block.blockShape.GetShape();
        var local = hitPoint - (Vector3)position;
        if (local.x < shape.xMin / 16f || local.x > shape.xMax / 16f ||
            local.y < shape.yMin / 16f || local.y > shape.yMax / 16f ||
            local.z < shape.zMin / 16f || local.z > shape.zMax / 16f ||
            !MiningLineOfSight(position, hitPoint)) return;
        serverMineSlot=inventory.selectedSlotIndex;serverMineItem=inventory.HeldItem();
        serverMineHitPoint = hitPoint;
        serverMining = true; serverMinePosition = position; serverMineType = type;
        serverMineProgress = serverMineClock = 0;
    }

    private void AdvanceMining(float dt)
    {
        if (!serverMining) return;
        var held=inventory.GetSlot(serverMineSlot);
        if(held==null||!SameMiningItem(serverMineItem,new InventoryStack(held.type,held.count,held.damage)))
        {serverMining=false;return;}
        if (world == null || !world.IsWorldCreated || health <= 0 || !InMiningReach(serverMinePosition) || !MiningLineOfSight(serverMinePosition, serverMineHitPoint)) { serverMining = false; return; }
        var block = world.GetBlock(serverMinePosition);
        if (block == null || block.type != serverMineType) { serverMining = false; return; }
        if(!world.CanReplaceChest(serverMinePosition,BlockType.Air)){serverMining=false;return;}
        serverMineClock += dt;
        while (serverMineClock + 0.000001f >= 0.05f && serverMining)
        {
            serverMineClock = Mathf.Max(0, serverMineClock - 0.05f);
            bool grounded = !networkStarted || isLocalPlayer ? isGrounded : serverGrounded;
            serverMineProgress += ToolMining.DamagePerTick(serverMineType, serverMineItem.type, grounded, HeadInWater());
            if (serverMineProgress < 1) continue;
            serverMining = false;
            nextServerMineTime = NetworkTime.time + 0.25;
            // SetBlock removes the paired half without emitting loot, so the
            // mined half must supply the pair's one drop, even at the top.
            var drop = world.MiningItemDrop(serverMineType,serverMineItem.type,serverMineItem.damage);
            var replacement = BlockType.Air;
            if (serverMineType == BlockType.Ice)
            {
                var below = world.GetBlock(serverMinePosition + Vector3Int.down);
                if (below != null && below.type != BlockType.Nothing && below.type != BlockType.Air) replacement = BlockType.Water;
            }
            world.SetBlock(serverMinePosition, replacement);
            GameAudio.BroadcastBlock(serverMineType,serverMinePosition,true,world);
            if (WorldServer.instance != null && isServer) WorldServer.instance.BroadcastBlock(world,serverMinePosition, replacement, 0);
            // Mining is a later survival addition to the Beta renderer: normal
            // harvested blocks use the classic 0.025 exhaustion cost.
            if (ToolMining.Harvestable(serverMineType,serverMineItem.type))
            { LoadHungerState(); hungerSimulation.AddExhaustion(0.025f); StoreHungerState(); }
            if(!drop.Empty)world.SpawnItemDrop(drop,(Vector3)serverMinePosition+Vector3.one*.5f);
            int wear=ItemDurability.MiningWear(serverMineItem.type,serverMineType);
            if (networkStarted)
            {
                inventory.ApplyItemWear(serverMineSlot,serverMineItem,wear);
                if(wear>0&&ItemDurability.Maximum(serverMineItem.type)>0)serverItemWearRevision++;
                if(isServer&&isLocalPlayer)ReceiveMiningCompleted(serverMineSlot,serverMineItem,wear,serverItemWearRevision);
                else TargetMiningCompleted(connectionToClient,serverMineSlot,serverMineItem,wear,serverItemWearRevision);
            }
            else ReceiveMiningCompleted(serverMineSlot,serverMineItem,wear,knownItemWearRevision+1);
        }
    }

    [TargetRpc] private void TargetMiningCompleted(NetworkConnection target,int slot,InventoryStack expected,int wear,int revision) { ReceiveMiningCompleted(slot,expected,wear,revision); }
    private void ReceiveMiningCompleted(int slot,InventoryStack expected,int wear,int revision)
    {
        if(revision>knownItemWearRevision)
        {
            if(!networkStarted||!isServer)inventory.ApplyCompletedMiningWear(slot,expected,wear);
            knownItemWearRevision=revision;
            if(!networkStarted)serverItemWearRevision=revision;
        }
        localMining = false; localMineProgress = localMineClock = 0;
        nextLocalMineTime = Time.time + 0.25f;
        if (crackOverlay != null) crackOverlay.Hide();
    }
    private void DisposeMining()
    {
        if (crackOverlay != null) crackOverlay.Dispose();
        crackOverlay = null;
    }
}
