using UnityEngine;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class GameCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 _defaultPosition = new Vector3(0f, 8f, -5f);
        [SerializeField] private Vector3 _defaultRotation = new Vector3(45f, 0f, 0f);
        [SerializeField] private float _shakeDecay = 5f;
        [SerializeField] private float _shakeMaxOffset = 0.15f;

        private Vector3 _shakeOffset;
        private float _shakeIntensity;
        private Vector3 _basePosition;

        private void Awake()
        {
            _basePosition = _defaultPosition;
            transform.position = _basePosition;
            transform.eulerAngles = _defaultRotation;
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Subscribe<GameEvents.HammerActivated>(HandleHammer);
            GameEvents.Subscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.EnemyDied>(HandleEnemyDied);
            GameEvents.Unsubscribe<GameEvents.HammerActivated>(HandleHammer);
            GameEvents.Unsubscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void LateUpdate()
        {
            if (_shakeIntensity > 0.001f)
            {
                _shakeIntensity *= Mathf.Exp(-_shakeDecay * Time.deltaTime);
                _shakeOffset = Random.insideUnitSphere * (_shakeIntensity * _shakeMaxOffset);
            }
            else
            {
                _shakeOffset = Vector3.zero;
                _shakeIntensity = 0f;
            }

            transform.position = _basePosition + _shakeOffset;
        }

        public void Shake(float intensity)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        private void HandleEnemyDied(GameEvents.EnemyDied evt)
        {
            Shake(0.3f);
        }

        private void HandleHammer(GameEvents.HammerActivated evt)
        {
            Shake(1f);
        }

        private void HandleMerge(GameEvents.MergeTriggered evt)
        {
            Shake(0.5f);
        }
    }
}
