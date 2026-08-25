using System;

namespace PromVR.MaterialAccumulation.Core
{
    public sealed class HeightField
    {
        private readonly float[] _heights;

        public HeightField(in GridDescriptor descriptor)
        {
            Descriptor = descriptor;
            _heights = new float[descriptor.HeightCount];
        }

        public GridDescriptor Descriptor { get; }

        public int Count => _heights.Length;

        public float GetHeight(int x, int z)
        {
            return _heights[Descriptor.GetIndex(x, z)];
        }

        public float GetHeightByIndex(int index)
        {
            return _heights[index];
        }

        public GridRect Reset()
        {
            Array.Clear(_heights, 0, _heights.Length);
            return GridRect.Full(Descriptor);
        }

        internal float GetHeightUnchecked(int index)
        {
            return _heights[index];
        }

        internal void SetHeightUnchecked(int index, float height)
        {
            _heights[index] = height;
        }
    }
}
