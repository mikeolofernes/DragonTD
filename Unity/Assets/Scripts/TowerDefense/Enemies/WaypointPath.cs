using UnityEngine;

namespace DragonTD.TowerDefense
{
    // Add to the waypoint container to visualize the enemy path in the Scene view.
    public class WaypointPath : MonoBehaviour
    {
        [SerializeField] private Color _lineColor = Color.yellow;
        [SerializeField] private float _nodeRadius = 0.2f;

        private void OnDrawGizmos()
        {
            int count = transform.childCount;
            if (count == 0) return;

            Gizmos.color = _lineColor;
            for (int i = 0; i < count; i++)
            {
                Transform wp = transform.GetChild(i);
                Gizmos.DrawSphere(wp.position, _nodeRadius);
                if (i > 0)
                    Gizmos.DrawLine(transform.GetChild(i - 1).position, wp.position);
            }
        }
    }
}
