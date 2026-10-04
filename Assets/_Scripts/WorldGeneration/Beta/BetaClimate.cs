using System;

/// <summary>
/// Beta 1.7.3 climate sampler using the historical 2D simplex octave path.
/// </summary>
public sealed class BetaClimate
{
    private readonly SimplexOctaves temperature;
    private readonly SimplexOctaves humidity;
    private readonly SimplexOctaves precipitation;

    public BetaClimate(long seed)
    {
        temperature = new SimplexOctaves(unchecked(seed * 9871L), 4, 0.25, 0.5);
        humidity = new SimplexOctaves(unchecked(seed * 39811L), 4, 1.0 / 3.0, 0.5);
        precipitation = new SimplexOctaves(unchecked(seed * 543321L), 2, 0.5882352941176471, 0.5);
    }

    public void Sample(double x, double z, out double temp, out double humid)
    {
        double t = temperature.Sample(x, z, 0.02500000037252903, 0.02500000037252903);
        double h = humidity.Sample(x, z, 0.05000000074505806, 0.05000000074505806);
        double p = precipitation.Sample(x, z, 0.25, 0.25);

        double precipitationValue = p * 1.1 + 0.5;
        temp = (t * 0.15 + 0.7) * 0.99 + precipitationValue * 0.01;
        temp = 1.0 - (1.0 - temp) * (1.0 - temp);
        humid = (h * 0.15 + 0.5) * 0.998 + precipitationValue * 0.002;

        temp = Clamp01(temp);
        humid = Clamp01(humid);
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    private sealed class SimplexOctaves
    {
        private readonly Simplex2D[] octaves;
        private readonly double frequencyMultiplier;
        private readonly double amplitudeMultiplier;

        public SimplexOctaves(long seed, int count, double frequencyMultiplier, double amplitudeMultiplier)
        {
            this.frequencyMultiplier = frequencyMultiplier;
            this.amplitudeMultiplier = amplitudeMultiplier;
            var random = new JavaRandom(seed);
            octaves = new Simplex2D[count];
            for (int i = 0; i < count; i++) octaves[i] = new Simplex2D(random);
        }

        public double Sample(double x, double z, double scaleX, double scaleZ)
        {
            scaleX /= 1.5;
            scaleZ /= 1.5;

            double sum = 0.0;
            double frequency = 1.0;
            double amplitudeDenominator = 1.0;
            for (int i = 0; i < octaves.Length; i++)
            {
                sum += octaves[i].Sample(
                    x * scaleX * frequency,
                    z * scaleZ * frequency) * (0.55 / amplitudeDenominator);
                frequency *= frequencyMultiplier;
                amplitudeDenominator *= amplitudeMultiplier;
            }
            return sum;
        }
    }

    private sealed class Simplex2D
    {
        private const double F2 = 0.3660254037844386;
        private const double G2 = 0.21132486540518713;
        private static readonly int[,] Grad =
        {
            {1,1},{-1,1},{1,-1},{-1,-1},{1,0},{-1,0},{1,0},{-1,0},
            {0,1},{0,-1},{0,1},{0,-1}
        };

        private readonly int[] perm = new int[512];
        private readonly double xo, zo;

        public Simplex2D(JavaRandom random)
        {
            // NoiseGenerator2 consumes three offsets, but its 2D path uses the
            // first two (field_4313_a and field_4312_b), not X and Z from the
            // 3D Perlin convention.
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

        public double Sample(double xin, double yin)
        {
            xin += xo;
            yin += zo;
            double skew = (xin + yin) * F2;
            int i = BetaWrap(xin + skew);
            int j = BetaWrap(yin + skew);
            double unskew = (i + j) * G2;
            double x0 = xin - (i - unskew);
            double y0 = yin - (j - unskew);

            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }

            double x1 = x0 - i1 + G2;
            double y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2;
            double y2 = y0 - 1.0 + 2.0 * G2;

            int ii = i & 255, jj = j & 255;
            int gi0 = perm[ii + perm[jj]] % 12;
            int gi1 = perm[ii + i1 + perm[jj + j1]] % 12;
            int gi2 = perm[ii + 1 + perm[jj + 1]] % 12;

            return 70.0 * (Corner(gi0, x0, y0) + Corner(gi1, x1, y1) + Corner(gi2, x2, y2));
        }

        private static double Corner(int gi, double x, double y)
        {
            double t = 0.5 - x * x - y * y;
            if (t < 0) return 0.0;
            t *= t;
            return t * t * (Grad[gi, 0] * x + Grad[gi, 1] * y);
        }

        // Preserve NoiseGenerator2.wrap exactly, including its unusual handling
        // of zero and exact negative integers.
        private static int BetaWrap(double v) => v > 0.0 ? (int)v : (int)v - 1;
    }
}
