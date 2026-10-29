using UnityEngine;

public partial class World
{
    [System.NonSerialized] private Plane[] rebuildPlanes;
    private Camera rebuildCamera;
    private int priorityDispatches;

    private void PrepareRebuildPriority()
    {
        if(rebuildCamera==null)rebuildCamera=Camera.main;
        if(rebuildCamera==null)return;
        if(rebuildPlanes==null)rebuildPlanes=new Plane[6];
        GeometryUtility.CalculateFrustumPlanes(rebuildCamera,rebuildPlanes);
    }

    private int SelectNextRebuild()
    {
        // Reserve every eighth selection for queue order so distant work
        // continues even during sustained nearby edits. Never drop requests.
        if(rebuildCamera==null || ++priorityDispatches>=8)
        {priorityDispatches=0;return 0;}
        var eye=rebuildCamera.transform.position;
        int best=-1;float bestDistance=float.MaxValue;bool bestVisible=false;
        for(int i=0;i<chunksToUpdate.Count;i++)
        {
            var chunk=chunksToUpdate[i];
            if(!IsLiveChunk(chunk))return i; // discard stale entries promptly
            var center=(Vector3)chunk.ChunkData.worldPos+new Vector3(chunkSize*.5f,worldHeight*.5f,chunkSize*.5f);
            var bounds=new Bounds(center,new Vector3(chunkSize,worldHeight,chunkSize));
            // Distance to bounds, not the column center: tall columns must not
            // appear far away merely because the player is near the ground.
            float distance=(bounds.ClosestPoint(eye)-eye).sqrMagnitude;
            bool visible=GeometryUtility.TestPlanesAABB(rebuildPlanes,bounds);
            if(best<0||RenderRebuildPriority.IsBetter(distance,visible,bestDistance,bestVisible))
            {best=i;bestDistance=distance;bestVisible=visible;}
        }
        return best<0?0:best;
    }
}
