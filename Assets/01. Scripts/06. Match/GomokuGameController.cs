using System;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public sealed class GomokuGameController : MonoBehaviour
    {
        private const int WinsNeeded = 2;

        [SerializeField] private UnitCatalogSO unitCatalog;
        [SerializeField] private GomokuHud hud;
        [SerializeField, Min(0f)] private float comPlacementDelay = 0.45f;
        [SerializeField, Min(1f)] private float combatDuration = 10f;

        private readonly GomokuGame game = new GomokuGame();
        private ShopState playerShop;
        private ShopState comShop;
        private GomokuCom com;
        private CombatResolver combat;
        private StoneColor playerSide;
        private int selectedOffer = -1;
        private int playerWins;
        private int comWins;
        private int gameNumber;
        private float comDelayRemaining;
        private bool comTurnPending;
        private bool waitingForContinue;
        private bool matchFinished;
        private bool lastGameWasDraw;

        public StoneColor PlayerSide => playerSide;

        private void Start()
        {
            if (unitCatalog == null || unitCatalog.Units.Count == 0 || hud == null)
            {
                Debug.LogError("Gomoku MVP scene is missing its UnitCatalog or HUD reference.", this);
                enabled = false;
                return;
            }

            var random = new System.Random();
            playerShop = new ShopState(unitCatalog.Units, random);
            comShop = new ShopState(unitCatalog.Units, random);
            com = new GomokuCom(random);
            combat = new CombatResolver(combatDuration);
            combat.UnitDamaged += HandleUnitDamaged;
            combat.UnitHealed += HandleUnitHealed;
            hud.Initialize(HandleBoardClick, HandleShopSelection, HandleReroll, HandleContinue);
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

            if (game.Phase == GamePhase.Combat && !waitingForContinue)
            {
                combat.Tick(Time.deltaTime);
                hud.RefreshBoard();
                hud.SetCombatStatus(Mathf.Max(0f, combat.Duration - combat.Elapsed), combat.LastAction);

                if (combat.IsFinished)
                {
                    game.CompleteCombat();
                    PreparePlacementTurn();
                }
            }
        }

        private void StartMatch()
        {
            playerWins = 0;
            comWins = 0;
            gameNumber = 1;
            matchFinished = false;
            lastGameWasDraw = false;
            waitingForContinue = false;
            hud.HideResult();
            StartGame();
        }

        private void StartGame()
        {
            playerSide = StoneColor.White;
            game.StartNewGame(StoneColor.Black);
            playerShop.ResetForGame();
            comShop.ResetForGame();
            selectedOffer = -1;
            waitingForContinue = false;
            hud.BindGame(game, playerSide);
            hud.HideResult();
            RefreshHeader("New game");
            PreparePlacementTurn();
        }

        private void PreparePlacementTurn()
        {
            hud.RefreshBoard();
            hud.ClearCombatStatus();

            if (game.CurrentTurn == playerSide)
            {
                playerShop.BeginPlacementTurn();
                selectedOffer = -1;
                comTurnPending = false;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, true);
                RefreshHeader("Choose a unit, then place it");
            }
            else
            {
                comShop.BeginPlacementTurn();
                selectedOffer = -1;
                hud.ShowShop(playerShop.Offers, playerShop.Gold, selectedOffer, false);
                RefreshHeader("COM is thinking...");
                comDelayRemaining = comPlacementDelay;
                comTurnPending = true;
            }
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
            hud.RefreshBoard();

            if (game.IsGameOver)
            {
                FinishGame();
            }
            else if (game.Phase == GamePhase.Combat)
            {
                hud.HideShop();
                RefreshHeader("Auto combat");
                combat.Begin(game);
            }
            else
            {
                PreparePlacementTurn();
            }
        }

        private void HandleUnitDamaged(BoardUnit attacker, BoardUnit target, int damage)
        {
            hud.ShowDamage(target.X, target.Y, damage, attacker.Side == playerSide);
        }

        private void HandleUnitHealed(BoardUnit healer, BoardUnit target, int healing)
        {
            hud.ShowHeal(target.X, target.Y, healing);
        }

        private void FinishGame()
        {
            comTurnPending = false;
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
                ? (playerWins >= WinsNeeded ? "Match Victory" : "Match Defeat")
                : (playerWon ? "Game Victory" : "Game Defeat");
            string buttonLabel = lastGameWasDraw
                ? "Replay Game"
                : matchFinished ? "Restart Match" : "Next Game";

            hud.HideShop();
            RefreshHeader(title);
            hud.ShowResult(title, $"Player {playerWins} : {comWins} COM", buttonLabel);
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
                gameNumber++;
                StartGame();
            }
        }

        private void RefreshHeader(string message)
        {
            string playerColor = playerSide == StoneColor.Black ? "Black" : "White";
            hud.SetHeader(
                $"Game {gameNumber}  |  Player {playerWins} : {comWins} COM  |  You are {playerColor}",
                message);
        }
    }
}
