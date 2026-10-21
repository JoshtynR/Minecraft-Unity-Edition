using UnityEngine;

public enum WorldDimension { Nether = -1, Overworld = 0 }

public static class DimensionRules
{
    public static WorldDimension Valid(WorldDimension value) => value == WorldDimension.Nether ? value : WorldDimension.Overworld;
    public static string StorageName(string name, WorldDimension dimension) => dimension == WorldDimension.Nether ? name + "/DIM-1" : name;
    public static Vector3 Destination(Vector3 source, WorldDimension target)
    {
        float scale = target == WorldDimension.Nether ? .125f : 8;
        return new Vector3(Mathf.Clamp(source.x * scale,-29999872,29999872),source.y,Mathf.Clamp(source.z * scale,-29999872,29999872));
    }
}
