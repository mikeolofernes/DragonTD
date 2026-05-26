using UnityEngine;

namespace DragonTD.TowerDefense
{
    // Revives once at 50% HP on first death.
    public class UndeadEnemy : EnemyBase
    {
        private bool _hasRevived;

        protected override void Die()
        {
            if (!_hasRevived)
            {
                _hasRevived = true;
                _currentHp = _maxHp * 0.5f;
                RaiseHpChanged();
                return;
            }
            base.Die();
        }
    }
}
