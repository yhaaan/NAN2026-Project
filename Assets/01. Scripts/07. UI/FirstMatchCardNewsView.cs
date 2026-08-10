using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [DisallowMultipleComponent]
    public sealed class FirstMatchCardNewsView : MonoBehaviour
    {
        private const string RootName = "FirstMatchCardNews";
        public const string SeenPlayerPrefsKey = "NAN2026.FirstMatchCardNews.Seen.v1";
        private const float ShowDuration = 0.24f;
        private const float HideDuration = 0.18f;

        private static readonly IReadOnlyList<string> Messages = Array.AsReadOnly(new[]
        {
            "유닛으로 싸우며 오목을 완성하는, 전투 오목입니다!",
            "상점에서 유닛 하나를 골라 빈 교차점에 배치하세요.",
            "양쪽이 하나씩 배치하면, 최대 10초 동안 자동 전투가 시작됩니다.",
            "쓰러진 유닛은 보드에서 사라집니다. 배치와 조합으로 내 진형을 지키세요!",
            "유닛 다섯을 한 줄로 연결하고 끝까지 지켜내면 승리합니다!"
        });

        private static readonly string[] TexturePaths =
        {
            "CardNews/01_Overview",
            "CardNews/02_Placement",
            "CardNews/03_Combat",
            "CardNews/04_Defeat",
            "CardNews/05_Victory"
        };

        private static readonly Color PanelBorderColor = new Color(0.95f, 0.79f, 0.38f, 1f);
        private static readonly Color PanelColor = new Color(0.105f, 0.09f, 0.07f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.95f, 0.79f, 0.38f, 1f);
        private static readonly Color ButtonDisabledColor = new Color(0.34f, 0.31f, 0.27f, 0.82f);
        private static readonly Color DarkTextColor = new Color(0.12f, 0.105f, 0.08f, 1f);
        private static readonly Color LightTextColor = new Color(0.98f, 0.96f, 0.9f, 1f);

        private CanvasGroup rootCanvasGroup;
        private RectTransform panelRect;
        private RawImage pageImage;
        private TMP_Text messageText;
        private TMP_Text pageCounterText;
        private Button leftButton;
        private Button rightButton;
        private Button startButton;
        private Image[] progressDots;
        private Texture2D[] pageTextures;
        private Texture2D roundedTexture;
        private Sprite roundedSprite;
        private Action onStartGame;
        private Coroutine transitionRoutine;
        private int currentPageIndex;
        private bool inputLocked;
        private bool isVisible;

        public static IReadOnlyList<string> PageMessages => Messages;
        public static bool HasBeenSeen => PlayerPrefs.GetInt(SeenPlayerPrefsKey, 0) == 1;
        public int PageCount => Messages.Count;
        public int CurrentPageIndex => currentPageIndex;
        public bool IsVisible => isVisible;
        public float TransitionHideDuration => HideDuration;
        public Button LeftButton => leftButton;
        public Button RightButton => rightButton;
        public Button StartButton => startButton;
        public TMP_Text MessageText => messageText;
        public TMP_Text PageCounterText => pageCounterText;
        public RawImage PageImage => pageImage;

        public static FirstMatchCardNewsView Create(
            Transform parent,
            Component styleSource,
            Action startGame,
            TMP_FontAsset boldFont = null,
            TMP_FontAsset lightFont = null)
        {
            if (parent == null)
            {
                Debug.LogError("First match card news needs a Canvas parent.");
                return null;
            }

            var root = new GameObject(
                RootName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(FirstMatchCardNewsView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.SetParent(parent, false);
            Stretch(rootRect);
            rootRect.SetAsLastSibling();

            FirstMatchCardNewsView view = root.GetComponent<FirstMatchCardNewsView>();
            view.Build(styleSource, startGame, boldFont, lightFont);
            return view;
        }

        private void Build(
            Component styleSource,
            Action startGame,
            TMP_FontAsset boldFont,
            TMP_FontAsset lightFont)
        {
            onStartGame = startGame;
            rootCanvasGroup = GetComponent<CanvasGroup>();
            Image backdrop = GetComponent<Image>();
            backdrop.color = new Color(0.025f, 0.022f, 0.018f, 0.88f);
            backdrop.raycastTarget = true;

            roundedSprite = CreateRoundedSprite();
            boldFont ??= FindFont(styleSource, "Bold") ?? TMP_Settings.defaultFontAsset;
            lightFont ??= FindFont(styleSource, "Light") ?? boldFont;

            CreatePanel();
            CreatePageImage();
            CreateMessage(boldFont);
            CreateProgress(lightFont);
            CreateNavigation(boldFont);
            LoadPageTextures();

            currentPageIndex = 0;
            isVisible = true;
            RefreshPage();

            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = true;
            panelRect.localScale = Vector3.one * 0.96f;
            transitionRoutine = StartCoroutine(AnimateVisibility(true));
        }

        private void OnDestroy()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            leftButton?.onClick.RemoveListener(ShowPreviousPage);
            rightButton?.onClick.RemoveListener(ShowNextPage);
            startButton?.onClick.RemoveListener(BeginGame);
            if (roundedSprite != null)
            {
                Destroy(roundedSprite);
            }

            if (roundedTexture != null)
            {
                Destroy(roundedTexture);
            }
        }

        public void ShowPreviousPage()
        {
            if (inputLocked || currentPageIndex <= 0)
            {
                return;
            }

            currentPageIndex--;
            RefreshPage();
            Select(rightButton);
        }

        public void ShowNextPage()
        {
            if (inputLocked || currentPageIndex >= PageCount - 1)
            {
                return;
            }

            currentPageIndex++;
            RefreshPage();
            Select(currentPageIndex == PageCount - 1 ? startButton : rightButton);
        }

        public void BeginGame()
        {
            if (inputLocked || currentPageIndex != PageCount - 1)
            {
                return;
            }

            PlayerPrefs.SetInt(SeenPlayerPrefsKey, 1);
            PlayerPrefs.Save();
            inputLocked = true;
            rootCanvasGroup.interactable = false;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(AnimateVisibility(false));
        }

        private void CreatePanel()
        {
            Image shadow = CreateImage("PanelShadow", transform, roundedSprite);
            shadow.color = new Color(0f, 0f, 0f, 0.48f);
            SetRect(shadow.rectTransform, new Vector2(0f, 4f), new Vector2(1136f, 736f));

            Image border = CreateImage("PanelBorder", transform, roundedSprite);
            border.color = PanelBorderColor;
            SetRect(border.rectTransform, Vector2.zero, new Vector2(1128f, 728f));

            Image panel = CreateImage("Panel", border.transform, roundedSprite);
            panel.color = PanelColor;
            Stretch(panel.rectTransform, 7f);
            panelRect = border.rectTransform;
        }

        private void CreatePageImage()
        {
            Image frame = CreateImage("ImageFrame", panelRect, roundedSprite);
            frame.color = new Color(0.98f, 0.87f, 0.58f, 1f);
            SetRect(frame.rectTransform, new Vector2(0f, 36f), new Vector2(982f, 552f));

            Image maskImage = CreateImage("ImageMask", frame.transform, roundedSprite);
            maskImage.color = Color.white;
            Stretch(maskImage.rectTransform, 7f);
            Mask mask = maskImage.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var imageObject = new GameObject(
                "PageImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(maskImage.transform, false);
            Stretch(imageRect);
            pageImage = imageObject.GetComponent<RawImage>();
            pageImage.raycastTarget = false;
        }

        private void CreateMessage(TMP_FontAsset font)
        {
            messageText = CreateText("Message", panelRect, font, 31f, LightTextColor);
            SetRect(messageText.rectTransform, new Vector2(0f, -292f), new Vector2(970f, 104f));
            messageText.textWrappingMode = TextWrappingModes.Normal;
            messageText.overflowMode = TextOverflowModes.Ellipsis;
        }

        private void CreateProgress(TMP_FontAsset font)
        {
            RectTransform dotsRoot = CreateRect("ProgressDots", panelRect);
            SetRect(dotsRoot, new Vector2(0f, 328f), new Vector2(220f, 24f));
            progressDots = new Image[PageCount];
            float startX = -(PageCount - 1) * 17f;
            for (int index = 0; index < progressDots.Length; index++)
            {
                Image dot = CreateImage($"Dot{index + 1}", dotsRoot, roundedSprite);
                SetRect(dot.rectTransform, new Vector2(startX + index * 34f, 0f), new Vector2(15f, 15f));
                progressDots[index] = dot;
            }

            pageCounterText = CreateText("PageCounter", panelRect, font, 18f, LightTextColor);
            SetRect(pageCounterText.rectTransform, new Vector2(460f, 328f), new Vector2(100f, 28f));
            pageCounterText.alignment = TextAlignmentOptions.Right;
        }

        private void CreateNavigation(TMP_FontAsset font)
        {
            leftButton = CreateButton("PreviousButton", transform, "<", font,
                new Vector2(-624f, 28f), new Vector2(82f, 82f), 47f);
            rightButton = CreateButton("NextButton", transform, ">", font,
                new Vector2(624f, 28f), new Vector2(82f, 82f), 47f);
            startButton = CreateButton("StartGameButton", transform, "게임 시작", font,
                new Vector2(0f, -392f), new Vector2(270f, 66f), 27f);

            leftButton.onClick.AddListener(ShowPreviousPage);
            rightButton.onClick.AddListener(ShowNextPage);
            startButton.onClick.AddListener(BeginGame);
        }

        private void LoadPageTextures()
        {
            pageTextures = new Texture2D[TexturePaths.Length];
            for (int index = 0; index < TexturePaths.Length; index++)
            {
                pageTextures[index] = Resources.Load<Texture2D>(TexturePaths[index]);
                if (pageTextures[index] == null)
                {
                    Debug.LogError($"Card news texture is missing: Resources/{TexturePaths[index]}", this);
                    pageTextures[index] = Texture2D.grayTexture;
                }
            }
        }

        private void RefreshPage()
        {
            pageImage.texture = pageTextures[currentPageIndex];
            messageText.text = Messages[currentPageIndex];
            pageCounterText.text = $"{currentPageIndex + 1} / {PageCount}";
            leftButton.interactable = currentPageIndex > 0;
            bool isLastPage = currentPageIndex == PageCount - 1;
            rightButton.gameObject.SetActive(!isLastPage);
            startButton.gameObject.SetActive(isLastPage);

            for (int index = 0; index < progressDots.Length; index++)
            {
                bool active = index == currentPageIndex;
                progressDots[index].color = active
                    ? PanelBorderColor
                    : new Color(0.48f, 0.44f, 0.38f, 0.9f);
                progressDots[index].rectTransform.sizeDelta = active
                    ? new Vector2(28f, 15f)
                    : new Vector2(15f, 15f);
            }
        }

        private IEnumerator AnimateVisibility(bool showing)
        {
            float duration = showing ? ShowDuration : HideDuration;
            float startAlpha = rootCanvasGroup.alpha;
            float targetAlpha = showing ? 1f : 0f;
            Vector3 startScale = panelRect.localScale;
            Vector3 targetScale = showing ? Vector3.one : Vector3.one * 0.97f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = duration <= Mathf.Epsilon ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                rootCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                panelRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
                yield return null;
            }

            rootCanvasGroup.alpha = targetAlpha;
            panelRect.localScale = targetScale;
            transitionRoutine = null;
            if (showing)
            {
                rootCanvasGroup.interactable = true;
                Select(rightButton);
                yield break;
            }

            isVisible = false;
            gameObject.SetActive(false);
            Action startGame = onStartGame;
            onStartGame = null;
            startGame?.Invoke();
        }

        private Button CreateButton(
            string objectName,
            Transform parent,
            string label,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size,
            float fontSize)
        {
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.SetParent(parent, false);
            SetRect(buttonRect, position, size);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = ButtonColor;
            colors.highlightedColor = new Color(1f, 0.9f, 0.62f, 1f);
            colors.pressedColor = new Color(0.82f, 0.62f, 0.24f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = ButtonDisabledColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text labelText = CreateText("Text", buttonRect, font, fontSize, DarkTextColor);
            Stretch(labelText.rectTransform, 4f);
            labelText.text = label;
            return button;
        }

        private Image CreateImage(string objectName, Transform parent, Sprite sprite)
        {
            var imageObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateText(
            string objectName,
            Transform parent,
            TMP_FontAsset font,
            float fontSize,
            Color color)
        {
            RectTransform rect = CreateRect(objectName, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private Sprite CreateRoundedSprite()
        {
            const int size = 64;
            const int radius = 18;
            roundedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CardNewsRoundedTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cornerX = x < radius
                        ? radius - x
                        : x >= size - radius ? x - (size - radius - 1) : 0f;
                    float cornerY = y < radius
                        ? radius - y
                        : y >= size - radius ? y - (size - radius - 1) : 0f;
                    bool visible = cornerX <= 0f
                        || cornerY <= 0f
                        || cornerX * cornerX + cornerY * cornerY <= radius * radius;
                    pixels[y * size + x] = visible
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            roundedTexture.SetPixels32(pixels);
            roundedTexture.Apply();
            return Sprite.Create(
                roundedTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static TMP_FontAsset FindFont(Component source, string nameFragment)
        {
            if (source == null)
            {
                return null;
            }

            foreach (TMP_Text text in source.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != null
                    && text.font.name.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return text.font;
                }
            }

            return null;
        }

        private static RectTransform CreateRect(string objectName, Transform parent)
        {
            var rectObject = new GameObject(objectName, typeof(RectTransform));
            RectTransform rect = rectObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void Select(Button button)
        {
            if (EventSystem.current != null
                && button != null
                && button.gameObject.activeInHierarchy
                && button.interactable)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }
}
