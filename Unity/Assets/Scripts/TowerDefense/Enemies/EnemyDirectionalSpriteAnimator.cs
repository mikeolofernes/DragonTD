using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class EnemyDirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private EnemyAnimatorBridge _bridge;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _frontRenderer;
        [SerializeField] private SpriteRenderer _rightRenderer;
        [SerializeField] private SpriteRenderer _backRenderer;
        [SerializeField] private SpriteRenderer _leftRenderer;
        [SerializeField] private Sprite[] _frontWalkFrames;
        [SerializeField] private Sprite[] _rightWalkFrames;
        [SerializeField] private Sprite[] _backWalkFrames;
        [SerializeField] private Sprite[] _leftWalkFrames;
        [SerializeField] private float _walkFps = 8f;
        [SerializeField] private float _strideBobAmplitude = 0.035f;
        [SerializeField] private float _strideSwayAmplitude = 0.012f;

        private float _timer;
        private int _frameIndex;
        private EnemyFacingDirection _lastDirection = EnemyFacingDirection.Right;
        private Vector3 _spriteBasePosition;
        private Vector3 _frontBasePosition;
        private Vector3 _rightBasePosition;
        private Vector3 _backBasePosition;
        private Vector3 _leftBasePosition;
        private Vector3 _frontBaseScale = Vector3.one;
        private Vector3 _rightBaseScale = Vector3.one;
        private Vector3 _backBaseScale = Vector3.one;
        private Vector3 _leftBaseScale = Vector3.one;
        private Quaternion _frontBaseRotation = Quaternion.identity;
        private Quaternion _rightBaseRotation = Quaternion.identity;
        private Quaternion _backBaseRotation = Quaternion.identity;
        private Quaternion _leftBaseRotation = Quaternion.identity;

        public int CurrentFrameIndex => _frameIndex;
        public EnemyFacingDirection CurrentDirection => _bridge != null ? _bridge.FacingDirection : _lastDirection;

        private void Awake()
        {
            if (_bridge == null)
                _bridge = GetComponent<EnemyAnimatorBridge>();
            // Only fall back to GetComponentInChildren when NO directional renderers are
            // configured. If directional renderers exist, the root SpriteRenderer must
            // NOT be used here — it is parented to the enemy root and ApplyStrideOffset
            // would write back to root.localPosition every frame, pinning the enemy to
            // its spawn position and preventing all movement.
            if (_spriteRenderer == null && _frontRenderer == null && _rightRenderer == null
                && _backRenderer == null && _leftRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }
            CacheBasePositions();
        }

        private void Update()
        {
            if (_bridge == null) return;

            Sprite[] frames = FramesFor(_bridge.FacingDirection);
            SpriteRenderer renderer = RendererFor(_bridge.FacingDirection);
            if (frames == null || frames.Length == 0 || renderer == null) return;

            if (_lastDirection != _bridge.FacingDirection)
            {
                ResetInactiveRenderers(_bridge.FacingDirection);
                _lastDirection = _bridge.FacingDirection;
                _timer = 0f;
                _frameIndex = 0;
            }

            float frameDuration = _walkFps > 0f ? 1f / _walkFps : 0f;
            if (_bridge.IsMoving && frameDuration > 0f)
            {
                _timer += Time.deltaTime;
                while (_timer >= frameDuration)
                {
                    _timer -= frameDuration;
                    _frameIndex = (_frameIndex + 1) % frames.Length;
                }
            }
            else
            {
                _timer = 0f;
                _frameIndex = 0;
            }

            int currentIndex = Mathf.Clamp(_frameIndex, 0, frames.Length - 1);
            renderer.color = WithAlpha(renderer.color, 1f);
            renderer.sprite = frames[currentIndex];
            ApplyStrideOffset(renderer, frameDuration);
        }

        private void CacheBasePositions()
        {
            if (_frontRenderer != null) _frontBasePosition = _frontRenderer.transform.localPosition;
            if (_rightRenderer != null) _rightBasePosition = _rightRenderer.transform.localPosition;
            if (_backRenderer != null) _backBasePosition = _backRenderer.transform.localPosition;
            if (_leftRenderer != null) _leftBasePosition = _leftRenderer.transform.localPosition;
            if (_frontRenderer != null) _frontBaseScale = _frontRenderer.transform.localScale;
            if (_rightRenderer != null) _rightBaseScale = _rightRenderer.transform.localScale;
            if (_backRenderer != null) _backBaseScale = _backRenderer.transform.localScale;
            if (_leftRenderer != null) _leftBaseScale = _leftRenderer.transform.localScale;
            if (_frontRenderer != null) _frontBaseRotation = _frontRenderer.transform.localRotation;
            if (_rightRenderer != null) _rightBaseRotation = _rightRenderer.transform.localRotation;
            if (_backRenderer != null) _backBaseRotation = _backRenderer.transform.localRotation;
            if (_leftRenderer != null) _leftBaseRotation = _leftRenderer.transform.localRotation;
            if (_spriteRenderer != null) _spriteBasePosition = _spriteRenderer.transform.localPosition;
        }

        private void ApplyStrideOffset(SpriteRenderer renderer, float frameDuration)
        {
            if (renderer == null) return;

            Vector3 basePosition = BasePositionFor(_bridge.FacingDirection);
            if (!_bridge.IsMoving || frameDuration <= 0f)
            {
                renderer.transform.localPosition = Vector3.Lerp(renderer.transform.localPosition, basePosition, Time.deltaTime * 18f);
                renderer.transform.localScale = Vector3.Lerp(renderer.transform.localScale, BaseScaleFor(_bridge.FacingDirection), Time.deltaTime * 18f);
                renderer.transform.localRotation = Quaternion.Slerp(renderer.transform.localRotation, BaseRotationFor(_bridge.FacingDirection), Time.deltaTime * 18f);
                return;
            }

            float step = (_frameIndex + Mathf.Clamp01(_timer / frameDuration)) / Mathf.Max(1, FramesFor(_bridge.FacingDirection).Length);
            float strideWave = Mathf.Sin(step * Mathf.PI * 2f);
            float footfall = Mathf.Abs(strideWave);
            float bob = footfall * _strideBobAmplitude;
            float sway = Mathf.Sin(step * Mathf.PI * 4f) * _strideSwayAmplitude;
            float lean = strideWave * 1.8f;
            Vector3 baseScale = BaseScaleFor(_bridge.FacingDirection);
            renderer.transform.localPosition = basePosition + new Vector3(sway, bob, 0f);
            renderer.transform.localScale = new Vector3(baseScale.x * (1f + footfall * 0.025f), baseScale.y * (1f - footfall * 0.018f), baseScale.z);
            renderer.transform.localRotation = BaseRotationFor(_bridge.FacingDirection) * Quaternion.Euler(0f, 0f, lean);
        }

        private Sprite[] FramesFor(EnemyFacingDirection direction)
        {
            return direction switch
            {
                EnemyFacingDirection.Front => _frontWalkFrames,
                EnemyFacingDirection.Right => _rightWalkFrames,
                EnemyFacingDirection.Back => _backWalkFrames,
                EnemyFacingDirection.Left => _leftWalkFrames,
                _ => _rightWalkFrames
            };
        }

        private SpriteRenderer RendererFor(EnemyFacingDirection direction)
        {
            return direction switch
            {
                EnemyFacingDirection.Front => _spriteRenderer != null ? _spriteRenderer : _frontRenderer,
                EnemyFacingDirection.Right => _spriteRenderer != null ? _spriteRenderer : _rightRenderer,
                EnemyFacingDirection.Back => _spriteRenderer != null ? _spriteRenderer : _backRenderer,
                EnemyFacingDirection.Left => _spriteRenderer != null ? _spriteRenderer : _leftRenderer,
                _ => _spriteRenderer != null ? _spriteRenderer : _rightRenderer
            };
        }

        private Vector3 BasePositionFor(EnemyFacingDirection direction)
        {
            return direction switch
            {
                EnemyFacingDirection.Front => _spriteRenderer != null ? _spriteBasePosition : _frontBasePosition,
                EnemyFacingDirection.Right => _spriteRenderer != null ? _spriteBasePosition : _rightBasePosition,
                EnemyFacingDirection.Back => _spriteRenderer != null ? _spriteBasePosition : _backBasePosition,
                EnemyFacingDirection.Left => _spriteRenderer != null ? _spriteBasePosition : _leftBasePosition,
                _ => _spriteRenderer != null ? _spriteBasePosition : _rightBasePosition
            };
        }

        private Vector3 BaseScaleFor(EnemyFacingDirection direction)
        {
            return direction switch
            {
                EnemyFacingDirection.Front => _spriteRenderer != null ? Vector3.one : _frontBaseScale,
                EnemyFacingDirection.Right => _spriteRenderer != null ? Vector3.one : _rightBaseScale,
                EnemyFacingDirection.Back => _spriteRenderer != null ? Vector3.one : _backBaseScale,
                EnemyFacingDirection.Left => _spriteRenderer != null ? Vector3.one : _leftBaseScale,
                _ => _spriteRenderer != null ? Vector3.one : _rightBaseScale
            };
        }

        private Quaternion BaseRotationFor(EnemyFacingDirection direction)
        {
            return direction switch
            {
                EnemyFacingDirection.Front => _spriteRenderer != null ? Quaternion.identity : _frontBaseRotation,
                EnemyFacingDirection.Right => _spriteRenderer != null ? Quaternion.identity : _rightBaseRotation,
                EnemyFacingDirection.Back => _spriteRenderer != null ? Quaternion.identity : _backBaseRotation,
                EnemyFacingDirection.Left => _spriteRenderer != null ? Quaternion.identity : _leftBaseRotation,
                _ => _spriteRenderer != null ? Quaternion.identity : _rightBaseRotation
            };
        }

        private void ResetInactiveRenderers(EnemyFacingDirection activeDirection)
        {
            ResetRenderer(_frontRenderer, _frontBasePosition, _frontBaseScale, _frontBaseRotation, activeDirection == EnemyFacingDirection.Front);
            ResetRenderer(_rightRenderer, _rightBasePosition, _rightBaseScale, _rightBaseRotation, activeDirection == EnemyFacingDirection.Right);
            ResetRenderer(_backRenderer, _backBasePosition, _backBaseScale, _backBaseRotation, activeDirection == EnemyFacingDirection.Back);
            ResetRenderer(_leftRenderer, _leftBasePosition, _leftBaseScale, _leftBaseRotation, activeDirection == EnemyFacingDirection.Left);
        }

        private static void ResetRenderer(SpriteRenderer renderer, Vector3 position, Vector3 scale, Quaternion rotation, bool isActive)
        {
            if (renderer == null || isActive) return;
            renderer.transform.localPosition = position;
            renderer.transform.localScale = scale;
            renderer.transform.localRotation = rotation;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
