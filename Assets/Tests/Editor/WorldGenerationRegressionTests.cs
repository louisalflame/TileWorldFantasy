using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Utility;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

public class WorldGenerationRegressionTests
{
    private readonly List<Object> _objects = new List<Object>();
    private Random.State _randomState;
    private string _directory;

    [SetUp]
    public void SetUp()
    {
        _randomState = Random.state;
        Random.InitState(12345);
        _directory = Path.Combine(Path.GetTempPath(), "TileWorldFantasy-Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var item in _objects) if (item != null) Object.DestroyImmediate(item);
        _objects.Clear();
        Random.state = _randomState;
        Directory.Delete(_directory, true);
    }

    private T Create<T>() where T : ScriptableObject
    {
        var item = ScriptableObject.CreateInstance<T>();
        _objects.Add(item);
        return item;
    }

    private static void Drain(IEnumerator iterator)
    {
        using (var coroutine = new Utility.Coroutine(iterator))
        {
            for (var i = 0; !coroutine.Finished; i++)
            {
                if (i > 10000) Assert.Fail("Generation did not finish.");
                coroutine.Resume(1f / 60f);
            }
        }
    }

    private static object Invoke(object target, string name, params object[] args)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private TerrainParameter Terrain()
    {
        var parameter = Create<TerrainParameter>();
        parameter.USE_LAYERED_TERRAIN = false;
        parameter.WORLD_SCALE = 24f;
        parameter.SCALE = 5f;
        parameter.LOW_GROUND_FACTOR = 1f;
        parameter.HIGH_MOUNTAIN_FACTOR = 1f;
        parameter.FREQ_COUNT_TIMES = 3;
        parameter.FREQ_GROW_FACTOR = 2;
        parameter.RANDOM_POINT_GEN_PARA = Create<RandomPointParameter>();
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_FREQ_COUNT_TIMES = 1;
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_FREQ_GROW_FACTOR = 2;
        parameter.OCEAN_BORDER_WIDTH = 2;
        parameter.COAST_TRANSITION_WIDTH = 3f;
        parameter.COAST_NOISE_AMPLITUDE = 2f;
        parameter.COAST_NOISE_SCALE = 8f;
        return parameter;
    }

    private static float[] GenerateTerrain(TerrainParameter parameter, int width, int height, float offset = 12.3f)
    {
        Random.InitState(12345);
        var result = new BlockMonad<float[]>(r => new TerrainGenerator().GenerateHeightMap(
            width, height, offset, 45.6f, parameter, r));
        Drain(result.Do());
        Assert.That(result.Error, Is.Null);
        return result.Result;
    }

    [TestCase(29, 19)]
    [TestCase(19, 29)]
    public void Terrain_GuaranteesOceanBandAndPreservesInterior(int width, int height)
    {
        var parameter = Terrain();
        var map = GenerateTerrain(parameter, width, height);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            float d = parameter.WORLD_SCALE * Mathf.Min(Mathf.Min((float)x / (width - 1), 1f - (float)x / (width - 1)),
                Mathf.Min((float)y / (height - 1), 1f - (float)y / (height - 1)));
            float value = map[MathUtility.MapIndex(x, y, height)];
            Assert.That(value, Is.InRange(0f, 1f));
            if (d < parameter.OCEAN_BORDER_WIDTH - 0.00001f)
            {
                Assert.That(value, Is.EqualTo(parameter.OCEAN_FLOOR_HEIGHT));
                Assert.That(value, Is.LessThan(parameter.SEA_LEVEL));
            }
            if (d > parameter.OCEAN_BORDER_WIDTH + parameter.COAST_TRANSITION_WIDTH + parameter.COAST_NOISE_AMPLITUDE)
            {
                float raw = NoiseUtility.CountRecursivePerlinNoise((float)x / width, (float)y / height,
                    12.3f, 45.6f, parameter.SCALE, parameter.FREQ_COUNT_TIMES, parameter.FREQ_GROW_FACTOR);
                Assert.That(value, Is.EqualTo(raw).Within(0.000001f));
            }
        }
        CollectionAssert.AreEqual(map, GenerateTerrain(parameter, width, height));
    }

    [TestCase(1, 1)]
    [TestCase(1, 9)]
    [TestCase(9, 1)]
    [TestCase(2, 5)]
    public void Terrain_TinyMapsRemainFiniteOcean(int width, int height)
    {
        var parameter = Terrain();
        Assert.That(GenerateTerrain(parameter, width, height),
            Is.All.EqualTo(parameter.OCEAN_FLOOR_HEIGHT));
    }

