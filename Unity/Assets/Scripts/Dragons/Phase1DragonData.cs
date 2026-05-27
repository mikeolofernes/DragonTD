// Static reference data for Phase 1 dragons.
// Artwork mapping:
//   voltaris_001     → portrait: Art/Dragons/voltaris_001/portrait.png       (dark purple/gold lightning dragon)
//   frostfang_002    → portrait: Art/Dragons/frostfang_002/portrait.png       (blue ice crystal dragon)
//                      idle video: Art/Dragons/frostfang_002/idle_anim.mp4
//   magmaclaw_003    → portrait: Art/Dragons/magmaclaw_003/portrait.png       (red fire-breathing dragon)
//                      idle video: Art/Dragons/magmaclaw_003/idle_anim.mp4
//   tempest_glacion_004 → portrait: Art/Dragons/tempest_glacion_004/portrait.png (blue/gold ice+lightning fusion)
//   stonehide_005    → idle video: Art/Dragons/stonehide_005/idle_anim.mp4
//   celestara_006    → portrait: Art/Dragons/celestara_006/portrait.png       (white/gold celestial dragon)
//                      idle video: Art/Dragons/celestara_006/idle_anim.mp4
//   shadowfang_007   → idle video: Art/Dragons/shadowfang_007/idle_anim.mp4
//
// To bind assets: open each DragonDefinition .asset in the Inspector, drag the
// corresponding portrait Sprite and VideoClip into visualData fields.

namespace DragonTD.Dragons
{
    // Stat reference used by DragonAssetFactory — no runtime use.
    public static class Phase1DragonData
    {
        public struct Def
        {
            public string Id;
            public string Name;
            public DragonRarity Rarity;
            public DragonElement Element;
            public DragonClass Class;
            public float Hp, Atk, Armor, Range, AttackSpeed, Mana;
            public int ManaCost;
            // Normal attack
            public string NormalSkillId, NormalAttackName;
            public float AtkCd, AtkMult;
            // Active skill
            public string SkillName;
            public float SkillCd, SkillMult, SkillRadius;
            public bool SkillAoe;
            // Fusion
            public string FusionPartnerId, FusionResultId;
        }

        public static readonly Def[] All = new Def[]
        {
            new Def
            {
                Id = "voltaris_001", Name = "Voltaris",
                Rarity = DragonRarity.Epic, Element = DragonElement.Lightning, Class = DragonClass.Storm,
                Hp = 900f, Atk = 220f, Armor = 60f, Range = 5f, AttackSpeed = 1.67f, Mana = 200f,
                ManaCost = 90,
                NormalSkillId = "voltaris_strike_001", NormalAttackName = "Volt Strike",
                AtkCd = 0.6f, AtkMult = 0.8f,
                SkillName = "Chain Lightning", SkillCd = 10f, SkillMult = 3.0f, SkillAoe = false, SkillRadius = 0f,
                FusionPartnerId = "frostfang_002", FusionResultId = "tempest_glacion_004"
            },
            new Def
            {
                Id = "frostfang_002", Name = "Frostfang",
                Rarity = DragonRarity.Epic, Element = DragonElement.Ice, Class = DragonClass.Frost,
                Hp = 1100f, Atk = 195f, Armor = 90f, Range = 4.5f, AttackSpeed = 0.83f, Mana = 180f,
                ManaCost = 85,
                NormalSkillId = "frostfang_bite_001", NormalAttackName = "Frost Bite",
                AtkCd = 1.2f, AtkMult = 1.0f,
                SkillName = "Blizzard", SkillCd = 12f, SkillMult = 2.0f, SkillAoe = true, SkillRadius = 3f,
                FusionPartnerId = "voltaris_001", FusionResultId = "tempest_glacion_004"
            },
            new Def
            {
                Id = "magmaclaw_003", Name = "Magmaclaw",
                Rarity = DragonRarity.Epic, Element = DragonElement.Fire, Class = DragonClass.Flame,
                Hp = 1100f, Atk = 260f, Armor = 70f, Range = 3.5f, AttackSpeed = 1.0f, Mana = 160f,
                ManaCost = 80,
                NormalSkillId = "magmaclaw_slash_001", NormalAttackName = "Magma Slash",
                AtkCd = 1.0f, AtkMult = 1.2f,
                SkillName = "Magma Burst", SkillCd = 8f, SkillMult = 2.8f, SkillAoe = true, SkillRadius = 2.5f,
            },
            new Def
            {
                Id = "tempest_glacion_004", Name = "Tempest Glacion",
                Rarity = DragonRarity.Legendary, Element = DragonElement.Lightning, Class = DragonClass.Storm,
                Hp = 1500f, Atk = 380f, Armor = 120f, Range = 5.5f, AttackSpeed = 1.5f, Mana = 300f,
                ManaCost = 150,
                NormalSkillId = "tempest_glacion_arc_001", NormalAttackName = "Tempest Arc",
                AtkCd = 0.67f, AtkMult = 1.0f,
                SkillName = "Glacial Storm", SkillCd = 15f, SkillMult = 4.0f, SkillAoe = true, SkillRadius = 4f,
            },
            new Def
            {
                Id = "stonehide_005", Name = "Stonehide",
                Rarity = DragonRarity.Rare, Element = DragonElement.Earth, Class = DragonClass.Earth,
                Hp = 2200f, Atk = 130f, Armor = 200f, Range = 2.5f, AttackSpeed = 0.5f, Mana = 140f,
                ManaCost = 75,
                NormalSkillId = "stonehide_boulder_001", NormalAttackName = "Boulder Crush",
                AtkCd = 2.0f, AtkMult = 1.5f,
                SkillName = "Fortify", SkillCd = 20f, SkillMult = 1.0f, SkillAoe = false, SkillRadius = 0f,
            },
            new Def
            {
                Id = "celestara_006", Name = "Celestara",
                Rarity = DragonRarity.Legendary, Element = DragonElement.Light, Class = DragonClass.Celestial,
                Hp = 1600f, Atk = 160f, Armor = 100f, Range = 6f, AttackSpeed = 0.77f, Mana = 320f,
                ManaCost = 120,
                NormalSkillId = "celestara_ray_001", NormalAttackName = "Celestial Ray",
                AtkCd = 1.3f, AtkMult = 0.8f,
                SkillName = "Divine Aura", SkillCd = 18f, SkillMult = 1.5f, SkillAoe = true, SkillRadius = 5f,
            },
            new Def
            {
                Id = "shadowfang_007", Name = "Shadowfang",
                Rarity = DragonRarity.Epic, Element = DragonElement.Shadow, Class = DragonClass.Abyssal,
                Hp = 1050f, Atk = 240f, Armor = 80f, Range = 4f, AttackSpeed = 1.25f, Mana = 200f,
                ManaCost = 95,
                NormalSkillId = "shadowfang_void_001", NormalAttackName = "Void Fang",
                AtkCd = 0.8f, AtkMult = 1.1f,
                SkillName = "Void Strike", SkillCd = 11f, SkillMult = 3.2f, SkillAoe = false, SkillRadius = 0f,
            },
        };
    }
}
