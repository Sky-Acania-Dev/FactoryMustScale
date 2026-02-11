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
        public void GetOrientationEnum_ReturnsExpectedDirection()
        {
            int variantId = 0;
            variantId = GridCellData.SetOrientation(variantId, CellOrientation.Up);
            Assert.That(GridCellData.GetOrientationEnum(variantId), Is.EqualTo(CellOrientation.Up));

            variantId = GridCellData.SetOrientation(variantId, CellOrientation.Left);
            Assert.That(GridCellData.GetOrientationEnum(variantId), Is.EqualTo(CellOrientation.Left));
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
        public void SetAndGetConstructionDestructionStage_UsesFourBits_AndSupportsFullStageRange()
        {
            int variantId = 0;
            variantId = GridCellData.SetConstructionDestructionStage(variantId, stage: 15);

            Assert.That(GridCellData.GetConstructionDestructionStage(variantId), Is.EqualTo(15));

            variantId = GridCellData.SetConstructionDestructionStage(variantId, stage: 31);
            Assert.That(GridCellData.GetConstructionDestructionStage(variantId), Is.EqualTo(15));
        }

        [Test]
        public void ConstructionAndDestructionHelpers_AreInferredFromStageCode()
        {
            int variantId = 0;

            variantId = GridCellData.SetConstructionDestructionStage(variantId, GridCellData.StageConstructionStart);
            Assert.That(GridCellData.IsUnderConstruction(variantId), Is.True);
            Assert.That(GridCellData.IsMarkedForDestruction(variantId), Is.False);

            variantId = GridCellData.SetConstructionDestructionStage(variantId, GridCellData.StageFullyBuilt);
            Assert.That(GridCellData.IsUnderConstruction(variantId), Is.False);
            Assert.That(GridCellData.IsMarkedForDestruction(variantId), Is.False);

            variantId = GridCellData.SetConstructionDestructionStage(variantId, GridCellData.StageDestructionStart);
            Assert.That(GridCellData.IsUnderConstruction(variantId), Is.False);
            Assert.That(GridCellData.IsMarkedForDestruction(variantId), Is.True);

            variantId = GridCellData.SetConstructionDestructionStage(variantId, GridCellData.StageDestroyed);
            Assert.That(GridCellData.IsUnderConstruction(variantId), Is.False);
            Assert.That(GridCellData.IsMarkedForDestruction(variantId), Is.False);
        }

        [Test]
        public void FlagHelpers_ReturnExpectedBooleanValues()
        {
            uint flags = 0u;
            flags |= GridCellData.FlagPowered;
            flags |= GridCellData.FlagCanAcceptPayload;

            Assert.That(GridCellData.IsPowered(flags), Is.True);
            Assert.That(GridCellData.IsConnected(flags), Is.False);
            Assert.That(GridCellData.CanAcceptPayload(flags), Is.True);
            Assert.That(GridCellData.CanOutputPayload(flags), Is.False);
            Assert.That(GridCellData.IsBlocked(flags), Is.False);
        }
    }
}
