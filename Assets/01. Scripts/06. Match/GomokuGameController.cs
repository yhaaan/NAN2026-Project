using System;
using System.Collections;
using System.Collections.Generic;
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
        private const float PvpCombatStep = 1f / 60f;

        [SerializeField] private UnitCatalogSO unitCatalog;
        [SerializeField] private GomokuHud hud;
        [SerializeField] private CameraEffectController cameraEffects;
        [SerializeField] private AudioClip placementSfx;
        [SerializeField] private AudioClip victorySfx;
        [SerializeField] private AudioClip defeatSfx;
        [SerializeField] private AudioClip prepareMusic;
        [SerializeField] private AudioClip battleMusic;
        [SerializeField, Min(0f)] private float musicFadeDuration = 0.75f;
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
        private FirstMatchCardNewsView cardNews;
        private readonly Queue<PvpMatchCommand> queuedPvpCommands = new Queue<PvpMatchCommand>();
        private PvpMatchCoordinator pvpCoordinator;
        private bool isPvp;
        private bool pvpCommandPending;
        private bool resultReady;
        private float pvpCombatAccumulator;

        private string OpponentLabel => isPvp ? "Opponent" : "COM";

        public StoneColor PlayerSide => playerSide;
        public float ShopHideDelayAfterPlacement => shopHideDelayAfterPlacement;
        public float CombatEndDelay => combatEndDelay;
        public AudioClip VictorySfx => victorySfx;
        public AudioClip DefeatSfx => defeatSfx;
        public AudioClip PrepareMusic => prepareMusic;
        public AudioClip BattleMusic => battleMusic;
        public float MusicFadeDuration => musicFadeDuration;

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

            pvpCoordinator = PvpMatchCoordinator.Instance;
            isPvp = pvpCoordinator != null && pvpCoordinator.IsMatchPending;
            hud.SetCombatSpeedControlsVisible(!isPvp);
            if (isPvp)
            {
                playerSide = pvpCoordinator.LocalSide;
                pvpCoordinator.CommandRequested += HandlePvpCommandRequest;
                pvpCoordinator.CommandReceived += HandlePvpCommandReceived;
                pvpCoordinator.BeginSynchronization(InitializePvpMatch);
                return;
            }

            var random = new System.Random();
            playerShop = new ShopState(
                unitCatalog.Units,
                random,
                usePlayerAdvantagePenalty: true);
            comShop = new ShopState(unitCatalog.Units, random);
            com = new GomokuCom(random, combatDuration);
            if (FirstMatchCardNewsView.HasBeenSeen)
            {
                StartMatch();
                return;
            }

            cardNews = FirstMatchCardNewsView.Create(
                hud.transform.parent,
                hud,
                StartMatch);
            if (cardNews == null)
            {
                StartMatch();
            }
        }

        private void Update()
        {
            if (!isPvp && comTurnPending)
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
                TickCombat();
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

            if (pvpCoordinator != null)
            {
                pvpCoordinator.CommandRequested -= HandlePvpCommandRequest;
                pvpCoordinator.CommandReceived -= HandlePvpCommandReceived;
                pvpCoordinator.EndSynchronization();
            }
        }

        private void InitializePvpMatch(int seed)
        {
            if (playerShop != null)
            {
                return;
            }

            playerShop = new ShopState(
                unitCatalog.Units,
                new System.Random(CreateSideSeed(seed, playerSide)));
            StoneColor enemySide = GomokuGame.OpponentOf(playerSide);
            comShop = new ShopState(
                unitCatalog.Units,
                new System.Random(CreateSideSeed(seed, enemySide)));
            StartMatch();
        }

        private static int CreateSideSeed(int seed, StoneColor side)
        {
            return seed ^ (side == StoneColor.Black ? 0x13579BDF : 0x02468ACE);
        }

        private void TickCombat()
        {
            if (!isPvp)
            {
                combat.Tick(Time.deltaTime);
                return;
            }

            pvpCombatAccumulator += Time.unscaledDeltaTime;
            int steps = 0;
            while (pvpCombatAccumulator >= PvpCombatStep
                && !combat.IsFinished
                && steps < 120)
            {
                combat.Tick(PvpCombatStep);
                pvpCombatAccumulator -= PvpCombatStep;
                steps++;
            }
        }

        private void StartMatch()
        {
            playerWins = 0;
            comWins = 0;
            matchFinished = false;
            lastGameWasDraw = false;
            waitingForContinue = false;
            resultReady = false;
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
            if (!isPvp)
            {
                playerSide = StoneColor.White;
            }

            game.StartNewGame(StoneColor.Black);
            playerShop.ResetForGame();
            comShop.ResetForGame();
            selectedOffer = -1;
            waitingForContinue = false;
            resultReady = false;
            CancelShopHideDelay();
            CancelCombatEndDelay();
            combatTransitionPending = false;
            hud.BindGame(game, playerSide);
            hud.HideResult();
            PreparePlacementTurn();
        }

        private void PreparePlacementTurn()
        {
            SoundManager.Instance.PlayMusic(prepareMusic, musicFadeDuration);
            hud.RefreshBoard();
            hud.HideCombatTimer();

            StoneColor currentSide = game.CurrentTurn;
            int unitDeficit = game.CountUnits(GomokuGame.OpponentOf(currentSide))
                - game.CountUnits(currentSide);

            if (game.CurrentTurn == playerSide)
            {
                playerShop.SetComebackDeficit(unitDeficit);
                playerShop.BeginPlacementTurn();
                selectedOffer = -1;
                comTurnPending = false;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
            }
            else
            {
                comShop.SetComebackDeficit(unitDeficit);
                comShop.BeginPlacementTurn();
                selectedOffer = -1;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, false);
                if (isPvp)
                {
                    comTurnPending = false;
                }
                else
                {
                    comDelayRemaining = comPlacementDelay;
                    comTurnPending = true;
                }
            }

            RefreshTurnStatus();
            DrainPvpCommandQueue();
        }

        private void HandleShopSelection(int offerIndex)
        {
            if (game.Phase != GamePhase.Placement
                || game.CurrentTurn != playerSide
                || pvpCommandPending
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
            if (game.Phase != GamePhase.Placement
                || game.CurrentTurn != playerSide
                || pvpCommandPending)
            {
                return;
            }

            if (isPvp)
            {
                if (playerShop.Gold < ShopState.RerollCost)
                {
                    return;
                }

                pvpCommandPending = true;
                pvpCoordinator.RequestCommand(new PvpMatchCommand(
                    PvpMatchCommandType.Reroll,
                    playerSide));
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
                || pvpCommandPending
                || selectedOffer < 0)
            {
                return;
            }

            if (isPvp)
            {
                UnitDefinitionSO definition = playerShop.Offers[selectedOffer];
                if (!game.CanPlace(x, y, definition))
                {
                    return;
                }

                pvpCommandPending = true;
                pvpCoordinator.RequestCommand(new PvpMatchCommand(
                    PvpMatchCommandType.Place,
                    playerSide,
                    x,
                    y,
                    selectedOffer));
                return;
            }

            if (game.TryPlace(x, y, playerShop.Offers[selectedOffer]))
            {
                AfterPlacement();
            }
        }

        private void PlaceComUnit()
        {
            if (isPvp
                || game.Phase != GamePhase.Placement || game.CurrentTurn == playerSide)
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

        private void HandlePvpCommandRequest(ulong senderClientId, PvpMatchCommand command)
        {
            if (!isPvp || !pvpCoordinator.IsHost)
            {
                return;
            }

            StoneColor senderSide = PvpMatchCoordinator.GetSideForClient(senderClientId);
            if (!ValidatePvpCommand(senderSide, command))
            {
                Debug.LogWarning($"Rejected invalid PvP command {command.Type} from client {senderClientId}.", this);
                return;
            }

            pvpCoordinator.ApproveCommand(command);
        }

        private bool ValidatePvpCommand(StoneColor senderSide, PvpMatchCommand command)
        {
            if (command.Type == PvpMatchCommandType.Continue)
            {
                return senderSide == StoneColor.Black && waitingForContinue;
            }

            if (command.Side != senderSide
                || game.Phase != GamePhase.Placement
                || game.CurrentTurn != senderSide)
            {
                return false;
            }

            ShopState shop = GetShopForSide(senderSide);
            if (command.Type == PvpMatchCommandType.Reroll)
            {
                return shop.Gold >= ShopState.RerollCost;
            }

            if (command.Type != PvpMatchCommandType.Place
                || command.OfferIndex < 0
                || command.OfferIndex >= shop.Offers.Count)
            {
                return false;
            }

            return game.CanPlace(command.X, command.Y, shop.Offers[command.OfferIndex]);
        }

        private void HandlePvpCommandReceived(PvpMatchCommand command)
        {
            if (!isPvp)
            {
                return;
            }

            queuedPvpCommands.Enqueue(command);
            DrainPvpCommandQueue();
        }

        private void DrainPvpCommandQueue()
        {
            if (!isPvp)
            {
                return;
            }

            while (queuedPvpCommands.Count > 0)
            {
                PvpMatchCommand command = queuedPvpCommands.Peek();
                if (!CanApplyPvpCommand(command))
                {
                    return;
                }

                queuedPvpCommands.Dequeue();
                ApplyPvpCommand(command);
            }
        }

        private bool CanApplyPvpCommand(PvpMatchCommand command)
        {
            if (command.Type == PvpMatchCommandType.Continue)
            {
                return resultReady;
            }

            return game.Phase == GamePhase.Placement
                && game.CurrentTurn == command.Side;
        }

        private void ApplyPvpCommand(PvpMatchCommand command)
        {
            if (command.Type == PvpMatchCommandType.Continue)
            {
                pvpCommandPending = false;
                ContinueAfterResult();
                return;
            }

            ShopState shop = GetShopForSide(command.Side);
            if (command.Side == playerSide)
            {
                pvpCommandPending = false;
            }

            if (command.Type == PvpMatchCommandType.Reroll)
            {
                if (!shop.TryReroll())
                {
                    Debug.LogError("The synchronized PvP reroll could not be applied.", this);
                    return;
                }

                if (command.Side == playerSide)
                {
                    selectedOffer = -1;
                    hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
                }

                return;
            }

            if (command.Type != PvpMatchCommandType.Place
                || command.OfferIndex < 0
                || command.OfferIndex >= shop.Offers.Count
                || !game.TryPlace(command.X, command.Y, shop.Offers[command.OfferIndex]))
            {
                Debug.LogError("The synchronized PvP placement could not be applied.", this);
                return;
            }

            AfterPlacement();
        }

        private ShopState GetShopForSide(StoneColor side)
        {
            return side == playerSide ? playerShop : comShop;
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
                Time.timeScale = isPvp ? 1f : combatSpeed;
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

            pvpCombatAccumulator = 0f;
            combat.Begin(game);
            SoundManager.Instance.PlayMusic(battleMusic, musicFadeDuration);
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
                hud.ShowResult(title, $"Player {playerWins} : {comWins} {OpponentLabel}", buttonLabel);
                hud.SetContinueButtonInteractable(!isPvp || pvpCoordinator.IsHost);
                resultReady = true;
                DrainPvpCommandQueue();
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
                    0.92f + index * 0.09f,
                    bypassConcurrencyLimit: true);

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
                $"Player {playerWins} : {comWins} {OpponentLabel}",
                buttonLabel);
            hud.SetContinueButtonInteractable(!isPvp || pvpCoordinator.IsHost);
            if (!matchFinished)
            {
                PlayResultSfx(playerWon);
            }

            if (matchFinished)
            {
                yield return new WaitForSecondsRealtime(MatchTitleDelay);
                hud.SetResultTitle(finalTitle);
                PlayResultSfx(playerWon);
                cameraEffects?.PlayScreenShake(playerWon ? 0.04f : 0.025f, 0.18f);
            }

            victoryRoutine = null;
            resultReady = true;
            DrainPvpCommandQueue();
        }

        private void PlayResultSfx(bool playerWon)
        {
            AudioClip resultSfx = playerWon ? victorySfx : defeatSfx;
            SoundManager.Instance.PlaySfx(resultSfx);
        }

        private void HandleContinue()
        {
            if (!waitingForContinue)
            {
                return;
            }

            if (isPvp)
            {
                if (!pvpCoordinator.IsHost || pvpCommandPending)
                {
                    return;
                }

                pvpCommandPending = true;
                pvpCoordinator.RequestCommand(new PvpMatchCommand(
                    PvpMatchCommandType.Continue,
                    playerSide));
                return;
            }

            ContinueAfterResult();
        }

        private void ContinueAfterResult()
        {
            if (matchFinished)
            {
                StartMatch();
            }
            else
            {
                StartGame();
            }
        }

        private void HandleCombatSpeedChanged(int speed)
        {
            if (isPvp)
            {
                return;
            }

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
