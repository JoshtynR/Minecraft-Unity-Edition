using System;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

public struct ChestRequest : NetworkMessage
{
    public int itemWearRevision;
    public byte action; // 0 open, 1 click, 2 close
    public Vector3Int position;
    public int slot;
    public bool right,shift;
    public InventoryStack[] inventory;
    public InventoryStack cursor;
    public int sequence;
}
public struct ChestReply : NetworkMessage
{
    public bool accepted,opened;
    public string error;
    public Vector3Int position;
    public InventoryStack[] chest,inventory;
    public InventoryStack cursor;
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
        foreach(var session in sessions.Values)if(session.position==position)return false;
        Entry entry;if(!entries.TryGetValue(position,out entry))return true;
        foreach(var item in entry.items)if(!item.Empty)return false;
        return true;
    }
    public void Removed(Vector3Int position){entries.Remove(position);}
    public int[] Owners() {var owners=new int[sessions.Count];sessions.Keys.CopyTo(owners,0);return owners;}
    public Vector3Int Position(int owner) => sessions[owner].position;
    public ChestReply Close(int owner)
    {
        Session session;if(!sessions.TryGetValue(owner,out session))return new ChestReply{accepted=false,error="Chest session closed."};
        session.cursor.count=InventoryRules.Insert(session.inventory,session.cursor.type,session.cursor.count,0,36,session.cursor.damage);
        session.cursor.Normalize();
        var reply=Reply(session,false);sessions.Remove(owner);LidChanged?.Invoke(session.position,false);return reply;
    }
    public ChestReply Process(string name,int owner,ChestRequest request,bool accessible)
    {
        Load(name);
        Session session;
        if(request.action==2)return Close(owner);
        if(!accessible)return new ChestReply{error="Chest is blocked or out of reach."};
        if(request.action==0)
        {
            if(sessions.ContainsKey(owner))return new ChestReply{error="Close the current chest first."};
            foreach(var active in sessions.Values)if(active.position==request.position)return new ChestReply{error="Chest is being used by another player."};
            if(!ChestTransactions.Valid(request.inventory,request.cursor))return new ChestReply{error="Invalid inventory."};
            Entry entry;if(!entries.TryGetValue(request.position,out entry))
                entries.Add(request.position,new Entry{position=request.position,items=new InventoryStack[ChestTransactions.Size]});
            session=new Session{position=request.position,inventory=(InventoryStack[])request.inventory.Clone(),cursor=request.cursor};
            sessions.Add(owner,session);LidChanged?.Invoke(request.position,true);return Reply(session,true);
        }
        if(request.action!=1||!sessions.TryGetValue(owner,out session)||session.position!=request.position||request.sequence!=session.sequence+1)
            return new ChestReply{error="Stale chest action."};
        if(!ChestTransactions.Click(entries[session.position].items,session.inventory,ref session.cursor,request.slot,request.right,request.shift))
            return new ChestReply{error="Invalid chest action."};
        session.sequence++;return Reply(session,true);
    }
    private ChestReply Reply(Session session,bool opened) => new ChestReply
    {
        accepted=true,opened=opened,position=session.position,sequence=session.sequence,
        inventory=(InventoryStack[])session.inventory.Clone(),cursor=session.cursor,
        chest=(InventoryStack[])entries[session.position].items.Clone()
    };
}
