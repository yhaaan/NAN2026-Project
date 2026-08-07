using System;
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
            UnityEngine.Object.DestroyImmediate(unit);
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
    }
}
