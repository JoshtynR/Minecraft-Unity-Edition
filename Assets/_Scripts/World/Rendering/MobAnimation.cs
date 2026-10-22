using UnityEngine;

public static class MobAnimation
{
    private static void Rotate(Transform[] parts,int index,Quaternion rotation)
    {if(index<parts.Length&&parts[index]!=null)parts[index].localRotation=rotation;}
    public static void Apply(Transform[] parts,MobState mob,float walk,float amplitude)
    {
        float angle=Mathf.Cos(walk*.6662f)*1.4f*amplitude*Mathf.Rad2Deg;
        var forward=Quaternion.Euler(angle,0,0);var back=Quaternion.Euler(-angle,0,0);
        if(mob.species==MobType.Spider)
        {
            for(int i=3;i<parts.Length;i++)
            {
                int leg=i-3,pair=leg/2;float side=leg%2==0?1:-1;
                float roll=(pair==0||pair==3?45:33)+Mathf.Abs(Mathf.Sin(walk*.6662f+pair))*amplitude*15;
                Rotate(parts,i,Quaternion.Euler(0,-side*(45-pair*30)+Mathf.Cos(walk*.6662f+pair)*amplitude*20,side*roll));
            }
        }
        else
        {
            Rotate(parts,5,forward);Rotate(parts,6,back);Rotate(parts,7,back);Rotate(parts,8,forward);
        }
        var head=Quaternion.Euler(mob.headPitch,mob.headYaw,0);
        Rotate(parts,1,head);
        float headScale=mob.ageTicks>0?1.5f:1;
        if(parts[1]!=null)parts[1].localScale=Vector3.one*headScale;
        if(mob.species==MobType.Cow)
        {
            for(int i=2;i<=3;i++)
            {parts[i].localScale=Vector3.one*headScale;parts[i].localPosition=MobModelGeometry.Pivot(1,mob.species)+head*((MobModelGeometry.Pivot(i,mob.species)-MobModelGeometry.Pivot(1,mob.species))*headScale);Rotate(parts,i,head);}
        }
        else if(mob.species==MobType.Pig){Rotate(parts,2,head);if(parts[2]!=null)parts[2].localScale=Vector3.one*headScale;}
        else if(mob.species==MobType.Sheep)
        {
            if(mob.eatTicks>0)head=Quaternion.Euler(50,0,0);
            Rotate(parts,1,head);Rotate(parts,3,head);
            parts[3].localScale=Vector3.one*headScale;
            var drop=mob.eatTicks>0?Vector3.down*.25f:Vector3.zero;
            parts[1].localPosition=MobModelGeometry.Pivot(1,mob.species)+drop;parts[3].localPosition=MobModelGeometry.Pivot(3,mob.species)+drop;
            for(int i=9;i<parts.Length;i++)Rotate(parts,i,parts[i-4].localRotation);
        }
        else if(mob.species==MobType.Chicken)
        {
            Rotate(parts,2,head);Rotate(parts,3,head);
            parts[2].localScale=parts[3].localScale=Vector3.one*headScale;
            float flap=mob.grounded?0:20+Mathf.Sin(Time.time*20)*35;
            Rotate(parts,7,Quaternion.Euler(0,0,flap));Rotate(parts,4,Quaternion.Euler(0,0,-flap));
        }
        else if(mob.species==MobType.Zombie||mob.species==MobType.Skeleton)
        {
            float pose=mob.targetMemoryTicks>0||mob.species==MobType.Zombie?-90:0;
            Rotate(parts,2,Quaternion.Euler(pose+angle*.15f,0,0));Rotate(parts,3,Quaternion.Euler(pose-angle*.15f,0,0));
        }
    }
}
