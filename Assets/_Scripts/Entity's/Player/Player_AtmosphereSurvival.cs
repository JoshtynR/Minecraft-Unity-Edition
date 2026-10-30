using Mirror;
using UnityEngine;

public partial class Player
{
    public bool viewBobbing=true;
    private Vector3 cameraRest,previousWalkPosition;
    private bool cameraBobKnown;
    private bool walkPositionKnown;
    private readonly BetaPlayerMotion walkMotion=new BetaPlayerMotion();
    private Vector3 viewBobTranslation;
    private Quaternion viewBobRotation=Quaternion.identity;
    public BetaMotionPose WalkPose => walkMotion.Sample(Mathf.Max(0,Time.time-Time.fixedTime));
    private void ResetWalkMotion(){walkPositionKnown=false;walkMotion.Reset();}
    private void AdvanceWalkMotion(float dt)
    {
        var position=transform.position;
        if(!walkPositionKnown||world==null||!world.IsWorldCreated)
        {previousWalkPosition=position;walkPositionKnown=true;walkMotion.Reset();return;}
        var delta=position-previousWalkPosition;previousWalkPosition=position;
        walkMotion.Advance(delta.x,delta.y,delta.z,dt,isGrounded,health>0,isFlying,isCrouching);
    }
    public void ApplyFirstPersonBob(Transform pose)
    {
        pose.localPosition=viewBobTranslation+viewBobRotation*pose.localPosition;
        pose.localRotation=viewBobRotation*pose.localRotation;
    }
    private readonly FallTracker falling=new FallTracker();
    private World fallWorld;
    private bool eating,foodWaiting;
    private int eatingSlot;
    private ItemType eatingType;
    private float eatingStarted,nextChew;
    private InventoryStack[] serverFoodSnapshot;
    private int serverFoodSlot,serverFoodRevision;
    private double serverFoodStarted;
    public bool IsEating => eating;
    public float EatingProgress => eating?Mathf.Clamp01((Time.time-eatingStarted)/FoodRules.EatSeconds):0;
    private void CancelFoodForTravel()
    {
        serverFoodSnapshot=null;
        if(eating)FinishEatingRequest(false);
    }

    private void UpdateCameraBobbing()
    {
        if(cam==null)return;
        if(!cameraBobKnown){cameraRest=cam.localPosition;cameraBobKnown=true;}
        bool active=viewBobbing&&health>0&&!isFlying&&inventory!=null&&!inventory.isOpen&&!escapeMenuOpen;
        var motion=WalkPose;
        viewBobTranslation=active?new Vector3(motion.X,motion.Y,0):Vector3.zero;
        viewBobRotation=active?Quaternion.Euler(0,0,motion.Roll)*Quaternion.Euler(-motion.Pitch,0,0):Quaternion.identity;
        // Java applies bob to the view matrix. A Unity camera needs the
        // inverse transform; held models get the forward transform separately.
        var inverse=Quaternion.Inverse(viewBobRotation);
        var offset=cam.rotation*(inverse*(-viewBobTranslation));
        cam.localPosition=cameraRest+(cam.parent!=null?cam.parent.InverseTransformVector(offset):offset);
        cam.localRotation*=inverse;
        if(cam.GetComponent<OverworldAtmosphere>()==null)cam.gameObject.AddComponent<OverworldAtmosphere>().player=this;
    }

