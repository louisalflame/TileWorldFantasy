using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpriteNoise))]
public class SpriteNoiseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpriteNoise myScriptNoise = (SpriteNoise)target;
        if (GUILayout.Button("DrawTexture"))
        {
            myScriptNoise.DrawTexture();
        }
    }
}
