using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WorldMapEditor : EditorWindow
{
    [MenuItem("Editor/WorldMapEditor")]
    public static WorldMapEditor OpenOrCreate()
    {
        WorldMapEditor window = EditorWindow.GetWindow<WorldMapEditor>("WorldMapEditor");

        window.Show();
        window.Focus();
        window.Init();
        return window;
    }

    #region UnityFunction
    void OnDestroy()
    {
    }

    void OnGUI()
    {
        try
        {
            _onGUI();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    void Update()
    {
    }
    #endregion

    private Texture2D _worldTexture = null;

    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;

    public void Init()
    {
        var mapData = SaveData.LoadMap();
        if (mapData != null)
        {
            _loadWorldMapTexture(mapData);
        }
    }

    private void _loadWorldMapTexture(SaveDataUnit mapData)
    {
        _worldTexture = new Texture2D(mapData.Width, mapData.Height);
        for (int i = 0; i < mapData.Width; i++)
        {
            for (int j = 0; j < mapData.Height; j++)
            {
                var height = mapData.Map[i * mapData.Width + j];
                _worldTexture.SetPixel(i, j, new Color(height, height, height));
            }
        }
        _worldTexture.Apply();
    }

    #region DrawGUI
    private GUIStyle _mapStyle;
    private void _onGUI()
    {
        if (EditorApplication.isCompiling)
        {
            _isEditorCompiling = true;
            return;
        }
        else if (_isEditorCompiling)
        {
            Init();
            _isEditorCompiling = false;
        }

        if (EditorApplication.isPlaying)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
                Init();
            }
        }
        else if (_isPlaying)
        {
            _isPlaying = false;
            Init();
        }

        if (!_initialGUI)
        {
            _InitGUI();
            _initialGUI = true;
        }

        // draw like that
        // +--------------------+
        // |       header       |
        // +-----------+--------+
        // |   main    |  side  |
        // |  content  | search |
        // +-----------+--------+
        // |       bottom       |
        // +--------------------+
        EditorGUILayout.BeginVertical();
        _DrawHeader();
        _DrawMain();
        EditorGUILayout.EndVertical();
        Repaint();

    }

    private void _InitGUI()
    {
        _mapStyle = new GUIStyle
        {
            fixedHeight = 150,
            fixedWidth = 150
        };
    }

    private void _DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload"))
        {
        }
        EditorGUILayout.EndHorizontal();
    }

    private void _DrawMain()
    {
        if (_worldTexture != null)
        {
            //EditorGUI.DrawPreviewTexture(new Rect(25, 60, 100, 100), _worldTexture);

            GUILayout.Box(_worldTexture, _mapStyle);
        }
    }
    #endregion
}
