using System;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
    public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int order){}}
    public class MonoBehaviour {public T GetComponent<T>() where T:new()=>new T();}
    public class Transform {public Vector3 localPosition;public Quaternion localRotation=Quaternion.identity,rotation;}
    // Rotation-call doubles: tests validate authored Euler inputs, not Unity's GPU.
    public struct Quaternion
    {
        public float x,y,z;
        public static Quaternion identity=>new Quaternion();
        public static Quaternion Euler(float x,float y,float z)=>new Quaternion{x=x,y=y,z=z};
        public static Quaternion operator*(Quaternion a,Quaternion b)=>Euler(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Quaternion LookRotation(Vector3 forward,Vector3 up)=>identity;
    }
    public class Animator {public void SetFloat(string name,float v){}public void SetBool(string name,bool v){}}
    public static class Mathf {public const float PI=(float)Math.PI;}
}
public static class WorldLoadingScreen {public static bool IsLoading;}
public class PlayerObjects
{
    public Transform body=new Transform(),head=new Transform(),cam=new Transform();
    public Transform leftArm=new Transform(),rightArm=new Transform(),leftLeg=new Transform(),rightLeg=new Transform();
}
public struct Stack {public bool Empty;}
public class Inventory {public Stack HeldItem()=>new Stack{Empty=true};}
public class Player
{
    public bool IsMounted,isCrouching,isFlying,sleeping,DimensionTransferInProgress;
    public Vector3 SleepingDirection;
    public BetaMotionPose WalkPose;
    public Inventory inventory=new Inventory();
}
public static class RidingCheck
{
    static int count;
    static void Check(bool value,string name){count++;if(!value)throw new Exception(name);}
    static void Late(MinecraftWalkAnimator a)=>typeof(MinecraftWalkAnimator).GetMethod("LateUpdate",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(a,null);
    public static void Main()
    {
        var original=new BetaMotionPose{LeftLeg=.9f,RightLeg=-.9f,LeftArm=.2f,RightArm=-.2f};
        var seated=original.Seated();
        Check(Math.Abs(seated.LeftLeg+1.2566371f)<.00001f&&Math.Abs(seated.RightLeg+1.2566371f)<.00001f,"source seated leg pitch");
        Check(Math.Abs(seated.RightLegYaw-.31415927f)<.00001f&&Math.Abs(seated.LeftLegYaw+.31415927f)<.00001f,"source seated leg spread");
        Check(Math.Abs(seated.LeftArm+.62831855f)<.00001f&&Math.Abs(seated.RightArm+.62831855f)<.00001f,"source arm resting pitch");
        Check(original.LeftLeg==.9f,"shared pose is a value snapshot, not sticky global state");
        var owner=new Player{IsMounted=true};var parts=new PlayerObjects();var animator=new MinecraftWalkAnimator();animator.Initialize(owner,parts);Late(animator);
        Check(Math.Abs(parts.leftLeg.localRotation.x+72)<.001f&&Math.Abs(parts.rightLeg.localRotation.x+72)<.001f,"actual animator bends both mounted legs");
        Check(Math.Abs(parts.leftLeg.localRotation.y+18)<.001f&&Math.Abs(parts.rightLeg.localRotation.y-18)<.001f,"actual animator spreads mounted knees");
        Check(Math.Abs(parts.leftArm.localRotation.x+36)<.001f,"actual animator rests mounted arm");
        owner.isCrouching=true;Late(animator);Check(Math.Abs(parts.leftArm.localRotation.x+36)<.001f,"crouching does not offset mounted pose");
        owner.IsMounted=false;owner.isCrouching=false;Late(animator);
        Check(parts.leftLeg.localRotation.x==0&&parts.leftLeg.localRotation.y==0&&parts.rightLeg.localRotation.x==0,"dismount restores walking limb rotations");
        owner.IsMounted=true;Late(animator);Check(Math.Abs(parts.rightLeg.localRotation.x+72)<.001f,"same pose can be reused on another mount");
        Console.WriteLine(count+" shared riding-pose and production animator checks passed (Unity transforms mocked).");
    }
}
