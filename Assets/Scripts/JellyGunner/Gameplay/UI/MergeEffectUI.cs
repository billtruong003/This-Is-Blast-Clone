using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class MergeEffectUI : MonoBehaviour
    {
        [SerializeField, Required] private ColorPalette _palette;
        [SerializeField, Required] private Image _flashOverlay;
        [SerializeField] private float _flashDuration = 0.3f;
        [SerializeField] private float _flashMaxAlpha = 0.25f;

        private void Awake()
        {
            _flashOverlay.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.Subscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void OnDisable()
        {
            GameEvents.Unsubscribe<GameEvents.MergeTriggered>(HandleMerge);
        }

        private void HandleMerge(GameEvents.MergeTriggered evt)
        {
            StartCoroutine(PlayFlash(evt.Color));
        }

        private IEnumerator PlayFlash(BlockColor color)
        {
            var flashColor = _palette.GetColor(color);
            flashColor.a = 0f;
            _flashOverlay.color = flashColor;
            _flashOverlay.gameObject.SetActive(true);

            float half = _flashDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                flashColor.a = Mathf.Lerp(0f, _flashMaxAlpha, t);
                _flashOverlay.color = flashColor;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                flashColor.a = Mathf.Lerp(_flashMaxAlpha, 0f, t);
                _flashOverlay.color = flashColor;
                yield return null;
            }

            _flashOverlay.gameObject.SetActive(false);
        }
    }
}
