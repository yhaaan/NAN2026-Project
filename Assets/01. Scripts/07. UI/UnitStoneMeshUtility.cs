using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    internal static class UnitStoneMeshUtility
    {
        public static void AddStone(
            VertexHelper vertexHelper,
            Vector2 center,
            float radius,
            StoneColor side,
            Color roleColor,
            bool translucent)
        {
            Color outer = side == StoneColor.Black
                ? new Color(0.03f, 0.03f, 0.04f)
                : new Color(0.1f, 0.1f, 0.1f);
            Color inner = side == StoneColor.Black
                ? new Color(0.08f, 0.08f, 0.1f)
                : new Color(0.95f, 0.95f, 0.92f);

            if (translucent)
            {
                outer.a = 0.5f;
                inner.a = 0.55f;
                roleColor.a = 0.65f;
            }

            AddCircle(vertexHelper, center, radius, outer, 24);
            AddCircle(vertexHelper, center, radius * 0.88f, inner, 24);
            AddCircle(vertexHelper, center, radius * 0.30f, roleColor, 16);
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
