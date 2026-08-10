using System;
using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GomokuBoardView : MaskableGraphic, IPointerClickHandler
    {
        private const int DebugPopupMinAmount = 10;
        private const int DebugPopupMaxAmountExclusive = 101;

        [SerializeField] private DamageNumber attackDamagePopup;
        [SerializeField] private DamageNumber hitDamagePopup;
        [SerializeField] private DamageNumber healPopup;
        [SerializeField] private UnitHealthBarView healthBarPrefab;
        [SerializeField] private AudioClip[] hitSfx = Array.Empty<AudioClip>();
        [SerializeField] private Sprite boardSprite;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private BoardWorldView worldView;

        private GomokuGame game;
        private StoneColor playerSide = StoneColor.White;
        private Action<int, int> onIntersectionClicked;
        private Canvas rootCanvas;
        private UnitDefinitionSO placementPreviewDefinition;
        private BoardPointerState pointerState = BoardPointerState.None;
        private bool debugPopupModeEnabled;
        private bool ownsWorldView;

        public BoardPointerState PointerState => pointerState;
        public BoardWorldView WorldView => worldView;
        public IReadOnlyList<AudioClip> HitSfx => hitSfx ?? Array.Empty<AudioClip>();
        public float PlacementPreviewWorldDiameter => 0.82f;
        public float PlacementPreviewDiameter
        {
            get
            {
                GetGridMetrics(out _, out float spacing);
                return spacing * PlacementPreviewWorldDiameter;
            }
        }

        public void Bind(GomokuGame targetGame, StoneColor perspectiveSide, Action<int, int> clickHandler)
        {
            game = targetGame;
            playerSide = perspectiveSide;
            onIntersectionClicked = clickHandler;
            rootCanvas = GetComponentInParent<Canvas>();
            placementPreviewDefinition = null;
            pointerState = BoardPointerState.None;
            raycastTarget = true;
            EnsureWorldView();
            worldView?.ClearAllUnits();
            worldView?.SyncUnits(game.Units, playerSide);
            UpdateWorldPointerPresentation();
            SetVerticesDirty();
        }

        public void Refresh()
        {
            if (game == null)
            {
                return;
            }

            if (pointerState.Mode == BoardPointerMode.UnitHover)
            {
                BoardUnit hoveredUnit = pointerState.HoveredUnit;
                if (hoveredUnit == null
                    || !hoveredUnit.IsAlive
                    || game.GetUnit(hoveredUnit.X, hoveredUnit.Y) != hoveredUnit)
                {
                    pointerState = BoardPointerState.None;
                }
            }
            else if (pointerState.Mode == BoardPointerMode.PlacementPreview
                && (placementPreviewDefinition == null
                    || game.CurrentTurn != playerSide
                    || !game.CanPlace(
                        pointerState.X,
                        pointerState.Y,
                        placementPreviewDefinition)))
            {
                pointerState = BoardPointerState.None;
            }

            EnsureWorldView();
            worldView?.SyncUnits(game.Units, playerSide);
            UpdateWorldPointerPresentation();
        }

        public void PlayPlacementImpact()
        {
            worldView?.PlayPlacementImpact();
        }

        public void PrepareVictory()
        {
            raycastTarget = false;
            placementPreviewDefinition = null;
            pointerState = BoardPointerState.None;
            worldView?.PrepareVictory();
            UpdateWorldPointerPresentation();
        }

        public void PlayVictoryStone(BoardUnit unit, bool finalStone)
        {
            worldView?.PlayVictoryStone(unit, finalStone);
        }

        public void RevealVictory(IReadOnlyList<BoardUnit> winningUnits, float duration)
        {
            worldView?.RevealVictory(winningUnits, duration);
        }

        public void SetPlacementPreview(UnitDefinitionSO definition)
        {
            if (placementPreviewDefinition == definition)
            {
                return;
            }

            placementPreviewDefinition = definition;
            ClearPointerState();
        }

        public void PlayCombatAction(CombatActionEvent actionEvent)
        {
            EnsureWorldView();
            if (worldView == null)
            {
                PresentCombatResults(actionEvent);
                return;
            }

            worldView.PlayCombatAction(actionEvent, () => PresentCombatResults(actionEvent));
        }

        private void PresentCombatResults(CombatActionEvent actionEvent)
        {
            foreach (CombatEffectResult result in actionEvent.Results)
            {
                if (result.Kind == CombatEffectKind.Damage)
                {
                    ShowDamage(
                        result.Target.X,
                        result.Target.Y,
                        result.Amount,
                        actionEvent.Actor.Side == playerSide,
                        false);
                }
                else
                {
                    ShowHeal(result.Target.X, result.Target.Y, result.Amount, false);
                }
            }

            Refresh();
        }

        public void ShowDamage(int x, int y, int damage, bool causedByPlayer)
        {
            ShowDamage(x, y, damage, causedByPlayer, true);
        }

        public void ShowHeal(int x, int y, int healing)
        {
            ShowHeal(x, y, healing, true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (game == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            if (TryGetIntersection(localPoint, out int x, out int y))
            {
                onIntersectionClicked?.Invoke(x, y);
            }
        }

        public void UpdatePointerPosition(Vector2 screenPosition)
        {
            UpdatePointerState(screenPosition);
        }

        public void ClearPointerPosition()
        {
            ClearPointerState();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                debugPopupModeEnabled = !debugPopupModeEnabled;
                Debug.Log($"Damage popup debug mode: {(debugPopupModeEnabled ? "ON" : "OFF")}", this);
            }

            if (!debugPopupModeEnabled || !UiPointerInputSource.TryGetScreenPosition(out Vector2 screenPosition))
            {
                return;
            }

            DamageNumber popup = null;
            if (keyboard.qKey.wasPressedThisFrame)
            {
                popup = attackDamagePopup;
            }
            else if (keyboard.wKey.wasPressedThisFrame)
            {
                popup = hitDamagePopup;
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                popup = healPopup;
            }

            if (popup != null)
            {
                SpawnDebugPopup(popup, screenPosition);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            AddTransparentQuad(vertexHelper, rectTransform.rect);
        }

        protected override void OnDestroy()
        {
            if (ownsWorldView && worldView != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(worldView.gameObject);
                }
                else
                {
                    DestroyImmediate(worldView.gameObject);
                }
            }

            base.OnDestroy();
        }

        private void ShowDamage(
            int x,
            int y,
            int damage,
            bool causedByPlayer,
            bool playWorldFeedback)
        {
            PlayRandomHitSfx();
            GetGridMetrics(out Rect gridRect, out float spacing);
            Vector2 position = Intersection(gridRect, spacing, x, y);
            if (playWorldFeedback)
            {
                EnsureWorldView();
                worldView?.PlayDamageAt(x, y);
            }

            DamageNumber popup = causedByPlayer ? attackDamagePopup : hitDamagePopup;
            if (popup != null)
            {
                popup.SpawnGUI(rectTransform, position + Vector2.up * spacing * 0.35f, damage);
            }
        }

        private void PlayRandomHitSfx()
        {
            if (hitSfx == null || hitSfx.Length == 0)
            {
                return;
            }

            int validClipCount = 0;
            foreach (AudioClip clip in hitSfx)
            {
                if (clip != null)
                {
                    validClipCount++;
                }
            }

            if (validClipCount == 0)
            {
                return;
            }

            int selectedClipIndex = UnityEngine.Random.Range(0, validClipCount);
            foreach (AudioClip clip in hitSfx)
            {
                if (clip == null)
                {
                    continue;
                }

                if (selectedClipIndex-- == 0)
                {
                    SoundManager.Instance.PlaySfx(clip);
                    return;
                }
            }
        }

        private void ShowHeal(int x, int y, int healing, bool playWorldFeedback)
        {
            GetGridMetrics(out Rect gridRect, out float spacing);
            Vector2 position = Intersection(gridRect, spacing, x, y);
            if (playWorldFeedback)
            {
                EnsureWorldView();
                worldView?.PlayHealAt(x, y);
            }

            if (healPopup != null)
            {
                healPopup.SpawnGUI(rectTransform, position + Vector2.up * spacing * 0.35f, healing);
            }
        }

        private void SpawnDebugPopup(DamageNumber popup, Vector2 screenPosition)
        {
            Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
            {
                return;
            }

            int amount = UnityEngine.Random.Range(DebugPopupMinAmount, DebugPopupMaxAmountExclusive);
            popup.SpawnGUI(rectTransform, localPosition, amount);
        }

        private void UpdatePointerState(Vector2 screenPosition)
        {
            Camera eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
            if (game == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint)
                || !TryGetIntersection(localPoint, out int x, out int y))
            {
                ClearPointerState();
                return;
            }

            if (placementPreviewDefinition != null)
            {
                bool canPreview = game.CurrentTurn == playerSide
                    && game.CanPlace(x, y, placementPreviewDefinition);
                if (canPreview)
                {
                    SetPointerState(BoardPointerState.ForPlacement(x, y));
                    return;
                }

                SetPointerState(BoardPointerState.ForUnit(game.GetUnit(x, y)));
                return;
            }

            SetPointerState(BoardPointerState.ForUnit(game.GetUnit(x, y)));
        }

        private void SetPointerState(BoardPointerState nextState)
        {
            if (pointerState.Equals(nextState))
            {
                return;
            }

            pointerState = nextState;
            UpdateWorldPointerPresentation();
        }

        private void ClearPointerState()
        {
            SetPointerState(BoardPointerState.None);
        }

        private void UpdateWorldPointerPresentation()
        {
            worldView?.SetPointerPresentation(
                pointerState,
                placementPreviewDefinition,
                playerSide);
        }

        private bool TryGetIntersection(Vector2 localPoint, out int x, out int y)
        {
            GetGridMetrics(out Rect gridRect, out float spacing);
            x = Mathf.RoundToInt((localPoint.x - gridRect.xMin) / spacing);
            y = Mathf.RoundToInt((localPoint.y - gridRect.yMin) / spacing);

            if (x < 0 || x >= GomokuGame.BoardSize || y < 0 || y >= GomokuGame.BoardSize)
            {
                return false;
            }

            Vector2 intersection = Intersection(gridRect, spacing, x, y);
            return Vector2.Distance(localPoint, intersection) <= spacing * 0.46f;
        }

        private void GetGridMetrics(out Rect gridRect, out float spacing)
        {
            Rect rect = rectTransform.rect;
            float boardSize = Mathf.Min(rect.width, rect.height);
            float margin = boardSize * 0.045f;
            gridRect = new Rect(
                -boardSize * 0.5f + margin,
                -boardSize * 0.5f + margin,
                boardSize - margin * 2f,
                boardSize - margin * 2f);
            spacing = gridRect.width / (GomokuGame.BoardSize - 1);
        }

        private void EnsureWorldView()
        {
            if (worldView == null)
            {
                var worldObject = new GameObject("BoardWorld", typeof(BoardWorldView));
                worldView = worldObject.GetComponent<BoardWorldView>();
                ownsWorldView = true;
            }

            worldView.Initialize(
                rectTransform,
                Camera.main,
                healthBarPrefab,
                boardSprite,
                backgroundSprite);
        }

        private static Vector2 Intersection(Rect gridRect, float spacing, int x, int y)
        {
            return new Vector2(gridRect.xMin + x * spacing, gridRect.yMin + y * spacing);
        }

        private static void AddTransparentQuad(VertexHelper vertexHelper, Rect rect)
        {
            int start = vertexHelper.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = Color.clear;
            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vertexHelper.AddVert(vertex);
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
