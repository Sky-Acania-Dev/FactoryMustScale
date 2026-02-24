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
                return _colorsByStateId[stateId];
            }

            return _defaultUnknownStateColor;
        }
    }
}
