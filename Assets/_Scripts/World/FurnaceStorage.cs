using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

public struct FurnaceRequest : NetworkMessage
{
    public byte action; // open, click, close, refresh
    public Vector3Int position;
    public int slot, sequence, itemWearRevision;
    public bool right, shift;
    public InventoryStack[] inventory;
    public InventoryStack cursor;
}
public struct FurnaceReply : NetworkMessage
{
    public bool accepted, opened;
    public string error;
    public Vector3Int position;
    public InventoryStack[] items, inventory;
    public InventoryStack cursor;
    public int sequence, burnTicks, fuelTicks, cookTicks;
}

// Owns both persistent furnace slots and the authoritative open-menu transactions.
public sealed class FurnaceStorage
{
    [Serializable] public sealed class Entry { public Vector3Int position; public BetaSmelting.State state = new BetaSmelting.State(); }
    [Serializable] private sealed class Save { public List<Entry> entries = new List<Entry>(); }
    private sealed class Session { public Vector3Int position; public InventoryStack[] inventory; public InventoryStack cursor; public int sequence; }
    private readonly Dictionary<Vector3Int, Entry> entries = new Dictionary<Vector3Int, Entry>();
    private readonly Dictionary<int, Session> sessions = new Dictionary<int, Session>();
    private string loadedWorld;
    public IEnumerable<Entry> Entries => entries.Values;
    public bool HasOwner(int owner) => sessions.ContainsKey(owner);
    public int[] Owners() { var result = new int[sessions.Count]; sessions.Keys.CopyTo(result, 0); return result; }
    public Vector3Int Position(int owner) => sessions[owner].position;
    private static string SavePath(string name) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves", name, "furnaces.json");
    public void Load(string name)
    {
        if (loadedWorld == name) return;
        entries.Clear(); sessions.Clear(); loadedWorld = null;
        string path = SavePath(name);
        if (!string.IsNullOrEmpty(name) && File.Exists(path))
        {
            var save = JsonUtility.FromJson<Save>(File.ReadAllText(path));
            if (save == null || save.entries == null) throw new InvalidDataException("Invalid furnace save: " + path);
            foreach (var entry in save.entries)
            {
                if (entry == null || entry.state == null || entry.state.items == null || entry.state.items.Length != 3 ||
                    entry.state.burnTicks < 0 || entry.state.burnTicks > 20000 || entry.state.fuelTicks < 0 || entry.state.fuelTicks > 20000 ||
                    entry.state.cookTicks < 0 || entry.state.cookTicks >= BetaSmelting.CookTicks) throw new InvalidDataException("Invalid furnace state: " + path);
                foreach (var item in entry.state.items)
                    if (!item.Empty && (!ItemCatalog.Valid(item.type) || item.count > InventoryRules.Limit(item.type) || item.damage < 0))
                        throw new InvalidDataException("Invalid furnace item: " + path);
                entries.Add(entry.position, entry);
            }
        }
        loadedWorld = name;
    }
    public void SaveWorld(string name)
    {
        Load(name); if (string.IsNullOrEmpty(name)) return;
        string path = SavePath(name); Directory.CreateDirectory(Path.GetDirectoryName(path));
        var save = new Save();
        foreach (var entry in entries.Values) save.entries.Add(new Entry { position = entry.position, state = entry.state.Copy() });
        File.WriteAllText(path + ".tmp", JsonUtility.ToJson(save));
        if (File.Exists(path)) File.Replace(path + ".tmp", path, path + ".bak"); else File.Move(path + ".tmp", path);
    }
    public bool CanRemove(string name, Vector3Int position)
    {
        Load(name); foreach (var session in sessions.Values) if (session.position == position) return false;
        return true;
    }
    public void Track(string name, Vector3Int position)
    {
        Load(name);
        if (!entries.ContainsKey(position)) entries.Add(position, new Entry { position = position });
    }
    public InventoryStack[] Remove(Vector3Int position)
    {
        if (!entries.TryGetValue(position, out var entry)) return new InventoryStack[0];
        entries.Remove(position); return (InventoryStack[])entry.state.items.Clone();
    }
    public FurnaceReply Close(int owner)
    {
        if (!sessions.TryGetValue(owner, out var session)) return new FurnaceReply { error = "Furnace session closed." };
        session.cursor.count = InventoryRules.Insert(session.inventory, session.cursor.type, session.cursor.count, 0, 36, session.cursor.damage);
        session.cursor.Normalize(); var reply = Reply(session, false); sessions.Remove(owner); return reply;
    }
    public FurnaceReply Process(string name, int owner, FurnaceRequest request, bool accessible)
    {
        Load(name);
        if (request.action == 2) return Close(owner);
        if (!accessible) return new FurnaceReply { error = "Furnace is out of reach." };
        if (request.action == 0)
        {
            if (sessions.ContainsKey(owner)) return new FurnaceReply { error = "Close the current furnace first." };
            foreach (var active in sessions.Values) if (active.position == request.position) return new FurnaceReply { error = "Furnace is being used by another player." };
            if (!ChestTransactions.Valid(request.inventory, request.cursor)) return new FurnaceReply { error = "Invalid inventory." };
            if (!entries.ContainsKey(request.position)) entries.Add(request.position, new Entry { position = request.position });
            var opened = new Session { position = request.position, inventory = (InventoryStack[])request.inventory.Clone(), cursor = request.cursor };
            sessions.Add(owner, opened); return Reply(opened, true);
        }
        if (!sessions.TryGetValue(owner, out var session) || session.position != request.position) return new FurnaceReply { error = "Furnace session closed." };
        if (request.action == 3) return Reply(session, true);
        if (request.action != 1 || request.sequence != session.sequence + 1) return new FurnaceReply { error = "Stale furnace action." };
        if (!BetaSmelting.Click(entries[session.position].state, session.inventory, ref session.cursor, request.slot, request.right, request.shift)) return new FurnaceReply { error = "Invalid furnace action." };
        session.sequence++; return Reply(session, true);
    }
    private FurnaceReply Reply(Session session, bool opened)
    {
        var state = entries[session.position].state;
        return new FurnaceReply { accepted = true, opened = opened, position = session.position, sequence = session.sequence,
            inventory = (InventoryStack[])session.inventory.Clone(), cursor = session.cursor, items = (InventoryStack[])state.items.Clone(),
            burnTicks = state.burnTicks, fuelTicks = state.fuelTicks, cookTicks = state.cookTicks };
    }
}
