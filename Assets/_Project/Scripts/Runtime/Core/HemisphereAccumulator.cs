using System;

namespace PromVR.MaterialAccumulation.Core
{
    public sealed class HemisphereAccumulator
    {
        private const float StationaryEpsilon = 0.000001f;

        private readonly HeightField _heightField;
        private readonly GridDescriptor _grid;
        private readonly float _maximumSampleStep;

        public HemisphereAccumulator(HeightField heightField)
        {
            _heightField = heightField ?? throw new ArgumentNullException(nameof(heightField));
            _grid = heightField.Descriptor;
            _maximumSampleStep = Math.Min(_grid.CellSizeX, _grid.CellSizeZ) * 0.5f;
        }

        public GridRect Apply(in Sweep sweep, float accumulationSpeed)
        {
            ValidateSweep(sweep, accumulationSpeed);

            if (sweep.DurationSeconds == 0f || accumulationSpeed == 0f)
            {
                return GridRect.Empty;
            }

            float travelX = sweep.EndX - sweep.StartX;
            float travelZ = sweep.EndZ - sweep.StartZ;
            float travelSquared = (travelX * travelX) + (travelZ * travelZ);
            float radiusDelta = sweep.EndRadius - sweep.StartRadius;

            if (travelSquared <= StationaryEpsilon * StationaryEpsilon &&
                Math.Abs(radiusDelta) <= StationaryEpsilon)
            {
                return ApplyStamp(
                    sweep.EndX,
                    sweep.EndZ,
                    sweep.EndRadius,
                    sweep.DurationSeconds,
                    accumulationSpeed);
            }

            int travelSegments = (int)Math.Ceiling(Math.Sqrt(travelSquared) / _maximumSampleStep);
            int radiusSegments = (int)Math.Ceiling(Math.Abs(radiusDelta) / _maximumSampleStep);
            int segmentCount = Math.Max(1, Math.Max(travelSegments, radiusSegments));
            float baseSampleTime = sweep.DurationSeconds / segmentCount;
            GridRect dirty = GridRect.Empty;

            // Trapezoidal endpoint weights preserve both ends of the path while
            // keeping the total exposure exactly equal to the frame duration.
            for (int sample = 0; sample <= segmentCount; sample++)
            {
                float interpolation = (float)sample / segmentCount;
                float endpointWeight = sample == 0 || sample == segmentCount ? 0.5f : 1f;
                float centerX = sweep.StartX + (travelX * interpolation);
                float centerZ = sweep.StartZ + (travelZ * interpolation);
                float radius = sweep.StartRadius + (radiusDelta * interpolation);
                GridRect stampDirty = ApplyStamp(
                    centerX,
                    centerZ,
                    radius,
                    baseSampleTime * endpointWeight,
                    accumulationSpeed);
                dirty = dirty.Union(stampDirty);
            }

            return dirty;
        }

        private GridRect ApplyStamp(
            float centerX,
            float centerZ,
            float radius,
            float sampleTime,
            float accumulationSpeed)
        {
            int minX = (int)Math.Ceiling((centerX - radius - _grid.MinX) / _grid.CellSizeX);
            int maxX = (int)Math.Floor((centerX + radius - _grid.MinX) / _grid.CellSizeX);
            int minZ = (int)Math.Ceiling((centerZ - radius - _grid.MinZ) / _grid.CellSizeZ);
            int maxZ = (int)Math.Floor((centerZ + radius - _grid.MinZ) / _grid.CellSizeZ);

            if (maxX < 0 || maxZ < 0 || minX >= _grid.VertexCountX || minZ >= _grid.VertexCountZ)
            {
                return GridRect.Empty;
            }

            minX = Math.Max(0, minX);
            minZ = Math.Max(0, minZ);
            maxX = Math.Min(_grid.VertexCountX - 1, maxX);
            maxZ = Math.Min(_grid.VertexCountZ - 1, maxZ);

            float radiusSquared = radius * radius;
            float heightDelta = accumulationSpeed * sampleTime;
            GridRect dirty = GridRect.Empty;

            for (int z = minZ; z <= maxZ; z++)
            {
                float localZ = _grid.GetLocalZUnchecked(z);
                float deltaZ = localZ - centerZ;
                float deltaZSquared = deltaZ * deltaZ;

                for (int x = minX; x <= maxX; x++)
                {
                    float localX = _grid.GetLocalXUnchecked(x);
                    float deltaX = localX - centerX;
                    float distanceSquared = (deltaX * deltaX) + deltaZSquared;

                    if (distanceSquared >= radiusSquared)
                    {
                        continue;
                    }

                    int index = _grid.GetIndexUnchecked(x, z);
                    float oldHeight = _heightField.GetHeightUnchecked(index);
                    float ceiling = (float)Math.Sqrt(radiusSquared - distanceSquared);

                    // A lower or shifted hemisphere may only block new material;
                    // it must never cut material that is already present.
                    if (ceiling <= oldHeight)
                    {
                        continue;
                    }

                    float candidate = oldHeight + heightDelta;
                    float newHeight = Math.Min(candidate, ceiling);

                    if (newHeight <= oldHeight)
                    {
                        continue;
                    }

                    _heightField.SetHeightUnchecked(index, newHeight);
                    dirty = dirty.Include(x, z);
                }
            }

            return dirty;
        }

        private static void ValidateSweep(in Sweep sweep, float accumulationSpeed)
        {
            if (!IsFinite(sweep.StartX) || !IsFinite(sweep.StartZ) ||
                !IsFinite(sweep.EndX) || !IsFinite(sweep.EndZ))
            {
                throw new ArgumentException("Sweep positions must be finite.", nameof(sweep));
            }

            if (!IsPositiveFinite(sweep.StartRadius) || !IsPositiveFinite(sweep.EndRadius))
            {
                throw new ArgumentException("Sweep radii must be positive and finite.", nameof(sweep));
            }

            if (!IsFinite(sweep.DurationSeconds) || sweep.DurationSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sweep), "Sweep duration must be finite and non-negative.");
            }

            if (!IsFinite(accumulationSpeed) || accumulationSpeed < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(accumulationSpeed));
            }
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
