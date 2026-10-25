using UnityEngine;

// Runs for visible network players as well as the local player. Cosmetic RNG
// and distance counters never touch world-generation or survival state.
public sealed class PlayerGameplayAudio : MonoBehaviour
{
    private Player player;
    private Vector3 previous;
    private int previousHealth,previousLevel;
    private bool initialized;
    private float distance,caveTime;
    private void Awake(){player=GetComponent<Player>();caveTime=GameAudio.Variation(90,180);}
    private void LateUpdate()
    {
        if(Application.isBatchMode)return;
        var world=World.Instance;
        if(player==null||world==null||player.dimension!=world.dimension||player.DimensionTransferInProgress||!world.IsWorldCreated||WorldLoadingScreen.IsLoading){initialized=false;return;}
        var position=transform.position;
        if(!initialized){previous=position;previousHealth=player.health;previousLevel=player.experienceLevel;initialized=true;return;}
        if(player.health<previousHealth)GameAudio.Play("random/classic_hurt",position+Vector3.up,.7f,GameAudio.Variation(.9f,1.1f));
        if(player.experienceLevel>previousLevel)GameAudio.Play("random/levelup",position,.5f);
        previousHealth=player.health;previousLevel=player.experienceLevel;
        var moved=position-previous;previous=position;
        float travel=new Vector2(moved.x,moved.z).magnitude;
        if(player.health<=0||player.isFlying||player.isCrouching||travel>4){distance=0;return;}
        var below=world.GetBlock(position+Vector3.down*.1f);
        if(player.InWater)
        {
            distance+=travel;
            if(distance>=1){distance%=1;GameAudio.Play("liquid/swim",position,.25f,GameAudio.Variation(.9f,1.1f));}
        }
        else if(below!=null&&BlockSoundRules.Material(below.type)!=null&&Mathf.Abs(moved.y)<.25f)
        {
            distance+=travel;
            if(distance>=1.67f)
            {distance%=1.67f;GameAudio.Play(BlockSoundRules.Step(below.type),position,.3f,GameAudio.Variation(.9f,1.1f));}
        }
        else distance=0;
        if(!player.isLocalPlayer)return;
        caveTime-=Time.deltaTime;
        if(caveTime<=0)
        {
            caveTime=GameAudio.Variation(90,180);
            var eye=world.GetBlock(position+Vector3.up*player.eyeHeight);
            if(eye!=null&&eye.GetSkyLight()==0&&eye.GetBlockLight()<7)GameAudio.Play("ambient/cave/cave",position,.45f);
        }
    }
}
