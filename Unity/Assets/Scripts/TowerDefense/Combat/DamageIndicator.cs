using System.Collections;
using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class DamageIndicator : MonoBehaviour
    {
        private const float Lifetime = 0.65f;
        private const float RiseDistance = 0.55f;

        private TextMesh _textMesh;
        private Vector3 _startPosition;
        private Color _startColor;

        public static void Spawn(Vector3 position, float damage, Color color)
        {
            var go = new GameObject("DamageIndicator");
            go.transform.position = position;

            var indicator = go.AddComponent<DamageIndicator>();
            indicator.Initialize(Mathf.CeilToInt(damage).ToString(), color);
        }

        public static void SpawnText(Vector3 position, string text, Color color)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var go = new GameObject("DamageIndicator");
            go.transform.position = position;

            var indicator = go.AddComponent<DamageIndicator>();
            indicator.Initialize(text, color);
        }

        private void Initialize(string text, Color color)
        {
            _startPosition = transform.position;
            _startColor = new Color(color.r, color.g, color.b, 1f);

            _textMesh = gameObject.AddComponent<TextMesh>();
            _textMesh.text = text;
            _textMesh.anchor = TextAnchor.MiddleCenter;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.fontSize = 32;
            _textMesh.characterSize = 0.08f;
            _textMesh.color = _startColor;

            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 20;

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            while (elapsed < Lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Lifetime);
                transform.position = _startPosition + Vector3.up * (RiseDistance * t);

                if (_textMesh != null)
                {
                    Color color = _startColor;
                    color.a = 1f - t;
                    _textMesh.color = color;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
