using UnityEngine;

public partial class World
{
    public bool HasHostileMobNear(Vector3 position,float horizontal,float vertical)
    {
        EnsureMobsRestored();
        foreach(var mob in mobs.Entities)
        {
            if(mob==null||mob.health<=0||!MobRules.Hostile(mob.species))continue;
            var delta=mob.position-position;
            if(Mathf.Abs(delta.x)<=horizontal&&Mathf.Abs(delta.y)<=vertical&&Mathf.Abs(delta.z)<=horizontal)return true;
        }
        return false;
    }
}
