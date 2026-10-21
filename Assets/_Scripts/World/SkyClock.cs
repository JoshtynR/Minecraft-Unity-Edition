using System;

public static class SkyClock
{
    public const double DayTicks=24000;
    public static float Angle(double ticks)
    {
        double phase=((ticks%DayTicks+DayTicks)%DayTicks)/DayTicks-.25;
        if(phase<0)phase++;
        double eased=1-(Math.Cos(phase*Math.PI)+1)/2;
        return (float)((phase+(eased-phase)/3)*360);
    }
    public static float Daylight(double ticks)
    {return (float)Math.Max(0,Math.Min(1,Math.Cos(Angle(ticks)*Math.PI/180)*2+.5));}
}
