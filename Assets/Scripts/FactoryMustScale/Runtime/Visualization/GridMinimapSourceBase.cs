using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Adapter contract for exposing minimap-ready read-only grid arrays.
    /// Implementations must return stable references without per-frame allocations.
    /// </summary>
    public abstract class GridMinimapSourceBase : MonoBehaviour
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract GridCellData[] Cells { get; }
        public abstract int[] ItemIdByCell { get; }
        public abstract int CurrentTick { get; }
    }
}
