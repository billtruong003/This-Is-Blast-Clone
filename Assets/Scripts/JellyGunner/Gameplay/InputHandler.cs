using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField, Required] private Camera _mainCamera;
        [SerializeField, Required] private SupplyLineManager _supply;
        [SerializeField, Required] private HammerPowerUp _hammer;
        [SerializeField] private LayerMask _supplyLayer;
        [SerializeField] private float _dragThreshold = 15f;

        private bool _isPressed;
        private Vector2 _pressStartPos;
        private bool _isDraggingHammer;

        private void Update()
        {
            HandleTouchOrMouse();
        }

        private void HandleTouchOrMouse()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnPressDown(touch.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        OnPressDrag(touch.position);
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        OnPressUp(touch.position);
                        break;
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
                OnPressDown(Input.mousePosition);

            if (Input.GetMouseButton(0))
                OnPressDrag(Input.mousePosition);

            if (Input.GetMouseButtonUp(0))
                OnPressUp(Input.mousePosition);
        }

        private void OnPressDown(Vector2 screenPos)
        {
            _isPressed = true;
            _pressStartPos = screenPos;
            _isDraggingHammer = false;
        }

        private void OnPressDrag(Vector2 screenPos)
        {
            if (_isDraggingHammer)
                _hammer.UpdateDrag(screenPos);
        }

        private void OnPressUp(Vector2 screenPos)
        {
            if (_isDraggingHammer)
            {
                _hammer.EndDrag();
                _isDraggingHammer = false;
                _isPressed = false;
                return;
            }

            if (!_isPressed) return;
            _isPressed = false;

            float dragDist = Vector2.Distance(_pressStartPos, screenPos);
            if (dragDist > _dragThreshold) return;

            TrySelectSupplyBlock(screenPos);
        }

        private void TrySelectSupplyBlock(Vector2 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, _supplyLayer)) return;

            var blaster = hit.collider.GetComponentInParent<Blaster>();
            if (blaster == null || blaster.State != BlasterState.InSupply) return;

            int displayIndex = _supply.ResolveDisplayIndex(blaster);
            if (displayIndex >= 0)
                _supply.OnBlockClicked(displayIndex);
        }

        public void ActivateHammerMode()
        {
            if (!_hammer.HasCharge) return;
            _isDraggingHammer = true;
            _hammer.BeginDrag();
        }

        public void CancelHammerMode()
        {
            if (!_isDraggingHammer) return;
            _isDraggingHammer = false;
            _hammer.CancelDrag();
        }
    }
}
