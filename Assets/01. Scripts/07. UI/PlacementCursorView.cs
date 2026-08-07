using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlacementCursorView : MaskableGraphic
    {
        [SerializeField] private GomokuBoardView boardView;
        [SerializeField] private RectTransform shopRect;
        [SerializeField] private GameObject resultPanel;

        private RectTransform hudRect;
        private Canvas rootCanvas;
        private UnitDefinitionSO selectedDefinition;
        private StoneColor playerSide = StoneColor.White;
        private bool cursorVisible;

        public bool IsCursorVisible => cursorVisible;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            CacheCanvasReferences();
            HideCursor();
        }

        protected override void OnDisable()
        {
            boardView?.ClearPointerPosition();
            cursorVisible = false;
            base.OnDisable();
        }

        private void LateUpdate()
        {
            if (!UiPointerInputSource.TryGetScreenPosition(out Vector2 screenPosition))
            {
                ClearPointerPresentation();
                return;
            }

            UpdatePointerPresentation(screenPosition);
        }

        public void SetSelection(UnitDefinitionSO definition, StoneColor side)
        {
            selectedDefinition = definition;
            playerSide = side;

            if (selectedDefinition == null)
            {
                HideCursor();
            }
        }

        public void UpdatePointerPresentation(Vector2 screenPosition)
        {
            CacheCanvasReferences();
            Camera eventCamera = GetEventCamera();
            bool pointerOverShop = shopRect != null
                && shopRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(shopRect, screenPosition, eventCamera);
            bool resultVisible = resultPanel != null && resultPanel.activeInHierarchy;

            if (pointerOverShop || resultVisible)
            {
                boardView?.ClearPointerPosition();
            }
            else
            {
                boardView?.UpdatePointerPosition(screenPosition);
            }

            if (selectedDefinition == null
                || shopRect == null
                || !shopRect.gameObject.activeInHierarchy
                || pointerOverShop
                || resultVisible
                || boardView == null
                || boardView.PointerState.Mode == BoardPointerMode.PlacementPreview)
            {
                HideCursor();
                return;
            }

            if (hudRect == null
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    hudRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                HideCursor();
                return;
            }

            ShowCursor(localPoint, boardView.PlacementPreviewDiameter);
        }

        public void ClearPointerPresentation()
        {
            boardView?.ClearPointerPosition();
            HideCursor();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!cursorVisible || selectedDefinition == null)
            {
                return;
            }

            Vector2 center = rectTransform.rect.center;
            float radius = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.5f;
            UnitStoneMeshUtility.AddStone(
                vertexHelper,
                center,
                radius,
                playerSide,
                selectedDefinition.RoleColor,
                true);
        }

        private void ShowCursor(Vector2 anchoredPosition, float diameter)
        {
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = Vector2.one * diameter;
            cursorVisible = true;
            SetVerticesDirty();
        }

        private void HideCursor()
        {
            if (!cursorVisible)
            {
                return;
            }

            cursorVisible = false;
            SetVerticesDirty();
        }

        private void CacheCanvasReferences()
        {
            if (hudRect == null)
            {
                hudRect = transform.parent as RectTransform;
            }

            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }
        }

        private Camera GetEventCamera()
        {
            return rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
        }
    }
}
