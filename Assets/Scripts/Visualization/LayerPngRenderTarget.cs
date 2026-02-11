using System;
using System.IO;
using UnityEngine;
using FactoryMustScale.Simulation;

namespace FactoryMustScale.Visualization
{
    /// <summary>
    /// Adapter-only renderer that converts terrain/factory simulation layers into a PNG image.
    /// Each cell becomes one pixel. Terrain is drawn first, then factory is alpha-blended on top.
    /// </summary>
    public sealed class LayerPngRenderTarget : MonoBehaviour
    {
        private const byte FactoryOverlayAlpha = 179; // ~0.7f * 255

        public Texture2D Render(Layer terrainLayer, Layer factoryLayer)
        {
            if (terrainLayer == null)
            {
                throw new ArgumentNullException(nameof(terrainLayer));
            }

            if (factoryLayer == null)
            {
                throw new ArgumentNullException(nameof(factoryLayer));
            }

            if (!HasMatchingBounds(terrainLayer, factoryLayer))
            {
                throw new ArgumentException("Terrain and factory layers must share identical bounds.");
            }

            int width = terrainLayer.Width;
            int height = terrainLayer.Height;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: true);
            var pixels = new Color32[width * height];

            for (int localY = 0; localY < height; localY++)
            {
                int y = terrainLayer.MinY + localY;

                for (int localX = 0; localX < width; localX++)
                {
                    int x = terrainLayer.MinX + localX;

                    terrainLayer.TryGet(x, y, out GridCellData terrainCell);
                    factoryLayer.TryGet(x, y, out GridCellData factoryCell);

                    Color32 terrainColor = GetColorForStateId(terrainCell.StateId, 255);
                    Color32 factoryColor = factoryCell.StateId == (int)GridStateId.Empty
                        ? new Color32(0, 0, 0, 0)
                        : GetColorForStateId(factoryCell.StateId, FactoryOverlayAlpha);

                    pixels[(localY * width) + localX] = AlphaBlend(factoryColor, terrainColor);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return texture;
        }

        public bool RenderAndSavePng(Layer terrainLayer, Layer factoryLayer, string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            Texture2D texture = Render(terrainLayer, factoryLayer);
            byte[] pngBytes = texture.EncodeToPNG();

            if (Application.isPlaying)
            {
                Destroy(texture);
            }
            else
            {
                DestroyImmediate(texture);
            }

            if (pngBytes == null || pngBytes.Length == 0)
            {
                return false;
            }

            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, pngBytes);
            return true;
        }

        private static bool HasMatchingBounds(Layer a, Layer b)
        {
            return a.MinX == b.MinX
                && a.MinY == b.MinY
                && a.Width == b.Width
                && a.Height == b.Height;
        }

        private static Color32 GetColorForStateId(int stateId, byte alpha)
        {
            unchecked
            {
                uint value = (uint)stateId;
                value ^= 0x9E3779B9u;
                value *= 0x85EBCA6Bu;
                value ^= value >> 13;
                value *= 0xC2B2AE35u;
                value ^= value >> 16;

                byte r = (byte)(48 + (value & 0x7F));
                byte g = (byte)(48 + ((value >> 8) & 0x7F));
                byte b = (byte)(48 + ((value >> 16) & 0x7F));
                return new Color32(r, g, b, alpha);
            }
        }

        private static Color32 AlphaBlend(Color32 foreground, Color32 background)
        {
            int alphaFg = foreground.a;
            int invAlphaFg = 255 - alphaFg;

            byte outR = (byte)(((foreground.r * alphaFg) + (background.r * invAlphaFg)) / 255);
            byte outG = (byte)(((foreground.g * alphaFg) + (background.g * invAlphaFg)) / 255);
            byte outB = (byte)(((foreground.b * alphaFg) + (background.b * invAlphaFg)) / 255);

            return new Color32(outR, outG, outB, 255);
        }
    }
}
