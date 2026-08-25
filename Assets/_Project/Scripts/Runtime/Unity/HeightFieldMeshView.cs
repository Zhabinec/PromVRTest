using System;
using System.Runtime.InteropServices;
using PromVR.MaterialAccumulation.Core;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PromVR.MaterialAccumulation.Unity
{
    internal sealed class HeightFieldMeshView : IDisposable
    {
        private const int NormalDependencyBorder = 1;

        private static readonly ProfilerMarker SyncMarker =
            new ProfilerMarker("MaterialAccumulation.MeshSync");

        private static readonly MeshUpdateFlags UploadFlags =
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontValidateIndices |
            MeshUpdateFlags.DontNotifyMeshUsers;

        private readonly MeshFilter _meshFilter;
        private readonly HeightField _heightField;
        private readonly GridDescriptor _grid;
        private readonly SurfaceVertex[] _vertices;
        private readonly Mesh _mesh;

        private float _maximumHeight;
        private bool _isDisposed;

        public HeightFieldMeshView(
            MeshFilter meshFilter,
            HeightField heightField,
            float maximumHeight)
        {
            _meshFilter = meshFilter != null
                ? meshFilter
                : throw new ArgumentNullException(nameof(meshFilter));
            _heightField = heightField ?? throw new ArgumentNullException(nameof(heightField));
            _grid = heightField.Descriptor;
            _vertices = new SurfaceVertex[_grid.HeightCount];
            _maximumHeight = Mathf.Max(0.01f, maximumHeight);

            InitializeStaticVertexData();
            _mesh = CreateMesh();
            _meshFilter.sharedMesh = _mesh;
            Sync(GridRect.Full(_grid));
        }

        public void Sync(in GridRect dirty)
        {
            if (_isDisposed || dirty.IsEmpty)
            {
                return;
            }

            using (SyncMarker.Auto())
            {
                GridRect uploadRect = dirty.Expand(
                    NormalDependencyBorder,
                    _grid.VertexCountX - 1,
                    _grid.VertexCountZ - 1);

                UpdateVertexData(uploadRect);

                for (int z = uploadRect.MinZ; z <= uploadRect.MaxZ; z++)
                {
                    int rowStart = _grid.GetIndex(uploadRect.MinX, z);
                    _mesh.SetVertexBufferData(
                        _vertices,
                        rowStart,
                        rowStart,
                        uploadRect.Width,
                        0,
                        UploadFlags);
                }
            }
        }

        public void SetMaximumHeight(float maximumHeight)
        {
            float clampedHeight = Mathf.Max(0.01f, maximumHeight);
            if (clampedHeight <= _maximumHeight)
            {
                return;
            }

            _maximumHeight = clampedHeight;
            UpdateBounds();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_meshFilter != null && _meshFilter.sharedMesh == _mesh)
            {
                _meshFilter.sharedMesh = null;
            }

            if (_mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(_mesh);
            }
            else
            {
                Object.DestroyImmediate(_mesh);
            }
        }

        private Mesh CreateMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "Material Accumulation Surface (Runtime)",
                hideFlags = HideFlags.DontSave,
                indexFormat = _grid.HeightCount > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            mesh.MarkDynamic();
            mesh.SetVertexBufferParams(
                _vertices.Length,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2));
            mesh.SetVertexBufferData(_vertices, 0, 0, _vertices.Length, 0, UploadFlags);
            mesh.SetIndices(CreateTriangleIndices(), MeshTopology.Triangles, 0, false);
            UpdateBounds(mesh);
            return mesh;
        }

        private int[] CreateTriangleIndices()
        {
            int[] indices = new int[_grid.QuadsX * _grid.QuadsZ * 6];
            int cursor = 0;

            for (int z = 0; z < _grid.QuadsZ; z++)
            {
                for (int x = 0; x < _grid.QuadsX; x++)
                {
                    int bottomLeft = _grid.GetIndex(x, z);
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + _grid.VertexCountX;
                    int topRight = topLeft + 1;

                    indices[cursor++] = bottomLeft;
                    indices[cursor++] = topLeft;
                    indices[cursor++] = topRight;
                    indices[cursor++] = bottomLeft;
                    indices[cursor++] = topRight;
                    indices[cursor++] = bottomRight;
                }
            }

            return indices;
        }

        private void InitializeStaticVertexData()
        {
            float inverseQuadsX = 1f / _grid.QuadsX;
            float inverseQuadsZ = 1f / _grid.QuadsZ;

            for (int z = 0; z < _grid.VertexCountZ; z++)
            {
                for (int x = 0; x < _grid.VertexCountX; x++)
                {
                    int index = _grid.GetIndex(x, z);
                    _vertices[index].Uv = new Vector2(x * inverseQuadsX, z * inverseQuadsZ);
                    _vertices[index].Normal = Vector3.up;
                }
            }
        }

        private void UpdateVertexData(in GridRect updateRect)
        {
            for (int z = updateRect.MinZ; z <= updateRect.MaxZ; z++)
            {
                for (int x = updateRect.MinX; x <= updateRect.MaxX; x++)
                {
                    int index = _grid.GetIndex(x, z);
                    _vertices[index].Position = new Vector3(
                        _grid.MinX + (x * _grid.CellSizeX),
                        _heightField.GetHeightByIndex(index),
                        _grid.MinZ + (z * _grid.CellSizeZ));
                    _vertices[index].Normal = CalculateNormal(x, z);
                }
            }
        }

        private Vector3 CalculateNormal(int x, int z)
        {
            int leftX = Mathf.Max(0, x - 1);
            int rightX = Mathf.Min(_grid.VertexCountX - 1, x + 1);
            int bottomZ = Mathf.Max(0, z - 1);
            int topZ = Mathf.Min(_grid.VertexCountZ - 1, z + 1);

            float horizontalDistance = (rightX - leftX) * _grid.CellSizeX;
            float verticalDistance = (topZ - bottomZ) * _grid.CellSizeZ;
            float horizontalSlope = (
                _heightField.GetHeight(rightX, z) - _heightField.GetHeight(leftX, z)) /
                horizontalDistance;
            float verticalSlope = (
                _heightField.GetHeight(x, topZ) - _heightField.GetHeight(x, bottomZ)) /
                verticalDistance;

            Vector3 normal = new Vector3(-horizontalSlope, 1f, -verticalSlope);
            normal.Normalize();
            return normal;
        }

        private void UpdateBounds()
        {
            UpdateBounds(_mesh);
        }

        private void UpdateBounds(Mesh mesh)
        {
            mesh.bounds = new Bounds(
                new Vector3(0f, _maximumHeight * 0.5f, 0f),
                new Vector3(_grid.SizeX, _maximumHeight, _grid.SizeZ));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SurfaceVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Uv;
        }
    }
}
