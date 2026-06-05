using System.Collections.Generic;
using UnityEngine;

namespace DragonTD.TowerDefense
{
    public enum EnemyFacingDirection
    {
        Front = 0,
        Right = 1,
        Back = 2,
        Left = 3
    }

    /// <summary>
    /// Presentation bridge for polished enemy prefabs. EnemyBase owns gameplay state;
    /// this component forwards that state into an optional Unity Animator controller.
    /// </summary>
    public class EnemyAnimatorBridge : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform _facingRoot;
        [SerializeField] private GameObject _frontView;
        [SerializeField] private GameObject _rightView;
        [SerializeField] private GameObject _backView;
        [SerializeField] private GameObject _leftView;
        [SerializeField] private bool _usesDirectionalSpriteFrames;
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private bool _rotateModelRootToFacing;
        [SerializeField] private float _modelYawOffset;

        [Header("Animator Parameters")]
        [SerializeField] private string _movingBool = "IsMoving";
        [SerializeField] private string _speedFloat = "Speed01";
        [SerializeField] private string _mixamoSpeedFloat = "Speed";
        [SerializeField] private string _facingFloat = "FacingX";
        [SerializeField] private string _facingYFloat = "FacingY";
        [SerializeField] private string _facingDirectionInt = "FacingDirection";
        [SerializeField] private string _stunnedBool = "IsStunned";
        [SerializeField] private string _hitTrigger = "Hit";
        [SerializeField] private string _deathTrigger = "Death";

        private readonly HashSet<int> _availableParameters = new HashSet<int>();
        private RuntimeAnimatorController _cachedController;
        private Vector3 _facingBaseScale = Vector3.one;
        private EnemyFacingDirection _pendingFacingDirection = EnemyFacingDirection.Right;
        private float _pendingFacingTime;
        private const float FacingSwitchDelay = 0.12f;

        public bool IsMoving { get; private set; }
        public bool IsStunned { get; private set; }
        public bool IsDead { get; private set; }
        public float Speed01 { get; private set; }
        public float FacingX { get; private set; } = 1f;
        public float FacingY { get; private set; }
        public EnemyFacingDirection FacingDirection { get; private set; } = EnemyFacingDirection.Right;
        public float HitPulse { get; private set; }
        public SpriteRenderer PrimarySpriteRenderer => _spriteRenderer;
        public bool HasRuntimeAnimator => _animator != null && _animator.runtimeAnimatorController != null;
        public bool HasDirectionalPresentation => HasCompleteDirectionalViews() || _usesDirectionalSpriteFrames;
        public bool HasPolishedPresentation => HasRuntimeAnimator || HasCompleteDirectionalViews() || _usesDirectionalSpriteFrames;

        private void Awake()
        {
            AutoBind();
            ApplyFacing();
        }

        private void OnValidate()
        {
            AutoBind();
            _availableParameters.Clear();
            _cachedController = null;
        }

        private void LateUpdate()
        {
            if (HitPulse > 0f)
                HitPulse = Mathf.MoveTowards(HitPulse, 0f, Time.deltaTime * 8f);
        }

        public void SetMovement(Vector3 direction, float speed01, bool isMoving)
        {
            AutoBind();
            IsMoving = isMoving && !IsDead;
            Speed01 = Mathf.Clamp01(speed01);

            UpdateFacing(direction, IsMoving);

            ApplyFacing();
            SetBool(_movingBool, IsMoving);
            SetFloat(_speedFloat, Speed01);
            SetFloat(_mixamoSpeedFloat, Speed01);
            SetFloat(_facingFloat, FacingX);
            SetFloat(_facingYFloat, FacingY);
            SetInt(_facingDirectionInt, (int)FacingDirection);
        }

        public void SetStunned(bool isStunned)
        {
            AutoBind();
            IsStunned = isStunned && !IsDead;
            SetBool(_stunnedBool, IsStunned);
        }

        public void PlayHit()
        {
            if (IsDead) return;
            AutoBind();
            HitPulse = 1f;
            SetTrigger(_hitTrigger);
        }

        public void PlayDeath()
        {
            AutoBind();
            IsDead = true;
            IsMoving = false;
            IsStunned = false;
            SetBool(_movingBool, false);
            SetBool(_stunnedBool, false);
            SetTrigger(_deathTrigger);
        }

