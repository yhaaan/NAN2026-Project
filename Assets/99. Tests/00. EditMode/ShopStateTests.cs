using NUnit.Framework;
using UnityEngine;

namespace NAN2026.Gomoku.Tests
{
    public sealed class ShopStateTests
    {
        private UnitDefinitionSO unit;
        private ShopState shop;

        [SetUp]
        public void SetUp()
        {
            unit = TestUnitFactory.Create();
            shop = new ShopState(new[] { unit }, new System.Random(1));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(unit);
        }

        [Test]
        public void FirstTurn_RefreshesFiveOffersWithoutIncome()
        {
            shop.BeginPlacementTurn();
            Assert.That(shop.Gold, Is.EqualTo(ShopState.StartingGold));
            Assert.That(shop.Offers, Has.Count.EqualTo(ShopState.SlotCount));
        }

        [Test]
        public void LaterTurn_AddsIncomeAndRerollSpendsGold()
        {
            shop.BeginPlacementTurn();
            shop.BeginPlacementTurn();
            Assert.That(shop.Gold, Is.EqualTo(3));
            Assert.That(shop.TryReroll(), Is.True);
            Assert.That(shop.Gold, Is.EqualTo(2));
        }

        [Test]
        public void ComebackDeficit_IncreasesEpicAndLegendaryOfferRate()
        {
            UnitDefinitionSO common = TestUnitFactory.Create("Common", grade: UnitGrade.Common);
            UnitDefinitionSO rare = TestUnitFactory.Create("Rare", grade: UnitGrade.Rare);
            UnitDefinitionSO epic = TestUnitFactory.Create("Epic", grade: UnitGrade.Epic);
            UnitDefinitionSO legendary = TestUnitFactory.Create("Legendary", grade: UnitGrade.Legendary);
            UnitDefinitionSO[] pool = { common, rare, epic, legendary };

            try
            {
                int normalHighGrade = CountHighGradeOffers(pool, 0, 7);
                int comebackHighGrade = CountHighGradeOffers(pool, 3, 7);
                Assert.That(comebackHighGrade, Is.GreaterThan(normalHighGrade + 250));
            }
            finally
            {
                Object.DestroyImmediate(common);
                Object.DestroyImmediate(rare);
                Object.DestroyImmediate(epic);
                Object.DestroyImmediate(legendary);
            }
        }

        private static int CountHighGradeOffers(
            UnitDefinitionSO[] pool,
            int deficit,
            int seed)
        {
            var random = new System.Random(seed);
            int count = 0;
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                var sampledShop = new ShopState(pool, random);
                sampledShop.SetComebackDeficit(deficit);
                sampledShop.BeginPlacementTurn();
                foreach (UnitDefinitionSO offer in sampledShop.Offers)
                {
                    if (offer.Grade >= UnitGrade.Epic) count++;
                }
            }
            return count;
        }
    }
}