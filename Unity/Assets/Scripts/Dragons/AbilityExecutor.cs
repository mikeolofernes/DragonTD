using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Dragons
{
    public static class AbilityExecutor
    {
        public static void ExecuteActiveSkill(
            SkillDefinition skill,
            DragonInstance caster,
            EnemyBase primaryTarget,
            Vector3 originPosition)
        {
            if (skill == null || primaryTarget == null || primaryTarget.IsDead) return;

            float baseDamage = caster.Attack * skill.GetDamageMultiplier(caster.SkillLevel);

            if (skill.isAoe)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(primaryTarget.transform.position, skill.aoeRadius);
                foreach (Collider2D hit in hits)
                {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy == null || enemy.IsDead) continue;
                    float mult = enemy.HasElement
                        ? ElementInteraction.GetMultiplier(caster.Definition.element, enemy.EnemyElement)
                        : 1f;
                    enemy.TakeDamage(baseDamage * mult);
                }
            }
            else
            {
                float mult = primaryTarget.HasElement
                    ? ElementInteraction.GetMultiplier(caster.Definition.element, primaryTarget.EnemyElement)
                    : 1f;
                primaryTarget.TakeDamage(baseDamage * mult);
            }
        }
    }
}
