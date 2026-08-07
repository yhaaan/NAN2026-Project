using UnityEngine;
using UnityEngine.InputSystem;

namespace NAN2026.Gomoku
{
    internal static class UiPointerInputSource
    {
        public static bool TryGetScreenPosition(out Vector2 screenPosition)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                screenPosition = default;
                return false;
            }

            screenPosition = pointer.position.ReadValue();
            return true;
        }
    }
}
