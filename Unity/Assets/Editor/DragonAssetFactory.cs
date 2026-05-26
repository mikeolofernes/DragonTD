#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;
using DragonTD.Dragons;

namespace DragonTD.Editor
{
    public static class DragonAssetFactory
    {
        private const string BasePath = "Assets/ScriptableObjects/Dragons";

        [MenuItem("DragonTD/Create Phase 1 Dragons")]
        public static void CreatePhase1Dragons()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "ScriptableObjects/Dragons"));

            // Ignarion — Flame Striker
            CreateDragon(
                id: "ignarion_001", name: "Ignarion",
                rarity: DragonRarity.Epic, element: DragonElement.Fire, dragonClass: DragonClass.Flame,
                hp: 1200f, atk: 180f, armor: 80f, speed: 3.5f, range: 4f, mana: 80,
                atkCd: 1.0f, atkMult: 1.0f, atkRange: 4f,
                skillCd: 8f, skillMult: 2.5f, skillAoe: true, skillRadius: 2f);

            // Aquariel — Frost Guardian
            CreateDragon(
                id: "aquariel_002", name: "Aquariel",
                rarity: DragonRarity.Rare, element: DragonElement.Water, dragonClass: DragonClass.Frost,
                hp: 1600f, atk: 100f, armor: 120f, speed: 2.5f, range: 3f, mana: 70,
                atkCd: 1.5f, atkMult: 1.0f, atkRange: 3f,
                skillCd: 12f, skillMult: 1.5f, skillAoe: true, skillRadius: 3f);

            // Voltaris — Storm Tempest
            CreateDragon(
                id: "voltaris_003", name: "Voltaris",
                rarity: DragonRarity.Epic, element: DragonElement.Lightning, dragonClass: DragonClass.Storm,
                hp: 900f, atk: 220f, armor: 60f, speed: 5f, range: 5f, mana: 90,
                atkCd: 0.6f, atkMult: 0.8f, atkRange: 5f,
                skillCd: 10f, skillMult: 3.0f, skillAoe: false, skillRadius: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DragonTD] Phase 1 dragons created at {BasePath}. Assign Prefab references in the Inspector.");
        }

        private static void CreateDragon(
            string id, string name,
            DragonRarity rarity, DragonElement element, DragonClass dragonClass,
            float hp, float atk, float armor, float speed, float range, int mana,
            float atkCd, float atkMult, float atkRange,
            float skillCd, float skillMult, bool skillAoe, float skillRadius)
        {
            var normalAttack = MakeSkill($"{id}_normal",
                $"{name} Strike", atkCd, atkMult, atkRange, false, 0f);

            var activeSkill = MakeSkill($"{id}_active",
                $"{name} Skill", skillCd, skillMult, range, skillAoe, skillRadius);

            var dragon = ScriptableObject.CreateInstance<DragonDefinition>();
            dragon.dragonId    = id;
            dragon.displayName = name;
            dragon.rarity      = rarity;
            dragon.element     = element;
            dragon.dragonClass = dragonClass;
            dragon.manaCost    = mana;

            dragon.baseStats = new DragonBaseStats
            {
                hp          = hp,
                attack      = atk,
                armor       = armor,
                flightSpeed = speed,
                range       = range,
                attackSpeed = 1f / atkCd
            };

            dragon.skillSet = new DragonSkillSet
            {
                normalAttack = normalAttack,
                activeSkill  = activeSkill
            };

            AssetDatabase.CreateAsset(dragon, $"{BasePath}/{name}.asset");
        }

        private static SkillDefinition MakeSkill(
            string assetName, string displayName,
            float cooldown, float dmgMult, float skillRange,
            bool isAoe, float aoeRadius)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            skill.skillId      = assetName;
            skill.displayName  = displayName;
            skill.cooldown     = cooldown;
            skill.range        = skillRange;
            skill.isAoe        = isAoe;
            skill.aoeRadius    = aoeRadius;

            // Encode dmgMult as uniform across all 10 levels
            skill.levelMultipliers = new float[10];
            for (int i = 0; i < 10; i++)
                skill.levelMultipliers[i] = dmgMult * (1f + i * 0.1f);

            AssetDatabase.CreateAsset(skill, $"{BasePath}/{assetName}.asset");
            return skill;
        }
    }
}
#endif
