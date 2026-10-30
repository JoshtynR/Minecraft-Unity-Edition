using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

public struct ChestRequest : NetworkMessage
{
    public int itemWearRevision;
    public byte action; // 0 open, 1 click, 2 close, 4 drag, 5 collect, 6 drop cursor
    public Vector3Int position;
    public int slot;
    public bool right,shift;
    public int[] slots;
    public InventoryStack[] inventory;
    public InventoryStack cursor;
    public int sequence;
}
public struct ChestReply : NetworkMessage
{
    public bool accepted,opened;
    public bool sessionClosed;
    public string error;
    public Vector3Int position;
    public InventoryStack[] chest,inventory;
    public InventoryStack cursor;
    public InventoryStack dropped;
    public int sequence;
}

// World-owned storage; contents outlive chunk renderer pooling and streaming.
public sealed class ChestStorage
{
    [Serializable] public class Entry { public Vector3Int position;public InventoryStack[] items; }
    [Serializable] private class Save {public List<Entry> entries=new List<Entry>();}
    private sealed class Session
    {
        public Vector3Int position;public InventoryStack[] inventory;public InventoryStack cursor;public int sequence;
        public Vector3Int? partner;
    }
    private readonly Dictionary<Vector3Int,Entry> entries=new Dictionary<Vector3Int,Entry>();
    private readonly Dictionary<int,Session> sessions=new Dictionary<int,Session>();
    private string loadedWorld;
    public event Action<Vector3Int,bool> LidChanged;
    private static string SavePath(string name) => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves",name,"chests.json");
    public void Load(string name)
    {
        if(loadedWorld==name)return;
        entries.Clear();sessions.Clear();loadedWorld=name;
        string path=SavePath(name);
        if(string.IsNullOrEmpty(name)||!File.Exists(path))return;
        // A bad save must not silently replace contents with an empty chest.
        var save=JsonUtility.FromJson<Save>(File.ReadAllText(path));
        if(save==null||save.entries==null)throw new InvalidDataException("Invalid chest save: "+path);
        foreach(var entry in save.entries)
        {
            if(entry.items==null||entry.items.Length!=ChestTransactions.Size)throw new InvalidDataException("Invalid chest slots: "+path);
            entries[entry.position]=entry;
        }
    }
    public void SaveWorld(string name)
    {
        Load(name);if(string.IsNullOrEmpty(name))return;
        string path=SavePath(name);Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary=path+".tmp";File.WriteAllText(temporary,JsonUtility.ToJson(new Save{entries=new List<Entry>(entries.Values)}));
        if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
    }
    public bool CanRemove(string name,Vector3Int position)
    {
        Load(name);
        foreach(var session in sessions.Values)if(session.position==position||session.partner==position)return false;
        return true;
    }
    public InventoryStack[] Remove(Vector3Int position)
    {
        if(!entries.TryGetValue(position,out var entry))return new InventoryStack[0];
        // Consume the storage entry before spawning drops so a repeated block
        // update cannot emit the same contents twice. The paired half is separate.
        entries.Remove(position);return (InventoryStack[])entry.items.Clone();
    }
    public int[] Owners() {var owners=new int[sessions.Count];sessions.Keys.CopyTo(owners,0);return owners;}
    public Vector3Int Position(int owner) => sessions[owner].position;
    public ChestReply Close(int owner)
    {
        Session session;if(!sessions.TryGetValue(owner,out session))return new ChestReply{sessionClosed=true,error="Chest session closed."};
        session.cursor.count=InventoryRules.Insert(session.inventory,session.cursor.type,session.cursor.count,0,36,session.cursor.damage);
        session.cursor.Normalize();
        var reply=Reply(session,false);sessions.Remove(owner);LidChanged?.Invoke(session.position,false);if(session.partner.HasValue)LidChanged?.Invoke(session.partner.Value,false);return reply;
    }
    public ChestReply Process(string name,int owner,ChestRequest request,bool accessible,Vector3Int? partner=null)
    {
        Load(name);
        Session session;
        if(request.action==2)return Close(owner);
        if(!accessible)return new ChestReply{error="Chest is blocked or out of reach."};
        if(request.action==0)
        {
            if(sessions.ContainsKey(owner))return new ChestReply{error="Close the current chest first."};
            foreach(var active in sessions.Values)if(active.position==request.position||active.partner==request.position||partner.HasValue&&(active.position==partner.Value||active.partner==partner.Value))return new ChestReply{error="Chest is being used by another player."};
            if(!ChestTransactions.Valid(request.inventory,request.cursor))return new ChestReply{error="Invalid inventory."};
            Entry entry;if(!entries.TryGetValue(request.position,out entry))
                entries.Add(request.position,new Entry{position=request.position,items=new InventoryStack[ChestTransactions.Size]});
            if(partner.HasValue&&!entries.ContainsKey(partner.Value))entries.Add(partner.Value,new Entry{position=partner.Value,items=new InventoryStack[27]});
            session=new Session{position=request.position,partner=partner,inventory=(InventoryStack[])request.inventory.Clone(),cursor=request.cursor};
            sessions.Add(owner,session);LidChanged?.Invoke(request.position,true);if(partner.HasValue)LidChanged?.Invoke(partner.Value,true);return Reply(session,true);
        }
        if(!sessions.TryGetValue(owner,out session))return new ChestReply{sessionClosed=true,error="Chest session closed."};
        if(session.position!=request.position||request.sequence!=session.sequence+1)
            return new ChestReply{error="Stale chest action."};
        bool valid;InventoryStack dropped=new InventoryStack();
        var items=Items(session);
        if(request.action==1)valid=ChestTransactions.Click(items,session.inventory,ref session.cursor,request.slot,request.right,request.shift);
        else if(request.action==4)valid=ChestTransactions.Drag(items,session.inventory,ref session.cursor,request.slots,request.right);
        else if(request.action==5)valid=ChestTransactions.Collect(items,session.inventory,ref session.cursor,request.slot);
        else if(request.action==6){dropped=InventoryRules.Drop(ref session.cursor,request.right);valid=!dropped.Empty;}
        else valid=false;
        if(!valid)
            return new ChestReply{error="Invalid chest action."};
        StoreItems(session,items);session.sequence++;var reply=Reply(session,true);reply.dropped=dropped;return reply;
    }
    // Stable spatial ordering is independent of which half the player clicked.
    // Each physical chest retains its original 27-slot save entry.
    private Vector3Int First(Session session)
    {var p=session.position;var q=session.partner;return q.HasValue&&(q.Value.x<p.x||q.Value.x==p.x&&q.Value.z<p.z)?q.Value:p;}
    private InventoryStack[] Items(Session session)
    {
        if(!session.partner.HasValue)return (InventoryStack[])entries[session.position].items.Clone();
        var first=First(session);var second=first==session.position?session.partner.Value:session.position;
        var result=new InventoryStack[54];Array.Copy(entries[first].items,result,27);Array.Copy(entries[second].items,0,result,27,27);return result;
    }
    private void StoreItems(Session session,InventoryStack[] items)
    {
        var first=First(session);Array.Copy(items,entries[first].items,27);
        if(session.partner.HasValue){var second=first==session.position?session.partner.Value:session.position;Array.Copy(items,27,entries[second].items,0,27);}
    }
    private ChestReply Reply(Session session,bool opened) => new ChestReply
    {
        accepted=true,opened=opened,position=session.position,sequence=session.sequence,
        inventory=(InventoryStack[])session.inventory.Clone(),cursor=session.cursor,
        chest=Items(session)
    };
}
