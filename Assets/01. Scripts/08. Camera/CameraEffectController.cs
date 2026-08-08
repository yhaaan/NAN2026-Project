using Unity.Cinemachine;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class CameraEffectController : MonoBehaviour
    {
        private const float MinimumDuration = 0.01f;

        [Header("Screen Shake")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField, Min(0f)] private float defaultStrength = 1f;
        [SerializeField, Min(MinimumDuration)] private float defaultDuration = 0.2f;
        [Tooltip("흔들림의 기준 방향입니다. 강도는 별도로 적용되므로 방향의 크기는 무시됩니다.")]
        [SerializeField] private Vector3 defaultDirection = Vector3.down;

        [Header("Placement Shake")]
        [Tooltip("돌을 착수할 때 카메라가 아래로 눌리는 정도입니다.")]
        [SerializeField, Min(0f)] private float placementStrength = 0.01f;
        [SerializeField, Min(MinimumDuration)] private float placementDuration = 0.12f;

        public float DefaultStrength => defaultStrength;
        public float DefaultDuration => defaultDuration;

        private void Awake()
        {
            CacheImpulseSource();
        }

        private void Reset()
        {
            CacheImpulseSource();

            if (impulseSource == null)
            {
                return;
            }

            impulseSource.DefaultVelocity = Vector3.down;
            impulseSource.ImpulseDefinition.ImpulseShape =
                CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulseSource.ImpulseDefinition.ImpulseDuration = defaultDuration;
            impulseSource.ImpulseDefinition.ImpulseType =
                CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        }

        private void OnValidate()
        {
            defaultStrength = Mathf.Max(0f, defaultStrength);
            defaultDuration = Mathf.Max(MinimumDuration, defaultDuration);
            placementStrength = Mathf.Max(0f, placementStrength);
            placementDuration = Mathf.Max(MinimumDuration, placementDuration);
        }

        public void PlayPlacementShake()
        {
            PlayScreenShake(Vector3.up, placementStrength, placementDuration);
        }

        public void PlayScreenShake()
        {
            PlayScreenShake(defaultDirection, defaultStrength, defaultDuration);
        }

        public void PlayScreenShake(float strength)
        {
            PlayScreenShake(defaultDirection, strength, defaultDuration);
        }

        public void PlayScreenShake(float strength, float duration)
        {
            PlayScreenShake(defaultDirection, strength, duration);
        }

        public void PlayScreenShake(Vector3 direction, float strength, float duration)
        {
            PlayScreenShakeAt(transform.position, direction, strength, duration);
        }

        public void PlayScreenShakeAt(
            Vector3 worldPosition,
            Vector3 direction,
            float strength,
            float duration)
        {
            if (strength <= 0f || duration <= 0f)
            {
                return;
            }

            CacheImpulseSource();
            if (impulseSource == null)
            {
                Debug.LogError(
                    "CameraEffectController requires a CinemachineImpulseSource.",
                    this);
                return;
            }

            Vector3 normalizedDirection = direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : GetDefaultDirection();
            CinemachineImpulseDefinition definition =
                CopyDefinition(impulseSource.ImpulseDefinition);
            SetDuration(definition, Mathf.Max(MinimumDuration, duration));
            definition.CreateEvent(worldPosition, normalizedDirection * strength);
        }

        private void CacheImpulseSource()
        {
            if (impulseSource == null)
            {
                impulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }

        private Vector3 GetDefaultDirection()
        {
            return defaultDirection.sqrMagnitude > Mathf.Epsilon
                ? defaultDirection.normalized
                : Vector3.down;
        }

        private static void SetDuration(
            CinemachineImpulseDefinition definition,
            float duration)
        {
            if (definition.ImpulseType
                != CinemachineImpulseDefinition.ImpulseTypes.Legacy)
            {
                definition.ImpulseDuration = duration;
                return;
            }

            CinemachineImpulseManager.EnvelopeDefinition envelope =
                definition.TimeEnvelope;
            float originalDuration = envelope.Duration;
            envelope.HoldForever = false;
            envelope.ScaleWithImpact = false;

            if (originalDuration > Mathf.Epsilon)
            {
                float scale = duration / originalDuration;
                envelope.AttackTime *= scale;
                envelope.SustainTime *= scale;
                envelope.DecayTime *= scale;
            }
            else
            {
                envelope.AttackTime = 0f;
                envelope.SustainTime = duration;
                envelope.DecayTime = 0f;
            }

            definition.TimeEnvelope = envelope;
        }

        private static CinemachineImpulseDefinition CopyDefinition(
            CinemachineImpulseDefinition source)
        {
            return new CinemachineImpulseDefinition
            {
                ImpulseChannel = source.ImpulseChannel,
                ImpulseShape = source.ImpulseShape,
                CustomImpulseShape = source.CustomImpulseShape,
                ImpulseDuration = source.ImpulseDuration,
                ImpulseType = source.ImpulseType,
                DissipationRate = source.DissipationRate,
                RawSignal = source.RawSignal,
                AmplitudeGain = source.AmplitudeGain,
                FrequencyGain = source.FrequencyGain,
                RepeatMode = source.RepeatMode,
                Randomize = source.Randomize,
                TimeEnvelope = source.TimeEnvelope,
                ImpactRadius = source.ImpactRadius,
                DirectionMode = source.DirectionMode,
                DissipationMode = source.DissipationMode,
                DissipationDistance = source.DissipationDistance,
                PropagationSpeed = source.PropagationSpeed
            };
        }
    }
}
