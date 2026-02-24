using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Runtime color lookup for item ids.
    /// ItemId 0 is reserved for no payload.
    /// </summary>
    public struct ItemColorPaletteData
    {
        private readonly Color32[] _colorsByItemId;
        private readonly Color32 _defaultUnknownItemColor;

        public ItemColorPaletteData(Color32[] colorsByItemId, Color32 defaultUnknownItemColor, Color32 emptyPayloadColor)
        {
            _colorsByItemId = colorsByItemId;
            _defaultUnknownItemColor = defaultUnknownItemColor;
            EmptyPayloadColor = emptyPayloadColor;
        }

        public Color32 EmptyPayloadColor { get; }

        public Color32 ResolveItemColor(int itemId)
        {
            if (itemId >= 0 && itemId < _colorsByItemId.Length)
            {
                return _colorsByItemId[itemId];
            }

            return _defaultUnknownItemColor;
        }
    }
}
