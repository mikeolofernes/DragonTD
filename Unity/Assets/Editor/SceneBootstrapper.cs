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
        private const string DragonArtDir = "Assets/Art/Dragons";

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
                "Assets/Art", ArtDir, DragonArtDir, "Assets/Scenes"})
                EnsureDir(d);
            foreach (var dragon in Phase1DragonData.All)
                EnsureDir(DragonArtDir+"/"+dragon.Id);

            // ── Create assets (before NewScene) ──────────────────────────────────
            CreateGrassSprite();
            CreateDirtSprite();
            CreateDirectorConfig();
            var orcData = CreateOrcData();
            var runnerData = CreateRunnerData();
            var bruteData = CreateBruteData();
            var shieldedData = CreateShieldedData();
            var regenData = CreateRegenData();
            var flyingData = CreateFlyingData();
            CreateTilePrefab();
            CreateOrcPrefab(orcData);
            CreateEnemyPrefab("OrcRunner", runnerData, new Color(0.55f, 1f, 0.35f), 0.62f);
            CreateEnemyPrefab("OrcBrute", bruteData, new Color(0.62f, 0.38f, 0.18f), 0.95f);
            CreateEnemyPrefab("OrcShielded", shieldedData, new Color(0.25f, 0.7f, 1f), 0.82f);
            CreateEnemyPrefab("OrcRegenerator", regenData, new Color(0.35f, 1f, 0.55f), 0.78f);
            CreateEnemyPrefab("OrcFlying", flyingData, new Color(0.85f, 0.65f, 1f), 0.58f);
            CreateProjectilePrefab();
            ConfigurePortraitImports();
            foreach (var dragon in Phase1DragonData.All)
            {
                CreateDragonTowerPrefab(dragon);
                CreateNormalAttack(dragon);
                CreateActiveSkill(dragon);
                CreateDragonDef(dragon);
            }
            var orcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcEnemy.prefab");
            var runnerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcRunner.prefab");
            var brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcBrute.prefab");
            var shieldedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcShielded.prefab");
            var regenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcRegenerator.prefab");
            var flyingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcFlying.prefab");
            CreateWave("Wave01", 120, 90,
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 4, SpawnInterval = 1.25f },
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 5, SpawnInterval = 0.82f });
            CreateWave("Wave02", 165, 115,
                new EnemySpawnEntry{ EnemyPrefab = orcPrefab, Count = 5, SpawnInterval = 1.0f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 2, SpawnInterval = 1.3f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 3, SpawnInterval = 1.1f });
            CreateWave("Wave03", 300, 190,
                new EnemySpawnEntry{ EnemyPrefab = runnerPrefab, Count = 5, SpawnInterval = 0.64f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 5, SpawnInterval = 0.9f },
                new EnemySpawnEntry{ EnemyPrefab = flyingPrefab, Count = 5, SpawnInterval = 0.82f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 4, SpawnInterval = 0.95f },
                new EnemySpawnEntry{ EnemyPrefab = brutePrefab, Count = 3, SpawnInterval = 1.08f },
                new EnemySpawnEntry{ EnemyPrefab = shieldedPrefab, Count = 2, SpawnInterval = 0.7f },
                new EnemySpawnEntry{ EnemyPrefab = regenPrefab, Count = 2, SpawnInterval = 0.7f });
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
            var orcPref   = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Enemies/OrcBrute.prefab");
            var waves     = new[]{
                AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave01.asset"),
                AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave02.asset"),
                AssetDatabase.LoadAssetAtPath<WaveData>(SODir+"/Waves/Wave03.asset")
            };
            var starters  = Phase1DragonData.All
                .Select(d => AssetDatabase.LoadAssetAtPath<DragonDefinition>(SODir+"/Dragons/"+d.Id+".asset"))
                .Where(d => d != null)
                .ToArray();
            var cardPref  = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/UI/PlacementCard.prefab");

            SetupCamera();
            CreateManagerRoot(dirCfg, starters);
            SetupGridManager(tilePref, grassSpr, dirtSpr);
            CreatePathDirectionMarkers(whiteSpr);
            CreateWaveManager(waves, orcPref);
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
            return CreateEnemyData("OrcScout", "Orc Scout", 185f, 2.35f, 4f, 10, 1, EnemyFaction.Orc, EnemyTrait.None);
        }

        static EnemyData CreateRunnerData()
        {
            return CreateEnemyData("OrcRunner", "Orc Runner", 120f, 3.35f, 1f, 8, 1, EnemyFaction.Orc, EnemyTrait.Runner);
        }

        static EnemyData CreateBruteData()
        {
            return CreateEnemyData("OrcBrute", "Orc Brute", 460f, 1.45f, 18f, 26, 3, EnemyFaction.Troll, EnemyTrait.Brute);
        }

        static EnemyData CreateShieldedData()
        {
            return CreateEnemyData("OrcShielded", "Shielded Orc", 260f, 2.0f, 8f, 18, 1, EnemyFaction.Orc,
                EnemyTrait.Shielded, 0f, PrototypeBalance.ShieldProjectileMultiplier, PrototypeBalance.ShieldSkillMultiplier, DragonElement.Lightning, true);
        }

        static EnemyData CreateRegenData()
        {
            return CreateEnemyData("OrcRegenerator", "Regenerating Orc", 300f, 1.9f, 5f, 20, 1, EnemyFaction.Undead,
                EnemyTrait.Regenerating, PrototypeBalance.RegenPerSecond, 1f, 1f, DragonElement.Fire, true);
        }

        static EnemyData CreateFlyingData()
        {
            return CreateEnemyData("OrcFlying", "Flying Orc", 155f, 3.0f, 2f, 18, 1, EnemyFaction.CorruptedDragon,
                EnemyTrait.Flying, 0f, 1f, 1f, DragonElement.Wind, true);
        }

        static EnemyData CreateEnemyData(string assetName, string enemyName, float hp, float speed,
                                         float armor, int gold, int baseDamage, EnemyFaction faction,
                                         EnemyTrait trait, float regenPerSecond = 0f,
                                         float projectileDamageMultiplier = 1f,
                                         float skillDamageMultiplier = 1f,
                                         DragonElement element = DragonElement.Earth,
                                         bool hasElement = false)
        {
            string path = SODir+"/Enemies/"+assetName+".asset";
            var d = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(d, path);
            }

            d.EnemyName = enemyName;
            d.MaxHp = hp;
            d.MoveSpeed = speed;
            d.Armor = armor;
            d.GoldValue = gold;
            d.DamageToBase = baseDamage;
            d.Faction = faction;
            d.Trait = trait;
            d.RegenPerSecond = regenPerSecond;
            d.ProjectileDamageMultiplier = projectileDamageMultiplier;
            d.SkillDamageMultiplier = skillDamageMultiplier;
            d.Element = element;
            d.HasElement = hasElement;
            EditorUtility.SetDirty(d);
            return d;
        }

        static void ConfigurePortraitImports()
        {
            foreach (var dragon in Phase1DragonData.All)
            {
                string path = DragonArtDir+"/"+dragon.Id+"/portrait.png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 900f))
                {
                    importer.spritePixelsPerUnit = 900f;
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        static WaveData CreateWave(string waveName, int goldReward, int manaReward, params EnemySpawnEntry[] entries)
        {
            string path = SODir+"/Waves/"+waveName+".asset";
            var w = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (w == null)
            {
                w = ScriptableObject.CreateInstance<WaveData>();
                AssetDatabase.CreateAsset(w, path);
            }

            // Always refresh the prefab ref — it may have been stale from a prior build
            var so = new SerializedObject(w);
            var groups = so.FindProperty("EnemyGroups");
            groups.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = groups.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("EnemyPrefab").objectReferenceValue = entries[i].EnemyPrefab;
                entry.FindPropertyRelative("Count").intValue = entries[i].Count;
                entry.FindPropertyRelative("SpawnInterval").floatValue = entries[i].SpawnInterval;
            }
            so.FindProperty("TimeBetweenGroups").floatValue = 2.5f;
            so.FindProperty("GoldReward").intValue = goldReward;
            so.FindProperty("ManaReward").intValue = manaReward;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(w);
            return w;
        }

        static void CreateNormalAttack(Phase1DragonData.Def dragon)
        {
            string path = SODir+"/Skills/"+dragon.NormalSkillId+".asset";
            var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(s, path);
            }
            s.skillId = dragon.NormalSkillId;
            s.displayName = dragon.NormalAttackName;
            s.description = "Prototype tower attack for "+dragon.Name+".";
            s.skillType = SkillType.Damage; s.targetType = TargetType.Single;
            s.range = dragon.Range; s.baseDamage = dragon.Atk * dragon.AtkMult; s.cooldown = dragon.AtkCd;
            s.isAoe = false; s.aoeRadius = 0f;
            s.levelMultipliers = Multipliers(dragon.AtkMult);
            EditorUtility.SetDirty(s);
        }

        static void CreateActiveSkill(Phase1DragonData.Def dragon)
        {
            if (string.IsNullOrWhiteSpace(dragon.SkillName)) return;

            string path = SODir+"/Skills/"+ActiveSkillId(dragon)+".asset";
            var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (s == null)
            {
                s = ScriptableObject.CreateInstance<SkillDefinition>();
                AssetDatabase.CreateAsset(s, path);
            }

            s.skillId = ActiveSkillId(dragon);
            s.displayName = dragon.SkillName;
            s.description = "Prototype active skill for "+dragon.Name+".";
            s.skillType = dragon.SkillName == "Fortify" ? SkillType.Buff : SkillType.Damage;
            s.targetType = dragon.SkillAoe ? TargetType.AoE : TargetType.Single;
            s.range = dragon.Range;
            s.baseDamage = dragon.Atk * dragon.SkillMult;
            s.cooldown = dragon.SkillCd;
            s.isAoe = dragon.SkillAoe;
            s.aoeRadius = dragon.SkillRadius;
            s.levelMultipliers = Multipliers(dragon.SkillMult);
            s.statusEffects = StatusEffectsFor(dragon);
            EditorUtility.SetDirty(s);
        }

        static void CreateDragonDef(Phase1DragonData.Def dragon)
        {
            string path = SODir+"/Dragons/"+dragon.Id+".asset";
            var def = AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DragonDefinition>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so  = new SerializedObject(def);
            so.FindProperty("dragonId").stringValue       = dragon.Id;
            so.FindProperty("displayName").stringValue    = dragon.Name;
            so.FindProperty("dragonClass").enumValueIndex = (int)dragon.Class;
            so.FindProperty("element").enumValueIndex     = (int)dragon.Element;
            so.FindProperty("rarity").enumValueIndex      = (int)dragon.Rarity;
            so.FindProperty("manaCost").intValue          = dragon.ManaCost;

            var bs = so.FindProperty("baseStats");
            bs.FindPropertyRelative("hp").floatValue          = dragon.Hp;
            bs.FindPropertyRelative("attack").floatValue      = dragon.Atk;
            bs.FindPropertyRelative("attackSpeed").floatValue = dragon.AttackSpeed;
            bs.FindPropertyRelative("armor").floatValue       = dragon.Armor;
            bs.FindPropertyRelative("range").floatValue       = dragon.Range;
            bs.FindPropertyRelative("mana").floatValue        = dragon.Mana;

            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SODir+"/Skills/"+dragon.NormalSkillId+".asset");
            var activeSkill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(SODir+"/Skills/"+ActiveSkillId(dragon)+".asset");
            var skillSet = so.FindProperty("skillSet");
            skillSet.FindPropertyRelative("normalAttack").objectReferenceValue = skill;
            skillSet.FindPropertyRelative("activeSkill").objectReferenceValue = activeSkill;

            var tower = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Dragons/"+dragon.Name+"Tower.prefab");
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(DragonArtDir+"/"+dragon.Id+"/portrait.png");
            var visuals = so.FindProperty("visualData");
            visuals.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            visuals.FindPropertyRelative("hatchlingPrefab").objectReferenceValue = tower;
            visuals.FindPropertyRelative("primaryColor").colorValue = ElementColor(dragon.Element);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
        }

        static string ActiveSkillId(Phase1DragonData.Def dragon)
        {
            string baseName = string.IsNullOrWhiteSpace(dragon.SkillName)
                ? dragon.Id+"_active"
                : dragon.SkillName.ToLowerInvariant().Replace(" ", "_");
            return baseName+"_001";
        }

        static float[] Multipliers(float baseMultiplier)
        {
            return new[]
            {
                baseMultiplier,
                baseMultiplier * 1.1f,
                baseMultiplier * 1.2f,
                baseMultiplier * 1.3f,
                baseMultiplier * 1.4f,
                baseMultiplier * 1.5f,
                baseMultiplier * 1.65f,
                baseMultiplier * 1.8f,
                baseMultiplier * 2f,
                baseMultiplier * 2.25f
            };
        }

        static StatusEffect[] StatusEffectsFor(Phase1DragonData.Def dragon)
        {
            string skill = (dragon.SkillName ?? string.Empty).ToLowerInvariant();
            if (skill.Contains("blizzard") || skill.Contains("glacial"))
                return new[]{ new StatusEffect{ effectId = "slow_001", displayName = "Slow", duration = 3f, magnitude = 0.45f } };
            if (skill.Contains("magma"))
                return new[]{ new StatusEffect{ effectId = "burn_001", displayName = "Burn", duration = 3f, magnitude = 18f } };
            if (skill.Contains("chain") || skill.Contains("void"))
                return new[]{ new StatusEffect{ effectId = "shock_vulnerable_001", displayName = "Vulnerable", duration = 2.5f, magnitude = 0.25f } };
            if (skill.Contains("aura"))
                return new[]{ new StatusEffect{ effectId = "slow_001", displayName = "Radiant Slow", duration = 2f, magnitude = 0.25f } };
            return new StatusEffect[0];
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
            CreateEnemyPrefab("OrcEnemy", data, new Color(0.35f, 0.75f, 0.2f), 0.75f);
        }

        static void CreateEnemyPrefab(string prefabName, EnemyData data, Color color, float scale)
        {
            string prefabPath = PrefDir+"/Enemies/"+prefabName+".prefab";

            var whiteSpr = GetOrCreateWhiteSprite();
            var go = new GameObject(prefabName);
            go.transform.localScale = Vector3.one * scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSpr;
            sr.color  = color;
            go.AddComponent<BoxCollider2D>();

            var orc = go.AddComponent<OrcEnemy>();
            var so  = new SerializedObject(orc);
            so.FindProperty("_data").objectReferenceValue = data;
            so.ApplyModifiedProperties();

            CreateEnemyHealthBar(go, whiteSpr);

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        static void CreateEnemyHealthBar(GameObject enemy, Sprite sprite)
        {
            var canvasGO = new GameObject("HealthBar");
            canvasGO.transform.SetParent(enemy.transform, false);
            canvasGO.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.012f;

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;

            var canvasRt = canvasGO.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(64f, 8f);

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRt = bgGO.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bg = bgGO.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color = new Color(0.04f, 0.02f, 0.02f, 0.85f);

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            var fillRt = fillGO.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0.05f, 0.2f);
            fillRt.anchorMax = new Vector2(0.95f, 0.8f);
            fillRt.sizeDelta = Vector2.zero;
            var fill = fillGO.AddComponent<Image>();
            fill.sprite = sprite;
            fill.color = new Color(0.2f, 1f, 0.25f, 0.95f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            var bar = canvasGO.AddComponent<EnemyHealthBar>();
            var so = new SerializedObject(bar);
            so.FindProperty("_fillImage").objectReferenceValue = fill;
            so.ApplyModifiedProperties();
        }

        static void CreateProjectilePrefab()
        {
            const string path = PrefDir+"/Projectile.prefab";
            var whiteSpr = GetOrCreateWhiteSprite();
            var go = new GameObject("Projectile");
            go.transform.localScale = Vector3.one * 0.22f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = whiteSpr;
            sr.color  = Color.white;
            sr.sortingOrder = 6;
            var trail = go.AddComponent<TrailRenderer>();
            trail.time = 0.18f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0.02f;
            trail.startColor = new Color(1f, 1f, 1f, 0.8f);
            trail.endColor = new Color(1f, 1f, 1f, 0f);
            trail.sortingOrder = 4;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            go.AddComponent<ProjectileBase>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static Color ElementColor(DragonElement element) => element switch
        {
            DragonElement.Fire => new Color(1f, 0.35f, 0.1f),
            DragonElement.Ice => new Color(0.3f, 0.85f, 1f),
            DragonElement.Lightning => new Color(1f, 0.82f, 0.15f),
            DragonElement.Earth => new Color(0.42f, 0.62f, 0.28f),
            DragonElement.Light => new Color(1f, 0.92f, 0.55f),
            DragonElement.Shadow => new Color(0.45f, 0.22f, 0.85f),
            _ => Color.white
        };

        static void CreateDragonTowerPrefab(Phase1DragonData.Def dragon)
        {
            string dragonName = dragon.Name;
            string path = PrefDir+"/Dragons/"+dragonName+"Tower.prefab";
            var whiteSpr   = GetOrCreateWhiteSprite();
            var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefDir+"/Projectile.prefab");
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(DragonArtDir+"/"+dragon.Id+"/portrait.png");
            Color color = ElementColor(dragon.Element);

            var go = new GameObject(dragonName+"Tower");
            go.transform.localScale = Vector3.one * 0.95f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = portrait != null ? portrait : whiteSpr;
            sr.color  = portrait != null ? Color.white : color;
            sr.sortingOrder = 2;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.1f, 1.1f);

            var fp = new GameObject("FirePoint");
            fp.transform.SetParent(go.transform);
            fp.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);

            var tower = go.AddComponent<DragonTower>();
            var so    = new SerializedObject(tower);
            so.FindProperty("_projectilePrefab").objectReferenceValue = projPrefab;
            so.FindProperty("_firePoint").objectReferenceValue        = fp.transform;
            so.FindProperty("_projectileColor").colorValue            = color;
            so.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        static void CreateCardPrefab()
        {
            const string path = PrefDir+"/UI/PlacementCard.prefab";
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
            costRt.anchorMin = new Vector2(0,0.18f);
            costRt.anchorMax = new Vector2(1,0.32f);
            costRt.sizeDelta = Vector2.zero;
            var costT  = costGO.AddComponent<Text>();
            costT.font = GetFont(); costT.fontSize = 12; costT.color = new Color(0.4f,0.8f,1f);
            costT.alignment = TextAnchor.MiddleCenter; costT.text = "80 MP";

            var detailsGO = new GameObject("DetailsText");
            detailsGO.transform.SetParent(root.transform, false);
            var detailsRt = detailsGO.AddComponent<RectTransform>();
            detailsRt.anchorMin = new Vector2(0.03f,0.01f);
            detailsRt.anchorMax = new Vector2(0.97f,0.18f);
            detailsRt.sizeDelta = Vector2.zero;
            var detailsT = detailsGO.AddComponent<Text>();
            detailsT.font = GetFont(); detailsT.fontSize = 9; detailsT.color = new Color(0.84f,0.9f,1f,1f);
            detailsT.alignment = TextAnchor.MiddleCenter; detailsT.text = "Role R4\nSkill";

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
            so.FindProperty("_detailsText").objectReferenceValue   = detailsT;
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
            Root<PlacementManager>("PlacementManager");
            Root<TowerSelectionManager>("TowerSelectionManager");
            Root<BattleStatsTracker>("BattleStatsTracker");
            var piGO  = Root<PlayerInventory>("PlayerInventory");
            var dalGO = Root<DragonAssetLoader>("DragonAssetLoader");
            var dirGO = Root<GameDirector>("GameDirector");

            var rm = rmGO.GetComponent<ResourceManager>();
            var rmSO = new SerializedObject(rm);
            rmSO.FindProperty("_startingMana").intValue = PrototypeBalance.StartingMana;
            rmSO.FindProperty("_startingGold").intValue = PrototypeBalance.StartingGold;
            rmSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(rm);

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
            SetVector2IntArray(so.FindProperty("_highGroundTiles"), new[]{ new Vector2Int(4,4), new Vector2Int(6,5) });
            SetVector2IntArray(so.FindProperty("_manaCrystalTiles"), new[]{ new Vector2Int(1,2), new Vector2Int(10,4) });
            SetVector2IntArray(so.FindProperty("_scorchedTiles"), new[]{ new Vector2Int(5,1), new Vector2Int(9,5) });
            SetVector2IntArray(so.FindProperty("_frostTiles"), new[]{ new Vector2Int(3,2), new Vector2Int(7,3) });
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gm);
        }

        static void SetVector2IntArray(SerializedProperty prop, Vector2Int[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("x").intValue = values[i].x;
                elem.FindPropertyRelative("y").intValue = values[i].y;
            }
        }

        static void CreatePathDirectionMarkers(Sprite sprite)
        {
            var root = new GameObject("PathDirectionMarkers");
            for (int i = 1; i < PathTiles.Length - 1; i += 2)
            {
                Vector2Int current = PathTiles[i];
                Vector2Int next = PathTiles[Mathf.Min(i + 1, PathTiles.Length - 1)];
                Vector2 direction = new Vector2(next.x - current.x, next.y - current.y);
                if (direction.sqrMagnitude < 0.01f) continue;

                var marker = new GameObject($"PathMarker_{i:00}");
                marker.transform.SetParent(root.transform);
                marker.transform.position = new Vector3(-5.5f + current.x, -3.5f + current.y, -0.05f);
                marker.transform.localScale = new Vector3(0.42f, 0.12f, 1f);
                marker.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

                var sr = marker.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = new Color(1f, 0.95f, 0.55f, 0.45f);
                sr.sortingOrder = 1;
            }
        }

        static void CreateWaveManager(WaveData[] waves, GameObject elitePrefab)
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
            wavesProp.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
                wavesProp.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];

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
            canvasGO.transform.Find("BattleHUD")?.SetAsLastSibling();
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
            var selectedText = MakeLabel(hudGO, "SelectedTowerText", "No tower selected",
                new Vector2(0, 74), 18);
            var selectedRt = selectedText.GetComponent<RectTransform>();
            selectedRt.anchorMin = new Vector2(0.5f, 0f);
            selectedRt.anchorMax = new Vector2(0.5f, 0f);
            selectedRt.pivot = new Vector2(0.5f, 0.5f);
            selectedRt.anchoredPosition = new Vector2(0f, 274f);
            selectedRt.sizeDelta = new Vector2(360f, 28f);
            selectedText.alignment = TextAnchor.MiddleCenter;
            selectedText.color = new Color(1f, 0.94f, 0.72f, 1f);
            var skillCooldownText = MakeLabel(hudGO, "SkillCooldownText", "Skill: -",
                new Vector2(0, 40), 16);
            var skillRt = skillCooldownText.GetComponent<RectTransform>();
            skillRt.anchorMin = new Vector2(0.5f, 0f);
            skillRt.anchorMax = new Vector2(0.5f, 0f);
            skillRt.pivot = new Vector2(0.5f, 0.5f);
            skillRt.anchoredPosition = new Vector2(0f, 246f);
            skillRt.sizeDelta = new Vector2(380f, 24f);
            skillCooldownText.alignment = TextAnchor.MiddleCenter;
            skillCooldownText.color = new Color(0.76f, 0.9f, 1f, 1f);
            var statusText = MakeLabel(hudGO, "StatusText", "Place dragons, then start the wave",
                new Vector2(0,-58), 22);
            var statusRt = statusText.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.5f, 1f);
            statusRt.anchorMax = new Vector2(0.5f, 1f);
            statusRt.pivot = new Vector2(0.5f, 1f);
            statusRt.anchoredPosition = new Vector2(0f, -18f);
            statusRt.sizeDelta = new Vector2(520f, 34f);
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.color = new Color(1f, 0.92f, 0.62f, 1f);

            var previewPanel = MakePanel(hudGO, "WavePreviewPanel", sprite,
                new Color(0f,0f,0f,0.68f),
                new Vector2(1f,0.5f), new Vector2(1f,0.5f), new Vector2(1f,0.5f),
                new Vector2(-12f,38f), new Vector2(300f,270f));
            var previewText = MakeLabel(previewPanel, "WavePreviewText", "Wave Preview", new Vector2(12f,-12f), 15);
            var previewRt = previewText.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0f,1f);
            previewRt.anchorMax = new Vector2(1f,1f);
            previewRt.pivot = new Vector2(0f,1f);
            previewRt.anchoredPosition = new Vector2(12f,-12f);
            previewRt.sizeDelta = new Vector2(-24f,246f);
            previewText.alignment = TextAnchor.UpperLeft;
            previewText.color = new Color(0.92f,0.96f,1f,1f);

            var summaryPanel = MakePanel(hudGO, "WaveSummaryPanel", sprite,
                new Color(0f,0f,0f,0.66f),
                new Vector2(0f,0.5f), new Vector2(0f,0.5f), new Vector2(0f,0.5f),
                new Vector2(12f,-40f), new Vector2(300f,136f));
            var summaryText = MakeLabel(summaryPanel, "WaveSummaryText", "Wave summary appears here", new Vector2(12f,-10f), 15);
            var summaryRt = summaryText.GetComponent<RectTransform>();
            summaryRt.anchorMin = new Vector2(0f,1f);
            summaryRt.anchorMax = new Vector2(1f,1f);
            summaryRt.pivot = new Vector2(0f,1f);
            summaryRt.anchoredPosition = new Vector2(12f,-10f);
            summaryRt.sizeDelta = new Vector2(-24f,112f);
            summaryText.alignment = TextAnchor.UpperLeft;
            summaryText.color = new Color(0.9f,1f,0.86f,1f);
            summaryPanel.SetActive(false);

            var pauseBtn    = MakeButton(hudGO,"PauseButton","Pause",sprite,
                new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-10,-10),new Vector2(110,44));
            var nextWaveBtn = MakeButton(hudGO,"NextWaveButton","Next Wave",sprite,
                new Vector2(1,1),new Vector2(1,1),new Vector2(1,1),new Vector2(-130,-10),new Vector2(120,44));
            var skillBtn = MakeButton(hudGO,"SkillButton","Skill",sprite,
                new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0.5f,0.5f),
                new Vector2(-72, 208),new Vector2(124,38));
            var upgradeBtn = MakeButton(hudGO,"UpgradeButton","Upgrade",sprite,
                new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0.5f,0.5f),
                new Vector2(72, 208),new Vector2(138,38));
            var mergeBtn = MakeButton(hudGO,"MergeButton","Fuse",sprite,
                new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0.5f,0.5f),
                new Vector2(-72, 168),new Vector2(124,34));
            var sellBtn = MakeButton(hudGO,"SellButton","Sell",sprite,
                new Vector2(0.5f,0),new Vector2(0.5f,0),new Vector2(0.5f,0.5f),
                new Vector2(72, 168),new Vector2(112,34));

            var hud = hudGO.AddComponent<BattleHUD>();
            var so  = new SerializedObject(hud);
            so.FindProperty("_livesText").objectReferenceValue      = livesText;
            so.FindProperty("_waveText").objectReferenceValue       = waveText;
            so.FindProperty("_manaText").objectReferenceValue       = manaText;
            so.FindProperty("_goldText").objectReferenceValue       = goldText;
            so.FindProperty("_statusText").objectReferenceValue     = statusText;
            so.FindProperty("_wavePreviewText").objectReferenceValue = previewText;
            so.FindProperty("_waveSummaryText").objectReferenceValue = summaryText;
            so.FindProperty("_selectedTowerText").objectReferenceValue = selectedText;
            so.FindProperty("_skillCooldownText").objectReferenceValue = skillCooldownText;
            so.FindProperty("_pauseButton").objectReferenceValue    = pauseBtn;
            so.FindProperty("_nextWaveButton").objectReferenceValue = nextWaveBtn;
            so.FindProperty("_skillButton").objectReferenceValue = skillBtn;
            so.FindProperty("_upgradeButton").objectReferenceValue = upgradeBtn;
            so.FindProperty("_mergeButton").objectReferenceValue = mergeBtn;
            so.FindProperty("_sellButton").objectReferenceValue = sellBtn;
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
            rt.anchorMin = new Vector2(0.2f,0.18f);
            rt.anchorMax = new Vector2(0.8f,0.82f);
            rt.sizeDelta = Vector2.zero;
            var bg = go.AddComponent<Image>();
            bg.sprite = sprite;
            bg.color  = new Color(0f,0f,0f,0.88f);

            var resultText = MakeCenteredLabel(go,"ResultText","VICTORY!",0.5f,0.84f,36);
            var statsText  = MakeCenteredLabel(go,"StatsText", "",        0.5f,0.54f,17);
            var statsRt = statsText.GetComponent<RectTransform>();
            statsRt.sizeDelta = new Vector2(520f, 190f);
            statsText.alignment = TextAnchor.MiddleCenter;
            var retryBtn   = MakeCenteredButton(go,"RetryButton","Replay",sprite,0.35f,0.18f);
            var quitBtn    = MakeCenteredButton(go,"QuitButton", "Quit", sprite,0.65f,0.18f);

            var panel = go.AddComponent<VictoryDefeatPanel>();
            go.AddComponent<CanvasGroup>();
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
