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

    [TestCase(12, 7)]
    [TestCase(7, 12)]
    public void WorldEditor_GeneratesValidReproducibleRectangularWorld(int width, int height)
    {
        var window = Create<WorldMapEditor>();
        window.Init();
        Set(window, "_width", width);
        Set(window, "_height", height);
        Set(window, "_seed", 9876);
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
