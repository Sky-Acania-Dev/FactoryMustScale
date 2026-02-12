using System.Collections;
using FactoryMustScale.Simulation;
using FactoryMustScale.Visualization;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FactoryMustScale.Tests.PlayMode
{
    public sealed class VisualizationPlayModeTests
    {
        [UnityTest]
        public IEnumerator TickCoroutineDriver_InvokesTickRoughlyEveryQuarterSecond()
        {
            var go = new GameObject("TickDriverTest");
            var driver = go.AddComponent<MinimalFactoryTickCoroutineDriver>();

            int tickCount = 0;
            driver.StartTickLoop(() => tickCount++);

            yield return new WaitForSecondsRealtime(1.00f);

            driver.StopTickLoop();

            Assert.That(tickCount, Is.GreaterThanOrEqualTo(3));

            Object.Destroy(go);
        }

        [Test]
        public void LayerPngRenderTarget_BlendsFactoryOnTopOfTerrain()
        {
            Layer terrainLayer = new Layer(0, 0, 2, 1, payloadChannelCount: 1);
            Layer factoryLayer = new Layer(0, 0, 2, 1);

            terrainLayer.TrySetCellState(0, 0, (int)TerrainType.Ground, 0, 0u, currentTick: 0, out _);
            terrainLayer.TrySetCellState(1, 0, (int)TerrainType.ResourceDeposit, 0, 0u, currentTick: 0, out _);
            terrainLayer.TrySetPayload(0, 0, 0, (int)ResourceType.None);
            terrainLayer.TrySetPayload(1, 0, 0, (int)ResourceType.Ore);

            factoryLayer.TrySetCellState(0, 0, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);
            factoryLayer.TrySetCellState(1, 0, (int)GridStateId.Empty, 0, 0u, currentTick: 0, out _);

            var go = new GameObject("LayerPngTargetTest");
            var target = go.AddComponent<LayerPngRenderTarget>();

            Texture2D terrainOnly = target.Render(terrainLayer, factoryLayer);
            Color32 terrainOnlyPixel = terrainOnly.GetPixel(1, 0);

            factoryLayer.TrySetCellState(1, 0, (int)GridStateId.Conveyor, 0, 0u, currentTick: 0, out _);
            Texture2D withFactory = target.Render(terrainLayer, factoryLayer);
            Color32 compositedPixel = withFactory.GetPixel(1, 0);

            Assert.That(terrainOnly.width, Is.EqualTo(2));
            Assert.That(terrainOnly.height, Is.EqualTo(1));
            bool changed = compositedPixel.r != terrainOnlyPixel.r
                || compositedPixel.g != terrainOnlyPixel.g
                || compositedPixel.b != terrainOnlyPixel.b;
            Assert.That(changed, Is.True);

            Object.DestroyImmediate(terrainOnly);
            Object.DestroyImmediate(withFactory);
            Object.DestroyImmediate(go);
        }
    }
}
