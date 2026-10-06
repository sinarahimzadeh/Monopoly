using System;
using System.Collections.Generic;

namespace Ropoly.Core.Board
{
    public static class BoardSnapshotValidator
    {
        public static BoardValidationResult Validate(BoardSnapshot snapshot)
        {
            BoardValidationResult result = new BoardValidationResult();
            if (snapshot == null)
            {
                result.Add(BoardValidationErrorCode.MissingSnapshot, "Board snapshot is required.");
                return result;
            }

            ValidateBoardIdentity(snapshot, result);

            HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, CitySnapshot> cities = ValidateCities(snapshot.Cities, allIds, result);
            Dictionary<string, AirportSnapshot> airports = ValidateAirports(snapshot.Airports, allIds, result);
            Dictionary<string, UtilitySnapshot> utilities = ValidateUtilities(snapshot.Utilities, allIds, result);
            Dictionary<string, SpecialSpaceSnapshot> specialSpaces =
                ValidateSpecialSpaces(snapshot.SpecialSpaces, allIds, result);

            ValidateCountries(snapshot.Countries, cities, allIds, result);
            ValidateTiles(snapshot.Tiles, cities, airports, utilities, specialSpaces, result);
            return result;
        }

        public static bool IsStableId(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Length > BoardContentLimits.MaximumStableIdLength ||
                value[0] == '-' ||
                value[value.Length - 1] == '-')
            {
                return false;
            }

