
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;

public interface ITerrainGenerator
{
    IEnumerator GenerateHeightMap(
        int width, 
        int height,
        float xOffset,
        float yOffset,
        TerrainParameter para,
        IReturn<float[]> ret);
} 

public class TerrainGenerator : ITerrainGenerator
{
    private TerrainParameter _para; 
    
    private int _width;
    private int _height;
    private float _xOffset;
    private float _yOffset;
     
    private IRandomPointGenerator _randPointGen = new RandomPointGenerator();
    private float[] _localAreaMap;

    private float[] _heightMap;
    private NoiseUtility.PreparedFractal _legacyNoise, _continents, _regions, _ridges, _details;
    public float[] HeightMap {
        get { return _heightMap; }
    }

    private int _sleepCount = 0;

    public IEnumerator GenerateHeightMap(
        int width, 
        int height, 
        float xOffset,
        float yOffset,
        TerrainParameter para,
        IReturn<float[]> ret)
    {
        GenerationValidation.CellCount(width, height);
        GenerationValidation.Terrain(para);
        GenerationValidation.Finite(xOffset, "terrain x offset");
        GenerationValidation.Finite(yOffset, "terrain y offset");
        _width = width;
        _height = height;
        _xOffset = xOffset;
        _yOffset = yOffset;
        _para = para;

        _heightMap = new float[_width * _height];

        _legacyNoise = _continents = _regions = _ridges = _details = null;
        if (_para.USE_LAYERED_TERRAIN)
        {
            _continents = Prepare(_para.CONTINENTS);
            _regions = Prepare(_para.MOUNTAIN_REGIONS);
            _ridges = _para.MOUNTAIN_HEIGHT > 0f ? Prepare(_para.RIDGES) : null;
            _details = _para.PLAIN_DETAIL_HEIGHT > 0f || _para.MOUNTAIN_DETAIL_HEIGHT > 0f
                ? Prepare(_para.DETAILS) : null;
            _localAreaMap = null;
        }
        else
        {
            _legacyNoise = new NoiseUtility.PreparedFractal(_para.SCALE, _para.FREQ_COUNT_TIMES,
                _para.FREQ_GROW_FACTOR, 1f / _para.FREQ_GROW_FACTOR);
            var genLocalMonad = new BlockMonad<float[]>(r =>
                _randPointGen.GenerateRandomLocalAreaMap(width, height, _para.RANDOM_POINT_GEN_PARA, r));
            yield return genLocalMonad.Do();
            if (genLocalMonad.Error != null)
            {
                ret.Fail(genLocalMonad.Error);
                yield break;
            }
            _localAreaMap = genLocalMonad.Result;
        }

        yield return _GenerateHeightMap();

        ret.Accept(_heightMap);
    }

    private IEnumerator _GenerateHeightMap()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                float xCoord = (float)x / _width;
                float yCoord = (float)y / _height;
                int idx = MathUtility.MapIndex(x, y, _height);

                // Both resolutions sample the same closed world domain, including all four edges.
                float worldX = (_width > 1 ? (float)x / (_width - 1) : 0f) * _para.WORLD_SCALE;
                float worldY = (_height > 1 ? (float)y / (_height - 1) : 0f) * _para.WORLD_SCALE;
                float distance = Mathf.Min(Mathf.Min(worldX, _para.WORLD_SCALE - worldX),
                    Mathf.Min(worldY, _para.WORLD_SCALE - worldY));
                // Fixed ocean cells need neither terrain nor coast noise.
                var sample = distance <= _para.OCEAN_BORDER_WIDTH ? _para.OCEAN_FLOOR_HEIGHT
                    : _para.USE_LAYERED_TERRAIN ? _CountLayeredHeight(worldX, worldY)
                    : _CountPerlinNoise(idx, xCoord, yCoord);

                _heightMap[idx] = _ApplyOceanBorder(sample, worldX, worldY, distance);

                if (_sleepCount++ > SettingUtility.MapRestCount)
                {
                    yield return null;
                    _sleepCount = 0;
                }
            }
        }
    }

    private float _CountPerlinNoise(int idx, float xCoord, float yCoord)
    {
        float sample = 0f;

        sample += _legacyNoise.Sample(xCoord, yCoord, _xOffset, _yOffset);

        sample += _localAreaMap[idx];

        // Local uplift can exceed one; clamp before fractional powers.
        sample = Mathf.Pow(Mathf.Clamp01(sample), _para.LOW_GROUND_FACTOR);
        sample = 1 - Mathf.Pow(1 - sample, _para.HIGH_MOUNTAIN_FACTOR);

        return sample;
    }

    private static NoiseUtility.PreparedFractal Prepare(TerrainNoiseLayer layer)
    {
        return layer.ENABLED ? new NoiseUtility.PreparedFractal(1f / layer.WAVELENGTH,
            layer.OCTAVES, layer.LACUNARITY, layer.PERSISTENCE) : null;
    }

    private float _CountLayeredHeight(float x, float y)
    {
        float elevation = _continents == null ? _para.BASE_LAND_HEIGHT :
            _para.SEA_LEVEL + (_continents.Sample(x, y, _xOffset, _yOffset) - _para.LAND_THRESHOLD) * _para.CONTINENT_RELIEF;
        elevation = Mathf.Clamp01(elevation);
        float land = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elevation - _para.SEA_LEVEL) / _para.LAND_BLEND_HEIGHT));
        if (land <= 0f || (_ridges == null && _details == null)) return elevation;

        // Fixed offsets separate layer signals without consuming the global random stream.
        float mountain = _regions == null ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
            (_regions.Sample(x, y, _xOffset + 37.1f, _yOffset + 91.7f) - _para.MOUNTAIN_THRESHOLD) / _para.MOUNTAIN_BLEND));
        if (_ridges != null && mountain > 0f)
            elevation += land * mountain * _para.MOUNTAIN_HEIGHT * Mathf.Pow(
                _ridges.Sample(x, y, _xOffset + 173.3f, _yOffset + 251.9f, true), _para.RIDGE_SHARPNESS);
        float detailHeight = Mathf.Lerp(_para.PLAIN_DETAIL_HEIGHT, _para.MOUNTAIN_DETAIL_HEIGHT, mountain);
        if (_details != null && detailHeight > 0f)
            elevation += land * detailHeight * (2f * _details.Sample(x, y, _xOffset + 419.5f, _yOffset + 613.7f) - 1f);
        return Mathf.Clamp01(elevation);
    }

    private float _ApplyOceanBorder(float sample, float x, float y, float distance)
    {
        float borderEnd = _para.OCEAN_BORDER_WIDTH;
        if (distance <= borderEnd) return _para.OCEAN_FLOOR_HEIGHT;
        if (distance - borderEnd >= _para.COAST_TRANSITION_WIDTH + _para.COAST_NOISE_AMPLITUDE)
            return Mathf.Clamp01(sample);

        // Noise only extends the transition inland, preserving the ocean band.
        float extraWidth = 0f;
        if (_para.COAST_NOISE_AMPLITUDE > 0f)
            extraWidth = _para.COAST_NOISE_AMPLITUDE * Mathf.Clamp01(Mathf.PerlinNoise(
                x / _para.COAST_NOISE_SCALE + _xOffset,
                y / _para.COAST_NOISE_SCALE + _yOffset));
        float t = Mathf.Clamp01((distance - borderEnd) / (_para.COAST_TRANSITION_WIDTH + extraWidth));
        t = Mathf.SmoothStep(0f, 1f, t);
        return Mathf.Lerp(_para.OCEAN_FLOOR_HEIGHT, Mathf.Clamp01(sample), t);
    }
}
