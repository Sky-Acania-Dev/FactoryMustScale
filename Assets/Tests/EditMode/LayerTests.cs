using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class LayerTests
    {
        [Test]
        public void TrySetCellState_UpdatesOnlyTargetCell()
        {
            var layer = new Layer(minX: -1, minY: -1, width: 3, height: 3);

            bool applied = layer.TrySetCellState(0, 1, stateId: 7, variantId: 2, flags: 4u, currentTick: 15, out GridCellData updated);

            Assert.That(applied, Is.True);
            Assert.That(updated.StateId, Is.EqualTo(7));
            Assert.That(updated.VariantId, Is.EqualTo(2));
            Assert.That(updated.Flags, Is.EqualTo(4u));
            Assert.That(updated.LastUpdatedTick, Is.EqualTo(15));
            Assert.That(updated.ChangeCount, Is.EqualTo(1));

            Assert.That(layer.TryGet(0, 1, out GridCellData target), Is.True);
            Assert.That(target.StateId, Is.EqualTo(7));
            Assert.That(target.VariantId, Is.EqualTo(2));
            Assert.That(target.Flags, Is.EqualTo(4u));
            Assert.That(target.LastUpdatedTick, Is.EqualTo(15));
            Assert.That(target.ChangeCount, Is.EqualTo(1));

            Assert.That(layer.TryGet(-1, -1, out GridCellData other), Is.True);
            Assert.That(other.StateId, Is.EqualTo(0));
            Assert.That(other.ChangeCount, Is.EqualTo(0));
        }

        [Test]
        public void TrySetCellState_ReturnsFalse_WhenOutOfRange()
        {
            var layer = new Layer(minX: 0, minY: 0, width: 2, height: 2);

            bool applied = layer.TrySetCellState(2, 1, stateId: 9, variantId: 0, flags: 0u, currentTick: 3, out GridCellData updated);

            Assert.That(applied, Is.False);
            Assert.That(updated.StateId, Is.EqualTo(0));
            Assert.That(updated.ChangeCount, Is.EqualTo(0));
        }

        [Test]
        public void TrySetCellState_IsDeterministic_ForSameEventSequence()
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
                    Assert.That(firstCell.StateId, Is.EqualTo(secondCell.StateId));
                    Assert.That(firstCell.VariantId, Is.EqualTo(secondCell.VariantId));
                    Assert.That(firstCell.Flags, Is.EqualTo(secondCell.Flags));
                    Assert.That(firstCell.LastUpdatedTick, Is.EqualTo(secondCell.LastUpdatedTick));
                    Assert.That(firstCell.ChangeCount, Is.EqualTo(secondCell.ChangeCount));
                }
            }
        }

        [Test]
        public void TrySetCellState_CanRepresentMultiCellCrafterInputOutput()
        {
            var layer = new Layer(minX: 0, minY: 0, width: 6, height: 4);

            // Same logical machine can mark different role/state at different cells.
            Assert.That(layer.TrySetCellState(2, 1, stateId: 100, variantId: 1, flags: 1u, currentTick: 20, out GridCellData inputCell), Is.True);
            Assert.That(layer.TrySetCellState(3, 1, stateId: 101, variantId: 2, flags: 2u, currentTick: 20, out GridCellData outputCell), Is.True);

            Assert.That(inputCell.StateId, Is.EqualTo(100));
            Assert.That(outputCell.StateId, Is.EqualTo(101));
            Assert.That(inputCell.LastUpdatedTick, Is.EqualTo(20));
            Assert.That(outputCell.LastUpdatedTick, Is.EqualTo(20));
        }

        private static void ApplySequence(Layer layer)
        {
            Assert.That(layer.TrySetCellState(0, 0, stateId: 10, variantId: 0, flags: 0u, currentTick: 1, out _), Is.True);
            Assert.That(layer.TrySetCellState(1, 0, stateId: 11, variantId: 1, flags: 8u, currentTick: 2, out _), Is.True);
            Assert.That(layer.TrySetCellState(0, 0, stateId: 12, variantId: 0, flags: 4u, currentTick: 3, out _), Is.True);
            Assert.That(layer.TrySetCellState(3, 2, stateId: 99, variantId: 2, flags: 16u, currentTick: 4, out _), Is.True);
        }
    }
}
