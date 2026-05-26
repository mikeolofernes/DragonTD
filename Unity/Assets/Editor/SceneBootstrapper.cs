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

        // Path layout on a 12×8 grid, origin (-5.5, -3.5):
        //   Entry  row 4: cols 0-2
        //   Up     col 2: rows 5-6
        //   Across row 6: cols 3-8
        //   Down   col 8: rows 5-1
        //   Exit   row 1: cols 9-11
        private static readonly Vector2Int[] PathTiles = {
            new(0,4),new(1,4),new(2,4),
            new(2,5),new(2,6),
            new(3,6),new(4,6),new(5,6),new(6,6),new(7,6),new(8,6),
            new(8,5),new(8,4),new(8,3),new(8,2),new(8,1),
            new(9,1),new(10,1),new(11,1)
        };

        // Waypoints matching the path corners
        private static readonly Vector3[] WaypointPositions = {
            new(-6.5f,  0.5f, 0f), // WP_00 spawn (off-screen left)
            new(-3.5f,  0.5f, 0f), // WP_01 first turn
            new(-3.5f,  2.5f, 0f), // WP_02 second turn
            new( 2.5f,  2.5f, 0f), // WP_03 third turn
            new( 2.5f, -2.5f, 0f), // WP_04 fourth turn
            new( 6.5f, -2.5f, 0f), // WP_05 exit (off-screen right)
        };

        [MenuItem("Dragon Dominion/★ Build Battle Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[Dragon Dominion] Stop Play mode first.");
                return;
            }

            // ── Folders ──────────────────────────────────────────────────────────
            foreach (var d in new[]{
                "Assets/ScriptableObjects",
                SODir+"/Enemies", SODir+"/Waves", SODir+"/Director",
                SODir+"/Skills",  SODir+"/Dragons",
                "Assets/Prefabs",
                PrefDir+"/Enemies", PrefDir+"/Dragons", PrefDir+"/UI",
                "Assets/Art", ArtDir, "Assets/Scenes"})
                EnsureDir(d);

            // ── Create assets (before NewScene) ──────────────────────────────────
            CreateGrassSprite();
            CreateDirtSprite();
            CreateDirectorConfig();
            var orcData = CreateOrcData();
            CreateTilePrefab();
            CreateOrcPrefab(orcData);
            CreateProjectilePrefab();
            CreateDragonTowerPrefab("Voltaris",  new Color(1f,   0.85f, 0.1f));
            CreateDragonTowerPrefab("Frostfang", new Color(0.3f, 0.85f, 1f  ));
            CreateDragonTowerPrefab("Magmaclaw", new Color(1f,   0.35f, 0.1f));
            CreateNormalAttack("voltaris_strike_001",  4.0f, 100f, 1.1f);
            CreateNormalAttack("frostfang_bite_001",   3.5f,  80f, 1.5f);
            CreateNormalAttack("magmaclaw_slash_001",  3.0f, 120f, 1.8f);
            CreateDragonDef("voltaris_001",  "Voltaris",  DragonElement.Lightning, DragonRarity.Epic,
                            800f, 100f, 4.0f, 80, "voltaris_strike_001",  "Voltaris");
            CreateDragonDef("frostfang_002", "Frostfang", DragonElement.Ice,       DragonRarity.Rare,
                            1000f, 80f, 3.5f, 60, "frostfang_bite_001",   "Frostfang");
            CreateDragonDef("magmaclaw_003", "Magmaclaw", DragonElement.Fire,      DragonRarity.Uncommon,
                            600f, 120f, 3.0f, 50, "magmaclaw_slash_001",  "Magmaclaw");
            CreateWave1(AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcEnemy.prefab"));
            CreateCardPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ── Build scene (reload all references fresh after NewScene) ─────────
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var grassSpr  = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir+"/grass.png");
            var dirtSpr   = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir+"/dirt.png");
            var whiteSpr  = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir+"/white.png");
            var dirCfg    = AssetDatabase.LoadAssetAtPath<GameDirectorConfig>(SODir+"/Director/DefaultDirectorConfig.asset");
            var tilePref  = AssetDatabase.LoadAssetAtPath<GridTile>(PrefDir+"/GridTile.prefab");
            var orcPref   = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcEnemy.prefab");
            var wave1     = AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave01.asset");
            var voltDef   = AssetDatabase.LoadAssetAtPath<DragonDefinition>(SODir+"/Dragons/voltaris_001.asset");
            var frostDef  = AssetDatabase.LoadAssetAtPath<DragonDefinition>(SODir+"/Dragons/frostfang_002.asset");
            var magmaDef  = AssetDatabase.LoadAssetAtPath<DragonDefinition>(SODir+"/Dragons/magmaclaw_003.asset");
            var cardPref  = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/UI/PlacementCard.prefab");

            var starters = new DragonDefinition[]{ voltDef, frostDef, magmaDef };

            SetupCamera();
            CreateManagerRoot(dirCfg, starters);
            SetupGridManager(tilePref, grassSpr, dirtSpr);
            CreateWaveManager(wave1, orcPref);
            AddSceneBootstrap();
            BuildUI(whiteSpr, cardPref);
            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/BattleScene.unity");
            Debug.Log("[Dragon Dominion] BattleScene ready — press Play!");
        }

        // ── Folder helper ──────────────────────────────────────────────────────────

        static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ── Procedural sprites ────────────────────────────────────────────────────

        static Sprite CreateGrassSprite()
        {
            const string path = ArtDir+"/grass.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return SaveProceduralSprite(path, 32, 32, (x,y) => {
                float n = Mathf.PerlinNoise(x * 0.35f + 7f, y * 0.35f + 3f);
                return new Color(Mathf.Lerp(0.14f,0.22f,n), Mathf.Lerp(0.42f,0.58f,n), Mathf.Lerp(0.09f,0.16f,n));
            });
        }

        static Sprite CreateDirtSprite()
        {
            const string path = ArtDir+"/dirt.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return SaveProceduralSprite(path, 32, 32, (x,y) => {
                float n = Mathf.PerlinNoise(x * 0.40f + 50f, y * 0.40f + 80f);
                return new Color(Mathf.Lerp(0.50f,0.65f,n), Mathf.Lerp(0.36f,0.48f,n), Mathf.Lerp(0.18f,0.27f,n));
            });
        }

        static Sprite GetOrCreateWhiteSprite()
        {
            const string path = ArtDir+"/white.png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            return SaveProceduralSprite(path, 4, 4, (x,y) => Color.white);
        }

        static Sprite SaveProceduralSprite(string path, int w, int h,
                                           System.Func<float,float,Color> colorFn)
        {
            var tex = new Texture2D(w, h);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    tex.SetPixel(x, y, colorFn(x, y));
            tex.Apply();

            File.WriteAllBytes(
                Path.Combine(Application.dataPath.Replace("/Assets",""), path),
                tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType        = TextureImporterType.Sprite;
            imp.spriteImportMode   = SpriteImportMode.Single;
            imp.filterMode         = FilterMode.Point;
            imp.spritePixelsPerUnit = w; // w px = 1 world unit → square tile fills exactly 1 unit
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ── ScriptableObjects ─────────────────────────────────────────────────────

        static GameDirectorConfig CreateDirectorConfig()
        {
            const string path = SODir+"/Director/DefaultDirectorConfig.asset";
            var ex = AssetDatabase.LoadAssetAtPath<GameDirectorConfig>(path);
            if (ex != null) return ex;
            var cfg = ScriptableObject.CreateInstance<GameDirectorConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            return cfg;
        }

        static EnemyData CreateOrcData()
        {
            const string path = SODir+"/Enemies/OrcScout.asset";
            var ex = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (ex != null) return ex;
            var d = ScriptableObject.CreateInstance<EnemyData>();
            d.EnemyName = "Orc Scout"; d.MaxHp = 200f; d.MoveSpeed = 2.5f;
            d.Armor = 5f; d.GoldValue = 10; d.DamageToBase = 1;
            d.Faction = EnemyFaction.Orc;
            AssetDatabase.CreateAsset(d, path);
            return d;
        }

        static WaveData CreateWave1(GameObject orcPrefab)
        {
            const string path = SODir+"/Waves/Wave01.asset";
            var w = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (w == null)
            {
                w = ScriptableObject.CreateInstance<WaveData>();
                w.EnemyGroups = new[]{ new EnemySpawnEntry{ Count=8, SpawnInterval=1.2f } };
                w.TimeBetweenGroups = 3f; w.GoldReward = 50; w.ManaReward = 20;
                AssetDatabase.CreateAsset(w, path);
            }

            // Always refresh the prefab ref — it may have been stale from a prior build
            var so = new SerializedObject(w);
            var groups = so.FindProperty("EnemyGroups");
            if (groups != null && groups.arraySize > 0)
                groups.GetArrayElementAtIndex(0)
                      .FindPropertyRelative("EnemyPrefab")
                      .objectReferenceValue = orcPrefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(w);
            return w;
        }

        static void CreateNormalAttack(string skillId, float range, float baseDmg, float cooldown)
        {
            string path = SODir+"/Skills/"+skillId+".asset";
            if (AssetDatabase.LoadAssetAtPath<SkillDefinition>(path) != null) return;
            var s = ScriptableObject.CreateInstance<SkillDefinition>();
            s.skillId = skillId; s.displayName = skillId;
            s.skillType = SkillType.Damage; s.targetType = TargetType.Single;
            s.range = range; s.baseDamage = baseDmg; s.cooldown = cooldown;
            // levelMultipliers uses SkillDefinition default {1,1.1,...}
            AssetDatabase.CreateAsset(s, path);
        }

        static void CreateDragonDef(string id, string name, DragonElement element,
                                    DragonRarity rarity, float hp, float atk,
                                    float range, int mana, string skillId, string towerName)
        {
            string path = SODir+"/Dragons/"+id+".asset";
            if (AssetDatabase.LoadAssetAtPath<DragonDefinition>(path) != null) return;

            var def = ScriptableObject.CreateInstance<DragonDefinition>();
            var so  = new SerializedObject(def);
            so.FindProperty("dragonId").stringValue    = id;
            so.FindProperty("displayName").stringValue = name;
            so.FindProperty("element").enumValueIndex  = (int)element;
            so.FindProperty("rarity").enumValueIndex   = (int)rarity;
            so.FindProperty("manaCost").intValue       = mana;

            var bs = so.FindProperty("baseStats");
            bs.FindPropertyRelative("hp").floatValue          = hp;
            bs.FindPropertyRelative("attack").floatValue      = atk;
            bs.FindPropertyRelative("range").floatValue       = range;
            bs.FindPropertyRelative("attackSpeed").floatValue = 1f;

            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SODir+"/Skills/"+skillId+".asset");
            so.FindProperty("skillSet").FindPropertyRelative("normalAttack").objectReferenceValue = skill;

            var tower = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Dragons/"+towerName+"Tower.prefab");
            so.FindProperty("visualData").FindPropertyRelative("hatchlingPrefab").objectReferenceValue = tower;

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(def, path);
        }

        // ── Prefabs ───────────────────────────────────────────────────────────────

        static GridTile CreateTilePrefab()
        {
            const string path = PrefDir+"/GridTile.prefab";

            // Always rebuild tile prefab so grass/dirt sprites are wired fresh
            var grassSpr = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir+"/grass.png");
            var dirtSpr  = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir+"/dirt.png");
            if (grassSpr == null) grassSpr = CreateGrassSprite();
            if (dirtSpr  == null) dirtSpr  = CreateDirtSprite();

            var go = new GameObject("GridTile");
            go.transform.localScale = new Vector3(0.94f, 0.94f, 1f); // small gap

            var sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite = grassSpr;
            go.AddComponent<BoxCollider2D>();

            var tile = go.AddComponent<GridTile>();
            var so   = new SerializedObject(tile);
            so.FindProperty("_spriteRenderer").objectReferenceValue  = sr;
            so.FindProperty("_buildableSprite").objectReferenceValue = grassSpr;
            so.FindProperty("_pathSprite").objectReferenceValue      = dirtSpr;
            so.ApplyModifiedProperties();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<GridTile>();
        }

        static void CreateOrcPrefab(EnemyData data)
        {
            const string path = PrefDir+"/Enemies/OrcEnemy.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            var whiteSpr = GetOrCreateWhiteSprite();
            var go = new GameObject("OrcEnemy");
            go.transform.localScale = Vector3.one * 0.75f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSpr;
            sr.color  = new Color(0.35f, 0.75f, 0.2f);
            go.AddComponent<BoxCollider2D>();

            var orc = go.AddComponent<OrcEnemy>();
            var so  = new SerializedObject(orc);
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateProjectilePrefab()
        {
            const string path = PrefDir+"/Projectile.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var whiteSpr = GetOrCreateWhiteSprite();
            var go = new GameObject("Projectile");
            go.transform.localScale = Vector3.one * 0.18f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSpr;
            sr.color  = Color.white;
            sr.sortingOrder = 5;
            go.AddComponent<ProjectileBase>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateDragonTowerPrefab(string dragonName, Color color)
        {
            string path = PrefDir+"/Dragons/"+dragonName+"Tower.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var whiteSpr   = GetOrCreateWhiteSprite();
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Projectile.prefab");

            var go = new GameObject(dragonName+"Tower");
            go.transform.localScale = Vector3.one * 0.85f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSpr;
            sr.color  = color;
            sr.sortingOrder = 2;

            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform);
            fp.transform.localPosition = Vector3.zero;

            var tower = go.AddComponent<DragonTower>();
            var so    = new SerializedObject(tower);
            so.FindProperty("_projectilePrefab").objectReferenceValue = projPrefab;
            so.FindProperty("_firePoint").objectReferenceValue        = fp.transform;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateCardPrefab()
        {
            const string path = PrefDir+"/UI/PlacementCard.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var whiteSpr = GetOrCreateWhiteSprite();

            var root = new GameObject("PlacementCard");
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(100f, 130f);

            // Background
            var bg = root.AddComponent<Image>();
            bg.sprite = whiteSpr;
            bg.color  = new Color(0.1f, 0.12f, 0.22f, 0.95f);

            // Portrait image (top 70%)
            var portGO = new GameObject("Portrait");
            portGO.transform.SetParent(root.transform, false);
            var portRt = portGO.AddComponent<RectTransform>();
            portRt.anchorMin = new Vector2(0.05f, 0.38f);
            portRt.anchorMax = new Vector2(0.95f, 0.95f);
            portRt.sizeDelta = Vector2.zero;
            var portImg = portGO.AddComponent<Image>();
            portImg.sprite = whiteSpr;
            portImg.color  = new Color(0.4f, 0.4f, 0.5f, 1f); // placeholder portrait

            // Name text
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(root.transform, false);
            var nameRt = nameGO.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0,0.2f);
            nameRt.anchorMax = new Vector2(1,0.38f);
            nameRt.sizeDelta = Vector2.zero;
            var nameT  = nameGO.AddComponent<Text>();
            nameT.font = GetFont(); nameT.fontSize = 13; nameT.color = Color.white;
            nameT.alignment = TextAnchor.MiddleCenter; nameT.text = "Dragon";

            // Cost text
            var costGO = new GameObject("CostText");
            costGO.transform.SetParent(root.transform, false);
            var costRt = costGO.AddComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0,0.02f);
            costRt.anchorMax = new Vector2(1,0.2f);
            costRt.sizeDelta = Vector2.zero;
            var costT  = costGO.AddComponent<Text>();
            costT.font = GetFont(); costT.fontSize = 12; costT.color = new Color(0.4f,0.8f,1f);
            costT.alignment = TextAnchor.MiddleCenter; costT.text = "80 MP";

            // Transparent button overlay (full card)
            var btnGO = new GameObject("SelectButton");
            btnGO.transform.SetParent(root.transform, false);
            var btnRt = btnGO.AddComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero; btnRt.anchorMax = Vector2.one; btnRt.sizeDelta = Vector2.zero;
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = Color.clear;
            var btn = btnGO.AddComponent<Button>();

            // Wire DragonPlacementCard
            var card = root.AddComponent<DragonPlacementCard>();
            var so   = new SerializedObject(card);
            so.FindProperty("_portrait").objectReferenceValue      = portImg;
            so.FindProperty("_nameText").objectReferenceValue      = nameT;
            so.FindProperty("_manaCostText").objectReferenceValue  = costT;
            so.FindProperty("_selectButton").objectReferenceValue  = btn;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ── Scene GameObjects ──────────────────────────────────────────────────────

        static void SetupCamera()
        {
            var go  = new GameObject("Main Camera");
            go.tag  = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic      = true;
            cam.orthographicSize  = 5f;
            cam.clearFlags        = CameraClearFlags.SolidColor;
            cam.backgroundColor   = new Color(0.04f, 0.06f, 0.04f); // dark ground color
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void CreateManagerRoot(GameDirectorConfig dirCfg, DragonDefinition[] starters)
        {
            var gmGO  = Root<GameManager>("GameManager");
            var rmGO  = Root<ResourceManager>("ResourceManager");
            var pmGO  = Root<PlacementManager>("PlacementManager");
            var piGO  = Root<PlayerInventory>("PlayerInventory");
            var dalGO = Root<DragonAssetLoader>("DragonAssetLoader");
            var dirGO = Root<GameDirector>("GameDirector");

            // Wire starter dragons into PlayerInventory
            var pi   = piGO.GetComponent<PlayerInventory>();
            var piSO = new SerializedObject(pi);
            var sp   = piSO.FindProperty("_starterDragons");
            sp.arraySize = starters.Length;
            for (int i = 0; i < starters.Length; i++)
                sp.GetArrayElementAtIndex(i).objectReferenceValue = starters[i];
            piSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(pi);

            // Wire director config
            var dir  = dirGO.GetComponent<GameDirector>();
            var dSO  = new SerializedObject(dir);
            dSO.FindProperty("_config").objectReferenceValue = dirCfg;
            dSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(dir);
        }

        static void SetupGridManager(GridTile tilePref, Sprite grassSpr, Sprite dirtSpr)
        {
            var go = new GameObject("GridManager");
            var gm = go.AddComponent<GridManager>();
            var so = new SerializedObject(gm);
            so.FindProperty("_tilePrefab").objectReferenceValue  = tilePref;
            so.FindProperty("_width").intValue                   = 12;
            so.FindProperty("_height").intValue                  = 8;
            so.FindProperty("_originPosition").vector3Value      = new Vector3(-5.5f, -3.5f, 0f);

            var pathProp = so.FindProperty("_pathTiles");
            pathProp.arraySize = PathTiles.Length;
            for (int i = 0; i < PathTiles.Length; i++)
            {
                var elem = pathProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("x").intValue = PathTiles[i].x;
                elem.FindPropertyRelative("y").intValue = PathTiles[i].y;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gm);
        }

        static void CreateWaveManager(WaveData wave1, GameObject elitePrefab)
        {
            var root = new GameObject("WaveSetup");
            var wm   = root.AddComponent<WaveManager>();

            var pathViz = new GameObject("WaypointPath");
            pathViz.transform.SetParent(root.transform);
            pathViz.AddComponent<WaypointPath>();

            var wps = new Transform[WaypointPositions.Length];
            for (int i = 0; i < WaypointPositions.Length; i++)
            {
                var wp = new GameObject($"WP_{i:00}");
                wp.transform.SetParent(pathViz.transform);
                wp.transform.position = WaypointPositions[i];
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
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wm);
        }

        static void AddSceneBootstrap()
        {
            var go = new GameObject("SceneBootstrap");
            go.AddComponent<SceneBootstrap>();
        }

        // ── UI ────────────────────────────────────────────────────────────────────

        static void BuildUI(Sprite whiteSpr, GameObject cardPrefab)
        {
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            BuildHUD(canvasGO, whiteSpr);
            BuildDragonPanel(canvasGO, whiteSpr, cardPrefab);
            BuildVictoryPanel(canvasGO, whiteSpr);
        }

        static void BuildHUD(GameObject canvas, Sprite sprite)
        {
            var hudGO = new GameObject("BattleHUD");
            hudGO.transform.SetParent(canvas.transform, false);
            StretchFull(hudGO);

            var statsPanel = MakePanel(hudGO, "StatsPanel", sprite,
                new Color(0f,0f,0f,0.6f),
                new Vector2(0,1), new Vector2(0,1), new Vector2(0,1),
                new Vector2(8,-8), new Vector2(190,160));

            var livesText = MakeLabel(statsPanel, "LivesText", "Lives: 20", new Vector2(10,-10));
            var waveText  = MakeLabel(statsPanel, "WaveText",  "Wave: 0",   new Vector2(10,-45));
            var manaText  = MakeLabel(statsPanel, "ManaText",  "Mana: 100", new Vector2(10,-80));
            var goldText  = MakeLabel(statsPanel, "GoldText",  "Gold: 0",   new Vector2(10,-115));

            var pauseBtn    = MakeButton(hudGO,"PauseButton","Pause",sprite,
                new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-10,-10),new Vector2(110,44));
            var nextWaveBtn = MakeButton(hudGO,"NextWaveButton","Next Wave",sprite,
                new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-130,-10),new Vector2(120,44));

            var hud = hudGO.AddComponent<BattleHUD>();
            var so  = new SerializedObject(hud);
            so.FindProperty("_livesText").objectReferenceValue      = livesText;
            so.FindProperty("_waveText").objectReferenceValue       = waveText;
            so.FindProperty("_manaText").objectReferenceValue       = manaText;
            so.FindProperty("_goldText").objectReferenceValue       = goldText;
            so.FindProperty("_pauseButton").objectReferenceValue    = pauseBtn;
            so.FindProperty("_nextWaveButton").objectReferenceValue = nextWaveBtn;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);
        }

        static void BuildDragonPanel(GameObject canvas, Sprite sprite, GameObject cardPrefab)
        {
            // Bottom bar: dragon selection panel
            var panelGO = new GameObject("DragonCollectionPanel");
            panelGO.transform.SetParent(canvas.transform, false);
            var rt = panelGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,0);
            rt.anchorMax = new Vector2(1,0);
            rt.pivot     = new Vector2(0.5f,0);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0,145);

            var bg = panelGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color  = new Color(0f,0f,0f,0.65f);

            // Card container (horizontal layout)
            var containerGO = new GameObject("CardContainer");
            containerGO.transform.SetParent(panelGO.transform, false);
            var crt = containerGO.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.sizeDelta = Vector2.zero;
            crt.offsetMin = new Vector2(8, 7);
            crt.offsetMax = new Vector2(-8,-7);

            var layout = containerGO.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth  = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth  = false;
            layout.childForceExpandHeight = false;

            var col = panelGO.AddComponent<DragonCollectionPanel>();
            var so  = new SerializedObject(col);
            so.FindProperty("_cardContainer").objectReferenceValue = containerGO.transform;
            so.FindProperty("_cardPrefab").objectReferenceValue    = cardPrefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(col);
        }

        static void BuildVictoryPanel(GameObject canvas, Sprite sprite)
        {
            var go = new GameObject("VictoryDefeatPanel");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f,0.25f);
            rt.anchorMax = new Vector2(0.75f,0.75f);
            rt.sizeDelta = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color  = new Color(0f,0f,0f,0.88f);

            var resultText = MakeCenteredLabel(go,"ResultText","VICTORY!",0.5f,0.78f,36);
            var statsText  = MakeCenteredLabel(go,"StatsText", "",        0.5f,0.52f,18);
            var retryBtn   = MakeCenteredButton(go,"RetryButton","Retry",sprite,0.35f,0.2f);
            var quitBtn    = MakeCenteredButton(go,"QuitButton", "Quit", sprite,0.65f,0.2f);

            go.SetActive(false);

            var panel = go.AddComponent<VictoryDefeatPanel>();
            var so    = new SerializedObject(panel);
            so.FindProperty("_resultText").objectReferenceValue  = resultText;
            so.FindProperty("_statsText").objectReferenceValue   = statsText;
            so.FindProperty("_retryButton").objectReferenceValue = retryBtn;
            so.FindProperty("_quitButton").objectReferenceValue  = quitBtn;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(panel);
        }

        // ── UI helpers ─────────────────────────────────────────────────────────────

        static Font GetFont() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        static void StretchFull(GameObject go)
        {
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        }

        static GameObject MakePanel(GameObject parent, string name, Sprite sprite, Color color,
                                    Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go  = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = color;
            return go;
        }

        static Text MakeLabel(GameObject parent, string name, string content, Vector2 ancPos,
                              int fontSize = 22)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0,1); rt.anchorMax = new Vector2(1,1);
            rt.pivot = new Vector2(0,1); rt.anchoredPosition = ancPos;
            rt.sizeDelta = new Vector2(0,32);
            var t = go.AddComponent<Text>();
            t.text = content; t.font = GetFont();
            t.fontSize = fontSize; t.color = Color.white;
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
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(420,55);
            var t = go.AddComponent<Text>();
            t.text = content; t.font = GetFont();
            t.fontSize = fontSize; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        static Button MakeButton(GameObject parent, string name, string label, Sprite sprite,
                                 Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite; img.color = new Color(0.12f,0.18f,0.4f,0.9f);
            var btn = go.AddComponent<Button>();
            var lblGO = new GameObject("Text");
            lblGO.transform.SetParent(go.transform, false);
            var lblRt = lblGO.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one; lblRt.sizeDelta = Vector2.zero;
            var lbl = lblGO.AddComponent<Text>();
            lbl.text = label; lbl.font = GetFont();
            lbl.fontSize = 18; lbl.color = Color.white;
            lbl.alignment = TextAnchor.MiddleCenter;
            return btn;
        }

        static Button MakeCenteredButton(GameObject parent, string name, string label,
                                         Sprite sprite, float ancX, float ancY) =>
            MakeButton(parent, name, label, sprite,
                new Vector2(ancX,ancY), new Vector2(ancX,ancY), new Vector2(0.5f,0.5f),
                Vector2.zero, new Vector2(130,46));

        // ── Misc ──────────────────────────────────────────────────────────────────

        static GameObject Root<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.AddComponent<T>();
            return go;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }
    }
}
#endif
