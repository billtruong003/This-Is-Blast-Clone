using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

namespace JellyGunner
{
    public class HammerButtonUI : MonoBehaviour
    {
        [SerializeField, Required] private InputHandler _input;
        [SerializeField, Required] private HammerPowerUp _hammer;
        [SerializeField, Required] private Button _hammerButton;
        [SerializeField, Required] private Text _chargeCountText;
        [SerializeField] private GameObject _emptyOverlay;

        private void OnEnable()
        {
            _hammerButton.onClick.AddListener(OnHammerClicked);
            GameEvents.Subscribe<GameEvents.HammerActivated>(HandleHammerUsed);
            Refresh();
        }

        private void OnDisable()
        {
            _hammerButton.onClick.RemoveListener(OnHammerClicked);
            GameEvents.Unsubscribe<GameEvents.HammerActivated>(HandleHammerUsed);
        }

        private void OnHammerClicked()
        {
            if (!_hammer.HasCharge) return;
            _input.ActivateHammerMode();
        }

        private void HandleHammerUsed(GameEvents.HammerActivated evt)
        {
            Refresh();
        }

        private void Refresh()
        {
            _chargeCountText.text = _hammer.Charges.ToString();
            _hammerButton.interactable = _hammer.HasCharge;
            if (_emptyOverlay) _emptyOverlay.SetActive(!_hammer.HasCharge);
        }
    }
}
