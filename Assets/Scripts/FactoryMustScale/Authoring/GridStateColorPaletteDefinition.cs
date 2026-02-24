using FactoryMustScale.Runtime.Visualization;
using UnityEngine;

namespace FactoryMustScale.Authoring
{
    [System.Serializable]
    public struct GridStateColorPaletteEntry
    {
        public int StateId;
        public Color32 Color;
    }

    [CreateAssetMenu(
        fileName = "GridStateColorPaletteDefinition",
        menuName = "FactoryMustScale/Definitions/Grid State Color Palette")]
    /// <summary>
    /// Authoring palette for debug grid/minimap state colors.
    /// StateId 0 should be configured to the desired empty-cell color.
    /// </summary>
    public sealed class GridStateColorPaletteDefinition : ScriptableObject
    {
        [SerializeField]
        private Color32 _defaultUnknownStateColor = new Color32(255, 0, 255, 255);

        [SerializeField]
        private GridStateColorPaletteEntry[] _entries;

        public Color32 DefaultUnknownStateColor => _defaultUnknownStateColor;
        public GridStateColorPaletteEntry[] Entries => _entries;

        public GridStateColorPaletteData Bake()
        {
            int maxStateId = 0;
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    int stateId = _entries[i].StateId;
                    if (stateId > maxStateId)
                    {
                        maxStateId = stateId;
                    }
                }
            }

            var colorsByStateId = new Color32[maxStateId + 1];
            for (int i = 0; i < colorsByStateId.Length; i++)
            {
                colorsByStateId[i] = _defaultUnknownStateColor;
            }

            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    int stateId = _entries[i].StateId;
                    if (stateId < 0 || stateId >= colorsByStateId.Length)
                    {
                        continue;
                    }

                    colorsByStateId[stateId] = _entries[i].Color;
                }
            }

            return new GridStateColorPaletteData(colorsByStateId, _defaultUnknownStateColor);
        }
    }
}
