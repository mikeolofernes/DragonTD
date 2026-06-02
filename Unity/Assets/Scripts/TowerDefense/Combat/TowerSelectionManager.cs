using UnityEngine;
using UnityEngine.EventSystems;
using DragonTD.Core;
using DragonTD.Dragons;

namespace DragonTD.TowerDefense
{
    public class TowerSelectionManager : MonoBehaviour
    {
        public static TowerSelectionManager Instance { get; private set; }

        private DragonTower _selectedTower;
        private bool _isTargetingSkill;
        private bool _isTargetingMerge;
        private LineRenderer _rangePreview;
        private LineRenderer _skillPreview;
        private LineRenderer _mergePreview;
        private DragonTower _pendingFusionTarget;

        public DragonTower SelectedTower => _selectedTower;
        public bool IsTargetingSkill => _isTargetingSkill;
        public bool IsTargetingMerge => _isTargetingMerge;

        public event System.Action<DragonTower> OnSelectedTowerChanged;
        public event System.Action<bool> OnTargetingChanged;

        public static TowerSelectionManager Ensure()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("TowerSelectionManager");
            return go.AddComponent<TowerSelectionManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isTargetingSkill || _isTargetingMerge)
                    CancelActiveTargeting();
                else if (_selectedTower != null)
                    ClearSelection();
                return;
            }

            if (Input.touchCount >= 2 && (_isTargetingSkill || _isTargetingMerge))
            {
                CancelActiveTargeting();
                return;
            }

            if (!_isTargetingSkill && !_isTargetingMerge) return;

            if (_selectedTower == null)
            {
                CancelActiveTargeting();
                return;
            }

            Vector3 pointer = GetPointerWorldPosition();
            if (_isTargetingSkill)
                UpdateSkillPreview(pointer);
            if (_isTargetingMerge)
                UpdateMergePreview(pointer);

            if (Input.GetMouseButtonDown(1))
            {
                CancelActiveTargeting();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUi()) return;
                if (_isTargetingSkill)
                    TryCastAt(pointer);
                else if (_isTargetingMerge)
                    TryMergeAt(pointer);
            }
        }

        public void SelectTower(DragonTower tower)
        {
            if (tower == null) return;

            _selectedTower = tower;
            CancelActiveTargeting(false);
            DrawRangePreview();
            OnSelectedTowerChanged?.Invoke(_selectedTower);
            GameManager.Instance?.ShowBattleMessage($"{tower.DisplayName} selected");
        }

        public void HandleTowerClicked(DragonTower tower)
        {
            if (tower == null) return;

            if (_isTargetingMerge && _selectedTower != null)
            {
                TryMergeWithTower(tower);
                return;
            }

            SelectTower(tower);
        }

        public void UpgradeSelectedTower()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Upgrade between waves");
                return;
            }

            if (_selectedTower == null)
            {
                GameManager.Instance?.ShowBattleMessage("Select a dragon tower first");
                return;
            }

            _selectedTower.TryUpgrade();
            OnSelectedTowerChanged?.Invoke(_selectedTower);
        }

        public void SellSelectedTower()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Sell between waves");
                return;
            }

            if (_selectedTower == null)
            {
                GameManager.Instance?.ShowBattleMessage("Select a dragon tower first");
                return;
            }

            DragonTower tower = _selectedTower;
            ClearSelection();
            tower.Sell();
        }

        public void BeginSkillTargeting()
        {
            if (_selectedTower == null)
            {
                GameManager.Instance?.ShowBattleMessage("Select a dragon tower first");
                return;
            }

            SkillDefinition skill = _selectedTower.ActiveSkill;
            if (skill == null)
            {
                GameManager.Instance?.ShowBattleMessage($"{_selectedTower.DisplayName} has no active skill");
                return;
            }

            if (!_selectedTower.CanCastActiveSkill(out string reason))
            {
                GameManager.Instance?.ShowBattleMessage(reason);
                return;
            }

            if ((skill.skillId ?? string.Empty).ToLowerInvariant().Contains("fortify"))
            {
                _selectedTower.TryCastActiveSkill(null);
                OnSelectedTowerChanged?.Invoke(_selectedTower);
                return;
            }

            _isTargetingSkill = true;
            _isTargetingMerge = false;
            _pendingFusionTarget = null;
            EnsureSkillPreview();
            UpdateSkillPreview(GetPointerWorldPosition());
            OnTargetingChanged?.Invoke(true);
            GameManager.Instance?.ShowBattleMessage($"Target {skill.displayName}");
        }

        public void BeginMergeTargeting()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlanningPhase)
            {
                GameManager.Instance.ShowBattleMessage("Fuse between waves");
                return;
            }

            if (_selectedTower == null)
            {
                GameManager.Instance?.ShowBattleMessage("Select a dragon tower first");
                return;
            }

            if (_selectedTower.IsFused)
            {
                GameManager.Instance?.ShowBattleMessage($"{_selectedTower.DisplayName} is already fused");
                return;
            }

            if (!_selectedTower.IsReadyToFuse)
            {
                GameManager.Instance?.ShowBattleMessage($"Upgrade {_selectedTower.DisplayName} to Lv3 before fusing");
                return;
            }

            if (!HasMergeCandidate(_selectedTower))
            {
                GameManager.Instance?.ShowBattleMessage("Need another Lv3 dragon to fuse");
                return;
            }

            _isTargetingMerge = true;
            _isTargetingSkill = false;
            _pendingFusionTarget = null;
            if (_skillPreview != null)
                _skillPreview.enabled = false;
            EnsureMergePreview();
            UpdateMergePreview(GetPointerWorldPosition());
            OnTargetingChanged?.Invoke(true);
            GameManager.Instance?.ShowBattleMessage("Pick another Lv3 dragon to fuse");
        }

        public bool HasMergeCandidate(DragonTower tower)
        {
            if (tower == null || !tower.IsReadyToFuse || tower.IsFused) return false;

            foreach (DragonTower other in FindObjectsByType<DragonTower>(FindObjectsSortMode.None))
            {
                if (other == null || other == tower) continue;
                if (tower.CanMergeWith(other, out _))
                    return true;
            }

            return false;
        }

        public void CancelSkillTargeting()
        {
            CancelActiveTargeting(true);
        }

        public void ClearSelection()
        {
            CancelActiveTargeting();
            _selectedTower = null;
            if (_rangePreview != null)
                _rangePreview.enabled = false;
            OnSelectedTowerChanged?.Invoke(null);
        }

        private void CancelActiveTargeting(bool notify = true)
        {
            _isTargetingSkill = false;
            _isTargetingMerge = false;
            _pendingFusionTarget = null;
            if (_skillPreview != null)
                _skillPreview.enabled = false;
            if (_mergePreview != null)
                _mergePreview.enabled = false;
            if (notify)
                OnTargetingChanged?.Invoke(false);
        }

        private void TryCastAt(Vector3 point)
        {
            EnemyBase target = FindEnemyNear(point);
            if (target == null)
            {
                GameManager.Instance?.ShowBattleMessage("Pick an enemy target");
                return;
            }

            if (_selectedTower.TryCastActiveSkill(target))
            {
                CancelSkillTargeting();
                OnSelectedTowerChanged?.Invoke(_selectedTower);
            }
        }

        private void TryMergeAt(Vector3 point)
        {
            DragonTower target = FindTowerNear(point);
            if (target == null)
            {
                GameManager.Instance?.ShowBattleMessage("Pick another Lv3 dragon tower");
                return;
            }

            TryMergeWithTower(target);
        }

        private void TryMergeWithTower(DragonTower target)
        {
            if (_selectedTower == null || target == null) return;
            if (!_selectedTower.CanMergeWith(target, out string reason))
            {
                _pendingFusionTarget = null;
                GameManager.Instance?.ShowBattleMessage(reason);
                return;
            }

            if (_pendingFusionTarget != target)
            {
                _pendingFusionTarget = target;
                GameManager.Instance?.ShowBattleMessage(_selectedTower.BuildFusionPreview(target));
                return;
            }

            if (_selectedTower.TryMergeWith(target))
            {
                _pendingFusionTarget = null;
                CancelActiveTargeting();
                DrawRangePreview();
                OnSelectedTowerChanged?.Invoke(_selectedTower);
            }
        }

        private EnemyBase FindEnemyNear(Vector3 point)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(point, 0.6f);
            EnemyBase nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                EnemyBase enemy = hit.GetComponent<EnemyBase>();
                if (enemy == null || enemy.IsDead) continue;

                float distance = Vector3.Distance(point, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }

        private DragonTower FindTowerNear(Vector3 point)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(point, 0.75f);
            DragonTower nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D hit in hits)
            {
                DragonTower tower = hit.GetComponent<DragonTower>();
                if (tower == null || tower == _selectedTower) continue;

                float distance = Vector3.Distance(point, tower.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = tower;
                }
            }

            return nearest;
        }

        private void DrawRangePreview()
        {
            if (_selectedTower == null) return;

            EnsureRangePreview();
            DrawRing(_rangePreview, _selectedTower.transform.position, _selectedTower.AttackRange, _selectedTower.ProjectileColor, 0.06f);
            _rangePreview.enabled = true;
        }

        private void UpdateSkillPreview(Vector3 pointer)
        {
            if (_selectedTower == null || _selectedTower.ActiveSkill == null) return;

            SkillDefinition skill = _selectedTower.ActiveSkill;
            float radius = skill.isAoe ? skill.aoeRadius : 0.45f;
            DrawRing(_skillPreview, pointer, radius, _selectedTower.ProjectileColor, 0.045f);
            _skillPreview.enabled = true;
        }

        private void UpdateMergePreview(Vector3 pointer)
        {
            if (_selectedTower == null) return;

            DrawRing(_mergePreview, pointer, 0.75f, _selectedTower.ProjectileColor, 0.055f);
            _mergePreview.enabled = true;
        }

        private void EnsureRangePreview()
        {
            if (_rangePreview != null) return;
            _rangePreview = CreateRing("SelectedTowerRangePreview", 21);
        }

        private void EnsureSkillPreview()
        {
            if (_skillPreview != null) return;
            _skillPreview = CreateRing("SkillTargetPreview", 22);
        }

        private void EnsureMergePreview()
        {
            if (_mergePreview != null) return;
            _mergePreview = CreateRing("MergeTargetPreview", 23);
        }

        private LineRenderer CreateRing(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var line = go.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = true;
            line.positionCount = 72;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = sortingOrder;
            line.enabled = false;
            return line;
        }

        private void DrawRing(LineRenderer line, Vector3 center, float radius, Color color, float width)
        {
            color.a = 0.85f;
            center.z = 0f;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;

            for (int i = 0; i < line.positionCount; i++)
            {
                float radians = i / (float)line.positionCount * Mathf.PI * 2f;
                Vector3 point = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * radius;
                line.SetPosition(i, point);
            }
        }

        private Vector3 GetPointerWorldPosition()
        {
            Camera cam = Camera.main;
            if (cam == null) return Vector3.zero;

            Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
            mouse.z = 0f;
            return mouse;
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
