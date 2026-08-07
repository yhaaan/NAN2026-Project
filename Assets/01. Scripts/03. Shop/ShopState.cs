using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    public sealed class ShopState
    {
        public const int SlotCount = 5;
        public const int RerollCost = 1;
        public const int StartingGold = 2;

        private readonly IReadOnlyList<UnitDefinitionSO> unitPool;
        private readonly Random random;
        private readonly List<UnitDefinitionSO> offers = new List<UnitDefinitionSO>(SlotCount);
        private int turnsStarted;

        public int Gold { get; private set; }
        public IReadOnlyList<UnitDefinitionSO> Offers => offers;

        public ShopState(IReadOnlyList<UnitDefinitionSO> unitPool, Random random)
        {
            if (unitPool == null || unitPool.Count == 0)
            {
                throw new ArgumentException("The shop needs at least one unit definition.", nameof(unitPool));
            }

            this.unitPool = unitPool;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            ResetForGame();
        }

        public void ResetForGame()
        {
            Gold = StartingGold;
            turnsStarted = 0;
            offers.Clear();
        }

        public void BeginPlacementTurn()
        {
            if (turnsStarted > 0)
            {
                Gold++;
            }

            turnsStarted++;
            RefreshOffers();
        }

        public bool TryReroll()
        {
            if (Gold < RerollCost)
            {
                return false;
            }

            Gold -= RerollCost;
            RefreshOffers();
            return true;
        }

        private void RefreshOffers()
        {
            offers.Clear();
            for (int index = 0; index < SlotCount; index++)
            {
                offers.Add(unitPool[random.Next(unitPool.Count)]);
            }
        }
    }
}