            foreach (char character in value)
            {
                bool isLowercaseLetter = character >= 'a' && character <= 'z';
                bool isNumber = character >= '0' && character <= '9';
                if (!isLowercaseLetter && !isNumber && character != '-')
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateBoardIdentity(BoardSnapshot snapshot, BoardValidationResult result)
        {
            if (snapshot.SchemaVersion != BoardContentLimits.CurrentSchemaVersion)
            {
                result.Add(
                    BoardValidationErrorCode.UnsupportedSchemaVersion,
                    $"Schema version must be {BoardContentLimits.CurrentSchemaVersion}.");
            }

            if (!IsStableId(snapshot.BoardId))
            {
                result.Add(BoardValidationErrorCode.InvalidBoardId, "Board ID must be a stable lowercase ID.");
            }

            if (!IsDisplayNameValid(snapshot.DisplayName))
            {
                result.Add(BoardValidationErrorCode.InvalidBoardName, "Board display name is invalid.");
            }

            if (snapshot.Tiles.Count < BoardContentLimits.MinimumBoardSpaces ||
                snapshot.Tiles.Count > BoardContentLimits.MaximumBoardSpaces)
            {
                result.Add(
                    BoardValidationErrorCode.SpaceCountOutOfRange,
                    $"Board space count must be between {BoardContentLimits.MinimumBoardSpaces} and " +
                    $"{BoardContentLimits.MaximumBoardSpaces}.");
            }
        }

        private static Dictionary<string, CitySnapshot> ValidateCities(
            IReadOnlyList<CitySnapshot> values,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            Dictionary<string, CitySnapshot> valid = new Dictionary<string, CitySnapshot>(StringComparer.Ordinal);
            foreach (CitySnapshot city in values)
            {
                if (city == null)
                {
                    result.Add(BoardValidationErrorCode.InvalidCity, "City definition is missing.");
                    continue;
                }

                bool identityValid = ValidateContentIdentity(
                    city.CityId,
                    city.DisplayName,
                    BoardValidationErrorCode.InvalidCity,
                    "city",
                    allIds,
                    result);
                bool economyValid = IsPurchasableEconomyValid(
                    city.PurchasePrice,
                    city.MortgageValue,
                    city.UpgradeCost) &&
                    IsNonDecreasingMoneySchedule(
                        city.RentByUpgradeLevel,
                        BoardContentLimits.CityRentLevelCount);

                if (!economyValid)
                {
                    result.Add(BoardValidationErrorCode.InvalidCity, $"City '{city.CityId}' has invalid economy values.");
                }

                if (identityValid && !valid.ContainsKey(city.CityId))
                {
                    valid.Add(city.CityId, city);
                }
            }

            return valid;
        }

        private static Dictionary<string, AirportSnapshot> ValidateAirports(
            IReadOnlyList<AirportSnapshot> values,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            Dictionary<string, AirportSnapshot> valid = new Dictionary<string, AirportSnapshot>(StringComparer.Ordinal);
            foreach (AirportSnapshot airport in values)
            {
                if (airport == null)
                {
                    result.Add(BoardValidationErrorCode.InvalidAirport, "Airport definition is missing.");
                    continue;
                }

                bool identityValid = ValidateContentIdentity(
                    airport.AirportId,
                    airport.DisplayName,
                    BoardValidationErrorCode.InvalidAirport,
                    "airport",
                    allIds,
                    result);
                bool economyValid = IsPurchasableEconomyValid(
                    airport.PurchasePrice,
                    airport.MortgageValue,
                    upgradeCost: 0) &&
                    IsNonDecreasingMoneySchedule(
                        airport.RentByOwnedCount,
                        BoardContentLimits.AirportRentLevelCount);

                if (!economyValid)
                {
                    result.Add(
                        BoardValidationErrorCode.InvalidAirport,
                        $"Airport '{airport.AirportId}' has invalid economy values.");
                }

                if (identityValid && !valid.ContainsKey(airport.AirportId))
                {
                    valid.Add(airport.AirportId, airport);
                }
            }

            return valid;
        }

        private static Dictionary<string, UtilitySnapshot> ValidateUtilities(
            IReadOnlyList<UtilitySnapshot> values,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            Dictionary<string, UtilitySnapshot> valid = new Dictionary<string, UtilitySnapshot>(StringComparer.Ordinal);
            foreach (UtilitySnapshot utility in values)
            {
                if (utility == null)
                {
                    result.Add(BoardValidationErrorCode.InvalidUtility, "Utility definition is missing.");
                    continue;
                }

                bool identityValid = ValidateContentIdentity(
                    utility.UtilityId,
                    utility.DisplayName,
                    BoardValidationErrorCode.InvalidUtility,
                    "utility",
                    allIds,
                    result);
                bool economyValid = IsPurchasableEconomyValid(
                    utility.PurchasePrice,
                    utility.MortgageValue,
                    upgradeCost: 0) &&
                    IsPositiveNonDecreasingSchedule(
                        utility.RentMultiplierByOwnedCount,
                        BoardContentLimits.UtilityRentMultiplierCount);

                if (!economyValid)
                {
                    result.Add(
                        BoardValidationErrorCode.InvalidUtility,
                        $"Utility '{utility.UtilityId}' has invalid economy values.");
                }

                if (identityValid && !valid.ContainsKey(utility.UtilityId))
                {
                    valid.Add(utility.UtilityId, utility);
                }
            }

            return valid;
        }

        private static Dictionary<string, SpecialSpaceSnapshot> ValidateSpecialSpaces(
            IReadOnlyList<SpecialSpaceSnapshot> values,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            Dictionary<string, SpecialSpaceSnapshot> valid =
                new Dictionary<string, SpecialSpaceSnapshot>(StringComparer.Ordinal);
            foreach (SpecialSpaceSnapshot space in values)
            {
                if (space == null)
                {
                    result.Add(BoardValidationErrorCode.InvalidSpecialSpace, "Special-space definition is missing.");
                    continue;
                }

                bool identityValid = ValidateContentIdentity(
                    space.SpaceId,
                    space.DisplayName,
                    BoardValidationErrorCode.InvalidSpecialSpace,
                    "special space",
                    allIds,
                    result);
                bool settingsValid = Enum.IsDefined(typeof(SpecialSpaceKind), space.SpecialKind) &&
                    IsMoneyValueValid(space.FeeAmount) &&
                    (space.SpecialKind != SpecialSpaceKind.Fee || space.FeeAmount > 0) &&
                    (space.SpecialKind != SpecialSpaceKind.Event || IsStableId(space.EventDeckId));

                if (!settingsValid)
                {
                    result.Add(
                        BoardValidationErrorCode.InvalidSpecialSpace,
                        $"Special space '{space.SpaceId}' has invalid settings.");
                }

                if (identityValid && !valid.ContainsKey(space.SpaceId))
                {
                    valid.Add(space.SpaceId, space);
                }
            }

            return valid;
        }

        private static void ValidateCountries(
            IReadOnlyList<CountrySnapshot> values,
            IReadOnlyDictionary<string, CitySnapshot> cities,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            Dictionary<string, int> cityMembership = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (CountrySnapshot country in values)
            {
                if (country == null)
                {
                    result.Add(BoardValidationErrorCode.InvalidCountry, "Country definition is missing.");
                    continue;
                }

                ValidateContentIdentity(
                    country.CountryId,
                    country.DisplayName,
                    BoardValidationErrorCode.InvalidCountry,
                    "country",
                    allIds,
                    result);

                bool settingsValid =
                    country.ExpectedCityCount >= BoardContentLimits.MinimumCitiesPerCountry &&
                    country.ExpectedCityCount <= BoardContentLimits.MaximumCitiesPerCountry &&
                    country.GdpPerCapitaUsd > 0 &&
                    country.EconomicDataYear >= BoardContentLimits.MinimumEconomicDataYear &&
                    country.EconomicDataYear <= BoardContentLimits.MaximumEconomicDataYear &&
                    !string.IsNullOrWhiteSpace(country.EconomicDataSource) &&
                    country.EconomicDataSource.Length <= BoardContentLimits.MaximumSourceNameLength &&
                    IsHtmlColor(country.SetColorHex);

                if (!settingsValid)
                {
                    result.Add(BoardValidationErrorCode.InvalidCountry, $"Country '{country.CountryId}' has invalid settings.");
                }

                if (country.CityIds.Count != country.ExpectedCityCount)
                {
                    result.Add(
                        BoardValidationErrorCode.CountryCityCountMismatch,
                        $"Country '{country.CountryId}' requires {country.ExpectedCityCount} cities but has " +
                        $"{country.CityIds.Count}.");
                }

                HashSet<string> localCityIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (string cityId in country.CityIds)
                {
                    if (!IsStableId(cityId) || !cities.ContainsKey(cityId) || !localCityIds.Add(cityId))
                    {
                        result.Add(
                            BoardValidationErrorCode.InvalidCountry,
                            $"Country '{country.CountryId}' contains an invalid or duplicate city reference.");
                        continue;
                    }

                    cityMembership.TryGetValue(cityId, out int membershipCount);
                    cityMembership[cityId] = membershipCount + 1;
                }
            }

            foreach (string cityId in cities.Keys)
            {
                cityMembership.TryGetValue(cityId, out int membershipCount);
                if (membershipCount == 0)
                {
                    result.Add(BoardValidationErrorCode.UnassignedCity, $"City '{cityId}' is not assigned to a country.");
                }
                else if (membershipCount > 1)
                {
                    result.Add(
                        BoardValidationErrorCode.CityAssignedToMultipleCountries,
                        $"City '{cityId}' is assigned to more than one country.");
                }
            }
        }

        private static void ValidateTiles(
            IReadOnlyList<BoardTileSnapshot> tiles,
            IReadOnlyDictionary<string, CitySnapshot> cities,
            IReadOnlyDictionary<string, AirportSnapshot> airports,
            IReadOnlyDictionary<string, UtilitySnapshot> utilities,
            IReadOnlyDictionary<string, SpecialSpaceSnapshot> specialSpaces,
            BoardValidationResult result)
        {
            HashSet<string> placedPurchasableContent = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> placedCities = new HashSet<string>(StringComparer.Ordinal);

            for (int index = 0; index < tiles.Count; index++)
            {
                BoardTileSnapshot tile = tiles[index];
                if (tile == null)
                {
                    result.Add(BoardValidationErrorCode.MissingTileContent, $"Board tile {index} is missing.");
                    continue;
                }

                if (tile.Index != index)
                {
                    result.Add(
                        BoardValidationErrorCode.InvalidTileIndex,
                        $"Board tile at position {index} stores index {tile.Index}.");
                }

                bool contentExists = tile.Kind switch
                {
                    BoardTileKind.City => cities.ContainsKey(tile.ContentId),
                    BoardTileKind.Airport => airports.ContainsKey(tile.ContentId),
                    BoardTileKind.Utility => utilities.ContainsKey(tile.ContentId),
                    BoardTileKind.Special => specialSpaces.ContainsKey(tile.ContentId),
                    _ => false,
                };

                if (!contentExists)
                {
                    result.Add(
                        BoardValidationErrorCode.MissingTileContent,
                        $"Board tile {index} references missing {tile.Kind} content '{tile.ContentId}'.");
                    continue;
                }

                if (tile.Kind == BoardTileKind.City)
                {
                    placedCities.Add(tile.ContentId);
                }

                if (tile.Kind != BoardTileKind.Special && !placedPurchasableContent.Add(tile.ContentId))
                {
                    result.Add(
                        BoardValidationErrorCode.DuplicatePlayableTile,
                        $"Purchasable content '{tile.ContentId}' appears on the board more than once.");
                }
            }

            foreach (string cityId in cities.Keys)
            {
                if (!placedCities.Contains(cityId))
                {
                    result.Add(
                        BoardValidationErrorCode.MissingTileContent,
                        $"Country city '{cityId}' is not placed on the board.");
                }
            }
        }

        private static bool ValidateContentIdentity(
            string id,
            string displayName,
            BoardValidationErrorCode invalidCode,
            string contentType,
            HashSet<string> allIds,
            BoardValidationResult result)
        {
            if (!IsStableId(id) || !IsDisplayNameValid(displayName))
            {
                result.Add(invalidCode, $"The {contentType} ID or display name is invalid.");
                return false;
            }

            if (!allIds.Add(id))
            {
                result.Add(BoardValidationErrorCode.DuplicateContentId, $"Content ID '{id}' is duplicated.");
                return false;
            }

            return true;
        }

        private static bool IsDisplayNameValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.Length <= BoardContentLimits.MaximumDisplayNameLength;
        }

