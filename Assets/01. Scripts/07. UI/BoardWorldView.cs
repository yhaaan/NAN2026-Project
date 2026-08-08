using System;
using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class BoardWorldView : MonoBehaviour
    {
        private const float BoardVisualSize = 15.384615f;
        private const float GridHalfSize = 7f;

        private static readonly Color BoardColor = new Color(0.78f, 0.57f, 0.30f);
        private static readonly Color GridColor = new Color(0.16f, 0.11f, 0.07f);
        private static readonly Color PlayerRangeColor = new Color(0.12f, 0.62f, 1f, 0.28f);
        private static readonly Color EnemyRangeColor = new Color(1f, 0.2f, 0.16f, 0.28f);

        private readonly Dictionary<BoardUnit, UnitView> unitViews =
            new Dictionary<BoardUnit, UnitView>();
        private readonly Dictionary<BoardUnit, UnitHealthBarView> healthBars =
            new Dictionary<BoardUnit, UnitHealthBarView>();
        private readonly List<SpriteRenderer> highlights = new List<SpriteRenderer>();
        private readonly HashSet<BoardUnit> currentUnits = new HashSet<BoardUnit>();
        private readonly HashSet<string> reportedMissingPresentations = new HashSet<string>();

        private RectTransform inputRect;
        private Camera worldCamera;
        private UnitHealthBarView healthBarPrefab;
        private Sprite boardSprite;
        private Transform unitRoot;
        private Transform overlayRoot;
        private UnitView previewView;
        private UnitDefinitionSO previewDefinition;
        private bool geometryCreated;
        private bool missingHealthBarReported;

        public int ActiveUnitViewCount => unitViews.Count;

        public void Initialize(
            RectTransform targetInputRect,
            Camera targetCamera,
            UnitHealthBarView targetHealthBarPrefab,
            Sprite targetBoardSprite)
        {
            inputRect = targetInputRect;
            worldCamera = targetCamera;
            healthBarPrefab = targetHealthBarPrefab;
            boardSprite = targetBoardSprite;
            if (healthBarPrefab != null)
            {
                missingHealthBarReported = false;
            }
            ConfigureCamera();
            CreateGeometry();
            UpdateLayout();
        }

        public void SyncUnits(IReadOnlyList<BoardUnit> units, StoneColor perspectiveSide)
        {
            if (!geometryCreated)
            {
                CreateGeometry();
            }

            currentUnits.Clear();
            foreach (BoardUnit unit in units)
            {
                currentUnits.Add(unit);
                if (!unitViews.TryGetValue(unit, out UnitView view))
                {
                    view = CreateUnitView(unit, false);
                    unitViews.Add(unit, view);
                    if (healthBarPrefab != null)
                    {
                        UnitHealthBarView healthBar = Instantiate(
                            healthBarPrefab,
                            inputRect,
                            false);
                        healthBar.name = $"Health_{unit.Definition.UnitId}_{unit.PlacementOrder}";
                        healthBar.Bind(unit, perspectiveSide);
                        healthBars.Add(unit, healthBar);
                    }
                    else if (!missingHealthBarReported)
                    {
                        missingHealthBarReported = true;
                        Debug.LogError(
                            "GomokuBoardView requires a UnitHealthBarView prefab.",
                            this);
                    }
                }

                view.transform.localPosition = CellToLocal(unit.X, unit.Y);
                UpdateHealthBar(unit);
            }

            var removedUnits = new List<BoardUnit>();
            foreach (KeyValuePair<BoardUnit, UnitView> pair in unitViews)
            {
                if (!currentUnits.Contains(pair.Key) && !pair.Value.IsDying)
                {
                    removedUnits.Add(pair.Key);
                }
            }

            foreach (BoardUnit removedUnit in removedUnits)
            {
                RemoveViewImmediately(removedUnit);
            }
        }

        public void SetPointerPresentation(
            BoardPointerState pointerState,
            UnitDefinitionSO placementDefinition,
            StoneColor playerSide)
        {
            HideHighlights();

            if (pointerState.Mode == BoardPointerMode.PlacementPreview
                && placementDefinition != null)
            {
                ShowRange(
                    pointerState.X,
                    pointerState.Y,
                    placementDefinition.Range,
                    PlayerRangeColor);
                ShowPreview(pointerState.X, pointerState.Y, placementDefinition, playerSide);
                return;
            }

            HidePreview();
            if (pointerState.Mode != BoardPointerMode.UnitHover
                || pointerState.HoveredUnit == null)
            {
                return;
            }

            BoardUnit hoveredUnit = pointerState.HoveredUnit;
            ShowRange(
                hoveredUnit.X,
                hoveredUnit.Y,
                hoveredUnit.Definition.Range,
                hoveredUnit.Side == playerSide ? PlayerRangeColor : EnemyRangeColor);
        }

        public void PlayCombatAction(CombatActionEvent actionEvent)
        {
            if (actionEvent == null)
            {
                return;
            }

            unitViews.TryGetValue(actionEvent.Actor, out UnitView actorView);
            UnitView firstTarget = null;
            if (actionEvent.Results.Count > 0)
            {
                unitViews.TryGetValue(actionEvent.Results[0].Target, out firstTarget);
            }

            actorView?.PlayAction(firstTarget);
            foreach (CombatEffectResult result in actionEvent.Results)
            {
                if (!unitViews.TryGetValue(result.Target, out UnitView targetView))
                {
                    continue;
                }

                if (healthBars.TryGetValue(result.Target, out UnitHealthBarView healthBar))
                {
                    healthBar.Refresh();
                }

                if (result.IsLethal)
                {
                    if (healthBar != null)
                    {
                        healthBar.gameObject.SetActive(false);
                    }

                    float timeout = result.Target.Definition.Presentation != null
                        ? result.Target.Definition.Presentation.DeathDuration
                        : 0.45f;
                    BoardUnit defeatedUnit = result.Target;
                    targetView.PlayDeath(timeout, () => RemoveViewImmediately(defeatedUnit));
                }
                else if (result.Kind == CombatEffectKind.Damage)
                {
                    targetView.PlayHit();
                }
                else
                {
                    targetView.PlayHeal();
                }
            }
        }

        public void PlayDamageAt(int x, int y)
        {
            UnitView view = FindViewAt(x, y);
            view?.PlayHit();
        }

        public void PlayHealAt(int x, int y)
        {
            UnitView view = FindViewAt(x, y);
            view?.PlayHeal();
        }

        public Vector3 CellToWorld(int x, int y)
        {
            return transform.TransformPoint(CellToLocal(x, y));
        }

        public void ClearTransientPresentation()
        {
            HidePreview();
            HideHighlights();
        }

        public void ClearAllUnits()
        {
            foreach (UnitView view in unitViews.Values)
            {
                DestroyComponentGameObject(view);
            }

            unitViews.Clear();
            foreach (UnitHealthBarView healthBar in healthBars.Values)
            {
                DestroyComponentGameObject(healthBar);
            }

            healthBars.Clear();
            ClearTransientPresentation();
        }

        private void LateUpdate()
        {
            UpdateLayout();
        }

        private void OnDestroy()
        {
            foreach (UnitHealthBarView healthBar in healthBars.Values)
            {
                DestroyComponentGameObject(healthBar);
            }

            healthBars.Clear();
            unitViews.Clear();
        }

        private void ConfigureCamera()
        {
            if (worldCamera == null)
            {
                return;
            }

            worldCamera.orthographic = true;
            if (Mathf.Abs(worldCamera.transform.position.z) < 0.1f)
            {
                Vector3 position = worldCamera.transform.position;
                position.z = -10f;
                worldCamera.transform.position = position;
            }

            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
        }

        private void CreateGeometry()
        {
            if (geometryCreated)
            {
                return;
            }

            geometryCreated = true;
            unitRoot = new GameObject("Units").transform;
            unitRoot.SetParent(transform, false);
            overlayRoot = new GameObject("Overlay").transform;
            overlayRoot.SetParent(transform, false);

            if (boardSprite != null)
            {
                SpriteRenderer authoredBoard = CreateRenderer(
                    transform,
                    "BoardBackground",
                    Color.white,
                    Vector3.one,
                    0,
                    "Board");
                authoredBoard.sprite = boardSprite;
                return;
            }

            CreateRenderer(
                transform,
                "BoardBackground",
                BoardColor,
                new Vector3(BoardVisualSize, BoardVisualSize, 1f),
                0,
                "Board");

            for (int index = 0; index < GomokuGame.BoardSize; index++)
            {
                float offset = index - GridHalfSize;
                SpriteRenderer vertical = CreateRenderer(
                    transform,
                    $"GridVertical_{index}",
                    GridColor,
                    new Vector3(0.025f, GridHalfSize * 2f, 1f),
                    1,
                    "Board");
                vertical.transform.localPosition = new Vector3(offset, 0f, 0f);

                SpriteRenderer horizontal = CreateRenderer(
                    transform,
                    $"GridHorizontal_{index}",
                    GridColor,
                    new Vector3(GridHalfSize * 2f, 0.025f, 1f),
                    1,
                    "Board");
                horizontal.transform.localPosition = new Vector3(0f, offset, 0f);
            }

            int[] starIndices = { 3, 7, 11 };
            foreach (int x in starIndices)
            {
                foreach (int y in starIndices)
                {
                    SpriteRenderer star = CreateRenderer(
                        transform,
                        $"Star_{x}_{y}",
                        GridColor,
                        new Vector3(0.13f, 0.13f, 1f),
                        2,
                        "Board");
                    star.sprite = WorldSpriteFactory.Circle;
                    star.transform.localPosition = CellToLocal(x, y);
                }
            }
        }

        private void UpdateLayout()
        {
            if (inputRect == null || worldCamera == null || worldCamera.pixelHeight <= 0)
            {
                return;
            }

            var corners = new Vector3[4];
            inputRect.GetWorldCorners(corners);
            Canvas canvas = inputRect.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(eventCamera, corners[2]);
            Vector2 screenCenter = (screenMin + screenMax) * 0.5f;
            float pixelSize = Mathf.Min(
                Mathf.Abs(screenMax.x - screenMin.x),
                Mathf.Abs(screenMax.y - screenMin.y));
            float cameraDistance = Mathf.Abs(worldCamera.transform.position.z);
            Vector3 worldCenter = worldCamera.ScreenToWorldPoint(
                new Vector3(screenCenter.x, screenCenter.y, cameraDistance));
            float worldUnitsPerPixel = worldCamera.orthographicSize * 2f / worldCamera.pixelHeight;
            float scale = pixelSize * worldUnitsPerPixel / BoardVisualSize;

            transform.position = new Vector3(worldCenter.x, worldCenter.y, 0f);
            transform.localScale = Vector3.one * scale;
        }

        private UnitView CreateUnitView(BoardUnit unit, bool isPreview)
        {
            UnitPresentationSO presentation = unit.Definition.Presentation;
            UnitView view;
            if (presentation != null && presentation.WorldPrefab != null)
            {
                view = Instantiate(presentation.WorldPrefab, unitRoot);
            }
            else
            {
                view = UnitView.CreateRuntimePlaceholder(unitRoot);
                if (Application.isPlaying
                    && reportedMissingPresentations.Add(unit.Definition.UnitId))
                {
                    Debug.LogWarning(
                        $"Unit '{unit.Definition.UnitId}' has no world presentation prefab. "
                        + "Using the runtime placeholder.",
                        this);
                }
            }

            view.name = $"{unit.Definition.UnitId}_{unit.PlacementOrder}";
            view.transform.localPosition = CellToLocal(unit.X, unit.Y);
            view.Bind(unit, presentation, isPreview);
            return view;
        }

        private void ShowPreview(
            int x,
            int y,
            UnitDefinitionSO definition,
            StoneColor side)
        {
            if (previewView == null || previewDefinition != definition)
            {
                HidePreview();
                var previewUnit = new BoardUnit(definition, side, x, y, int.MaxValue);
                previewDefinition = definition;
                previewView = CreateUnitView(previewUnit, true);
                previewView.name = $"Preview_{definition.UnitId}";
            }

            previewView.transform.localPosition = CellToLocal(x, y);
        }

        private void HidePreview()
        {
            if (previewView != null)
            {
                DestroyUnityObject(previewView.gameObject);
            }

            previewView = null;
            previewDefinition = null;
        }

        private void ShowRange(int originX, int originY, int range, Color color)
        {
            int highlightIndex = 0;
            for (int x = Mathf.Max(0, originX - range);
                x <= Mathf.Min(GomokuGame.BoardSize - 1, originX + range);
                x++)
            {
                for (int y = Mathf.Max(0, originY - range);
                    y <= Mathf.Min(GomokuGame.BoardSize - 1, originY + range);
                    y++)
                {
                    SpriteRenderer highlight = GetHighlight(highlightIndex++);
                    highlight.gameObject.SetActive(true);
                    highlight.color = color;
                    highlight.transform.localPosition = CellToLocal(x, y);
                }
            }
        }

        private SpriteRenderer GetHighlight(int index)
        {
            while (highlights.Count <= index)
            {
                SpriteRenderer highlight = CreateRenderer(
                    overlayRoot,
                    $"Range_{highlights.Count}",
                    PlayerRangeColor,
                    new Vector3(0.78f, 0.78f, 1f),
                    0,
                    "BoardOverlay");
                highlights.Add(highlight);
            }

            return highlights[index];
        }

        private void HideHighlights()
        {
            foreach (SpriteRenderer highlight in highlights)
            {
                highlight.gameObject.SetActive(false);
            }
        }

        private UnitView FindViewAt(int x, int y)
        {
            foreach (KeyValuePair<BoardUnit, UnitView> pair in unitViews)
            {
                if (pair.Key.X == x && pair.Key.Y == y)
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private void RemoveViewImmediately(BoardUnit unit)
        {
            if (!unitViews.TryGetValue(unit, out UnitView view))
            {
                return;
            }

            unitViews.Remove(unit);
            DestroyComponentGameObject(view);
            if (healthBars.TryGetValue(unit, out UnitHealthBarView healthBar))
            {
                healthBars.Remove(unit);
                DestroyComponentGameObject(healthBar);
            }
        }

        private void UpdateHealthBar(BoardUnit unit)
        {
            if (!healthBars.TryGetValue(unit, out UnitHealthBarView healthBar)
                || inputRect == null)
            {
                return;
            }

            Rect rect = inputRect.rect;
            float boardSize = Mathf.Min(rect.width, rect.height);
            float margin = boardSize * 0.045f;
            var gridRect = new Rect(
                -boardSize * 0.5f + margin,
                -boardSize * 0.5f + margin,
                boardSize - margin * 2f,
                boardSize - margin * 2f);
            float spacing = gridRect.width / (GomokuGame.BoardSize - 1);
            var cell = new Vector2(
                gridRect.xMin + unit.X * spacing,
                gridRect.yMin + unit.Y * spacing);
            healthBar.SetLayout(
                cell + Vector2.down * spacing * 0.48f,
                new Vector2(spacing * 0.82f, Mathf.Max(4f, spacing * 0.08f)));
            healthBar.Refresh();
        }

        private static SpriteRenderer CreateRenderer(
            Transform parent,
            string objectName,
            Color color,
            Vector3 scale,
            int order,
            string sortingLayer)
        {
            var target = new GameObject(objectName, typeof(SpriteRenderer));
            target.transform.SetParent(parent, false);
            target.transform.localScale = scale;
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = WorldSpriteFactory.Square;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Vector3 CellToLocal(int x, int y)
        {
            return new Vector3(x - GridHalfSize, y - GridHalfSize, 0f);
        }

        private static void DestroyUnityObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void DestroyComponentGameObject(Component target)
        {
            if (target == null)
            {
                return;
            }

            DestroyUnityObject(target.gameObject);
        }
    }
}
