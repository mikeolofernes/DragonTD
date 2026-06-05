using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class OrcEnemy : EnemyBase
    {
        protected override void MoveTowardsWaypoint()
        {
            base.MoveTowardsWaypoint();
        }

        protected override void Die()
        {
            // TODO: Play orc death visual effect (e.g., particle burst) and audio clip here
            base.Die();
        }
    }
}
