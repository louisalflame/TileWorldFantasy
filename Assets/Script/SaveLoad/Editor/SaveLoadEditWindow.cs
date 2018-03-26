using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode] 
public class SaveLoadEditWindow : EditorWindow
{

    [MenuItem("Editor/SaveLoadEditor")]
    public static SaveLoadEditWindow OpenOrCreate()
    {
        SaveLoadEditWindow window = EditorWindow.GetWindow<SaveLoadEditWindow>("SaveLoadEditor");

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

    private bool _isEditorCompiling = false;
    private bool _isPlaying = false;
    private bool _initialGUI = false;

    public void Init()
    { 
    }

    #region DrawGUI
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
        EditorGUILayout.EndVertical();
        Repaint();
         
    }
    #endregion

    private void _DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload"))
        {
        }
        EditorGUILayout.EndHorizontal();
    }
}
