using NUnit.Framework;
using PromVR.MaterialAccumulation.Core;

namespace PromVR.MaterialAccumulation.Tests.EditMode
{
    public sealed class HeightFieldTests
    {
        [Test]
        public void Reset_ClearsEveryHeightAndMarksFullGridDirty()
        {
            GridDescriptor grid = new GridDescriptor(20, 20, 10f, 10f);
            HeightField field = new HeightField(grid);
            HemisphereAccumulator accumulator = new HemisphereAccumulator(field);
            accumulator.Apply(new Sweep(0f, 0f, 0f, 0f, 2f, 2f, 1f), 1f);

            GridRect dirty = field.Reset();

            Assert.That(dirty.IsEmpty, Is.False);
            Assert.That(dirty.MinX, Is.EqualTo(0));
            Assert.That(dirty.MinZ, Is.EqualTo(0));
            Assert.That(dirty.MaxX, Is.EqualTo(grid.VertexCountX - 1));
            Assert.That(dirty.MaxZ, Is.EqualTo(grid.VertexCountZ - 1));

            for (int index = 0; index < field.Count; index++)
            {
                Assert.That(field.GetHeightByIndex(index), Is.Zero);
            }
        }

        [Test]
        public void Descriptor_UsesCenteredRowMajorGrid()
        {
            GridDescriptor grid = new GridDescriptor(4, 2, 8f, 4f);

            Assert.That(grid.VertexCountX, Is.EqualTo(5));
            Assert.That(grid.VertexCountZ, Is.EqualTo(3));
            Assert.That(grid.GetLocalX(0), Is.EqualTo(-4f));
            Assert.That(grid.GetLocalX(4), Is.EqualTo(4f));
            Assert.That(grid.GetLocalZ(0), Is.EqualTo(-2f));
            Assert.That(grid.GetLocalZ(2), Is.EqualTo(2f));
            Assert.That(grid.GetIndex(3, 1), Is.EqualTo(8));
        }
    }
}
