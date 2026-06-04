using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DragonTD.TowerDefense
{
    public class WaypointPathAuthoring : MonoBehaviour
    {
        [SerializeField] private bool _useChildren = true;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private Color _lineColor = new Color(0.25f, 1f, 0.95f, 0.9f);
        [SerializeField] private Color _nodeColor = new Color(1f, 0.9f, 0.25f, 0.95f);
        [SerializeField] private float _nodeRadius = 0.18f;

        public bool HasWaypoints => GetWaypointTransforms().Length >= 2;

        public Transform[] GetWaypointTransforms()
        {
            if (!_useChildren)
                return CompactAssignedWaypoints();

            int count = transform.childCount;
            if (count == 0)
                return CompactAssignedWaypoints();

            Transform[] childWaypoints = new Transform[count];
            for (int i = 0; i < count; i++)
                childWaypoints[i] = transform.GetChild(i);
            return childWaypoints;
        }

        public Vector3[] GetWorldPoints()
        {
            Transform[] waypointTransforms = GetWaypointTransforms();
            Vector3[] points = new Vector3[waypointTransforms.Length];
            for (int i = 0; i < waypointTransforms.Length; i++)
                points[i] = waypointTransforms[i].position;
            return points;
        }

        public void RebuildFromPoints(Vector3[] points, string waypointPrefix = "WP")
        {
            if (points == null) return;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }

            _useChildren = true;
            _waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject($"{waypointPrefix}_{i:00}");
                waypoint.transform.SetParent(transform, false);
                waypoint.transform.position = points[i];
                _waypoints[i] = waypoint.transform;
            }
        }

        private Transform[] CompactAssignedWaypoints()
        {
            if (_waypoints == null || _waypoints.Length == 0)
                return System.Array.Empty<Transform>();

            int count = 0;
            for (int i = 0; i < _waypoints.Length; i++)
                if (_waypoints[i] != null)
                    count++;

            Transform[] compact = new Transform[count];
            int write = 0;
            for (int i = 0; i < _waypoints.Length; i++)
                if (_waypoints[i] != null)
                    compact[write++] = _waypoints[i];

            return compact;
        }

        private void OnValidate()
        {
            if (!_useChildren || transform.childCount == 0)
                return;

            _waypoints = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                _waypoints[i] = transform.GetChild(i);
        }

        private void OnDrawGizmos()
        {
            Transform[] waypointTransforms = GetWaypointTransforms();
            if (waypointTransforms.Length == 0) return;

            Gizmos.color = _lineColor;
            for (int i = 1; i < waypointTransforms.Length; i++)
            {
                if (waypointTransforms[i - 1] == null || waypointTransforms[i] == null)
                    continue;
                Gizmos.DrawLine(waypointTransforms[i - 1].position, waypointTransforms[i].position);
            }

            Gizmos.color = _nodeColor;
            for (int i = 0; i < waypointTransforms.Length; i++)
            {
                if (waypointTransforms[i] == null) continue;
                Gizmos.DrawSphere(waypointTransforms[i].position, _nodeRadius);

#if UNITY_EDITOR
                Handles.Label(waypointTransforms[i].position + Vector3.up * 0.22f, i.ToString("00"));
#endif
            }
        }
    }
}
