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
        int xi0 = BetaFloor(x), yi0 = BetaFloor(y), zi0 = BetaFloor(z);
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

    // Beta's scalar two-argument overload samples (x, second, 0) with
    // all three permutation offsets. It is not the sizeY==1 bulk shortcut.
    public double Sample2D(double x, double z) => Sample(x, z, 0.0);

    // NoiseGeneratorPerlin casts to int and decrements only when the source
    // value is strictly less than that truncation. This preserves exact
    // negative integers (unlike the common v < 0 ? (int)v - 1 shortcut).
    private static int BetaFloor(double v)
    {
        return BetaMathHelper.Floor(v);
    }

    private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static double Lerp(double t, double a, double b) => a + t * (b - a);
    private static double Grad(int hash, double x, double y, double z)
    {
        int h = hash & 15;
        double u = h < 8 ? x : y;
        double v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    public void AddNoise(
        double[] buffer,
        double startX, double startY, double startZ,
        int sizeX, int sizeY, int sizeZ,
        double scaleX, double scaleY, double scaleZ,
        double octaveScale)
    {
        int index = 0;
        double amplitude = 1.0 / octaveScale;

        if (sizeY == 1)
        {
            for (int xIndex = 0; xIndex < sizeX; xIndex++)
            {
                double x = (startX + xIndex) * scaleX + offsetX;
                int xi0 = BetaFloor(x);
                int xi = xi0 & 255;
                x -= xi0;
                double u = Fade(x);

                for (int zIndex = 0; zIndex < sizeZ; zIndex++)
                {
                    double z = (startZ + zIndex) * scaleZ + offsetZ;
                    int zi0 = BetaFloor(z);
                    int zi = zi0 & 255;
                    z -= zi0;
                    double w = Fade(z);

                    int a = permutations[xi];
                    int aa = permutations[a] + zi;
                    int b = permutations[xi + 1];
                    int ba = permutations[b] + zi;

                    double n0 = Lerp(u,
                        Grad2D(permutations[aa], x, z),
                        Grad(permutations[ba], x - 1.0, 0.0, z));
                    double n1 = Lerp(u,
                        Grad(permutations[aa + 1], x, 0.0, z - 1.0),
                        Grad(permutations[ba + 1], x - 1.0, 0.0, z - 1.0));

                    buffer[index++] += Lerp(w, n0, n1) * amplitude;
                }
            }
            return;
        }

        int lastYCell = -1;
        double x00 = 0.0, x10 = 0.0, x01 = 0.0, x11 = 0.0;

        for (int xIndex = 0; xIndex < sizeX; xIndex++)
        {
            double x = (startX + xIndex) * scaleX + offsetX;
            int xi0 = BetaFloor(x);
            int xi = xi0 & 255;
            x -= xi0;
            double u = Fade(x);

            for (int zIndex = 0; zIndex < sizeZ; zIndex++)
            {
                double z = (startZ + zIndex) * scaleZ + offsetZ;
                int zi0 = BetaFloor(z);
                int zi = zi0 & 255;
                z -= zi0;
                double w = Fade(z);

                for (int yIndex = 0; yIndex < sizeY; yIndex++)
                {
                    double y = (startY + yIndex) * scaleY + offsetY;
                    int yi0 = BetaFloor(y);
                    int yi = yi0 & 255;
                    y -= yi0;
                    double v = Fade(y);

                    if (yIndex == 0 || yi != lastYCell)
                    {
                        lastYCell = yi;

                        int a = permutations[xi] + yi;
                        int aa = permutations[a] + zi;
                        int ab = permutations[a + 1] + zi;
                        int b = permutations[xi + 1] + yi;
                        int ba = permutations[b] + zi;
                        int bb = permutations[b + 1] + zi;

                        x00 = Lerp(u,
                            Grad(permutations[aa], x, y, z),
                            Grad(permutations[ba], x - 1.0, y, z));
                        x10 = Lerp(u,
                            Grad(permutations[ab], x, y - 1.0, z),
                            Grad(permutations[bb], x - 1.0, y - 1.0, z));
                        x01 = Lerp(u,
                            Grad(permutations[aa + 1], x, y, z - 1.0),
                            Grad(permutations[ba + 1], x - 1.0, y, z - 1.0));
                        x11 = Lerp(u,
                            Grad(permutations[ab + 1], x, y - 1.0, z - 1.0),
                            Grad(permutations[bb + 1], x - 1.0, y - 1.0, z - 1.0));
                    }

                    double n0 = Lerp(v, x00, x10);
                    double n1 = Lerp(v, x01, x11);
                    buffer[index++] += Lerp(w, n0, n1) * amplitude;
                }
            }
        }
    }

    private static double Grad2D(int hash, double x, double z)
    {
        int h = hash & 15;
        double a = (1 - ((h & 8) >> 3)) * x;
        double b = h < 4 ? 0.0 : (h == 12 || h == 14 ? x : z);
        return ((h & 1) == 0 ? a : -a) + ((h & 2) == 0 ? b : -b);
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

    public double[] GenerateNoiseOctaves(
        double[] buffer,
        double startX, double startY, double startZ,
        int sizeX, int sizeY, int sizeZ,
        double scaleX, double scaleY, double scaleZ)
    {
        int length = sizeX * sizeY * sizeZ;
        if (buffer == null || buffer.Length < length)
            buffer = new double[length];
        else
            Array.Clear(buffer, 0, buffer.Length);

        double octaveScale = 1.0;
        for (int i = 0; i < octaves.Length; i++)
        {
            octaves[i].AddNoise(
                buffer,
                startX, startY, startZ,
                sizeX, sizeY, sizeZ,
                scaleX * octaveScale,
                scaleY * octaveScale,
                scaleZ * octaveScale,
                octaveScale);
            octaveScale *= 0.5;
        }

        return buffer;
    }

    public double[] Generate2D(
        double[] buffer,
        int startX, int startZ,
        int sizeX, int sizeZ,
        double scaleX, double scaleZ)
    {
        return GenerateNoiseOctaves(
            buffer,
            startX, 10.0, startZ,
            sizeX, 1, sizeZ,
            scaleX, 1.0, scaleZ);
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
