using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class LayerTests
    {
        [Test]
        public void TryApplyEvent_UpdatesOnlyTargetCell()
        {
            var layer = new Layer(minX: -1, minY: -1, width: 3, height: 3);

            bool applied = layer.TryApplyEvent(0, 1, 77, out GridCellData updated);

            Assert.That(applied, Is.True);
            Assert.That(updated.LastEventValue, Is.EqualTo(77));
            Assert.That(updated.EventCount, Is.EqualTo(1));

            Assert.That(layer.TryGet(0, 1, out GridCellData target), Is.True);
            Assert.That(target.LastEventValue, Is.EqualTo(77));
            Assert.That(target.EventCount, Is.EqualTo(1));

            Assert.That(layer.TryGet(-1, -1, out GridCellData other), Is.True);
            Assert.That(other.LastEventValue, Is.EqualTo(0));
            Assert.That(other.EventCount, Is.EqualTo(0));
        }

        [Test]
        public void TryApplyEvent_ReturnsFalse_WhenOutOfRange()
        {
            var layer = new Layer(minX: 0, minY: 0, width: 2, height: 2);

            bool applied = layer.TryApplyEvent(2, 1, 9, out GridCellData updated);

            Assert.That(applied, Is.False);
            Assert.That(updated.LastEventValue, Is.EqualTo(0));
            Assert.That(updated.EventCount, Is.EqualTo(0));
        }

        [Test]
        public void TryApplyEvent_IsDeterministic_ForSameEventSequence()
        {
            var firstLayer = new Layer(minX: 0, minY: 0, width: 4, height: 4);
            var secondLayer = new Layer(minX: 0, minY: 0, width: 4, height: 4);

            ApplySequence(firstLayer);
            ApplySequence(secondLayer);

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    Assert.That(firstLayer.TryGet(x, y, out GridCellData firstCell), Is.True);
                    Assert.That(secondLayer.TryGet(x, y, out GridCellData secondCell), Is.True);
                    Assert.That(firstCell.LastEventValue, Is.EqualTo(secondCell.LastEventValue));
                    Assert.That(firstCell.EventCount, Is.EqualTo(secondCell.EventCount));
                }
            }
        }

        private static void ApplySequence(Layer layer)
        {
            Assert.That(layer.TryApplyEvent(0, 0, 10, out _), Is.True);
            Assert.That(layer.TryApplyEvent(1, 0, 11, out _), Is.True);
            Assert.That(layer.TryApplyEvent(0, 0, 12, out _), Is.True);
            Assert.That(layer.TryApplyEvent(3, 2, 99, out _), Is.True);
        }
    }
}
