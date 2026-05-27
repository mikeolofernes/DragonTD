using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class OrcEnemy : EnemyBase
    {
        protected override void MoveTowardsWaypoint()
        {
            if (_waypoints == null || _waypointIndex >= _waypoints.Length)
                return;

            Transform target = _waypoints[_waypointIndex];
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * (CurrentMoveSpeed * 1.2f * Time.deltaTime);

            if (Vector3.Distance(transform.position, target.position) < 0.1f)
            {
                _waypointIndex++;
                if (_waypointIndex >= _waypoints.Length)
                {
                    ReachBase();
                }
            }
        }

        protected override void Die()
        {
            // TODO: Play orc death visual effect (e.g., particle burst) and audio clip here
            base.Die();
        }
    }
}
