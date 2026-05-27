using System.Collections;
using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class SkillCastEffect : MonoBehaviour
    {
        private const float BeamLifetime = 0.22f;
        private const float PulseLifetime = 0.42f;
        private static Sprite _runtimeSprite;

        private LineRenderer _line;
        private SpriteRenderer _spriteRenderer;
        private Color _color;
        private float _radius;

        public static void SpawnBeam(Vector3 start, Vector3 end, Color color)
        {
            var go = new GameObject("SkillBeamEffect");
            var effect = go.AddComponent<SkillCastEffect>();
            effect.InitializeBeam(start, end, color);
        }

        public static void SpawnPulse(Vector3 position, float radius, Color color)
        {
            var go = new GameObject("SkillPulseEffect");
            go.transform.position = position;
            var effect = go.AddComponent<SkillCastEffect>();
            effect.InitializePulse(radius, color);
        }

        private void InitializeBeam(Vector3 start, Vector3 end, Color color)
        {
            _color = new Color(color.r, color.g, color.b, 1f);
            _line = gameObject.AddComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.SetPosition(0, start);
            _line.SetPosition(1, end);
            _line.startWidth = 0.18f;
            _line.endWidth = 0.08f;
            _line.startColor = _color;
            _line.endColor = new Color(_color.r, _color.g, _color.b, 0.2f);
            _line.sortingOrder = 14;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            StartCoroutine(FadeBeam());
        }

        private void InitializePulse(float radius, Color color)
        {
            _radius = Mathf.Max(0.6f, radius);
            _color = new Color(color.r, color.g, color.b, 0.45f);
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = RuntimeSprite();
            _spriteRenderer.color = _color;
            _spriteRenderer.sortingOrder = 13;
            transform.localScale = Vector3.one * 0.2f;
            StartCoroutine(ExpandPulse());
        }

        private IEnumerator FadeBeam()
        {
            float elapsed = 0f;
            while (elapsed < BeamLifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / BeamLifetime);
                float alpha = 1f - t;
                _line.startColor = new Color(_color.r, _color.g, _color.b, alpha);
                _line.endColor = new Color(_color.r, _color.g, _color.b, alpha * 0.2f);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator ExpandPulse()
        {
            float elapsed = 0f;
            while (elapsed < PulseLifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PulseLifetime);
                transform.localScale = Vector3.one * Mathf.Lerp(0.2f, _radius * 2f, t);

                if (_spriteRenderer != null)
                    _spriteRenderer.color = new Color(_color.r, _color.g, _color.b, Mathf.Lerp(0.45f, 0f, t));

                yield return null;
            }

            Destroy(gameObject);
        }

        private static Sprite RuntimeSprite()
        {
            if (_runtimeSprite != null)
                return _runtimeSprite;

            const int size = 64;
            var texture = new Texture2D(size, size);
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.46f;
            float inner = size * 0.34f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = dist <= outer && dist >= inner ? 1f : 0f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            _runtimeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _runtimeSprite;
        }
    }
}
