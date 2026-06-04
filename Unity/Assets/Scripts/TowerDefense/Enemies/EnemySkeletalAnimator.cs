using UnityEngine;

namespace DragonTD.TowerDefense
{
    public class EnemySkeletalAnimator : MonoBehaviour
    {
        [Header("Rig Root")]
        [SerializeField] private Transform _facingRoot;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _tail;

        [Header("Front Limbs")]
        [SerializeField] private Transform _frontUpperLeg;
        [SerializeField] private Transform _frontLowerLeg;
        [SerializeField] private Transform _frontArm;

        [Header("Back Limbs")]
        [SerializeField] private Transform _backUpperLeg;
        [SerializeField] private Transform _backLowerLeg;
        [SerializeField] private Transform _backArm;

        [Header("Motion")]
        [SerializeField] private float _walkCyclesPerSecond = 2.2f;
        [SerializeField] private float _runCyclesPerSecond = 3.4f;
        [SerializeField] private float _legSwingDegrees = 24f;
        [SerializeField] private float _lowerLegSwingDegrees = 16f;
        [SerializeField] private float _armSwingDegrees = 12f;
        [SerializeField] private float _bodyBob = 0.045f;
        [SerializeField] private float _bodyLeanDegrees = 4f;
        [SerializeField] private float _tailSwingDegrees = 8f;
        [SerializeField] private float _idleBreathScale = 0.018f;

        private Transform[] _parts;
        private Vector3[] _basePositions;
        private Vector3[] _baseScales;
        private Quaternion[] _baseRotations;
        private Vector3 _facingBaseScale = Vector3.one;
        private float _phase;
        private bool _cached;

        public bool IsConfigured =>
            _body != null ||
            _frontUpperLeg != null ||
            _backUpperLeg != null ||
            _frontArm != null ||
            _backArm != null;

        private void Awake()
        {
            AutoBindMissingParts();
            CacheRig();
        }

        private void OnValidate()
        {
            AutoBindMissingParts();
            _cached = false;
        }

        public void TickWalk(Vector3 direction, float speed01, EnemyTrait trait)
        {
            if (!IsConfigured) return;
            CacheRig();

            float cyclesPerSecond = trait == EnemyTrait.Runner ? _runCyclesPerSecond : _walkCyclesPerSecond;
            if (trait == EnemyTrait.Brute)
                cyclesPerSecond *= 0.78f;

            _phase += Time.deltaTime * cyclesPerSecond * Mathf.PI * 2f * Mathf.Lerp(0.7f, 1.3f, Mathf.Clamp01(speed01));
            float sin = Mathf.Sin(_phase);
            float cos = Mathf.Cos(_phase);
            float contact = Mathf.Abs(cos);
            float lean = Mathf.Clamp(-direction.x * _bodyLeanDegrees + sin * 1.4f, -7f, 7f);

            ApplyFacing(direction);
            ApplyLocal(_body, new Vector3(0f, contact * _bodyBob, 0f), Quaternion.Euler(0f, 0f, lean), Vector3.one);
            ApplyLocal(_head, new Vector3(0f, contact * _bodyBob * 0.55f, 0f), Quaternion.Euler(0f, 0f, -lean * 0.45f), Vector3.one);
            ApplyLocal(_tail, Vector3.zero, Quaternion.Euler(0f, 0f, -lean * 0.6f + sin * _tailSwingDegrees), Vector3.one);

            Swing(_frontUpperLeg, sin * _legSwingDegrees);
            Swing(_frontLowerLeg, -Mathf.Max(0f, -sin) * _lowerLegSwingDegrees);
            Swing(_backUpperLeg, -sin * _legSwingDegrees);
            Swing(_backLowerLeg, -Mathf.Max(0f, sin) * _lowerLegSwingDegrees);
            Swing(_frontArm, -sin * _armSwingDegrees);
            Swing(_backArm, sin * _armSwingDegrees);
        }

