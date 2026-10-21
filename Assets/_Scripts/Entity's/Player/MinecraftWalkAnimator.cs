using UnityEngine;

// Override only limb rotations after Animator evaluates crouch/head/body poses.
[DefaultExecutionOrder(50)]
public sealed class MinecraftWalkAnimator : MonoBehaviour
{
    private Player player;
    private PlayerObjects parts;
    private Animator animator;
    private Quaternion leftArm,rightArm,leftLeg,rightLeg;
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
        var pose=player.WalkPose;float degrees=180f/Mathf.PI;
        float crouch=player.isCrouching&&!player.isFlying?.4f:0;
        float right=pose.RightArm;
        if(player.inventory!=null&&!player.inventory.HeldItem().Empty)right=right*.5f-.31415927f;
        if(parts.leftArm!=null)parts.leftArm.localRotation=leftArm*Quaternion.Euler(0,0,-pose.IdleRoll*degrees)*Quaternion.Euler((pose.LeftArm+crouch-pose.IdleX)*degrees,0,0);
        if(parts.rightArm!=null)parts.rightArm.localRotation=rightArm*Quaternion.Euler(0,0,pose.IdleRoll*degrees)*Quaternion.Euler((right+crouch+pose.IdleX)*degrees,0,0);
        if(parts.leftLeg!=null)parts.leftLeg.localRotation=leftLeg*Quaternion.Euler(pose.LeftLeg*degrees,0,0);
        if(parts.rightLeg!=null)parts.rightLeg.localRotation=rightLeg*Quaternion.Euler(pose.RightLeg*degrees,0,0);
    }
}
