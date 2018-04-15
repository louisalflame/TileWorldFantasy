using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
[CreateAssetMenu(menuName = "World/ParamRandomPoint", order = 12)]
public class RandomPointParameter : ScriptableObject
{
    public int NUM;

    public int COUNT_TIME;
    public float POINTS_MIN_DISTANCE;
    public float POINTS_SEPARATE_SPEED;
    public float WALL_MIN_DISTANCE;
    public float WALL_SEPARATE_SPEED;

    public float LOCAL_SCALE;
    public float LOCAL_XOFFSET;
    public float LOCAL_YOFFSET;

    public int LOCAL_FREQ_COUNT_TIMES;
    public int LOCAL_FREQ_GROW_FACTOR;

    public float LOCAL_AREA_RADIUS;
    public float LOCAL_AREA_SCALE;
}