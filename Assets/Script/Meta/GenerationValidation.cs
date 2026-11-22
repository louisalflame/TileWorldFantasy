using System;
using UnityEngine;

public static class GenerationValidation
{
    public static int CellCount(int width, int height)
    {
        if (width <= 0 || height <= 0 || (long)width * height > int.MaxValue)
            throw new ArgumentOutOfRangeException("width", "Map dimensions must be positive and fit in an array.");
        return width * height;
    }

    public static void Map(Array map, int count, string name)
    {
        if (map == null || map.Length != count)
            throw new ArgumentException("Map length must match its dimensions.", name);
    }

    public static void Finite(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            throw new ArgumentException("Value must be finite.", name);
    }

    public static void UnitValue(float value, string name)
    {
        Finite(value, name);
        if (value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name, "Value must be between 0 and 1.");
    }

    public static void Noise(int octaves, int growth)
    {
        if (octaves <= 0 || growth <= 0)
            throw new ArgumentException("Noise octave count and frequency growth must be positive.");
    }

    public static void Rain(RainParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        UnitValue(parameter.HEIGHT_LOCATIONS, "river source fraction");
        UnitValue(parameter.RIVER_END_HEIGHT, "river end height");
        Finite(parameter.RIVER_AVERAGE_FACTOR, "river average factor");
        if (parameter.RIVER_LENGTH < 0 || parameter.RIVER_AVERAGE_FACTOR <= 0f)
            throw new ArgumentException("River length must be nonnegative and average factor must be positive.");
    }

    public static void Population(PopulationParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        UnitValue(parameter.BasicProbability, "basic population probability");
        Probabilities(parameter.HeightProbability, parameter.HeightVariety, "height probabilities");
        Probabilities(parameter.HumidityProbability, parameter.HumidityVariety, "humidity probabilities");
        Probabilities(parameter.TemperatureProbability, parameter.TemperatureVariety, "temperature probabilities");
    }

    private static void Probabilities(float[] values, int count, string name)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(name, "Classification count must be positive.");
        Map(values, count, name);
        foreach (var value in values) UnitValue(value, name);
    }

    public static void Mana(ManaParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("parameter");
        RandomPoints(parameter.RANDOM_POINT_GEN_PARA);
        var counts = new[] { parameter.POSITIVE_HIGH_POINTS_NUM, parameter.POSITIVE_MID_POINTS_NUM,
            parameter.POSITIVE_LOW_POINTS_NUM, parameter.NEGATIVE_HIGH_POINTS_NUM,
            parameter.NEGATIVE_MID_POINTS_NUM, parameter.NEGATIVE_LOW_POINTS_NUM };
        long total = 0;
        foreach (var count in counts)
        {
            if (count < 0) throw new ArgumentException("Mana point counts must be nonnegative.");
            total += count;
        }
        if (total > int.MaxValue) throw new ArgumentException("Too many mana points.");
        foreach (var radius in new[] { parameter.POSITIVE_HIGH_RADIUS, parameter.POSITIVE_MID_RADIUS,
            parameter.POSITIVE_LOW_RADIUS, parameter.NEGATIVE_HIGH_RADIUS,
            parameter.NEGATIVE_MID_RADIUS, parameter.NEGATIVE_LOW_RADIUS })
        {
            Finite(radius, "mana radius");
            if (radius < 0f) throw new ArgumentException("Mana radii must be nonnegative.");
        }
        foreach (var factor in new[] { parameter.POSITIVE_HIGH_FACTOR, parameter.POSITIVE_MID_FACTOR,
            parameter.POSITIVE_LOW_FACTOR, parameter.NEGATIVE_HIGH_FACTOR,
            parameter.NEGATIVE_MID_FACTOR, parameter.NEGATIVE_LOW_FACTOR }) Finite(factor, "mana factor");
    }

    public static void RandomPoints(RandomPointParameter parameter)
    {
        if (parameter == null) throw new ArgumentNullException("random point parameter");
        Noise(parameter.LOCAL_FREQ_COUNT_TIMES, parameter.LOCAL_FREQ_GROW_FACTOR);
    }
}
