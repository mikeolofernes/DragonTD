#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using DragonTD.Core;
using DragonTD.Dragons;
using DragonTD.TowerDefense;
using DragonTD.UI;

namespace DragonTD.Editor
{
    public static class SceneBootstrapper
    {
        private const string SODir   = "Assets/ScriptableObjects";
        private const string PrefDir = "Assets/Prefabs";
        private const string ArtDir  = "Assets/Art/UI";

        [MenuItem("Dragon Dominion/★ Build Battle Scene")]
        public static void Build()
        {
            if (UnityEditor.EditorApplication.isPlaying)
            {
                Debug.LogError("[Dragon Dominion] Stop Play mode before running Build Battle Scene.");
                return;
            }

            // ── Folders ──────────────────────────────────────────────────────────
            EnsureDir("Assets/ScriptableObjects");
            EnsureDir(SODir + "/Enemies");
            EnsureDir(SODir + "/Waves");
            EnsureDir(SODir + "/Director");
            EnsureDir("Assets/Prefabs");
            EnsureDir(PrefDir + "/Enemies");
            EnsureDir("Assets/Art");
            EnsureDir(ArtDir);
            EnsureDir("Assets/Scenes");

            // ── Assets ───────────────────────────────────────────────────────────
            Sprite             whiteSprite = GetOrCreateWhiteSprite();
            GameDirectorConfig dirCfg      = CreateDirectorConfig();
            EnemyData          orcData     = CreateOrcData();
            GridTile           tilePrefab  = CreateTilePrefab(whiteSprite);
            GameObject         orcPrefab   = CreateOrcPrefab(orcData, whiteSprite);
            WaveData           wave1       = CreateWave1(orcPrefab);

            AssetDatabase.SaveAssets();

            // ── Scene ────────────────────────────────────────────────────────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();
            CreateManagerRoot(dirCfg);
            CreateGridManager(tilePrefab);
            CreateWaveManager(wave1, orcPrefab);
            AddSceneBootstrap();
            CreateUI(whiteSprite);
            EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BattleScene.unity");
            Debug.Log("[Dragon Dominion] BattleScene built. Open Assets/Scenes/BattleScene.unity and press Play.");
        }

        // ── Folder helper ─────────────────────────────────────────────────────────

        static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ── White sprite ─────────────────────────────────────────────────────────

