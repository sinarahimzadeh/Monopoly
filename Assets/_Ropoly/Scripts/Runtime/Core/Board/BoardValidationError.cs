namespace Ropoly.Core.Board
{
    public enum BoardValidationErrorCode
    {
        MissingSnapshot,
        UnsupportedSchemaVersion,
        InvalidBoardId,
        InvalidBoardName,
        SpaceCountOutOfRange,
        InvalidCountry,
        InvalidCity,
        InvalidAirport,
        InvalidUtility,
        InvalidSpecialSpace,
        DuplicateContentId,
        CountryCityCountMismatch,
        CityAssignedToMultipleCountries,
        UnassignedCity,
        InvalidTileIndex,
        MissingTileContent,
        DuplicatePlayableTile,
    }

    public readonly struct BoardValidationError
    {
        public BoardValidationError(BoardValidationErrorCode code, string message)
        {
            Code = code;
            Message = message;
        }

        public BoardValidationErrorCode Code { get; }
        public string Message { get; }

        public override string ToString()
        {
            return $"{Code}: {Message}";
        }
    }
}
