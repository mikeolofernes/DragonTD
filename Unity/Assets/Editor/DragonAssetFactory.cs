#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using DragonTD.Dragons;

namespace DragonTD.Editor
{
    public static class DragonAssetFactory
    {
        private const string BasePath    = "Assets/ScriptableObjects/Dragons";
        private const string ArtBasePath = "Assets/Art/Dragons";

        [MenuItem("DragonTD/Create Phase 1 Dragons")]
        public static void CreatePhase1Dragons()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Dragons"));
            AssetDatabase.Refresh();

            foreach (var d in Phase1DragonData.All)
                CreateDragon(d);

            // Wire fusion entries after all dragons exist
            WireFusion("voltaris_001",  "frostfang_002",    "tempest_glacion_004");
            WireFusion("frostfang_002", "voltaris_001",     "tempest_glacion_004");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DragonTD] Phase 1 dragons created. Open each .asset to assign portrait sprites and idle video clips.");
        }

        private static void CreateDragon(Phase1DragonData.Def d)
        {
            string assetPath = $"{BasePath}/{d.Name}.asset";

            // Don't overwrite existing assets
            if (AssetDatabase.LoadAssetAtPath<DragonDefinition>(assetPath) != null)
            {
                Debug.Log($"[DragonTD] {d.Name} already exists — skipping.");
                return;
            }

            var normalAttack = MakeSkill($"{d.Id}_normal", $"{d.Name} Strike",
                d.AtkCd, d.AtkMult, d.Range, false, 0f);

            var activeSkill = MakeSkill($"{d.Id}_active", d.SkillName,
                d.SkillCd, d.SkillMult, d.Range, d.SkillAoe, d.SkillRadius);

            var def = ScriptableObject.CreateInstance<DragonDefinition>();
            def.dragonId    = d.Id;
            def.displayName = d.Name;
            def.rarity      = d.Rarity;
            def.element     = d.Element;
            def.dragonClass = d.Class;
            def.manaCost    = d.ManaCost;

            def.baseStats = new DragonBaseStats
            {
                hp          = d.Hp,
                attack      = d.Atk,
                armor       = d.Armor,
                range       = d.Range,
                attackSpeed = d.AttackSpeed,
                mana        = d.Mana,
            };

            def.skillSet = new DragonSkillSet
            {
                normalAttack = normalAttack,
                activeSkill  = activeSkill
            };

            def.bondData = MakeDefaultBondData();
            def.visualData = TryBindVisualData(d.Id);

            AssetDatabase.CreateAsset(def, assetPath);
        }

        private static void WireFusion(string initiatorId, string partnerId, string resultId)
        {
            string path = $"{BasePath}/{GetNameById(initiatorId)}.asset";
            var def = AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
            if (def == null) return;

            def.fusionTable = new DragonFusionTable
            {
                fusionEntries = new FusionEntry[]
                {
                    new FusionEntry
                    {
                        partnerDragonId       = partnerId,
                        resultDragonId        = resultId,
                        successChance         = 0.7f,
                        fusionType            = FusionType.Hybrid,
                        midBattleFusionAllowed = true,
                        bondBonusPerLevel      = 0.05f
                    }
                }
            };
            EditorUtility.SetDirty(def);
        }

        private static SkillDefinition MakeSkill(
            string assetName, string displayName,
            float cooldown, float baseMult, float skillRange,
            bool isAoe, float aoeRadius)
        {
            string path = $"{BasePath}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
            if (existing != null) return existing;

            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.skillId     = assetName;
            skill.displayName = displayName;
            skill.cooldown    = cooldown;
            skill.range       = skillRange;
            skill.isAoe       = isAoe;
            skill.aoeRadius   = aoeRadius;

            // Scale multiplier 10% per skill level
            skill.levelMultipliers = new float[10];
            for (int i = 0; i < 10; i++)
                skill.levelMultipliers[i] = baseMult * (1f + i * 0.1f);

            AssetDatabase.CreateAsset(skill, path);
            return skill;
        }

        private static DragonBondData MakeDefaultBondData()
        {
            return new DragonBondData
            {
                battleBondXP  = 10f,
                feedBondXP    = 5f,
                trainBondXP   = 8f,
                exploreBondXP = 3f,
                bondLevels = new BondLevel[]
                {
                    new BondLevel { level = 1, xpRequired = 0,    statBoostPercent = 0.00f },
                    new BondLevel { level = 2, xpRequired = 100f,  statBoostPercent = 0.05f },
                    new BondLevel { level = 3, xpRequired = 300f,  statBoostPercent = 0.08f, skillUnlockId = null },
                    new BondLevel { level = 4, xpRequired = 700f,  statBoostPercent = 0.12f },
                    new BondLevel { level = 5, xpRequired = 1500f, statBoostPercent = 0.18f, triggersEvolutionAccess = true },
                    new BondLevel { level = 6, xpRequired = 3000f, statBoostPercent = 0.25f },
                    new BondLevel { level = 7, xpRequired = 6000f, statBoostPercent = 0.35f },
                }
            };
        }

        private static DragonVisualData TryBindVisualData(string dragonId)
        {
            var vd = new DragonVisualData
            {
                portraitKey = $"dragons/{dragonId}/portrait",
                iconKey     = $"dragons/{dragonId}/icon",
                idleVfxKey  = $"dragons/{dragonId}/idle_vfx",
            };

            // Try to auto-bind portrait sprite if it already exists in project
            string spritePath = $"{ArtBasePath}/{dragonId}/portrait.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null) vd.portrait = sprite;

            // Try to auto-bind dragon video
            string videoPath = $"{ArtBasePath}/{dragonId}/idle_anim.mp4";
            var video = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(videoPath);
            if (video != null) vd.dragonVideo = video;

            return vd;
        }

        private static string GetNameById(string id) => id switch
        {
            "voltaris_001"        => "Voltaris",
            "frostfang_002"       => "Frostfang",
            "magmaclaw_003"       => "Magmaclaw",
            "tempest_glacion_004" => "Tempest Glacion",
            "stonehide_005"       => "Stonehide",
            "celestara_006"       => "Celestara",
            "shadowfang_007"      => "Shadowfang",
            _                     => id
        };
    }
}
#endif
