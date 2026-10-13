using System;
using System.Collections.Generic;
using Ropoly.Core.Board;
using Ropoly.Core.Time;

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
        private readonly bool _mortgageEnabled;
        private readonly int _turnDurationSeconds;
        private readonly int _disconnectGraceSeconds;
        private readonly IClock _clock;
        private readonly int _fullSetRentMultiplier;
        private readonly Dictionary<int, PropertyState> _propertiesByTileIndex;
        private readonly Dictionary<string, List<PropertyState>> _propertiesByCountryId;
        private readonly Dictionary<string, CountrySnapshot> _countriesById;

        public LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            int boardTileCount = 40,
            int passStartCash = 200,
            bool mortgageEnabled = true,
            int turnDurationSeconds = 60,
            int disconnectGraceSeconds = 60,
            IClock clock = null,
            int fullSetRentMultiplier = 2)
            : this(
                playerCount,
                startingCash,
                availableCreatureIds,
                boardTileCount,
                passStartCash,
                mortgageEnabled,
                board: null,
                turnDurationSeconds,
                disconnectGraceSeconds,
                clock,
                fullSetRentMultiplier)
        {
        }

        public LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            BoardSnapshot board,
            int passStartCash = 200,
            bool mortgageEnabled = true,
            int turnDurationSeconds = 60,
            int disconnectGraceSeconds = 60,
            IClock clock = null,
            int fullSetRentMultiplier = 2)
            : this(
                playerCount,
                startingCash,
                availableCreatureIds,
                GetBoardTileCount(board),
                passStartCash,
                mortgageEnabled,
                board,
                turnDurationSeconds,
                disconnectGraceSeconds,
                clock,
                fullSetRentMultiplier)
        {
        }

        private LocalMatchSession(
            int playerCount,
            int startingCash,
            IEnumerable<string> availableCreatureIds,
            int boardTileCount,
            int passStartCash,
            bool mortgageEnabled,
            BoardSnapshot board,
            int turnDurationSeconds,
            int disconnectGraceSeconds,
            IClock clock,
            int fullSetRentMultiplier)
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

            if (turnDurationSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(turnDurationSeconds));
            }

            if (disconnectGraceSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(disconnectGraceSeconds));
            }

            if (fullSetRentMultiplier < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(fullSetRentMultiplier));
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
            _mortgageEnabled = mortgageEnabled;
            _turnDurationSeconds = turnDurationSeconds;
            _disconnectGraceSeconds = disconnectGraceSeconds;
            _clock = clock ?? SystemClock.Instance;
            _fullSetRentMultiplier = fullSetRentMultiplier;
            _propertiesByCountryId = new Dictionary<string, List<PropertyState>>(StringComparer.Ordinal);
            _countriesById = new Dictionary<string, CountrySnapshot>(StringComparer.Ordinal);
            if (board?.Countries != null)
            {
                foreach (CountrySnapshot country in board.Countries)
                {
                    if (country != null && !string.IsNullOrEmpty(country.CountryId))
                    {
                        _countriesById[country.CountryId] = country;
                        _propertiesByCountryId[country.CountryId] = new List<PropertyState>();
                    }
                }
            }

            List<PropertyState> properties = CreatePropertyStates(board);
            State = new MatchState(
                CreateLocalMatchId(),
                CreatePlayers(playerCount),
                properties);
            _propertiesByTileIndex = new Dictionary<int, PropertyState>(properties.Count);
            foreach (PropertyState property in properties)
            {
                _propertiesByTileIndex.Add(property.TileIndex, property);
                if (!string.IsNullOrEmpty(property.CountryId))
                {
                    if (!_propertiesByCountryId.TryGetValue(property.CountryId, out List<PropertyState> list))
                    {
                        list = new List<PropertyState>();
                        _propertiesByCountryId[property.CountryId] = list;
                    }

                    list.Add(property);
                }
            }
        }

        public MatchState State { get; }

        public bool MortgageEnabled => _mortgageEnabled;

        public IClock Clock => _clock;

        public int TurnDurationSeconds => _turnDurationSeconds;

        public int DisconnectGraceSeconds => _disconnectGraceSeconds;
        public int FullSetRentMultiplier => _fullSetRentMultiplier;

        public TimeSpan? CurrentTurnRemainingTime => State.Turn?.GetRemainingTime(_clock.UtcNow);

        public TimeSpan? GetPlayerDisconnectRemainingTime(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return null;
            }

            PlayerState player = State.Players[playerIndex];
            if (!player.IsDisconnected || !player.DisconnectDeadline.HasValue)
            {
                return null;
            }

            TimeSpan remaining = player.DisconnectDeadline.Value - _clock.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        public event Action<int, EliminationReason> PlayerEliminated;

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
            ResetTurnDeadline();
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
            ResetTurnDeadline();
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

            int nextPlayerIndex = FindNextActivePlayer(State.Turn.CurrentPlayerIndex);
            if (nextPlayerIndex < 0)
            {
                return false;
            }

            State.Turn.AdvanceTo(nextPlayerIndex);
            ResetTurnDeadline();
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
            ResetTurnDeadline();
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
                    out PropertyState property))
            {
                if (!property.IsOwned)
                {
                    State.Turn.BeginPropertyDecision(property.TileIndex);
                }
                else if (property.OwnerPlayerIndex == State.Turn.CurrentPlayerIndex)
                {
                    State.Turn.CompleteLanding();
                }
                else
                {
                    ResolveRent(property);
                }
            }
            else
            {
                State.Turn.CompleteLanding();
            }

            ResetTurnDeadline();
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
                ResetTurnDeadline();
                return PropertyPurchaseResult.AlreadyOwned;
            }

            PlayerState player = State.MutablePlayers[offer.PlayerIndex];
            if (!player.TrySpendCash(offer.PurchasePrice))
            {
                return PropertyPurchaseResult.InsufficientCash;
            }

            property.TryAssignOwner(offer.PlayerIndex);
            State.Turn.CompleteLanding();
            ResetTurnDeadline();
            return PropertyPurchaseResult.Success;
        }

        public bool TryDeclineCurrentProperty()
        {
            if (CurrentPurchaseOffer == null)
            {
                return false;
            }

            State.Turn.CompleteLanding();
            ResetTurnDeadline();
            return true;
        }

        public RentPaymentResult TryPayCurrentDebt()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.DebtResolution ||
                State.Turn.RentPayment == null ||
                State.Turn.RentPayment.IsPaid)
            {
                return RentPaymentResult.NotAwaitingPayment;
            }

            RentPayment payment = State.Turn.RentPayment;
            if (!TryTransferCash(
                    payment.PayerPlayerIndex,
                    payment.OwnerPlayerIndex,
                    payment.Amount))
            {
                return RentPaymentResult.InsufficientCash;
            }

            payment.MarkPaid();
            State.Turn.CompleteDebtResolution();
            ResetTurnDeadline();
            return RentPaymentResult.Success;
        }

        public IReadOnlyList<PropertyState> GetPropertiesOwnedBy(int playerIndex)
        {
            List<PropertyState> owned = new List<PropertyState>();
            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return owned;
            }

            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex)
                {
                    owned.Add(property);
                }
            }

            return owned;
        }

        public AssetLiquidationResult TryMortgageProperty(int tileIndex)
        {
            if (!TryGetDebtProperty(tileIndex, out PropertyState property, out int playerIndex))
            {
                return GetDebtPropertyFailure(tileIndex, property);
            }

            if (!_mortgageEnabled)
            {
                return AssetLiquidationResult.MortgageDisabled;
            }

            if (property.IsMortgaged)
            {
                return AssetLiquidationResult.AlreadyMortgaged;
            }

            if (property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                HasUpgradesInCountrySet(property.CountryId))
            {
                return AssetLiquidationResult.HasUpgradedPropertiesInSet;
            }

            if (!property.TryMortgage(playerIndex))
            {
                return AssetLiquidationResult.NoLiquidationValue;
            }

            State.MutablePlayers[playerIndex].AddCash(property.MortgageValue);
            ResetTurnDeadline();
            return AssetLiquidationResult.Success;
        }

        public AssetLiquidationResult TrySellPropertyToBank(int tileIndex)
        {
            if (!TryGetDebtProperty(tileIndex, out PropertyState property, out int playerIndex))
            {
                return GetDebtPropertyFailure(tileIndex, property);
            }

            if (property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                HasUpgradesInCountrySet(property.CountryId))
            {
                return AssetLiquidationResult.HasUpgradedPropertiesInSet;
            }

            if (!property.TrySellToBank(playerIndex, out int proceeds))
            {
                return AssetLiquidationResult.NoLiquidationValue;
            }

            State.MutablePlayers[playerIndex].AddCash(proceeds);
            ResetTurnDeadline();
            return AssetLiquidationResult.Success;
        }

        public bool TryDeclareCurrentPlayerBankruptcy()
        {
            if (!IsResolvingDebt())
            {
                return false;
            }

            return EliminatePlayer(State.Turn.CurrentPlayerIndex, EliminationReason.Bankruptcy);
        }

        public bool TryDisconnectPlayer(int playerIndex)
        {
            if (State.Phase != MatchPhase.InProgress ||
                playerIndex < 0 ||
                playerIndex >= State.Players.Count)
            {
                return false;
            }

            DateTimeOffset deadline = _clock.UtcNow + TimeSpan.FromSeconds(_disconnectGraceSeconds);
            return State.MutablePlayers[playerIndex].TryDisconnect(deadline);
        }

        public bool TryReconnectPlayer(int playerIndex)
        {
            if (State.Phase != MatchPhase.InProgress ||
                playerIndex < 0 ||
                playerIndex >= State.Players.Count)
            {
                return false;
            }

            return State.MutablePlayers[playerIndex].TryReconnect();
        }

        public void ResetCurrentPlayerActivity()
        {
            if (State.Phase == MatchPhase.InProgress && State.Turn != null)
            {
                ResetTurnDeadline();
            }
        }

        public bool Tick()
        {
            if (State.Phase != MatchPhase.InProgress)
            {
                return false;
            }

            DateTimeOffset now = _clock.UtcNow;
            bool stateChanged = false;

            for (int index = 0; index < State.Players.Count; index++)
            {
                PlayerState player = State.Players[index];
                if (player.IsDisconnected &&
                    player.DisconnectDeadline.HasValue &&
                    player.DisconnectDeadline.Value <= now)
                {
                    EliminatePlayer(index, EliminationReason.DisconnectTimeout);
                    stateChanged = true;

                    if (State.Phase == MatchPhase.Finished)
                    {
                        return true;
                    }
                }
            }

            if (State.Phase == MatchPhase.InProgress &&
                State.Turn != null &&
                State.Turn.Deadline.HasValue &&
                State.Turn.Deadline.Value <= now)
            {
                int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
                EliminationReason reason = State.Turn.Phase == TurnPhase.DebtResolution
                    ? EliminationReason.DebtTimeout
                    : EliminationReason.InactivityTimeout;

                EliminatePlayer(currentPlayerIndex, reason);
                stateChanged = true;
            }

            return stateChanged;
        }

        private bool EliminatePlayer(int playerIndex, EliminationReason reason)
        {
            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return false;
            }

            PlayerState player = State.MutablePlayers[playerIndex];
            if (player.IsEliminated)
            {
                return false;
            }

            if (State.Turn?.RentPayment != null && State.Turn.RentPayment.PayerPlayerIndex == playerIndex)
            {
                State.Turn.RentPayment.CancelByBankruptcy();
            }

            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex)
                {
                    property.ReleaseToBank();
                }
            }

            player.Eliminate();
            PlayerEliminated?.Invoke(playerIndex, reason);

            int winnerPlayerIndex = GetSoleActivePlayerIndex();
            if (winnerPlayerIndex >= 0)
            {
                State.WinnerPlayerIndex = winnerPlayerIndex;
                State.Phase = MatchPhase.Finished;
                return true;
            }

            if (State.Turn != null && State.Turn.CurrentPlayerIndex == playerIndex)
            {
                int nextPlayerIndex = FindNextActivePlayer(playerIndex);
                if (nextPlayerIndex >= 0)
                {
                    State.Turn.AdvanceTo(nextPlayerIndex);
                    ResetTurnDeadline();
                }
            }

            return true;
        }

        private void ResetTurnDeadline()
        {
            if (State.Turn != null)
            {
                State.Turn.Deadline = _turnDurationSeconds > 0
                    ? _clock.UtcNow + TimeSpan.FromSeconds(_turnDurationSeconds)
                    : null;
            }
        }

        public bool TryGetPropertyAt(int tileIndex, out PropertyState property)
        {
            return _propertiesByTileIndex.TryGetValue(tileIndex, out property);
        }

        public bool IsCountrySetFullyOwned(string countryId, int playerIndex)
        {
            if (string.IsNullOrEmpty(countryId) ||
                playerIndex < 0 ||
                playerIndex >= State.Players.Count)
            {
                return false;
            }

            if (!_propertiesByCountryId.TryGetValue(countryId, out List<PropertyState> properties) ||
                properties.Count == 0)
            {
                return false;
            }

            if (_countriesById.TryGetValue(countryId, out CountrySnapshot country))
            {
                if (properties.Count < country.CityIds.Count)
                {
                    return false;
                }
            }

            foreach (PropertyState property in properties)
            {
                if (property.OwnerPlayerIndex != playerIndex)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasMortgagedPropertyInCountrySet(string countryId)
        {
            if (string.IsNullOrEmpty(countryId) ||
                !_propertiesByCountryId.TryGetValue(countryId, out List<PropertyState> properties))
            {
                return false;
            }

            foreach (PropertyState property in properties)
            {
                if (property.IsMortgaged)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasUpgradesInCountrySet(string countryId)
        {
            if (string.IsNullOrEmpty(countryId) ||
                !_propertiesByCountryId.TryGetValue(countryId, out List<PropertyState> properties))
            {
                return false;
            }

            foreach (PropertyState property in properties)
            {
                if (property.UpgradeLevel > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<PropertyState> GetPropertiesInCountry(string countryId)
        {
            if (!string.IsNullOrEmpty(countryId) &&
                _propertiesByCountryId.TryGetValue(countryId, out List<PropertyState> properties))
            {
                return properties;
            }

            return Array.Empty<PropertyState>();
        }

        public bool CanUpgradeProperty(int tileIndex, out PropertyUpgradeResult result)
        {
            result = PropertyUpgradeResult.NotAllowedInCurrentPhase;
            if (State.Phase != MatchPhase.InProgress || State.Turn == null)
            {
                return false;
            }

            if (State.Turn.Phase == TurnPhase.DebtResolution ||
                State.Turn.Phase == TurnPhase.Moving)
            {
                return false;
            }

            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState property))
            {
                result = PropertyUpgradeResult.PropertyNotFound;
                return false;
            }

            if (property.Kind != BoardTileKind.City || string.IsNullOrEmpty(property.CountryId))
            {
                result = PropertyUpgradeResult.NotACity;
                return false;
            }

            if (property.OwnerPlayerIndex != currentPlayerIndex)
            {
                result = PropertyUpgradeResult.NotOwnedByPlayer;
                return false;
            }

            if (property.IsMortgaged)
            {
                result = PropertyUpgradeResult.IsMortgaged;
                return false;
            }

            if (!IsCountrySetFullyOwned(property.CountryId, currentPlayerIndex))
            {
                result = PropertyUpgradeResult.CountrySetIncomplete;
                return false;
            }

            if (HasMortgagedPropertyInCountrySet(property.CountryId))
            {
                result = PropertyUpgradeResult.HasMortgagedPropertyInSet;
                return false;
            }

            if (property.UpgradeLevel >= BoardContentLimits.CityRentLevelCount - 1)
            {
                result = PropertyUpgradeResult.MaximumLevelReached;
                return false;
            }

            List<PropertyState> sisters = _propertiesByCountryId[property.CountryId];
            foreach (PropertyState sister in sisters)
            {
                if (sister.TileIndex != property.TileIndex && sister.UpgradeLevel < property.UpgradeLevel)
                {
                    result = PropertyUpgradeResult.EvenBuildRuleViolation;
                    return false;
                }
            }

            if (State.Players[currentPlayerIndex].Cash < property.UpgradeCost)
            {
                result = PropertyUpgradeResult.InsufficientCash;
                return false;
            }

            result = PropertyUpgradeResult.Success;
            return true;
        }

        public PropertyUpgradeResult TryUpgradeProperty(int tileIndex)
        {
            if (!CanUpgradeProperty(tileIndex, out PropertyUpgradeResult result))
            {
                return result;
            }

            PropertyState property = _propertiesByTileIndex[tileIndex];
            PlayerState player = State.MutablePlayers[State.Turn.CurrentPlayerIndex];
            if (!player.TrySpendCash(property.UpgradeCost))
            {
                return PropertyUpgradeResult.InsufficientCash;
            }

            property.TryUpgrade();
            ResetTurnDeadline();
            return PropertyUpgradeResult.Success;
        }

        public bool CanDowngradeProperty(int tileIndex, out PropertyDowngradeResult result)
        {
            result = PropertyDowngradeResult.NotAllowedInCurrentPhase;
            if (State.Phase != MatchPhase.InProgress || State.Turn == null)
            {
                return false;
            }

            if (State.Turn.Phase == TurnPhase.Moving)
            {
                return false;
            }

            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState property))
            {
                result = PropertyDowngradeResult.PropertyNotFound;
                return false;
            }

            if (property.Kind != BoardTileKind.City || string.IsNullOrEmpty(property.CountryId))
            {
                result = PropertyDowngradeResult.NotACity;
                return false;
            }

            if (property.OwnerPlayerIndex != currentPlayerIndex)
            {
                result = PropertyDowngradeResult.NotOwnedByPlayer;
                return false;
            }

            if (property.UpgradeLevel <= 0)
            {
                result = PropertyDowngradeResult.NoUpgradesToSell;
                return false;
            }

            if (_propertiesByCountryId.TryGetValue(property.CountryId, out List<PropertyState> sisters))
            {
                foreach (PropertyState sister in sisters)
                {
                    if (sister.TileIndex != property.TileIndex && sister.UpgradeLevel > property.UpgradeLevel)
                    {
                        result = PropertyDowngradeResult.EvenDowngradeRuleViolation;
                        return false;
                    }
                }
            }

            result = PropertyDowngradeResult.Success;
            return true;
        }

        public PropertyDowngradeResult TryDowngradeProperty(int tileIndex, out int refund)
        {
            refund = 0;
            if (!CanDowngradeProperty(tileIndex, out PropertyDowngradeResult result))
            {
                return result;
            }

            PropertyState property = _propertiesByTileIndex[tileIndex];
            refund = property.UpgradeCost / 2;
            property.TryDowngrade();
            State.MutablePlayers[State.Turn.CurrentPlayerIndex].AddCash(refund);
            ResetTurnDeadline();
            return PropertyDowngradeResult.Success;
        }

        public PropertyDowngradeResult TryDowngradeProperty(int tileIndex)
        {
            return TryDowngradeProperty(tileIndex, out _);
        }

        private bool TryGetDebtProperty(
            int tileIndex,
            out PropertyState property,
            out int playerIndex)
        {
            property = null;
            playerIndex = PropertyState.UnownedPlayerIndex;
            if (!IsResolvingDebt())
            {
                return false;
            }

            playerIndex = State.Turn.CurrentPlayerIndex;
            return _propertiesByTileIndex.TryGetValue(tileIndex, out property) &&
                   property.OwnerPlayerIndex == playerIndex;
        }

        private AssetLiquidationResult GetDebtPropertyFailure(
            int tileIndex,
            PropertyState property)
        {
            if (!IsResolvingDebt())
            {
                return AssetLiquidationResult.NotResolvingDebt;
            }

            return property == null && !_propertiesByTileIndex.ContainsKey(tileIndex)
                ? AssetLiquidationResult.PropertyNotFound
                : AssetLiquidationResult.NotOwnedByCurrentPlayer;
        }

        private bool IsResolvingDebt()
        {
            return State.Phase == MatchPhase.InProgress &&
                   State.Turn != null &&
                   State.Turn.Phase == TurnPhase.DebtResolution &&
                   State.Turn.RentPayment != null &&
                   !State.Turn.RentPayment.IsResolved;
        }

        private int FindNextActivePlayer(int currentPlayerIndex)
        {
            for (int offset = 1; offset <= State.Players.Count; offset++)
            {
                int candidate = (currentPlayerIndex + offset) % State.Players.Count;
                if (!State.Players[candidate].IsEliminated)
                {
                    return candidate;
                }
            }

            return PropertyState.UnownedPlayerIndex;
        }

        private int GetSoleActivePlayerIndex()
        {
            int activePlayerIndex = PropertyState.UnownedPlayerIndex;
            int activeCount = 0;
            for (int index = 0; index < State.Players.Count; index++)
            {
                if (State.Players[index].IsEliminated)
                {
                    continue;
                }

                activePlayerIndex = index;
                activeCount++;
            }

            return activeCount == 1
                ? activePlayerIndex
                : PropertyState.UnownedPlayerIndex;
        }

        private void ResolveRent(PropertyState property)
        {
            int payerPlayerIndex = State.Turn.CurrentPlayerIndex;
            int ownerPlayerIndex = property.OwnerPlayerIndex;
            int sameKindOwnedCount = CountOwnedProperties(ownerPlayerIndex, property.Kind);
            bool isFullCountrySetUnmortgaged =
                property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                IsCountrySetFullyOwned(property.CountryId, ownerPlayerIndex) &&
                !HasMortgagedPropertyInCountrySet(property.CountryId);

            int rentAmount = property.CalculateRent(
                sameKindOwnedCount,
                State.Turn.LastRoll.Total,
                isFullCountrySetUnmortgaged,
                _fullSetRentMultiplier);
            RentPayment payment = new RentPayment(
                property.TileIndex,
                property.PropertyId,
                property.DisplayName,
                payerPlayerIndex,
                ownerPlayerIndex,
                rentAmount);

            if (TryTransferCash(payerPlayerIndex, ownerPlayerIndex, rentAmount))
            {
                payment.MarkPaid();
            }

            State.Turn.RecordRentPayment(payment);
        }

        private int CountOwnedProperties(int ownerPlayerIndex, BoardTileKind kind)
        {
            int count = 0;
            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == ownerPlayerIndex && property.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryTransferCash(int payerPlayerIndex, int receiverPlayerIndex, int amount)
        {
            PlayerState payer = State.MutablePlayers[payerPlayerIndex];
            if (!payer.TrySpendCash(amount))
            {
                return false;
            }

            State.MutablePlayers[receiverPlayerIndex].AddCash(amount);
            return true;
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

            Dictionary<string, string> countryIdByCityId = new Dictionary<string, string>(StringComparer.Ordinal);
            if (board.Countries != null)
            {
                foreach (CountrySnapshot country in board.Countries)
                {
                    if (country?.CityIds != null)
                    {
                        foreach (string cityId in country.CityIds)
                        {
                            if (!string.IsNullOrEmpty(cityId))
                            {
                                countryIdByCityId[cityId] = country.CountryId;
                            }
                        }
                    }
                }
            }

            foreach (BoardTileSnapshot tile in board.Tiles)
            {
                switch (tile.Kind)
                {
                    case BoardTileKind.City:
                        CitySnapshot city = FindById(board.Cities, tile.ContentId);
                        if (city != null)
                        {
                            countryIdByCityId.TryGetValue(city.CityId, out string countryId);
                            properties.Add(new PropertyState(
                                tile.Index,
                                tile.Kind,
                                city.CityId,
                                city.DisplayName,
                                city.PurchasePrice,
                                city.MortgageValue,
                                city.RentByUpgradeLevel,
                                city.UpgradeCost,
                                countryId));
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
                                airport.PurchasePrice,
                                airport.MortgageValue,
                                airport.RentByOwnedCount));
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
                                utility.PurchasePrice,
                                utility.MortgageValue,
                                utility.RentMultiplierByOwnedCount));
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