        private static bool IsPurchasableEconomyValid(int purchasePrice, int mortgageValue, int upgradeCost)
        {
            return purchasePrice > 0 && IsMoneyValueValid(purchasePrice) &&
                mortgageValue >= 0 && mortgageValue <= purchasePrice &&
                IsMoneyValueValid(mortgageValue) &&
                upgradeCost >= 0 && IsMoneyValueValid(upgradeCost);
        }

        private static bool IsNonDecreasingMoneySchedule(IReadOnlyList<int> values, int expectedCount)
        {
            if (values.Count != expectedCount)
            {
                return false;
            }

            int previous = -1;
            foreach (int value in values)
            {
                if (!IsMoneyValueValid(value) || value < previous)
                {
                    return false;
                }

                previous = value;
            }

            return true;
        }

        private static bool IsPositiveNonDecreasingSchedule(IReadOnlyList<int> values, int expectedCount)
        {
            if (values.Count != expectedCount)
            {
                return false;
            }

            int previous = 0;
            foreach (int value in values)
            {
                if (value <= 0 || value < previous)
                {
                    return false;
                }

                previous = value;
            }

            return true;
        }

        private static bool IsMoneyValueValid(int value)
        {
            return value >= 0 && value <= BoardContentLimits.MaximumMoneyValue;
        }

        private static bool IsHtmlColor(string value)
        {
            if (string.IsNullOrEmpty(value) || (value.Length != 7 && value.Length != 9) || value[0] != '#')
            {
                return false;
            }

            for (int index = 1; index < value.Length; index++)
            {
                char character = value[index];
                bool isDigit = character >= '0' && character <= '9';
                bool isUpperHex = character >= 'A' && character <= 'F';
                if (!isDigit && !isUpperHex)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
