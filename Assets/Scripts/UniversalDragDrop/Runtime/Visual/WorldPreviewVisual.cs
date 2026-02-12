using UnityEngine;

namespace Universal.DragDrop
{
    /// <summary>
    /// Shows a 3D preview object in the world when dragging from UI onto 3D space.
    /// Perfect for strategy games where you drag a unit card onto the battlefield.
    /// Attach to a UI Draggable and set the worldPreviewPrefab.
    /// </summary>
    public class WorldPreviewVisual : DragVisualBase
    {
        [Header("World Preview")]
        [Tooltip("3D prefab to spawn as preview in the world")]
        [SerializeField] private GameObject _worldPreviewPrefab;

        [Tooltip("Offset from the ground hit point")]
        [SerializeField] private Vector3 _previewOffset = Vector3.zero;

        [Tooltip("Rotation of the preview")]
        [SerializeField] private Vector3 _previewRotation = Vector3.zero;

        [Tooltip("Scale of the preview")]
        [SerializeField] private float _previewScale = 1f;

        [Tooltip("Layer mask for ground detection")]
        [SerializeField] private LayerMask _groundLayer = -1;

        [Tooltip("Color when placement is valid")]
        [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.5f);

        [Tooltip("Color when placement is invalid")]
        [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.5f);

        [Tooltip("Bobbing animation amplitude")]
        [SerializeField] private float _bobAmplitude = 0.1f;

        [Tooltip("Bobbing animation speed")]
        [SerializeField] private float _bobSpeed = 3f;

        private GameObject _previewInstance;
        private Renderer[] _previewRenderers;
        private MaterialPropertyBlock _propBlock;
        private bool _isOverWorld;
        private float _bobTime;

        /// <summary>Set the preview prefab at runtime.</summary>
        public void SetPreviewPrefab(GameObject prefab) => _worldPreviewPrefab = prefab;

        protected override void OnDragVisualStart(DragContext context)
        {
            _isOverWorld = false;
            _bobTime = 0f;
        }

        protected override void OnDragVisualUpdate(DragContext context)
        {
            Camera cam = context.RaycastCamera ?? Camera.main;
            if (cam == null) return;

            // Raycast to check if we're over the world
            Ray ray = cam.ScreenPointToRay(context.CurrentScreenPosition);
            bool hitWorld = Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer);

            if (hitWorld)
            {
                if (!_isOverWorld)
                {
                    // Entering world space — spawn preview
                    SpawnPreview();
                    _isOverWorld = true;
                }

                if (_previewInstance != null)
                {
                    // Update position with bobbing
                    _bobTime += Time.deltaTime * _bobSpeed;
                    float bob = Mathf.Sin(_bobTime) * _bobAmplitude;
                    _previewInstance.transform.position = hit.point + _previewOffset + Vector3.up * bob;
                    _previewInstance.transform.rotation = Quaternion.Euler(_previewRotation);

                    // Update color based on drop zone validity
                    bool isValid = context.HoveredDropZone != null &&
                                   context.HoveredDropZone.CanAccept(context.Draggable, context);
                    SetPreviewColor(isValid ? _validColor : _invalidColor);
                }
            }
            else if (_isOverWorld)
            {
                // Left world space — hide preview
                DestroyPreview();
                _isOverWorld = false;
            }
        }

        protected override void OnDragVisualEnd(DragContext context, DropResult result)
        {
            DestroyPreview();
            _isOverWorld = false;
        }

        protected override void OnCrossSpaceTransition(DragSpace from, DragSpace to, DragContext context)
        {
            if (to == DragSpace.World3D && !_isOverWorld)
            {
                SpawnPreview();
                _isOverWorld = true;
            }
            else if (from == DragSpace.World3D && _isOverWorld)
            {
                DestroyPreview();
                _isOverWorld = false;
            }
        }

        private void SpawnPreview()
        {
            if (_worldPreviewPrefab == null || _previewInstance != null) return;

            _previewInstance = Instantiate(_worldPreviewPrefab);
            _previewInstance.name = "DragPreview_" + gameObject.name;
            _previewInstance.transform.localScale = Vector3.one * _previewScale;

            // Disable colliders on preview
            foreach (var col in _previewInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;

            // Cache renderers
            _previewRenderers = _previewInstance.GetComponentsInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void DestroyPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
                _previewRenderers = null;
            }
        }

        private void SetPreviewColor(Color color)
        {
            if (_previewRenderers == null || _propBlock == null) return;

            _propBlock.SetColor("_Color", color);
            foreach (var r in _previewRenderers)
            {
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
