using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    public sealed class UiButtonSfxFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip clickSfx;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        private readonly Dictionary<Button, UnityAction> listeners =
            new Dictionary<Button, UnityAction>();

        public AudioClip ClickSfx => clickSfx;
        public Vector2 PitchRange => pitchRange;
        public int BoundButtonCount => listeners.Count;

        private IEnumerator Start()
        {
            yield return null;
            BindButtons();
        }

        public void BindButtons()
        {
            UnbindButtons();
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.GetComponent<ShopSlotView>() != null)
                {
                    continue;
                }

                GomokuHud hud = button.GetComponentInParent<GomokuHud>(true);
                UnityAction listener = hud != null && button == hud.CombatSpeedButton
                    ? () => PlayCombatSpeedFeedback(hud.CombatSpeed)
                    : PlayFeedback;
                button.onClick.AddListener(listener);
                listeners.Add(button, listener);
            }
        }

        public void PlayFeedback()
        {
            if (clickSfx == null)
            {
                return;
            }

            float minPitch = Mathf.Min(pitchRange.x, pitchRange.y);
            float maxPitch = Mathf.Max(pitchRange.x, pitchRange.y);
            SoundManager.Instance.PlaySfx(
                clickSfx,
                1f,
                UnityEngine.Random.Range(minPitch, maxPitch));
        }

        public void PlayCombatSpeedFeedback(int combatSpeed)
        {
            if (clickSfx != null)
            {
                SoundManager.Instance.PlaySfx(
                    clickSfx,
                    1f,
                    CombatSpeedPitch(combatSpeed));
            }
        }

        public static float CombatSpeedPitch(int combatSpeed)
        {
            int clampedSpeed = Mathf.Clamp(combatSpeed, 1, 5);
            return 0.84f + (clampedSpeed - 1) * 0.08f;
        }

        private void OnDestroy()
        {
            UnbindButtons();
        }

        private void UnbindButtons()
        {
            foreach (KeyValuePair<Button, UnityAction> pair in listeners)
            {
                if (pair.Key != null)
                {
                    pair.Key.onClick.RemoveListener(pair.Value);
                }
            }

            listeners.Clear();
        }
    }
}
