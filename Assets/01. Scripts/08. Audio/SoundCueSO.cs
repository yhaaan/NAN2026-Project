using UnityEngine;

namespace NAN2026.Gomoku
{
    [CreateAssetMenu(fileName = "SoundCue", menuName = "NAN2026/Audio/Sound Cue")]
    public sealed class SoundCueSO : ScriptableObject
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Min(0.01f)] private float minPitch = 1f;
        [SerializeField, Min(0.01f)] private float maxPitch = 1f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend;
        [SerializeField, Range(0, 256)] private int priority = 128;

        public float Volume => volume;
        public float SpatialBlend => spatialBlend;
        public int Priority => priority;

        public float GetRandomPitch()
        {
            float low = Mathf.Min(minPitch, maxPitch);
            float high = Mathf.Max(minPitch, maxPitch);
            return Random.Range(low, high);
        }

        public bool TryGetRandomClip(out AudioClip clip)
        {
            clip = null;
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int startIndex = Random.Range(0, clips.Length);
            for (int offset = 0; offset < clips.Length; offset++)
            {
                AudioClip candidate = clips[(startIndex + offset) % clips.Length];
                if (candidate != null)
                {
                    clip = candidate;
                    return true;
                }
            }

            return false;
        }

        private void OnValidate()
        {
            minPitch = Mathf.Max(0.01f, minPitch);
            maxPitch = Mathf.Max(0.01f, maxPitch);
        }
    }
}
