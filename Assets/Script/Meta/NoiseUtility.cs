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
        float freqGrowFactor)
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