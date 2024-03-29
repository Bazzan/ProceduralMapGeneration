﻿using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(MapPreview))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapPreview mapPreview = (MapPreview)target;
        if (DrawDefaultInspector())
        {
            if (mapPreview.autoUpdate)
            {
                mapPreview.DrawMapInEditor();
            }
        }
        if (GUILayout.Button("Generate Map"))
        {
            mapPreview.DrawMapInEditor();
        }
    }
}
