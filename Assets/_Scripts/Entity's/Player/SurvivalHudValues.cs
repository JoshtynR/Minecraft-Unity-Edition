using System;

public static class SurvivalHudValues
{
    public static int ClampPoints(int points) { return Math.Max(0, Math.Min(20, points)); }
    public static int IconFill(int points, int index)
    {
        return Math.Max(0, Math.Min(2, ClampPoints(points) - index * 2));
    }
    public static int ExperiencePixels(float progress)
    {
        if (float.IsNaN(progress) || progress <= 0) return 0;
        if (progress >= 1) return 182;
        return Math.Min(182, (int)(progress * 183));
    }
}

// Presentation clock only: it never consumes world RNG or changes health/food.
public sealed class SurvivalHudAnimation
{
    private int health=-1,lastChange,flashUntil;
    public int PreviousHealth {get;private set;}
    public void Reset(int value,int tick){health=PreviousHealth=value;lastChange=tick;flashUntil=tick;}
    public void Observe(int value,int tick)
    {
        if(health<0){Reset(value,tick);return;}
        if(health==0&&value>0){Reset(value,tick);return;}
        if(value!=health)
        {
            if(tick-lastChange>20)PreviousHealth=health;
            flashUntil=tick+(value<health?20:10);lastChange=tick;health=value;
        }
        if(tick-lastChange>20)PreviousHealth=health;
    }
    public bool Flash(int tick)=>tick<flashUntil&&(flashUntil-tick)/3%2==1;
    private static int Noise(int tick,int index,int salt,int bound)
    {
        uint n=unchecked((uint)(tick*312871+index*127+salt));
        n^=n<<13;n^=n>>17;n^=n<<5;return (int)(n%(uint)bound);
    }
    public static int HeartOffset(int points,int tick,int index)=>points<=4?-Noise(tick,index,19,2):0;
    public static int FoodOffset(int points,float saturation,int tick,int index)
        =>saturation<=0&&tick%(SurvivalHudValues.ClampPoints(points)*3+1)==0?Noise(tick,index,43,3)-1:0;
}
