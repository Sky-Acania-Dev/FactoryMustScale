using FactoryMustScale.Authoring;
using FactoryMustScale.Simulation;
using UnityEngine;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Grid minimap presenter setup:
    /// 1) Add SimulationLoopDriver to a scene object and configure the real simulation state.
    /// 2) Add SimLoopMinimapSource and assign the SimulationLoopDriver reference.
    /// 3) Add GridMinimapView and assign Source + palette assets.
    /// 4) Assign a Quad Renderer with an unlit material; this component writes the minimap Texture2D to material.mainTexture.
    ///
    /// This view only observes source data in LateUpdate and never creates/ticks simulation logic.
    /// </summary>
    public sealed class GridMinimapView : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField]
        private GridMinimapSourceBase _source;

        [Header("Palettes")]
        [SerializeField]
        private GridStateColorPaletteDefinition _statePaletteAsset;

        [SerializeField]
        private ItemColorPaletteDefinition _itemPaletteAsset;

        [Header("Renderer")]
        [SerializeField]
        private int _cellPixelSize = 3;

        [Tooltip("Render every frame when set to 0. Values > 0 render when source tick advances by at least this amount.")]
        [SerializeField]
        private int _renderEveryNTicks;

        [SerializeField]
        private Renderer _targetQuadRenderer;

        private GridMinimapRenderer _renderer;
        private GridStateColorPaletteData _statePalette;
        private ItemColorPaletteData _itemPalette;
        private int _initializedWidth;
        private int _initializedHeight;
        private int _lastRenderedTick = int.MinValue;

        private void Start()
        {
            _statePalette = _statePaletteAsset != null
                ? _statePaletteAsset.Bake()
                : new GridStateColorPaletteData(new[] { new Color32(0, 0, 0, 255) }, new Color32(255, 0, 255, 255));

            _itemPalette = _itemPaletteAsset != null
                ? _itemPaletteAsset.Bake()
                : new ItemColorPaletteData(new[] { new Color32(0, 0, 0, 0), new Color32(255, 255, 0, 255) }, new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0));
        }

        private void LateUpdate()
        {
            if (_source == null)
            {
                return;
            }

            int width = _source.Width;
            int height = _source.Height;
            GridCellData[] cells = _source.Cells;
            int[] itemIdByCell = _source.ItemIdByCell;
            if (width <= 0 || height <= 0 || cells == null || itemIdByCell == null)
            {
                return;
            }

            if (_renderer == null || width != _initializedWidth || height != _initializedHeight)
            {
                _renderer = new GridMinimapRenderer();
                _renderer.Initialize(width, height, _cellPixelSize);
                _initializedWidth = width;
                _initializedHeight = height;
                _lastRenderedTick = int.MinValue;
                BindTextureTarget(_renderer.Texture);
            }

            int sourceTick = _source.CurrentTick;
            if (_renderEveryNTicks > 0
                && _lastRenderedTick != int.MinValue
                && sourceTick - _lastRenderedTick < _renderEveryNTicks)
            {
                return;
            }

            _renderer.Render(cells, width, height, itemIdByCell, in _statePalette, in _itemPalette);
            _lastRenderedTick = sourceTick;
        }

        private void BindTextureTarget(Texture texture)
        {
            if (_targetQuadRenderer == null)
            {
                return;
            }

            Material targetMaterial = _targetQuadRenderer.sharedMaterial;
            if (targetMaterial == null)
            {
                return;
            }

            targetMaterial.mainTexture = texture;
        }
    }
}
