using System;
using System.Globalization;
using System.IO;
class LightmapCheck
{
    static void Main(string[] args)
    {
        int count=0;
        foreach(var line in File.ReadLines(args[0]))
        {
            var fields=line.Split('\t');
            var rgb=LightmapLighting.Sample(int.Parse(fields[4]),int.Parse(fields[5]),
                float.Parse(fields[1],CultureInfo.InvariantCulture),float.Parse(fields[2],CultureInfo.InvariantCulture),
                fields[0]=="1",float.Parse(fields[3],CultureInfo.InvariantCulture));
            int packed=((int)Math.Round(rgb.r*255)<<16)|((int)Math.Round(rgb.g*255)<<8)|(int)Math.Round(rgb.b*255);
            if(packed!=int.Parse(fields[6]))throw new Exception("Lightmap differs from reference: "+line+" got "+packed);
            count++;
        }
        if(LightmapLighting.SunBrightness(0)!=1||LightmapLighting.SunBrightness(180)!=.2f)throw new Exception("Day/night sun endpoints");
        var torch=LightmapLighting.Sample(0,10);
        if(!(torch.r>torch.g&&torch.g>torch.b))throw new Exception("Torch color must be warm");
        var moon=LightmapLighting.Sample(15,0,.2f);
        if(!(moon.b>moon.r))throw new Exception("Night skylight must be cool");
        Console.WriteLine(count+" reference lightmap samples match across sky/block levels, daylight, gamma, dimensions and flicker.");
    }
}
