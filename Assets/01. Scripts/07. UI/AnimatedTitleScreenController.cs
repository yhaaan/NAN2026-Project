using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class AnimatedTitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button guideButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private TMP_FontAsset cardNewsBoldFont;
        [SerializeField] private TMP_FontAsset cardNewsLightFont;
        [SerializeField] private AudioClip titleMusic;
        [SerializeField, Min(0f)] private float musicFadeDuration = 0.75f;

        private FirstMatchCardNewsView guideCardNews;

        public Button StartButton => startButton;
        public Button GuideButton => guideButton;
        public Button ExitButton => exitButton;
        public TMP_FontAsset CardNewsBoldFont => cardNewsBoldFont;
        public TMP_FontAsset CardNewsLightFont => cardNewsLightFont;
        public AudioClip TitleMusic => titleMusic;
        public float MusicFadeDuration => musicFadeDuration;

        private void Awake()
        {
            Time.timeScale = 1f;
            SoundManager.Instance.PlayMusic(titleMusic, musicFadeDuration);

            if (startButton == null)
            {
                Debug.LogError("AnimatedTitleScreenController requires a start button.", this);
            }
            else
            {
                startButton.onClick.AddListener(SceneTransitionController.LoadMainGame);
            }

            if (guideButton == null)
            {
                Debug.LogError("AnimatedTitleScreenController requires a guide button.", this);
            }
            else
            {
                guideButton.onClick.AddListener(ShowGuide);
            }

            if (exitButton == null)
            {
                Debug.LogError("AnimatedTitleScreenController requires an exit button.", this);
            }
            else
            {
                exitButton.onClick.AddListener(ExitGame);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(SceneTransitionController.LoadMainGame);
            }

            if (guideButton != null)
            {
                guideButton.onClick.RemoveListener(ShowGuide);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(ExitGame);
            }
        }

        private void ShowGuide()
        {
            if (guideCardNews != null)
            {
                return;
            }

            guideCardNews = FirstMatchCardNewsView.Create(
                transform,
                this,
                StartMainGameFromGuide,
                cardNewsBoldFont,
                cardNewsLightFont);
        }

        private void StartMainGameFromGuide()
        {
            guideCardNews = null;
            SceneTransitionController.LoadMainGame();
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
