using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class AnimatedPauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private RectTransform menuPanel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button titleButton;

        [Header("Menu Animation")]
        [SerializeField, Min(0f)] private float showDuration = 0.22f;
        [SerializeField, Min(0f)] private float hideDuration = 0.16f;
        [SerializeField, Range(0.5f, 1f)] private float hiddenScale = 0.92f;
        [SerializeField, Min(0f)] private float hiddenOffset = 24f;

        private bool isPausedByThisMenu;
        private Coroutine menuAnimation;
        private Vector2 panelShownPosition;
        private Vector3 panelShownScale;

        public bool IsOpen => menuRoot != null && menuRoot.activeSelf;
        public bool IsAnimating => menuAnimation != null;
        public float MenuAlpha => menuCanvasGroup != null ? menuCanvasGroup.alpha : 0f;

        private void Awake()
        {
            if (!HasRequiredReferences())
            {
                Debug.LogError("AnimatedPauseMenuController has missing UI references.", this);
                enabled = false;
                return;
            }

            panelShownPosition = menuPanel.anchoredPosition;
            panelShownScale = menuPanel.localScale;
            menuCanvasGroup.alpha = 0f;
            SetMenuInteraction(false);
            menuRoot.SetActive(false);

            settingsButton.onClick.AddListener(Open);
            resumeButton.onClick.AddListener(Resume);
            restartButton.onClick.AddListener(RestartGame);
            titleButton.onClick.AddListener(ReturnToTitle);
        }

        private void OnDestroy()
        {
            if (menuAnimation != null)
            {
                StopCoroutine(menuAnimation);
                menuAnimation = null;
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(Open);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
            }

            if (titleButton != null)
            {
                titleButton.onClick.RemoveListener(ReturnToTitle);
            }

            if (isPausedByThisMenu)
            {
                Time.timeScale = 1f;
            }
        }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            menuRoot.SetActive(true);
            Time.timeScale = 0f;
            isPausedByThisMenu = true;
            menuCanvasGroup.alpha = 0f;
            menuPanel.anchoredPosition = panelShownPosition + Vector2.down * hiddenOffset;
            menuPanel.localScale = panelShownScale * hiddenScale;
            SetMenuInteraction(false);
            menuAnimation = StartCoroutine(AnimateMenu(true, null));
        }

        public void Resume()
        {
            Hide(null);
        }

        public void RestartGame()
        {
            Hide(SceneTransitionController.RestartCurrentScene);
        }

        public void ReturnToTitle()
        {
            Hide(SceneTransitionController.LoadTitle);
        }

        private void Hide(Action onHidden)
        {
            if (!IsOpen || menuAnimation != null)
            {
                return;
            }

            SetMenuInteraction(false);
            menuAnimation = StartCoroutine(AnimateMenu(false, onHidden));
        }

        private IEnumerator AnimateMenu(bool visible, Action onHidden)
        {
            float duration = visible ? showDuration : hideDuration;
            float startAlpha = menuCanvasGroup.alpha;
            float targetAlpha = visible ? 1f : 0f;
            Vector2 startPosition = menuPanel.anchoredPosition;
            Vector2 targetPosition = visible
                ? panelShownPosition
                : panelShownPosition + Vector2.down * hiddenOffset;
            Vector3 startScale = menuPanel.localScale;
            Vector3 targetScale = visible
                ? panelShownScale
                : panelShownScale * hiddenScale;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = duration <= Mathf.Epsilon
                    ? 1f
                    : Mathf.Clamp01(elapsed / duration);
                float eased = visible
                    ? 1f - Mathf.Pow(1f - progress, 3f)
                    : progress * progress * progress;

                menuCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                menuPanel.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
                menuPanel.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            menuCanvasGroup.alpha = targetAlpha;
            menuPanel.anchoredPosition = targetPosition;
            menuPanel.localScale = targetScale;
            menuAnimation = null;

            if (visible)
            {
                SetMenuInteraction(true);
                Select(resumeButton);
                yield break;
            }

            menuRoot.SetActive(false);
            Time.timeScale = 1f;
            isPausedByThisMenu = false;
            Select(settingsButton);
            onHidden?.Invoke();
        }

        private bool HasRequiredReferences()
        {
            return menuRoot != null
                && menuCanvasGroup != null
                && menuPanel != null
                && settingsButton != null
                && resumeButton != null
                && restartButton != null
                && titleButton != null;
        }

        private void SetMenuInteraction(bool interactable)
        {
            menuCanvasGroup.interactable = interactable;
            menuCanvasGroup.blocksRaycasts = true;
        }

        private static void Select(Button button)
        {
            if (EventSystem.current != null && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
