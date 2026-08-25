using PromVR.MaterialAccumulation.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace PromVR.MaterialAccumulation.Presentation
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class DemoHudBehaviour : MonoBehaviour
    {
        private const string ReadyLabel = "READY";
        private const string AccumulatingLabel = "ACCUMULATING";

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
        private Graphic _holdPrompt;

        [Header("Palette")]
        [SerializeField]
        private Color _idleColor = new Color(0.12f, 0.78f, 0.95f, 1f);

        [SerializeField]
        private Color _activeColor = new Color(1f, 0.42f, 0.08f, 1f);

        [SerializeField]
        private Color _mutedPromptColor = new Color(0.72f, 0.79f, 0.88f, 0.72f);

        private bool _hasState;
        private bool _previousAccumulationState;

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
            ApplyAccumulationState(_controller.IsAccumulating);
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
            if (_radiusFill != null)
            {
                _radiusFill.raycastTarget = false;
            }

            if (_stateIndicator != null)
            {
                _stateIndicator.raycastTarget = false;
            }

            if (_stateLabel != null)
            {
                _stateLabel.raycastTarget = false;
            }

            if (_holdPrompt != null)
            {
                _holdPrompt.raycastTarget = false;
            }
        }

        private bool HasRequiredReferences()
        {
            return _controller != null &&
                   _radiusFill != null &&
                   _stateIndicator != null &&
                   _stateLabel != null &&
                   _holdPrompt != null;
        }

        private void ApplyAccumulationState(bool isAccumulating)
        {
            _hasState = true;
            _previousAccumulationState = isAccumulating;
            Color stateColor = isAccumulating ? _activeColor : _idleColor;

            _stateIndicator.color = stateColor;
            _radiusFill.color = stateColor;
            _stateLabel.color = stateColor;
            _stateLabel.text = isAccumulating ? AccumulatingLabel : ReadyLabel;
            _holdPrompt.color = isAccumulating ? _activeColor : _mutedPromptColor;
        }
    }
}
