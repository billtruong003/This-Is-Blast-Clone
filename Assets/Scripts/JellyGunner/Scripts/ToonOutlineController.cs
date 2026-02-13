using UnityEngine;

namespace JellyGunner.Rendering
{
    [RequireComponent(typeof(Renderer))]
    public class ToonOutlineController : MonoBehaviour
    {
        [Header("Outline Settings")]
        [SerializeField] private float _defaultWidth = 0.02f;
        [SerializeField] private Color _defaultColor = Color.black;
        [SerializeField] private float _blockIntensity = 2.0f; // HDR Intensity for block color

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private static readonly int _OutlineColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int _OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            // Initialize with default
            ResetDefault();
        }

        /// <summary>
        /// Resets the outline to default black color and width.
        /// </summary>
        public void ResetDefault()
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_OutlineColorID, _defaultColor);
            _propBlock.SetFloat(_OutlineWidthID, _defaultWidth);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Sets a custom color for the outline.
        /// </summary>
        public void SetOutlineColor(Color color)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_OutlineColorID, color);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Sets the outline to a "Block" state (e.g., bright highlight color).
        /// Using HDR intensity boost.
        /// </summary>
        public void SetBlockColor(Color baseColor)
        {
            if (_renderer == null) return;

            // Boost color intensity for HDR glow effect
            Color hdrColor = baseColor * _blockIntensity;
            // Ensure alpha remains 1 (or original)
            hdrColor.a = baseColor.a;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(_OutlineColorID, hdrColor);
            _renderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Dynamically change width if needed.
        /// </summary>
        public void SetWidth(float width)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_OutlineWidthID, width);
            _renderer.SetPropertyBlock(_propBlock);
        }
    }
}