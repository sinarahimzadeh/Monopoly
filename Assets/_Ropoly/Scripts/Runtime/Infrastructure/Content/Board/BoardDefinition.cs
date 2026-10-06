using System.Collections.Generic;
using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "Board", menuName = "Ropoly/Board/Board", order = 1)]
    public sealed class BoardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable lowercase ID used by saves and networking.")]
        private string _boardId;

        [SerializeField]
        private string _displayName;

        [Header("Country sets")]
        [SerializeField]
        private List<CountryDefinition> _countries = new List<CountryDefinition>();

        [Header("Clockwise board order")]
        [SerializeField]
        [Tooltip("Element zero is the starting corner. Add the remaining spaces clockwise.")]
        private List<BoardTileDefinition> _spaces = new List<BoardTileDefinition>();

        public string BoardId => _boardId;
        public string DisplayName => _displayName;
        public IReadOnlyList<CountryDefinition> Countries => _countries;
        public IReadOnlyList<BoardTileDefinition> Spaces => _spaces;

        public BoardSnapshot CreateSnapshot()
        {
            List<CountrySnapshot> countries = new List<CountrySnapshot>();
            List<CitySnapshot> cities = new List<CitySnapshot>();
            HashSet<CityDefinition> seenCities = new HashSet<CityDefinition>();

            foreach (CountryDefinition country in _countries)
            {
                countries.Add(country == null ? null : country.CreateSnapshot());
                if (country == null)
                {
                    continue;
                }

                foreach (CityDefinition city in country.Cities)
                {
                    if (city != null && seenCities.Add(city))
                    {
                        cities.Add(city.CreateSnapshot());
                    }
                }
            }

            List<AirportSnapshot> airports = new List<AirportSnapshot>();
            List<UtilitySnapshot> utilities = new List<UtilitySnapshot>();
            List<SpecialSpaceSnapshot> specialSpaces = new List<SpecialSpaceSnapshot>();
            HashSet<BoardTileDefinition> capturedDefinitions = new HashSet<BoardTileDefinition>();
            BoardTileSnapshot[] tiles = new BoardTileSnapshot[_spaces.Count];

            for (int index = 0; index < _spaces.Count; index++)
            {
                BoardTileDefinition definition = _spaces[index];
                if (definition == null)
                {
                    tiles[index] = new BoardTileSnapshot(index, BoardTileKind.Special, null);
                    continue;
                }

                tiles[index] = new BoardTileSnapshot(index, definition.Kind, definition.ContentId);
                if (!capturedDefinitions.Add(definition))
                {
                    continue;
                }

                switch (definition)
                {
                    case AirportDefinition airport:
                        airports.Add(airport.CreateSnapshot());
                        break;
                    case UtilityDefinition utility:
                        utilities.Add(utility.CreateSnapshot());
                        break;
                    case SpecialSpaceDefinition specialSpace:
                        specialSpaces.Add(specialSpace.CreateSnapshot());
                        break;
                }
            }

            return new BoardSnapshot(
                BoardContentLimits.CurrentSchemaVersion,
                _boardId,
                _displayName,
                countries.ToArray(),
                cities.ToArray(),
                airports.ToArray(),
                utilities.ToArray(),
                specialSpaces.ToArray(),
                tiles);
        }

        public BoardValidationResult Validate()
        {
            return BoardSnapshotValidator.Validate(CreateSnapshot());
        }
    }
}