    [Test]
    public void Terrain_CoastNoiseChangesTransitionWithoutAffectingOceanOrInterior()
    {
        var parameter = Terrain();
        parameter.COAST_NOISE_AMPLITUDE = 0f;
        var plain = GenerateTerrain(parameter, 25, 19);
        parameter.COAST_NOISE_AMPLITUDE = 2f;
        var noisy = GenerateTerrain(parameter, 25, 19);
        Assert.That(noisy.Where((v, i) => Mathf.Abs(v - plain[i]) > 0.00001f).Any(), Is.True);
        Assert.That(noisy[0], Is.EqualTo(plain[0]));
        int center = MathUtility.MapIndex(12, 9, 19);
        Assert.That(noisy[center], Is.EqualTo(plain[center]));
        parameter.COAST_NOISE_SCALE = 3f;
        var rescaled = GenerateTerrain(parameter, 25, 19);
        Assert.That(rescaled.SequenceEqual(noisy), Is.False);
        parameter.OCEAN_FLOOR_HEIGHT = 0.1f;
        parameter.OCEAN_BORDER_WIDTH = 4;
        Assert.That(GenerateTerrain(parameter, 25, 19)[MathUtility.MapIndex(3, 8, 19)], Is.EqualTo(0.1f));
    }

    [Test]
    public void Terrain_NoiseFreeTransitionHasSmoothBlendAndConfigurableWidth()
    {
        var parameter = Terrain();
        parameter.COAST_NOISE_AMPLITUDE = 0f;
        parameter.COAST_TRANSITION_WIDTH = 4f;
        var map = GenerateTerrain(parameter, 25, 19);
        int x = 4, y = 9; // World distance 4: halfway after the fixed ocean width of 2.
        float raw = NoiseUtility.CountRecursivePerlinNoise((float)x / 25, (float)y / 19,
            12.3f, 45.6f, parameter.SCALE, parameter.FREQ_COUNT_TIMES, parameter.FREQ_GROW_FACTOR);
        Assert.That(map[MathUtility.MapIndex(x, y, 19)],
            Is.EqualTo(Mathf.Lerp(parameter.OCEAN_FLOOR_HEIGHT, raw, 0.5f)).Within(0.000001f));
        parameter.COAST_TRANSITION_WIDTH = 2f;
        Assert.That(GenerateTerrain(parameter, 25, 19)[MathUtility.MapIndex(x, y, 19)],
            Is.EqualTo(raw).Within(0.000001f));
    }

    [TestCase("SEA_LEVEL", 0f)]
    [TestCase("WORLD_SCALE", 0f)]
    [TestCase("WORLD_SCALE", float.NaN)]
    [TestCase("OCEAN_FLOOR_HEIGHT", 0.23f)]
    [TestCase("COAST_TRANSITION_WIDTH", 0f)]
    [TestCase("COAST_TRANSITION_WIDTH", float.NaN)]
    [TestCase("COAST_NOISE_AMPLITUDE", -1f)]
    [TestCase("COAST_NOISE_SCALE", float.PositiveInfinity)]
    [TestCase("LOW_GROUND_FACTOR", -1f)]
    public void Terrain_RejectsInvalidSettings(string field, float value)
    {
        var parameter = Terrain();
        typeof(TerrainParameter).GetField(field).SetValue(parameter, value);
        Assert.Throws<ArgumentException>(() => GenerationValidation.Terrain(parameter));
        Assert.Throws<ArgumentException>(() => Drain(new TerrainGenerator().GenerateHeightMap(
            9, 7, 0f, 0f, parameter, new BlockMonad<float[]>(r => null))));
    }

    [Test]
    public void Terrain_RejectsMissingOceanBandAndSupportsSerializedEditorSettings()
    {
        var parameter = Terrain();
        parameter.OCEAN_BORDER_WIDTH = 0;
        Assert.Throws<ArgumentException>(() => GenerationValidation.Terrain(parameter));
        using (var serialized = new SerializedObject(parameter))
        {
            serialized.FindProperty("OCEAN_BORDER_WIDTH").intValue = 3;
            serialized.FindProperty("COAST_TRANSITION_WIDTH").floatValue = 5f;
            serialized.ApplyModifiedProperties();
        }
        Assert.That(parameter.OCEAN_BORDER_WIDTH, Is.EqualTo(3));
        Assert.That(parameter.COAST_TRANSITION_WIDTH, Is.EqualTo(5f));
        Assert.That(GenerateTerrain(parameter, 25, 19)[MathUtility.MapIndex(2, 9, 19)],
            Is.EqualTo(parameter.OCEAN_FLOOR_HEIGHT));
    }

    [Test]
    public void Terrain_LargeUpliftWithFractionalPowersRemainsFinite()
    {
        var parameter = Terrain();
        parameter.LOW_GROUND_FACTOR = 1.3f;
        parameter.HIGH_MOUNTAIN_FACTOR = 2.7f;
        parameter.RANDOM_POINT_GEN_PARA.NUM = 1;
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_AREA_RADIUS = 2f;
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_AREA_SCALE = 100f;
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_SCALE = 1f;
        var map = GenerateTerrain(parameter, 25, 19);
        Assert.That(map.All(v => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0f && v <= 1f));
        Assert.That(map[MathUtility.MapIndex(12, 9, 19)], Is.EqualTo(1f));
    }