        public void TickIdle()
        {
            if (!IsConfigured) return;
            CacheRig();

            _phase += Time.deltaTime * Mathf.PI;
            float breath = Mathf.Sin(_phase) * _idleBreathScale;
            ApplyLocal(_body, new Vector3(0f, breath * 0.45f, 0f), Quaternion.identity, new Vector3(1f - breath * 0.25f, 1f + breath, 1f));
            ApplyLocal(_head, new Vector3(0f, breath * 0.55f, 0f), Quaternion.Euler(0f, 0f, -breath * 80f), Vector3.one);
            ApplyLocal(_tail, Vector3.zero, Quaternion.Euler(0f, 0f, breath * 80f), Vector3.one);
            Swing(_frontUpperLeg, 0f);
            Swing(_frontLowerLeg, 0f);
            Swing(_backUpperLeg, 0f);
            Swing(_backLowerLeg, 0f);
            Swing(_frontArm, 0f);
            Swing(_backArm, 0f);
        }

        public void AutoBindMissingParts()
        {
            if (_facingRoot == null)
                _facingRoot = transform;

            if (_body == null) _body = FindPart("body", "torso", "chest", "hips");
            if (_head == null) _head = FindPart("head", "face");
            if (_tail == null) _tail = FindPart("tail", "scarf", "cape");
            if (_frontUpperLeg == null) _frontUpperLeg = FindPart("front_upper_leg", "front_thigh", "right_upper_leg", "right_thigh", "leg_front_upper");
            if (_frontLowerLeg == null) _frontLowerLeg = FindPart("front_lower_leg", "front_shin", "right_lower_leg", "right_shin", "leg_front_lower", "front_foot");
            if (_frontArm == null) _frontArm = FindPart("front_arm", "front_upper_arm", "right_arm", "weapon_arm", "arm_front");
            if (_backUpperLeg == null) _backUpperLeg = FindPart("back_upper_leg", "back_thigh", "left_upper_leg", "left_thigh", "leg_back_upper");
            if (_backLowerLeg == null) _backLowerLeg = FindPart("back_lower_leg", "back_shin", "left_lower_leg", "left_shin", "leg_back_lower", "back_foot");
            if (_backArm == null) _backArm = FindPart("back_arm", "back_upper_arm", "left_arm", "shield_arm", "arm_back");
        }

        private void CacheRig()
        {
            AutoBindMissingParts();
            if (_cached) return;

            if (_facingRoot == null)
                _facingRoot = transform;

            _parts = new[]
            {
                _body, _head, _tail,
                _frontUpperLeg, _frontLowerLeg, _frontArm,
                _backUpperLeg, _backLowerLeg, _backArm
            };

            _basePositions = new Vector3[_parts.Length];
            _baseScales = new Vector3[_parts.Length];
            _baseRotations = new Quaternion[_parts.Length];

            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] == null) continue;
                _basePositions[i] = _parts[i].localPosition;
                _baseScales[i] = _parts[i].localScale;
                _baseRotations[i] = _parts[i].localRotation;
            }

            _facingBaseScale = _facingRoot.localScale;
            _cached = true;
        }

        private void ApplyFacing(Vector3 direction)
        {
            if (_facingRoot == null || Mathf.Abs(direction.x) <= 0.05f) return;

            float xSign = direction.x < 0f ? -1f : 1f;
            _facingRoot.localScale = new Vector3(Mathf.Abs(_facingBaseScale.x) * xSign, _facingBaseScale.y, _facingBaseScale.z);
        }

        private void Swing(Transform part, float degrees)
        {
            ApplyLocal(part, Vector3.zero, Quaternion.Euler(0f, 0f, degrees), Vector3.one);
        }

        private void ApplyLocal(Transform part, Vector3 positionOffset, Quaternion rotationOffset, Vector3 scaleMultiplier)
        {
            int index = IndexOf(part);
            if (index < 0) return;

            part.localPosition = Vector3.Lerp(part.localPosition, _basePositions[index] + positionOffset, Time.deltaTime * 18f);
            part.localRotation = Quaternion.Slerp(part.localRotation, _baseRotations[index] * rotationOffset, Time.deltaTime * 18f);
            part.localScale = Vector3.Lerp(part.localScale, Vector3.Scale(_baseScales[index], scaleMultiplier), Time.deltaTime * 18f);
        }

        private int IndexOf(Transform part)
        {
            if (part == null || _parts == null) return -1;

            for (int i = 0; i < _parts.Length; i++)
                if (_parts[i] == part)
                    return i;

            return -1;
        }

        private Transform FindPart(params string[] aliases)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                string normalized = NormalizeName(children[i].name);
                for (int aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
                    if (normalized == NormalizeName(aliases[aliasIndex]))
                        return children[i];
            }

            return null;
        }

        private static string NormalizeName(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
        }
    }
}
