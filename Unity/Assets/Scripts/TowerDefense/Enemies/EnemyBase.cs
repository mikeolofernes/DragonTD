using UnityEngine;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData _data;

        protected float _currentHp;
        protected float _maxHp;
        protected float _moveSpeed;
        protected Transform[] _waypoints;
        protected int _waypointIndex;

        public bool IsDead { get; private set; }
        public float HpPercent => _maxHp > 0f ? _currentHp / _maxHp : 0f;
        public DragonElement EnemyElement => _data.Element;
        public bool HasElement => _data.HasElement;

        public event System.Action<EnemyBase> OnDied;
        public event System.Action<float> OnHpChanged;

        protected void RaiseHpChanged() => OnHpChanged?.Invoke(HpPercent);

        public float DistanceToGoal
        {
            get
            {
                if (_waypoints == null || _waypointIndex >= _waypoints.Length) return 0f;
                float dist = Vector3.Distance(transform.position, _waypoints[_waypointIndex].position);
                for (int i = _waypointIndex + 1; i < _waypoints.Length; i++)
                    dist += Vector3.Distance(_waypoints[i - 1].position, _waypoints[i].position);
                return dist;
            }
        }

        public void Initialize(Transform[] waypoints)
        {
            _waypoints = waypoints;
            _maxHp = _data.MaxHp;
            _moveSpeed = _data.MoveSpeed;
            _currentHp = _maxHp;
            _waypointIndex = 0;
        }

        public void ApplyDifficultyMultiplier(float multiplier)
        {
            _maxHp *= multiplier;
            _currentHp = _maxHp;
            _moveSpeed *= Mathf.Lerp(1f, multiplier, 0.5f);
            OnHpChanged?.Invoke(HpPercent);
        }

        private void Update()
        {
            if (IsDead) return;
            MoveTowardsWaypoint();
            Tick();
        }

        protected virtual void Tick() { }

        protected virtual void MoveTowardsWaypoint()
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Length) return;

            Transform target = _waypoints[_waypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, target.position, _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Length)
                    ReachBase();
            }
        }

        public virtual void TakeDamage(float damage)
        {
            if (IsDead) return;
            float effective = Mathf.Max(1f, damage - _data.Armor);
            _currentHp -= effective;
            OnHpChanged?.Invoke(HpPercent);
            if (_currentHp <= 0f) Die();
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
