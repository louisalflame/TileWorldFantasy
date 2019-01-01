using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamMana", order = 14)]
public class ManaParameter : ScriptableObject
{
    public int POSITIVE_HIGH_POINTS_NUM;
    public int POSITIVE_MID_POINTS_NUM;
    public int POSITIVE_LOW_POINTS_NUM;
    public int NEGATIVE_HIGH_POINTS_NUM;
    public int NEGATIVE_MID_POINTS_NUM;
    public int NEGATIVE_LOW_POINTS_NUM;
     
    public float POSITIVE_HIGH_RADIUS;
    public float POSITIVE_MID_RADIUS;
    public float POSITIVE_LOW_RADIUS;
    public float NEGATIVE_HIGH_RADIUS;
    public float NEGATIVE_MID_RADIUS;
    public float NEGATIVE_LOW_RADIUS;

    public float POSITIVE_HIGH_FACTOR;
    public float POSITIVE_MID_FACTOR;
    public float POSITIVE_LOW_FACTOR;
    public float NEGATIVE_HIGH_FACTOR;
    public float NEGATIVE_MID_FACTOR;
    public float NEGATIVE_LOW_FACTOR;

    public RandomPointParameter RANDOM_POINT_GEN_PARA;
}
