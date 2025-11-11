using UnityEngine;
using UnityEditor;
using PlasticPipe.Certificates;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        MapGenerator map = (MapGenerator)target;
        if (GUILayout.Button("Generate Map")) map.SpawnMap();
        if (GUILayout.Button("Clear Map")) map.ClearMap();
        
    }
}