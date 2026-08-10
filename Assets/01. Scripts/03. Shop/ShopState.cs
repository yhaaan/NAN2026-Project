using System;
using System.Collections.Generic;

namespace NAN2026.Gomoku
{
    public sealed class ShopState
    {
        public const int SlotCount = 5;
        public const int RerollCost = 1;
        public const int StartingGold = 2;
        public const int MaxComebackDeficit = 6;
        public const int MaxPlayerAdvantagePenalty = 4;

        private static readonly int[][] GradeWeights =
        {
            new[] { 75, 23, 2, 0 },
            new[] { 65, 28, 6, 1 },
            new[] { 63, 26, 8, 3 },
            new[] { 55, 30, 10, 5 },
            new[] { 48, 33, 12, 7 },
            new[] { 48, 30, 12, 10 },
            new[] { 0, 10, 40, 50 }
        };

        private static readonly int[][] PlayerAdvantageGradeWeights =
        {
            new[] { 70, 23, 6, 1 },
            new[] { 78, 18, 4, 0 },
            new[] { 86, 12, 2, 0 },
            new[] { 94, 6, 0, 0 },
            new[] { 100, 0, 0, 0 }
        };

        private readonly IReadOnlyList<UnitDefinitionSO> unitPool;
        private readonly Random random;
        private readonly bool usePlayerAdvantagePenalty;
        private readonly List<UnitDefinitionSO> offers = new List<UnitDefinitionSO>(SlotCount);
        private readonly List<UnitDefinitionSO> gradeCandidates = new List<UnitDefinitionSO>();
        private int turnsStarted;

        public int Gold { get; private set; }
        public int ComebackDeficit { get; private set; }
        public int PlayerAdvantage { get; private set; }
        public bool IsLowestGradeOnly => PlayerAdvantage >= MaxPlayerAdvantagePenalty;
        public IReadOnlyList<UnitDefinitionSO> Offers => offers;

        public ShopState(
            IReadOnlyList<UnitDefinitionSO> unitPool,
            Random random,
            bool usePlayerAdvantagePenalty = false)
        {
            if (unitPool == null || unitPool.Count == 0)
            {
                throw new ArgumentException("The shop needs at least one unit definition.", nameof(unitPool));
            }

            this.unitPool = unitPool;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.usePlayerAdvantagePenalty = usePlayerAdvantagePenalty;
            ResetForGame();
        }

        public void ResetForGame()
        {
            Gold = StartingGold;
            turnsStarted = 0;
            ComebackDeficit = 0;
            PlayerAdvantage = 0;
            offers.Clear();
        }

        public void SetComebackDeficit(int unitDeficit)
        {
            ComebackDeficit = Math.Max(0, Math.Min(MaxComebackDeficit, unitDeficit));
            PlayerAdvantage = usePlayerAdvantagePenalty
                ? Math.Max(0, Math.Min(MaxPlayerAdvantagePenalty, -unitDeficit))
                : 0;
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
            int[] weights = PlayerAdvantage > 0
                ? PlayerAdvantageGradeWeights[PlayerAdvantage]
                : GradeWeights[ComebackDeficit];
            int totalWeight = 0;
            for (int gradeIndex = 0; gradeIndex < weights.Length; gradeIndex++)
            {
                if (HasGrade((UnitGrade)gradeIndex))
                {
                    totalWeight += weights[gradeIndex];
                }
            }

            if (totalWeight <= 0)
            {
                return RollLowestAvailableGradeOffer();
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

        private UnitDefinitionSO RollLowestAvailableGradeOffer()
        {
            UnitGrade lowestGrade = UnitGrade.Legendary;
            foreach (UnitDefinitionSO definition in unitPool)
            {
                if (definition.Grade < lowestGrade)
                {
                    lowestGrade = definition.Grade;
                }
            }

            gradeCandidates.Clear();
            foreach (UnitDefinitionSO definition in unitPool)
            {
                if (definition.Grade == lowestGrade)
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
