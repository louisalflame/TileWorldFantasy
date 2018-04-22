using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface IWorldMap
{
}

public class WorldMap : IWorldMap
{
    private List<TileUnit> _map = new List<TileUnit>();

    private const int MAP_WIDTH = 1000;
    private const int MAP_HEIGHT = 1000;

    private ITerrainGenerator _terrainGen = new TerrainGenerator();
    private IWeatherGenerator _temperatureGen = new WeatherGenerator();
    private IWeatherGenerator _humidityGen = new WeatherGenerator();
    private IWeatherGenerator _manaGen = new WeatherGenerator();

    private Executor _executor = new Executor();
    
    public IEnumerator Initialize()
    {
        var genTerrainMonad = new BlockMonad<float[]>(r =>
            _terrainGen.GenerateHeightMap(
                MAP_WIDTH,
                MAP_HEIGHT,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                new TerrainParameter(),
                r));
        var genTemperatureMonad = new BlockMonad<float[]>(r =>
            _temperatureGen.GenerateWeatherMap(
                MAP_WIDTH,
                MAP_HEIGHT,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                new WeatherParameter(),
                r));
        var genHumidityMonad = new BlockMonad<float[]>(r =>
            _humidityGen.GenerateWeatherMap(
                MAP_WIDTH,
                MAP_HEIGHT,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                new WeatherParameter(),
                r));
        var genManaMonad = new BlockMonad<float[]>(r =>
            _manaGen.GenerateWeatherMap(
                MAP_WIDTH,
                MAP_HEIGHT,
                Random.Range(0, 10000),
                Random.Range(0, 10000),
                new WeatherParameter(),
                r));

        var combineM = Monad.WhenAll(
            genTerrainMonad, 
            genTemperatureMonad,
            genHumidityMonad,
            genManaMonad);

        _executor.Add(combineM.Do());
        yield return _executor.Join();
    }
}