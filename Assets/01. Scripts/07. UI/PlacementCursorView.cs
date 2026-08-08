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
        private Image spritePreview;
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
            if (boardView != null)
            {
                boardView.ClearPointerPosition();
            }

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
            UpdateSpritePreview();

            if (selectedDefinition == null)
            {
                HideCursor();
            }
        }

        public void UpdatePointerPresentation(Vector2 screenPosition)
        {
            CacheCanvasReferences();
            if (boardView == null)
            {
                HideCursor();
                return;
            }

            Camera eventCamera = GetEventCamera();
            bool pointerOverShop = shopRect != null
                && shopRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(shopRect, screenPosition, eventCamera);
            bool resultVisible = resultPanel != null && resultPanel.activeInHierarchy;

            if (pointerOverShop || resultVisible)
            {
                boardView.ClearPointerPosition();
            }
            else
            {
                boardView.UpdatePointerPosition(screenPosition);
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
            if (boardView != null)
            {
                boardView.ClearPointerPosition();
            }

            HideCursor();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!cursorVisible || selectedDefinition == null)
            {
                return;
            }

            if (TryGetAuthoredBody(out _, out _))
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
            UpdateSpritePreview();
            SetVerticesDirty();
        }

        private void HideCursor()
        {
            bool wasVisible = cursorVisible;
            cursorVisible = false;
            if (spritePreview != null)
            {
                spritePreview.gameObject.SetActive(false);
            }

            if (wasVisible)
            {
                SetVerticesDirty();
            }
        }

        private void UpdateSpritePreview()
        {
            if (!cursorVisible || !TryGetAuthoredBody(out SpriteRenderer renderer, out Transform body))
            {
                if (spritePreview != null)
                {
                    spritePreview.gameObject.SetActive(false);
                }

                return;
            }

            EnsureSpritePreview();
            spritePreview.gameObject.SetActive(true);
            spritePreview.sprite = renderer.sprite;
            spritePreview.color = WithAlpha(renderer.color, 0.48f);
            spritePreview.preserveAspect = true;

            RectTransform imageRect = spritePreview.rectTransform;
            Vector2 spriteSize = renderer.sprite.bounds.size;
            float largestDimension = Mathf.Max(spriteSize.x, spriteSize.y);
            float normalization = 1f;
            UnitView prefab = selectedDefinition.Presentation.WorldPrefab;
            if (prefab.NormalizeSpriteSize && largestDimension > Mathf.Epsilon)
            {
                normalization = prefab.VisualDiameter / largestDimension;
            }

            float containerWorldDiameter = boardView.PlacementPreviewWorldDiameter;
            float containerScale = largestDimension > Mathf.Epsilon
                ? normalization * largestDimension / containerWorldDiameter
                : 1f;
            imageRect.localScale = new Vector3(
                body.localScale.x * containerScale,
                body.localScale.y * containerScale,
                1f);
            imageRect.localRotation = body.localRotation;
            imageRect.anchoredPosition = new Vector2(body.localPosition.x, body.localPosition.y)
                * (rectTransform.rect.width / containerWorldDiameter);
        }

        private bool TryGetAuthoredBody(out SpriteRenderer renderer, out Transform body)
        {
            renderer = null;
            body = null;
            UnitView prefab = selectedDefinition != null
                ? selectedDefinition.Presentation?.WorldPrefab
                : null;
            if (prefab == null || prefab.BodyRenderer == null || prefab.BodyRenderer.sprite == null)
            {
                return false;
            }

            renderer = prefab.BodyRenderer;
            body = prefab.BodyRoot != null ? prefab.BodyRoot : renderer.transform;
            return true;
        }

        private void EnsureSpritePreview()
        {
            if (spritePreview != null)
            {
                return;
            }

            var previewObject = new GameObject(
                "SpritePreview",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            previewObject.layer = gameObject.layer;
            previewObject.transform.SetParent(transform, false);
            spritePreview = previewObject.GetComponent<Image>();
            spritePreview.raycastTarget = false;

            RectTransform imageRect = spritePreview.rectTransform;
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
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
