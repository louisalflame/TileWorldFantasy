using UnityEngine;

public static class NoiseUtility
{
    public static float CountRecursivePerlinNoise(
        float xCoord,
        float yCoord,
        float xOffset,
        float yOffset,
        float scale,
        int freqCountTimes,
        int freqGrowFactor)
    {
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

        return sample;
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
}

public static class SettingUtility
{
    public const int MapRestCount = 2000;
}