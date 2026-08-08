using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    public sealed class ShopState
    {
        public const int SlotCount = 5;
        public const int RerollCost = 1;
        public const int StartingGold = 2;
        public const int MaxComebackDeficit = 3;

        private static readonly int[][] GradeWeights =
        {
            new[] { 70, 23, 6, 1 },
            new[] { 65, 26, 8, 1 },
            new[] { 60, 28, 10, 2 },
            new[] { 55, 30, 12, 3 }
        };

        private readonly IReadOnlyList<UnitDefinitionSO> unitPool;
        private readonly Random random;
        private readonly List<UnitDefinitionSO> offers = new List<UnitDefinitionSO>(SlotCount);
        private readonly List<UnitDefinitionSO> gradeCandidates = new List<UnitDefinitionSO>();
        private int turnsStarted;

        public int Gold { get; private set; }
        public int ComebackDeficit { get; private set; }
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
            ComebackDeficit = 0;
            offers.Clear();
        }

        public void SetComebackDeficit(int unitDeficit)
        {
            ComebackDeficit = Math.Max(0, Math.Min(MaxComebackDeficit, unitDeficit));
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
                offers.Add(RollOffer());
            }
        }

        private UnitDefinitionSO RollOffer()
        {
            int[] weights = GradeWeights[ComebackDeficit];
            int totalWeight = 0;
            for (int gradeIndex = 0; gradeIndex < weights.Length; gradeIndex++)
            {
                if (HasGrade((UnitGrade)gradeIndex))
                {
                    totalWeight += weights[gradeIndex];
                }
            }

            int roll = random.Next(totalWeight);
            UnitGrade selectedGrade = UnitGrade.Common;
            for (int gradeIndex = 0; gradeIndex < weights.Length; gradeIndex++)
            {
                UnitGrade grade = (UnitGrade)gradeIndex;
                if (!HasGrade(grade))
                {
                    continue;
                }

                if (roll < weights[gradeIndex])
                {
                    selectedGrade = grade;
                    break;
                }

                roll -= weights[gradeIndex];
            }

            gradeCandidates.Clear();
            foreach (UnitDefinitionSO definition in unitPool)
            {
                if (definition.Grade == selectedGrade)
                {
                    gradeCandidates.Add(definition);
                }
            }

            return gradeCandidates[random.Next(gradeCandidates.Count)];
        }

        private bool HasGrade(UnitGrade grade)
        {
            foreach (UnitDefinitionSO definition in unitPool)
            {
                if (definition.Grade == grade)
                {
                    return true;
                }
            }

            return false;
        }
    }
}