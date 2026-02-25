using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Runtime color lookup for cell state ids.
    /// Baked from ScriptableObject authoring and used in minimap hot paths (array lookup, no dictionary).
    /// </summary>
    public struct GridStateColorPaletteData
    {
        private readonly Color32[] _colorsByStateId;
        private readonly Color32 _defaultUnknownStateColor;

        public GridStateColorPaletteData(Color32[] colorsByStateId, Color32 defaultUnknownStateColor)
        {
            _colorsByStateId = colorsByStateId;
            _defaultUnknownStateColor = defaultUnknownStateColor;
        }

        public Color32 ResolveStateColor(int stateId)
        {
            if (stateId >= 0 && stateId < _colorsByStateId.Length)
            {
                //Debug.Log("Resolving grid state id " + stateId + " to color " + _colorsByStateId[stateId]);
                return _colorsByStateId[stateId];
            }
            //Debug.LogWarning("Grid state id " + stateId + " is out of palette bounds; returning default unknown state color.");
            return _defaultUnknownStateColor;
        }
    }
}
