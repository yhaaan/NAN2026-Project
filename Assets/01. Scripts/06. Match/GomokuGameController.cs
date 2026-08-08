using System;
using System.Collections;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class GomokuGameController : MonoBehaviour
    {
        private const int WinsNeeded = 2;
        private const float VictoryPauseDuration = 0.25f;
        private const float VictoryJumpInterval = 0.16f;
        private const float FinalJumpSettleDuration = 0.43f;
        private const float VictoryLineDuration = 0.3f;
        private const float MatchTitleDelay = 0.38f;
        private const int MinCombatSpeed = 1;
        private const int MaxCombatSpeed = 5;

        [SerializeField] private UnitCatalogSO unitCatalog;
        [SerializeField] private GomokuHud hud;
        [SerializeField] private CameraEffectController cameraEffects;
        [SerializeField] private AudioClip placementSfx;
        [SerializeField, Min(0f)] private float comPlacementDelay = 0.45f;
        [SerializeField, Min(1f)] private float combatDuration = 10f;
        [SerializeField, Min(0f)] private float shopHideDelayAfterPlacement = 0.2f;
        [SerializeField, Min(0f)] private float combatEndDelay = 0.2f;

        private readonly GomokuGame game = new GomokuGame();
        private ShopState playerShop;
        private ShopState comShop;
        private GomokuCom com;
        private CombatResolver combat;
        private StoneColor playerSide;
        private int selectedOffer = -1;
        private int playerWins;
        private int comWins;
        private float comDelayRemaining;
        private bool comTurnPending;
        private bool waitingForContinue;
        private bool matchFinished;
        private bool lastGameWasDraw;
        private int combatSpeed = MinCombatSpeed;
        private bool combatTransitionPending;
        private Coroutine victoryRoutine;
        private Coroutine shopHideDelayRoutine;
        private Coroutine combatEndDelayRoutine;

        public StoneColor PlayerSide => playerSide;
        public float ShopHideDelayAfterPlacement => shopHideDelayAfterPlacement;
        public float CombatEndDelay => combatEndDelay;

        private void Start()
        {
            if (unitCatalog == null || unitCatalog.Units.Count == 0 || hud == null)
            {
                Debug.LogError("Gomoku MVP scene is missing its UnitCatalog or HUD reference.", this);
                enabled = false;
                return;
            }

            if (!unitCatalog.TryValidate(out string catalogError))
            {
                Debug.LogError($"Unit catalog is invalid: {catalogError}", this);
                enabled = false;
                return;
            }

            var random = new System.Random();
            playerShop = new ShopState(unitCatalog.Units, random);
            comShop = new ShopState(unitCatalog.Units, random);
            com = new GomokuCom(random);
            combat = new CombatResolver(combatDuration);
            combat.ActionResolved += HandleCombatAction;
            Time.timeScale = 1f;
            hud.Initialize(
                HandleBoardClick,
                HandleShopSelection,
                HandleReroll,
                HandleContinue,
                HandleCombatSpeedChanged,
                combatSpeed);
            hud.SetCombatResolver(combat);
            StartMatch();
        }

        private void Update()
        {
            if (comTurnPending)
            {
                comDelayRemaining -= Time.deltaTime;
                if (comDelayRemaining <= 0f)
                {
                    comTurnPending = false;
                    PlaceComUnit();
                }
            }

            if (game.Phase == GamePhase.Combat
                && !waitingForContinue
                && !combatTransitionPending)
            {
                combat.Tick(Time.deltaTime);
                hud.SetCombatElapsed(combat.Elapsed);

                if (combat.IsFinished)
                {
                    BeginCombatEndDelay();
                }
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            CancelShopHideDelay();
            CancelCombatEndDelay();

            if (victoryRoutine != null)
            {
                StopCoroutine(victoryRoutine);
                victoryRoutine = null;
            }

            if (combat != null)
            {
                combat.ActionResolved -= HandleCombatAction;
            }
        }

        private void StartMatch()
        {
            playerWins = 0;
            comWins = 0;
            matchFinished = false;
            lastGameWasDraw = false;
            waitingForContinue = false;
            if (victoryRoutine != null)
            {
                StopCoroutine(victoryRoutine);
                victoryRoutine = null;
            }

            hud.HideResult();
            StartGame();
        }

        private void StartGame()
        {
            Time.timeScale = 1f;
            playerSide = StoneColor.White;
            game.StartNewGame(StoneColor.Black);
            playerShop.ResetForGame();
            comShop.ResetForGame();
            selectedOffer = -1;
            waitingForContinue = false;
            CancelShopHideDelay();
            CancelCombatEndDelay();
            combatTransitionPending = false;
            hud.BindGame(game, playerSide);
            hud.HideResult();
            PreparePlacementTurn();
        }

        private void PreparePlacementTurn()
        {
            hud.RefreshBoard();
            hud.HideCombatTimer();

            if (game.CurrentTurn == playerSide)
            {
                playerShop.BeginPlacementTurn();
                selectedOffer = -1;
                comTurnPending = false;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
            }
            else
            {
                comShop.BeginPlacementTurn();
                selectedOffer = -1;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, false);
                comDelayRemaining = comPlacementDelay;
                comTurnPending = true;
            }

            RefreshTurnStatus();
        }

        private void HandleShopSelection(int offerIndex)
        {
            if (game.Phase != GamePhase.Placement
                || game.CurrentTurn != playerSide
                || offerIndex < 0
                || offerIndex >= playerShop.Offers.Count)
            {
                return;
            }

            selectedOffer = offerIndex;
            hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
        }

        private void HandleReroll()
        {
            if (game.Phase != GamePhase.Placement || game.CurrentTurn != playerSide)
            {
                return;
            }

            if (playerShop.TryReroll())
            {
                selectedOffer = -1;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
            }
        }

        private void HandleBoardClick(int x, int y)
        {
            if (game.Phase != GamePhase.Placement
                || game.CurrentTurn != playerSide
                || selectedOffer < 0)
            {
                return;
            }

            if (game.TryPlace(x, y, playerShop.Offers[selectedOffer]))
            {
                AfterPlacement();
            }
        }

        private void PlaceComUnit()
        {
            if (game.Phase != GamePhase.Placement || game.CurrentTurn == playerSide)
            {
                return;
            }

            StoneColor comSide = GomokuGame.OpponentOf(playerSide);
            ComDecision decision = com.ChooseMove(game, comShop.Offers, comSide);

            if (decision.Score < 45f && comShop.Gold > 0 && comShop.TryReroll())
            {
                decision = com.ChooseMove(game, comShop.Offers, comSide);
            }

            if (decision.OfferIndex >= 0
                && game.TryPlace(decision.X, decision.Y, comShop.Offers[decision.OfferIndex]))
            {
                AfterPlacement();
            }
        }

        private void AfterPlacement()
        {
            selectedOffer = -1;
            hud.ClearShopSelection();
            hud.RefreshBoard();
            hud.PlayPlacementImpact();
            cameraEffects?.PlayPlacementShake();
            SoundManager.Instance.PlaySfx(placementSfx);

            if (game.IsGameOver)
            {
                FinishGame();
            }
            else if (game.Phase == GamePhase.Combat)
            {
                combatTransitionPending = true;
                HideShopAfterPlacement(BeginCombat);
                hud.ShowCombatTimer(combat.Duration);
                Time.timeScale = combatSpeed;
                RefreshTurnStatus();
            }
            else
            {
                PreparePlacementTurn();
            }
        }

        private void HandleCombatAction(CombatActionEvent actionEvent)
        {
            hud.PlayCombatAction(actionEvent);
        }

        private void BeginCombat()
        {
            if (!combatTransitionPending || game.Phase != GamePhase.Combat)
            {
                return;
            }

            combat.Begin(game);
            combatTransitionPending = false;
        }

        private void HideShopAfterPlacement(Action onHidden = null)
        {
            CancelShopHideDelay();
            if (shopHideDelayAfterPlacement <= Mathf.Epsilon)
            {
                hud.HideShop(onHidden);
                return;
            }

            shopHideDelayRoutine = StartCoroutine(
                DelayShopHideAfterPlacement(onHidden));
        }

        private IEnumerator DelayShopHideAfterPlacement(Action onHidden)
        {
            yield return new WaitForSecondsRealtime(shopHideDelayAfterPlacement);
            shopHideDelayRoutine = null;
            hud.HideShop(onHidden);
        }

        private void CancelShopHideDelay()
        {
            if (shopHideDelayRoutine == null)
            {
                return;
            }

            StopCoroutine(shopHideDelayRoutine);
            shopHideDelayRoutine = null;
        }

        private void BeginCombatEndDelay()
        {
            Time.timeScale = 1f;
            game.CompleteCombat();
            CancelCombatEndDelay();
            if (combatEndDelay <= Mathf.Epsilon)
            {
                CompleteCombatEndTransition();
                return;
            }

            combatEndDelayRoutine = StartCoroutine(DelayCombatEnd());
        }

        private IEnumerator DelayCombatEnd()
        {
            yield return new WaitForSecondsRealtime(combatEndDelay);
            combatEndDelayRoutine = null;
            CompleteCombatEndTransition();
        }

        private void CompleteCombatEndTransition()
        {
            if (game.IsGameOver)
            {
                FinishGame();
            }
            else
            {
                PreparePlacementTurn();
            }
        }

        private void CancelCombatEndDelay()
        {
            if (combatEndDelayRoutine == null)
            {
                return;
            }

            StopCoroutine(combatEndDelayRoutine);
            combatEndDelayRoutine = null;
        }

        private void FinishGame()
        {
            Time.timeScale = 1f;
            comTurnPending = false;
            CancelShopHideDelay();
            CancelCombatEndDelay();
            combatTransitionPending = false;
            waitingForContinue = true;
            lastGameWasDraw = game.Winner == StoneColor.None;
            bool playerWon = game.Winner == playerSide;
            if (lastGameWasDraw)
            {
                // A full board restarts without changing the match score or first player.
            }
            else if (playerWon)
            {
                playerWins++;
            }
            else
            {
                comWins++;
            }

            matchFinished = playerWins >= WinsNeeded || comWins >= WinsNeeded;
            string title = lastGameWasDraw
                ? "Game Draw"
                : matchFinished
                ? (playerWins >= WinsNeeded ? "VICTORY" : "DEFEAT")
                : (playerWon ? "Game Victory" : "Game Defeat");
            string buttonLabel = lastGameWasDraw
                ? "Replay Game"
                : matchFinished ? "Restart Match" : "Next Game";

            HideShopAfterPlacement();
            hud.HideCombatTimer();

            if (lastGameWasDraw)
            {
                RefreshTurnStatus();
                hud.ShowResult(title, $"Player {playerWins} : {comWins} COM", buttonLabel);
                return;
            }

            victoryRoutine = StartCoroutine(PlayVictorySequence(
                playerWon,
                title,
                buttonLabel));
        }

        private IEnumerator PlayVictorySequence(
            bool playerWon,
            string finalTitle,
            string buttonLabel)
        {
            hud.PrepareVictory();
            yield return new WaitForSecondsRealtime(VictoryPauseDuration);

            for (int index = 0; index < game.WinningUnits.Count; index++)
            {
                bool finalStone = index == game.WinningUnits.Count - 1;
                hud.PlayVictoryStone(game.WinningUnits[index], finalStone);
                SoundManager.Instance.PlaySfx(
                    placementSfx,
                    finalStone ? 1f : 0.78f,
                    0.92f + index * 0.09f);

                if (finalStone)
                {
                    cameraEffects?.PlayScreenShake(0.025f, 0.13f);
                }

                yield return new WaitForSecondsRealtime(
                    finalStone ? FinalJumpSettleDuration : VictoryJumpInterval);
            }

            hud.RevealVictory(game.WinningUnits, VictoryLineDuration);
            yield return new WaitForSecondsRealtime(VictoryLineDuration);

            RefreshTurnStatus();
            string gameTitle = playerWon ? "GAME WIN" : "GAME LOSE";
            hud.ShowResult(
                gameTitle,
                $"Player {playerWins} : {comWins} COM",
                buttonLabel);
            SoundManager.Instance.PlaySfx(placementSfx, 1f, 1.38f);

            if (matchFinished)
            {
                yield return new WaitForSecondsRealtime(MatchTitleDelay);
                hud.SetResultTitle(finalTitle);
                SoundManager.Instance.PlaySfx(placementSfx, 1f, 1.52f);
                if (playerWon)
                {
                    SoundManager.Instance.PlaySfx(placementSfx, 0.72f, 1.76f);
                }

                cameraEffects?.PlayScreenShake(playerWon ? 0.04f : 0.025f, 0.18f);
            }

            victoryRoutine = null;
        }

        private void HandleContinue()
        {
            if (!waitingForContinue)
            {
                return;
            }

            if (matchFinished)
            {
                StartMatch();
            }
            else if (lastGameWasDraw)
            {
                StartGame();
            }
            else
            {
                StartGame();
            }
        }

        private void HandleCombatSpeedChanged(int speed)
        {
            combatSpeed = Mathf.Clamp(speed, MinCombatSpeed, MaxCombatSpeed);
            if (game.Phase == GamePhase.Combat && !waitingForContinue)
            {
                Time.timeScale = combatSpeed;
            }
        }

        private void RefreshTurnStatus()
        {
            TurnUiPhase phase = game.Phase == GamePhase.Combat
                ? TurnUiPhase.Combat
                : game.CurrentTurn == playerSide ? TurnUiPhase.Player : TurnUiPhase.Enemy;
            hud.SetTurnStatus(game.TurnNumber, phase, playerWins, comWins);
        }
    }
}
