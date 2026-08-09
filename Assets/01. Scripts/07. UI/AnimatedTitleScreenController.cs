using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class AnimatedTitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private AudioClip titleMusic;
        [SerializeField, Min(0f)] private float musicFadeDuration = 0.75f;

        public AudioClip TitleMusic => titleMusic;
        public float MusicFadeDuration => musicFadeDuration;

        private void Awake()
        {
            Time.timeScale = 1f;
            SoundManager.Instance.PlayMusic(titleMusic, musicFadeDuration);

            if (startButton == null)
            {
                Debug.LogError("AnimatedTitleScreenController requires a start button.", this);
                return;
            }

            startButton.onClick.AddListener(SceneTransitionController.LoadMainGame);
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(SceneTransitionController.LoadMainGame);
            }
        }
    }
}
