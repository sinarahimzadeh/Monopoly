using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;

namespace Ropoly.Tests.EditMode
{
    public sealed class PropertyUpgradeTests
    {
        private const string DefaultBoardPath =
            "Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset";

        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean",
        };

        [Test]
        public void DefaultWorldBoard_DefaultUpgradeCosts_MatchSpecifiedCountryTiers()
        {
            BoardDefinition definition = AssetDatabase.LoadAssetAtPath<BoardDefinition>(DefaultBoardPath);
            Assert.That(definition, Is.Not.Null);
            BoardSnapshot board = definition.CreateSnapshot();

            Dictionary<string, int> cityUpgradeCosts = new Dictionary<string, int>();
            foreach (CitySnapshot city in board.Cities)
            {
                cityUpgradeCosts[city.CityId] = city.UpgradeCost;
            }

            Dictionary<string, int> expectedTierCosts = new Dictionary<string, int>
            {
                { "country-india", 50 },
                { "country-brazil", 50 },
                { "country-china", 100 },
                { "country-russia", 100 },
                { "country-italy", 150 },
                { "country-france", 150 },
                { "country-united-kingdom", 200 },
                { "country-united-states", 200 },
            };

            foreach (CountrySnapshot country in board.Countries)
            {
                Assert.That(expectedTierCosts.ContainsKey(country.CountryId), Is.True, $"Unexpected country {country.CountryId}");
                int expectedCost = expectedTierCosts[country.CountryId];
                foreach (string cityId in country.CityIds)
                {
                    Assert.That(cityUpgradeCosts[cityId], Is.EqualTo(expectedCost),
                        $"{cityId} in {country.CountryId} expected upgrade cost {expectedCost}");
                }
            }
        }

