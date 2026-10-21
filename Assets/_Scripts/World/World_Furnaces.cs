using Mirror;
using UnityEngine;

public partial class World
{
    public readonly FurnaceStorage furnaces = new FurnaceStorage();
    private double furnaceClock;
    public static bool IsFurnace(BlockType type) => type == BlockType.Furnace || type == BlockType.LitFurnace;
    public bool FurnaceAccessible(Vector3 eye, Vector3Int position)
    {
        var block = GetBlock(position);
        if (block == null || !IsFurnace(block.type)) return false;
        Vector3 end = (Vector3)position + Vector3.one * .5f;
        if ((end - eye).sqrMagnitude > 64) return false;
        int steps = Mathf.Max(1, Mathf.CeilToInt((end - eye).magnitude / .1f));
        for (int i = 1; i < steps; i++)
        {
            var p = Vector3Int.FloorToInt(Vector3.Lerp(eye, end, (float)i / steps));
            if (p == position) return true;
            var other = GetBlock(p);
            if (other == null || other.type == BlockType.Nothing || SpecialBlockGeometry.NormalCube(other.type)) return false;
        }
        return true;
    }
    private void UpdateFurnaces()
    {
        if (!Application.isPlaying || disabled || !IsWorldCreated || GenerationBusy || !DropAuthority || isSaving) return;
        furnaces.Load(StorageName); furnaceClock += Time.deltaTime;
        while (furnaceClock + .000001 >= .05)
        {
            furnaceClock = System.Math.Max(0, furnaceClock - .05);
            foreach (var entry in furnaces.Entries)
            {
                var block = GetBlock(entry.position);
                // Streaming pauses tile entities; do not process time spent unloaded.
                if (block == null || !IsFurnace(block.type)) continue;
                if (WorldDataHelper.GetChunk(this, WorldDataHelper.GetChunkPosition(this, entry.position)) == null) continue;
                BetaSmelting.Tick(entry.state);
                var type = entry.state.Burning ? BlockType.LitFurnace : BlockType.Furnace;
                if (block.type == type) continue;
                byte facing = SpecialBlockGeometry.State(block.type, block.blockShape);
                SetBlock(entry.position, type, facing);
                if (NetworkServer.active && WorldServer.instance != null) WorldServer.instance.BroadcastBlock(this,entry.position, type, facing);
            }
        }
    }
    private void RemoveFurnace(Vector3Int position)
    {
        if (!DropAuthority) return;
        foreach (var stack in furnaces.Remove(position))
            if (!stack.Empty) SpawnItemDrop(stack, (Vector3)position + Vector3.one * .5f);
    }
    private void TrackFurnace(Vector3Int position)
    { if (DropAuthority) furnaces.Track(StorageName, position); }
}

public partial class WorldServer
{
    public void ReceiveFurnaceRequest(NetworkConnectionToClient connection, FurnaceRequest request)
    {
        if (connection.identity == null || World.Instance == null) return;
        var player = connection.identity.GetComponent<Player>(); if (player == null || player.DimensionTransferInProgress) return;
        var world = player.CurrentWorld;
        if (request.action == 0 && !player.AcceptsItemRevision(request.itemWearRevision))
        { connection.Send(new FurnaceReply { error = "Inventory update pending; open the furnace again." }); return; }
        if (request.action == 0)
            foreach (int owner in world.chests.Owners()) if (owner == connection.connectionId)
            { connection.Send(new FurnaceReply { error = "Close the chest first." }); return; }
        if (world.isSaving && request.action != 2)
        { connection.Send(new FurnaceReply { error = "World is saving; try again in a moment." }); return; }
        bool accessible = player.health > 0 && world.FurnaceAccessible(player.transform.position + Vector3.up * 1.6f, request.position);
        var reply = world.furnaces.Process(world.StorageName, connection.connectionId, request, accessible);
        if (reply.accepted) player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory, reply.cursor);
        connection.Send(reply);
        if (reply.accepted && !reply.opened) world.SaveWorld();
    }
    public void CloseFurnaceForDisconnect(NetworkConnectionToClient connection)
    {
        foreach(var world in World.LoadedDimensions)
        {
            var reply=world.furnaces.Close(connection.connectionId);if(!reply.accepted)continue;
            if(connection.identity!=null)connection.identity.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory,reply.cursor);
            world.SaveWorld();
        }
    }
    private void CheckFurnaceSessions(World world)
    {
        foreach (int owner in world.furnaces.Owners())
        {
            bool connected = NetworkServer.connections.TryGetValue(owner, out var connection) && connection != null && connection.identity != null;
            var player = connected ? connection.identity.GetComponent<Player>() : null;
            if (player != null && player.dimension == world.dimension && !player.DimensionTransferInProgress && player.health > 0 && world.FurnaceAccessible(player.transform.position + Vector3.up * 1.6f, world.furnaces.Position(owner))) continue;
            var reply = world.furnaces.Close(owner);
            if (player != null) player.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(reply.inventory, reply.cursor);
            if (connected) connection.Send(reply);
            world.SaveWorld();
        }
    }
}
