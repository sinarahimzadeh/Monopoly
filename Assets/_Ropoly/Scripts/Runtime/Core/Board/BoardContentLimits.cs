namespace Ropoly.Core.Board
{
    public static class BoardContentLimits
    {
        public const int CurrentSchemaVersion = 1;

        public const int MinimumCitiesPerCountry = 2;
        public const int MaximumCitiesPerCountry = 3;
        public const int MinimumBoardSpaces = 1;
        public const int MaximumBoardSpaces = 128;

        public const int CityRentLevelCount = 6;
        public const int AirportRentLevelCount = 4;
        public const int UtilityRentMultiplierCount = 2;

        public const int MinimumEconomicDataYear = 1900;
        public const int MaximumEconomicDataYear = 2100;
        public const int MaximumMoneyValue = 1_000_000;

        public const int MaximumStableIdLength = 64;
        public const int MaximumDisplayNameLength = 64;
        public const int MaximumSourceNameLength = 128;
    }
}