        [Test]
        public void UpgradeProperty_DisallowedWithoutFullCountrySet()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 3, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board);

            // Player 1 buys only city 1 (tile 1)
            BuyPropertyAt(session, tileIndex: 1, roll: 1);

            // Attempt to upgrade tile 1 without owning city 2 and city 3
            PropertyUpgradeResult result = session.TryUpgradeProperty(1);
            Assert.That(result, Is.EqualTo(PropertyUpgradeResult.CountrySetIncomplete));
        }

        [Test]
        public void UpgradeProperty_DisallowedWhenAnySisterPropertyIsMortgaged()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 3, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board);

            // Player 1 buys all 3 cities
            BuyAllCitiesForPlayer1(session);

            // Force mortgage city 3 during debt resolution or directly
            Assert.That(session.TryGetPropertyAt(3, out PropertyState city3), Is.True);
            city3.TryMortgage(0);
            Assert.That(city3.IsMortgaged, Is.True);

            // Attempt to upgrade city 1
            PropertyUpgradeResult result = session.TryUpgradeProperty(1);
            Assert.That(result, Is.EqualTo(PropertyUpgradeResult.HasMortgagedPropertyInSet));
        }

        [Test]
        public void UpgradeProperty_DeductsCost_IncreasesLevel_IncreasesRent()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 3, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 1000);

            BuyAllCitiesForPlayer1(session);
            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            int cashBefore = session.State.Players[0].Cash;

            // Upgrade city 1 from level 0 to 1
            PropertyUpgradeResult result = session.TryUpgradeProperty(1);
            Assert.That(result, Is.EqualTo(PropertyUpgradeResult.Success));
            Assert.That(city1.UpgradeLevel, Is.EqualTo(1));
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore - 50));

            // Level 1 rent is 35 (from rentByUpgradeLevel: 10, 35, 100, 250, 400, 600)
            Assert.That(city1.CalculateRent(3, 0), Is.EqualTo(35));
        }

        [Test]
        public void UnimprovedFullSet_DoublesBaseRent_WhenAllUnmortgaged()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 3, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 1000);

            // Buy only city 1: rent is base 10
            BuyPropertyAt(session, tileIndex: 1, roll: 1);
            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            Assert.That(session.IsCountrySetFullyOwned("test-country", 0), Is.False);

            // Advance to player 2, move player 2 to tile 1
            session.TryAdvanceTurn();
            session.TryRollDice(new FixedDiceRollSource(1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();

            // Opponent pays 10
            Assert.That(session.State.Turn.RentPayment.Amount, Is.EqualTo(10));
            session.TryAdvanceTurn();

            // Now player 1 buys the rest of the set (city 2 and city 3)
            session.TryRollDice(new FixedDiceRollSource(1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryPurchaseCurrentProperty(); // buys city 2 (tile 2)
            session.TryAdvanceTurn();
            session.TryRollDice(new FixedDiceRollSource(4), out _); // player 2 passes
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryAdvanceTurn();

            session.TryRollDice(new FixedDiceRollSource(1), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryPurchaseCurrentProperty(); // buys city 3 (tile 3)
            session.TryAdvanceTurn();

            Assert.That(session.IsCountrySetFullyOwned("test-country", 0), Is.True);

            // Player 2 lands on tile 1 (city 1) again
            session.TryRollDice(new FixedDiceRollSource(2), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();

            // Unimproved monopoly doubles base rent (10 * 2 = 20)
            Assert.That(session.State.Turn.RentPayment.Amount, Is.EqualTo(20));
        }

        [Test]
        public void EvenBuildingRule_Enforced_DuringUpgrades()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 3, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 2000);
            BuyAllCitiesForPlayer1(session);

            // Upgrade city 1 to level 1
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.Success));

            // Cannot upgrade city 1 to level 2 while city 2 and 3 are still at 0
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.EvenBuildRuleViolation));

            // Can upgrade city 2 and city 3 to level 1
            Assert.That(session.TryUpgradeProperty(2), Is.EqualTo(PropertyUpgradeResult.Success));
            Assert.That(session.TryUpgradeProperty(3), Is.EqualTo(PropertyUpgradeResult.Success));

            // Now city 1 can be upgraded to level 2
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.Success));

            // City 1 cannot be upgraded to level 3 until city 2 and 3 reach level 2
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.EvenBuildRuleViolation));
        }

        [Test]
        public void MaximumUpgradeLevel_CappedAtFive()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 2, upgradeCost: 50);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 5000);
            BuyAllCitiesForPlayer1(session, cityCount: 2);

            // Bring both to level 5
            for (int level = 1; level <= 5; level++)
            {
                Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.Success));
                Assert.That(session.TryUpgradeProperty(2), Is.EqualTo(PropertyUpgradeResult.Success));
            }

            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            Assert.That(city1.UpgradeLevel, Is.EqualTo(5));

            // Attempt to upgrade beyond level 5
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.MaximumLevelReached));
        }

        [Test]
        public void InsufficientCash_BlocksUpgrade()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 2, upgradeCost: 500);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 250);
            BuyAllCitiesForPlayer1(session, cityCount: 2, freePurchase: true);

            PropertyUpgradeResult result = session.TryUpgradeProperty(1);
            Assert.That(result, Is.EqualTo(PropertyUpgradeResult.InsufficientCash));
        }

        [Test]
        public void EvenDowngradeRule_And_RefundCalculation()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 2, upgradeCost: 100);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 2000);
            BuyAllCitiesForPlayer1(session, cityCount: 2);

            // Build: 1 at level 2, 2 at level 1
            session.TryUpgradeProperty(1);
            session.TryUpgradeProperty(2);
            session.TryUpgradeProperty(1);

            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            Assert.That(session.TryGetPropertyAt(2, out PropertyState city2), Is.True);
            Assert.That(city1.UpgradeLevel, Is.EqualTo(2));
            Assert.That(city2.UpgradeLevel, Is.EqualTo(1));

            // Cannot downgrade city 2 (level 1) while city 1 is level 2
            PropertyDowngradeResult failResult = session.TryDowngradeProperty(2);
            Assert.That(failResult, Is.EqualTo(PropertyDowngradeResult.EvenDowngradeRuleViolation));

            int cashBefore = session.State.Players[0].Cash;

            // Downgrade city 1 (level 2 -> 1) gives 50% refund (100 / 2 = 50)
            PropertyDowngradeResult okResult = session.TryDowngradeProperty(1, out int refund);
            Assert.That(okResult, Is.EqualTo(PropertyDowngradeResult.Success));
            Assert.That(refund, Is.EqualTo(50));
            Assert.That(city1.UpgradeLevel, Is.EqualTo(1));
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore + 50));

            // Downgrading when 0 returns NoUpgradesToSell
            session.TryDowngradeProperty(1);
            session.TryDowngradeProperty(2);
            Assert.That(city1.UpgradeLevel, Is.Zero);
            Assert.That(session.TryDowngradeProperty(1), Is.EqualTo(PropertyDowngradeResult.NoUpgradesToSell));
        }

        [Test]
        public void MortgageAndBankSale_BlockedWhileBuildingsExistInCountrySet()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 2, upgradeCost: 100);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 1000);
            BuyAllCitiesForPlayer1(session, cityCount: 2);

            // Upgrade city 1 to level 1
            Assert.That(session.TryUpgradeProperty(1), Is.EqualTo(PropertyUpgradeResult.Success));

            // Enter debt resolution for player 1 by creating a debt situation
            // First verify HasUpgradesInCountrySet
            Assert.That(session.HasUpgradesInCountrySet("test-country"), Is.True);

            // Attempting to mortgage or sell city 1 or city 2 while upgrades exist fails
            // Force debt resolution turn state to test liquidation checks:
            RentPayment debt = new RentPayment(tileIndex: 5, "fake-tile", "Fake", payerPlayerIndex: 0, ownerPlayerIndex: 1, amount: 500);
            session.State.Turn.RecordRentPayment(debt);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.DebtResolution));

            Assert.That(session.TryMortgageProperty(1), Is.EqualTo(AssetLiquidationResult.HasUpgradedPropertiesInSet));
            Assert.That(session.TryMortgageProperty(2), Is.EqualTo(AssetLiquidationResult.HasUpgradedPropertiesInSet));
            Assert.That(session.TrySellPropertyToBank(1), Is.EqualTo(AssetLiquidationResult.HasUpgradedPropertiesInSet));
            Assert.That(session.TrySellPropertyToBank(2), Is.EqualTo(AssetLiquidationResult.HasUpgradedPropertiesInSet));

            // Sell the building back in DebtResolution
            PropertyDowngradeResult downgrade = session.TryDowngradeProperty(1, out int refund);
            Assert.That(downgrade, Is.EqualTo(PropertyDowngradeResult.Success));
            Assert.That(refund, Is.EqualTo(50));
            Assert.That(session.HasUpgradesInCountrySet("test-country"), Is.False);

            // Now mortgage is permitted!
            Assert.That(session.TryMortgageProperty(1), Is.EqualTo(AssetLiquidationResult.Success));
        }

        [Test]
        public void Bankruptcy_ResetsUpgradesToZero()
        {
            BoardSnapshot board = CreateCountrySetBoard("test-country", cityCount: 2, upgradeCost: 100);
            LocalMatchSession session = CreateStartedSession(board, startingCash: 1000);
            BuyAllCitiesForPlayer1(session, cityCount: 2);
            session.TryUpgradeProperty(1);

            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            Assert.That(city1.UpgradeLevel, Is.EqualTo(1));

            // Enter debt resolution and declare bankruptcy
            RentPayment debt = new RentPayment(tileIndex: 5, "fake-tile", "Fake", payerPlayerIndex: 0, ownerPlayerIndex: 1, amount: 9999);
            session.State.Turn.RecordRentPayment(debt);
            session.TryDeclareCurrentPlayerBankruptcy();

            Assert.That(city1.IsOwned, Is.False);
            Assert.That(city1.UpgradeLevel, Is.Zero);
        }

        private static LocalMatchSession CreateStartedSession(BoardSnapshot board, int startingCash = 1500)
        {
            LocalMatchSession session = new LocalMatchSession(
                playerCount: 2,
                startingCash: startingCash,
                availableCreatureIds: CreatureIds,
                board: board,
                passStartCash: 0,
                mortgageEnabled: true);

            session.TrySelectCreature(0, "pip");
            session.TrySelectCreature(1, "bubu");
            Assert.That(session.TryStartMatch(), Is.True);
            return session;
        }

        private static void BuyPropertyAt(LocalMatchSession session, int tileIndex, int roll)
        {
            session.TryRollDice(new FixedDiceRollSource(roll), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryPurchaseCurrentProperty();
        }

        private static void BuyAllCitiesForPlayer1(
            LocalMatchSession session,
            int cityCount = 3,
            bool freePurchase = false)
        {
            for (int i = 1; i <= cityCount; i++)
            {
                Assert.That(session.TryGetPropertyAt(i, out PropertyState city), Is.True);
                city.TryAssignOwner(0);
            }
        }

        private static BoardSnapshot CreateCountrySetBoard(string countryId, int cityCount, int upgradeCost)
        {
            List<string> cityIds = new List<string>();
            List<CitySnapshot> cities = new List<CitySnapshot>();
            List<BoardTileSnapshot> tiles = new List<BoardTileSnapshot>
            {
                new BoardTileSnapshot(0, BoardTileKind.Special, "start"),
            };

            int[] rents = { 10, 35, 100, 250, 400, 600 };
            for (int i = 1; i <= cityCount; i++)
            {
                string cityId = $"{countryId}-city-{i}";
                cityIds.Add(cityId);
                cities.Add(new CitySnapshot(cityId, $"City {i}", 100, 50, upgradeCost, rents));
                tiles.Add(new BoardTileSnapshot(i, BoardTileKind.City, cityId));
            }

            // Fill up to 6 tiles with special space
            for (int i = cityCount + 1; i <= 6; i++)
            {
                tiles.Add(new BoardTileSnapshot(i, BoardTileKind.Special, "special"));
            }

            CountrySnapshot country = new CountrySnapshot(
                countryId,
                "Test Country",
                cityCount,
                10000,
                2024,
                "Test Source",
                "#FF0000",
                cityIds.ToArray());

            return new BoardSnapshot(
                schemaVersion: 1,
                boardId: "upgrade-test-board",
                displayName: "Upgrade Test Board",
                countries: new[] { country },
                cities: cities.ToArray(),
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("start", "Start", SpecialSpaceKind.Start, string.Empty, 0),
                    new SpecialSpaceSnapshot("special", "Special", SpecialSpaceKind.Vacation, string.Empty, 0),
                },
                tiles: tiles.ToArray());
        }

        private sealed class FixedDiceRollSource : IDiceRollSource
        {
            private readonly Queue<int> _values;

            public FixedDiceRollSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int NextDieValue()
            {
                return _values.Count > 0 ? _values.Dequeue() : 1;
            }
        }
    }
}
