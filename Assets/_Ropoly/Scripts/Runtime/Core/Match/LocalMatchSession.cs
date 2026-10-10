using System;
using System.Collections.Generic;
using Ropoly.Core.Board;

namespace Ropoly.Core.Match
{
    /// <summary>
    /// Owns local lobby decisions. A future host/server can call the same operations.
    /// </summary>
    public sealed class LocalMatchSession
    {
        public const int MinimumPlayers = 2;
        public const int MaximumPlayers = 4;

        private readonly HashSet<string> _availableCreatureIds;
        private readonly int _startingCash;
        private readonly int _boardTileCount;
        private readonly int _passStartCash;
        private readonly Dictionary<int, PropertyState> _propertiesByTileIndex;

        public LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            int boardTileCount = 40,
            int passStartCash = 200)
            : this(
                playerCount,
                startingCash,
                availableCreatureIds,
                boardTileCount,
                passStartCash,
                board: null)
        {
        }

        public LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            BoardSnapshot board,
            int passStartCash = 200)
            : this(
                playerCount,
                startingCash,
                availableCreatureIds,
                GetBoardTileCount(board),
                passStartCash,
                board)
        {
        }

        private LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            int boardTileCount,
            int passStartCash,
            BoardSnapshot board)
        {
            if (playerCount < MinimumPlayers || playerCount > MaximumPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerCount));
            }

            if (startingCash < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingCash));
            }

            if (boardTileCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(boardTileCount));
            }

            if (passStartCash < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(passStartCash));
            }

            _availableCreatureIds = new HashSet<string>(
                availableCreatureIds ?? throw new ArgumentNullException(nameof(availableCreatureIds)),
                StringComparer.Ordinal);
            if (_availableCreatureIds.Count < MaximumPlayers)
            {
                throw new ArgumentException(
                    $"At least {MaximumPlayers} unique creatures are required.",
                    nameof(availableCreatureIds));
            }

            _startingCash = startingCash;
            _boardTileCount = boardTileCount;
            _passStartCash = passStartCash;
            List<PropertyState> properties = CreatePropertyStates(board);
            State = new MatchState(
                CreateLocalMatchId(),
                CreatePlayers(playerCount),
                properties);
            _propertiesByTileIndex = new Dictionary<int, PropertyState>(properties.Count);
            foreach (PropertyState property in properties)
            {
                _propertiesByTileIndex.Add(property.TileIndex, property);
            }
        }

        public MatchState State { get; }

        public PropertyPurchaseOffer CurrentPurchaseOffer
        {
            get
            {
                if (State.Phase != MatchPhase.InProgress ||
                    State.Turn == null ||
                    State.Turn.Phase != TurnPhase.AwaitingPropertyDecision ||
                    !_propertiesByTileIndex.TryGetValue(
                        State.Turn.PendingPropertyTileIndex,
                        out PropertyState property) ||
                    property.IsOwned)
                {
                    return null;
                }

                int playerIndex = State.Turn.CurrentPlayerIndex;
                return new PropertyPurchaseOffer(
                    property,
                    playerIndex,
                    State.Players[playerIndex].Cash);
            }
        }

        public bool CanStart
        {
            get
            {
                if (State.Phase != MatchPhase.Lobby || State.Players.Count < MinimumPlayers)
                {
                    return false;
                }

                foreach (PlayerState player in State.Players)
                {
                    if (!player.IsReady)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool TrySetPlayerCount(int playerCount)
        {
            if (State.Phase != MatchPhase.Lobby ||
                playerCount < MinimumPlayers ||
                playerCount > MaximumPlayers)
            {
                return false;
            }

            List<PlayerState> players = State.MutablePlayers;
            while (players.Count > playerCount)
            {
                players.RemoveAt(players.Count - 1);
            }

            while (players.Count < playerCount)
            {
                players.Add(CreatePlayer(players.Count));
            }

            return true;
        }

        public CreatureSelectionResult TrySelectCreature(int playerIndex, string creatureId)
        {
            if (State.Phase != MatchPhase.Lobby)
            {
                return CreatureSelectionResult.MatchAlreadyStarted;
            }

            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return CreatureSelectionResult.InvalidPlayerIndex;
            }

            if (string.IsNullOrWhiteSpace(creatureId) || !_availableCreatureIds.Contains(creatureId))
            {
                return CreatureSelectionResult.UnknownCreature;
            }

            for (int index = 0; index < State.Players.Count; index++)
            {
                if (index != playerIndex &&
                    string.Equals(State.Players[index].CreatureId, creatureId, StringComparison.Ordinal))
                {
                    return CreatureSelectionResult.CreatureAlreadySelected;
                }
            }

            State.MutablePlayers[playerIndex].SelectCreature(creatureId);
            return CreatureSelectionResult.Success;
        }

        public bool TryStartMatch()
        {
            if (!CanStart)
            {
                return false;
            }

            State.Phase = MatchPhase.InProgress;
            State.Turn = new TurnState(currentPlayerIndex: 0);
            return true;
        }

        public bool TryRollDice(IDiceRollSource rollSource, out DiceRoll roll)
        {
            roll = null;
            if (rollSource == null ||
                State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingRoll)
            {
                return false;
            }

            int firstValue = rollSource.NextDieValue();
            int secondValue = rollSource.NextDieValue();
            if (!DiceRoll.IsDieValueValid(firstValue) ||
                !DiceRoll.IsDieValueValid(secondValue))
            {
                return false;
            }

            roll = new DiceRoll(firstValue, secondValue);
            State.Turn.LastRoll = roll;
            State.Turn.Phase = TurnPhase.AwaitingMovement;
            return true;
        }

        public bool TryAdvanceTurn()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingTurnEnd)
            {
                return false;
            }

            State.Turn.Advance(State.Players.Count);
            return true;
        }

        public bool TryBeginMovement(out PlayerMovement movement)
        {
            movement = null;
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingMovement ||
                State.Turn.LastRoll == null)
            {
                return false;
            }

            int playerIndex = State.Turn.CurrentPlayerIndex;
            PlayerState player = State.MutablePlayers[playerIndex];
            int fromIndex = player.BoardPosition;
            int spaces = State.Turn.LastRoll.Total;
            int absoluteDestination = fromIndex + spaces;
            int completedLaps = absoluteDestination / _boardTileCount;
            int destinationIndex = absoluteDestination % _boardTileCount;
            int cashAward = checked(completedLaps * _passStartCash);

            player.MoveTo(destinationIndex);
            player.AddCash(cashAward);
            movement = new PlayerMovement(
                playerIndex,
                fromIndex,
                destinationIndex,
                spaces,
                completedLaps,
                cashAward);
            State.Turn.Phase = TurnPhase.Moving;
            return true;
        }

        public bool TryCompleteMovement()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.Moving)
            {
                return false;
            }

            PlayerState player = State.Players[State.Turn.CurrentPlayerIndex];
            if (_propertiesByTileIndex.TryGetValue(
                    player.BoardPosition,
                    out PropertyState property) &&
                !property.IsOwned)
            {
                State.Turn.BeginPropertyDecision(property.TileIndex);
            }
            else
            {
                State.Turn.CompleteLanding();
            }

            return true;
        }

        public PropertyPurchaseResult TryPurchaseCurrentProperty()
        {
            PropertyPurchaseOffer offer = CurrentPurchaseOffer;
            if (offer == null)
            {
                return PropertyPurchaseResult.NotAwaitingDecision;
            }

            PropertyState property = _propertiesByTileIndex[offer.TileIndex];
            if (property.IsOwned)
            {
                State.Turn.CompleteLanding();
                return PropertyPurchaseResult.AlreadyOwned;
            }

            PlayerState player = State.MutablePlayers[offer.PlayerIndex];
            if (!player.TrySpendCash(offer.PurchasePrice))
            {
                return PropertyPurchaseResult.InsufficientCash;
            }

            property.TryAssignOwner(offer.PlayerIndex);
            State.Turn.CompleteLanding();
            return PropertyPurchaseResult.Success;
        }

        public bool TryDeclineCurrentProperty()
        {
            if (CurrentPurchaseOffer == null)
            {
                return false;
            }

            State.Turn.CompleteLanding();
            return true;
        }

        public bool TryGetPropertyAt(int tileIndex, out PropertyState property)
        {
            return _propertiesByTileIndex.TryGetValue(tileIndex, out property);
        }

        private List<PlayerState> CreatePlayers(int count)
        {
            List<PlayerState> players = new List<PlayerState>(count);
            for (int index = 0; index < count; index++)
            {
                players.Add(CreatePlayer(index));
            }

            return players;
        }

        private PlayerState CreatePlayer(int index)
        {
            int number = index + 1;
            return new PlayerState($"local-player-{number}", $"PLAYER {number}", _startingCash);
        }

        private static string CreateLocalMatchId()
        {
            return $"local-{Guid.NewGuid():N}";
        }

        private static int GetBoardTileCount(BoardSnapshot board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            return board.Tiles.Count;
        }

        private static List<PropertyState> CreatePropertyStates(BoardSnapshot board)
        {
            List<PropertyState> properties = new List<PropertyState>();
            if (board == null)
            {
                return properties;
            }

            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                switch (tile.Kind)
                {
                    case BoardTileKind.City:
                        CitySnapshot city = FindById(board.Cities, tile.ContentId);
                        if (city != null)
                        {
                            properties.Add(new PropertyState(
                                tile.Index,
                                tile.Kind,
                                city.CityId,
                                city.DisplayName,
                                city.PurchasePrice));
                        }

                        break;
                    case BoardTileKind.Airport:
                        AirportSnapshot airport = FindById(board.Airports, tile.ContentId);
                        if (airport != null)
                        {
                            properties.Add(new PropertyState(
                                tile.Index,
                                tile.Kind,
                                airport.AirportId,
                                airport.DisplayName,
                                airport.PurchasePrice));
                        }

                        break;
                    case BoardTileKind.Utility:
                        UtilitySnapshot utility = FindById(board.Utilities, tile.ContentId);
                        if (utility != null)
                        {
                            properties.Add(new PropertyState(
                                tile.Index,
                                tile.Kind,
                                utility.UtilityId,
                                utility.DisplayName,
                                utility.PurchasePrice));
                        }

                        break;
                }
            }

            return properties;
        }

        private static CitySnapshot FindById(
            IReadOnlyList<CitySnapshot> cities,
            string contentId)
        {
            foreach (CitySnapshot city in cities)
            {
                if (city != null && string.Equals(city.CityId, contentId, StringComparison.Ordinal))
                {
                    return city;
                }
            }

            return null;
        }

        private static AirportSnapshot FindById(
            IReadOnlyList<AirportSnapshot> airports,
            string contentId)
        {
            foreach (AirportSnapshot airport in airports)
            {
                if (airport != null &&
                    string.Equals(airport.AirportId, contentId, StringComparison.Ordinal))
                {
                    return airport;
                }
            }

            return null;
        }

        private static UtilitySnapshot FindById(
            IReadOnlyList<UtilitySnapshot> utilities,
            string contentId)
        {
            foreach (UtilitySnapshot utility in utilities)
            {
                if (utility != null &&
                    string.Equals(utility.UtilityId, contentId, StringComparison.Ordinal))
                {
                    return utility;
                }
            }

            return null;
        }
    }
}
