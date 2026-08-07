using System;
using System.Collections;
using DamageNumbersPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class GomokuBoardView : MaskableGraphic, IPointerClickHandler
    {
        private static readonly Color BoardColor = new Color(0.78f, 0.57f, 0.30f);
        private static readonly Color GridColor = new Color(0.16f, 0.11f, 0.07f);

        [SerializeField] private DamageNumber attackDamagePopup;
        [SerializeField] private DamageNumber hitDamagePopup;
        [SerializeField] private DamageNumber healPopup;

        private GomokuGame game;
        private StoneColor playerSide = StoneColor.White;
        private Action<int, int> onIntersectionClicked;

        public void Bind(GomokuGame targetGame, StoneColor perspectiveSide, Action<int, int> clickHandler)
        {
            game = targetGame;
            playerSide = perspectiveSide;
            onIntersectionClicked = clickHandler;
            raycastTarget = true;
            SetVerticesDirty();
        }

        public void Refresh()
        {
            SetVerticesDirty();
        }

        public void ShowDamage(int x, int y, int damage, bool causedByPlayer)
        {
            GetGridMetrics(out Rect gridRect, out float spacing);
            Vector2 position = Intersection(gridRect, spacing, x, y);
            StartCoroutine(PlayDamageEffect(position, spacing));

            DamageNumber popup = causedByPlayer ? attackDamagePopup : hitDamagePopup;
            if (popup != null)
            {
                popup.SpawnGUI(rectTransform, position + Vector2.up * spacing * 0.35f, damage);
            }
        }

        public void ShowHeal(int x, int y, int healing)
        {
            GetGridMetrics(out Rect gridRect, out float spacing);
            Vector2 position = Intersection(gridRect, spacing, x, y);
            if (healPopup != null)
            {
                healPopup.SpawnGUI(rectTransform, position + Vector2.up * spacing * 0.35f, healing);
            }
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

            GetGridMetrics(out Rect gridRect, out float spacing);
            int x = Mathf.RoundToInt((localPoint.x - gridRect.xMin) / spacing);
            int y = Mathf.RoundToInt((localPoint.y - gridRect.yMin) / spacing);

            if (x < 0 || x >= GomokuGame.BoardSize || y < 0 || y >= GomokuGame.BoardSize)
            {
                return;
            }

            Vector2 intersection = new Vector2(gridRect.xMin + x * spacing, gridRect.yMin + y * spacing);
            if (Vector2.Distance(localPoint, intersection) <= spacing * 0.46f)
            {
                onIntersectionClicked?.Invoke(x, y);
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            GetGridMetrics(out Rect gridRect, out float spacing);

            float boardSize = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height);
            AddQuad(vertexHelper, new Rect(-boardSize * 0.5f, -boardSize * 0.5f, boardSize, boardSize), BoardColor);

            for (int index = 0; index < GomokuGame.BoardSize; index++)
            {
                float offset = index * spacing;
                AddQuad(vertexHelper, new Rect(gridRect.xMin + offset - 1f, gridRect.yMin, 2f, gridRect.height), GridColor);
                AddQuad(vertexHelper, new Rect(gridRect.xMin, gridRect.yMin + offset - 1f, gridRect.width, 2f), GridColor);
            }

            int[] starIndices = { 3, 7, 11 };
            foreach (int x in starIndices)
            {
                foreach (int y in starIndices)
                {
                    AddCircle(vertexHelper, Intersection(gridRect, spacing, x, y), Mathf.Max(3f, spacing * 0.09f), GridColor, 12);
                }
            }

            if (game == null)
            {
                return;
            }

            foreach (BoardUnit unit in game.Units)
            {
                DrawUnit(vertexHelper, gridRect, spacing, unit);
            }
        }

        private void DrawUnit(VertexHelper vertexHelper, Rect gridRect, float spacing, BoardUnit unit)
        {
            Vector2 center = Intersection(gridRect, spacing, unit.X, unit.Y);
            float radius = spacing * 0.41f;
            Color outer = unit.Side == StoneColor.Black ? new Color(0.03f, 0.03f, 0.04f) : new Color(0.1f, 0.1f, 0.1f);
            Color inner = unit.Side == StoneColor.Black ? new Color(0.08f, 0.08f, 0.1f) : new Color(0.95f, 0.95f, 0.92f);

            AddCircle(vertexHelper, center, radius, outer, 24);
            AddCircle(vertexHelper, center, radius * 0.88f, inner, 24);
            AddCircle(vertexHelper, center, radius * 0.30f, unit.Definition.RoleColor, 16);

            float healthRatio = (float)unit.CurrentHealth / unit.Definition.MaxHealth;
            Rect healthBack = new Rect(center.x - radius, center.y - radius - 5f, radius * 2f, 4f);
            AddQuad(vertexHelper, healthBack, new Color(0.12f, 0.12f, 0.12f));
            AddQuad(
                vertexHelper,
                new Rect(healthBack.x, healthBack.y, healthBack.width * healthRatio, healthBack.height),
                unit.Side == playerSide
                    ? new Color(0.25f, 0.85f, 0.34f)
                    : new Color(0.92f, 0.18f, 0.12f));

            if (unit.X == game.LastMoveX && unit.Y == game.LastMoveY)
            {
                AddCircle(vertexHelper, center, Mathf.Max(2.5f, radius * 0.09f), Color.red, 10);
            }
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

        private static Vector2 Intersection(Rect gridRect, float spacing, int x, int y)
        {
            return new Vector2(gridRect.xMin + x * spacing, gridRect.yMin + y * spacing);
        }

        private IEnumerator PlayDamageEffect(Vector2 center, float spacing)
        {
            const int ParticleCount = 8;
            const float Duration = 0.32f;
            var particles = new RectTransform[ParticleCount];
            var images = new Image[ParticleCount];
            var directions = new Vector2[ParticleCount];

            for (int index = 0; index < ParticleCount; index++)
            {
                float angle = Mathf.PI * 2f * index / ParticleCount;
                directions[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                var particle = new GameObject(
                    "DamageParticle",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                particle.layer = gameObject.layer;
                RectTransform particleRect = particle.GetComponent<RectTransform>();
                particleRect.SetParent(rectTransform, false);
                particleRect.anchoredPosition = center;
                particleRect.sizeDelta = Vector2.one * Mathf.Max(4f, spacing * 0.14f);

                Image image = particle.GetComponent<Image>();
                image.color = new Color(1f, 0.42f, 0.06f);
                image.raycastTarget = false;
                particles[index] = particleRect;
                images[index] = image;
            }

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / Duration);
                float distance = Mathf.Lerp(
                    spacing * 0.08f,
                    spacing * 0.65f,
                    1f - (1f - progress) * (1f - progress));

                for (int index = 0; index < ParticleCount; index++)
                {
                    particles[index].anchoredPosition = center + directions[index] * distance;
                    particles[index].localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, progress);
                    Color color = images[index].color;
                    color.a = 1f - progress;
                    images[index].color = color;
                }

                yield return null;
            }

            foreach (RectTransform particle in particles)
            {
                if (particle != null)
                {
                    Destroy(particle.gameObject);
                }
            }
        }

        private static void AddQuad(VertexHelper vertexHelper, Rect rect, Color color)
        {
            int start = vertexHelper.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
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

        private static void AddCircle(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            Color color,
            int segments)
        {
            int start = vertexHelper.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = center;
            vertexHelper.AddVert(vertex);

            for (int index = 0; index <= segments; index++)
            {
                float angle = Mathf.PI * 2f * index / segments;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vertexHelper.AddVert(vertex);
            }

            for (int index = 0; index < segments; index++)
            {
                vertexHelper.AddTriangle(start, start + index + 1, start + index + 2);
            }
        }
    }
}
