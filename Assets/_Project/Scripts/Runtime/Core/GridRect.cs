using System;

namespace PromVR.MaterialAccumulation.Core
{
    public readonly struct GridRect
    {
        private readonly bool _hasValue;

        private GridRect(bool hasValue, int minX, int minZ, int maxX, int maxZ)
        {
            _hasValue = hasValue;
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public static GridRect Empty => default;

        public bool IsEmpty => !_hasValue;

        public int MinX { get; }

        public int MinZ { get; }

        public int MaxX { get; }

        public int MaxZ { get; }

        public int Width => IsEmpty ? 0 : MaxX - MinX + 1;

        public int Height => IsEmpty ? 0 : MaxZ - MinZ + 1;

        public static GridRect FromPoint(int x, int z)
        {
            return new GridRect(true, x, z, x, z);
        }

        public static GridRect Full(in GridDescriptor descriptor)
        {
            return new GridRect(
                true,
                0,
                0,
                descriptor.VertexCountX - 1,
                descriptor.VertexCountZ - 1);
        }

        public GridRect Include(int x, int z)
        {
            if (IsEmpty)
            {
                return FromPoint(x, z);
            }

            return new GridRect(
                true,
                Math.Min(MinX, x),
                Math.Min(MinZ, z),
                Math.Max(MaxX, x),
                Math.Max(MaxZ, z));
        }

        public GridRect Union(in GridRect other)
        {
            if (IsEmpty)
            {
                return other;
            }

            if (other.IsEmpty)
            {
                return this;
            }

            return new GridRect(
                true,
                Math.Min(MinX, other.MinX),
                Math.Min(MinZ, other.MinZ),
                Math.Max(MaxX, other.MaxX),
                Math.Max(MaxZ, other.MaxZ));
        }

        public GridRect Expand(int border, int maxXInclusive, int maxZInclusive)
        {
            if (border < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(border));
            }

            if (IsEmpty)
            {
                return this;
            }

            return new GridRect(
                true,
                Math.Max(0, MinX - border),
                Math.Max(0, MinZ - border),
                Math.Min(maxXInclusive, MaxX + border),
                Math.Min(maxZInclusive, MaxZ + border));
        }
    }
}
