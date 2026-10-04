using System;

public sealed class BetaImprovedNoise
{
    private readonly int[] permutations = new int[512];
    private readonly double offsetX, offsetY, offsetZ;

    public BetaImprovedNoise(JavaRandom random)
    {
        offsetX = random.NextDouble() * 256.0;
        offsetY = random.NextDouble() * 256.0;
        offsetZ = random.NextDouble() * 256.0;

        for (int i = 0; i < 256; i++) permutations[i] = i;
        for (int i = 0; i < 256; i++)
        {
            int j = random.NextInt(256 - i) + i;
            int t = permutations[i];
            permutations[i] = permutations[j];
            permutations[j] = t;
            permutations[i + 256] = permutations[i];
        }
    }

    public double Sample(double x, double y, double z)
    {
        x += offsetX; y += offsetY; z += offsetZ;
        int xi0 = FastFloor(x), yi0 = FastFloor(y), zi0 = FastFloor(z);
        int xi = xi0 & 255, yi = yi0 & 255, zi = zi0 & 255;
        x -= xi0; y -= yi0; z -= zi0;

        double u = Fade(x), v = Fade(y), w = Fade(z);
        int a = permutations[xi] + yi;
        int aa = permutations[a] + zi;
        int ab = permutations[a + 1] + zi;
        int b = permutations[xi + 1] + yi;
        int ba = permutations[b] + zi;
        int bb = permutations[b + 1] + zi;

        return Lerp(w,
            Lerp(v,
                Lerp(u, Grad(permutations[aa], x, y, z), Grad(permutations[ba], x - 1, y, z)),
                Lerp(u, Grad(permutations[ab], x, y - 1, z), Grad(permutations[bb], x - 1, y - 1, z))),
            Lerp(v,
                Lerp(u, Grad(permutations[aa + 1], x, y, z - 1), Grad(permutations[ba + 1], x - 1, y, z - 1)),
                Lerp(u, Grad(permutations[ab + 1], x, y - 1, z - 1), Grad(permutations[bb + 1], x - 1, y - 1, z - 1))));
    }

    public double Sample2D(double x, double z)
    {
        x += offsetX;
        z += offsetZ;
        int xi0 = FastFloor(x), zi0 = FastFloor(z);
        int xi = xi0 & 255, zi = zi0 & 255;
        x -= xi0; z -= zi0;

        double u = Fade(x), w = Fade(z);
        int a = permutations[permutations[xi] & 255] + zi;
        int b = permutations[permutations[(xi + 1) & 255] & 255] + zi;

        double n0 = Lerp(u,
            Grad(permutations[a & 255], x, 0.0, z),
            Grad(permutations[b & 255], x - 1.0, 0.0, z));
        double n1 = Lerp(u,
            Grad(permutations[(a + 1) & 255], x, 0.0, z - 1.0),
            Grad(permutations[(b + 1) & 255], x - 1.0, 0.0, z - 1.0));
        return Lerp(w, n0, n1);
    }

    private static int FastFloor(double v) => v >= 0 ? (int)v : (int)v - 1;
    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static double Lerp(double t, double a, double b) => a + t * (b - a);
    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}

public sealed class BetaOctaveNoise
{
    private readonly BetaImprovedNoise[] octaves;

    public BetaOctaveNoise(JavaRandom random, int count)
    {
        octaves = new BetaImprovedNoise[count];
        for (int i = 0; i < count; i++) octaves[i] = new BetaImprovedNoise(random);
    }

    public double Sample(double x, double y, double z, double scaleX, double scaleY, double scaleZ)
    {
        double result = 0.0;
        double octaveScale = 1.0;
        for (int i = 0; i < octaves.Length; i++)
        {
            result += octaves[i].Sample(
                x * scaleX * octaveScale,
                y * scaleY * octaveScale,
                z * scaleZ * octaveScale) / octaveScale;
            octaveScale *= 0.5;
        }
        return result;
    }

    // Beta's scale/depth generators use the old fixed 2D path, whose gradient
    // is evaluated with Y=0 rather than by sampling an arbitrary 3D slice.
    public double Sample2D(double x, double z, double scaleX, double scaleZ)
    {
        double result = 0.0;
        double octaveScale = 1.0;
        for (int i = 0; i < octaves.Length; i++)
        {
            result += octaves[i].Sample2D(
                x * scaleX * octaveScale,
                z * scaleZ * octaveScale) / octaveScale;
            octaveScale *= 0.5;
        }
        return result;
    }
}
