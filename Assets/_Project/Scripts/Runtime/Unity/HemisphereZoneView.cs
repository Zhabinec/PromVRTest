using UnityEngine;

namespace PromVR.MaterialAccumulation.Unity
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HemisphereZoneView : MonoBehaviour
    {
        private const int AngularSegments = 48;
        private const int RadialRings = 12;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private Color _idleColor = new Color(0.1f, 0.8f, 1f, 0.18f);

        [SerializeField]
        private Color _activeColor = new Color(1f, 0.45f, 0.08f, 0.28f);

        private Mesh _mesh;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private bool _hasColorState;
        private bool _wasActive;

        private void Awake()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _mesh = CreateHemisphereMesh();
            meshFilter.sharedMesh = _mesh;
        }

        private void OnDestroy()
        {
            if (_mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }

            _mesh = null;
        }

        public void SetState(
            Vector3 worldPosition,
            Quaternion worldRotation,
            float radius,
            bool isAccumulating)
        {
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            transform.localScale = Vector3.one * radius;

            if (_hasColorState && _wasActive == isAccumulating)
            {
                return;
            }

            _hasColorState = true;
            _wasActive = isAccumulating;
            Color color = isAccumulating ? _activeColor : _idleColor;
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            _meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        private static Mesh CreateHemisphereMesh()
        {
            int ringVertexCount = RadialRings * AngularSegments;
            int topIndex = ringVertexCount;
            Vector3[] vertices = new Vector3[ringVertexCount + 1];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[((RadialRings - 1) * AngularSegments * 6) + (AngularSegments * 3)];

            for (int ring = 0; ring < RadialRings; ring++)
            {
                float elevation = Mathf.PI * 0.5f * ring / RadialRings;
                float horizontalRadius = Mathf.Cos(elevation);
                float height = Mathf.Sin(elevation);

                for (int segment = 0; segment < AngularSegments; segment++)
                {
                    float angle = Mathf.PI * 2f * segment / AngularSegments;
                    int index = (ring * AngularSegments) + segment;
                    Vector3 vertex = new Vector3(
                        Mathf.Cos(angle) * horizontalRadius,
                        height,
                        Mathf.Sin(angle) * horizontalRadius);
                    vertices[index] = vertex;
                    normals[index] = vertex.normalized;
                    uvs[index] = new Vector2(
                        (float)segment / AngularSegments,
                        (float)ring / RadialRings);
                }
            }

            vertices[topIndex] = Vector3.up;
            normals[topIndex] = Vector3.up;
            uvs[topIndex] = new Vector2(0.5f, 1f);

            int cursor = 0;
            for (int ring = 0; ring < RadialRings - 1; ring++)
            {
                int nextRing = ring + 1;
                for (int segment = 0; segment < AngularSegments; segment++)
                {
                    int nextSegment = (segment + 1) % AngularSegments;
                    int bottomLeft = (ring * AngularSegments) + segment;
                    int bottomRight = (ring * AngularSegments) + nextSegment;
                    int topLeft = (nextRing * AngularSegments) + segment;
                    int topRight = (nextRing * AngularSegments) + nextSegment;

                    triangles[cursor++] = bottomLeft;
                    triangles[cursor++] = topLeft;
                    triangles[cursor++] = topRight;
                    triangles[cursor++] = bottomLeft;
                    triangles[cursor++] = topRight;
                    triangles[cursor++] = bottomRight;
                }
            }

            int lastRingStart = (RadialRings - 1) * AngularSegments;
            for (int segment = 0; segment < AngularSegments; segment++)
            {
                int nextSegment = (segment + 1) % AngularSegments;
                triangles[cursor++] = lastRingStart + segment;
                triangles[cursor++] = topIndex;
                triangles[cursor++] = lastRingStart + nextSegment;
            }

            Mesh mesh = new Mesh
            {
                name = "Hemisphere Zone Preview (Runtime)",
                hideFlags = HideFlags.DontSave,
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