        static Sprite GetOrCreateWhiteSprite()
        {
            const string path = ArtDir + "/white.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var tex = new Texture2D(4, 4);
            tex.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            tex.Apply();

            File.WriteAllBytes(
                Path.Combine(Application.dataPath.Replace("/Assets", ""), path),
                tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType        = TextureImporterType.Sprite;
            imp.spriteImportMode   = SpriteImportMode.Single;
            imp.filterMode         = FilterMode.Point;
            imp.spritePixelsPerUnit = 4; // 4 px = 1 world unit → 4×4 sprite = 1×1 unit
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ── ScriptableObjects ─────────────────────────────────────────────────────

        static GameDirectorConfig CreateDirectorConfig()
        {
            const string path = SODir + "/Director/DefaultDirectorConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameDirectorConfig>(path);
            if (existing != null) return existing;

            var cfg = ScriptableObject.CreateInstance<GameDirectorConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        static EnemyData CreateOrcData()
        {
            const string path = SODir + "/Enemies/OrcScout.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null) return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.EnemyName    = "Orc Scout";
            data.MaxHp        = 200f;
            data.MoveSpeed    = 2.5f;
            data.Armor        = 5f;
            data.GoldValue    = 10;
            data.DamageToBase = 1;
            data.Faction      = EnemyFaction.Orc;
            data.HasElement   = false;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        static WaveData CreateWave1(GameObject orcPrefab)
        {
            const string path = SODir + "/Waves/Wave01.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (existing != null) return existing;

            var wave = ScriptableObject.CreateInstance<WaveData>();
            wave.EnemyGroups = new[]
            {
                new EnemySpawnEntry { EnemyPrefab = orcPrefab, Count = 8, SpawnInterval = 1.2f }
            };
            wave.TimeBetweenGroups = 3f;
            wave.GoldReward  = 50;
            wave.ManaReward  = 20;
            AssetDatabase.CreateAsset(wave, path);
            return wave;
        }

        // ── Prefabs ───────────────────────────────────────────────────────────────

        static GridTile CreateTilePrefab(Sprite sprite)
        {
            const string path = PrefDir + "/GridTile.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GridTile>(path);
            if (existing != null) return existing;

            var go = new GameObject("GridTile");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color  = new Color(0.2f, 0.6f, 0.2f, 0.4f);

            go.AddComponent<BoxCollider2D>(); // OnMouseEnter/Exit on tiles

            var tile = go.AddComponent<GridTile>();
            var so   = new SerializedObject(tile);
            so.FindProperty("_spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<GridTile>();
        }

        static GameObject CreateOrcPrefab(EnemyData data, Sprite sprite)
        {
            const string path = PrefDir + "/Enemies/OrcEnemy.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("OrcEnemy");
            go.transform.localScale = Vector3.one * 0.75f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color  = new Color(0.35f, 0.75f, 0.2f); // green orc

            var orc = go.AddComponent<OrcEnemy>();
            var so  = new SerializedObject(orc);
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ── Scene GameObjects ─────────────────────────────────────────────────────

        static void SetupCamera()
        {
            var go  = new GameObject("Main Camera");
            go.tag  = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic      = true;
            cam.orthographicSize  = 5f;
            cam.backgroundColor   = new Color(0.06f, 0.06f, 0.12f);
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void CreateManagerRoot(GameDirectorConfig dirCfg)
        {
            var root = new GameObject("Managers");

            Child<GameManager>(root,       "GameManager");
            Child<ResourceManager>(root,   "ResourceManager");
            Child<PlacementManager>(root,  "PlacementManager");
            Child<PlayerInventory>(root,   "PlayerInventory");
            Child<DragonAssetLoader>(root, "DragonAssetLoader");

            var dirGO = Child<GameDirector>(root, "GameDirector");
            var so    = new SerializedObject(dirGO.GetComponent<GameDirector>());
            so.FindProperty("_config").objectReferenceValue = dirCfg;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateGridManager(GridTile tilePrefab)
        {
            var go = new GameObject("GridManager");
            var gm = go.AddComponent<GridManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("_tilePrefab").objectReferenceValue  = tilePrefab;
            so.FindProperty("_width").intValue                   = 12;
            so.FindProperty("_height").intValue                  = 8;
            so.FindProperty("_originPosition").vector3Value      = new Vector3(-5.5f, -3.5f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateWaveManager(WaveData wave1, GameObject elitePrefab)
        {
            var root = new GameObject("WaveSetup");
            var wm   = root.AddComponent<WaveManager>();

            var pathViz = new GameObject("WaypointPath");
            pathViz.transform.SetParent(root.transform);
            pathViz.AddComponent<WaypointPath>();

            // S-curve path that weaves through the grid
            var positions = new Vector3[]
            {
                new Vector3(-7f,  0f,   0f), // WP_00  spawn (off-screen left)
                new Vector3(-3f,  0f,   0f), // WP_01
                new Vector3(-3f,  2.5f, 0f), // WP_02
                new Vector3( 3f,  2.5f, 0f), // WP_03
                new Vector3( 3f, -2.5f, 0f), // WP_04
                new Vector3( 7f, -2.5f, 0f), // WP_05  base (off-screen right)
            };
            var wps = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var wp = new GameObject($"WP_{i:00}");
                wp.transform.SetParent(pathViz.transform);
                wp.transform.position = positions[i];
                wps[i] = wp.transform;
            }

            var so = new SerializedObject(wm);

            var wavesProp = so.FindProperty("_waves");
            wavesProp.arraySize = 1;
            wavesProp.GetArrayElementAtIndex(0).objectReferenceValue = wave1;

            var spawnProp = so.FindProperty("_spawnPoints");
            spawnProp.arraySize = 1;
            spawnProp.GetArrayElementAtIndex(0).objectReferenceValue = wps[0];

            var wpProp = so.FindProperty("_waypoints");
            wpProp.arraySize = wps.Length;
            for (int i = 0; i < wps.Length; i++)
                wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];

            so.FindProperty("_eliteEnemyPrefab").objectReferenceValue = elitePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSceneBootstrap()
        {
            var go = new GameObject("SceneBootstrap");
            go.AddComponent<SceneBootstrap>();
        }

        // ── UI ────────────────────────────────────────────────────────────────────

        static void CreateUI(Sprite whiteSprite)
        {
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            BuildHUD(canvasGO, whiteSprite);
            BuildVictoryPanel(canvasGO, whiteSprite);
        }

        static void BuildHUD(GameObject canvas, Sprite sprite)
        {
            var hudGO = new GameObject("BattleHUD");
            hudGO.transform.SetParent(canvas.transform, false);
            StretchFull(hudGO);

            // Semi-transparent stats panel (top-left)
            var panel = MakePanel(hudGO, "StatsPanel", sprite,
                new Color(0f, 0f, 0f, 0.55f),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(8, -8), new Vector2(190, 160));

            var livesText = MakeLabel(panel, "LivesText", "Lives: 20", new Vector2(10, -10));
            var waveText  = MakeLabel(panel, "WaveText",  "Wave: 0",   new Vector2(10, -45));
            var manaText  = MakeLabel(panel, "ManaText",  "Mana: 100", new Vector2(10, -80));
            var goldText  = MakeLabel(panel, "GoldText",  "Gold: 0",   new Vector2(10, -115));

            var pauseBtn    = MakeButton(hudGO, "PauseButton",    "Pause",     sprite,
                new Vector2(1,1), new Vector2(1,1), new Vector2(1,1), new Vector2(-10,-10),  new Vector2(110,44));
            var nextWaveBtn = MakeButton(hudGO, "NextWaveButton", "Next Wave", sprite,
                new Vector2(1,1), new Vector2(1,1), new Vector2(1,1), new Vector2(-130,-10), new Vector2(120,44));

            var hud = hudGO.AddComponent<BattleHUD>();
            var so  = new SerializedObject(hud);
            so.FindProperty("_livesText").objectReferenceValue      = livesText;
            so.FindProperty("_waveText").objectReferenceValue       = waveText;
            so.FindProperty("_manaText").objectReferenceValue       = manaText;
            so.FindProperty("_goldText").objectReferenceValue       = goldText;
            so.FindProperty("_pauseButton").objectReferenceValue    = pauseBtn;
            so.FindProperty("_nextWaveButton").objectReferenceValue = nextWaveBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildVictoryPanel(GameObject canvas, Sprite sprite)
        {
            var go = new GameObject("VictoryDefeatPanel");
            go.transform.SetParent(canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.25f);
            rt.anchorMax = new Vector2(0.75f, 0.75f);
            rt.sizeDelta = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color  = new Color(0f, 0f, 0f, 0.88f);

            var resultText = MakeCenteredLabel(go, "ResultText", "VICTORY!", 0.5f, 0.78f, 36);
            var statsText  = MakeCenteredLabel(go, "StatsText",  "",         0.5f, 0.52f, 18);
            var retryBtn   = MakeCenteredButton(go, "RetryButton", "Retry",  sprite, 0.35f, 0.2f);
            var quitBtn    = MakeCenteredButton(go, "QuitButton",  "Quit",   sprite, 0.65f, 0.2f);

            go.SetActive(false);

            var panel = go.AddComponent<VictoryDefeatPanel>();
            var so    = new SerializedObject(panel);
            so.FindProperty("_resultText").objectReferenceValue  = resultText;
            so.FindProperty("_statsText").objectReferenceValue   = statsText;
            so.FindProperty("_retryButton").objectReferenceValue = retryBtn;
            so.FindProperty("_quitButton").objectReferenceValue  = quitBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        static Font GetFont() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        static void StretchFull(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        static GameObject MakePanel(GameObject parent, string name, Sprite sprite, Color color,
                                    Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
                                    Vector2 ancPos, Vector2 size)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = ancMin;
            rt.anchorMax        = ancMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = ancPos;
            rt.sizeDelta        = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color  = color;
            return go;
        }

        static Text MakeLabel(GameObject parent, string name, string content, Vector2 ancPos,
                              int fontSize = 22)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(0, 1);
            rt.anchoredPosition = ancPos;
            rt.sizeDelta        = new Vector2(0, 32);
            var t = go.AddComponent<Text>();
            t.text      = content;
            t.font      = GetFont();
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleLeft;
            return t;
        }

        static Text MakeCenteredLabel(GameObject parent, string name, string content,
                                      float ancX, float ancY, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ancX, ancY);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(420, 55);
            var t = go.AddComponent<Text>();
            t.text      = content;
            t.font      = GetFont();
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        static Button MakeButton(GameObject parent, string name, string label, Sprite sprite,
                                 Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
                                 Vector2 ancPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = ancMin;
            rt.anchorMax        = ancMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = ancPos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color  = new Color(0.15f, 0.2f, 0.45f, 0.9f);
            var btn = go.AddComponent<Button>();

            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRt = lblGO.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = Vector2.zero;
            var lbl = lblGO.AddComponent<Text>();
            lbl.text      = label;
            lbl.font      = GetFont();
            lbl.fontSize  = 18;
            lbl.color     = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;

            return btn;
        }

        static Button MakeCenteredButton(GameObject parent, string name, string label,
                                         Sprite sprite, float ancX, float ancY)
        {
            return MakeButton(parent, name, label, sprite,
                new Vector2(ancX, ancY), new Vector2(ancX, ancY), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(130, 46));
        }

        // ── Misc ──────────────────────────────────────────────────────────────────

        static GameObject Child<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            go.AddComponent<T>();
            return go;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
#endif
