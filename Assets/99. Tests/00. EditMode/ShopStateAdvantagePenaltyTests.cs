using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class ShopStateAdvantagePenaltyTests
    {
        private const int SampleTurns = 200;

        [TestCase(-4)]
        [TestCase(-7)]
        public void PlayerAdvantagePenalty_FourOrMoreLeadOffersOnlyCommon(int unitDeficit)
        {
            UnitDefinitionSO[] pool = CreateFullGradePool();

            try
            {
                for (int seed = 0; seed < 20; seed++)
                {
                    var shop = new ShopState(
                        pool,
                        new System.Random(seed),
                        usePlayerAdvantagePenalty: true);
                    shop.SetComebackDeficit(unitDeficit);
                    shop.BeginPlacementTurn();

                    Assert.That(shop.PlayerAdvantage, Is.EqualTo(4));
                    Assert.That(shop.IsLowestGradeOnly, Is.True);
                    foreach (UnitDefinitionSO offer in shop.Offers)
                    {
                        Assert.That(offer.Grade, Is.EqualTo(UnitGrade.Common));
                    }
                }
            }
            finally
            {
                DestroyPool(pool);
            }
        }

        [Test]
        public void PlayerAdvantagePenalty_ProgressivelyReducesHighGradeOffers()
        {
            UnitDefinitionSO[] pool = CreateFullGradePool();

            try
            {
                int normal = CountHighGradeOffers(pool, 0, 11);
                int oneAhead = CountHighGradeOffers(pool, -1, 11);
                int twoAhead = CountHighGradeOffers(pool, -2, 11);
                int threeAhead = CountHighGradeOffers(pool, -3, 11);

                Assert.That(oneAhead, Is.LessThan(normal));
                Assert.That(twoAhead, Is.LessThan(oneAhead));
                Assert.That(threeAhead, Is.Zero);
            }
            finally
            {
                DestroyPool(pool);
            }
        }

        [Test]
        public void ComShop_DoesNotUsePlayerAdvantagePenalty()
        {
            UnitDefinitionSO[] pool = CreateFullGradePool();

            try
            {
                var shop = new ShopState(pool, new System.Random(4));
                shop.SetComebackDeficit(-8);
                shop.BeginPlacementTurn();

                Assert.That(shop.ComebackDeficit, Is.Zero);
                Assert.That(shop.PlayerAdvantage, Is.Zero);
                Assert.That(shop.IsLowestGradeOnly, Is.False);
            }
            finally
            {
                DestroyPool(pool);
            }
        }

        [Test]
        public void LowestGradeLock_UsesLowestGradeAvailableInPool()
        {
            UnitDefinitionSO rare = TestUnitFactory.Create("Rare", grade: UnitGrade.Rare);
            UnitDefinitionSO epic = TestUnitFactory.Create("Epic", grade: UnitGrade.Epic);

            try
            {
                var shop = new ShopState(
                    new[] { rare, epic },
                    new System.Random(3),
                    usePlayerAdvantagePenalty: true);
                shop.SetComebackDeficit(-4);
                shop.BeginPlacementTurn();

                foreach (UnitDefinitionSO offer in shop.Offers)
                {
                    Assert.That(offer.Grade, Is.EqualTo(UnitGrade.Rare));
                }
            }
            finally
            {
                Object.DestroyImmediate(rare);
                Object.DestroyImmediate(epic);
            }
        }

        private static int CountHighGradeOffers(
            UnitDefinitionSO[] pool,
            int unitDeficit,
            int seed)
        {
            var shop = new ShopState(
                pool,
                new System.Random(seed),
                usePlayerAdvantagePenalty: true);
            shop.SetComebackDeficit(unitDeficit);

            int count = 0;
            for (int turn = 0; turn < SampleTurns; turn++)
            {
                shop.BeginPlacementTurn();
                foreach (UnitDefinitionSO offer in shop.Offers)
                {
                    if (offer.Grade >= UnitGrade.Epic)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static UnitDefinitionSO[] CreateFullGradePool()
        {
            return new[]
            {
                TestUnitFactory.Create("Common", grade: UnitGrade.Common),
                TestUnitFactory.Create("Rare", grade: UnitGrade.Rare),
                TestUnitFactory.Create("Epic", grade: UnitGrade.Epic),
                TestUnitFactory.Create("Legendary", grade: UnitGrade.Legendary)
            };
        }

        private static void DestroyPool(UnitDefinitionSO[] pool)
        {
            foreach (UnitDefinitionSO definition in pool)
            {
                Object.DestroyImmediate(definition);
            }
        }
    }
}
