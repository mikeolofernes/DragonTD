#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Editor
{
    public static class WaypointPathAuthoringMenu
    {
        [MenuItem("DragonTD/Path/Create Authoring Path From WaveManager")]
        public static void CreateAuthoringPathFromWaveManager()
        {
            WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("[WaypointPathAuthoring] No WaveManager found in the open scene.");
                return;
            }

            Vector3[] points = ReadWaveManagerWaypointPositions(waveManager);
            if (points.Length < 2)
                points = DefaultPaintedPathWaypoints();

            GameObject existing = GameObject.Find("WaypointPath");
            GameObject pathObject = existing != null ? existing : new GameObject("WaypointPath");
            if (pathObject.GetComponent<WaypointPath>() == null)
                pathObject.AddComponent<WaypointPath>();

            WaypointPathAuthoring authoring = pathObject.GetComponent<WaypointPathAuthoring>();
            if (authoring == null)
                authoring = pathObject.AddComponent<WaypointPathAuthoring>();

            Undo.RegisterFullObjectHierarchyUndo(pathObject, "Create Authoring Path");
            authoring.RebuildFromPoints(points);

            var waveManagerObject = new SerializedObject(waveManager);
            waveManagerObject.FindProperty("_authoredPath").objectReferenceValue = authoring;
            waveManagerObject.ApplyModifiedProperties();

            Selection.activeGameObject = pathObject;
            EditorUtility.SetDirty(pathObject);
            EditorUtility.SetDirty(waveManager);
            EditorSceneManager.MarkSceneDirty(pathObject.scene);
        }

        private static Vector3[] ReadWaveManagerWaypointPositions(WaveManager waveManager)
        {
            var waveManagerObject = new SerializedObject(waveManager);
            SerializedProperty waypoints = waveManagerObject.FindProperty("_waypoints");
            if (waypoints == null || waypoints.arraySize < 2)
                return System.Array.Empty<Vector3>();

            Vector3[] points = new Vector3[waypoints.arraySize];
            int count = 0;
            for (int i = 0; i < waypoints.arraySize; i++)
            {
                Transform waypoint = waypoints.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                if (waypoint == null) continue;
                points[count++] = waypoint.position;
            }

            if (count == points.Length)
                return points;

            Vector3[] compact = new Vector3[count];
            for (int i = 0; i < count; i++)
                compact[i] = points[i];
            return compact;
        }

        private static Vector3[] DefaultPaintedPathWaypoints()
        {
            return new[]
            {
                new Vector3(-9.35f, -0.2f, 0f),
                new Vector3(-4.65f, -0.2f, 0f),
                new Vector3(-4.65f, 2.28f, 0f),
                new Vector3(4.72f, 2.28f, 0f),
                new Vector3(4.72f, -3.02f, 0f),
                new Vector3(8.7f, -3.02f, 0f),
            };
        }
    }
}
#endif
