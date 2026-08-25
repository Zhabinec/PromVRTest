using PromVR.MaterialAccumulation.Core;
using UnityEngine;

namespace PromVR.MaterialAccumulation.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class AccumulationSurfaceBehaviour : MonoBehaviour
    {
        [Header("Surface")]
        [SerializeField, Min(0.1f), Tooltip("Surface width in local metres.")]
        private float _sizeX = 12f;

        [SerializeField, Min(0.1f), Tooltip("Surface depth in local metres.")]
        private float _sizeZ = 12f;

        [SerializeField, Range(4, 512), Tooltip("Number of quads along the local X axis.")]
        private int _resolutionX = 128;

        [SerializeField, Range(4, 512), Tooltip("Number of quads along the local Z axis.")]
        private int _resolutionZ = 128;

        [Header("Accumulation")]
        [SerializeField, Min(0f), Tooltip("Vertical accumulation speed in metres per second.")]
        private float _accumulationSpeed = 0.6f;

        [SerializeField, Min(0.01f), Tooltip("Initial conservative Y bound for the runtime mesh.")]
        private float _initialMaximumHeight = 2f;

        private HeightField _heightField;
        private HemisphereAccumulator _accumulator;
        private HeightFieldMeshView _meshView;
        private bool _isInitialized;

        public Vector2 Size => new Vector2(_sizeX, _sizeZ);

        public float MaximumRadiusInsideSurface => Mathf.Min(_sizeX, _sizeZ) * 0.5f;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            _meshView?.Dispose();
            _meshView = null;
            _accumulator = null;
            _heightField = null;
            _isInitialized = false;
        }

        private void OnValidate()
        {
            _sizeX = Mathf.Max(0.1f, _sizeX);
            _sizeZ = Mathf.Max(0.1f, _sizeZ);
            _resolutionX = Mathf.Clamp(_resolutionX, 4, 512);
            _resolutionZ = Mathf.Clamp(_resolutionZ, 4, 512);
            _accumulationSpeed = Mathf.Max(0f, _accumulationSpeed);
            _initialMaximumHeight = Mathf.Max(0.01f, _initialMaximumHeight);
        }

        public void ApplySweep(in Sweep sweep)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            GridRect dirty = _accumulator.Apply(sweep, _accumulationSpeed);
            _meshView.Sync(dirty);
        }

        public void ResetSurface()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            GridRect dirty = _heightField.Reset();
            _meshView.Sync(dirty);
        }

        public void SetMaximumExpectedHeight(float maximumHeight)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _meshView.SetMaximumHeight(maximumHeight);
        }

        public void ClampCenter(ref float localX, ref float localZ, float radius)
        {
            float availableX = Mathf.Max(0f, (_sizeX * 0.5f) - radius);
            float availableZ = Mathf.Max(0f, (_sizeZ * 0.5f) - radius);
            localX = Mathf.Clamp(localX, -availableX, availableX);
            localZ = Mathf.Clamp(localZ, -availableZ, availableZ);
        }

        public Vector3 LocalToWorldPoint(float localX, float localY, float localZ)
        {
            return transform.TransformPoint(localX, localY, localZ);
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            GridDescriptor descriptor = new GridDescriptor(
                _resolutionX,
                _resolutionZ,
                _sizeX,
                _sizeZ);
            _heightField = new HeightField(descriptor);
            _accumulator = new HemisphereAccumulator(_heightField);
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _meshView = new HeightFieldMeshView(meshFilter, _heightField, _initialMaximumHeight);
            _isInitialized = true;

            Vector3 scale = transform.lossyScale;
            if (!Mathf.Approximately(scale.x, 1f) ||
                !Mathf.Approximately(scale.y, 1f) ||
                !Mathf.Approximately(scale.z, 1f))
            {
                Debug.LogWarning(
                    "Material accumulation expects a unit surface scale so brush radii stay metric.",
                    this);
            }
        }
    }
}
