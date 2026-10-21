using System;

// Entity/EntityPlayer/ModelBiped motion, sampled at Java's 20 Hz independently
// of Unity's physics and rendering rates. No movement or terrain RNG changes.
public sealed class BetaPlayerMotion
{
    private double pendingTime,pendingX,pendingY,pendingZ;
    private float distance,previousDistance,yaw,previousYaw,pitch,previousPitch;
    private float limb,previousLimb,limbSwing,age;
    public void Reset()
    {pendingTime=pendingX=pendingY=pendingZ=0;distance=previousDistance=yaw=previousYaw=pitch=previousPitch=limb=previousLimb=limbSwing=age=0;}
    public void Advance(float dx,float dy,float dz,float dt,bool grounded,bool alive,bool flying,bool crouching=false)
    {
        if(dt<=0||dt>.25f||float.IsNaN(dt)||float.IsNaN(dx)||float.IsNaN(dy)||float.IsNaN(dz)||
            float.IsInfinity(dx)||float.IsInfinity(dy)||float.IsInfinity(dz)||dx*dx+dy*dy+dz*dz>16)
        {Reset();return;}
        pendingTime+=dt;pendingX+=dx;pendingY+=dy;pendingZ+=dz;
        while(pendingTime>=.05-1e-7)
        {
            double fraction=Math.Min(1,.05/pendingTime);
            Tick((float)(pendingX*fraction),(float)(pendingY*fraction),(float)(pendingZ*fraction),grounded,alive,flying,crouching);
            pendingX*=1-fraction;pendingY*=1-fraction;pendingZ*=1-fraction;pendingTime=Math.Max(0,pendingTime-.05);
        }
    }
    private void Tick(float dx,float dy,float dz,bool grounded,bool alive,bool flying,bool crouching)
    {
        float travel=(float)Math.Sqrt(dx*dx+dz*dz);
        previousDistance=distance;if(!(grounded&&crouching))distance+=travel*.6f;
        previousYaw=yaw;previousPitch=pitch;previousLimb=limb;
        float targetYaw=grounded&&alive&&!flying?Math.Min(.1f,travel):0;
        float targetPitch=!grounded&&alive&&!flying?(float)Math.Atan(-dy*.2)*15:0;
        yaw+=(targetYaw-yaw)*.4f;pitch+=(targetPitch-pitch)*.8f;
        limb+=((alive&&!flying?Math.Min(1,travel*4):0)-limb)*.4f;
        limbSwing+=limb;age++;
    }
    public BetaMotionPose Sample(float renderSeconds=0)
    {
        float alpha=(float)Math.Max(0,Math.Min(1,(pendingTime+renderSeconds)/.05));
        float phase=-(distance+(distance-previousDistance)*alpha)*(float)Math.PI;
        float amount=previousYaw+(yaw-previousYaw)*alpha;
        float bodyAmount=previousLimb+(limb-previousLimb)*alpha;
        float bodyPhase=(limbSwing-limb*(1-alpha))*.6662f;
        return new BetaMotionPose {
            X=(float)Math.Sin(phase)*amount*.5f,Y=-(float)Math.Abs(Math.Cos(phase)*amount),
            Roll=(float)Math.Sin(phase)*amount*3,
            Pitch=(float)Math.Abs(Math.Cos(phase-.2f)*amount)*5+previousPitch+(pitch-previousPitch)*alpha,
            RightArm=(float)Math.Cos(bodyPhase+Math.PI)*bodyAmount,LeftArm=(float)Math.Cos(bodyPhase)*bodyAmount,
            RightLeg=(float)Math.Cos(bodyPhase)*1.4f*bodyAmount,LeftLeg=(float)Math.Cos(bodyPhase+Math.PI)*1.4f*bodyAmount,
            IdleX=(float)Math.Sin((age+alpha)*.067f)*.05f,IdleRoll=(float)Math.Cos((age+alpha)*.09f)*.05f+.05f
        };
    }
}
public struct BetaMotionPose
{
    public float X,Y,Roll,Pitch,RightArm,LeftArm,RightLeg,LeftLeg,IdleX,IdleRoll;
}
