using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace NAN2026.Gomoku
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class GomokuHud : MonoBehaviour
    {
        [SerializeField] private GomokuBoardView boardView;
        [SerializeField] private PlacementCursorView placementCursorView;
        [SerializeField] private UnitInfoPanelView unitInfoPanelPrefab;
        [SerializeField] private TurnStatusView turnStatusView;
        [SerializeField] private GameObject combatSpeedPanel;
        [SerializeField] private Button combatSpeedButton;
        [SerializeField] private Text combatSpeedText;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Text goldText;
        [SerializeField] private Text selectedText;
        [SerializeField] private Button rerollButton;
        [SerializeField] private ShopSlotView[] shopSlots;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text resultTitleText;
        [SerializeField] private Text resultScoreText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text continueButtonText;

        [Header("Shop Transition")]
        [SerializeField, Min(0f)] private float shopShowDuration = 0.38f;
        [SerializeField, Min(0f)] private float shopHideDuration = 0.32f;
        [SerializeField, Min(0f)] private float shopHiddenPadding = 18f;

        private Action<int> onShopSelection;
        private Action<int, int> onBoardClick;
        private Action<int> onCombatSpeedChanged;
        private int combatSpeed = 1;
        private StoneColor playerSide = StoneColor.White;
        private UnitInfoPanelView unitInfoPanel;
        private CombatResolver combat;
        private RectTransform shopRect;
        private CanvasGroup shopCanvasGroup;
        private Vector2 shopShownPosition;
        private Vector2 shopHiddenPosition;
        private Tween shopTransition;
        private Action pendingHideCompletion;
        private bool shopPresentationInitialized;
        private bool shopVisible;
        private bool shopShowWaitingForCamera;

        public event Action<bool, float, Action> CameraFramingRequested;

        public RectTransform BoardRect => boardView != null ? boardView.rectTransform : null;
        public RectTransform ShopRect => shopRect != null
            ? shopRect
            : shopPanel != null ? shopPanel.transform as RectTransform : null;
        public RectTransform TurnStatusRect => turnStatusView != null
            ? turnStatusView.transform as RectTransform
            : null;
        public float ShopShowDuration => shopShowDuration;
        public float ShopHideDuration => shopHideDuration;

        private void Awake()
        {
            InitializeShopPresentation();

            if (unitInfoPanelPrefab != null)
            {
                unitInfoPanel = Instantiate(unitInfoPanelPrefab, transform, false);
                unitInfoPanel.name = unitInfoPanelPrefab.name;
                unitInfoPanel.transform.SetSiblingIndex(Mathf.Max(0, transform.childCount - 2));
            }
        }

        private void LateUpdate()
        {
            BoardUnit hoveredUnit = boardView != null
                && boardView.PointerState.Mode == BoardPointerMode.UnitHover
                ? boardView.PointerState.HoveredUnit
                : null;
            unitInfoPanel?.Refresh(hoveredUnit, combat, playerSide);
        }

        private void OnDestroy()
        {
            shopTransition?.Kill();
            shopTransition = null;
        }

        public void Initialize(
            Action<int, int> boardClick,
            Action<int> shopSelection,
            Action reroll,
            Action continueAction,
            Action<int> combatSpeedChanged,
            int initialCombatSpeed)
        {
            onBoardClick = boardClick;
            onShopSelection = shopSelection;
            onCombatSpeedChanged = combatSpeedChanged;
            combatSpeed = Mathf.Clamp(initialCombatSpeed, 1, 5);

            for (int index = 0; index < shopSlots.Length; index++)
            {
                shopSlots[index].Initialize(index, HandleShopSelection);
            }

            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => reroll());
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continueAction());
            combatSpeedButton.onClick.RemoveAllListeners();
            combatSpeedButton.onClick.AddListener(CycleCombatSpeed);
            combatSpeedPanel.SetActive(true);
            RefreshCombatSpeedLabel();
        }

        public void BindGame(GomokuGame game, StoneColor playerSide)
        {
            boardView.Bind(game, playerSide, onBoardClick);
            this.playerSide = playerSide;
        }

        public void SetCombatResolver(CombatResolver resolver)
        {
            combat = resolver;
        }

        public void SetTurnStatus(
            int turnNumber,
            TurnUiPhase phase,
            int playerScore,
            int enemyScore)
        {
            turnStatusView.SetHeader(turnNumber, phase, playerScore, enemyScore);
        }

        public void ShowShop(
            IReadOnlyList<UnitDefinitionSO> offers,
            int gold,
            int selectedIndex,
            bool interactable)
        {
            EnsureShopPresentation();
            bool shouldAnimate = !shopVisible;
            shopVisible = true;
            if (!shouldAnimate && !shopShowWaitingForCamera)
            {
                shopPanel.SetActive(true);
            }

            goldText.text = $"Gold: {gold}";
            rerollButton.interactable = interactable && gold >= ShopState.RerollCost;

            for (int index = 0; index < shopSlots.Length; index++)
            {
                shopSlots[index].gameObject.SetActive(index < offers.Count);
                if (index < offers.Count)
                {
                    shopSlots[index].Bind(offers[index], index == selectedIndex, interactable);
                }
            }

            UnitDefinitionSO selectedDefinition = interactable
                && selectedIndex >= 0
                && selectedIndex < offers.Count
                ? offers[selectedIndex]
                : null;
            placementCursorView.SetSelection(selectedDefinition, playerSide);

            boardView.SetPlacementPreview(selectedDefinition);

            selectedText.text = selectedDefinition != null
                ? $"선택: {selectedDefinition.DisplayName}"
                : (interactable ? "유닛을 선택하세요" : "COM 배치 대기 중");

            if (shouldAnimate)
            {
                pendingHideCompletion = null;
                shopShowWaitingForCamera = true;
                RequestCameraFraming(false, shopShowDuration, BeginShopShowAfterCamera);
            }
        }

        public void ClearShopSelection()
        {
            placementCursorView.SetSelection(null, playerSide);
            boardView.SetPlacementPreview(null);

            foreach (ShopSlotView shopSlot in shopSlots)
            {
                if (shopSlot != null && shopSlot.gameObject.activeSelf)
                {
                    shopSlot.SetSelected(false);
                }
            }

            selectedText.text = "배치 완료";
        }

        public void HideShop()
        {
            HideShop(null);
        }

        public void HideShop(Action onHidden)
        {
            placementCursorView.SetSelection(null, playerSide);
            boardView.SetPlacementPreview(null);

            EnsureShopPresentation();
            if (!shopVisible)
            {
                if (shopTransition != null)
                {
                    pendingHideCompletion += onHidden;
                }
                else
                {
                    onHidden?.Invoke();
                }

                return;
            }

            shopVisible = false;
            shopShowWaitingForCamera = false;
            pendingHideCompletion += onHidden;
            StartShopTransition(false, shopHideDuration);
        }

        public void ShowCombatTimer(float duration)
        {
            turnStatusView.ShowCombatTimer(duration);
        }

        public void SetCombatElapsed(float elapsedSeconds)
        {
            turnStatusView.SetCombatElapsed(elapsedSeconds);
        }

        public void HideCombatTimer()
        {
            turnStatusView.HideCombatTimer();
        }

        public void RefreshBoard()
        {
            boardView.Refresh();
        }

        public void PlayPlacementImpact()
        {
            boardView.PlayPlacementImpact();
        }

        public void PrepareVictory()
        {
            boardView.PrepareVictory();
        }

        public void PlayVictoryStone(BoardUnit unit, bool finalStone)
        {
            boardView.PlayVictoryStone(unit, finalStone);
        }

        public void RevealVictory(IReadOnlyList<BoardUnit> winningUnits, float duration)
        {
            boardView.RevealVictory(winningUnits, duration);
        }

        public void ShowDamage(int x, int y, int damage, bool causedByPlayer)
        {
            boardView.ShowDamage(x, y, damage, causedByPlayer);
        }

        public void ShowHeal(int x, int y, int healing)
        {
            boardView.ShowHeal(x, y, healing);
        }

        public void PlayCombatAction(CombatActionEvent actionEvent)
        {
            boardView.PlayCombatAction(actionEvent);
        }

        public void ShowResult(string title, string score, string buttonLabel)
        {
            resultPanel.SetActive(true);
            resultTitleText.text = title;
            resultScoreText.text = score;
            continueButtonText.text = buttonLabel;
        }

        public void SetResultTitle(string title)
        {
            resultTitleText.text = title;
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }

        private void CycleCombatSpeed()
        {
            combatSpeed = combatSpeed >= 5 ? 1 : combatSpeed + 1;
            RefreshCombatSpeedLabel();
            onCombatSpeedChanged?.Invoke(combatSpeed);
        }

        private void RefreshCombatSpeedLabel()
        {
            combatSpeedText.text = $"x{combatSpeed}";
        }

        private void HandleShopSelection(int index)
        {
            onShopSelection?.Invoke(index);
        }

        private void InitializeShopPresentation()
        {
            EnsureShopPresentation();
            if (!shopPresentationInitialized)
            {
                return;
            }

            shopVisible = false;
            shopShowWaitingForCamera = false;
            shopRect.anchoredPosition = shopHiddenPosition;
            SetShopInteraction(false);
            shopPanel.SetActive(false);
        }

        private void EnsureShopPresentation()
        {
            if (shopPresentationInitialized || shopPanel == null)
            {
                return;
            }

            shopRect = shopPanel.transform as RectTransform;
            if (shopRect == null)
            {
                Debug.LogError("ShopPanel requires a RectTransform.", this);
                return;
            }

            shopCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (shopCanvasGroup == null && Application.isPlaying)
            {
                shopCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
            }

            shopShownPosition = shopRect.anchoredPosition;
            float travelDistance = shopRect.rect.height + shopHiddenPadding;
            shopHiddenPosition = shopShownPosition + Vector2.down * travelDistance;
            shopVisible = shopPanel.activeSelf;
            shopPresentationInitialized = true;
        }

        private void StartShopTransition(bool visible, float duration)
        {
            if (shopTransition != null)
            {
                shopTransition.Kill();
                shopTransition = null;
            }

            SetShopInteraction(false);

            if (!Application.isPlaying
                || !isActiveAndEnabled
                || duration <= Mathf.Epsilon)
            {
                CompleteShopTransition(visible);
                return;
            }

            Ease ease = visible ? Ease.OutQuart : Ease.InQuart;
            Vector2 targetPosition = visible ? shopShownPosition : shopHiddenPosition;
            shopTransition = DOTween
                .To(
                    () => shopRect.anchoredPosition,
                    value => shopRect.anchoredPosition = value,
                    targetPosition,
                    duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() => CompleteShopTransition(visible));
        }

        private void CompleteShopTransition(bool visible)
        {
            shopTransition = null;
            shopRect.anchoredPosition = visible ? shopShownPosition : shopHiddenPosition;
            SetShopInteraction(visible);

            if (!visible)
            {
                shopPanel.SetActive(false);
                Action completion = pendingHideCompletion;
                pendingHideCompletion = null;
                RequestCameraFraming(true, shopHideDuration, completion);
            }
        }

        private void BeginShopShowAfterCamera()
        {
            shopShowWaitingForCamera = false;
            if (!shopVisible)
            {
                return;
            }

            shopPanel.SetActive(true);
            StartShopTransition(true, shopShowDuration);
        }

        private void RequestCameraFraming(
            bool expanded,
            float duration,
            Action onCompleted)
        {
            Action<bool, float, Action> request = CameraFramingRequested;
            if (request == null)
            {
                onCompleted?.Invoke();
                return;
            }

            request.Invoke(expanded, duration, onCompleted);
        }

        private void SetShopInteraction(bool interactable)
        {
            if (shopCanvasGroup == null)
            {
                return;
            }

            shopCanvasGroup.interactable = interactable;
            shopCanvasGroup.blocksRaycasts = interactable;
        }
    }
}
