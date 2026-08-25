using System;
using NUnit.Framework;
using PromVR.MaterialAccumulation.Core;

namespace PromVR.MaterialAccumulation.Tests.EditMode
{
    public sealed class SweepContinuityTests
    {
        private const float Tolerance = 0.00001f;

        [Test]
        public void Sweep_CoversFastMovementWithoutCenterlineGaps()
        {
            GridDescriptor grid = new GridDescriptor(40, 40, 10f, 10f);
            HeightField field = new HeightField(grid);
            HemisphereAccumulator accumulator = new HemisphereAccumulator(field);

            accumulator.Apply(new Sweep(-4f, 0f, 4f, 0f, 0.6f, 0.6f, 0.1f), 1f);

            int centerZ = FindIndex(0f, grid.MinZ, grid.CellSizeZ);
            for (float x = -4f; x <= 4f; x += grid.CellSizeX)
            {
                int gridX = FindIndex(x, grid.MinX, grid.CellSizeX);
                Assert.That(field.GetHeight(gridX, centerZ), Is.GreaterThan(0f), $"Gap at x={x}");
            }
        }

        [Test]
        public void RadiusSubdivision_PreservesTotalExposureTime()
        {
            GridDescriptor grid = new GridDescriptor(40, 40, 10f, 10f);
            HeightField field = new HeightField(grid);
            HemisphereAccumulator accumulator = new HemisphereAccumulator(field);

            accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 1f, 2f, 0.75f), 0.4f);

            Assert.That(GetHeightAt(field, grid, 0f, 0f), Is.EqualTo(0.3f).Within(Tolerance));
        }

        [Test]
        public void EquivalentMotion_WithDifferentFramePartitions_IsWithinGridTolerance()
        {
            GridDescriptor grid = new GridDescriptor(40, 40, 10f, 10f);
            HeightField singleFrameField = new HeightField(grid);
            HeightField partitionedField = new HeightField(grid);
            HemisphereAccumulator singleFrame = new HemisphereAccumulator(singleFrameField);
            HemisphereAccumulator partitioned = new HemisphereAccumulator(partitionedField);
            const float speed = 0.1f;

            singleFrame.Apply(new Sweep(-3f, 0f, 3f, 0f, 2f, 2f, 1f), speed);

            const int frameCount = 10;
            for (int frame = 0; frame < frameCount; frame++)
            {
                float start = -3f + (6f * frame / frameCount);
                float end = -3f + (6f * (frame + 1) / frameCount);
                partitioned.Apply(new Sweep(start, 0f, end, 0f, 2f, 2f, 1f / frameCount), speed);
            }

            float singleHeight = GetHeightAt(singleFrameField, grid, 0f, 0f);
            float partitionedHeight = GetHeightAt(partitionedField, grid, 0f, 0f);
            Assert.That(partitionedHeight, Is.EqualTo(singleHeight).Within(0.01f));
        }

        [Test]
        public void ChangingRadius_ChangesTrailWidthAlongPath()
        {
            GridDescriptor grid = new GridDescriptor(40, 40, 10f, 10f);
            HeightField field = new HeightField(grid);
            HemisphereAccumulator accumulator = new HemisphereAccumulator(field);

            accumulator.Apply(new Sweep(-3f, 0f, 3f, 0f, 0.5f, 2f, 1f), 0.5f);

            float narrowSide = GetHeightAt(field, grid, -2.5f, 1.25f);
            float wideSide = GetHeightAt(field, grid, 2.5f, 1.25f);
            Assert.That(narrowSide, Is.Zero);
            Assert.That(wideSide, Is.GreaterThan(0f));
        }

        private static float GetHeightAt(
            HeightField field,
            in GridDescriptor grid,
            float localX,
            float localZ)
        {
            int x = FindIndex(localX, grid.MinX, grid.CellSizeX);
            int z = FindIndex(localZ, grid.MinZ, grid.CellSizeZ);
            return field.GetHeight(x, z);
        }

        private static int FindIndex(float position, float minimum, float cellSize)
        {
            return (int)Math.Round((position - minimum) / cellSize);
        }
    }
}
