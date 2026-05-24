using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData _data;

        protected float _currentHp;
        protected Transform[] _waypoints;
        protected int _waypointIndex;

        public bool IsDead { get; private set; }

        public event System.Action<EnemyBase> OnDied;

        public float DistanceToGoal
        {
            get
            {
                if (_waypoints == null || _waypointIndex >= _waypoints.Length)
                    return 0f;

                float distance = Vector3.Distance(transform.position, _waypoints[_waypointIndex].position);
                for (int i = _waypointIndex + 1; i < _waypoints.Length; i++)
                {
                    distance += Vector3.Distance(_waypoints[i - 1].position, _waypoints[i].position);
                }
                return distance;
            }
        }

        public void Initialize(Transform[] waypoints)
        {
            _waypoints = waypoints;
            _currentHp = _data.MaxHp;
            _waypointIndex = 0;
        }

        private void Update()
        {
            if (!IsDead)
                MoveTowardsWaypoint();
        }

        protected virtual void MoveTowardsWaypoint()
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Length)
                return;

            Transform target = _waypoints[_waypointIndex];
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * (_data.MoveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Length)
                {
                    ReachBase();
                }
            }
        }

        public virtual void TakeDamage(float damage)
        {
            float effective = Mathf.Max(1f, damage - _data.Armor);
            _currentHp -= effective;
            if (_currentHp <= 0f)
                Die();
        }

        protected virtual void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDied?.Invoke(this);
            WaveManager.Instance.OnEnemyDied();
            ResourceManager.Instance.AddGold(_data.GoldValue);
            Destroy(gameObject);
        }

        protected void ReachBase()
        {
            GameManager.Instance.LoseLife(_data.DamageToBase);
            Die();
        }
    }
}
