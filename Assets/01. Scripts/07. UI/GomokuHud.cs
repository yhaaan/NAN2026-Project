using System;
using System.Collections.Generic;
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

        private Action<int> onShopSelection;
        private Action<int, int> onBoardClick;
        private StoneColor playerSide = StoneColor.White;
        private UnitInfoPanelView unitInfoPanel;
        private CombatResolver combat;

        private void Awake()
        {
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

        public void Initialize(
            Action<int, int> boardClick,
            Action<int> shopSelection,
            Action reroll,
            Action continueAction)
        {
            onBoardClick = boardClick;
            onShopSelection = shopSelection;

            for (int index = 0; index < shopSlots.Length; index++)
            {
                shopSlots[index].Initialize(index, HandleShopSelection);
            }

            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => reroll());
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => continueAction());
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
            shopPanel.SetActive(true);
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
        }

        public void HideShop()
        {
            shopPanel.SetActive(false);
            placementCursorView.SetSelection(null, playerSide);
            boardView.SetPlacementPreview(null);
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

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }

        private void HandleShopSelection(int index)
        {
            onShopSelection?.Invoke(index);
        }
    }
}
