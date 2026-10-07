using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;

namespace Ropoly.Tests.EditMode
{
    public sealed class DefaultWorldBoardTests
    {
        private const string DefaultBoardPath =
            "Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset";

        [Test]
        public void DefaultWorldBoard_ExistsAndIsValid()
        {
            BoardDefinition definition = AssetDatabase.LoadAssetAtPath<BoardDefinition>(DefaultBoardPath);

            Assert.That(definition, Is.Not.Null, "Default world board asset is missing.");
            BoardValidationResult validation = definition.Validate();
            Assert.That(validation.IsValid, Is.True, JoinErrors(validation));
        }

        [Test]
        public void DefaultWorldBoard_HasExpectedFortySpaceComposition()
        {
            BoardSnapshot board = LoadSnapshot();
            Dictionary<BoardTileKind, int> tileCounts = new Dictionary<BoardTileKind, int>();
            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                tileCounts.TryGetValue(tile.Kind, out int count);
                tileCounts[tile.Kind] = count + 1;
            }

            Assert.That(board.Tiles.Count, Is.EqualTo(40));
            Assert.That(tileCounts[BoardTileKind.City], Is.EqualTo(22));
            Assert.That(tileCounts[BoardTileKind.Airport], Is.EqualTo(4));
            Assert.That(tileCounts[BoardTileKind.Utility], Is.EqualTo(2));
            Assert.That(tileCounts[BoardTileKind.Special], Is.EqualTo(12));
            Assert.That(board.Countries.Count, Is.EqualTo(8));
            Assert.That(board.Cities.Count, Is.EqualTo(22));
            Assert.That(board.Airports.Count, Is.EqualTo(4));
            Assert.That(board.Utilities.Count, Is.EqualTo(2));
            Assert.That(board.SpecialSpaces.Count, Is.EqualTo(8));
        }

        [Test]
        public void DefaultWorldBoard_CountriesFollowLockedGdpPerCapitaOrder()
        {
            BoardSnapshot board = LoadSnapshot();
            string[] expectedIds =
            {
                "country-india",
                "country-brazil",
                "country-china",
                "country-russia",
                "country-italy",
                "country-france",
                "country-united-kingdom",
                "country-united-states",
            };
            int[] expectedGdpPerCapita = { 2_592, 10_311, 13_293, 14_962, 40_430, 46_103, 53_341, 86_170 };

            Assert.That(board.Countries.Count, Is.EqualTo(expectedIds.Length));
            for (int index = 0; index < expectedIds.Length; index++)
            {
                CountrySnapshot country = board.Countries[index];
                Assert.That(country.CountryId, Is.EqualTo(expectedIds[index]));
                Assert.That(country.GdpPerCapitaUsd, Is.EqualTo(expectedGdpPerCapita[index]));
                Assert.That(country.EconomicDataYear, Is.EqualTo(2024));
                Assert.That(country.EconomicDataSource, Does.Contain("World Bank"));
                if (index > 0)
                {
                    Assert.That(
                        country.GdpPerCapitaUsd,
                        Is.GreaterThan(board.Countries[index - 1].GdpPerCapitaUsd));
                }
            }
        }

        [Test]
        public void DefaultWorldBoard_UsesTwoCitiesForItalyAndFranceAndThreeForOthers()
        {
            BoardSnapshot board = LoadSnapshot();
            foreach (CountrySnapshot country in board.Countries)
            {
                int expectedCount =
                    country.CountryId == "country-italy" || country.CountryId == "country-france" ? 2 : 3;
                Assert.That(country.ExpectedCityCount, Is.EqualTo(expectedCount), country.CountryId);
                Assert.That(country.CityIds.Count, Is.EqualTo(expectedCount), country.CountryId);
            }
        }

        [Test]
        public void DefaultWorldBoard_PricesIncreaseAcrossCountryTiers()
        {
            BoardSnapshot board = LoadSnapshot();
            Dictionary<string, int> cityPrices = new Dictionary<string, int>();
            foreach (CitySnapshot city in board.Cities)
            {
                cityPrices.Add(city.CityId, city.PurchasePrice);
            }

            int previousCountryMaximum = 0;
            foreach (CountrySnapshot country in board.Countries)
            {
                int countryMinimum = int.MaxValue;
                int countryMaximum = 0;
                foreach (string cityId in country.CityIds)
                {
                    int price = cityPrices[cityId];
                    countryMinimum = System.Math.Min(countryMinimum, price);
                    countryMaximum = System.Math.Max(countryMaximum, price);
                }

                Assert.That(countryMinimum, Is.GreaterThan(previousCountryMaximum), country.CountryId);
                previousCountryMaximum = countryMaximum;
            }
        }

        [Test]
        public void DefaultWorldBoard_HasExpectedCornersAndEventDistribution()
        {
            BoardSnapshot board = LoadSnapshot();
            Assert.That(board.Tiles[0].ContentId, Is.EqualTo("space-start"));
            Assert.That(board.Tiles[10].ContentId, Is.EqualTo("space-detention"));
            Assert.That(board.Tiles[20].ContentId, Is.EqualTo("space-vacation"));
            Assert.That(board.Tiles[30].ContentId, Is.EqualTo("space-go-to-detention"));

            int worldEvents = 0;
            int localOpportunities = 0;
            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                if (tile.ContentId == "space-world-event")
                {
                    worldEvents++;
                }
                else if (tile.ContentId == "space-local-opportunity")
                {
                    localOpportunities++;
                }
            }

            Assert.That(worldEvents, Is.EqualTo(3));
            Assert.That(localOpportunities, Is.EqualTo(3));
        }

        private static BoardSnapshot LoadSnapshot()
        {
            BoardDefinition definition = AssetDatabase.LoadAssetAtPath<BoardDefinition>(DefaultBoardPath);
            Assert.That(definition, Is.Not.Null, "Default world board asset is missing.");
            return definition.CreateSnapshot();
        }

        private static string JoinErrors(BoardValidationResult result)
        {
            return string.Join("\n", result.Errors);
        }
    }
}
