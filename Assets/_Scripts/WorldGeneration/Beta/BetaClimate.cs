using System;

/// <summary>Beta 1.7.3 WorldChunkManager climate path.</summary>
public sealed class BetaClimate
{
    private readonly SimplexOctaves temperature;
    private readonly SimplexOctaves humidity;
    private readonly SimplexOctaves precipitation;

    public BetaClimate(long seed)
    {
        temperature = new SimplexOctaves(unchecked(seed * 9871L), 4);
        humidity = new SimplexOctaves(unchecked(seed * 39811L), 4);
        precipitation = new SimplexOctaves(unchecked(seed * 543321L), 2);
    }

    public void Sample(double x, double z, out double temp, out double humid)
    {
        SampleRegion((int)x, (int)z, 1, 1, out double[] t, out double[] h);
        temp = t[0]; humid = h[0];
    }

    // Mirrors WorldChunkManager.loadBlockGeneratorData: generate all three
    // simplex buffers first, then blend them sequentially in buffer order.
    public void SampleRegion(int startX, int startZ, int width, int depth,
        out double[] temperatures, out double[] humidities)
    {
        temperatures = temperature.Generate(null, startX, startZ, width, depth,
            0.02500000037252903, 0.02500000037252903, 0.25);
        humidities = humidity.Generate(null, startX, startZ, width, depth,
            0.05000000074505806, 0.05000000074505806, 1.0 / 3.0);
        double[] precipitationValues = precipitation.Generate(null, startX, startZ, width, depth,
            0.25, 0.25, 0.5882352941176471);

        int count = width * depth;
        for (int i = 0; i < count; i++)
        {
            double p = precipitationValues[i] * 1.1 + 0.5;
            double t = (temperatures[i] * 0.15 + 0.7) * 0.99 + p * 0.01;
            double h = (humidities[i] * 0.15 + 0.5) * 0.998 + p * 0.002;
            t = 1.0 - (1.0 - t) * (1.0 - t);
            temperatures[i] = Clamp01(t);
            humidities[i] = Clamp01(h);
        }
    }

    private static double Clamp01(double v) => v < 0.0 ? 0.0 : (v > 1.0 ? 1.0 : v);

    private sealed class SimplexOctaves
    {
        private readonly Simplex2D[] octaves;
        public SimplexOctaves(long seed, int count)
        {
            var random = new JavaRandom(seed);
            octaves = new Simplex2D[count];
            for (int i = 0; i < count; i++) octaves[i] = new Simplex2D(random);
        }

        public double[] Generate(double[] output, double startX, double startZ,
            int width, int depth, double scaleX, double scaleZ, double frequencyMultiplier)
        {
            scaleX /= 1.5;
            scaleZ /= 1.5;
            int count = width * depth;
            if (output == null || output.Length < count) output = new double[count];
            else Array.Clear(output, 0, output.Length);

            double amplitudeDenominator = 1.0;
            double frequency = 1.0;
            for (int octave = 0; octave < octaves.Length; octave++)
            {
                octaves[octave].Add(output, startX, startZ, width, depth,
                    scaleX * frequency, scaleZ * frequency, 0.55 / amplitudeDenominator);
                frequency *= frequencyMultiplier;
                amplitudeDenominator *= 0.5;
            }
            return output;
        }
    }

    private sealed class Simplex2D
    {
        private const double F2 = 0.3660254037844386;
        private const double G2 = 0.21132486540518713;
        private static readonly int[,] Grad = {
            {1,1},{-1,1},{1,-1},{-1,-1},{1,0},{-1,0},{1,0},{-1,0},
            {0,1},{0,-1},{0,1},{0,-1}
        };
        private readonly int[] perm = new int[512];
        private readonly double xo, zo;

        public Simplex2D(JavaRandom random)
        {
            xo = random.NextDouble() * 256.0;
            zo = random.NextDouble() * 256.0;
            _ = random.NextDouble();
            for (int i = 0; i < 256; i++) perm[i] = i;
            for (int i = 0; i < 256; i++)
            {
                int j = random.NextInt(256 - i) + i;
                int t = perm[i]; perm[i] = perm[j]; perm[j] = t;
                perm[i + 256] = perm[i];
            }
        }

        public void Add(double[] output, double startX, double startZ, int width, int depth,
            double scaleX, double scaleZ, double amplitude)
        {
            int index = 0;
            for (int x = 0; x < width; x++)
            {
                double xin = (startX + x) * scaleX + xo;
                for (int z = 0; z < depth; z++)
                {
                    double yin = (startZ + z) * scaleZ + zo;
                    output[index++] += Evaluate(xin, yin) * amplitude;
                }
            }
        }

        private double Evaluate(double xin, double yin)
        {
            double skew = (xin + yin) * F2;
            int i = BetaWrap(xin + skew), j = BetaWrap(yin + skew);
            double unskew = (i + j) * G2;
            double x0 = xin - (i - unskew), y0 = yin - (j - unskew);
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; } else { i1 = 0; j1 = 1; }
            double x1 = x0 - i1 + G2, y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2, y2 = y0 - 1.0 + 2.0 * G2;
            int ii = i & 255, jj = j & 255;
            int gi0 = perm[ii + perm[jj]] % 12;
            int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
            int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;
            return 70.0 * (Corner(gi0, x0, y0) + Corner(gi1, x1, y1) + Corner(gi2, x2, y2));
        }

        private static double Corner(int gi, double x, double y)
        {
            double t = 0.5 - x * x - y * y;
            if (t < 0.0) return 0.0;
            t *= t;
            return t * t * (Grad[gi, 0] * x + Grad[gi, 1] * y);
        }
        private static int BetaWrap(double v) => v > 0.0
            ? BetaMathHelper.JavaInt(v) : unchecked(BetaMathHelper.JavaInt(v) - 1);
    }
}
