using UnityEngine;
using UnityEngine.UI;

namespace DragonTD.TowerDefense
{
    // World-space HP bar. Add to a child Canvas on the enemy prefab.
    [RequireComponent(typeof(Canvas))]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;

        private EnemyBase _enemy;
        private Canvas _canvas;

        public void Initialize(EnemyBase enemy)
        {
            _enemy = enemy;
            _enemy.OnHpChanged += SetFill;
            _canvas = GetComponent<Canvas>();
            _canvas.worldCamera = Camera.main;
            SetFill(1f);
        }

        private void LateUpdate()
        {
            if (_canvas != null && _canvas.worldCamera == null)
                _canvas.worldCamera = Camera.main;
        }

        private void SetFill(float percent)
        {
            if (_fillImage != null)
                _fillImage.fillAmount = Mathf.Clamp01(percent);
        }

        private void OnDestroy()
        {
            if (_enemy != null) _enemy.OnHpChanged -= SetFill;
        }
    }
}
