using FactoryMustScale.Authoring;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryMustScale.Runtime.Visualization
{
    /// <summary>
    /// Drop this component on any GameObject in a scene, assign palette assets, then press Play.
    /// If no targets are assigned, it auto-creates a Canvas + RawImage for quick debug use.
    /// Debug scaffold today; intended to evolve into a production minimap view later.
    /// </summary>
    public sealed class DebugGridMinimapView : MonoBehaviour
    {
        private const float SimTickIntervalSeconds = 0.25f;

        [Header("Palettes")]
        [SerializeField]
        private GridStateColorPaletteDefinition _statePaletteAsset;

        [SerializeField]
        private ItemColorPaletteDefinition _itemPaletteAsset;

        [Header("Renderer")]
        [SerializeField]
        private int _cellPixelSize = 3;

        [SerializeField]
        private bool _autoCreateTestScenario = true;

        [SerializeField]
        private RawImage _targetUI;

        [SerializeField]
        private Renderer _targetQuadRenderer;

        private DebugGridMinimapRenderer _renderer;
        private DebugGridMinimapTestScenario _scenario;
        private GridStateColorPaletteData _statePalette;
        private ItemColorPaletteData _itemPalette;
        private float _simTickAccumulatorSeconds;

        private void Start()
        {
            _statePalette = _statePaletteAsset != null
                ? _statePaletteAsset.Bake()
                : new GridStateColorPaletteData(new[] { new Color32(0, 0, 0, 255) }, new Color32(255, 0, 255, 255));

            _itemPalette = _itemPaletteAsset != null
                ? _itemPaletteAsset.Bake()
                : new ItemColorPaletteData(new[] { new Color32(0, 0, 0, 0), new Color32(255, 255, 0, 255) }, new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0));

            if (_autoCreateTestScenario)
            {
                _scenario = DebugGridMinimapTestScenario.CreateDefaultSShape();
            }

            if (_scenario == null)
            {
                enabled = false;
                return;
            }

            _renderer = new DebugGridMinimapRenderer();
            _renderer.Initialize(_scenario.Width, _scenario.Height, _cellPixelSize);
            BindTextureTargets(_renderer.Texture);
            RenderCurrentState();
        }

        private void FixedUpdate()
        {
            if (_renderer == null || _scenario == null)
            {
                return;
            }

            _simTickAccumulatorSeconds += Time.fixedDeltaTime;
            while (_simTickAccumulatorSeconds >= SimTickIntervalSeconds)
            {
                _scenario.Tick(1);
                _simTickAccumulatorSeconds -= SimTickIntervalSeconds;
            }
        }

        private void Update()
        {
            if (_renderer == null || _scenario == null)
            {
                return;
            }

            RenderCurrentState();
        }

        private void RenderCurrentState()
        {
            _renderer.Render(
                _scenario.Cells,
                _scenario.Width,
                _scenario.Height,
                _scenario.ItemIdByCell,
                in _statePalette,
                in _itemPalette);
        }

        private void BindTextureTargets(Texture texture)
        {
            if (_targetUI == null && _targetQuadRenderer == null)
            {
                _targetUI = CreateDefaultUiTarget();
            }

            if (_targetUI != null)
            {
                _targetUI.texture = texture;
            }

            if (_targetQuadRenderer != null)
            {
                _targetQuadRenderer.material.mainTexture = texture;
            }
        }

        private RawImage CreateDefaultUiTarget()
        {
            var canvasGo = new GameObject("DebugGridMinimapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var rawImageGo = new GameObject("DebugGridMinimap", typeof(RectTransform), typeof(RawImage));
            rawImageGo.transform.SetParent(canvasGo.transform, false);

            var rect = rawImageGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(16f, 16f);
            rect.sizeDelta = new Vector2(_scenario.Width * _cellPixelSize, _scenario.Height * _cellPixelSize);

            return rawImageGo.GetComponent<RawImage>();
        }
    }
}
