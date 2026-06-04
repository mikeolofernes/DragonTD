#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Editor
{
    public static class EnemyAnimationTestSetup
    {
        private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
        private const string TestWavePath = "Assets/ScriptableObjects/Waves/AnimationTest_OrcRunner.asset";
        private const string OrcRunnerPrefabPath = "Assets/Prefabs/Enemies/OrcRunner.prefab";
        private const string Chapter1ContentPath = "Assets/ScriptableObjects/Chapters/Chapter1Content.asset";

        [MenuItem("DragonTD/Enemies/Configure OrcRunner Animation Test")]
        public static void ConfigureOrcRunnerAnimationTest()
        {
            WaveData testWave = EnsureTestWave();
            ConfigureChapterFirstWave(testWave);
            ConfigureBattleScene(testWave);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EnemyAnimationTestSetup] BattleScene wave 1 now spawns rigged OrcRunner only, with authored path restored.");
        }

        private static WaveData EnsureTestWave()
        {
            WaveData wave = AssetDatabase.LoadAssetAtPath<WaveData>(TestWavePath);
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(wave, TestWavePath);
            }

            GameObject orcRunner = AssetDatabase.LoadAssetAtPath<GameObject>(OrcRunnerPrefabPath);
            var so = new SerializedObject(wave);
            SerializedProperty groups = so.FindProperty("EnemyGroups");
            groups.arraySize = 1;
            SerializedProperty group = groups.GetArrayElementAtIndex(0);
            group.FindPropertyRelative("EnemyPrefab").objectReferenceValue = orcRunner;
            group.FindPropertyRelative("Count").intValue = 8;
            group.FindPropertyRelative("SpawnInterval").floatValue = 0.75f;
            so.FindProperty("TimeBetweenGroups").floatValue = 1f;
            so.FindProperty("GoldReward").intValue = 0;
            so.FindProperty("ManaReward").intValue = 0;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wave);
            return wave;
        }

        private static void ConfigureChapterFirstWave(WaveData testWave)
        {
            ChapterContent chapter = AssetDatabase.LoadAssetAtPath<ChapterContent>(Chapter1ContentPath);
            if (chapter == null) return;

            var so = new SerializedObject(chapter);
            SerializedProperty waves = so.FindProperty("waves");
            if (waves.arraySize == 0)
                waves.arraySize = 1;
            waves.GetArrayElementAtIndex(0).objectReferenceValue = testWave;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(chapter);
        }

        private static void ConfigureBattleScene(WaveData testWave)
        {
            var scene = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);
            WaveManager waveManager = Object.FindAnyObjectByType<WaveManager>();
            if (waveManager == null)
            {
                Debug.LogWarning("[EnemyAnimationTestSetup] No WaveManager found in BattleScene.");
                return;
            }

            WaypointPathAuthoring path = Object.FindAnyObjectByType<WaypointPathAuthoring>();
            if (path == null)
            {
                var pathObject = new GameObject("EnemyPathAuthoring");
                path = pathObject.AddComponent<WaypointPathAuthoring>();
                pathObject.AddComponent<WaypointPath>();
            }

            path.gameObject.name = "EnemyPathAuthoring";
            path.transform.SetParent(null, true);
            if (!path.HasWaypoints)
                path.RebuildFromPoints(DefaultPaintedPathWaypoints());

            Transform[] waypoints = path.GetWaypointTransforms();
            var so = new SerializedObject(waveManager);
            SerializedProperty waves = so.FindProperty("_waves");
            if (waves.arraySize == 0)
                waves.arraySize = 1;
            waves.GetArrayElementAtIndex(0).objectReferenceValue = testWave;

            SerializedProperty spawnPoints = so.FindProperty("_spawnPoints");
            spawnPoints.arraySize = waypoints.Length > 0 ? 1 : 0;
            if (waypoints.Length > 0)
                spawnPoints.GetArrayElementAtIndex(0).objectReferenceValue = waypoints[0];

            SerializedProperty waypointProp = so.FindProperty("_waypoints");
            waypointProp.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
                waypointProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];

            so.FindProperty("_authoredPath").objectReferenceValue = path;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(path);
            EditorUtility.SetDirty(waveManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
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
