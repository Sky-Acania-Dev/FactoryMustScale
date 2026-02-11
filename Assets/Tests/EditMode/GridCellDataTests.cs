using FactoryMustScale.Simulation;
using NUnit.Framework;

namespace FactoryMustScale.Tests.EditMode
{
    public sealed class GridCellDataTests
    {
        [Test]
        public void SetAndGetOrientation_WritesOnlyOrientationBits()
        {
            const int initialVariantId = unchecked((int)0x7FFF_FFFF);

            int updatedVariantId = GridCellData.SetOrientation(initialVariantId, orientation: 2);

            Assert.That(GridCellData.GetOrientation(updatedVariantId), Is.EqualTo(2));
            Assert.That((updatedVariantId >> 2), Is.EqualTo(initialVariantId >> 2));
        }

        [Test]
        public void SetAndGetVariantCode_UsesSevenBits_AndReturnsByte()
        {
            int variantId = 0;
            variantId = GridCellData.SetVariantCode(variantId, (byte)63);

            Assert.That(GridCellData.GetVariantCode(variantId), Is.EqualTo((byte)63));

            variantId = GridCellData.SetVariantCode(variantId, byte.MaxValue);
            Assert.That(GridCellData.GetVariantCode(variantId), Is.EqualTo((byte)127));
        }

        [Test]
        public void SetAndGetConstructionDestructionStage_WritesExpectedBits()
        {
            int variantId = 0;
            variantId = GridCellData.SetConstructionDestructionStage(variantId, stage: 3);

            Assert.That(GridCellData.GetConstructionDestructionStage(variantId), Is.EqualTo(3));

            variantId = GridCellData.SetConstructionDestructionStage(variantId, stage: 7);
            Assert.That(GridCellData.GetConstructionDestructionStage(variantId), Is.EqualTo(3));
        }

        [Test]
        public void FlagHelpers_ReturnExpectedBooleanValues()
        {
            uint flags = 0u;
            flags |= GridCellData.FlagPowered;
            flags |= GridCellData.FlagCanAcceptPayload;
            flags |= GridCellData.FlagMarkedForDestruction;

            Assert.That(GridCellData.IsPowered(flags), Is.True);
            Assert.That(GridCellData.IsConnected(flags), Is.False);
            Assert.That(GridCellData.CanAcceptPayload(flags), Is.True);
            Assert.That(GridCellData.CanOutputPayload(flags), Is.False);
            Assert.That(GridCellData.IsBlocked(flags), Is.False);
            Assert.That(GridCellData.IsUnderConstruction(flags), Is.False);
            Assert.That(GridCellData.IsMarkedForDestruction(flags), Is.True);
        }
    }
}
