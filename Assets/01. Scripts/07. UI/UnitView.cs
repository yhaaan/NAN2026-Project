using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    public sealed class UnitView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Transform bodyRoot;
        [SerializeField] private bool normalizeSpriteSize;
        private float visualDiameter = 2f;
        [SerializeField] private Material feedbackParticleMaterial;
        [SerializeField] private Transform vfxRoot;
        [Header("Action VFX")]
        [SerializeField] private ProjectileVfxView projectilePrefab;
        [Header("Action Audio")]
        [SerializeField] private AudioClip[] attackPopSfx = Array.Empty<AudioClip>();
        [SerializeField] private AudioClip arrowAttackSfx;
        [SerializeField] private AudioClip healingMagicSfx;
        [SerializeField] private AudioClip ninjaAttackSfx;
        [SerializeField] private AudioClip casterMagicSfx;

        private SpriteRenderer innerRenderer;
        private SpriteRenderer accentRenderer;
        private ParticleSystem feedbackParticles;
        private bool isBound;
        private Color accentColor = Color.white;
        private Color innerColor = Color.white;
        private Color authoredBodyColor = Color.white;
        private Tween motionTween;
        private Tween feedbackTween;
        private Sequence deathSequence;
        private Vector3 restLocalPosition;
        private Vector3 authoredBodyScale = Vector3.one;
        private bool preview;
        private bool visualsInitialized;
        private bool usesGeneratedStone;
        private StoneColor boundSide;
        private UnitRole role;
        private UnitAbility ability;
        private string boundUnitId = string.Empty;
        private static Material runtimeParticleMaterial;

        public bool IsDying { get; private set; }
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public Transform BodyRoot => bodyRoot;
        public bool NormalizeSpriteSize => normalizeSpriteSize;
        public float VisualDiameter => visualDiameter;
        public IReadOnlyList<AudioClip> AttackPopSfx => attackPopSfx ?? Array.Empty<AudioClip>();
        public AudioClip ArrowAttackSfx => arrowAttackSfx;
        public AudioClip HealingMagicSfx => healingMagicSfx;
        public AudioClip NinjaAttackSfx => ninjaAttackSfx;
        public AudioClip CasterMagicSfx => casterMagicSfx;
        public bool UsesProjectile => isBound
            && projectilePrefab != null
            && (role == UnitRole.Marksman || role == UnitRole.Caster);

        private void Awake()
        {
            EnsureVisuals();
        }

        public void Bind(BoardUnit targetUnit, UnitPresentationSO presentation, bool isPreview = false)
        {
            EnsureVisuals();
            Sprite unitSprite = targetUnit.Definition.GetSprite(targetUnit.Side);
            if (unitSprite != null)
            {
                bodyRenderer.sprite = unitSprite;
                bodyRenderer.color = Color.white;
                authoredBodyColor = Color.white;
                usesGeneratedStone = false;
            }

            if (isBound)
            {
                transform.localPosition = restLocalPosition;
            }

            KillTweens();
            isBound = true;
            accentColor = presentation != null ? presentation.AccentColor : targetUnit.Definition.RoleColor;
            boundSide = targetUnit.Side;
            role = targetUnit.Definition.Role;
            ability = targetUnit.Definition.Ability;
            boundUnitId = targetUnit.Definition.UnitId;
            restLocalPosition = transform.localPosition;
            transform.localScale = Vector3.one;
            ApplyBodyScale();
            IsDying = false;
            SetPreview(isPreview);

            innerColor = InnerColor(targetUnit.Side);
            if (usesGeneratedStone)
            {
                bodyRenderer.color = WithAlpha(OuterColor(targetUnit.Side), isPreview ? 0.48f : 1f);
                innerRenderer.color = WithAlpha(innerColor, isPreview ? 0.48f : 1f);
                accentRenderer.color = WithAlpha(accentColor, isPreview ? 0.48f : 1f);
            }
            else
            {
                bodyRenderer.color = WithAlpha(authoredBodyColor, isPreview ? 0.48f : authoredBodyColor.a);
            }
        }

        public void SetPreview(bool value)
        {
            preview = value;
        }

        public void SetVictoryFocus(bool focused)
        {
            EnsureVisuals();
            float alphaMultiplier = focused ? 1f : 0.28f;
            if (usesGeneratedStone)
            {
                bodyRenderer.color = WithAlpha(OuterColor(boundSide), alphaMultiplier);
                innerRenderer.color = WithAlpha(innerColor, alphaMultiplier);
                accentRenderer.color = WithAlpha(accentColor, alphaMultiplier);
            }
            else
            {
                bodyRenderer.color = WithAlpha(
                    authoredBodyColor,
                    authoredBodyColor.a * alphaMultiplier);
            }
        }

        public void PlayVictoryJump(bool finalStone)
        {
            if (IsDying || preview)
            {
                return;
            }

            StopMotionTween();
            float height = finalStone ? 0.62f : 0.42f;
            float riseDuration = finalStone ? 0.16f : 0.13f;
            float fallDuration = finalStone ? 0.14f : 0.12f;
            float landingScale = finalStone ? 1.22f : 1.12f;
            Sequence victorySequence = DOTween.Sequence();
            victorySequence.SetTarget(this);
            victorySequence.SetUpdate(true);
            victorySequence
                .Append(transform.DOLocalMove(restLocalPosition + Vector3.up * height, riseDuration)
                    .SetEase(Ease.OutQuad))
                .Join(transform.DOScale(finalStone ? 1.12f : 1.06f, riseDuration)
                    .SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(restLocalPosition, fallDuration).SetEase(Ease.InQuad))
                .Join(transform.DOScale(
                    new Vector3(landingScale, 0.84f, 1f),
                    fallDuration).SetEase(Ease.InQuad))
                .Append(transform.DOScale(Vector3.one, finalStone ? 0.13f : 0.1f)
                    .SetEase(Ease.OutBack));
            motionTween = victorySequence;

            if (finalStone)
            {
                victorySequence.InsertCallback(
                    riseDuration + fallDuration,
                    () => PlayImpactPulse(
                        restLocalPosition,
                        new Color(1f, 0.82f, 0.28f, 0.9f),
                        new Vector3(1.35f, 1.35f, 1f),
                        0.2f));
            }

            motionTween.OnComplete(CompleteMotion);
        }

        public void PlayAction(UnitView target)
        {
            PlayAction(target, UnitActionKind.Damage);
        }

        public void PlayAction(UnitView target, UnitActionKind actionKind)
        {
            PlayAction(target, actionKind, null);
        }

        public void PlayAction(
            UnitView target,
            UnitActionKind actionKind,
            Action projectileArrived)
        {
            if (IsDying || preview)
            {
                projectileArrived?.Invoke();
                return;
            }

            StopMotionTween();
            Vector3 direction = target != null
                ? (target.transform.localPosition - restLocalPosition).normalized
                : Vector3.up;

            if (!isBound)
            {
                PlayDefaultAction(direction, actionKind);
                return;
            }

            switch (role)
            {
                case UnitRole.Vanguard:
                    PlayMeleeAction(target, direction, actionKind);
                    break;
                case UnitRole.Marksman:
                case UnitRole.Caster:
                    PlayRangedAction(target, direction, actionKind, projectileArrived);
                    break;
                case UnitRole.Support:
                    PlayHealerAction(actionKind);
                    break;
                case UnitRole.Guardian:
                    PlayTankAction(target, direction, actionKind);
                    break;
                default:
                    PlayDefaultAction(direction, actionKind);
                    break;
            }
        }

        private void PlayMeleeAction(
            UnitView target,
            Vector3 direction,
            UnitActionKind actionKind)
        {
            Vector3 windupPosition = restLocalPosition - direction * 0.06f;
            Vector3 strikePosition = restLocalPosition + direction * 0.34f;
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOLocalMove(windupPosition, 0.07f).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(strikePosition, 0.08f).SetEase(Ease.InQuad))
                .AppendCallback(() =>
                {
                    PlayActionSfx(actionKind);
                    PlayImpactPulse(
                        TargetPosition(target, strikePosition),
                        new Color(1f, 0.42f, 0.1f, 0.85f),
                        new Vector3(0.46f, 0.24f, 1f),
                        0.16f);
                })
                .Append(transform.DOLocalMove(restLocalPosition, 0.11f).SetEase(Ease.OutQuad))
                .OnComplete(CompleteMotion);
        }

        private void PlayRangedAction(
            UnitView target,
            Vector3 direction,
            UnitActionKind actionKind,
            Action projectileArrived)
        {
            Vector3 windupPosition = restLocalPosition - direction * 0.07f;
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOLocalMove(windupPosition, 0.1f).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(restLocalPosition + direction * 0.025f, 0.045f)
                    .SetEase(Ease.OutQuad))
                .AppendCallback(() =>
                {
                    PlayActionSfx(actionKind);
                    LaunchProjectile(target, projectileArrived);
                })
                .Append(transform.DOLocalMove(restLocalPosition, 0.1f).SetEase(Ease.OutQuad))
                .OnComplete(CompleteMotion);
        }

        private void PlayHealerAction(UnitActionKind actionKind)
        {
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOLocalMove(restLocalPosition + Vector3.up * 0.08f, 0.12f)
                    .SetEase(Ease.OutQuad))
                .Join(transform.DOScale(1.1f, 0.12f).SetEase(Ease.OutQuad))
                .AppendCallback(() =>
                {
                    PlayActionSfx(actionKind);
                    PlayHealWave();
                })
                .Append(transform.DOLocalMove(restLocalPosition, 0.18f).SetEase(Ease.InOutQuad))
                .Join(transform.DOScale(Vector3.one, 0.18f).SetEase(Ease.InOutQuad))
                .OnComplete(CompleteMotion);
        }

        private void PlayTankAction(
            UnitView target,
            Vector3 direction,
            UnitActionKind actionKind)
        {
            Vector3 strikePosition = restLocalPosition + direction * 0.14f;
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOScale(new Vector3(1.05f, 0.86f, 1f), 0.14f)
                    .SetEase(Ease.InQuad))
                .Append(transform.DOLocalMove(strikePosition, 0.13f).SetEase(Ease.OutQuad))
                .Join(transform.DOScale(new Vector3(1.08f, 1f, 1f), 0.13f))
                .AppendCallback(() =>
                {
                    PlayActionSfx(actionKind);
                    PlayImpactPulse(
                        TargetPosition(target, strikePosition),
                        new Color(0.82f, 0.72f, 0.48f, 0.8f),
                        new Vector3(0.54f, 0.34f, 1f),
                        0.24f);
                })
                .Append(transform.DOLocalMove(restLocalPosition, 0.24f).SetEase(Ease.OutSine))
                .Join(transform.DOScale(Vector3.one, 0.24f).SetEase(Ease.OutSine))
                .OnComplete(CompleteMotion);
        }

        private void PlayDefaultAction(Vector3 direction, UnitActionKind actionKind)
        {
            Vector3 actionPosition = restLocalPosition + direction * 0.18f;
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .AppendCallback(() => PlayActionSfx(actionKind))
                .Append(transform.DOLocalMove(actionPosition, 0.11f).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(restLocalPosition, 0.11f).SetEase(Ease.InQuad))
                .OnComplete(CompleteMotion);
        }

        private void LaunchProjectile(UnitView target, Action arrived)
        {
            if (target == null || projectilePrefab == null || transform.parent == null)
            {
                arrived?.Invoke();
                return;
            }

            ProjectileVfxView projectile = Instantiate(projectilePrefab, transform.parent);
            if (ability == UnitAbility.PiercingShot)
            {
                Vector3 direction = (target.transform.localPosition - transform.localPosition).normalized;
                projectile.PlayLinear(
                    transform.localPosition,
                    target.transform.localPosition + direction * 0.45f,
                    arrived);
                return;
            }

            projectile.Play(
                transform.localPosition,
                target.transform.localPosition,
                () =>
                {
                    if (target != null)
                    {
                        target.EmitFeedback(new Color(1f, 0.74f, 0.18f), 6);
                    }

                    arrived?.Invoke();
                });
        }

        private void PlayHealWave()
        {
            PlayImpactPulse(
                restLocalPosition,
                new Color(0.28f, 1f, 0.52f, 0.72f),
                new Vector3(1.65f, 1.65f, 1f),
                0.36f);
        }

        private void PlayActionSfx(UnitActionKind actionKind)
        {
            AudioClip clip;
            Vector2 pitchRange;

            if (actionKind == UnitActionKind.Heal && healingMagicSfx != null)
            {
                clip = healingMagicSfx;
                pitchRange = new Vector2(0.96f, 1.06f);
            }
            else if (boundUnitId == "rare-ninja" && ninjaAttackSfx != null)
            {
                clip = ninjaAttackSfx;
                pitchRange = new Vector2(0.98f, 1.12f);
            }
            else if (boundUnitId == "common-marksman" && arrowAttackSfx != null)
            {
                clip = arrowAttackSfx;
                pitchRange = new Vector2(0.96f, 1.08f);
            }
            else if (role == UnitRole.Caster && casterMagicSfx != null)
            {
                clip = casterMagicSfx;
                pitchRange = boundUnitId == "legendary-storm-sage"
                    ? new Vector2(1.08f, 1.22f)
                    : new Vector2(0.94f, 1.06f);
            }
            else
            {
                clip = GetRandomAttackPop();
                pitchRange = GetAttackPitchRange(role);
            }

            if (clip != null)
            {
                SoundManager.Instance.PlaySfx(
                    clip,
                    1f,
                    UnityEngine.Random.Range(pitchRange.x, pitchRange.y));
            }
        }

        private AudioClip GetRandomAttackPop()
        {
            if (attackPopSfx == null || attackPopSfx.Length == 0)
            {
                return null;
            }

            int validClipCount = 0;
            foreach (AudioClip clip in attackPopSfx)
            {
                if (clip != null)
                {
                    validClipCount++;
                }
            }

            if (validClipCount == 0)
            {
                return null;
            }

            int selectedClipIndex = UnityEngine.Random.Range(0, validClipCount);
            foreach (AudioClip clip in attackPopSfx)
            {
                if (clip != null && selectedClipIndex-- == 0)
                {
                    return clip;
                }
            }

            return null;
        }

        public static Vector2 GetAttackPitchRange(UnitRole unitRole)
        {
            switch (unitRole)
            {
                case UnitRole.Guardian:
                    return new Vector2(0.72f, 0.86f);
                case UnitRole.Vanguard:
                    return new Vector2(0.88f, 1.02f);
                case UnitRole.Support:
                    return new Vector2(1.05f, 1.18f);
                case UnitRole.Marksman:
                    return new Vector2(1.08f, 1.22f);
                case UnitRole.Caster:
                    return new Vector2(1.2f, 1.36f);
                default:
                    return new Vector2(0.95f, 1.05f);
            }
        }

        private void PlayImpactPulse(
            Vector3 localPosition,
            Color color,
            Vector3 targetScale,
            float duration)
        {
            if (transform.parent == null)
            {
                return;
            }

            var pulseObject = new GameObject("ActionPulse", typeof(SpriteRenderer));
            pulseObject.transform.SetParent(transform.parent, false);
            pulseObject.transform.localPosition = localPosition;
            pulseObject.transform.localScale = targetScale * 0.18f;

            SpriteRenderer pulseRenderer = pulseObject.GetComponent<SpriteRenderer>();
            pulseRenderer.sprite = WorldSpriteFactory.Ring;
            pulseRenderer.color = color;
            pulseRenderer.sortingLayerName = "WorldVfx";
            pulseRenderer.sortingOrder = 0;

            Color transparent = color;
            transparent.a = 0f;
            DOTween.Sequence()
                .SetTarget(pulseObject)
                .Join(pulseObject.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad))
                .Join(CreateColorTween(pulseRenderer, transparent, duration).SetEase(Ease.OutQuad))
                .OnComplete(() => Destroy(pulseObject));
        }

        private void CompleteMotion()
        {
            transform.localPosition = restLocalPosition;
            transform.localScale = Vector3.one;
            motionTween = null;
        }

        private static Vector3 TargetPosition(UnitView target, Vector3 fallback)
        {
            return target != null ? target.transform.localPosition : fallback;
        }

        public void PlayHit()
        {
            if (IsDying || preview)
            {
                return;
            }

            EmitFeedback(new Color(1f, 0.32f, 0.08f), 8);
            PlayColorFlash(new Color(1f, 0.36f, 0.16f));
        }

        public void PlayHeal()
        {
            if (IsDying || preview)
            {
                return;
            }

            EmitFeedback(new Color(0.24f, 1f, 0.48f), 10);
            PlayColorFlash(new Color(0.35f, 1f, 0.55f));
        }

        public void PlayDeath(float durationSeconds, Action completed)
        {
            if (IsDying)
            {
                return;
            }

            IsDying = true;
            StopMotionTween();
            feedbackTween?.Kill();
            feedbackTween = null;

            float duration = Mathf.Max(0.1f, durationSeconds);
            Color bodyColor = bodyRenderer.color;
            deathSequence = DOTween.Sequence().SetTarget(this);
            deathSequence.Join(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad));
            deathSequence.Join(CreateColorTween(bodyRenderer, WithAlpha(bodyColor, 0f), duration));
            if (innerRenderer != null)
            {
                Color inner = innerRenderer.color;
                deathSequence.Join(CreateColorTween(innerRenderer, WithAlpha(inner, 0f), duration));
            }

            if (accentRenderer != null)
            {
                Color accent = accentRenderer.color;
                deathSequence.Join(CreateColorTween(accentRenderer, WithAlpha(accent, 0f), duration));
            }

            deathSequence.OnComplete(() =>
            {
                deathSequence = null;
                completed?.Invoke();
            });
        }

        private void OnDestroy()
        {
            KillTweens();
        }

        public static UnitView CreateRuntimePlaceholder(Transform parent)
        {
            var target = new GameObject(
                "MissingUnitView",
                typeof(UnitView));
            target.transform.SetParent(parent, false);
            UnitView view = target.GetComponent<UnitView>();
            view.normalizeSpriteSize = true;
            return view;
        }

        private void EnsureVisuals()
        {
            if (visualsInitialized)
            {
                return;
            }

            if (bodyRenderer == null)
            {
                bodyRoot = transform.Find("Body");
                if (bodyRoot == null)
                {
                    bodyRoot = new GameObject("Body").transform;
                    bodyRoot.SetParent(transform, false);
                }

                bodyRenderer = bodyRoot.GetComponent<SpriteRenderer>();
                if (bodyRenderer == null)
                {
                    bodyRenderer = bodyRoot.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            bodyRoot = bodyRenderer.transform;
            usesGeneratedStone = bodyRenderer.sprite == null;
            authoredBodyColor = bodyRenderer.color;
            authoredBodyScale = bodyRoot.localScale;
            if (usesGeneratedStone)
            {
                bodyRenderer.sprite = WorldSpriteFactory.Circle;
            }

            ApplyBodyScale();

            bodyRenderer.sortingLayerName = "Units";
            bodyRenderer.sortingOrder = 2;

            vfxRoot = EnsureAnchor(vfxRoot, "VfxRoot", Vector3.zero);

            if (usesGeneratedStone && innerRenderer == null)
            {
                innerRenderer = CreateChildRenderer(
                    bodyRoot,
                    "StoneInner",
                    WorldSpriteFactory.Circle,
                    new Vector3(0.88f, 0.88f, 1f),
                    3);
            }

            if (usesGeneratedStone && accentRenderer == null)
            {
                accentRenderer = CreateChildRenderer(
                    bodyRoot,
                    "RoleDot",
                    WorldSpriteFactory.Circle,
                    new Vector3(0.3f, 0.3f, 1f),
                    4);
            }

            if (feedbackParticles == null)
            {
                feedbackParticles = CreateFeedbackParticles();
            }

            visualsInitialized = true;
        }

        private Transform EnsureAnchor(Transform current, string anchorName, Vector3 localPosition)
        {
            if (current != null)
            {
                return current;
            }

            var anchor = new GameObject(anchorName).transform;
            anchor.SetParent(transform, false);
            anchor.localPosition = localPosition;
            return anchor;
        }

        private ParticleSystem CreateFeedbackParticles()
        {
            var particleObject = new GameObject("FeedbackParticles", typeof(ParticleSystem));
            particleObject.transform.SetParent(vfxRoot, false);
            ParticleSystem particles = particleObject.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 24;

            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.12f;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = "WorldVfx";
            renderer.sortingOrder = 0;
            renderer.sharedMaterial = feedbackParticleMaterial != null
                ? feedbackParticleMaterial
                : GetRuntimeParticleMaterial();
            return particles;
        }

        private static Material GetRuntimeParticleMaterial()
        {
            if (runtimeParticleMaterial != null)
            {
                return runtimeParticleMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                Debug.LogError(
                    "The URP Particles/Unlit shader could not be found. "
                    + "Assign a feedback particle material to UnitView.");
                return null;
            }

            runtimeParticleMaterial = new Material(shader)
            {
                name = "Runtime Unit Feedback Particles",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = 3000
            };
            runtimeParticleMaterial.SetOverrideTag("RenderType", "Transparent");
            runtimeParticleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            runtimeParticleMaterial.SetFloat("_Surface", 1f);
            runtimeParticleMaterial.SetFloat("_SrcBlend", 5f);
            runtimeParticleMaterial.SetFloat("_DstBlend", 10f);
            runtimeParticleMaterial.SetFloat("_ZWrite", 0f);
            return runtimeParticleMaterial;
        }

        private void EmitFeedback(Color color, int count)
        {
            if (feedbackParticles == null)
            {
                return;
            }

            var main = feedbackParticles.main;
            main.startColor = color;
            feedbackParticles.Emit(count);
        }

        private SpriteRenderer CreateChildRenderer(
            Transform parent,
            string objectName,
            Sprite sprite,
            Vector3 scale,
            int order)
        {
            var child = new GameObject(objectName, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localScale = scale;
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Units";
            renderer.sortingOrder = order;
            return renderer;
        }

        private void ApplyBodyScale()
        {
            if (bodyRoot == null || bodyRenderer == null)
            {
                return;
            }

            float normalization = 1f;
            if (normalizeSpriteSize && bodyRenderer.sprite != null)
            {
                Vector2 spriteSize = bodyRenderer.sprite.bounds.size;
                float largestDimension = Mathf.Max(spriteSize.x, spriteSize.y);
                if (largestDimension > Mathf.Epsilon)
                {
                    normalization = visualDiameter / largestDimension;
                }
            }

            bodyRoot.localScale = new Vector3(
                authoredBodyScale.x * normalization,
                authoredBodyScale.y * normalization,
                authoredBodyScale.z);
        }

        private void PlayColorFlash(Color flashColor)
        {
            feedbackTween?.Kill();
            SpriteRenderer flashRenderer = usesGeneratedStone ? innerRenderer : bodyRenderer;
            Color baseColor = usesGeneratedStone ? innerColor : authoredBodyColor;
            Color targetColor = WithAlpha(
                baseColor,
                preview ? 0.48f : baseColor.a);
            flashRenderer.color = WithAlpha(flashColor, targetColor.a);
            feedbackTween = CreateColorTween(flashRenderer, targetColor, 0.18f)
                .SetEase(Ease.Linear)
                .SetTarget(this)
                .OnComplete(() => feedbackTween = null);
        }

        private static Tweener CreateColorTween(
            SpriteRenderer renderer,
            Color targetColor,
            float duration)
        {
            return DOTween.To(
                () => renderer.color,
                color => renderer.color = color,
                targetColor,
                duration);
        }

        private void StopMotionTween()
        {
            motionTween?.Kill();
            motionTween = null;
            transform.localPosition = restLocalPosition;
            transform.localScale = Vector3.one;
        }

        private void KillTweens()
        {
            motionTween?.Kill();
            feedbackTween?.Kill();
            deathSequence?.Kill();
            motionTween = null;
            feedbackTween = null;
            deathSequence = null;
        }

        private static Color OuterColor(StoneColor side)
        {
            return side == StoneColor.Black
                ? new Color(0.03f, 0.03f, 0.04f)
                : new Color(0.1f, 0.1f, 0.1f);
        }

        private static Color InnerColor(StoneColor side)
        {
            return side == StoneColor.Black
                ? new Color(0.08f, 0.08f, 0.1f)
                : new Color(0.95f, 0.95f, 0.92f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }

    internal static class WorldSpriteFactory
    {
        private const int TextureSize = 64;
        private static Sprite circle;
        private static Sprite arrow;
        private static Sprite ring;
        private static Sprite square;

        public static Sprite Square
        {
            get
            {
                if (square == null)
                {
                    square = CreateSprite((x, y) => true, "WorldSquare");
                }

                return square;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null)
                {
                    circle = CreateSprite(
                        (x, y) => x * x + y * y <= 0.88f,
                        "WorldCircle");
                }

                return circle;
            }
        }

        public static Sprite Arrow
        {
            get
            {
                if (arrow == null)
                {
                    arrow = CreateSprite(
                        (x, y) => (x >= -0.85f && x <= 0.42f && Mathf.Abs(y) <= 0.12f)
                            || (x >= 0.15f && x <= 0.85f && Mathf.Abs(y) <= (0.85f - x) * 0.72f),
                        "WorldArrow");
                }

                return arrow;
            }
        }

        public static Sprite Ring
        {
            get
            {
                if (ring == null)
                {
                    ring = CreateSprite(
                        (x, y) =>
                        {
                            float radiusSquared = x * x + y * y;
                            return radiusSquared <= 0.9f && radiusSquared >= 0.64f;
                        },
                        "WorldRing");
                }

                return ring;
            }
        }

        private static Sprite CreateSprite(Func<float, float, bool> contains, string spriteName)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = spriteName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            var pixels = new Color32[TextureSize * TextureSize];
            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    float normalizedX = (x + 0.5f) / TextureSize * 2f - 1f;
                    float normalizedY = (y + 0.5f) / TextureSize * 2f - 1f;
                    pixels[y * TextureSize + x] = contains(normalizedX, normalizedY)
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                TextureSize);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
