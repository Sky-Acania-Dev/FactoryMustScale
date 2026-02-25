using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Real simulation-backed minimap source that reads authoritative references from SimulationLoopDriver.
    /// </summary>
    public sealed class SimLoopMinimapSource : GridMinimapSourceBase
    {
        [SerializeField]
        private SimulationLoopDriver _driver;

        public override int Width => _driver != null ? _driver.MinimapGridWidth : 0;

        public override int Height => _driver != null ? _driver.MinimapGridHeight : 0;

        public override GridCellData[] Cells => _driver != null ? _driver.MinimapCells : null;

        public override int[] ItemIdByCell => _driver != null ? _driver.MinimapItemIdByCell : null;

        public override int CurrentTick => _driver != null ? _driver.UnitTick : 0;
    }
}
