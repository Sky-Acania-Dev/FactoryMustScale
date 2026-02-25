using FactoryMustScale.Simulation;
using Unity.Collections;
using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Stateless-style minimap renderer over deterministic grid data.
    /// Coordinate convention: y-up simulation indexing and y-up texture addressing (row-major index = x + y * width).
    /// </summary>
    public sealed class GridMinimapRenderer
    {
        private Texture2D _texture;
        private int _gridWidth;
        private int _gridHeight;
        private int _cellPixelSize;
        private int _textureWidth;

        public Texture2D Texture => _texture;

        public void Initialize(int width, int height, int cellPixelSize = 3)
        {
            _gridWidth = width;
            _gridHeight = height;
            _cellPixelSize = cellPixelSize < 1 ? 1 : cellPixelSize;
            _textureWidth = _gridWidth * _cellPixelSize;
            int textureHeight = _gridHeight * _cellPixelSize;

            _texture = new Texture2D(_textureWidth, textureHeight, TextureFormat.RGBA32, mipChain: false, linear: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        public void Render(
            GridCellData[] cells,
            int width,
            int height,
            int[] itemIdByCell,
            in GridStateColorPaletteData statePalette,
            in ItemColorPaletteData itemPalette)
        {
            if (_texture == null
                || cells == null
                || itemIdByCell == null
                || width != _gridWidth
                || height != _gridHeight)
            {
                return;
            }

            int cellCount = width * height;
            if (cells.Length < cellCount || itemIdByCell.Length < cellCount)
            {
                return;
            }

            NativeArray<Color32> rawPixels = _texture.GetRawTextureData<Color32>();
            int centerOffset = _cellPixelSize / 2;

            for (int y = 0; y < height; y++)
            {
                int rowCellOffset = y * width;
                int blockRowStart = y * _cellPixelSize;

                for (int x = 0; x < width; x++)
                {
                    int cellIndex = rowCellOffset + x;
                    Color32 stateColor = statePalette.ResolveStateColor(cells[cellIndex].StateId);

                    int blockXStart = x * _cellPixelSize;
                    for (int py = 0; py < _cellPixelSize; py++)
                    {
                        int pixelRowStart = (blockRowStart + py) * _textureWidth + blockXStart;
                        for (int px = 0; px < _cellPixelSize; px++)
                        {
                            rawPixels[pixelRowStart + px] = stateColor;
                        }
                    }

                    int itemId = itemIdByCell[cellIndex];
                    if (itemId != 0)
                    {
                        int dotX = blockXStart + centerOffset;
                        int dotY = blockRowStart + centerOffset;
                        int dotIndex = dotX + dotY * _textureWidth;
                        rawPixels[dotIndex] = itemPalette.ResolveItemColor(itemId);
                    }
                }
            }

            _texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        }
    }
}