    private void AdvanceFallDamage()
    {
        if(world==null||!world.IsWorldCreated||health<=0||dimensionTransfer||fallWorld!=world)
        {falling.Reset();fallWorld=world;return;}
        var below=world.GetBlock(transform.position+Vector3.down*.05f);
        bool fluid=FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Water,out _)||
            FluidWorldQueries.Touches(world,transform.position,entityWidth,entityHeight,BlockType.Lava,out _);
        bool local=!networkStarted||isLocalPlayer;
        bool immune=(local?isFlying:serverFlying)||fluid||below==null||below.type==BlockType.Nothing||below.type==BlockType.Ladder;
        int damage=falling.Step(transform.position.y,local?isGrounded:serverGrounded,immune);
        if(damage>0)
        {ApplyArmouredDamage(damage);GameAudio.Broadcast(damage>4?"damage/fallbig":"damage/fallsmall",transform.position,1,1,world);}
    }

    private void AdvanceCactusDamage()
    {
        if(world==null||!world.IsWorldCreated||health<=0||dimensionTransfer)return;
        bool local=!networkStarted||isLocalPlayer;
        if((local?isFlying:serverFlying)||NetworkTime.time<hurtUntil||
            !BlockCollision.TouchesBlock(world,transform.position,entityWidth,entityHeight,BlockType.Cactus))return;
        hurtUntil=NetworkTime.time+.5;
        ApplyArmouredDamage(1);
        exhaustion=System.Math.Min(40,exhaustion+.3f);
        GameAudio.Broadcast("damage/hit",transform.position,1,1,world);
    }

    private bool TryStartEating()
    {
        if(inventory==null||inventory.DropRequestPending||hunger>=20||health<=0)return false;
        var held=inventory.HeldItem();int food;float sat;
        if(held.Empty||!FoodRules.TryGet(held.type,out food,out sat))return false;
        eating=true;foodWaiting=false;eatingSlot=inventory.selectedSlotIndex;eatingType=held.type;
        eatingStarted=nextChew=Time.time;inventory.DropRequestPending=true;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted&&!isServer)CmdBeginEating(eatingSlot,snapshot,knownItemWearRevision);
        else BeginEating(eatingSlot,snapshot,knownItemWearRevision,false);
        return true;
    }
    private void UpdateEating()
    {
        if(inventory==null)return;
        if((eating||foodWaiting)&&networkStarted&&!NetworkClient.active)
        {eating=foodWaiting=false;serverFoodSnapshot=null;inventory.DropRequestPending=false;return;}
        if(eating)
        {
            bool cancelled=!Input.GetMouseButton(1)||health<=0||inventory.isOpen||escapeMenuOpen||dimensionTransfer||
                inventory.selectedSlotIndex!=eatingSlot||inventory.HeldItem().type!=eatingType;
            if(cancelled){FinishEatingRequest(false);return;}
            if(Time.time>=nextChew)
            {nextChew=Time.time+.25f;GameAudio.Play("random/eat",cam.position,.5f,GameAudio.Variation(.9f,1.1f));}
            if(EatingProgress>=1)FinishEatingRequest(true);
        }
        else if(!foodWaiting&&Input.GetMouseButton(1)&&!Input.GetMouseButtonDown(1)&&!inventory.isOpen&&!escapeMenuOpen)
            TryStartEating();
    }
    [Command] private void CmdBeginEating(int slot,InventoryStack[] snapshot,int revision){BeginEating(slot,snapshot,revision,true);}
    private void BeginEating(int slot,InventoryStack[] snapshot,int revision,bool network)
    {
        int food;float sat;
        if(world==null||!world.IsWorldCreated||health<=0||hunger>=20||dimensionTransfer||slot<0||slot>=9||!AcceptsItemRevision(revision)||
            !ChestTransactions.Valid(snapshot,new InventoryStack())||snapshot[slot].Empty||!FoodRules.TryGet(snapshot[slot].type,out food,out sat))
        {CompleteEating(null,false,network);return;}
        serverFoodSnapshot=(InventoryStack[])snapshot.Clone();serverFoodSlot=slot;serverFoodRevision=revision;serverFoodStarted=NetworkTime.time;
    }
    private void FinishEatingRequest(bool finish)
    {
        eating=false;foodWaiting=true;
        if(networkStarted&&!isServer)CmdFinishEating(finish);else FinishEating(finish,false);
    }
    [Command] private void CmdFinishEating(bool finish){FinishEating(finish,true);}
    private void FinishEating(bool finish,bool network)
    {
        var snapshot=serverFoodSnapshot;serverFoodSnapshot=null;int food;float sat;
        bool accepted=finish&&snapshot!=null&&health>0&&hunger<20&&!dimensionTransfer&&AcceptsItemRevision(serverFoodRevision)&&
            NetworkTime.time-serverFoodStarted>=FoodRules.EatSeconds-.05&&FoodRules.TryGet(snapshot[serverFoodSlot].type,out food,out sat);
        if(accepted)
        {
            FoodRules.TryGet(snapshot[serverFoodSlot].type,out food,out sat);
            bool bowl=snapshot[serverFoodSlot].type==ItemType.MushroomStew;
            hunger=Mathf.Min(20,hunger+food);saturation=Mathf.Min(hunger,saturation+sat);
            snapshot[serverFoodSlot].count--;snapshot[serverFoodSlot].Normalize();
            if(bowl)
            {
                if(snapshot[serverFoodSlot].Empty)snapshot[serverFoodSlot]=new InventoryStack{type=ItemType.Bowl,count=1};
                else if(InventoryRules.Insert(snapshot,ItemType.Bowl,1,0,36)>0)
                    world.SpawnItemDrop(new InventoryStack{type=ItemType.Bowl,count=1},transform.position+Vector3.up,Vector3.up*.1f,40);
            }
            inventory.ApplyDropInventory(snapshot);
            GameAudio.Broadcast("random/burp",transform.position,.5f,1,world);
        }
        CompleteEating(snapshot,accepted,network);
    }
    private void CompleteEating(InventoryStack[] snapshot,bool accepted,bool network)
    {if(network)TargetEating(connectionToClient,snapshot,accepted);else ReceiveEating(snapshot,accepted);}
    [TargetRpc] private void TargetEating(NetworkConnection target,InventoryStack[] snapshot,bool accepted){ReceiveEating(snapshot,accepted);}
    private void ReceiveEating(InventoryStack[] snapshot,bool accepted)
    {if(inventory==null)return;if(accepted)inventory.ApplyDropInventory(snapshot);eating=foodWaiting=false;inventory.DropRequestPending=false;}
}
