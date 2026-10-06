using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class BoardContentTests
    {
        private readonly List<Object> _createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void ValidDefinition_CreatesOrderedProviderNeutralSnapshot()
        {
            CityDefinition firstCity = CreateTile<CityDefinition>("first-city", "First City");
            CityDefinition secondCity = CreateTile<CityDefinition>("second-city", "Second City");
            AirportDefinition airport = CreateTile<AirportDefinition>("central-airport", "Central Airport");
            UtilityDefinition utility = CreateTile<UtilityDefinition>("water-service", "Water Service");
            SpecialSpaceDefinition start = CreateTile<SpecialSpaceDefinition>("start", "Start");
            CountryDefinition country = CreateCountry("sample-country", "Sample Country", firstCity, secondCity);
            BoardDefinition board = CreateBoard(
                "test-board",
                "Test Board",
                new[] { country },
                new BoardTileDefinition[] { start, firstCity, airport, utility, secondCity });

            BoardSnapshot snapshot = board.CreateSnapshot();
            BoardValidationResult validation = board.Validate();

            Assert.That(validation.IsValid, Is.True, JoinErrors(validation));
            Assert.That(snapshot.SchemaVersion, Is.EqualTo(BoardContentLimits.CurrentSchemaVersion));
            Assert.That(snapshot.BoardId, Is.EqualTo("test-board"));
            Assert.That(snapshot.Countries.Count, Is.EqualTo(1));
            Assert.That(snapshot.Countries[0].CityIds, Is.EqualTo(new[] { "first-city", "second-city" }));
            Assert.That(snapshot.Countries[0].SetColorHex, Does.StartWith("#"));
            Assert.That(snapshot.Cities.Count, Is.EqualTo(2));
            Assert.That(snapshot.Airports.Count, Is.EqualTo(1));
            Assert.That(snapshot.Utilities.Count, Is.EqualTo(1));
            Assert.That(snapshot.SpecialSpaces.Count, Is.EqualTo(1));
            Assert.That(snapshot.Tiles.Count, Is.EqualTo(5));
            Assert.That(snapshot.Tiles[0].Kind, Is.EqualTo(BoardTileKind.Special));
            Assert.That(snapshot.Tiles[1].ContentId, Is.EqualTo("first-city"));
            Assert.That(snapshot.Tiles[2].Kind, Is.EqualTo(BoardTileKind.Airport));
            Assert.That(snapshot.Tiles[3].Kind, Is.EqualTo(BoardTileKind.Utility));
            Assert.That(snapshot.Tiles[4].Index, Is.EqualTo(4));
        }

        [Test]
        public void Validator_ReportsMalformedEconomyAndSpecialSettings()
        {
            BoardSnapshot snapshot = new BoardSnapshot(
                BoardContentLimits.CurrentSchemaVersion,
                "invalid-economy-board",
                "Invalid Economy Board",
                countries: new CountrySnapshot[0],
                cities: new[]
                {
                    new CitySnapshot("bad-city", "Bad City", 0, 10, 50, new[] { 5, 2 }),
                },
                airports: new[]
                {
                    new AirportSnapshot("bad-airport", "Bad Airport", 100, 200, new[] { 25 }),
                },
                utilities: new[]
                {
                    new UtilitySnapshot("bad-utility", "Bad Utility", 100, 50, new[] { 0, 10 }),
                },
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("bad-event", "Bad Event", SpecialSpaceKind.Event, "Invalid ID", 0),
                },
                tiles: new[]
                {
                    new BoardTileSnapshot(0, BoardTileKind.Special, "bad-event"),
                });

            BoardValidationResult result = BoardSnapshotValidator.Validate(snapshot);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Contains(BoardValidationErrorCode.InvalidCity), Is.True);
            Assert.That(result.Contains(BoardValidationErrorCode.InvalidAirport), Is.True);
            Assert.That(result.Contains(BoardValidationErrorCode.InvalidUtility), Is.True);
            Assert.That(result.Contains(BoardValidationErrorCode.InvalidSpecialSpace), Is.True);
        }

        [Test]
        public void Validator_ReportsBrokenCountryMembershipAndDuplicatePlayableTiles()
        {
            CitySnapshot city = new CitySnapshot(
                "shared-city",
                "Shared City",
                100,
                50,
                50,
                new[] { 5, 10, 20, 40, 80, 160 });
            CountrySnapshot firstCountry = new CountrySnapshot(
                "first-country",
                "First Country",
                2,
                50_000,
                2025,
                "Test Source",
                "#112233FF",
                new[] { "shared-city" });
            CountrySnapshot secondCountry = new CountrySnapshot(
                "second-country",
                "Second Country",
                2,
                40_000,
                2025,
                "Test Source",
                "#445566FF",
                new[] { "shared-city" });
            BoardSnapshot snapshot = new BoardSnapshot(
                BoardContentLimits.CurrentSchemaVersion,
                "broken-board",
                "Broken Board",
                new[] { firstCountry, secondCountry },
                new[] { city },
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new SpecialSpaceSnapshot[0],
                tiles: new[]
                {
                    new BoardTileSnapshot(0, BoardTileKind.City, "shared-city"),
                    new BoardTileSnapshot(1, BoardTileKind.City, "shared-city"),
                });

            BoardValidationResult result = BoardSnapshotValidator.Validate(snapshot);

            Assert.That(result.Contains(BoardValidationErrorCode.CountryCityCountMismatch), Is.True);
            Assert.That(result.Contains(BoardValidationErrorCode.CityAssignedToMultipleCountries), Is.True);
            Assert.That(result.Contains(BoardValidationErrorCode.DuplicatePlayableTile), Is.True);
        }

        [Test]
        public void Snapshots_CopyMutableInputArrays()
        {
            int[] rents = { 2, 10, 30, 90, 160, 250 };
            CitySnapshot city = new CitySnapshot("safe-city", "Safe City", 60, 30, 50, rents);
            rents[0] = 999;

            BoardTileSnapshot[] tiles =
            {
                new BoardTileSnapshot(0, BoardTileKind.City, "safe-city"),
            };
            BoardSnapshot board = new BoardSnapshot(
                BoardContentLimits.CurrentSchemaVersion,
                "safe-board",
                "Safe Board",
                countries: new CountrySnapshot[0],
                cities: new[] { city },
                airports: new AirportSnapshot[0],
                utilities: new UtilitySnapshot[0],
                specialSpaces: new SpecialSpaceSnapshot[0],
                tiles: tiles);
            tiles[0] = null;

            Assert.That(city.RentByUpgradeLevel[0], Is.EqualTo(2));
            Assert.That(board.Tiles[0], Is.Not.Null);
        }

        [Test]
        public void Validator_RejectsMissingSnapshot()
        {
            BoardValidationResult result = BoardSnapshotValidator.Validate(null);

            Assert.That(result.Contains(BoardValidationErrorCode.MissingSnapshot), Is.True);
        }

        private T CreateTile<T>(string id, string displayName)
            where T : BoardTileDefinition
        {
            T definition = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(definition);
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_contentId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private CountryDefinition CreateCountry(
            string id,
            string displayName,
            params CityDefinition[] cities)
        {
            CountryDefinition country = ScriptableObject.CreateInstance<CountryDefinition>();
            _createdObjects.Add(country);
            SerializedObject serialized = new SerializedObject(country);
            serialized.FindProperty("_countryId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_expectedCityCount").intValue = cities.Length;
            serialized.FindProperty("_gdpPerCapitaUsd").intValue = 50_000;
            serialized.FindProperty("_economicDataYear").intValue = 2025;
            serialized.FindProperty("_economicDataSource").stringValue = "Test Source";
            SetObjectReferences(serialized.FindProperty("_cities"), cities);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return country;
        }

        private BoardDefinition CreateBoard(
            string id,
            string displayName,
            CountryDefinition[] countries,
            BoardTileDefinition[] spaces)
        {
            BoardDefinition board = ScriptableObject.CreateInstance<BoardDefinition>();
            _createdObjects.Add(board);
            SerializedObject serialized = new SerializedObject(board);
            serialized.FindProperty("_boardId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            SetObjectReferences(serialized.FindProperty("_countries"), countries);
            SetObjectReferences(serialized.FindProperty("_spaces"), spaces);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return board;
        }

        private static void SetObjectReferences<T>(SerializedProperty property, T[] values)
            where T : Object
        {
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static string JoinErrors(BoardValidationResult result)
        {
            return string.Join("\n", result.Errors);
        }
    }
}
