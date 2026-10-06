using System;
using System.Collections.Generic;

namespace Ropoly.Core.Board
{
    /// <summary>
    /// Frozen board values used by gameplay, saves, replays, and networking.
    /// It deliberately contains no Unity object references.
    /// </summary>
    [Serializable]
    public sealed class BoardSnapshot
    {
        private readonly CountrySnapshot[] _countries;
        private readonly CitySnapshot[] _cities;
        private readonly AirportSnapshot[] _airports;
        private readonly UtilitySnapshot[] _utilities;
        private readonly SpecialSpaceSnapshot[] _specialSpaces;
        private readonly BoardTileSnapshot[] _tiles;

        public BoardSnapshot(
            int schemaVersion,
            string boardId,
            string displayName,
            CountrySnapshot[] countries,
            CitySnapshot[] cities,
            AirportSnapshot[] airports,
            UtilitySnapshot[] utilities,
            SpecialSpaceSnapshot[] specialSpaces,
            BoardTileSnapshot[] tiles)
        {
            SchemaVersion = schemaVersion;
            BoardId = boardId;
            DisplayName = displayName;
            _countries = Copy(countries);
            _cities = Copy(cities);
            _airports = Copy(airports);
            _utilities = Copy(utilities);
            _specialSpaces = Copy(specialSpaces);
            _tiles = Copy(tiles);
        }

        public int SchemaVersion { get; }
        public string BoardId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<CountrySnapshot> Countries => _countries;
        public IReadOnlyList<CitySnapshot> Cities => _cities;
        public IReadOnlyList<AirportSnapshot> Airports => _airports;
        public IReadOnlyList<UtilitySnapshot> Utilities => _utilities;
        public IReadOnlyList<SpecialSpaceSnapshot> SpecialSpaces => _specialSpaces;
        public IReadOnlyList<BoardTileSnapshot> Tiles => _tiles;

        private static T[] Copy<T>(T[] values)
        {
            return values == null ? Array.Empty<T>() : (T[])values.Clone();
        }
    }
}
