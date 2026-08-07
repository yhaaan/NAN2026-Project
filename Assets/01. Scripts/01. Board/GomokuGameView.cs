using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class GomokuGameView : MonoBehaviour
    {
        private const float HeaderHeight = 58f;
        private const float FooterHeight = 48f;

        private static readonly Color BackgroundColor = new Color(0.11f, 0.12f, 0.14f);
        private static readonly Color BoardColor = new Color(0.78f, 0.57f, 0.30f);
        private static readonly Color GridColor = new Color(0.16f, 0.11f, 0.07f);

        private GomokuGame game;
        private Texture2D circleTexture;
        private GUIStyle statusStyle;
        private GUIStyle hintStyle;

        private void Awake()
        {
            game = new GomokuGame();
            circleTexture = CreateCircleTexture(64);
        }

        private void OnDestroy()
        {
            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), BackgroundColor);

            Rect boardRect = CalculateBoardRect();
            Rect gridRect = CalculateGridRect(boardRect);

            DrawHeader();
            DrawBoard(boardRect, gridRect);
            HandleBoardInput(gridRect);

            GUI.Label(
                new Rect(16f, Screen.height - FooterHeight + 8f, Screen.width - 32f, 28f),
                game.IsGameOver ? "Press Restart to play again." : "Click an empty intersection to place a stone.",
                hintStyle);
        }

        private void DrawHeader()
        {
            string status = game.IsGameOver
                ? $"{GetColorName(game.Winner)} wins!"
                : $"{GetColorName(game.CurrentTurn)}'s turn";

            GUI.Label(new Rect(120f, 10f, Screen.width - 240f, 38f), status, statusStyle);

            if (GUI.Button(new Rect(Screen.width - 108f, 13f, 92f, 32f), "Restart"))
            {
                game.Restart();
            }
        }

        private void DrawBoard(Rect boardRect, Rect gridRect)
        {
            DrawSolidRect(boardRect, BoardColor);

            float spacing = gridRect.width / (GomokuGame.BoardSize - 1);
            for (int index = 0; index < GomokuGame.BoardSize; index++)
            {
                float offset = index * spacing;
                DrawSolidRect(new Rect(gridRect.x + offset - 1f, gridRect.y, 2f, gridRect.height), GridColor);
                DrawSolidRect(new Rect(gridRect.x, gridRect.y + offset - 1f, gridRect.width, 2f), GridColor);
            }

            DrawStarPoints(gridRect, spacing);
            DrawPlacedStones(gridRect, spacing);
            DrawHoverStone(gridRect, spacing);
        }

        private void DrawStarPoints(Rect gridRect, float spacing)
        {
            int[] starIndices = { 3, 7, 11 };
            const float starSize = 7f;

            foreach (int x in starIndices)
            {
                foreach (int y in starIndices)
                {
                    Rect starRect = CenteredRect(
                        gridRect.x + x * spacing,
                        gridRect.y + y * spacing,
                        starSize);
                    DrawTintedTexture(starRect, circleTexture, GridColor);
                }
            }
        }

        private void DrawPlacedStones(Rect gridRect, float spacing)
        {
            float stoneSize = spacing * 0.82f;

            for (int x = 0; x < GomokuGame.BoardSize; x++)
            {
                for (int y = 0; y < GomokuGame.BoardSize; y++)
                {
                    StoneColor stone = game.GetStone(x, y);
                    if (stone == StoneColor.None)
                    {
                        continue;
                    }

                    Vector2 center = GetIntersectionPosition(gridRect, spacing, x, y);
                    DrawStone(center, stoneSize, stone, 1f);

                    if (x == game.LastMoveX && y == game.LastMoveY)
                    {
                        float markerSize = Mathf.Max(3f, stoneSize * 0.12f);
                        DrawTintedTexture(
                            CenteredRect(center.x, center.y, markerSize),
                            circleTexture,
                            stone == StoneColor.Black ? Color.white : Color.black);
                    }
                }
            }
        }

        private void DrawHoverStone(Rect gridRect, float spacing)
        {
            if (game.IsGameOver
                || !TryGetIntersection(Event.current.mousePosition, gridRect, spacing, out int x, out int y)
                || game.GetStone(x, y) != StoneColor.None)
            {
                return;
            }

            Vector2 center = GetIntersectionPosition(gridRect, spacing, x, y);
            DrawStone(center, spacing * 0.82f, game.CurrentTurn, 0.42f);
        }

        private void HandleBoardInput(Rect gridRect)
        {
            Event currentEvent = Event.current;
            if (game.IsGameOver || currentEvent.type != EventType.MouseDown || currentEvent.button != 0)
            {
                return;
            }

            float spacing = gridRect.width / (GomokuGame.BoardSize - 1);
            if (TryGetIntersection(currentEvent.mousePosition, gridRect, spacing, out int x, out int y)
                && game.TryPlace(x, y))
            {
                currentEvent.Use();
            }
        }

        private void DrawStone(Vector2 center, float size, StoneColor stone, float alpha)
        {
            Color stoneColor = stone == StoneColor.Black
                ? new Color(0.05f, 0.05f, 0.06f, alpha)
                : new Color(0.95f, 0.95f, 0.92f, alpha);

            Rect outerRect = CenteredRect(center.x, center.y, size);
            DrawTintedTexture(outerRect, circleTexture, new Color(0f, 0f, 0f, alpha));

            float inset = Mathf.Max(1f, size * 0.055f);
            Rect innerRect = new Rect(
                outerRect.x + inset,
                outerRect.y + inset,
                outerRect.width - inset * 2f,
                outerRect.height - inset * 2f);
            DrawTintedTexture(innerRect, circleTexture, stoneColor);
        }

        private static Rect CalculateBoardRect()
        {
            float availableWidth = Mathf.Max(140f, Screen.width - 32f);
            float availableHeight = Mathf.Max(140f, Screen.height - HeaderHeight - FooterHeight);
            float size = Mathf.Min(availableWidth, availableHeight);
            return new Rect((Screen.width - size) * 0.5f, HeaderHeight, size, size);
        }

        private static Rect CalculateGridRect(Rect boardRect)
        {
            float margin = boardRect.width * 0.045f;
            return new Rect(
                boardRect.x + margin,
                boardRect.y + margin,
                boardRect.width - margin * 2f,
                boardRect.height - margin * 2f);
        }

        private static bool TryGetIntersection(
            Vector2 mousePosition,
            Rect gridRect,
            float spacing,
            out int x,
            out int y)
        {
            x = Mathf.RoundToInt((mousePosition.x - gridRect.x) / spacing);
            y = Mathf.RoundToInt((mousePosition.y - gridRect.y) / spacing);

            if (x < 0 || x >= GomokuGame.BoardSize || y < 0 || y >= GomokuGame.BoardSize)
            {
                return false;
            }

            Vector2 intersection = GetIntersectionPosition(gridRect, spacing, x, y);
            return Vector2.Distance(mousePosition, intersection) <= spacing * 0.46f;
        }

        private static Vector2 GetIntersectionPosition(Rect gridRect, float spacing, int x, int y)
        {
            return new Vector2(gridRect.x + x * spacing, gridRect.y + y * spacing);
        }

        private static Rect CenteredRect(float centerX, float centerY, float size)
        {
            return new Rect(centerX - size * 0.5f, centerY - size * 0.5f, size, size);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            DrawTintedTexture(rect, Texture2D.whiteTexture, color);
        }

        private static void DrawTintedTexture(Rect rect, Texture texture, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture);
            GUI.color = previousColor;
        }

        private static string GetColorName(StoneColor color)
        {
            return color == StoneColor.Black ? "Black" : "White";
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[size * size];
            float radius = size * 0.5f;
            Vector2 center = new Vector2(radius - 0.5f, radius - 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void EnsureStyles()
        {
            if (statusStyle != null)
            {
                return;
            }

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                normal = { textColor = new Color(0.8f, 0.82f, 0.86f) }
            };
        }
    }

    internal static class GomokuBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureViewExists()
        {
            if (Object.FindFirstObjectByType<GomokuGameView>() != null)
            {
                return;
            }

            var gameObject = new GameObject(nameof(GomokuGameView));
            gameObject.AddComponent<GomokuGameView>();
        }
    }
}
