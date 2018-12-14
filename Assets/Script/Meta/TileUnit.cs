using System;
using System.Runtime.Serialization;
using UnityEngine;

public interface ITileUnit
{
    float Height { get; }
    float Humidity { get; }
    float Temperature { get; }
    float Mana { get; }
    float River { get; }
    float Population { get; }
    Biome Biome { get; }
}

[Serializable]
public class TileUnit : ITileUnit
{
    [SerializeField]
    private Biome _biome;
    [SerializeField]
    private float _height;
    [SerializeField]
    private float _humidity;
    [SerializeField]
    private float _temperature;
    [SerializeField]
    private float _mana;
    [SerializeField]
    private float _river;
    [SerializeField]
    private int _population;

    public float Height { get { return _height; } }
    public float Humidity { get { return _humidity; } }
    public float Temperature { get { return _temperature; } }
    public float Mana { get { return _mana; } }
    public float River { get { return _river; } }
    public float Population { get { return _population; } }
    public Biome Biome { get { return _biome; } }

    public TileUnit(
        float height,
        float humudity,
        float temperature,
        float mana,
        float river,
        Biome biome)
    {
        _height = height;
        _humidity = humudity;
        _temperature = temperature;
        _mana = mana;
        _river = river;
        _biome = biome;
    }
}