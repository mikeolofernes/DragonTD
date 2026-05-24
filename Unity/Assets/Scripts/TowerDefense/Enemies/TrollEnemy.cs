using UnityEngine;

namespace DragonTD.TowerDefense
{
    // Slow tank with HP regeneration per second. Set low MoveSpeed in EnemyData.
    public class TrollEnemy : EnemyBase
    {
        [SerializeField] private float _regenPerSecond = 8f;

        protected override void Tick()
        {
            if (_currentHp < _maxHp)
            {
                _currentHp = Mathf.Min(_maxHp, _currentHp + _regenPerSecond * Time.deltaTime);
                OnHpChanged?.Invoke(HpPercent);
            }
        }
    }
}