    [TestCase(96, 64)]
    [TestCase(64, 96)]
    [TestCase(1, 1)]
    [TestCase(2, 9)]
    public void LayeredTerrain_IsDeterministicFiniteAndSurroundedByOcean(int width, int height)
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        parameter.CONTINENTS.WAVELENGTH = 30f;
        parameter.MOUNTAIN_REGIONS.WAVELENGTH = 20f;
        parameter.RIDGES.WAVELENGTH = 12f;
        parameter.RANDOM_POINT_GEN_PARA = null; // New mode has no dependency on legacy points.
        var first = GenerateTerrain(parameter, width, height);
        Assert.That(first.All(v => !float.IsNaN(v) && v >= 0f && v <= 1f));
        CollectionAssert.AreEqual(first, GenerateTerrain(parameter, width, height));
        if (width > 2 * parameter.OCEAN_BORDER_WIDTH && height > 2 * parameter.OCEAN_BORDER_WIDTH)
            Assert.That(first.SequenceEqual(GenerateTerrain(parameter, width, height, 78.9f)), Is.False);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                Assert.That(first[MathUtility.MapIndex(x, y, height)], Is.EqualTo(parameter.OCEAN_FLOOR_HEIGHT));
    }

    [Test]
    public void LayeredTerrain_DisabledLayersYieldFlatInteriorAndIndependentControls()
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        parameter.CONTINENTS.ENABLED = parameter.MOUNTAIN_REGIONS.ENABLED =
            parameter.RIDGES.ENABLED = parameter.DETAILS.ENABLED = false;
        int center = MathUtility.MapIndex(24, 24, 49);
        Assert.That(GenerateTerrain(parameter, 49, 49)[center], Is.EqualTo(parameter.BASE_LAND_HEIGHT));
        parameter.BASE_LAND_HEIGHT = 0.6f;
        var flat = GenerateTerrain(parameter, 49, 49);
        Assert.That(flat[center], Is.EqualTo(0.6f));
        parameter.RIDGES.ENABLED = true;
        var mountains = GenerateTerrain(parameter, 49, 49);
        Assert.That(mountains[center], Is.GreaterThan(flat[center]));
        parameter.MOUNTAIN_HEIGHT = 0f;
        CollectionAssert.AreEqual(flat, GenerateTerrain(parameter, 49, 49));
        parameter.DETAILS.ENABLED = true;
        Assert.That(flat.SequenceEqual(GenerateTerrain(parameter, 49, 49)), Is.False);
        parameter.PLAIN_DETAIL_HEIGHT = parameter.MOUNTAIN_DETAIL_HEIGHT = 0f;
        CollectionAssert.AreEqual(flat, GenerateTerrain(parameter, 49, 49));
    }

    [Test]
    public void LayeredTerrain_LandThresholdChangesLandAreaAndMountainMaskLimitsRidges()
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        parameter.CONTINENTS.WAVELENGTH = 25f;
        parameter.RIDGES.ENABLED = parameter.DETAILS.ENABLED = false;
        parameter.LAND_THRESHOLD = 0.3f;
        var moreLand = GenerateTerrain(parameter, 79, 61);
        parameter.LAND_THRESHOLD = 0.7f;
        var lessLand = GenerateTerrain(parameter, 79, 61);
        Assert.That(moreLand.Count(v => v > parameter.SEA_LEVEL), Is.GreaterThan(lessLand.Count(v => v > parameter.SEA_LEVEL)));
        parameter.CONTINENTS.ENABLED = false;
        parameter.RIDGES.ENABLED = true;
        parameter.MOUNTAIN_THRESHOLD = 1f;
        var noMountains = GenerateTerrain(parameter, 79, 61);
        parameter.MOUNTAIN_THRESHOLD = 0f;
        var allMountains = GenerateTerrain(parameter, 79, 61);
        int center = MathUtility.MapIndex(39, 30, 61);
        Assert.That(noMountains[center], Is.EqualTo(parameter.BASE_LAND_HEIGHT));
        Assert.That(allMountains[center], Is.GreaterThan(noMountains[center]));
    }

    [TestCase(33, 33)]
    [TestCase(33, 25)]
    [TestCase(25, 33)]
    public void LayeredTerrain_ResolutionChangesPreserveWorldAndCoast(int width, int height)
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        parameter.WORLD_SCALE = 256f;
        parameter.OCEAN_BORDER_WIDTH = 3;
        parameter.COAST_TRANSITION_WIDTH = 20f;
        parameter.COAST_NOISE_AMPLITUDE = 8f;
        parameter.COAST_NOISE_SCALE = 40f;
        int largeWidth = (width - 1) * 4 + 1, largeHeight = (height - 1) * 4 + 1;
        var a = GenerateTerrain(parameter, width, height);
        var b = GenerateTerrain(parameter, largeWidth, largeHeight);
        // Includes the fixed sea band, noisy coast, continental interior and all detail layers.
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            Assert.That(a[MathUtility.MapIndex(x, y, height)],
                Is.EqualTo(b[MathUtility.MapIndex(x * 4, y * 4, largeHeight)]).Within(0.000001f),
                "World sample changed at " + x + ", " + y);
    }

    [Test]
    public void LayeredTerrain_RectangularAspectChangesKeepNormalizedWorldPositions()
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        var square = GenerateTerrain(parameter, 33, 33);
        var rectangle = GenerateTerrain(parameter, 65, 33);
        for (int x = 0; x < 33; x++)
        for (int y = 0; y < 33; y++)
            Assert.That(square[MathUtility.MapIndex(x, y, 33)],
                Is.EqualTo(rectangle[MathUtility.MapIndex(x * 2, y, 33)]).Within(0.000001f));
    }

    [TestCase("WAVELENGTH", 0f)]
    [TestCase("WAVELENGTH", float.NaN)]
    [TestCase("LACUNARITY", 0.5f)]
    [TestCase("PERSISTENCE", 1.1f)]
    public void LayeredTerrain_RejectsInvalidNoiseSettings(string name, float value)
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        typeof(TerrainNoiseLayer).GetField(name).SetValue(parameter.CONTINENTS, value);
        Assert.Catch<ArgumentException>(() => GenerationValidation.Terrain(parameter));
    }

    [TestCase(0)]
    [TestCase(13)]
    public void LayeredTerrain_RejectsInvalidOctaveCounts(int count)
    {
        var parameter = Terrain();
        parameter.USE_LAYERED_TERRAIN = true;
        parameter.RIDGES.OCTAVES = count;
        Assert.Throws<ArgumentException>(() => GenerationValidation.Terrain(parameter));
    }

    [Test]
    public void LayeredTerrain_SerializedNestedSettingsAndChineseTooltipsAreAvailable()
    {
        var parameter = Terrain();
        using (var settings = new SerializedObject(parameter))
        {
            settings.FindProperty("USE_LAYERED_TERRAIN").boolValue = true;
            var continents = settings.FindProperty("CONTINENTS");
            continents.FindPropertyRelative("WAVELENGTH").floatValue = 75f;
            continents.FindPropertyRelative("OCTAVES").intValue = 3;
            Assert.That(settings.FindProperty("COAST_NOISE_SCALE").tooltip, Does.Contain("海岸"));
            Assert.That(continents.FindPropertyRelative("WAVELENGTH").tooltip, Does.Contain("波長"));
            settings.ApplyModifiedProperties();
        }
        Assert.That(parameter.CONTINENTS.WAVELENGTH, Is.EqualTo(75f));
        Assert.That(parameter.CONTINENTS.OCTAVES, Is.EqualTo(3));
        var copy = Create<TerrainParameter>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(parameter), copy);
        Assert.That(copy.USE_LAYERED_TERRAIN, Is.True);
        Assert.That(copy.CONTINENTS.WAVELENGTH, Is.EqualTo(75f));
        CollectionAssert.AreEqual(GenerateTerrain(parameter, 49, 49), GenerateTerrain(copy, 49, 49));
    }

    [Test]
    public void PreparedNoise_MatchesLegacyOctavesAndSupportsRidges()
    {
        var noise = new NoiseUtility.PreparedFractal(5f, 5, 2f, 0.5f);
        for (int i = 0; i < 50; i++)
        {
            float x = i / 50f, y = 1f - x;
            Assert.That(noise.Sample(x, y, 12.3f, 45.6f), Is.EqualTo(
                NoiseUtility.CountRecursivePerlinNoise(x, y, 12.3f, 45.6f, 5f, 5, 2)).Within(0.00001f));
            Assert.That(noise.Sample(x, y, 12.3f, 45.6f, true), Is.InRange(0f, 1f));
        }
    }

    [Test]
    public void LocalUplift_SharedNoisePreservesOverlappingPointContributions()
    {
        var parameter = Create<RandomPointParameter>();
        parameter.NUM = 3;
        parameter.LOCAL_SCALE = 5f;
        parameter.LOCAL_FREQ_COUNT_TIMES = 3;
        parameter.LOCAL_FREQ_GROW_FACTOR = 2;
        parameter.LOCAL_AREA_RADIUS = 2f;
        parameter.LOCAL_AREA_SCALE = 0.4f;
        var generator = new RandomPointGenerator();
        var result = new BlockMonad<float[]>(r => generator.GenerateRandomLocalAreaMap(7, 5, parameter, r));
        Drain(result.Do());
        Assert.That(result.Error, Is.Null);
        for (int x = 0; x < 7; x++)
        for (int y = 0; y < 5; y++)
        {
            var coord = new Vector2((float)x / 7, (float)y / 5);
            float expected = 0f;
            foreach (var point in generator.Points)
            {
                float distance = Vector2.Distance(point, coord);
                if (distance >= parameter.LOCAL_AREA_RADIUS) continue;
                float noise = NoiseUtility.CountRecursivePerlinNoise(coord.x, coord.y, 0f, 0f,
                    parameter.LOCAL_SCALE, parameter.LOCAL_FREQ_COUNT_TIMES, parameter.LOCAL_FREQ_GROW_FACTOR);
                expected += noise * (parameter.LOCAL_AREA_SCALE * (parameter.LOCAL_AREA_RADIUS - distance));
            }
            Assert.That(result.Result[MathUtility.MapIndex(x, y, 5)], Is.EqualTo(expected).Within(0.00001f));
        }
    }

    private RainParameter Rain(float fraction = 0.2f)
    {
        var parameter = Create<RainParameter>();
        parameter.HEIGHT_LOCATIONS = fraction;
        parameter.RIVER_LENGTH = 20;
        parameter.RIVER_END_HEIGHT = 0.2f;
        parameter.RIVER_AVERAGE_FACTOR = 1f;
        return parameter;
    }

    [TestCase(3, 2)]
    [TestCase(2, 3)]
    public void River_FollowsAdjacentDownhillTileOnRectangularMaps(int width, int height)
    {
        var heights = Enumerable.Repeat(0.5f, width * height).ToArray();
        heights[0] = 0.9f;
        heights[1] = 0.8f;
        var outlet = MathUtility.MapIndex(1, 0, height);
        heights[outlet] = 0.1f;
        var result = new BlockMonad<float[]>(r => new RiverGenerator().GenerateRiverMap(width, height, heights, Rain(), r));
        Drain(result.Do());
        var expected = new float[heights.Length];
        expected[outlet] = 1f;
        CollectionAssert.AreEqual(expected, result.Result);
    }

    [TestCase(0f)]
    [TestCase(1f)]
    public void River_FlatMapOrNoSourcesReturnsFiniteZeros(float fraction)
    {
        var result = new BlockMonad<float[]>(r => new RiverGenerator().GenerateRiverMap(3, 2,
            Enumerable.Repeat(0.5f, 6).ToArray(), Rain(fraction), r));
        Drain(result.Do());
        CollectionAssert.AreEqual(new float[6], result.Result);
    }

    [Test]
    public void River_FractionOneUsesAllSourcesWithoutDividingByZero()
    {
        var result = new BlockMonad<float[]>(r => new RiverGenerator().GenerateRiverMap(2, 2,
            new[] { 0.9f, 0.6f, 0.5f, 0.1f }, Rain(1f), r));
        Drain(result.Do());
        Assert.That(result.Result.Any(v => v > 0f));
        Assert.That(result.Result.All(v => !float.IsNaN(v) && v >= 0f && v <= 1f));
    }

    [TestCase(0, 2)]
    [TestCase(-1, 3)]
    [TestCase(int.MaxValue, 2)]
    public void Generation_RejectsInvalidDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Drain(new RiverGenerator().GenerateRiverMap(
            width, height, new float[0], Rain(), new BlockMonad<float[]>(r => null))));
    }

    [Test]
    public void River_RejectsMismatchedInputAndInvalidParameters()
    {
        Assert.Throws<ArgumentException>(() => Drain(new RiverGenerator().GenerateRiverMap(
            3, 2, new float[5], Rain(), new BlockMonad<float[]>(r => null))));
        var parameter = Rain();
        parameter.RIVER_AVERAGE_FACTOR = 0f;
        Assert.Throws<ArgumentException>(() => Drain(new RiverGenerator().GenerateRiverMap(
            1, 1, new[] { 0.5f }, parameter, new BlockMonad<float[]>(r => null))));
    }

    [Test]
    public void Noise_RejectsZeroOctaves()
    {
        Assert.Throws<ArgumentException>(() => NoiseUtility.CountRecursivePerlinNoise(0, 0, 0, 0, 1, 0, 2));
    }

    [Test]
    public void Noise_NormalizesKnownPerlinUndershoot()
    {
        // Actual humidity sample from a 128x128 layered world with seed 32.
        float sample = NoiseUtility.CountRecursivePerlinNoise(3606f + 108f / 128f,
            8693f + 107f / 128f, 0f, 0f, 4f, 2, 2);
        Assert.That(sample, Is.EqualTo(0f));
    }

    [Test]
    public void Weather_KnownUndershootProducesValidBiomeSamples()
    {
        var parameter = AssetDatabase.LoadAssetAtPath<WeatherParameter>("Assets/Data/GeneratorParameter/BasicParamWeather.asset");
        var result = new BlockMonad<float[]>(r => new WeatherGenerator().GenerateWeatherMap(
            128, 128, 3606f, 8693f, parameter, r));
        Drain(result.Do());
        Assert.That(result.Error, Is.Null);
        Assert.That(result.Result.All(v => !float.IsNaN(v) && v >= 0f && v <= 1f));
        Assert.That(result.Result[MathUtility.MapIndex(108, 107, 128)], Is.EqualTo(0f));
    }

    [TestCase(0f)]
    [TestCase(1f)]
    public void Population_HandlesBothNormalizedEndpoints(float value)
    {
        var parameter = Create<PopulationParameter>();
        parameter.HeightVariety = parameter.HumidityVariety = parameter.TemperatureVariety = 2;
        parameter.HeightProbability = parameter.HumidityProbability = parameter.TemperatureProbability = new[] { 1f, 1f };
        parameter.BasicProbability = 1f;
        var tile = new TileUnit(value, value, value, 0.5f, 0f, Biome.TUNDRA);
        var result = new BlockMonad<int[]>(r => new PopulationGenerator().GeneratePopulationMap(
            1, 1, new ITileUnit[] { tile }, null, parameter, r));
        Drain(result.Do());
        CollectionAssert.AreEqual(new[] { 1 }, result.Result);
        parameter.HeightProbability = new[] { 0f, 0f };
        Drain(result.Do());
        CollectionAssert.AreEqual(new[] { 0 }, result.Result);
    }

    [Test]
    public void Population_RejectsNonfiniteValuesAndMalformedProbabilities()
    {
        var parameter = Create<PopulationParameter>();
        parameter.HeightProbability = new[] { float.NaN };
        Assert.Throws<ArgumentException>(() => GenerationValidation.Population(parameter));
        parameter.HeightProbability = new float[0];
        Assert.Throws<ArgumentException>(() => GenerationValidation.Population(parameter));
        parameter.HeightProbability = new[] { 1f };
        var tile = new TileUnit(0f, 0f, float.NaN, 0f, 0f, Biome.TUNDRA);
        Assert.Throws<ArgumentException>(() => Drain(new PopulationGenerator().GeneratePopulationMap(
            1, 1, new ITileUnit[] { tile }, null, parameter, new BlockMonad<int[]>(r => null))));
    }

    private BiomeDistribution Distribution(int humidity, int height, int temperature)
    {
        var distribution = Create<BiomeDistribution>();
        distribution.HumidityVariety = humidity;
        distribution.HeightVariety = height;
        distribution.TemperatureVariety = temperature;
        distribution.HumidityRange = Enumerable.Repeat(1, humidity).ToArray();
        distribution.HeightRange = Enumerable.Repeat(1, height).ToArray();
        distribution.TemperatureRange = Enumerable.Repeat(1, temperature).ToArray();
        var biome = Create<BiomeData>();
        distribution.BiomeHumiditys = Enumerable.Range(0, humidity).Select(h => new BiomeDistribution.BiomeHeight
        {
            BiomeHeights = Enumerable.Range(0, height).Select(y => new BiomeDistribution.BiomeTemperature
            {
                BiomeTemperatures = Enumerable.Repeat(biome, temperature).ToArray()
            }).ToArray()
        }).ToArray();
        return distribution;
    }

    private void ResizeDistribution(BiomeDistribution distribution, int humidity, int height, int temperature)
    {
        var editor = UnityEditor.Editor.CreateEditor(distribution, typeof(BiomeDistributionEditor));
        _objects.Add(editor);
        distribution.HumidityVariety = humidity;
        distribution.HeightVariety = height;
        distribution.TemperatureVariety = temperature;
        editor.serializedObject.Update();
        Invoke(editor, "_UpdateRange", humidity, height, temperature);
        Invoke(editor, "_UpdateBiomeVariety", humidity, height, temperature);
        editor.serializedObject.ApplyModifiedProperties();
    }

    [Test]
    public void BiomeEditor_ResizesUnequalDimensionsWithoutLosingExistingCells()
    {
        var distribution = Distribution(1, 2, 3);
        distribution.HeightRange[1] = 7;
        var existing = distribution.BiomeHumiditys[0].BiomeHeights[1].BiomeTemperatures[2];
        ResizeDistribution(distribution, 2, 3, 4);
        Assert.That(distribution.HeightRange, Is.EqualTo(new[] { 1, 7, 1 }));
        Assert.That(distribution.BiomeHumiditys.Length, Is.EqualTo(2));
        foreach (var humidity in distribution.BiomeHumiditys)
        {
            Assert.That(humidity.BiomeHeights.Length, Is.EqualTo(3));
            foreach (var height in humidity.BiomeHeights) Assert.That(height.BiomeTemperatures.Length, Is.EqualTo(4));
        }
        Assert.That(distribution.BiomeHumiditys[0].BiomeHeights[1].BiomeTemperatures[2], Is.SameAs(existing));
        Assert.That(distribution.BiomeHumiditys[1].BiomeHeights[0].BiomeTemperatures[0], Is.Null);
        Assert.That(distribution.BiomeHumiditys[0].BiomeHeights[0].BiomeTemperatures[3], Is.Null);
        ResizeDistribution(distribution, 1, 2, 3);
        Assert.That(distribution.BiomeHumiditys[0].BiomeHeights[1].BiomeTemperatures[2], Is.SameAs(existing));
    }

    [Test]
    public void BiomeEditor_TrimsLegacySurplusWithoutChangingActiveAssignments()
    {
        var original = AssetDatabase.LoadAssetAtPath<BiomeDistribution>("Assets/Data/BiomeDistribute/NormalBiomeDistribution.asset");
        var distribution = Object.Instantiate(original);
        _objects.Add(distribution);
        var before = Enumerable.Range(0, original.HumidityVariety).SelectMany(h =>
            Enumerable.Range(0, original.HeightVariety).SelectMany(y =>
                original.BiomeHumiditys[h].BiomeHeights[y].BiomeTemperatures.Take(original.TemperatureVariety))).ToArray();
        ResizeDistribution(distribution, original.HumidityVariety, original.HeightVariety, original.TemperatureVariety);
        var after = distribution.BiomeHumiditys.SelectMany(h => h.BiomeHeights.SelectMany(y => y.BiomeTemperatures)).ToArray();
        CollectionAssert.AreEqual(before, after);
        Assert.That(after.Length, Is.EqualTo(100));
    }

    [Test]
    public void BiomeIdentifier_UsesHumidityHeightTemperatureOrderAtEndpoints()
    {
        var distribution = Distribution(2, 3, 4);
        var expected = Create<BiomeData>();
        distribution.BiomeHumiditys[1].BiomeHeights[2].BiomeTemperatures[3] = expected;
        IBiomeIdentifier identifier = new BasicBiomeIdentifier(distribution);
        Assert.That(identifier.IdentifyBiome(1f, 1f, 1f), Is.SameAs(expected));
        Assert.That(identifier.GetHeightVarietyIndex(0f), Is.EqualTo(0));
        distribution.HeightRange = new[] { 0, 0, 0 };
        Assert.Throws<ArgumentException>(() => new BasicBiomeIdentifier(distribution));
    }

    [Test]
    public void PopulationEditor_KeepsExistingProbabilitiesWhenGrowing()
    {
        var parameter = Create<PopulationParameter>();
        parameter.HeightProbability = new[] { 0.75f };
        var editor = UnityEditor.Editor.CreateEditor(parameter, typeof(PopulationParameterEditor));
        _objects.Add(editor);
        editor.serializedObject.Update();
        Invoke(editor, "_UpdateRange", 2, 3, 4);
        editor.serializedObject.ApplyModifiedProperties();
        Assert.That(parameter.HeightProbability, Is.EqualTo(new[] { 0.75f, 0f, 0f }));
    }

    private class CoincidentPoints : IRandomPointGenerator
    {
        public List<Vector2> Points { get; private set; }
        public IEnumerator GenerateRandomPoints(int width, int height, int pointNum, RandomPointParameter parameter)
        {
            Points = Enumerable.Repeat(Vector2.zero, pointNum).ToList();
            yield break;
        }
        public IEnumerator GenerateRandomLocalAreaMap(int width, int height, RandomPointParameter parameter, IReturn<float[]> result)
        {
            throw new NotSupportedException();
        }
    }

    [Test]
    public void Mana_UsesAllConfiguredMidStrengthPoints()
    {
        var parameter = Create<ManaParameter>();
        parameter.RANDOM_POINT_GEN_PARA = Create<RandomPointParameter>();
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_FREQ_COUNT_TIMES = 1;
        parameter.RANDOM_POINT_GEN_PARA.LOCAL_FREQ_GROW_FACTOR = 1;
        parameter.POSITIVE_HIGH_POINTS_NUM = 2;
        parameter.POSITIVE_MID_POINTS_NUM = 1;
        parameter.POSITIVE_MID_RADIUS = parameter.POSITIVE_MID_FACTOR = 1f;
        var generator = new ManaGenerator();
        Set(generator, "_randPointGen", new CoincidentPoints());
        var result = new BlockMonad<float[]>(r => generator.GenerateManaMap(1, 1, parameter, r));
        Drain(result.Do());
        var singlePointContribution = result.Result[0] - 0.5f;
        Assert.That(singlePointContribution, Is.GreaterThan(0f));
        parameter.POSITIVE_MID_POINTS_NUM = 5;
        Drain(result.Do());
        Assert.That(result.Result[0] - 0.5f, Is.EqualTo(5f * singlePointContribution).Within(0.0001f));
    }

    private static TileDataUnit SavedMap(int population = 1)
    {
        return new TileDataUnit
        {
            Width = 1, Height = 1, Populations = new[] { population },
            Map = new[] { new TileUnit(0.1f, 0.2f, 0.3f, 0.4f, 0.5f, Biome.TUNDRA) }
        };
    }

    [Test]
    public void Save_RoundTripsAllTileDataAndCreatesVersionedBackup()
    {
        var path = Path.Combine(_directory, "map.json");
        var storage = new MapSaveStorage(path);
        string message;
        Assert.That(storage.Save(SavedMap(), out message), Is.True, message);
        Assert.That(File.ReadAllText(path), Does.Contain("\"Version\":1"));
        var loaded = storage.Load(out message);
        Assert.That(JsonUtility.ToJson(loaded), Is.EqualTo(JsonUtility.ToJson(SavedMap())));
        Assert.That(storage.Save(SavedMap(2), out message), Is.True, message);
        Assert.That(storage.Load(out message).Populations[0], Is.EqualTo(2));
        File.WriteAllText(path, "corrupt");
        Assert.That(storage.Load(out message).Populations[0], Is.EqualTo(1));
        Assert.That(message, Does.Contain("backup"));
    }

    [Test]
    public void Save_RejectsInvalidDataWithoutReplacingExistingSave()
    {
        var path = Path.Combine(_directory, "map.json");
        var storage = new MapSaveStorage(path);
        string error;
        Assert.That(storage.Save(SavedMap(), out error), Is.True, error);
        var before = File.ReadAllText(path);
        var invalid = SavedMap();
        invalid.Populations = new int[0];
        Assert.That(storage.Save(invalid, out error), Is.False);
        Assert.That(error, Is.Not.Empty);
        Assert.That(File.ReadAllText(path), Is.EqualTo(before));
        Assert.That(File.Exists(path + ".tmp"), Is.False);
    }

    [Test]
    public void Save_ReportsFilesystemFailure()
    {
        var blocked = Path.Combine(_directory, "blocked");
        File.WriteAllText(blocked, "not a directory");
        string error;
        Assert.That(new MapSaveStorage(Path.Combine(blocked, "map.json")).Save(SavedMap(), out error), Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void LegacySave_MigratesToValidatedVersionedFile()
    {
        var path = Path.Combine(_directory, "map.json");
        var storage = new MapSaveStorage(path);
        var json = JsonUtility.ToJson(SavedMap(3));
        string message;
        Assert.That(storage.MigrateLegacy(json, out message).Populations[0], Is.EqualTo(3));
        Assert.That(message, Does.Contain("Migrated"));
        Assert.That(storage.Load(out message).Populations[0], Is.EqualTo(3));
        Assert.That(File.ReadAllText(path), Does.Contain("\"Version\":1"));
    }

    [Test]
    public void LegacySave_CorruptDataCannotReplaceValidSave()
    {
        var path = Path.Combine(_directory, "map.json");
        var storage = new MapSaveStorage(path);
        string message;
        Assert.That(storage.Save(SavedMap(), out message), Is.True);
        Assert.That(storage.MigrateLegacy("{}", out message), Is.Null);
        Assert.That(storage.Load(out message).Populations[0], Is.EqualTo(1));
    }

    [TestCase("{\"Version\":999}")]
    [TestCase("{\"Version\":1,\"Data\":null}")]
    [TestCase("not json")]
    public void Load_RejectsUnsupportedOrCorruptSaves(string contents)
    {
        var path = Path.Combine(_directory, "map.json");
        File.WriteAllText(path, contents);
        string message;
        Assert.That(new MapSaveStorage(path).Load(out message), Is.Null);
        Assert.That(message, Is.Not.Empty);
    }

    [TestCase(12, 7, 9876)]
    [TestCase(7, 12, 9876)]
    [TestCase(96, 64, 9876)]
    [TestCase(128, 128, 32)]
    [TestCase(256, 128, 32)]
    public void WorldEditor_GeneratesValidReproducibleRectangularWorld(int width, int height, int seed)
    {
        var window = Create<WorldMapEditor>();
        window.Init();
        Set(window, "_width", width);
        Set(window, "_height", height);
        Set(window, "_seed", seed);
        Drain((IEnumerator)Invoke(window, "_CreateNewWorldMonad"));
        var field = typeof(WorldMapEditor).GetField("_tileData", BindingFlags.Instance | BindingFlags.NonPublic);
        var data = (TileDataUnit)field.GetValue(window);
        MapSaveStorage.Validate(data);
        Assert.That(data.Map.Length, Is.EqualTo(width * height));
        var first = JsonUtility.ToJson(data);
        Drain((IEnumerator)Invoke(window, "_CreateNewWorldMonad"));
        Assert.That(JsonUtility.ToJson(field.GetValue(window)), Is.EqualTo(first));
    }
}
