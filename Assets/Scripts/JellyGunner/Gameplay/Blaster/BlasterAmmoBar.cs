using UnityEngine;

namespace JellyGunner
{
    [RequireComponent(typeof(Blaster))]
    public class BlasterAmmoBar : MonoBehaviour
    {
        [SerializeField] private float _barWidth = 0.8f;
        [SerializeField] private float _barHeight = 0.08f;
        [SerializeField] private float _yOffset = 0.6f;
        [SerializeField] private Color _barBgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
        [SerializeField] private Color _barFillColor = new Color(0.3f, 1f, 0.4f, 1f);
        [SerializeField] private Color _barLowColor = new Color(1f, 0.3f, 0.2f, 1f);
        [SerializeField] private float _lowThreshold = 0.25f;

        private Blaster _blaster;
        private Transform _bgQuad;
        private Transform _fillQuad;
        private Material _fillMat;
        private int _maxAmmo;

        private void Awake()
        {
            _blaster = GetComponent<Blaster>();
            CreateBar();
        }

        private void LateUpdate()
        {
            if (_blaster.State != BlasterState.Active)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            if (_maxAmmo <= 0 && _blaster.Definition != null)
                _maxAmmo = _blaster.CurrentAmmo > 0 ? _blaster.CurrentAmmo : 1;

            float ratio = _maxAmmo > 0 ? (float)_blaster.CurrentAmmo / _maxAmmo : 0f;
            _fillQuad.localScale = new Vector3(_barWidth * ratio, _barHeight, 1f);
            _fillQuad.localPosition = new Vector3(-_barWidth * (1f - ratio) * 0.5f, _yOffset, 0f);

            Color fill = ratio > _lowThreshold ? _barFillColor : _barLowColor;
            _fillMat.color = fill;

            var cam = Camera.main;
            if (cam)
            {
                _bgQuad.rotation = cam.transform.rotation;
                _fillQuad.rotation = cam.transform.rotation;
            }
        }

        public void ResetMaxAmmo(int max)
        {
            _maxAmmo = max;
        }

        private void CreateBar()
        {
            _bgQuad = CreateQuad("AmmoBG", _barBgColor);
            _bgQuad.localScale = new Vector3(_barWidth, _barHeight, 1f);
            _bgQuad.localPosition = new Vector3(0f, _yOffset, 0f);

            _fillQuad = CreateQuad("AmmoFill", _barFillColor);
            _fillQuad.localScale = new Vector3(_barWidth, _barHeight, 1f);
            _fillQuad.localPosition = new Vector3(0f, _yOffset, 0f);

            _fillMat = _fillQuad.GetComponent<Renderer>().material;

            SetVisible(false);
        }

        private Transform CreateQuad(string name, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.localRotation = Quaternion.identity;

            if (go.TryGetComponent<Collider>(out var col))
                Destroy(col);

            var rend = go.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Sprites/Default"));
            rend.material.color = color;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            return go.transform;
        }

        private void SetVisible(bool visible)
        {
            if (_bgQuad) _bgQuad.gameObject.SetActive(visible);
            if (_fillQuad) _fillQuad.gameObject.SetActive(visible);
        }
    }
}
