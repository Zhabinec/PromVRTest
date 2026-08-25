using System;

namespace PromVR.MaterialAccumulation.Core
{
    public readonly struct GridDescriptor
    {
        public GridDescriptor(int quadsX, int quadsZ, float sizeX, float sizeZ)
        {
            if (quadsX < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quadsX));
            }

            if (quadsZ < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(quadsZ));
            }

            if (!IsPositiveFinite(sizeX))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeX));
            }

            if (!IsPositiveFinite(sizeZ))
            {
                throw new ArgumentOutOfRangeException(nameof(sizeZ));
            }

            QuadsX = quadsX;
            QuadsZ = quadsZ;
            SizeX = sizeX;
            SizeZ = sizeZ;
            VertexCountX = quadsX + 1;
            VertexCountZ = quadsZ + 1;
            CellSizeX = sizeX / quadsX;
            CellSizeZ = sizeZ / quadsZ;
            MinX = sizeX * -0.5f;
            MinZ = sizeZ * -0.5f;
        }

        public int QuadsX { get; }

        public int QuadsZ { get; }

        public int VertexCountX { get; }

        public int VertexCountZ { get; }

        public int HeightCount => VertexCountX * VertexCountZ;

        public float SizeX { get; }

        public float SizeZ { get; }

        public float CellSizeX { get; }

        public float CellSizeZ { get; }

        public float MinX { get; }

        public float MinZ { get; }

        public float MaxX => MinX + SizeX;

        public float MaxZ => MinZ + SizeZ;

        public int GetIndex(int x, int z)
        {
            if ((uint)x >= (uint)VertexCountX)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if ((uint)z >= (uint)VertexCountZ)
            {
                throw new ArgumentOutOfRangeException(nameof(z));
            }

            return GetIndexUnchecked(x, z);
        }

        public float GetLocalX(int x)
        {
            if ((uint)x >= (uint)VertexCountX)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            return MinX + (x * CellSizeX);
        }

        public float GetLocalZ(int z)
        {
            if ((uint)z >= (uint)VertexCountZ)
            {
                throw new ArgumentOutOfRangeException(nameof(z));
            }

            return MinZ + (z * CellSizeZ);
        }

        internal int GetIndexUnchecked(int x, int z)
        {
            return (z * VertexCountX) + x;
        }

        internal float GetLocalXUnchecked(int x)
        {
            return MinX + (x * CellSizeX);
        }

        internal float GetLocalZUnchecked(int z)
        {
            return MinZ + (z * CellSizeZ);
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
