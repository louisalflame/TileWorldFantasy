using UnityEngine;

public static class NoiseUtility
{
    // Prepared once per generation; no parameter validation or weight setup per cell.
    public sealed class PreparedFractal
    {
        private readonly float[] _frequencies;
        private readonly float[] _weights;
        private readonly float _weightSum;

        public PreparedFractal(float scale, int octaves, float lacunarity, float persistence)
        {
            _frequencies = new float[octaves];
            _weights = new float[octaves];
            float frequency = scale, weight = 1f;
            for (int i = 0; i < octaves; i++)
            {
                _frequencies[i] = frequency;
                _weights[i] = weight;
                _weightSum += weight;
                frequency *= lacunarity;
                weight *= persistence;
            }
        }

        public float Sample(float x, float y, float xOffset, float yOffset, bool ridged = false)
        {
            float result = 0f;
            for (int i = 0; i < _frequencies.Length; i++)
            {
                float value = Mathf.PerlinNoise(x * _frequencies[i] + xOffset, y * _frequencies[i] + yOffset);
                if (ridged) value = 1f - Mathf.Abs(2f * value - 1f);
                result += value * _weights[i];
            }
            return Mathf.Clamp01(result / _weightSum);
        }
    }

    public static float CountRecursivePerlinNoise(
        float xCoord,
        float yCoord,
        float xOffset,
        float yOffset,
        float scale,
        int freqCountTimes,
        int freqGrowFactor)
    {
        GenerationValidation.Noise(freqCountTimes, freqGrowFactor);
        float sample = 0f;
        float freq = 1f;
        float sampleTimes = 0f;

        for (int i = 0; i < freqCountTimes; i++)
        {
            var x = scale * xCoord * freq + xOffset;
            var y = scale * yCoord * freq + yOffset;

            sample += (1 / freq) * Mathf.PerlinNoise(x, y);

            sampleTimes += (1 / freq);
            freq *= freqGrowFactor;
        }
        sample /= sampleTimes;

        // Unity PerlinNoise can slightly exceed [0, 1], including after octave averaging.
        // Keep the normalized-noise contract here, before humidity reaches biome lookup.
        // Clamp01 does not turn NaN into a valid sample; downstream validation still rejects it.
        return Mathf.Clamp01(sample);
    }
}

public static class MathUtility
{
    public static float CountPow(float number, float pow)
    {
        var ans = 1f;
        while (pow >= 1)
        {
            ans *= number;
            pow -= 1;
        }

        var floatPow = 0.5f;
        var root = Mathf.Sqrt(number);
        while (pow >= 0.05f)
        {
            if (pow >= floatPow)
            {
                ans *= root;
                pow -= floatPow;
            }
            floatPow /= 2;
            root = Mathf.Sqrt(root);
        }

        return ans;
    }

    public static int MapIndex(int x, int y, int height)
    {
        return x * height + y;
    }
}

public static class SettingUtility
{
    public const int MapRestCount = 20000;
}
