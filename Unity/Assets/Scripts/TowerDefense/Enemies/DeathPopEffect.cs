using System.Collections;
using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class DeathPopEffect : MonoBehaviour
    {
        private const float Lifetime = 0.35f;

        private SpriteRenderer[] _pieces;
        private Vector3[] _velocities;
        private Color _startColor;
        private static Sprite _pieceSprite;

        public static void Spawn(Vector3 position, Color color)
        {
            var root = new GameObject("DeathPopEffect");
            root.transform.position = position;

            var effect = root.AddComponent<DeathPopEffect>();
            effect.Initialize(color);
        }

        private void Initialize(Color color)
        {
            _startColor = new Color(color.r, color.g, color.b, 1f);
            _pieces = new SpriteRenderer[6];
            _velocities = new Vector3[_pieces.Length];

            Sprite sprite = CreateRuntimeSprite();
            for (int i = 0; i < _pieces.Length; i++)
            {
                var piece = new GameObject("PopPiece");
                piece.transform.SetParent(transform, false);
                piece.transform.localScale = Vector3.one * Random.Range(0.07f, 0.12f);

                var renderer = piece.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = _startColor;
                renderer.sortingOrder = 8;
                _pieces[i] = renderer;

                float angle = i * Mathf.PI * 2f / _pieces.Length;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                _velocities[i] = direction * Random.Range(1.2f, 1.9f) + Vector3.up * 0.35f;
            }

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed = 0f;
            while (elapsed < Lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / Lifetime);

                for (int i = 0; i < _pieces.Length; i++)
                {
                    if (_pieces[i] == null) continue;
                    _pieces[i].transform.position += _velocities[i] * Time.deltaTime;
                    _pieces[i].transform.localScale *= 1f + Time.deltaTime * 1.4f;

                    Color color = _startColor;
                    color.a = 1f - t;
                    _pieces[i].color = color;
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private static Sprite CreateRuntimeSprite()
        {
            if (_pieceSprite != null)
                return _pieceSprite;

            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            _pieceSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return _pieceSprite;
        }
    }
}
