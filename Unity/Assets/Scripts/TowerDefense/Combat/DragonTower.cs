using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class DragonTower : MonoBehaviour
    {
        private DragonInstance _dragonInstance;
        private float _attackCooldown;
        private float _lastAttackTime;

        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private Transform _firePoint;

        public void Setup(DragonInstance instance)
        {
            _dragonInstance = instance;
            _attackCooldown = instance.Data.NormalAttack != null ? instance.Data.NormalAttack.Cooldown : 1f;
        }

        private void Update()
        {
            if (_dragonInstance == null) return;

            if (Time.time - _lastAttackTime >= _attackCooldown)
            {
                EnemyBase target = FindNearestEnemy();
                if (target != null)
                {
                    FireAt(target);
                    _lastAttackTime = Time.time;
                }
            }
        }

        private EnemyBase FindNearestEnemy()
        {
            float range = _dragonInstance.Data.NormalAttack != null
                ? _dragonInstance.Data.NormalAttack.Range
                : _dragonInstance.Data.BaseRange;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);

            EnemyBase nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private void FireAt(EnemyBase target)
        {
            if (_projectilePrefab == null || _firePoint == null) return;

            float damageMultiplier = _dragonInstance.Data.NormalAttack != null
                ? _dragonInstance.Data.NormalAttack.DamageMultiplier
                : 1f;
            float damage = _dragonInstance.Attack * damageMultiplier;

            GameObject projectileGO = Instantiate(_projectilePrefab, _firePoint.position, Quaternion.identity);
            ProjectileBase projectile = projectileGO.GetComponent<ProjectileBase>();
            if (projectile != null)
            {
                projectile.Initialize(target.transform, damage);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_dragonInstance == null) return;

            float range = _dragonInstance.Data.NormalAttack != null
                ? _dragonInstance.Data.NormalAttack.Range
                : _dragonInstance.Data.BaseRange;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
