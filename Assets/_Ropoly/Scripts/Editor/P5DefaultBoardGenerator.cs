using System;
using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Editor
{
    public static class P5DefaultBoardGenerator
    {
        public const string DefaultBoardPath =
            "Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset";

        private const string CountriesFolder = "Assets/_Ropoly/Content/Countries/ClassicWorld";
        private const string CitiesFolder = "Assets/_Ropoly/Content/Cities/ClassicWorld";
        private const string AirportsFolder = "Assets/_Ropoly/Content/Airports/ClassicWorld";
        private const string UtilitiesFolder = "Assets/_Ropoly/Content/Utilities/ClassicWorld";
        private const string SpecialSpacesFolder = "Assets/_Ropoly/Content/SpecialSpaces/ClassicWorld";
        private const string BoardsFolder = "Assets/_Ropoly/Content/Boards/ClassicWorld";
        private const string EconomicDataSource =
            "World Bank, World Development Indicators (NY.GDP.PCAP.CD)";
        private const int EconomicDataYear = 2024;

        [MenuItem("Ropoly/Content/Create Missing Default World Board")]
        public static void CreateDefaultWorldBoard()
        {
            BoardDefinition existing = AssetDatabase.LoadAssetAtPath<BoardDefinition>(DefaultBoardPath);
            if (existing != null)
            {
                Debug.Log($"Default world board already exists at '{DefaultBoardPath}'.", existing);
                return;
            }

            EnsureFolder(CountriesFolder);
            EnsureFolder(CitiesFolder);
            EnsureFolder(AirportsFolder);
            EnsureFolder(UtilitiesFolder);
            EnsureFolder(SpecialSpacesFolder);
            EnsureFolder(BoardsFolder);

            CityDefinition delhi = CreateCity("Delhi", "city-delhi", "Delhi", 60, 50);
            CityDefinition bengaluru = CreateCity("Bengaluru", "city-bengaluru", "Bengaluru", 70, 50);
            CityDefinition mumbai = CreateCity("Mumbai", "city-mumbai", "Mumbai", 80, 50);

            CityDefinition brasilia = CreateCity("Brasilia", "city-brasilia", "Brasília", 100, 50);
            CityDefinition rioDeJaneiro = CreateCity(
                "RioDeJaneiro",
                "city-rio-de-janeiro",
                "Rio de Janeiro",
                110,
                50);
            CityDefinition saoPaulo = CreateCity("SaoPaulo", "city-sao-paulo", "São Paulo", 120, 50);

            CityDefinition beijing = CreateCity("Beijing", "city-beijing", "Beijing", 140, 100);
            CityDefinition shenzhen = CreateCity("Shenzhen", "city-shenzhen", "Shenzhen", 150, 100);
            CityDefinition shanghai = CreateCity("Shanghai", "city-shanghai", "Shanghai", 160, 100);

            CityDefinition kazan = CreateCity("Kazan", "city-kazan", "Kazan", 180, 100);
            CityDefinition saintPetersburg = CreateCity(
                "SaintPetersburg",
                "city-saint-petersburg",
                "Saint Petersburg",
                190,
                100);
            CityDefinition moscow = CreateCity("Moscow", "city-moscow", "Moscow", 200, 100);

            CityDefinition rome = CreateCity("Rome", "city-rome", "Rome", 220, 150);
            CityDefinition milan = CreateCity("Milan", "city-milan", "Milan", 230, 150);

            CityDefinition marseille = CreateCity("Marseille", "city-marseille", "Marseille", 240, 150);
            CityDefinition paris = CreateCity("Paris", "city-paris", "Paris", 260, 150);

            CityDefinition liverpool = CreateCity("Liverpool", "city-liverpool", "Liverpool", 280, 200);
            CityDefinition manchester = CreateCity("Manchester", "city-manchester", "Manchester", 290, 200);
            CityDefinition london = CreateCity("London", "city-london", "London", 310, 200);

            CityDefinition sanFrancisco = CreateCity(
                "SanFrancisco",
                "city-san-francisco",
                "San Francisco",
                330,
                200);
            CityDefinition losAngeles = CreateCity(
                "LosAngeles",
                "city-los-angeles",
                "Los Angeles",
                360,
                200);
            CityDefinition newYork = CreateCity("NewYork", "city-new-york", "New York", 400, 200);

            CountryDefinition india = CreateCountry(
                "India",
                "country-india",
                "India",
                2_592,
                "#8E44AD",
                delhi,
                bengaluru,
                mumbai);
            CountryDefinition brazil = CreateCountry(
                "Brazil",
                "country-brazil",
                "Brazil",
                10_311,
                "#16A085",
                brasilia,
                rioDeJaneiro,
                saoPaulo);
            CountryDefinition china = CreateCountry(
                "China",
                "country-china",
                "China",
                13_293,
                "#C0392B",
                beijing,
                shenzhen,
                shanghai);
            CountryDefinition russia = CreateCountry(
                "Russia",
                "country-russia",
                "Russia",
                14_962,
                "#3498DB",
                kazan,
                saintPetersburg,
                moscow);
            CountryDefinition italy = CreateCountry(
                "Italy",
                "country-italy",
                "Italy",
                40_430,
                "#F39C12",
                rome,
                milan);
            CountryDefinition france = CreateCountry(
                "France",
                "country-france",
                "France",
                46_103,
                "#E84393",
                marseille,
                paris);
            CountryDefinition unitedKingdom = CreateCountry(
                "UnitedKingdom",
                "country-united-kingdom",
                "United Kingdom",
                53_341,
                "#2C3E50",
                liverpool,
                manchester,
                london);
            CountryDefinition unitedStates = CreateCountry(
                "UnitedStates",
                "country-united-states",
                "United States",
                86_170,
                "#27AE60",
                sanFrancisco,
                losAngeles,
                newYork);

            AirportDefinition jfkAirport = CreateAirport(
                "JfkInternationalAirport",
                "airport-jfk",
                "JFK International Airport");
            AirportDefinition heathrowAirport = CreateAirport(
                "HeathrowAirport",
                "airport-heathrow",
                "Heathrow Airport");
            AirportDefinition charlesDeGaulleAirport = CreateAirport(
                "CharlesDeGaulleAirport",
                "airport-charles-de-gaulle",
                "Charles de Gaulle Airport");
            AirportDefinition beijingCapitalAirport = CreateAirport(
                "BeijingCapitalAirport",
                "airport-beijing-capital",
                "Beijing Capital Airport");

            UtilityDefinition waterService = CreateUtility(
                "WaterService",
                "utility-water-service",
                "Water Service");
            UtilityDefinition electricNetwork = CreateUtility(
                "ElectricNetwork",
                "utility-electric-network",
                "Electric Network");

            SpecialSpaceDefinition start = CreateSpecialSpace(
                "Start",
                "space-start",
                "Start",
                SpecialSpaceKind.Start);
            SpecialSpaceDefinition worldEvent = CreateSpecialSpace(
                "WorldEvent",
                "space-world-event",
                "World Event",
                SpecialSpaceKind.Event,
                eventDeckId: "deck-world-event");
            SpecialSpaceDefinition localOpportunity = CreateSpecialSpace(
                "LocalOpportunity",
                "space-local-opportunity",
                "Local Opportunity",
                SpecialSpaceKind.Event,
                eventDeckId: "deck-local-opportunity");
            SpecialSpaceDefinition incomeFee = CreateSpecialSpace(
                "IncomeFee",
                "space-income-fee",
                "Income Fee",
                SpecialSpaceKind.Fee,
                feeAmount: 200);
            SpecialSpaceDefinition premiumFee = CreateSpecialSpace(
                "PremiumFee",
                "space-premium-fee",
                "Premium Fee",
                SpecialSpaceKind.Fee,
                feeAmount: 100);
            SpecialSpaceDefinition detention = CreateSpecialSpace(
                "Detention",
                "space-detention",
                "Detention / Visiting",
                SpecialSpaceKind.Detention);
            SpecialSpaceDefinition vacation = CreateSpecialSpace(
                "Vacation",
                "space-vacation",
                "Vacation",
                SpecialSpaceKind.Vacation);
            SpecialSpaceDefinition goToDetention = CreateSpecialSpace(
                "GoToDetention",
                "space-go-to-detention",
                "Go to Detention",
                SpecialSpaceKind.GoToDetention);

            CountryDefinition[] countries =
            {
                india,
                brazil,
                china,
                russia,
                italy,
                france,
                unitedKingdom,
                unitedStates,
            };

            BoardTileDefinition[] spaces =
            {
                start,
                delhi,
                worldEvent,
                bengaluru,
                incomeFee,
                jfkAirport,
                mumbai,
                localOpportunity,
                brasilia,
                rioDeJaneiro,
                detention,
                saoPaulo,
                waterService,
                beijing,
                shenzhen,
                heathrowAirport,
                shanghai,
                worldEvent,
                kazan,
                saintPetersburg,
                vacation,
                moscow,
                localOpportunity,
                rome,
                milan,
                charlesDeGaulleAirport,
                marseille,
                paris,
                electricNetwork,
                worldEvent,
                goToDetention,
                liverpool,
                manchester,
                localOpportunity,
                london,
                beijingCapitalAirport,
                premiumFee,
                sanFrancisco,
                losAngeles,
                newYork,
            };

            BoardDefinition board = LoadOrCreate<BoardDefinition>(DefaultBoardPath);
            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("_boardId").stringValue = "classic-world";
            serializedBoard.FindProperty("_displayName").stringValue = "Classic World";
            SetObjectReferences(serializedBoard.FindProperty("_countries"), countries);
            SetObjectReferences(serializedBoard.FindProperty("_spaces"), spaces);
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(board);

            BoardValidationResult validation = board.Validate();
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(
                    "Generated default world board is invalid:\n" + FormatErrors(validation));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!UnityEngine.Application.isBatchMode)
            {
                Selection.activeObject = board;
            }

            Debug.Log($"Created validated 40-space default world board at '{DefaultBoardPath}'.", board);
        }

        private static CityDefinition CreateCity(
            string assetName,
            string id,
            string displayName,
            int purchasePrice,
            int upgradeCost)
        {
            CityDefinition city = LoadOrCreate<CityDefinition>($"{CitiesFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(city);
            SetIdentity(serialized, id, displayName);
            serialized.FindProperty("_purchasePrice").intValue = purchasePrice;
            serialized.FindProperty("_mortgageValue").intValue = purchasePrice / 2;
            serialized.FindProperty("_upgradeCost").intValue = upgradeCost;
            SetIntValues(serialized.FindProperty("_rentByUpgradeLevel"), CreateRentSchedule(purchasePrice));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(city);
            return city;
        }

        private static CountryDefinition CreateCountry(
            string assetName,
            string id,
            string displayName,
            int gdpPerCapitaUsd,
            string setColorHex,
            params CityDefinition[] cities)
        {
            CountryDefinition country = LoadOrCreate<CountryDefinition>($"{CountriesFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(country);
            serialized.FindProperty("_countryId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
            serialized.FindProperty("_expectedCityCount").intValue = cities.Length;
            if (!ColorUtility.TryParseHtmlString(setColorHex, out Color setColor))
            {
                throw new ArgumentException($"Invalid set color '{setColorHex}'.", nameof(setColorHex));
            }

            serialized.FindProperty("_setColor").colorValue = setColor;
            SetObjectReferences(serialized.FindProperty("_cities"), cities);
            serialized.FindProperty("_gdpPerCapitaUsd").intValue = gdpPerCapitaUsd;
            serialized.FindProperty("_economicDataYear").intValue = EconomicDataYear;
            serialized.FindProperty("_economicDataSource").stringValue = EconomicDataSource;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(country);
            return country;
        }

        private static AirportDefinition CreateAirport(string assetName, string id, string displayName)
        {
            AirportDefinition airport = LoadOrCreate<AirportDefinition>($"{AirportsFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(airport);
            SetIdentity(serialized, id, displayName);
            serialized.FindProperty("_purchasePrice").intValue = 200;
            serialized.FindProperty("_mortgageValue").intValue = 100;
            SetIntValues(serialized.FindProperty("_rentByOwnedCount"), new[] { 25, 50, 100, 200 });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(airport);
            return airport;
        }

        private static UtilityDefinition CreateUtility(string assetName, string id, string displayName)
        {
            UtilityDefinition utility = LoadOrCreate<UtilityDefinition>($"{UtilitiesFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(utility);
            SetIdentity(serialized, id, displayName);
            serialized.FindProperty("_purchasePrice").intValue = 150;
            serialized.FindProperty("_mortgageValue").intValue = 75;
            SetIntValues(serialized.FindProperty("_rentMultiplierByOwnedCount"), new[] { 4, 10 });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(utility);
            return utility;
        }

        private static SpecialSpaceDefinition CreateSpecialSpace(
            string assetName,
            string id,
            string displayName,
            SpecialSpaceKind kind,
            string eventDeckId = null,
            int feeAmount = 0)
        {
            SpecialSpaceDefinition space =
                LoadOrCreate<SpecialSpaceDefinition>($"{SpecialSpacesFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(space);
            SetIdentity(serialized, id, displayName);
            serialized.FindProperty("_specialKind").enumValueIndex = (int)kind;
            serialized.FindProperty("_eventDeckId").stringValue = eventDeckId ?? string.Empty;
            serialized.FindProperty("_feeAmount").intValue = feeAmount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(space);
            return space;
        }

        private static int[] CreateRentSchedule(int purchasePrice)
        {
            return new[]
            {
                RoundToFive(purchasePrice * 0.08f),
                RoundToFive(purchasePrice * 0.50f),
                RoundToFive(purchasePrice * 1.50f),
                RoundToFive(purchasePrice * 3.20f),
                RoundToFive(purchasePrice * 4.50f),
                RoundToFive(purchasePrice * 5.80f),
            };
        }

        private static int RoundToFive(float value)
        {
            return Mathf.Max(5, Mathf.RoundToInt(value / 5f) * 5);
        }

        private static void SetIdentity(SerializedObject serialized, string id, string displayName)
        {
            serialized.FindProperty("_contentId").stringValue = id;
            serialized.FindProperty("_displayName").stringValue = displayName;
        }

        private static void SetIntValues(SerializedProperty property, IReadOnlyList<int> values)
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).intValue = values[index];
            }
        }

        private static void SetObjectReferences<T>(SerializedProperty property, IReadOnlyList<T> values)
            where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int index = 0; index < values.Count; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string currentPath = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string nextPath = $"{currentPath}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }

        private static string FormatErrors(BoardValidationResult validation)
        {
            List<string> messages = new List<string>();
            foreach (BoardValidationError error in validation.Errors)
            {
                messages.Add(error.ToString());
            }

            return string.Join("\n", messages);
        }
    }
}
