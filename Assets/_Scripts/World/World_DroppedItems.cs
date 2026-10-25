using System.Collections.Generic;
using Mirror;
using UnityEngine;

public struct DropStateMessage : NetworkMessage { public WorldDimension dimension;public DroppedItemState item;public bool pickedUp;public Vector3 collector; }
public struct DropSyncRequest : NetworkMessage {}

public partial class World
{
    [System.NonSerialized] private DroppedItemStore runtimeDroppedItems;
    public DroppedItemStore droppedItems => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeDroppedItems, () => new DroppedItemStore());
    [System.NonSerialized] private Dictionary<int,DroppedItemVisual> runtimeDropVisuals;
    private Dictionary<int,DroppedItemVisual> dropVisuals => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeDropVisuals, () => new Dictionary<int,DroppedItemVisual>());
    [System.NonSerialized] private Dictionary<ItemType,Material> runtimeDropMaterials;
    private Dictionary<ItemType,Material> dropMaterials => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeDropMaterials, () => new Dictionary<ItemType,Material>());
    [System.NonSerialized] private Dictionary<BlockType,Mesh> runtimeDropMeshes;
    private Dictionary<BlockType,Mesh> dropMeshes => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeDropMeshes, () => new Dictionary<BlockType,Mesh>());
    [System.NonSerialized] private List<DroppedItemState> runtimeDropTickBuffer;
    private List<DroppedItemState> dropTickBuffer => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeDropTickBuffer, () => new List<DroppedItemState>());
    [System.NonSerialized] private List<int> runtimeRemovedDropVisuals;
    private List<int> removedDropVisuals => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeRemovedDropVisuals, () => new List<int>());
    private Mesh dropBillboard;
    private DroppedItemState[] dropRestore;
    private float dropClock;
    private bool dropSyncRequested;
    private void InitializeDroppedItems()
    {
        NetworkClient.RegisterHandler<DropStateMessage>(message=>
        {
            if(NetworkServer.active)return;
            var world=FindDimension(message.dimension);if(world==null)return;
            if(message.pickedUp&&message.item!=null)world.BeginItemPickup(message.item.id,message.collector);
            world.droppedItems.Apply(message.item);
        });
    }
    public InventoryStack MiningItemDrop(BlockType type,ItemType tool=ItemType.Nothing,int damage=0)
    {
        if(type==BlockType.TallGrass||type==BlockType.Fern)
        {
            if(tool==ItemType.Shears&&damage<ItemDurability.Maximum(tool))return new InventoryStack(ItemCatalog.FromBlock(type),1);
            return Random.Range(0,8)==0?new InventoryStack(ItemType.Seeds,1):new InventoryStack();
        }
        return ToolMining.Drop(type,tool,Random.Range(0,10),Random.Range(0,20),Random.Range(0,30),damage);
    }
    public bool SpawnItemDrop(InventoryStack stack,Vector3 position,Vector3? motion=null,int delay=10)
    {
        if(!DropAuthority||stack.Empty||!DroppedItemRules.Finite(position))return false;
        EnsureDroppedItemsRestored();
        int count=stack.count;bool spawned=false;
        while(count>0)
        {
            var portion=new InventoryStack(stack.type,System.Math.Min(count,InventoryRules.Limit(stack.type)),stack.damage);
            var velocity=motion??new Vector3(Random.Range(-.1f,.1f),.2f,Random.Range(-.1f,.1f));
            var item=droppedItems.Spawn(portion,position,velocity,delay,Random.Range(0,Mathf.PI*2));
            if(item==null)return spawned;
            BroadcastDrop(item);spawned=true;count-=portion.count;
        }
        return spawned;
    }
    public DroppedItemState[] CaptureDroppedItems()
    {
        if(dropRestore==null)return droppedItems.Capture();
        var copy=new DroppedItemStore();copy.Restore(dropRestore);return copy.Capture();
    }
    public void RestoreDroppedItems(DroppedItemState[] saved)
    {ResetDroppedItems();dropRestore=DropAuthority?saved:null;}
    public void BroadcastDrop(DroppedItemState item)
    {if(NetworkServer.active)NetworkServer.SendToAll(new DropStateMessage{dimension=dimension,item=item.Copy()});}
    public void BroadcastDropRemoval(int id)
    {BroadcastDrop(new DroppedItemState{id=id,stack=new InventoryStack()});}
    public int PickupItemDrop(int id,Vector3 position,InventoryStack[] inventory)
    {
        if(!DropAuthority||isSaving)return 0;
        EnsureDroppedItemsRestored();
        int count=droppedItems.Pickup(id,position,inventory);
        if(count>0)
        {
            var item=droppedItems.Get(id);
            if(item==null)
            {
                Vector3 collector=position+Vector3.up;
                BeginItemPickup(id,collector);
                if(NetworkServer.active)NetworkServer.SendToAll(new DropStateMessage{dimension=dimension,item=new DroppedItemState{id=id},pickedUp=true,collector=collector});
            }
            else BroadcastDrop(item);
        }
        return count;
    }
    private void BeginItemPickup(int id,Vector3 target)
    {
        if(Instance!=this)return;
        if(NetworkServer.active&&!NetworkClient.active)return;
        if(dropVisuals.TryGetValue(id,out var visual))visual.BeginPickup(target);
        var assets=Resources.Load<PlayerHudAssets>("PlayerHudAssets");
        GameAudio.PlayClip(assets!=null&&assets.itemPickup!=null?assets.itemPickup:GameAudio.Find("random/pop"),target,.2f,true,GameAudio.Variation(.9f,1.1f));
    }
    private void EnsureDroppedItemsRestored()
    {
        if(!DropAuthority||dropRestore==null)return;
        droppedItems.Restore(dropRestore);dropRestore=null;
        foreach(var item in droppedItems.Items)BroadcastDrop(item);
    }
    private void UpdateDroppedItems()
    {
        if(!Application.isPlaying||disabled||!IsWorldCreated||GenerationBusy)return;
        EnsureDroppedItemsRestored();
        if(!DropAuthority&&!dropSyncRequested&&NetworkClient.ready)
        {dropSyncRequested=true;NetworkClient.Send(new DropSyncRequest());}
        if(DropAuthority&&!isSaving)
        {
            dropClock+=Time.deltaTime;
            // Catch up without tying gravity or despawn age to Unity's physics rate.
            while(dropClock+.000001f>=.05f)
            {
                dropClock=Mathf.Max(0,dropClock-.05f);dropTickBuffer.Clear();dropTickBuffer.AddRange(droppedItems.Items);
                foreach(var item in dropTickBuffer)
                {
                    if(item.position.y<-64){droppedItems.Remove(item.id);BroadcastDropRemoval(item.id);continue;}
                    // Beta cactus destroys item entities immediately on contact.
                    // Test the entity box, not only its center voxel, so an item
                    // touching the inset cactus edge cannot survive in a neighbor.
                    if(BlockCollision.TouchesBlock(this,item.position,DroppedItemRules.HalfWidth,DroppedItemRules.Height,BlockType.Cactus))
                    {droppedItems.Remove(item.id);BroadcastDropRemoval(item.id);continue;}
                    var cell=Vector3Int.FloorToInt(item.position);
                    var block=GetBlock(new Vector3Int(cell.x,Mathf.Clamp(cell.y,0,worldHeight-1),cell.z));
                    // Unloaded chunks pause entities rather than dropping them through the void.
                    if(block==null||block.type==BlockType.Nothing)continue;
                    PushDropOutOfBlock(item,cell,block.type);
                    var current=new Vector3();
                    if(block.type==BlockType.Water)
                    {
                        var p=Vector3Int.FloorToInt(item.position);
                        var flow=BetaFluidPhysics.Current(q=>FluidWorldQueries.Sample(this,q,BlockType.Water),q=>FluidWorldQueries.SolidFace(this,q),new WaterPosition(p.x,p.y,p.z));
                        current=new Vector3((float)flow.x,(float)flow.y,(float)flow.z);
                    }
                    if(block.type==BlockType.Lava){item.motion.x=Random.Range(-.2f,.2f);item.motion.z=Random.Range(-.2f,.2f);}
                    var below=GetBlock(Vector3Int.FloorToInt(item.position+Vector3.down*.01f));
                    bool alive=DroppedItemRules.Tick(item,(feet,move)=>BlockCollision.Resolve(this,feet,DroppedItemRules.HalfWidth,DroppedItemRules.Height,move,false),
                        below!=null&&below.type==BlockType.Ice?.98f:.6f,current,block.type==BlockType.Lava,block.type==BlockType.Fire);
                    if(!alive){droppedItems.Remove(item.id);BroadcastDropRemoval(item.id);}
                    else if(item.age%5==0)BroadcastDrop(item);
                }
            }
        }
        if(Instance==this&&(!NetworkServer.active||NetworkClient.active))UpdateDropVisuals();
    }
    private void PushDropOutOfBlock(DroppedItemState item,Vector3Int cell,BlockType type)
    {
        if(!SpecialBlockGeometry.NormalCube(type))return;
        float best=2;int face=-1;
        Vector3 local=item.position-(Vector3)cell;
        for(int i=0;i<5;i++)
        {
            var delta=i==0?new Vector3Int(-1,0,0):i==1?new Vector3Int(1,0,0):i==2?new Vector3Int(0,0,-1):i==3?new Vector3Int(0,0,1):new Vector3Int(0,1,0);
            var neighbor=GetBlock(cell+delta);
            if(neighbor==null||neighbor.type==BlockType.Nothing||SpecialBlockGeometry.NormalCube(neighbor.type))continue;
            float distance=i==0?local.x:i==1?1-local.x:i==2?local.z:i==3?1-local.z:1-local.y;
            if(distance<best){best=distance;face=i;}
        }
        float speed=Random.Range(.1f,.3f);
        if(face<2&&face>=0)item.motion.x=face==0?-speed:speed;
        else if(face<4&&face>=2)item.motion.z=face==2?-speed:speed;
        else if(face==4)item.motion.y=speed;
    }
    private void UpdateDropVisuals()
    {
        removedDropVisuals.Clear();
        foreach(var pair in dropVisuals)if(droppedItems.Get(pair.Key)==null&&!pair.Value.AnimatePickup())removedDropVisuals.Add(pair.Key);
        foreach(int id in removedDropVisuals){dropVisuals[id].Dispose();dropVisuals.Remove(id);}
        foreach(var item in droppedItems.Items)
        {
            var block=GetBlock(Vector3Int.FloorToInt(item.position));
            bool visible=block!=null&&block.type!=BlockType.Nothing;
            if(!dropVisuals.TryGetValue(item.id,out var visual)&&visible)
            {
                var definition=ItemCatalog.Get(item.stack.type);if(definition==null)continue;
                bool flat=definition.icon!=null||!ItemCatalog.TryPlacedBlock(item.stack.type,out _);
                Mesh mesh;
                if(flat){if(dropBillboard==null)dropBillboard=ItemVisualMesh.Billboard();mesh=dropBillboard;}
                else
                {
                    ItemCatalog.TryPlacedBlock(item.stack.type,out var type);
                    if(!dropMeshes.TryGetValue(type,out mesh))dropMeshes[type]=mesh=ItemVisualMesh.Build(type);
                }
                if(!dropMaterials.TryGetValue(item.stack.type,out var material))
                {
                    var template=Resources.Load<Material>(flat?"DroppedItemMaterial":"DroppedBlockMaterial");
                    if(template==null)continue;
                    material=new Material(template);
                    if(flat)material.mainTexture=definition.icon!=null?definition.icon:Texture2D.whiteTexture;
                    dropMaterials[item.stack.type]=material;
                }
                visual=new DroppedItemVisual(worldRenderer.transform,item.stack,mesh,material,flat);dropVisuals.Add(item.id,visual);
            }
            if(visual!=null){visual.root.SetActive(visible);if(visible)visual.Update(item,this,DropCamera());}
        }
    }
    private static Camera DropCamera()
    {
        var player=GameManager.Instance!=null?GameManager.Instance.localPlayer:null;
        return player!=null&&player.cam!=null?player.cam.GetComponent<Camera>():Camera.main;
    }
    private void ResetDroppedItems(bool discardSave=false)
    {
        foreach(var visual in dropVisuals.Values)visual.Dispose();dropVisuals.Clear();
        foreach(var mesh in dropMeshes.Values)if(mesh!=null)Destroy(mesh);dropMeshes.Clear();
        foreach(var material in dropMaterials.Values)if(material!=null)Destroy(material);dropMaterials.Clear();
        if(dropBillboard!=null)Destroy(dropBillboard);dropBillboard=null;
        if(discardSave)dropRestore=null;
        droppedItems.Restore(null);dropClock=0;dropSyncRequested=false;dropTickBuffer.Clear();
    }
}
