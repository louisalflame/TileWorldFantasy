using System;
using System.Runtime.Serialization;
using UnityEngine;

public interface ITileUnit
{
    float Height { get; }
    float Humidity { get; }
    float Temperature { get; }
    float Mana { get; }
    float Population { get; }
    Biome Biome { get; }
}

[Serializable]
public class TileUnit : ITileUnit
{
    [SerializeField]
    private float _height;
    [SerializeField]
    private float _humidity;
    [SerializeField]
    private float _temperature;
    [SerializeField]
    private Biome _biome;

    [SerializeField]
    private float _mana;
    [SerializeField]
    private int _population;

    public TileUnit(
        float height,
        float humudity,
        float temperature,
        Biome biome)
    {
        _height = height;
        _humidity = humudity;
        _temperature = temperature;
        _biome = biome;
    }

    public float Height { get { return _height; } }
    public float Humidity { get { return _humidity; } }
    public float Temperature { get { return _temperature; } }
    public float Mana { get { return _mana; } }
    public float Population { get { return _population; } }
    public Biome Biome { get { return _biome; } }
}