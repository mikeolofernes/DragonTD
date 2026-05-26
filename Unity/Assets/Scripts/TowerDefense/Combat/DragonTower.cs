using UnityEngine;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class DragonTower : MonoBehaviour
    {
        private DragonInstance _dragonInstance;
        private float _attackCooldown;
        private float _lastAttackTime;
        private float _activeSkillCooldown;
        private float _lastActiveSkillTime = float.NegativeInfinity;

        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;

        public void Setup(DragonInstance instance)
        {
            _dragonInstance = instance;
            _attackCooldown = instance.Definition.NormalAttack != null
                ? instance.Definition.NormalAttack.cooldown : 1f;
            _activeSkillCooldown = instance.Definition.ActiveSkill != null
                ? instance.Definition.ActiveSkill.cooldown : 10f;
        }

        private void Update()
        {
            if (_dragonInstance == null) return;

            EnemyBase target = FindNearestEnemy();
            if (target == null) return;

            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                FireAt(target);
                _lastAttackTime = Time.time;
            }

            if (_dragonInstance.Definition.ActiveSkill != null &&
                Time.time - _lastActiveSkillTime >= _activeSkillCooldown)
            {
                AbilityExecutor.ExecuteActiveSkill(
                    _dragonInstance.Definition.ActiveSkill, _dragonInstance, target, transform.position);
                _lastActiveSkillTime = Time.time;
            }
        }

        private EnemyBase FindNearestEnemy()
        {
            float range = _dragonInstance.Definition.NormalAttack != null
                ? _dragonInstance.Definition.NormalAttack.range
                : _dragonInstance.Definition.baseStats.range;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
            EnemyBase nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = enemy; }
            }
            return nearest;
        }

        private void FireAt(EnemyBase target)
        {
            if (_projectilePrefab == null || _firePoint == null) return;

            float dmgMult = _dragonInstance.Definition.NormalAttack != null
                ? _dragonInstance.Definition.NormalAttack.GetDamageMultiplier(_dragonInstance.SkillLevel) : 1f;
            float elemMult = target.HasElement
                ? ElementInteraction.GetMultiplier(_dragonInstance.Definition.element, target.EnemyElement) : 1f;
            float damage = _dragonInstance.Attack * dmgMult * elemMult;

            GameObject go = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
            go.GetComponent<ProjectileBase>()?.Initialize(target.transform, damage);
        }

        private void OnDrawGizmosSelected()
        {
            if (_dragonInstance == null) return;
            float range = _dragonInstance.Definition.NormalAttack != null
                ? _dragonInstance.Definition.NormalAttack.range
                : _dragonInstance.Definition.baseStats.range;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
            if (_dragonInstance.Definition.ActiveSkill is { isAoe: true } skill)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, skill.aoeRadius);
            }
        }
    }
}
