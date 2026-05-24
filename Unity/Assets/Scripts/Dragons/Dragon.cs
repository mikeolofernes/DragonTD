using UnityEngine;

namespace DragonTD.Dragons
{
    public class Dragon : MonoBehaviour
    {
        private DragonInstance _instance;
        private float _currentHp;

        public bool IsAlive { get; private set; }
        public DragonInstance Instance => _instance;

        public event System.Action<Dragon> OnDragonDied;

        public float HpPercent => _currentHp / _instance.Hp;

        public void Initialize(DragonInstance instance)
        {
            _instance = instance;
            _currentHp = instance.Hp;
            IsAlive = true;
        }

        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _currentHp -= damage;
            if (_currentHp <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            IsAlive = false;
            OnDragonDied?.Invoke(this);
            Destroy(gameObject, 1f);
        }
    }
}
