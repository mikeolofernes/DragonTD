#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DragonTD.Core;
using DragonTD.TowerDefense;
using DragonTD.UI;

namespace DragonTD.Editor
{
    public static class SceneBootstrapper
    {
        [MenuItem("DragonTD/★ Create Battle Scene")]
        public static void CreateBattleScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGO = new GameObject("Main Camera");
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // Managers
            var gmGO  = CreateSingleton<GameManager>("GameManager");
            var rmGO  = CreateSingleton<ResourceManager>("ResourceManager");
            var wmGO  = CreateSingleton<WaveManager>("WaveManager");
            var gridGO = CreateSingleton<GridManager>("GridManager");
            var pmGO  = CreateSingleton<PlacementManager>("PlacementManager");
            var piGO  = CreateSingleton<PlayerInventory>("PlayerInventory");
            var dirGO = new GameObject("GameDirector");
            dirGO.AddComponent<GameDirector>();

            // Waypoint path  (L-shaped default path)
            var pathRoot = new GameObject("WaypointPath");
            pathRoot.AddComponent<WaypointPath>();

            Transform[] waypoints = CreateWaypoints(pathRoot.transform, new Vector3[]
            {
                new Vector3(-8f,  0f, 0f),   // spawn
                new Vector3(-3f,  0f, 0f),
                new Vector3(-3f,  3f, 0f),
                new Vector3( 3f,  3f, 0f),
                new Vector3( 3f, -3f, 0f),
                new Vector3( 8f, -3f, 0f),   // base
            });

            // Wire WaveManager serialized fields via SerializedObject
            var wmSO = new SerializedObject(wmGO.GetComponent<WaveManager>());
            SetTransformArray(wmSO, "_spawnPoints", new Transform[] { waypoints[0] });
            SetTransformArray(wmSO, "_waypoints",   waypoints);
            wmSO.ApplyModifiedPropertiesWithoutUndo();

            // Canvas / HUD
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<BattleHUD>();

            // Victory/Defeat panel (starts inactive)
            var vdGO = new GameObject("VictoryDefeatPanel");
            vdGO.transform.SetParent(canvasGO.transform, false);
            vdGO.AddComponent<VictoryDefeatPanel>();
            vdGO.SetActive(false);

            // Collection panel
            var cpGO = new GameObject("DragonCollectionPanel");
            cpGO.transform.SetParent(canvasGO.transform, false);
            cpGO.AddComponent<DragonCollectionPanel>();

            // EventSystem
            var evGO = new GameObject("EventSystem");
            evGO.AddComponent<EventSystem>();
            evGO.AddComponent<StandaloneInputModule>();

            // Save
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BattleScene.unity");

            Debug.Log("[DragonTD] BattleScene created at Assets/Scenes/BattleScene.unity.\n" +
                      "Next: assign WaveData assets to WaveManager, enemy prefabs to wave entries, " +
                      "and starter dragons to PlayerInventory in the Inspector.");

            Selection.activeGameObject = wmGO;
        }

        private static GameObject CreateSingleton<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        private static Transform[] CreateWaypoints(Transform parent, Vector3[] positions)
        {
            var result = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var wp = new GameObject($"WP_{i:00}");
                wp.transform.SetParent(parent, false);
                wp.transform.position = positions[i];
                result[i] = wp.transform;
            }
            return result;
        }

        private static void SetTransformArray(SerializedObject so, string fieldName, Transform[] transforms)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = transforms.Length;
            for (int i = 0; i < transforms.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = transforms[i];
        }
    }
}
#endif
