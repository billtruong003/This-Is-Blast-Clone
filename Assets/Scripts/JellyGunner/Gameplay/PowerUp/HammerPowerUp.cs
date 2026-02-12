using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class HammerPowerUp : MonoBehaviour
    {
        [SerializeField, Required] private GameConfig _config;
        [SerializeField, Required] private EnemyGridManager _enemyGrid;
        [SerializeField, Required] private Camera _mainCamera;
        [SerializeField] private LayerMask _enemyRaycastLayer;
        [SerializeField] private Transform _hammerVisual;

        private int _charges;
        private bool _isDragging;
        private BlockColor _hoverColor;
        private bool _isHovering;

        public int Charges => _charges;
        public bool HasCharge => _charges > 0;
        public bool IsDragging => _isDragging;

        public void Initialize(int charges)
        {
            _charges = charges;
            if (_hammerVisual) _hammerVisual.gameObject.SetActive(false);
        }

        public void BeginDrag()
        {
            if (!HasCharge) return;
            _isDragging = true;
            if (_hammerVisual) _hammerVisual.gameObject.SetActive(true);
        }

        public void UpdateDrag(Vector2 screenPosition)
        {
            if (!_isDragging) return;

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
            if (_hammerVisual)
            {
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                if (groundPlane.Raycast(ray, out float dist))
                    _hammerVisual.position = ray.GetPoint(dist) + Vector3.up * 0.5f;
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _enemyRaycastLayer))
            {
                BlockColor detectedColor = DetectColorAtPoint(hit.point);
                if (!_isHovering || detectedColor != _hoverColor)
                {
                    if (_isHovering)
                        _enemyGrid.SetHighlightColor(_hoverColor, false);

                    _hoverColor = detectedColor;
                    _isHovering = true;
                    _enemyGrid.SetHighlightColor(_hoverColor, true);

                    GameEvents.Publish(new GameEvents.HammerHoverChanged
                    {
                        HoverColor = _hoverColor,
                        IsHovering = true
                    });
                }
            }
            else if (_isHovering)
            {
                _enemyGrid.SetHighlightColor(_hoverColor, false);
                _isHovering = false;

                GameEvents.Publish(new GameEvents.HammerHoverChanged
                {
                    HoverColor = _hoverColor,
                    IsHovering = false
                });
            }
        }

        public void EndDrag()
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (_isHovering)
            {
                ExecuteStrike(_hoverColor);
                _enemyGrid.SetHighlightColor(_hoverColor, false);
                _isHovering = false;
            }

            if (_hammerVisual) _hammerVisual.gameObject.SetActive(false);
        }

        public void CancelDrag()
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (_isHovering)
            {
                _enemyGrid.SetHighlightColor(_hoverColor, false);
                _isHovering = false;
            }

            if (_hammerVisual) _hammerVisual.gameObject.SetActive(false);
        }

        private void ExecuteStrike(BlockColor targetColor)
        {
            _charges--;
            int killed = _enemyGrid.HammerStrike(targetColor);

            GameEvents.Publish(new GameEvents.HammerActivated
            {
                TargetColor = targetColor,
                EnemiesKilled = killed
            });
        }

        private BlockColor DetectColorAtPoint(Vector3 worldPoint)
        {
            float bestDist = float.MaxValue;
            BlockColor bestColor = BlockColor.Red;

            for (int c = 0; c < (int)BlockColor.Count; c++)
            {
                var color = (BlockColor)c;
                int idx = _enemyGrid.FindBottomMostByColor(color, worldPoint);
                if (idx < 0) continue;

                float dist = (worldPoint - _enemyGrid.GetPosition(idx)).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestColor = color;
                }
            }

            return bestColor;
        }
    }
}
