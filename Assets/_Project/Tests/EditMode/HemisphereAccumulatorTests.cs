using NUnit.Framework;
using PromVR.MaterialAccumulation.Core;

namespace PromVR.MaterialAccumulation.Tests.EditMode
{
    public sealed class HemisphereAccumulatorTests
    {
        private const float Tolerance = 0.00001f;

        private GridDescriptor _grid;
        private HeightField _field;
        private HemisphereAccumulator _accumulator;

        [SetUp]
        public void SetUp()
        {
            _grid = new GridDescriptor(40, 40, 10f, 10f);
            _field = new HeightField(_grid);
            _accumulator = new HemisphereAccumulator(_field);
        }

        [Test]
        public void StaticStamp_IncreasesHeightByRateTimesDuration()
        {
            _accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 2f, 2f, 0.4f), 0.5f);

            Assert.That(GetHeightAt(0f, 0f), Is.EqualTo(0.2f).Within(Tolerance));
        }

        [Test]
        public void StaticStamp_NeverExceedsHemisphereCeiling()
        {
            _accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 1f, 1f, 10f), 10f);

            Assert.That(GetHeightAt(0f, 0f), Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void LowerCeiling_NeverReducesExistingHeight()
        {
            _accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 2f, 2f, 5f), 1f);
            float heightBefore = GetHeightAt(0f, 0f);

            GridRect dirty = _accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 0.5f, 0.5f, 1f), 1f);

            Assert.That(GetHeightAt(0f, 0f), Is.EqualTo(heightBefore).Within(Tolerance));
            Assert.That(dirty.IsEmpty, Is.True);
        }

        [Test]
        public void RepeatedExposure_ContinuesAccumulation()
        {
            Sweep exposure = new Sweep(0f, 0f, 0f, 0f, 2f, 2f, 0.25f);

            _accumulator.Apply(exposure, 1f);
            _accumulator.Apply(exposure, 1f);

            Assert.That(GetHeightAt(0f, 0f), Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void OverlappingSweeps_ModifyTheSameHeightField()
        {
            _accumulator.Apply(new Sweep(-0.5f, 0f, -0.5f, 0f, 1.5f, 1.5f, 0.2f), 1f);
            float afterFirstPass = GetHeightAt(0f, 0f);

            _accumulator.Apply(new Sweep(0.5f, 0f, 0.5f, 0f, 1.5f, 1.5f, 0.2f), 1f);

            Assert.That(afterFirstPass, Is.GreaterThan(0f));
            Assert.That(GetHeightAt(0f, 0f), Is.GreaterThan(afterFirstPass));
        }

        [Test]
        public void StampAtBoundary_DoesNotAccessOutsideGrid()
        {
            Assert.DoesNotThrow(() =>
                _accumulator.Apply(new Sweep(-5f, -5f, -5f, -5f, 2f, 2f, 0.5f), 1f));
            Assert.That(_field.GetHeight(0, 0), Is.GreaterThan(0f));
        }

        [Test]
        public void ZeroDuration_DoesNotChangeState()
        {
            GridRect dirty = _accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 2f, 2f, 0f), 1f);

            Assert.That(dirty.IsEmpty, Is.True);
            Assert.That(GetHeightAt(0f, 0f), Is.Zero);
        }

        private float GetHeightAt(float localX, float localZ)
        {
            int x = (int)System.Math.Round((localX - _grid.MinX) / _grid.CellSizeX);
            int z = (int)System.Math.Round((localZ - _grid.MinZ) / _grid.CellSizeZ);
            return _field.GetHeight(x, z);
        }
    }
}
