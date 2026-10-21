using Mirror;
using UnityEngine;

public partial class Player
{
    public bool viewBobbing=true;
    private Vector3 cameraRest,bobPrevious;
    private bool cameraBobKnown;
    private float cameraBobPhase,cameraBobAmount;
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
        if(!cameraBobKnown){cameraRest=cam.localPosition;bobPrevious=transform.position;cameraBobKnown=true;}
        var delta=transform.position-bobPrevious;bobPrevious=transform.position;
        float distance=new Vector2(delta.x,delta.z).magnitude;
        bool active=viewBobbing&&health>0&&isGrounded&&!isFlying&&!InWater&&!InLava&&
            inventory!=null&&!inventory.isOpen&&!escapeMenuOpen&&distance<1;
        cameraBobAmount=Mathf.MoveTowards(cameraBobAmount,active?Mathf.Clamp01(distance/Mathf.Max(Time.deltaTime,.001f)/walkSpeed):0,Time.deltaTime*8);
        if(active)cameraBobPhase+=distance*6;
        var offset=cam.right*(Mathf.Sin(cameraBobPhase)*.025f*cameraBobAmount)+
            Vector3.up*(Mathf.Abs(Mathf.Cos(cameraBobPhase))*.035f*cameraBobAmount);
        cam.localPosition=cameraRest+(cam.parent!=null?cam.parent.InverseTransformVector(offset):offset);
        cam.localRotation*=Quaternion.Euler(0,0,Mathf.Sin(cameraBobPhase)*.6f*cameraBobAmount);
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
        {health=Mathf.Max(0,health-damage);GameAudio.Broadcast(damage>4?"damage/fallbig":"damage/fallsmall",transform.position,1,1,world);}
    }

    private bool TryStartEating()
    {
        if(inventory==null||inventory.DropRequestPending||hunger>=20||health<=0)return false;
        var held=inventory.HeldItem();int food;float sat;
        if(held.Empty||!FoodRules.TryGet(held.type,out food,out sat))return false;
        eating=true;foodWaiting=false;eatingSlot=inventory.selectedSlotIndex;eatingType=held.type;
        eatingStarted=nextChew=Time.time;inventory.DropRequestPending=true;
        var snapshot=inventory.DropInventorySnapshot();
        if(networkStarted)CmdBeginEating(eatingSlot,snapshot,knownItemWearRevision);
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
        if(networkStarted)CmdFinishEating(finish);else FinishEating(finish,false);
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