        private void AutoBind()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_spriteRenderer == null && !HasCompleteDirectionalViews())
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (_facingRoot == null)
                _facingRoot = transform;

            if (_facingRoot != null && _facingBaseScale == Vector3.one)
                _facingBaseScale = _facingRoot.localScale;
        }

        private void UpdateFacing(Vector3 direction, bool isMoving)
        {
            if (direction.sqrMagnitude <= 0.0025f)
                return;

            EnemyFacingDirection requestedDirection;
            float requestedX;
            float requestedY;
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                requestedX = direction.x < 0f ? -1f : 1f;
                requestedY = 0f;
                requestedDirection = requestedX < 0f ? EnemyFacingDirection.Left : EnemyFacingDirection.Right;
            }
            else
            {
                requestedX = 0f;
                requestedY = direction.y < 0f ? -1f : 1f;
                requestedDirection = requestedY < 0f ? EnemyFacingDirection.Front : EnemyFacingDirection.Back;
            }

            if (!isMoving || requestedDirection == FacingDirection)
            {
                CommitFacing(requestedDirection, requestedX, requestedY);
                return;
            }

            if (requestedDirection != _pendingFacingDirection)
            {
                _pendingFacingDirection = requestedDirection;
                _pendingFacingTime = 0f;
                return;
            }

            _pendingFacingTime += Time.deltaTime;
            if (_pendingFacingTime >= FacingSwitchDelay)
                CommitFacing(requestedDirection, requestedX, requestedY);
        }

        private void CommitFacing(EnemyFacingDirection direction, float facingX, float facingY)
        {
            FacingDirection = direction;
            FacingX = facingX;
            FacingY = facingY;
            _pendingFacingDirection = direction;
            _pendingFacingTime = 0f;
        }

        private void ApplyFacing()
        {
            bool hasCompleteViews = HasCompleteDirectionalViews();
            if (hasCompleteViews)
            {
                SetViewActive(_frontView, FacingDirection == EnemyFacingDirection.Front);
                SetViewActive(_rightView, FacingDirection == EnemyFacingDirection.Right);
                SetViewActive(_backView, FacingDirection == EnemyFacingDirection.Back);
                SetViewActive(_leftView, FacingDirection == EnemyFacingDirection.Left);
            }

            if (_spriteRenderer != null && !hasCompleteViews && !_usesDirectionalSpriteFrames)
                _spriteRenderer.flipX = FacingDirection == EnemyFacingDirection.Left;

            if (_facingRoot == null) return;
            if (!hasCompleteViews && !_usesDirectionalSpriteFrames)
            {
                float xSign = FacingDirection == EnemyFacingDirection.Left ? -1f : 1f;
                _facingRoot.localScale = new Vector3(Mathf.Abs(_facingBaseScale.x) * xSign, _facingBaseScale.y, _facingBaseScale.z);
            }

            if (_modelRoot != null && _rotateModelRootToFacing)
            {
                float yaw = Mathf.Atan2(FacingX, FacingY) * Mathf.Rad2Deg + _modelYawOffset;
                _modelRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }
        }

        private bool HasCompleteDirectionalViews()
        {
            return _frontView != null && _rightView != null && _backView != null && _leftView != null;
        }

        private static void SetViewActive(GameObject view, bool active)
        {
            if (view != null && view.activeSelf != active)
                view.SetActive(active);
        }

        private void SetBool(string parameter, bool value)
        {
            if (!CanSet(parameter)) return;
            _animator.SetBool(parameter, value);
        }

        private void SetFloat(string parameter, float value)
        {
            if (!CanSet(parameter)) return;
            _animator.SetFloat(parameter, value);
        }

        private void SetInt(string parameter, int value)
        {
            if (!CanSet(parameter)) return;
            _animator.SetInteger(parameter, value);
        }

        private void SetTrigger(string parameter)
        {
            if (!CanSet(parameter)) return;
            _animator.SetTrigger(parameter);
        }

        private bool CanSet(string parameter)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(parameter))
                return false;

            CacheParameters();
            return _availableParameters.Contains(Animator.StringToHash(parameter));
        }

        private void CacheParameters()
        {
            if (_animator == null || _cachedController == _animator.runtimeAnimatorController) return;

            _availableParameters.Clear();
            _cachedController = _animator.runtimeAnimatorController;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
                _availableParameters.Add(parameter.nameHash);
        }
    }
}
