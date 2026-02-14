using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class FactoryProcessDirectionResolverTests
    {
        [Test]
        public void ResolveWorldDirections_UpMask_WithRightOrientation_RotatesToRight()
        {
            bool ok = FactoryProcessDirectionResolver.TryResolveWorldDirections(
                CardinalDirectionMask.Up,
                CellOrientation.Right,
                out CardinalDirectionMask resolved);

            Assert.That(ok, Is.True);
            Assert.That(resolved, Is.EqualTo(CardinalDirectionMask.Right));
        }

        [Test]
        public void ResolveWorldDirections_MultiMask_WithDownOrientation_RotatesAllBits()
        {
            CardinalDirectionMask local = CardinalDirectionMask.Up | CardinalDirectionMask.Left;

            bool ok = FactoryProcessDirectionResolver.TryResolveWorldDirections(
                local,
                CellOrientation.Down,
                out CardinalDirectionMask resolved);

            Assert.That(ok, Is.True);
            Assert.That(resolved, Is.EqualTo(CardinalDirectionMask.Down | CardinalDirectionMask.Right));
        }

        [Test]
        public void ResolveWorldDirections_FromVariantId_UsesGridCellOrientation()
        {
            int variantId = GridCellData.SetOrientation(0, CellOrientation.Left);

            bool ok = FactoryProcessDirectionResolver.TryResolveWorldDirections(
                CardinalDirectionMask.Up,
                variantId,
                out CardinalDirectionMask resolved);

            Assert.That(ok, Is.True);
            Assert.That(resolved, Is.EqualTo(CardinalDirectionMask.Left));
        }

        [Test]
        public void ResolveWorldDirections_NonCardinalOrientation_ReturnsFalse()
        {
            bool ok = FactoryProcessDirectionResolver.TryResolveWorldDirections(
                CardinalDirectionMask.Up,
                CellOrientation.UpRight,
                out CardinalDirectionMask resolved);

            Assert.That(ok, Is.False);
            Assert.That(resolved, Is.EqualTo(CardinalDirectionMask.None));
        }
    }
}
