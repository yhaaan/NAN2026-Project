using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    public sealed class AnimatedTitleScreenController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void Awake()
        {
            Time.timeScale = 1f;

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
