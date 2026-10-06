using System;
using System.Collections.Generic;

namespace Ropoly.Core.Board
{
    [Serializable]
    public sealed class CountrySnapshot
    {
        private readonly string[] _cityIds;

        public CountrySnapshot(
            string countryId,
            string displayName,
            int expectedCityCount,
            int gdpPerCapitaUsd,
            int economicDataYear,
            string economicDataSource,
            string setColorHex,
            string[] cityIds)
        {
            CountryId = countryId;
            DisplayName = displayName;
            ExpectedCityCount = expectedCityCount;
            GdpPerCapitaUsd = gdpPerCapitaUsd;
            EconomicDataYear = economicDataYear;
            EconomicDataSource = economicDataSource;
            SetColorHex = setColorHex;
            _cityIds = Copy(cityIds);
        }

        public string CountryId { get; }
        public string DisplayName { get; }
        public int ExpectedCityCount { get; }
        public int GdpPerCapitaUsd { get; }
        public int EconomicDataYear { get; }
        public string EconomicDataSource { get; }
        public string SetColorHex { get; }
        public IReadOnlyList<string> CityIds => _cityIds;

        private static string[] Copy(string[] values)
        {
            return values == null ? Array.Empty<string>() : (string[])values.Clone();
        }
    }

    [Serializable]
    public sealed class CitySnapshot
    {
        private readonly int[] _rentByUpgradeLevel;

        public CitySnapshot(
            string cityId,
            string displayName,
            int purchasePrice,
            int mortgageValue,
            int upgradeCost,
            int[] rentByUpgradeLevel)
        {
            CityId = cityId;
            DisplayName = displayName;
            PurchasePrice = purchasePrice;
            MortgageValue = mortgageValue;
            UpgradeCost = upgradeCost;
            _rentByUpgradeLevel = Copy(rentByUpgradeLevel);
        }

        public string CityId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int MortgageValue { get; }
        public int UpgradeCost { get; }
        public IReadOnlyList<int> RentByUpgradeLevel => _rentByUpgradeLevel;

        private static int[] Copy(int[] values)
        {
            return values == null ? Array.Empty<int>() : (int[])values.Clone();
        }
    }

    [Serializable]
    public sealed class AirportSnapshot
    {
        private readonly int[] _rentByOwnedCount;

        public AirportSnapshot(
            string airportId,
            string displayName,
            int purchasePrice,
            int mortgageValue,
            int[] rentByOwnedCount)
        {
            AirportId = airportId;
            DisplayName = displayName;
            PurchasePrice = purchasePrice;
            MortgageValue = mortgageValue;
            _rentByOwnedCount = Copy(rentByOwnedCount);
        }

        public string AirportId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int MortgageValue { get; }
        public IReadOnlyList<int> RentByOwnedCount => _rentByOwnedCount;

        private static int[] Copy(int[] values)
        {
            return values == null ? Array.Empty<int>() : (int[])values.Clone();
        }
    }

    [Serializable]
    public sealed class UtilitySnapshot
    {
        private readonly int[] _rentMultiplierByOwnedCount;

        public UtilitySnapshot(
            string utilityId,
            string displayName,
            int purchasePrice,
            int mortgageValue,
            int[] rentMultiplierByOwnedCount)
        {
            UtilityId = utilityId;
            DisplayName = displayName;
            PurchasePrice = purchasePrice;
            MortgageValue = mortgageValue;
            _rentMultiplierByOwnedCount = Copy(rentMultiplierByOwnedCount);
        }

        public string UtilityId { get; }
        public string DisplayName { get; }
        public int PurchasePrice { get; }
        public int MortgageValue { get; }
        public IReadOnlyList<int> RentMultiplierByOwnedCount => _rentMultiplierByOwnedCount;

        private static int[] Copy(int[] values)
        {
            return values == null ? Array.Empty<int>() : (int[])values.Clone();
        }
    }

    [Serializable]
    public sealed class SpecialSpaceSnapshot
    {
        public SpecialSpaceSnapshot(
            string spaceId,
            string displayName,
            SpecialSpaceKind specialKind,
            string eventDeckId,
            int feeAmount)
        {
            SpaceId = spaceId;
            DisplayName = displayName;
            SpecialKind = specialKind;
            EventDeckId = eventDeckId;
            FeeAmount = feeAmount;
        }

        public string SpaceId { get; }
        public string DisplayName { get; }
        public SpecialSpaceKind SpecialKind { get; }
        public string EventDeckId { get; }
        public int FeeAmount { get; }
    }

    [Serializable]
    public sealed class BoardTileSnapshot
    {
        public BoardTileSnapshot(int index, BoardTileKind kind, string contentId)
        {
            Index = index;
            Kind = kind;
            ContentId = contentId;
        }

        public int Index { get; }
        public BoardTileKind Kind { get; }
        public string ContentId { get; }
    }
}
