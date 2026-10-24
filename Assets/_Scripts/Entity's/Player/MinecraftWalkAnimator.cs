using UnityEngine;

// Override only limb rotations after Animator evaluates crouch/head/body poses.
[DefaultExecutionOrder(50)]
public sealed class MinecraftWalkAnimator : MonoBehaviour
{
    private Player player;
    private PlayerObjects parts;
    private Animator animator;
    private Quaternion leftArm,rightArm,leftLeg,rightLeg;
    private Vector3 wakingBodyPosition;
    private Quaternion wakingBodyRotation,wakingHeadRotation,wakingCameraRotation;
    private bool sleepPoseApplied;
    public void Initialize(Player owner,PlayerObjects objects)
    {
        if(player!=null||owner==null||objects==null)return;
        player=owner;parts=objects;
        animator=GetComponent<Animator>();
        leftArm=Rest(parts.leftArm);rightArm=Rest(parts.rightArm);
        leftLeg=Rest(parts.leftLeg);rightLeg=Rest(parts.rightLeg);
    }
    private static Quaternion Rest(Transform part)=>part!=null?part.localRotation:Quaternion.identity;
    private void Update()
    {
        if(player==null||animator==null)return;
        // Keep authored crouch translations for both local and remote players.
        // The old fixed-duration walking clip no longer drives the limbs.
        animator.SetFloat("speed",0);
        animator.SetBool("sneaking",player.isCrouching&&!player.isFlying);
    }
    private void LateUpdate()
    {
        if(player==null||parts==null||player.DimensionTransferInProgress||WorldLoadingScreen.IsLoading)return;
        if(player.sleeping){ApplySleepPose();return;}
        RestoreSleepPose();
        var pose=player.WalkPose;float degrees=180f/Mathf.PI;
        float crouch=player.isCrouching&&!player.isFlying?.4f:0;
        float right=pose.RightArm;
        if(player.inventory!=null&&!player.inventory.HeldItem().Empty)right=right*.5f-.31415927f;
        if(parts.leftArm!=null)parts.leftArm.localRotation=leftArm*Quaternion.Euler(0,0,-pose.IdleRoll*degrees)*Quaternion.Euler((pose.LeftArm+crouch-pose.IdleX)*degrees,0,0);
        if(parts.rightArm!=null)parts.rightArm.localRotation=rightArm*Quaternion.Euler(0,0,pose.IdleRoll*degrees)*Quaternion.Euler((right+crouch+pose.IdleX)*degrees,0,0);
        if(parts.leftLeg!=null)parts.leftLeg.localRotation=leftLeg*Quaternion.Euler(pose.LeftLeg*degrees,0,0);
        if(parts.rightLeg!=null)parts.rightLeg.localRotation=rightLeg*Quaternion.Euler(pose.RightLeg*degrees,0,0);
    }

    private void ApplySleepPose()
    {
        if(!sleepPoseApplied)
        {
            wakingBodyPosition=parts.body.localPosition;wakingBodyRotation=parts.body.localRotation;
            wakingHeadRotation=parts.head!=null?parts.head.localRotation:Quaternion.identity;
            wakingCameraRotation=parts.cam!=null?parts.cam.localRotation:Quaternion.identity;
            sleepPoseApplied=true;
        }
        var direction=player.SleepingDirection;if(direction.sqrMagnitude<.5f)direction=Vector3.forward;
        // The model faces upward with its feet pointing toward the foot half.
        // Keep the view horizontal and fixed along the bed as classic clients do.
        parts.body.localPosition=new Vector3(wakingBodyPosition.x,.23f,wakingBodyPosition.z);
        parts.body.rotation=Quaternion.LookRotation(Vector3.up,direction);
        if(parts.head!=null)parts.head.localRotation=Quaternion.identity;
        if(parts.cam!=null)parts.cam.rotation=Quaternion.LookRotation(direction,Vector3.up);
        if(parts.leftArm!=null)parts.leftArm.localRotation=leftArm;
        if(parts.rightArm!=null)parts.rightArm.localRotation=rightArm;
        if(parts.leftLeg!=null)parts.leftLeg.localRotation=leftLeg;
        if(parts.rightLeg!=null)parts.rightLeg.localRotation=rightLeg;
    }

    private void RestoreSleepPose()
    {
        if(!sleepPoseApplied)return;sleepPoseApplied=false;
        parts.body.localPosition=wakingBodyPosition;parts.body.localRotation=wakingBodyRotation;
        if(parts.head!=null)parts.head.localRotation=wakingHeadRotation;
        if(parts.cam!=null)parts.cam.localRotation=wakingCameraRotation;
    }

    private void OnDisable(){if(parts!=null)RestoreSleepPose();}
}
