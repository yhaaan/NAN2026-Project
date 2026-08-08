using System;
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
        [SerializeField, Min(0.01f)] private float visualDiameter = 0.78f;
        [SerializeField] private Material feedbackParticleMaterial;
        [SerializeField] private Transform vfxRoot;

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
        private static Material runtimeParticleMaterial;

        public bool IsDying { get; private set; }
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public Transform BodyRoot => bodyRoot;
        public bool NormalizeSpriteSize => normalizeSpriteSize;
        public float VisualDiameter => visualDiameter;

        private void Awake()
        {
            EnsureVisuals();
        }

        public void Bind(BoardUnit targetUnit, UnitPresentationSO presentation, bool isPreview = false)
        {
            EnsureVisuals();
            if (isBound)
            {
                transform.localPosition = restLocalPosition;
            }

            KillTweens();
            isBound = true;
            accentColor = presentation != null ? presentation.AccentColor : targetUnit.Definition.RoleColor;
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

        public void PlayAction(UnitView target)
        {
            if (IsDying || preview)
            {
                return;
            }

            StopMotionTween();
            Vector3 direction = target != null
                ? (target.transform.localPosition - restLocalPosition).normalized
                : Vector3.up;
            Vector3 actionPosition = restLocalPosition + direction * 0.18f;
            motionTween = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOLocalMove(actionPosition, 0.11f).SetEase(Ease.OutQuad))
                .Append(transform.DOLocalMove(restLocalPosition, 0.11f).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    transform.localPosition = restLocalPosition;
                    motionTween = null;
                });
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
