using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator map = target as MapGenerator;

        if (DrawDefaultInspector())
        {
            map.GenerateMap(map.mapIndex); // Pass the current mapIndex
        }

        if (GUILayout.Button("Generate Map"))
        {
            map.GenerateMap(map.mapIndex); // Pass the current mapIndex
        }
    }
}