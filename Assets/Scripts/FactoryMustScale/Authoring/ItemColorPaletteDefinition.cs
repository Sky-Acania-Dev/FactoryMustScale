using FactoryMustScale.Runtime.Visualization;
using UnityEngine;

namespace FactoryMustScale.Authoring
{
    [System.Serializable]
    public struct ItemColorPaletteEntry
    {
        public string name;
        public int ItemId;
        public Color32 Color;
    }

    [CreateAssetMenu(
        fileName = "ItemColorPaletteDefinition",
        menuName = "FactoryMustScale/Definitions/Item Color Palette")]
    /// <summary>
    /// Authoring palette for payload/item colors used by debug minimap rendering.
    /// ItemId 0 is "no payload" and should not be drawn as a dot.
    /// </summary>
    public sealed class ItemColorPaletteDefinition : ScriptableObject
    {
        [SerializeField]
        private Color32 _defaultUnknownItemColor = new Color32(255, 255, 255, 255);

        [SerializeField]
        private Color32 _emptyPayloadColor = new Color32(0, 0, 0, 0);

        [SerializeField]
        private ItemColorPaletteEntry[] _entries;

        public Color32 DefaultUnknownItemColor => _defaultUnknownItemColor;
        public Color32 EmptyPayloadColor => _emptyPayloadColor;
        public ItemColorPaletteEntry[] Entries => _entries;

        public ItemColorPaletteData Bake()
        {
            int maxItemId = 0;
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    int itemId = _entries[i].ItemId;
                    if (itemId > maxItemId)
                    {
                        maxItemId = itemId;
                    }
                }
            }

            var colorsByItemId = new Color32[maxItemId + 1];
            for (int i = 0; i < colorsByItemId.Length; i++)
            {
                colorsByItemId[i] = _defaultUnknownItemColor;
            }

            if (colorsByItemId.Length > 0)
            {
                colorsByItemId[0] = _emptyPayloadColor;
            }

            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    int itemId = _entries[i].ItemId;
                    if (itemId <= 0 || itemId >= colorsByItemId.Length)
                    {
                        continue;
                    }

                    colorsByItemId[itemId] = _entries[i].Color;
                }
            }

            return new ItemColorPaletteData(colorsByItemId, _defaultUnknownItemColor, _emptyPayloadColor);
        }
    }
}
