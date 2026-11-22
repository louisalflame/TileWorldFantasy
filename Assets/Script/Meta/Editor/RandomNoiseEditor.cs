using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RandomNoise))]
public class RandomNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RandomNoise myRandomNoise = (RandomNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myRandomNoise.DrawTexture();
        }
    }
}
