using PromVR.MaterialAccumulation.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PromVR.MaterialAccumulation.Unity
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public sealed class BrushControllerBehaviour : MonoBehaviour
    {
        private const float MinimumRadius = 0.01f;

        [Header("References")]
        [SerializeField]
        private AccumulationSurfaceBehaviour _surface;

        [SerializeField]
        private HemisphereZoneView _zoneView;

        [Header("Movement")]
        [SerializeField, Min(0f), Tooltip("Brush movement speed in local metres per second.")]
        private float _movementSpeed = 3f;

        [Header("Radius")]
        [SerializeField, Min(MinimumRadius), Tooltip("Middle value of the animated radius in metres.")]
        private float _baseRadius = 1.25f;

        [SerializeField, Min(0f), Tooltip("Maximum deviation from the base radius in metres.")]
        private float _radiusAmplitude = 0.35f;

        [SerializeField, Min(0f), Tooltip("Animation cycles per second.")]
        private float _radiusFrequency = 0.25f;

        [SerializeField, Tooltip("One normalized cycle. Values are clamped to the 0..1 range.")]
        private AnimationCurve _radiusCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.25f, 1f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(0.75f, 0f),
            new Keyframe(1f, 0.5f));

        private double _elapsedSeconds;
        private float _centerX;
        private float _centerZ;
        private float _currentRadius;

        private void Awake()
        {
            if (_surface == null || _zoneView == null)
            {
                Debug.LogError("Brush controller requires explicit Surface and Zone View references.", this);
                enabled = false;
                return;
            }

            ValidateConfiguration();
            _currentRadius = EvaluateRadius();
            _surface.SetMaximumExpectedHeight(_baseRadius + _radiusAmplitude);
            _surface.ClampCenter(ref _centerX, ref _centerZ, _currentRadius);
            UpdateZoneView(false);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float previousX = _centerX;
            float previousZ = _centerZ;
            float previousRadius = _currentRadius;
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                Vector2 movement = ReadMovement(keyboard);
                _centerX += movement.x * _movementSpeed * deltaTime;
                _centerZ += movement.y * _movementSpeed * deltaTime;
            }

            _elapsedSeconds += deltaTime;
            _currentRadius = EvaluateRadius();
            _surface.ClampCenter(ref _centerX, ref _centerZ, _currentRadius);

            bool isAccumulating = keyboard != null && keyboard.spaceKey.isPressed;
            if (isAccumulating && deltaTime > 0f)
            {
                Sweep sweep = new Sweep(
                    previousX,
                    previousZ,
                    _centerX,
                    _centerZ,
                    previousRadius,
                    _currentRadius,
                    deltaTime);
                _surface.ApplySweep(sweep);
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                _surface.ResetSurface();
            }

#if !UNITY_EDITOR
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
#endif

            UpdateZoneView(isAccumulating);
        }

        private void OnValidate()
        {
            ValidateConfiguration();
        }

        private Vector2 ReadMovement(Keyboard keyboard)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                vertical += 1f;
            }

            Vector2 movement = new Vector2(horizontal, vertical);
            return movement.sqrMagnitude > 1f ? movement.normalized : movement;
        }

        private float EvaluateRadius()
        {
            float phase = Mathf.Repeat((float)(_elapsedSeconds * _radiusFrequency), 1f);
            float curveValue = _radiusCurve == null ? 0.5f : Mathf.Clamp01(_radiusCurve.Evaluate(phase));
            return _baseRadius + (_radiusAmplitude * ((2f * curveValue) - 1f));
        }

        private void UpdateZoneView(bool isAccumulating)
        {
            Vector3 worldPosition = _surface.LocalToWorldPoint(_centerX, 0.01f, _centerZ);
            _zoneView.SetState(worldPosition, _surface.transform.rotation, _currentRadius, isAccumulating);
        }

        private void ValidateConfiguration()
        {
            _movementSpeed = Mathf.Max(0f, _movementSpeed);
            _baseRadius = Mathf.Max(MinimumRadius, _baseRadius);
            _radiusAmplitude = Mathf.Clamp(_radiusAmplitude, 0f, _baseRadius - MinimumRadius);
            _radiusFrequency = Mathf.Max(0f, _radiusFrequency);
        }
    }
}
