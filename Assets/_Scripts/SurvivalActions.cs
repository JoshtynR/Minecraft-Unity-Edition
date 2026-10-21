using System;

public static class FoodRules
{
    public const float EatSeconds = 1.6f;
    public static bool TryGet(ItemType item, out int food, out float saturation)
    {
        float modifier;
        switch(item)
        {
            case ItemType.Apple: food=4;modifier=.3f;break;
            case ItemType.GoldenApple: food=4;modifier=1.2f;break;
            case ItemType.Bread: food=5;modifier=.6f;break;
            case ItemType.MushroomStew: food=6;modifier=.6f;break;
            case ItemType.RawPorkchop: food=3;modifier=.3f;break;
            case ItemType.CookedPorkchop: food=8;modifier=.8f;break;
            case ItemType.RawFish: food=2;modifier=.3f;break;
            case ItemType.CookedFish: food=5;modifier=.6f;break;
            case ItemType.Cookie: food=2;modifier=.1f;break;
            default: food=0;saturation=0;return false;
        }
        saturation=food*modifier*2;return true;
    }
}

// Track downward travel rather than time in the air. Upward jumps add nothing.
public sealed class FallTracker
{
    private float previousY,distance;
    private bool known;
    public void Reset(){known=false;distance=0;}
    public int Step(float y,bool grounded,bool immune)
    {
        if(float.IsNaN(y)||float.IsInfinity(y)){Reset();return 0;}
        if(!known||immune||Math.Abs(y-previousY)>8)
        {previousY=y;distance=0;known=true;return 0;}
        distance+=Math.Max(0,previousY-y);previousY=y;
        if(!grounded)return 0;
        int damage=Math.Max(0,(int)Math.Ceiling(distance-3-.0001f));distance=0;return damage;
    }
}

