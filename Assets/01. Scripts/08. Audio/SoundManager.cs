using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    public sealed class SoundManager : MonoBehaviour
    {
        private const string MasterVolumeKey = "NAN2026.Audio.MasterVolume";
        private const string MusicVolumeKey = "NAN2026.Audio.MusicVolume";
        private const string SfxVolumeKey = "NAN2026.Audio.SfxVolume";
        private const string MasterMutedKey = "NAN2026.Audio.MasterMuted";
        private const string MusicMutedKey = "NAN2026.Audio.MusicMuted";
        private const string SfxMutedKey = "NAN2026.Audio.SfxMuted";

        private static SoundManager instance;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        [Header("Playback")]
        [SerializeField, Min(1)] private int sfxPoolSize = 8;
        [SerializeField, Min(0f)] private float defaultMusicFadeDuration = 0.5f;

        private readonly List<AudioSource> sfxSources = new List<AudioSource>();
        private readonly Dictionary<AudioSource, float> sfxGains = new Dictionary<AudioSource, float>();
        private AudioSource firstMusicSource;
        private AudioSource secondMusicSource;
        private AudioSource activeMusicSource;
        private float firstMusicGain;
        private float secondMusicGain;
        private bool masterMuted;
        private bool musicMuted;
        private bool sfxMuted;
        private int nextSfxSourceIndex;
        private Coroutine musicFadeRoutine;

        public static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<SoundManager>();
                }

                if (instance == null)
                {
                    var managerObject = new GameObject(nameof(SoundManager));
                    instance = managerObject.AddComponent<SoundManager>();
                }

                return instance;
            }
        }

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public bool IsMasterMuted => masterMuted;
        public bool IsMusicMuted => musicMuted;
        public bool IsSfxMuted => sfxMuted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            _ = Instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            CreateAudioSources();
            RefreshAllVolumes();
        }

        public void PlayMusic(AudioClip clip, float fadeDuration = -1f, bool restart = false)
        {
            if (clip == null)
            {
                return;
            }

            if (!restart
                && activeMusicSource != null
                && activeMusicSource.clip == clip
                && activeMusicSource.isPlaying)
            {
                return;
            }

            StopMusicFadeRoutine();
            AudioSource previousSource = activeMusicSource;
            AudioSource nextSource = previousSource == firstMusicSource
                ? secondMusicSource
                : firstMusicSource;

            nextSource.Stop();
            nextSource.clip = clip;
            nextSource.loop = true;
            SetMusicGain(nextSource, 0f);
            nextSource.Play();
            activeMusicSource = nextSource;

            float duration = fadeDuration < 0f ? defaultMusicFadeDuration : fadeDuration;
            if (duration <= 0f)
            {
                StopAndClear(previousSource);
                SetMusicGain(nextSource, 1f);
                return;
            }

            float previousGain = GetMusicGain(previousSource);
            musicFadeRoutine = StartCoroutine(CrossfadeMusic(
                previousSource,
                nextSource,
                previousGain,
                duration));
        }

        public void StopMusic(float fadeDuration = -1f)
        {
            StopMusicFadeRoutine();
            AudioSource source = activeMusicSource;
            if (source == null || !source.isPlaying)
            {
                return;
            }

            float duration = fadeDuration < 0f ? defaultMusicFadeDuration : fadeDuration;
            if (duration <= 0f)
            {
                StopAndClear(source);
                activeMusicSource = null;
                return;
            }

            musicFadeRoutine = StartCoroutine(FadeOutMusic(
                source,
                GetMusicGain(source),
                duration));
        }

        public AudioSource PlaySfx(
            AudioClip clip,
            float volume = 1f,
            float pitch = 1f,
            float spatialBlend = 0f,
            Vector3 position = default)
        {
            if (clip == null)
            {
                return null;
            }

            AudioSource source = GetAvailableSfxSource();
            source.Stop();
            source.transform.position = position;
            source.clip = clip;
            source.loop = false;
            source.pitch = Mathf.Max(0.01f, pitch);
            source.spatialBlend = Mathf.Clamp01(spatialBlend);
            source.priority = 128;
            sfxGains[source] = Mathf.Clamp01(volume);
            RefreshSfxVolume(source);
            source.Play();
            return source;
        }

        public AudioSource PlaySfx(SoundCueSO cue, Vector3 position = default)
        {
            if (cue == null || !cue.TryGetRandomClip(out AudioClip clip))
            {
                return null;
            }

            AudioSource source = PlaySfx(
                clip,
                cue.Volume,
                cue.GetRandomPitch(),
                cue.SpatialBlend,
                position);
            source.priority = cue.Priority;
            return source;
        }

        public void StopAllSfx()
        {
            foreach (AudioSource source in sfxSources)
            {
                source.Stop();
                source.clip = null;
            }
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            RefreshAllVolumes();
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            RefreshMusicVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            RefreshSfxVolumes();
        }

        public void SetMasterMuted(bool muted)
        {
            masterMuted = muted;
            PlayerPrefs.SetInt(MasterMutedKey, muted ? 1 : 0);
            RefreshAllVolumes();
        }

        public void SetMusicMuted(bool muted)
        {
            musicMuted = muted;
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
            RefreshMusicVolumes();
        }

        public void SetSfxMuted(bool muted)
        {
            sfxMuted = muted;
            PlayerPrefs.SetInt(SfxMutedKey, muted ? 1 : 0);
            RefreshSfxVolumes();
        }

        private void CreateAudioSources()
        {
            firstMusicSource = CreateSource("Music A");
            secondMusicSource = CreateSource("Music B");
            firstMusicSource.loop = true;
            secondMusicSource.loop = true;

            int poolSize = Mathf.Max(1, sfxPoolSize);
            for (int index = 0; index < poolSize; index++)
            {
                AudioSource source = CreateSource($"SFX {index + 1}");
                sfxSources.Add(source);
                sfxGains[source] = 1f;
            }
        }

        private AudioSource CreateSource(string sourceName)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }

        private AudioSource GetAvailableSfxSource()
        {
            foreach (AudioSource source in sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            AudioSource fallback = sfxSources[nextSfxSourceIndex];
            nextSfxSourceIndex = (nextSfxSourceIndex + 1) % sfxSources.Count;
            return fallback;
        }

        private IEnumerator CrossfadeMusic(
            AudioSource previousSource,
            AudioSource nextSource,
            float previousGain,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetMusicGain(previousSource, Mathf.Lerp(previousGain, 0f, progress));
                SetMusicGain(nextSource, progress);
                yield return null;
            }

            StopAndClear(previousSource);
            SetMusicGain(nextSource, 1f);
            musicFadeRoutine = null;
        }

        private IEnumerator FadeOutMusic(AudioSource source, float startGain, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                SetMusicGain(source, Mathf.Lerp(startGain, 0f, progress));
                yield return null;
            }

            StopAndClear(source);
            if (activeMusicSource == source)
            {
                activeMusicSource = null;
            }

            musicFadeRoutine = null;
        }

        private void StopMusicFadeRoutine()
        {
            if (musicFadeRoutine == null)
            {
                return;
            }

            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        private void StopAndClear(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            SetMusicGain(source, 0f);
        }

        private void SetMusicGain(AudioSource source, float gain)
        {
            if (source == null)
            {
                return;
            }

            gain = Mathf.Clamp01(gain);
            if (source == firstMusicSource)
            {
                firstMusicGain = gain;
            }
            else if (source == secondMusicSource)
            {
                secondMusicGain = gain;
            }

            RefreshMusicVolume(source);
        }

        private float GetMusicGain(AudioSource source)
        {
            if (source == firstMusicSource)
            {
                return firstMusicGain;
            }

            return source == secondMusicSource ? secondMusicGain : 0f;
        }

        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            masterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) != 0;
            musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) != 0;
            sfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) != 0;
        }

        private void RefreshAllVolumes()
        {
            RefreshMusicVolumes();
            RefreshSfxVolumes();
        }

        private void RefreshMusicVolumes()
        {
            RefreshMusicVolume(firstMusicSource);
            RefreshMusicVolume(secondMusicSource);
        }

        private void RefreshMusicVolume(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            float gain = GetMusicGain(source);
            source.volume = masterMuted || musicMuted
                ? 0f
                : gain * masterVolume * musicVolume;
        }

        private void RefreshSfxVolumes()
        {
            foreach (AudioSource source in sfxSources)
            {
                RefreshSfxVolume(source);
            }
        }

        private void RefreshSfxVolume(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            float gain = sfxGains.TryGetValue(source, out float value) ? value : 1f;
            source.volume = masterMuted || sfxMuted
                ? 0f
                : gain * masterVolume * sfxVolume;
        }
    }
}
