using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamTerrain", order = 11)]
public class TerrainParameter : ScriptableObject
{
    public float SCALE;
    public float LOW_GROUND_FACTOR;
    public float HIGH_MOUNTAIN_FACTOR;

    public int FREQ_COUNT_TIMES;
    public int FREQ_GROW_FACTOR;

    public float SURROUND_DOWN_OFFSET;
    public float SURROUND_DOWN_SPEED;

    public RandomPointParameter RANDOM_POINT_GEN_PARA;
}
