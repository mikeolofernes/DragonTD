using System.Collections.Generic;
using System.Text;

namespace DragonTD.Dragons
{
    public static class DragonRoleUtility
    {
        public static bool HasRole(DragonInstance dragon, DragonRoleTag role)
        {
            return HasRole(dragon?.Definition, role);
        }

        public static bool HasRole(DragonDefinition definition, DragonRoleTag role)
        {
            if (definition == null)
                return false;

            return role switch
            {
                DragonRoleTag.Damage => IsDamage(definition),
                DragonRoleTag.Slow => IsSlow(definition),
                DragonRoleTag.Aoe => IsAoe(definition),
                DragonRoleTag.Support => IsSupport(definition),
                DragonRoleTag.AntiShield => IsAntiShield(definition),
                DragonRoleTag.AntiFlying => IsAntiFlying(definition),
                _ => false
            };
        }

        public static string BuildRoleText(DragonInstance dragon)
        {
            if (dragon?.Definition == null)
                return "None";

            var roles = new List<string>();
            foreach (DragonRoleTag role in System.Enum.GetValues(typeof(DragonRoleTag)))
            {
                if (HasRole(dragon, role))
                    roles.Add(Label(role));
            }

            return roles.Count == 0 ? "Generalist" : string.Join(", ", roles);
        }

        public static string Label(DragonRoleTag role) => role switch
        {
            DragonRoleTag.Damage => "Damage",
            DragonRoleTag.Slow => "Slow",
            DragonRoleTag.Aoe => "AoE",
            DragonRoleTag.Support => "Support",
            DragonRoleTag.AntiShield => "Anti-Shield",
            DragonRoleTag.AntiFlying => "Anti-Flying",
            _ => "Role"
        };

        public static string BuildRoleList(IEnumerable<DragonRoleTag> roles)
        {
            var labels = new List<string>();
            if (roles != null)
            {
                foreach (DragonRoleTag role in roles)
                    labels.Add(Label(role));
            }
            return labels.Count == 0 ? "None" : string.Join(", ", labels);
        }

        public static bool LoadoutHasRole(IEnumerable<DragonInstance> dragons, DragonRoleTag role)
        {
            if (dragons == null)
                return false;

            foreach (DragonInstance dragon in dragons)
            {
                if (HasRole(dragon, role))
                    return true;
            }

            return false;
        }

        public static string BuildCoverageReport(IEnumerable<DragonInstance> dragons)
        {
            var sb = new StringBuilder();
            foreach (DragonRoleTag role in System.Enum.GetValues(typeof(DragonRoleTag)))
                sb.Append($"{Label(role)}:{(LoadoutHasRole(dragons, role) ? "Y" : "N")} ");
            return sb.ToString().TrimEnd();
        }

        private static bool IsDamage(DragonDefinition definition)
        {
            return definition.baseStats.attack >= 220f ||
                   definition.rarity >= DragonRarity.Epic ||
                   definition.dragonClass == DragonClass.Flame ||
                   definition.dragonClass == DragonClass.Storm ||
                   definition.dragonClass == DragonClass.Abyssal ||
                   definition.element == DragonElement.Fire ||
                   definition.element == DragonElement.Lightning ||
                   HasDamageSkill(definition.ActiveSkill);
        }

        private static bool IsSlow(DragonDefinition definition)
        {
            return definition.dragonClass == DragonClass.Frost ||
                   definition.element == DragonElement.Ice ||
                   definition.element == DragonElement.Water ||
                   HasKeyword(definition.ActiveSkill, "slow", "frost", "freeze", "blizzard", "drench");
        }

        private static bool IsAoe(DragonDefinition definition)
        {
            return definition.ActiveSkill != null && (definition.ActiveSkill.isAoe || definition.ActiveSkill.aoeRadius > 0f) ||
                   definition.dragonClass == DragonClass.Venom ||
                   HasKeyword(definition.ActiveSkill, "area", "aoe", "cloud", "field", "storm");
        }

        private static bool IsSupport(DragonDefinition definition)
        {
            return definition.dragonClass == DragonClass.Earth ||
                   definition.dragonClass == DragonClass.Celestial ||
                   definition.element == DragonElement.Light ||
                   HasKeyword(definition.ActiveSkill, "heal", "fortify", "buff", "aura", "shield");
        }

        private static bool IsAntiShield(DragonDefinition definition)
        {
            return definition.dragonClass == DragonClass.Storm ||
                   definition.dragonClass == DragonClass.Abyssal ||
                   definition.element == DragonElement.Lightning ||
                   definition.element == DragonElement.Shadow ||
                   HasKeyword(definition.ActiveSkill, "lightning", "shock", "void", "break");
        }

        private static bool IsAntiFlying(DragonDefinition definition)
        {
            return definition.baseStats.range >= 4.5f ||
                   definition.dragonClass == DragonClass.Storm ||
                   definition.dragonClass == DragonClass.Celestial ||
                   definition.element == DragonElement.Wind ||
                   definition.element == DragonElement.Lightning;
        }

        private static bool HasDamageSkill(SkillDefinition skill)
        {
            return skill != null && skill.baseDamage >= 120f;
        }

        private static bool HasKeyword(SkillDefinition skill, params string[] keywords)
        {
            if (skill == null || keywords == null)
                return false;

            string text = $"{skill.skillId} {skill.displayName} {skill.description}".ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword) && text.Contains(keyword.ToLowerInvariant()))
                    return true;
            }

            if (skill.statusEffects != null)
            {
                foreach (StatusEffect effect in skill.statusEffects)
                {
                    string effectText = $"{effect?.effectId} {effect?.displayName}".ToLowerInvariant();
                    foreach (string keyword in keywords)
                    {
                        if (!string.IsNullOrWhiteSpace(keyword) && effectText.Contains(keyword.ToLowerInvariant()))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
