using UnityEngine;

namespace NAN2026.Gomoku
{
    [DefaultExecutionOrder(1000)]
    public sealed class PauseMenuBoardInputBlocker : MonoBehaviour
    {
        [SerializeField] private AnimatedPauseMenuController pauseMenu;
        [SerializeField] private RectTransform settingsButtonRect;
        [SerializeField] private GomokuBoardView boardView;

        private Canvas rootCanvas;
        private bool isBlockingBoardInput;
        private bool boardRaycastStateBeforeBlock;

        public bool IsBlockingBoardInput => isBlockingBoardInput;

        private void LateUpdate()
        {
            CacheReferences();
            if (boardView == null)
            {
                return;
            }

            if (pauseMenu != null && pauseMenu.IsOpen)
            {
                SetBlocking(true);
                return;
            }

            if (!UiPointerInputSource.TryGetScreenPosition(out Vector2 screenPosition))
            {
                SetBlocking(false);
                return;
            }

            EvaluatePointer(screenPosition);
        }

        private void OnDisable()
        {
            RestoreBoardInput();
        }

        private void OnDestroy()
        {
            RestoreBoardInput();
        }

        public void EvaluatePointer(Vector2 screenPosition)
        {
            CacheReferences();
            bool pointerOverSettings = settingsButtonRect != null
                && settingsButtonRect.gameObject.activeInHierarchy
                && RectTransformUtility.RectangleContainsScreenPoint(
                    settingsButtonRect,
                    screenPosition,
                    GetEventCamera());
            bool menuOpen = pauseMenu != null && pauseMenu.IsOpen;
            SetBlocking(pointerOverSettings || menuOpen);
        }

        private void SetBlocking(bool blocked)
        {
            if (boardView == null)
            {
                isBlockingBoardInput = false;
                return;
            }

            if (blocked)
            {
                if (!isBlockingBoardInput)
                {
                    boardRaycastStateBeforeBlock = boardView.raycastTarget;
                }

                isBlockingBoardInput = true;
                boardView.raycastTarget = false;
                boardView.ClearPointerPosition();
                return;
            }

            RestoreBoardInput();
        }

        private void RestoreBoardInput()
        {
            if (!isBlockingBoardInput)
            {
                return;
            }

            if (boardView != null)
            {
                boardView.raycastTarget = boardRaycastStateBeforeBlock;
            }

            isBlockingBoardInput = false;
        }

        private void CacheReferences()
        {
            if (pauseMenu == null)
            {
                pauseMenu = GetComponent<AnimatedPauseMenuController>();
            }

            if (boardView == null)
            {
                boardView = FindFirstObjectByType<GomokuBoardView>();
            }

            if (rootCanvas == null && settingsButtonRect != null)
            {
                rootCanvas = settingsButtonRect.GetComponentInParent<Canvas>();
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
