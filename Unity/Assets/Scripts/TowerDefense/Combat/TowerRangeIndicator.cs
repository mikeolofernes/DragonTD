using UnityEngine;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class TowerRangeIndicator : MonoBehaviour
    {
        private LineRenderer _lineRenderer;
        private float _range = 3f;
        private Color _color = Color.white;

        public void Configure(DragonInstance dragon)
        {
            if (dragon == null || dragon.Definition == null) return;

            _range = dragon.Definition.NormalAttack != null
                ? dragon.Definition.NormalAttack.range
                : dragon.Definition.baseStats.range;
            _color = dragon.Definition.visualData.primaryColor;
            _color.a = 0.34f;
        }

        public void Configure(float range, Color color)
        {
            _range = range;
            _color = color;
            _color.a = 0.34f;
        }

        private void OnMouseEnter()
        {
            EnsureRenderer();
            DrawRing();
            _lineRenderer.enabled = true;
        }

        private void OnMouseExit()
        {
            if (_lineRenderer != null)
                _lineRenderer.enabled = false;
        }

        private void EnsureRenderer()
        {
            if (_lineRenderer != null) return;

            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.loop = true;
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 64;
            _lineRenderer.startWidth = 0.025f;
            _lineRenderer.endWidth = 0.025f;
            _lineRenderer.sortingOrder = 18;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.enabled = false;
        }

        private void DrawRing()
        {
            _lineRenderer.startColor = _color;
            _lineRenderer.endColor = _color;

            Vector3 center = transform.position;
            center.z = 0f;
            for (int i = 0; i < _lineRenderer.positionCount; i++)
            {
                float radians = i / (float)_lineRenderer.positionCount * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * _range;
                _lineRenderer.SetPosition(i, point);
            }
        }
    }
}
