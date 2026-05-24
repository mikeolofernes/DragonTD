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

            // Ignarion — Fire Striker
            CreateDragon("Ignarion",
                "A young dragon born from volcanic eruptions. Its fire burns with unrelenting fury.",
                DragonRarity.S, DragonElement.Fire, DragonRole.Striker,
                hp: 1200f, atk: 180f, def: 80f, spd: 3.5f, range: 4f, mana: 80,
                atkCd: 1.0f, atkMult: 1.0f, atkRange: 4f,
                skillCd: 8f, skillMult: 2.5f, skillAoe: true, skillRadius: 2f);

            // Aquariel — Water Guardian
            CreateDragon("Aquariel",
                "Guardian of the ocean depths. Slows enemies and sustains allied dragons.",
                DragonRarity.A, DragonElement.Water, DragonRole.Guardian,
                hp: 1600f, atk: 100f, def: 120f, spd: 2.5f, range: 3f, mana: 70,
                atkCd: 1.5f, atkMult: 1.0f, atkRange: 3f,
                skillCd: 12f, skillMult: 1.5f, skillAoe: true, skillRadius: 3f);

            // Voltaris — Lightning Tempest
            CreateDragon("Voltaris",
                "Lightning given form. Strikes before the thunder reaches your ears.",
                DragonRarity.S, DragonElement.Lightning, DragonRole.Tempest,
                hp: 900f, atk: 220f, def: 60f, spd: 5f, range: 5f, mana: 90,
                atkCd: 0.6f, atkMult: 0.8f, atkRange: 5f,
                skillCd: 10f, skillMult: 3.0f, skillAoe: false, skillRadius: 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DragonTD] Phase 1 dragons created at {BasePath}. Assign Prefab references in the Inspector.");
        }

        private static void CreateDragon(
            string dragonName, string lore,
            DragonRarity rarity, DragonElement element, DragonRole role,
            float hp, float atk, float def, float spd, float range, int mana,
            float atkCd, float atkMult, float atkRange,
            float skillCd, float skillMult, bool skillAoe, float skillRadius)
        {
            var normalAttack = MakeAbility($"{dragonName}_NormalAttack",
                $"{dragonName} Strike", AbilityType.NormalAttack, atkCd, atkMult, atkRange, false, 0f);

            var activeSkill = MakeAbility($"{dragonName}_ActiveSkill",
                $"{dragonName} Skill", AbilityType.ActiveSkill, skillCd, skillMult, range, skillAoe, skillRadius);

            var dragon = ScriptableObject.CreateInstance<DragonData>();
            dragon.DragonName = dragonName;
            dragon.Lore       = lore;
            dragon.Rarity     = rarity;
            dragon.Element    = element;
            dragon.Role       = role;
            dragon.BaseHp     = hp;
            dragon.BaseAttack = atk;
            dragon.BaseDefense = def;
            dragon.BaseSpeed  = spd;
            dragon.BaseRange  = range;
            dragon.ManaCost   = mana;
            dragon.NormalAttack = normalAttack;
            dragon.ActiveSkill  = activeSkill;
            AssetDatabase.CreateAsset(dragon, $"{BasePath}/{dragonName}.asset");
        }

        private static AbilityData MakeAbility(
            string assetName, string displayName,
            AbilityType type, float cooldown, float dmgMult, float abilityRange,
            bool isAoe, float aoeRadius)
        {
            var ability = ScriptableObject.CreateInstance<AbilityData>();
            ability.AbilityName      = displayName;
            ability.Type             = type;
            ability.Cooldown         = cooldown;
            ability.DamageMultiplier = dmgMult;
            ability.Range            = abilityRange;
            ability.IsAoe            = isAoe;
            ability.AoeRadius        = aoeRadius;
            AssetDatabase.CreateAsset(ability, $"{BasePath}/{assetName}.asset");
            return ability;
        }
    }
}
#endif
