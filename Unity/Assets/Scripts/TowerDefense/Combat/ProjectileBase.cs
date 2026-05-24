using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class ProjectileBase : MonoBehaviour
    {
        private Transform _target;
        private float _damage;
        private float _speed = 10f;

        public void Initialize(Transform target, float damage, float speed = 10f)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            EnemyBase enemy = _target.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, _target.position, _speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _target.position) < 0.2f)
            {
                Hit();
            }
        }

        private void Hit()
        {
            EnemyBase enemy = _target.GetComponent<EnemyBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
            }
            Destroy(gameObject);
        }
    }
}
