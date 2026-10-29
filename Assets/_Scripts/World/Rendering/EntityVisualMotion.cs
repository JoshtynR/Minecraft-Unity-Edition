using UnityEngine;

// Presentation-only interpolation. Never feeds collision, AI or saved state.
public sealed class EntityVisualMotion
{
    private bool initialized;
    private Vector3 start,target,current;
    private float elapsed;
    public Vector3 Step(Vector3 next,float delta,float interval)
    {
        if(!initialized||(next-target).sqrMagnitude>64)
        {initialized=true;start=target=current=next;elapsed=0;return current;}
        if((next-target).sqrMagnitude>.00000001f)
        {start=current;target=next;elapsed=0;}
        elapsed+=Mathf.Max(0,delta);
        current=Vector3.Lerp(start,target,Mathf.Clamp01(elapsed/Mathf.Max(.001f,interval)));
        return current;
    }
}
