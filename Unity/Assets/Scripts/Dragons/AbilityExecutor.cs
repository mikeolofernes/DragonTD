using UnityEngine;
using DragonTD.TowerDefense;

namespace DragonTD.Dragons
{
    public static class AbilityExecutor
    {
        public static void ExecuteActiveSkill(
            AbilityData ability,
            DragonInstance caster,
            EnemyBase primaryTarget,
            Vector3 originPosition)
        {
            if (ability == null || primaryTarget == null || primaryTarget.IsDead) return;

            float baseDamage = caster.Attack * ability.DamageMultiplier;

            if (ability.IsAoe)
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(primaryTarget.transform.position, ability.AoeRadius);
                foreach (Collider2D hit in hits)
                {
                    EnemyBase enemy = hit.GetComponent<EnemyBase>();
                    if (enemy == null || enemy.IsDead) continue;
                    float mult = enemy.HasElement
                        ? ElementInteraction.GetMultiplier(caster.Data.Element, enemy.EnemyElement)
                        : 1f;
                    enemy.TakeDamage(baseDamage * mult);
                }
            }
            else
            {
                float mult = primaryTarget.HasElement
                    ? ElementInteraction.GetMultiplier(caster.Data.Element, primaryTarget.EnemyElement)
                    : 1f;
                primaryTarget.TakeDamage(baseDamage * mult);
            }
        }
    }
}
