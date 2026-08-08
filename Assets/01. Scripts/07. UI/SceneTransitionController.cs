using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SceneTransitionController : MonoBehaviour
    {
        public const string TitleSceneName = "Title";
        public const string MainGameSceneName = "GomokuMvp";

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.28f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.32f;

        private static SceneTransitionController instance;

        private Coroutine transitionRoutine;
        private bool isLoadingScene;

        public bool IsTransitioning => transitionRoutine != null;
        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 1f;
            SetInputBlocked(true);
            transitionRoutine = StartCoroutine(FadeInOnStartup());
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public static void LoadMainGame()
        {
            LoadScene(MainGameSceneName);
        }

        public static void LoadTitle()
        {
            LoadScene(TitleSceneName);
        }

        public static void RestartCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        private static void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;

            if (instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            instance.BeginLoadScene(sceneName);
        }

        private void BeginLoadScene(string sceneName)
        {
            if (isLoadingScene)
            {
                return;
            }

            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator FadeInOnStartup()
        {
            yield return FadeTo(0f, fadeInDuration);
            SetInputBlocked(false);
            transitionRoutine = null;
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isLoadingScene = true;
            SetInputBlocked(true);
            yield return FadeTo(1f, fadeOutDuration);

            SceneManager.LoadScene(sceneName);
            yield return null;

            yield return FadeTo(0f, fadeInDuration);
            SetInputBlocked(false);
            isLoadingScene = false;
            transitionRoutine = null;
        }

        private IEnumerator FadeTo(float targetAlpha, float duration)
        {
            float startAlpha = canvasGroup.alpha;
            if (duration <= Mathf.Epsilon)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, progress);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void SetInputBlocked(bool blocked)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = blocked;
        }
    }
}
