using UnityEngine;
using DragonTD.TowerDefense;
using DragonTD.Core;

namespace DragonTD.Dragons
{
    public static class AbilityExecutor
    {
        public static void ExecuteActiveSkill(
            SkillDefinition skill,
            DragonInstance caster,
            EnemyBase primaryTarget,
            Vector3 originPosition,
            float damageMultiplier = 1f,
            float statusMagnitudeMultiplier = 1f)
        {
            if (skill == null) return;

            float baseDamage = caster.Attack * skill.GetDamageMultiplier(caster.SkillLevel) * damageMultiplier;
            Color color = caster.Definition.visualData.primaryColor;

            GameManager.Instance?.ShowBattleMessage($"{caster.Definition.displayName}: {skill.displayName}");

            if ((skill.skillId ?? string.Empty).ToLowerInvariant().Contains("chain"))
            {
                ExecuteChainSkill(skill, caster, primaryTarget, originPosition, baseDamage, color, statusMagnitudeMultiplier);
                return;
            }

            if ((skill.skillId ?? string.Empty).ToLowerInvariant().Contains("fortify"))
            {
                ResourceManager.Instance?.AddMana(12);
                SkillCastEffect.SpawnPulse(originPosition, 1.8f, color);
                GameManager.Instance?.ShowBattleMessage("Stonehide fortifies the defense: +12 MP");
                return;
            }

            if (primaryTarget == null || primaryTarget.IsDead) return;

            if (skill.isAoe)
            {
                SkillCastEffect.SpawnBeam(originPosition, primaryTarget.transform.position, color);
                SkillCastEffect.SpawnPulse(primaryTarget.transform.position, skill.aoeRadius, color);
                Collider2D[] hits = Physics2D.OverlapCircleAll(primaryTarget.transform.position, skill.aoeRadius);
                foreach (Collider2D hit in hits)
                {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy == null || enemy.IsDead) continue;
                    float mult = enemy.HasElement
                        ? ElementInteraction.GetMultiplier(caster.Definition.element, enemy.EnemyElement)
                        : 1f;
                    float damage = baseDamage * mult;
                    BattleStatsTracker.Instance?.RecordDamage(caster.Definition.displayName, damage);
                    enemy.TakeDamage(damage, color, DamageSource.Skill);
                    ShowElementFeedback(enemy, mult, color);
                    enemy.ApplyStatusEffects(skill.statusEffects, color, statusMagnitudeMultiplier);
                }
            }
            else
            {
                SkillCastEffect.SpawnBeam(originPosition, primaryTarget.transform.position, color);
                float mult = primaryTarget.HasElement
                    ? ElementInteraction.GetMultiplier(caster.Definition.element, primaryTarget.EnemyElement)
                    : 1f;
                float damage = baseDamage * mult;
                BattleStatsTracker.Instance?.RecordDamage(caster.Definition.displayName, damage);
                primaryTarget.TakeDamage(damage, color, DamageSource.Skill);
                ShowElementFeedback(primaryTarget, mult, color);
                primaryTarget.ApplyStatusEffects(skill.statusEffects, color, statusMagnitudeMultiplier);
            }
        }

        private static void ExecuteChainSkill(
            SkillDefinition skill,
            DragonInstance caster,
            EnemyBase primaryTarget,
            Vector3 originPosition,
            float baseDamage,
            Color color,
            float statusMagnitudeMultiplier)
        {
            if (primaryTarget == null || primaryTarget.IsDead) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(primaryTarget.transform.position, Mathf.Max(3.5f, skill.aoeRadius));
            int hitCount = 0;
            float falloff = 1f;
            Vector3 previousPosition = originPosition;

            foreach (Collider2D hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                float mult = enemy.HasElement
                    ? ElementInteraction.GetMultiplier(caster.Definition.element, enemy.EnemyElement)
                    : 1f;
                float damage = baseDamage * falloff * mult;
                BattleStatsTracker.Instance?.RecordDamage(caster.Definition.displayName, damage);
                enemy.TakeDamage(damage, color, DamageSource.Skill);
                ShowElementFeedback(enemy, mult, color);
                enemy.ApplyStatusEffects(skill.statusEffects, color, statusMagnitudeMultiplier);
                SkillCastEffect.SpawnBeam(previousPosition, enemy.transform.position, color);
                previousPosition = enemy.transform.position;

                hitCount++;
                falloff *= 0.75f;
                if (hitCount >= 4) break;
            }
        }

        private static void ShowElementFeedback(EnemyBase enemy, float multiplier, Color color)
        {
            if (enemy == null) return;
            if (multiplier >= 1.25f)
                DamageIndicator.SpawnText(enemy.transform.position + Vector3.up * 1.25f, "WEAK", PrototypeBalance.WeakFeedbackColor);
            else if (multiplier <= 0.8f)
                DamageIndicator.SpawnText(enemy.transform.position + Vector3.up * 1.25f, "RESIST", PrototypeBalance.ResistFeedbackColor);
        }
    }
}
