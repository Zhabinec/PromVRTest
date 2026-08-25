using PromVR.MaterialAccumulation.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace PromVR.MaterialAccumulation.Presentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class DemoHudBehaviour : MonoBehaviour
    {
        private const string LanguagePreferenceKey = "MaterialAccumulation.HudLanguage";

        private enum HudLanguage
        {
            English,
            Russian
        }

        [Header("References")]
        [SerializeField]
        private BrushControllerBehaviour _controller;

        [SerializeField]
        private Image _radiusFill;

        [SerializeField]
        private Image _stateIndicator;

        [SerializeField]
        private Text _stateLabel;

        [SerializeField]
        private Text _holdPrompt;

        [Header("Localization")]
        [SerializeField]
        private Button _languageButton;

        [SerializeField]
        private Text _languageLabel;

        [SerializeField]
        private Text _titleLabel;

        [SerializeField]
        private Text _subtitleLabel;

        [SerializeField]
        private Text _statusCaption;

        [SerializeField]
        private Text _radiusCaption;

        [SerializeField]
        private Text _moveActionLabel;

        [SerializeField]
        private Text _depositActionLabel;

        [SerializeField]
        private Text _resetActionLabel;

        [SerializeField]
        private Text _quitActionLabel;

        [Header("Palette")]
        [SerializeField]
        private Color _idleColor = new Color(0.12f, 0.78f, 0.95f, 1f);

        [SerializeField]
        private Color _activeColor = new Color(1f, 0.42f, 0.08f, 1f);

        [SerializeField]
        private Color _mutedPromptColor = new Color(0.72f, 0.79f, 0.88f, 0.72f);

        private bool _hasState;
        private bool _previousAccumulationState;
        private HudLanguage _language;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("Demo HUD requires explicit controller and graphic references.", this);
                enabled = false;
                return;
            }

            _radiusFill.type = Image.Type.Filled;
            _radiusFill.fillMethod = Image.FillMethod.Horizontal;
            _radiusFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _radiusFill.fillAmount = _controller.NormalizedRadius;

            _language = LoadLanguage();
            _languageButton.onClick.AddListener(ToggleLanguage);
            ApplyLanguage();
        }

        private void OnDestroy()
        {
            if (_languageButton != null)
            {
                _languageButton.onClick.RemoveListener(ToggleLanguage);
            }
        }

        private void LateUpdate()
        {
            _radiusFill.fillAmount = _controller.NormalizedRadius;

            bool isAccumulating = _controller.IsAccumulating;
            if (!_hasState || isAccumulating != _previousAccumulationState)
            {
                ApplyAccumulationState(isAccumulating);
            }
        }

        private void OnValidate()
        {
            DisableRaycast(_radiusFill);
            DisableRaycast(_stateIndicator);
            DisableRaycast(_stateLabel);
            DisableRaycast(_holdPrompt);
            DisableRaycast(_languageLabel);
            DisableRaycast(_titleLabel);
            DisableRaycast(_subtitleLabel);
            DisableRaycast(_statusCaption);
            DisableRaycast(_radiusCaption);
            DisableRaycast(_moveActionLabel);
            DisableRaycast(_depositActionLabel);
            DisableRaycast(_resetActionLabel);
            DisableRaycast(_quitActionLabel);
        }

        private bool HasRequiredReferences()
        {
            return _controller != null &&
                   _radiusFill != null &&
                   _stateIndicator != null &&
                   _stateLabel != null &&
                   _holdPrompt != null &&
                   _languageButton != null &&
                   _languageLabel != null &&
                   _titleLabel != null &&
                   _subtitleLabel != null &&
                   _statusCaption != null &&
                   _radiusCaption != null &&
                   _moveActionLabel != null &&
                   _depositActionLabel != null &&
                   _resetActionLabel != null &&
                   _quitActionLabel != null;
        }

        private static void DisableRaycast(Graphic graphic)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }
        }

        private static HudLanguage LoadLanguage()
        {
            HudLanguage fallback = Application.systemLanguage == SystemLanguage.Russian
                ? HudLanguage.Russian
                : HudLanguage.English;
            int savedValue = PlayerPrefs.GetInt(LanguagePreferenceKey, (int)fallback);
            return savedValue == (int)HudLanguage.Russian
                ? HudLanguage.Russian
                : HudLanguage.English;
        }

        private void ToggleLanguage()
        {
            _language = _language == HudLanguage.English
                ? HudLanguage.Russian
                : HudLanguage.English;
            PlayerPrefs.SetInt(LanguagePreferenceKey, (int)_language);
            PlayerPrefs.Save();
            ApplyLanguage();
        }

        private void ApplyLanguage()
        {
            bool isRussian = _language == HudLanguage.Russian;

            _languageLabel.text = isRussian ? "RU" : "EN";
            _titleLabel.text = isRussian ? "НАКОПЛЕНИЕ МАТЕРИАЛА" : "MATERIAL ACCUMULATION";
            _subtitleLabel.text = isRussian
                ? "CPU-карта высот • непрерывный объёмный след"
                : "Persistent CPU height field • swept hemisphere";
            _holdPrompt.text = isRussian
                ? "УДЕРЖИВАЙТЕ ПРОБЕЛ: НАКОПЛЕНИЕ"
                : "HOLD SPACE TO ACCUMULATE";
            _statusCaption.text = isRussian ? "СОСТОЯНИЕ КИСТИ" : "BRUSH STATUS";
            _radiusCaption.text = isRussian ? "АНИМИРОВАННЫЙ РАДИУС" : "ANIMATED RADIUS";
            _moveActionLabel.text = isRussian ? "ДВИЖЕНИЕ" : "MOVE";
            _depositActionLabel.text = isRussian ? "НАКОПЛЕНИЕ" : "DEPOSIT";
            _resetActionLabel.text = isRussian ? "СБРОС" : "CLEAR";
            _quitActionLabel.text = isRussian ? "ВЫХОД" : "QUIT BUILD";

            ApplyAccumulationState(_controller.IsAccumulating);
        }

        private void ApplyAccumulationState(bool isAccumulating)
        {
            _hasState = true;
            _previousAccumulationState = isAccumulating;
            Color stateColor = isAccumulating ? _activeColor : _idleColor;

            _stateIndicator.color = stateColor;
            _radiusFill.color = stateColor;
            _stateLabel.color = stateColor;
            _stateLabel.text = GetStateLabel(isAccumulating);
            _holdPrompt.color = isAccumulating ? _activeColor : _mutedPromptColor;
        }

        private string GetStateLabel(bool isAccumulating)
        {
            if (_language == HudLanguage.Russian)
            {
                return isAccumulating ? "НАКОПЛЕНИЕ" : "ГОТОВО";
            }

            return isAccumulating ? "ACCUMULATING" : "READY";
        }
    }
}
