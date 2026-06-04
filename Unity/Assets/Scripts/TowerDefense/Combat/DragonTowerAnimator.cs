using UnityEngine;

namespace DragonTD.TowerDefense
{
    /// <summary>
    /// Plays a horizontal sprite-sheet idle animation on a placed DragonTower.
    /// Attach alongside a SpriteRenderer. The sheet must be imported as Multiple sprites
    /// (set up by SceneBootstrapper.ImportDeployedStrip). _frames is populated at runtime
    /// by scanning sibling sprites in the same texture asset.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class DragonTowerAnimator : MonoBehaviour
    {
        [SerializeField] private int     _frameCount = 5;
        [SerializeField] private float   _fps        = 8f;
        [SerializeField] private Sprite[] _frames;

        private SpriteRenderer _sr;
        private float          _timer;
        private int            _currentFrame;
        // Subtle idle bob
        private float _bobPhase;
        private Vector3 _baseScale;

        private void Awake()
        {
            _sr        = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
            _bobPhase  = Random.value * Mathf.PI * 2f;
            TryBuildFrames();
        }

        private void TryBuildFrames()
        {
#if UNITY_EDITOR
            if (_sr == null || _sr.sprite == null) return;
            string path = UnityEditor.AssetDatabase.GetAssetPath(_sr.sprite);
            if (string.IsNullOrEmpty(path)) return;

            var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new System.Collections.Generic.List<Sprite>();
            foreach (var obj in all)
                if (obj is Sprite s) list.Add(s);
            list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            if (list.Count > 1)
            {
                _frames     = list.ToArray();
                _frameCount = _frames.Length;
            }
#endif
        }

        private void Update()
        {
            // Frame animation
            if (_frames != null && _frames.Length > 1 && _fps > 0f)
            {
                _timer += Time.deltaTime;
                float dur = 1f / _fps;
                if (_timer >= dur)
                {
                    _timer -= dur;
                    _currentFrame = (_currentFrame + 1) % _frames.Length;
                    _sr.sprite    = _frames[_currentFrame];
                }
            }

            // Gentle idle breathe scale
            float breathe = 1f + Mathf.Sin(Time.time * 1.8f + _bobPhase) * 0.025f;
            transform.localScale = _baseScale * breathe;
        }
    }
}
