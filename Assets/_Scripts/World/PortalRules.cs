using System;
using UnityEngine;

// Beta portals have a 2x3 interior; corners of the obsidian frame are optional.
public static class PortalRules
{
    public sealed class Frame
    {
        public Vector3Int bottom,along;
        public byte State => (byte)(along.x==0?1:0);
        public Vector3 Center => (Vector3)bottom + (Vector3)along*.5f + new Vector3(.5f,0,.5f);
    }
    public static bool FindFrame(Func<Vector3Int,BlockType> get,Vector3Int inside,bool lit,out Frame frame)
    {
        foreach(var axis in new[]{Vector3Int.right,new Vector3Int(0,0,1)})
        for(int x=0;x<2;x++)for(int y=0;y<3;y++)
        {
            var bottom=inside-axis*x-Vector3Int.up*y;bool valid=true;
            for(int u=-1;valid&&u<=2;u++)for(int v=-1;valid&&v<=3;v++)
            {
                bool side=u==-1||u==2,edge=v==-1||v==3;
                if(side&&edge)continue;
                var type=get(bottom+axis*u+Vector3Int.up*v);
                if(side||edge)valid=type==BlockType.Obsidian;
                else valid=lit?type==BlockType.NetherPortal:type==BlockType.Air||type==BlockType.Fire||type==BlockType.NetherPortal;
            }
            if(valid){frame=new Frame{bottom=bottom,along=axis};return true;}
        }
        frame=null;return false;
    }
    public static bool IsPortal(Func<Vector3Int,BlockType> get,Vector3 position)
    {
        var p=Vector3Int.FloorToInt(position);
        return get(p)==BlockType.NetherPortal||get(p+Vector3Int.up)==BlockType.NetherPortal;
    }
}
