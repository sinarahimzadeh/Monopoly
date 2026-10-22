using System;
using System.Collections.Generic;
using Ropoly.Core.Auctions;
using Ropoly.Core.Board;
using Ropoly.Core.Cards;
using Ropoly.Core.Time;
using Ropoly.Core.Trading;

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
        private readonly bool _auctionsEnabled;
        private readonly int _auctionBidTimeoutSeconds;
        private readonly Dictionary<int, PropertyState> _propertiesByTileIndex;
        private readonly Dictionary<string, List<PropertyState>> _propertiesByCountryId;
        private readonly Dictionary<string, CountrySnapshot> _countriesById;
        private readonly Dictionary<int, SpecialSpaceSnapshot> _specialSpacesByTileIndex;
        private readonly Dictionary<string, CardDeck> _cardDecks;
        private readonly bool _vacationRewardEnabled;
        private readonly int _baseVacationReward;
        private int _vacationPool;

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
            int fullSetRentMultiplier = 2,
            bool vacationRewardEnabled = false,
            int vacationReward = 0,
            IReadOnlyDictionary<string, CardDeck> cardDecks = null,
            bool auctionsEnabled = false,
            int auctionBidTimeoutSeconds = 15)
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
                fullSetRentMultiplier,
                vacationRewardEnabled,
                vacationReward,
                cardDecks,
                auctionsEnabled,
                auctionBidTimeoutSeconds)
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
            int fullSetRentMultiplier = 2,
            bool vacationRewardEnabled = false,
            int vacationReward = 0,
            IReadOnlyDictionary<string, CardDeck> cardDecks = null,
            bool auctionsEnabled = false,
            int auctionBidTimeoutSeconds = 15)
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
                fullSetRentMultiplier,
                vacationRewardEnabled,
                vacationReward,
                cardDecks,
                auctionsEnabled,
                auctionBidTimeoutSeconds)
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
            int fullSetRentMultiplier,
            bool vacationRewardEnabled,
            int vacationReward,
            IReadOnlyDictionary<string, CardDeck> cardDecks,
            bool auctionsEnabled,
            int auctionBidTimeoutSeconds)
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
            _auctionsEnabled = auctionsEnabled;
            _auctionBidTimeoutSeconds = auctionBidTimeoutSeconds > 0 ? auctionBidTimeoutSeconds : 15;
            _vacationRewardEnabled = vacationRewardEnabled;
            _baseVacationReward = Math.Max(0, vacationReward);
            _vacationPool = _baseVacationReward;

            if (cardDecks != null)
            {
                _cardDecks = new Dictionary<string, CardDeck>(cardDecks, StringComparer.Ordinal);
            }
            else
            {
                _cardDecks = DefaultCardDecks.CreateDefaultDecks();
            }

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

            _specialSpacesByTileIndex = new Dictionary<int, SpecialSpaceSnapshot>();
            if (board != null && board.SpecialSpaces != null && board.Tiles != null)
            {
                Dictionary<string, SpecialSpaceSnapshot> specialById =
                    new Dictionary<string, SpecialSpaceSnapshot>(StringComparer.Ordinal);
                foreach (SpecialSpaceSnapshot special in board.SpecialSpaces)
                {
                    if (special != null && !string.IsNullOrEmpty(special.SpaceId))
                    {
                        specialById[special.SpaceId] = special;
                    }
                }

                foreach (BoardTileSnapshot tile in board.Tiles)
                {
                    if (tile.Kind == BoardTileKind.Special &&
                        !string.IsNullOrEmpty(tile.ContentId) &&
                        specialById.TryGetValue(tile.ContentId, out SpecialSpaceSnapshot snapshot))
                    {
                        _specialSpacesByTileIndex[tile.Index] = snapshot;
                    }
                }
            }
        }

        public MatchState State { get; }

        public bool MortgageEnabled => _mortgageEnabled;

        public IClock Clock => _clock;

        public int TurnDurationSeconds => _turnDurationSeconds;

        public bool VacationRewardEnabled => _vacationRewardEnabled;

        public int VacationPool => _vacationPool;

        public IReadOnlyDictionary<string, CardDeck> CardDecks => _cardDecks;

        public int DisconnectGraceSeconds => _disconnectGraceSeconds;
        public int FullSetRentMultiplier => _fullSetRentMultiplier;
        public bool AuctionsEnabled => _auctionsEnabled;
        public int AuctionBidTimeoutSeconds => _auctionBidTimeoutSeconds;
        public PropertyAuctionState ActiveAuction { get; private set; }

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
        public TradeOffer ActiveTradeOffer { get; private set; }
        public event Action<TradeOffer> TradeProposed;
        public event Action<TradeOffer> TradeCompleted;
        public event Action<TradeOffer> TradeRejected;
        public event Action<TradeOffer> TradeCancelled;
        public event Action<MatchResult> MatchEnded;
        public event Action<PropertyAuctionState> AuctionStarted;
        public event Action<PropertyAuctionState, int, int> AuctionBidPlaced;
        public event Action<PropertyAuctionState, int> AuctionPlayerPassed;
        public event Action<PropertyAuctionState> AuctionCompleted;
        private readonly List<PlayerEliminationRecord> _eliminations = new();
        public IReadOnlyList<PlayerEliminationRecord> Eliminations => _eliminations;

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

        public bool TryPayDetentionBail()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingRoll)
            {
                return false;
            }

            PlayerState player = State.MutablePlayers[State.Turn.CurrentPlayerIndex];
            if (!player.IsInDetention)
            {
                return false;
            }

            const int bailCost = 50;
            if (!player.TrySpendCash(bailCost))
            {
                return false;
            }

            if (_vacationRewardEnabled)
            {
                _vacationPool = checked(_vacationPool + bailCost);
            }

            player.ReleaseFromDetention();
            ResetTurnDeadline();
            return true;
        }

        public bool TryUseDetentionWaiver()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingRoll)
            {
                return false;
            }

            PlayerState player = State.MutablePlayers[State.Turn.CurrentPlayerIndex];
            if (!player.IsInDetention)
            {
                return false;
            }

            if (!player.TryUseDetentionWaiver())
            {
                return false;
            }

            if (_cardDecks != null && _cardDecks.TryGetValue(DefaultCardDecks.WorldEventDeckId, out CardDeck deck))
            {
                CardSnapshot waiver = new CardSnapshot(
                    "returned-waiver",
                    DefaultCardDecks.WorldEventDeckId,
                    "Consular Release Order",
                    "Get Out of Detention Free.",
                    CardEffectKind.DetentionWaiver);
                deck.ReturnWaiverCard(waiver);
            }

            player.ReleaseFromDetention();
            ResetTurnDeadline();
            return true;
        }

        public void SendPlayerToDetention(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return;
            }

            PlayerState player = State.MutablePlayers[playerIndex];
            player.SendToDetention(detentionTileIndex: 10);
            if (State.Turn != null && State.Turn.CurrentPlayerIndex == playerIndex)
            {
                State.Turn.ConsecutiveDoublesCount = 0;
                State.Turn.SentToDetentionThisTurn = true;
            }
        }

        public bool TryRollDice(IDiceRollSource rollSource, out DiceRoll roll)
        {
            roll = null;
            if (rollSource == null ||
                State.Phase != MatchPhase.InProgress ||
                State.Turn == null)
            {
                return false;
            }

            bool isAwaitingRoll = State.Turn.Phase == TurnPhase.AwaitingRoll;
            bool isRollingAgain = State.Turn.CanRollAgain;
            if (!isAwaitingRoll && !isRollingAgain)
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

            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            PlayerState player = State.MutablePlayers[currentPlayerIndex];

            if (player.IsInDetention)
            {
                if (roll.IsDouble)
                {
                    player.ReleaseFromDetention();
                    State.Turn.RolledDoublesToEscapeDetention = true;
                    State.Turn.ConsecutiveDoublesCount = 0;
                    State.Turn.Phase = TurnPhase.AwaitingMovement;
                }
                else
                {
                    player.IncrementDetentionTurns();
                    if (player.DetentionTurns < 3)
                    {
                        State.Turn.Phase = TurnPhase.AwaitingTurnEnd;
                    }
                    else
                    {
                        const int forcedBailAmount = 50;
                        if (player.TrySpendCash(forcedBailAmount))
                        {
                            if (_vacationRewardEnabled)
                            {
                                _vacationPool = checked(_vacationPool + forcedBailAmount);
                            }

                            player.ReleaseFromDetention();
                            State.Turn.Phase = TurnPhase.AwaitingMovement;
                        }
                        else
                        {
                            RentPayment payment = new RentPayment(
                                tileIndex: 10,
                                propertyId: "space-detention",
                                propertyDisplayName: "Detention Bail",
                                payerPlayerIndex: currentPlayerIndex,
                                ownerPlayerIndex: PropertyState.UnownedPlayerIndex,
                                amount: forcedBailAmount);
                            State.Turn.RecordRentPayment(payment);
                        }
                    }
                }
            }
            else
            {
                if (roll.IsDouble)
                {
                    State.Turn.ConsecutiveDoublesCount++;
                    if (State.Turn.ConsecutiveDoublesCount >= 3)
                    {
                        SendPlayerToDetention(currentPlayerIndex);
                        State.Turn.ConsecutiveDoublesCount = 0;
                        State.Turn.Phase = TurnPhase.AwaitingTurnEnd;
                        ResetTurnDeadline();
                        return true;
                    }
                }
                else
                {
                    State.Turn.ConsecutiveDoublesCount = 0;
                }

                State.Turn.Phase = TurnPhase.AwaitingMovement;
            }

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
            int currentTileIndex = player.BoardPosition;

            if (_propertiesByTileIndex.TryGetValue(
                    currentTileIndex,
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
            else if (_specialSpacesByTileIndex.TryGetValue(
                    currentTileIndex,
                    out SpecialSpaceSnapshot specialSpace))
            {
                ResolveSpecialSpace(specialSpace);
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

            if (_auctionsEnabled)
            {
                return TryStartAuctionForCurrentProperty();
            }

            State.Turn.CompleteLanding();
            ResetTurnDeadline();
            return true;
        }

        public bool TryStartAuctionForCurrentProperty()
        {
            if (State.Phase != MatchPhase.InProgress ||
                State.Turn == null ||
                State.Turn.Phase != TurnPhase.AwaitingPropertyDecision ||
                !_propertiesByTileIndex.TryGetValue(
                    State.Turn.PendingPropertyTileIndex,
                    out PropertyState property) ||
                property.IsOwned)
            {
                return false;
            }

            int tileIndex = property.TileIndex;
            State.Turn.BeginAuction(tileIndex);
            DateTimeOffset deadline = _clock.UtcNow + TimeSpan.FromSeconds(_auctionBidTimeoutSeconds);
            ActiveAuction = new PropertyAuctionState(
                tileIndex,
                property.PropertyId,
                property.DisplayName,
                property.PurchasePrice,
                _auctionBidTimeoutSeconds,
                deadline);

            AuctionStarted?.Invoke(ActiveAuction);
            return true;
        }

        public bool TryPlaceAuctionBid(int playerIndex, int amount, out AuctionBidResult result)
        {
            if (ActiveAuction == null || !ActiveAuction.IsActive || State.Turn?.Phase != TurnPhase.Auction)
            {
                result = AuctionBidResult.NoActiveAuction;
                return false;
            }

            if (playerIndex < 0 || playerIndex >= State.Players.Count || State.Players[playerIndex].IsEliminated)
            {
                result = AuctionBidResult.PlayerEliminated;
                return false;
            }

            if (ActiveAuction.DroppedOutPlayerIndices.Contains(playerIndex))
            {
                result = AuctionBidResult.PlayerDroppedOut;
                return false;
            }

            if (ActiveAuction.HighestBidderPlayerIndex == playerIndex)
            {
                result = AuctionBidResult.AlreadyHighestBidder;
                return false;
            }

            if (amount <= ActiveAuction.CurrentBid || amount < 1)
            {
                result = AuctionBidResult.BidTooLow;
                return false;
            }

            PlayerState player = State.Players[playerIndex];
            if (player.Cash < amount)
            {
                result = AuctionBidResult.InsufficientCash;
                return false;
            }

            ActiveAuction.CurrentBid = amount;
            ActiveAuction.HighestBidderPlayerIndex = playerIndex;
            ActiveAuction.Deadline = _clock.UtcNow + TimeSpan.FromSeconds(ActiveAuction.TimeoutDurationSeconds);

            AuctionBidPlaced?.Invoke(ActiveAuction, playerIndex, amount);
            result = AuctionBidResult.Success;
            return true;
        }

        public bool TryPlaceAuctionBid(int playerIndex, int amount)
        {
            return TryPlaceAuctionBid(playerIndex, amount, out _);
        }

        public bool TryPassAuction(int playerIndex, out AuctionPassResult result)
        {
            if (ActiveAuction == null || !ActiveAuction.IsActive || State.Turn?.Phase != TurnPhase.Auction)
            {
                result = AuctionPassResult.NoActiveAuction;
                return false;
            }

            if (playerIndex < 0 || playerIndex >= State.Players.Count || State.Players[playerIndex].IsEliminated)
            {
                result = AuctionPassResult.PlayerEliminated;
                return false;
            }

            if (ActiveAuction.DroppedOutPlayerIndices.Contains(playerIndex))
            {
                result = AuctionPassResult.AlreadyDroppedOut;
                return false;
            }

            if (ActiveAuction.HighestBidderPlayerIndex == playerIndex)
            {
                result = AuctionPassResult.HighestBidderCannotPass;
                return false;
            }

            ActiveAuction.DroppedOutPlayerIndices.Add(playerIndex);
            AuctionPlayerPassed?.Invoke(ActiveAuction, playerIndex);

            CheckAuctionAutoConclusion();

            result = AuctionPassResult.Success;
            return true;
        }

        public bool TryPassAuction(int playerIndex)
        {
            return TryPassAuction(playerIndex, out _);
        }

        private void CheckAuctionAutoConclusion()
        {
            if (ActiveAuction == null || !ActiveAuction.IsActive)
            {
                return;
            }

            int activeCount = 0;
            for (int i = 0; i < State.Players.Count; i++)
            {
                if (!State.Players[i].IsEliminated && !ActiveAuction.DroppedOutPlayerIndices.Contains(i))
                {
                    activeCount++;
                }
            }

            if (ActiveAuction.HighestBidderPlayerIndex >= 0)
            {
                if (activeCount <= 1)
                {
                    ConcludeAuction();
                }
            }
            else
            {
                if (activeCount == 0)
                {
                    ConcludeAuction();
                }
            }
        }

        public void ConcludeAuction()
        {
            if (ActiveAuction == null || !ActiveAuction.IsActive)
            {
                return;
            }

            PropertyAuctionState auction = ActiveAuction;
            int winner = auction.HighestBidderPlayerIndex;
            int winningBid = auction.CurrentBid;

            if (winner >= 0 && winner < State.Players.Count && winningBid > 0)
            {
                PlayerState winnerPlayer = State.MutablePlayers[winner];
                winnerPlayer.TrySpendCash(winningBid);
                if (_propertiesByTileIndex.TryGetValue(auction.TileIndex, out PropertyState prop))
                {
                    prop.TryAssignOwner(winner);
                }

                auction.SetResult(winner, winningBid);
            }
            else
            {
                auction.SetResult(-1, 0);
            }

            if (State.Turn != null && State.Turn.Phase == TurnPhase.Auction)
            {
                State.Turn.CompleteAuction();
                ResetTurnDeadline();
            }

            AuctionCompleted?.Invoke(auction);
        }

        public void ApplyAuthoritativeAuctionStarted(int tileIndex, int timeoutSeconds)
        {
            if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState property))
            {
                return;
            }

            State.Turn?.BeginAuction(tileIndex);
            int duration = timeoutSeconds > 0 ? timeoutSeconds : _auctionBidTimeoutSeconds;
            DateTimeOffset deadline = _clock.UtcNow + TimeSpan.FromSeconds(duration);
            ActiveAuction = new PropertyAuctionState(
                tileIndex,
                property.PropertyId,
                property.DisplayName,
                property.PurchasePrice,
                duration,
                deadline);

            AuctionStarted?.Invoke(ActiveAuction);
        }

        public void ApplyAuthoritativeAuctionBid(int bidderPlayerIndex, int amount)
        {
            if (ActiveAuction == null)
            {
                return;
            }

            ActiveAuction.CurrentBid = amount;
            ActiveAuction.HighestBidderPlayerIndex = bidderPlayerIndex;
            ActiveAuction.Deadline = _clock.UtcNow + TimeSpan.FromSeconds(ActiveAuction.TimeoutDurationSeconds);
            AuctionBidPlaced?.Invoke(ActiveAuction, bidderPlayerIndex, amount);
        }

        public void ApplyAuthoritativeAuctionPass(int playerIndex)
        {
            if (ActiveAuction == null)
            {
                return;
            }

            ActiveAuction.DroppedOutPlayerIndices.Add(playerIndex);
            AuctionPlayerPassed?.Invoke(ActiveAuction, playerIndex);
        }

        public void ApplyAuthoritativeAuctionCompleted(int winnerPlayerIndex, int winningBid)
        {
            if (ActiveAuction == null)
            {
                return;
            }

            if (winnerPlayerIndex >= 0 && winnerPlayerIndex < State.Players.Count && winningBid > 0)
            {
                State.MutablePlayers[winnerPlayerIndex].TrySpendCash(winningBid);
                if (_propertiesByTileIndex.TryGetValue(ActiveAuction.TileIndex, out PropertyState prop))
                {
                    prop.TryAssignOwner(winnerPlayerIndex);
                }

                ActiveAuction.SetResult(winnerPlayerIndex, winningBid);
            }
            else
            {
                ActiveAuction.SetResult(-1, 0);
            }

            if (State.Turn != null && State.Turn.Phase == TurnPhase.Auction)
            {
                State.Turn.CompleteAuction();
                ResetTurnDeadline();
            }

            AuctionCompleted?.Invoke(ActiveAuction);
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
            if (string.Equals(payment.PropertyId, "space-detention", StringComparison.Ordinal))
            {
                State.MutablePlayers[payment.PayerPlayerIndex].ReleaseFromDetention();
                State.Turn.Phase = TurnPhase.AwaitingMovement;
            }
            else
            {
                State.Turn.CompleteDebtResolution();
            }

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

        public bool CanProposeTrade(TradeOffer offer, out TradeOfferResult result)
        {
            if (State.Phase != MatchPhase.InProgress)
            {
                result = TradeOfferResult.MatchFinished;
                return false;
            }

            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }

            if (offer.ProposerPlayerIndex == offer.TargetPlayerIndex ||
                offer.ProposerPlayerIndex < 0 || offer.ProposerPlayerIndex >= State.Players.Count ||
                offer.TargetPlayerIndex < 0 || offer.TargetPlayerIndex >= State.Players.Count)
            {
                result = TradeOfferResult.InvalidPlayers;
                return false;
            }

            PlayerState proposer = State.Players[offer.ProposerPlayerIndex];
            PlayerState target = State.Players[offer.TargetPlayerIndex];

            if (proposer.IsEliminated || target.IsEliminated)
            {
                result = TradeOfferResult.PlayerEliminated;
                return false;
            }

            bool hasOfferedItems = offer.OfferedCash > 0 ||
                                  (offer.OfferedPropertyTileIndices != null && offer.OfferedPropertyTileIndices.Count > 0) ||
                                  offer.OfferedWaivers > 0;
            bool hasRequestedItems = offer.RequestedCash > 0 ||
                                    (offer.RequestedPropertyTileIndices != null && offer.RequestedPropertyTileIndices.Count > 0) ||
                                    offer.RequestedWaivers > 0;

            if (!hasOfferedItems && !hasRequestedItems)
            {
                result = TradeOfferResult.EmptyTrade;
                return false;
            }

            if (proposer.Cash < offer.OfferedCash || target.Cash < offer.RequestedCash)
            {
                result = TradeOfferResult.InsufficientCash;
                return false;
            }

            if (proposer.HeldDetentionWaivers < offer.OfferedWaivers || target.HeldDetentionWaivers < offer.RequestedWaivers)
            {
                result = TradeOfferResult.InsufficientWaivers;
                return false;
            }

            if (offer.OfferedPropertyTileIndices != null)
            {
                foreach (int tileIndex in offer.OfferedPropertyTileIndices)
                {
                    if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState prop) ||
                        prop.OwnerPlayerIndex != offer.ProposerPlayerIndex)
                    {
                        result = TradeOfferResult.PropertyNotOwned;
                        return false;
                    }

                    if (prop.UpgradeLevel > 0 ||
                        (!string.IsNullOrEmpty(prop.CountryId) && HasUpgradesInCountrySet(prop.CountryId)))
                    {
                        result = TradeOfferResult.PropertyHasBuildings;
                        return false;
                    }
                }
            }

            if (offer.RequestedPropertyTileIndices != null)
            {
                foreach (int tileIndex in offer.RequestedPropertyTileIndices)
                {
                    if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState prop) ||
                        prop.OwnerPlayerIndex != offer.TargetPlayerIndex)
                    {
                        result = TradeOfferResult.PropertyNotOwned;
                        return false;
                    }

                    if (prop.UpgradeLevel > 0 ||
                        (!string.IsNullOrEmpty(prop.CountryId) && HasUpgradesInCountrySet(prop.CountryId)))
                    {
                        result = TradeOfferResult.PropertyHasBuildings;
                        return false;
                    }
                }
            }

            result = TradeOfferResult.Success;
            return true;
        }

        public bool TryProposeTrade(TradeOffer offer, out TradeOfferResult result)
        {
            if (!CanProposeTrade(offer, out result))
            {
                return false;
            }

            if (ActiveTradeOffer != null && ActiveTradeOffer.Status == TradeOfferStatus.Pending)
            {
                result = TradeOfferResult.TradeAlreadyActive;
                return false;
            }

            ActiveTradeOffer = offer;
            offer.Status = TradeOfferStatus.Pending;
            TradeProposed?.Invoke(offer);
            result = TradeOfferResult.Success;
            return true;
        }

        public bool TryAcceptActiveTrade(int respondingPlayerIndex, out TradeOfferResult result)
        {
            if (ActiveTradeOffer == null || ActiveTradeOffer.Status != TradeOfferStatus.Pending)
            {
                result = TradeOfferResult.NotAwaitingTrade;
                return false;
            }

            if (respondingPlayerIndex != ActiveTradeOffer.TargetPlayerIndex)
            {
                result = TradeOfferResult.Unauthorized;
                return false;
            }

            TradeOffer offer = ActiveTradeOffer;
            if (!CanProposeTrade(offer, out result))
            {
                ActiveTradeOffer.Status = TradeOfferStatus.Cancelled;
                ActiveTradeOffer = null;
                TradeCancelled?.Invoke(offer);
                return false;
            }

            if (offer.OfferedCash > 0)
            {
                TryTransferCash(offer.ProposerPlayerIndex, offer.TargetPlayerIndex, offer.OfferedCash);
            }

            if (offer.RequestedCash > 0)
            {
                TryTransferCash(offer.TargetPlayerIndex, offer.ProposerPlayerIndex, offer.RequestedCash);
            }

            if (offer.OfferedPropertyTileIndices != null)
            {
                foreach (int tileIndex in offer.OfferedPropertyTileIndices)
                {
                    _propertiesByTileIndex[tileIndex].TryTransferOwnership(offer.ProposerPlayerIndex, offer.TargetPlayerIndex);
                }
            }

            if (offer.RequestedPropertyTileIndices != null)
            {
                foreach (int tileIndex in offer.RequestedPropertyTileIndices)
                {
                    _propertiesByTileIndex[tileIndex].TryTransferOwnership(offer.TargetPlayerIndex, offer.ProposerPlayerIndex);
                }
            }

            if (offer.OfferedWaivers > 0)
            {
                State.MutablePlayers[offer.ProposerPlayerIndex].TryRemoveDetentionWaivers(offer.OfferedWaivers);
                State.MutablePlayers[offer.TargetPlayerIndex].AddDetentionWaivers(offer.OfferedWaivers);
            }

            if (offer.RequestedWaivers > 0)
            {
                State.MutablePlayers[offer.TargetPlayerIndex].TryRemoveDetentionWaivers(offer.RequestedWaivers);
                State.MutablePlayers[offer.ProposerPlayerIndex].AddDetentionWaivers(offer.RequestedWaivers);
            }

            offer.Status = TradeOfferStatus.Accepted;
            ActiveTradeOffer = null;
            TradeCompleted?.Invoke(offer);
            result = TradeOfferResult.Success;
            return true;
        }

        public bool TryRejectActiveTrade(int respondingPlayerIndex, out TradeOfferResult result)
        {
            if (ActiveTradeOffer == null || ActiveTradeOffer.Status != TradeOfferStatus.Pending)
            {
                result = TradeOfferResult.NotAwaitingTrade;
                return false;
            }

            if (respondingPlayerIndex != ActiveTradeOffer.TargetPlayerIndex)
            {
                result = TradeOfferResult.Unauthorized;
                return false;
            }

            TradeOffer offer = ActiveTradeOffer;
            offer.Status = TradeOfferStatus.Rejected;
            ActiveTradeOffer = null;
            TradeRejected?.Invoke(offer);
            result = TradeOfferResult.Success;
            return true;
        }

        public bool TryCancelActiveTrade(int requestingPlayerIndex, out TradeOfferResult result)
        {
            if (ActiveTradeOffer == null || ActiveTradeOffer.Status != TradeOfferStatus.Pending)
            {
                result = TradeOfferResult.NotAwaitingTrade;
                return false;
            }

            if (requestingPlayerIndex != ActiveTradeOffer.ProposerPlayerIndex)
            {
                result = TradeOfferResult.Unauthorized;
                return false;
            }

            TradeOffer offer = ActiveTradeOffer;
            offer.Status = TradeOfferStatus.Cancelled;
            ActiveTradeOffer = null;
            TradeCancelled?.Invoke(offer);
            result = TradeOfferResult.Success;
            return true;
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

        /// <summary>
        /// Applies an elimination already validated by an authoritative multiplayer host.
        /// Local gameplay continues to use the normal bankruptcy and timeout entry points.
        /// </summary>
        public bool ApplyAuthoritativeElimination(int playerIndex, EliminationReason reason)
        {
            if (State.Phase != MatchPhase.InProgress)
            {
                return false;
            }

            return EliminatePlayer(playerIndex, reason);
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

            if (ActiveAuction != null && ActiveAuction.IsActive && ActiveAuction.Deadline <= now)
            {
                ConcludeAuction();
                stateChanged = true;
            }

            if (State.Phase == MatchPhase.InProgress &&
                State.Turn != null &&
                State.Turn.Phase != TurnPhase.Auction &&
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

            int cashAtElim = player.Cash;
            int propsAtElim = 0;
            int netWorthAtElim = CalculatePlayerNetWorth(playerIndex);
            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex)
                {
                    propsAtElim++;
                }
            }

            int turnNumber = State.Turn?.TurnNumber ?? 1;
            _eliminations.Add(new PlayerEliminationRecord(
                playerIndex,
                reason,
                turnNumber,
                cashAtElim,
                propsAtElim,
                netWorthAtElim));

            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex)
                {
                    property.ReleaseToBank();
                }
            }

            player.Eliminate();
            if (ActiveTradeOffer != null && (ActiveTradeOffer.ProposerPlayerIndex == playerIndex || ActiveTradeOffer.TargetPlayerIndex == playerIndex))
            {
                TradeOffer cancelled = ActiveTradeOffer;
                cancelled.Status = TradeOfferStatus.Cancelled;
                ActiveTradeOffer = null;
                TradeCancelled?.Invoke(cancelled);
            }

            if (ActiveAuction != null && ActiveAuction.IsActive)
            {
                ActiveAuction.DroppedOutPlayerIndices.Add(playerIndex);
                if (ActiveAuction.HighestBidderPlayerIndex == playerIndex)
                {
                    ActiveAuction.HighestBidderPlayerIndex = -1;
                    ActiveAuction.CurrentBid = 0;
                }

                CheckAuctionAutoConclusion();
            }

            PlayerEliminated?.Invoke(playerIndex, reason);

            int winnerPlayerIndex = GetSoleActivePlayerIndex();
            if (winnerPlayerIndex >= 0)
            {
                State.WinnerPlayerIndex = winnerPlayerIndex;
                State.Phase = MatchPhase.Finished;
                MatchEnded?.Invoke(GetMatchResult());
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

        public int GetPropertyRent(int tileIndex, int diceTotal = 7)
        {
            if (!_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState property) || !property.IsOwned)
            {
                return 0;
            }

            int sameKindOwnedCount = CountOwnedProperties(property.OwnerPlayerIndex, property.Kind);
            bool isFullCountrySetUnmortgaged =
                property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                IsCountrySetFullyOwned(property.CountryId, property.OwnerPlayerIndex) &&
                !HasMortgagedPropertyInCountrySet(property.CountryId);

            return property.CalculateRent(
                sameKindOwnedCount,
                diceTotal,
                isFullCountrySetUnmortgaged,
                _fullSetRentMultiplier);
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

        private void ResolveRent(PropertyState property, int rentMultiplier = 1)
        {
            int payerPlayerIndex = State.Turn.CurrentPlayerIndex;
            int ownerPlayerIndex = property.OwnerPlayerIndex;
            int sameKindOwnedCount = CountOwnedProperties(ownerPlayerIndex, property.Kind);
            bool isFullCountrySetUnmortgaged =
                property.Kind == BoardTileKind.City &&
                !string.IsNullOrEmpty(property.CountryId) &&
                IsCountrySetFullyOwned(property.CountryId, ownerPlayerIndex) &&
                !HasMortgagedPropertyInCountrySet(property.CountryId);

            int baseRent = property.CalculateRent(
                sameKindOwnedCount,
                State.Turn.LastRoll != null ? State.Turn.LastRoll.Total : 7,
                isFullCountrySetUnmortgaged,
                _fullSetRentMultiplier);

            int rentAmount = checked(baseRent * rentMultiplier);
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

        private void ResolveSpecialSpace(SpecialSpaceSnapshot specialSpace)
        {
            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            PlayerState player = State.MutablePlayers[currentPlayerIndex];

            switch (specialSpace.SpecialKind)
            {
                case SpecialSpaceKind.Start:
                case SpecialSpaceKind.Detention:
                    State.Turn.CompleteLanding();
                    break;

                case SpecialSpaceKind.GoToDetention:
                    SendPlayerToDetention(currentPlayerIndex);
                    State.Turn.CompleteLanding();
                    break;

                case SpecialSpaceKind.Vacation:
                    if (_vacationRewardEnabled && _vacationPool > 0)
                    {
                        int award = _vacationPool;
                        player.AddCash(award);
                        State.Turn.VacationRewardAwarded = award;
                        _vacationPool = _baseVacationReward;
                    }

                    State.Turn.CompleteLanding();
                    break;

                case SpecialSpaceKind.Fee:
                    ResolveFee(specialSpace);
                    break;

                case SpecialSpaceKind.Event:
                    ResolveEvent(specialSpace);
                    break;

                default:
                    State.Turn.CompleteLanding();
                    break;
            }
        }

        private void ResolveFee(SpecialSpaceSnapshot specialSpace)
        {
            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            int amount = specialSpace.FeeAmount;

            RentPayment payment = new RentPayment(
                State.MutablePlayers[currentPlayerIndex].BoardPosition,
                specialSpace.SpaceId,
                specialSpace.DisplayName,
                currentPlayerIndex,
                PropertyState.UnownedPlayerIndex,
                amount);

            if (TryTransferCash(currentPlayerIndex, PropertyState.UnownedPlayerIndex, amount))
            {
                payment.MarkPaid();
                State.Turn.PaidFeeAmount = amount;
            }

            State.Turn.RecordRentPayment(payment);
        }

        private void ResolveEvent(SpecialSpaceSnapshot specialSpace)
        {
            string deckId = specialSpace.EventDeckId;
            if (string.IsNullOrEmpty(deckId) || !_cardDecks.TryGetValue(deckId, out CardDeck deck))
            {
                State.Turn.CompleteLanding();
                return;
            }

            CardSnapshot card = deck.Draw();
            if (card == null)
            {
                State.Turn.CompleteLanding();
                return;
            }

            State.Turn.DrawnCard = card;
            ExecuteCardEffect(card, deck);
        }

        private void ExecuteCardEffect(CardSnapshot card, CardDeck deck)
        {
            int currentPlayerIndex = State.Turn.CurrentPlayerIndex;
            PlayerState player = State.MutablePlayers[currentPlayerIndex];

            switch (card.EffectKind)
            {
                case CardEffectKind.CashBank:
                    if (card.Amount >= 0)
                    {
                        player.AddCash(card.Amount);
                        State.Turn.CompleteLanding();
                    }
                    else
                    {
                        int feeAmount = Math.Abs(card.Amount);
                        RentPayment payment = new RentPayment(
                            player.BoardPosition,
                            card.CardId,
                            card.Title,
                            currentPlayerIndex,
                            PropertyState.UnownedPlayerIndex,
                            feeAmount);

                        if (TryTransferCash(currentPlayerIndex, PropertyState.UnownedPlayerIndex, feeAmount))
                        {
                            payment.MarkPaid();
                            State.Turn.PaidFeeAmount = feeAmount;
                        }

                        State.Turn.RecordRentPayment(payment);
                    }
                    break;

                case CardEffectKind.CashFromEachPlayer:
                    int amountFromEach = card.Amount;
                    for (int i = 0; i < State.Players.Count; i++)
                    {
                        if (i != currentPlayerIndex && !State.Players[i].IsEliminated)
                        {
                            PlayerState other = State.MutablePlayers[i];
                            int collected = Math.Min(other.Cash, amountFromEach);
                            if (other.TrySpendCash(collected))
                            {
                                player.AddCash(collected);
                            }
                        }
                    }

                    State.Turn.CompleteLanding();
                    break;

                case CardEffectKind.CashToEachPlayer:
                    int amountToEach = card.Amount;
                    int activeOtherCount = 0;
                    for (int i = 0; i < State.Players.Count; i++)
                    {
                        if (i != currentPlayerIndex && !State.Players[i].IsEliminated)
                        {
                            activeOtherCount++;
                        }
                    }

                    int totalOwed = amountToEach * activeOtherCount;
                    if (player.Cash >= totalOwed)
                    {
                        for (int i = 0; i < State.Players.Count; i++)
                        {
                            if (i != currentPlayerIndex && !State.Players[i].IsEliminated)
                            {
                                player.TrySpendCash(amountToEach);
                                State.MutablePlayers[i].AddCash(amountToEach);
                            }
                        }

                        State.Turn.CompleteLanding();
                    }
                    else
                    {
                        RentPayment payment = new RentPayment(
                            player.BoardPosition,
                            card.CardId,
                            card.Title,
                            currentPlayerIndex,
                            PropertyState.UnownedPlayerIndex,
                            totalOwed);

                        State.Turn.RecordRentPayment(payment);
                    }
                    break;

                case CardEffectKind.MoveToTile:
                    int destination = card.TargetTileIndex;
                    if (card.CanCollectPassStart)
                    {
                        if (destination < player.BoardPosition || destination == 0)
                        {
                            player.AddCash(_passStartCash);
                        }
                    }

                    player.MoveTo(destination);
                    ResolveLandingAt(destination);
                    break;

                case CardEffectKind.MoveRelative:
                    int newPos = (player.BoardPosition + card.RelativeSteps + _boardTileCount) % _boardTileCount;
                    player.MoveTo(newPos);
                    ResolveLandingAt(newPos);
                    break;

                case CardEffectKind.MoveToNearest:
                    int nearestIndex = FindNearestTileOfKind(player.BoardPosition, card.TargetTileKind);
                    if (nearestIndex >= 0)
                    {
                        if (nearestIndex < player.BoardPosition)
                        {
                            player.AddCash(_passStartCash);
                        }

                        player.MoveTo(nearestIndex);
                        ResolveLandingAt(nearestIndex, card.RentMultiplier);
                    }
                    else
                    {
                        State.Turn.CompleteLanding();
                    }
                    break;

                case CardEffectKind.PropertyRepair:
                    int totalRepairCost = CalculatePropertyRepairCost(currentPlayerIndex, card.CostPerHouse, card.CostPerHotel);
                    if (totalRepairCost == 0)
                    {
                        State.Turn.CompleteLanding();
                    }
                    else
                    {
                        RentPayment payment = new RentPayment(
                            player.BoardPosition,
                            card.CardId,
                            card.Title,
                            currentPlayerIndex,
                            PropertyState.UnownedPlayerIndex,
                            totalRepairCost);

                        if (TryTransferCash(currentPlayerIndex, PropertyState.UnownedPlayerIndex, totalRepairCost))
                        {
                            payment.MarkPaid();
                            State.Turn.PaidFeeAmount = totalRepairCost;
                        }

                        State.Turn.RecordRentPayment(payment);
                    }
                    break;

                case CardEffectKind.GoToDetention:
                    SendPlayerToDetention(currentPlayerIndex);
                    State.Turn.CompleteLanding();
                    break;

                case CardEffectKind.DetentionWaiver:
                    player.AddDetentionWaiver();
                    State.Turn.CompleteLanding();
                    break;

                default:
                    State.Turn.CompleteLanding();
                    break;
            }
        }

        private void ResolveLandingAt(int tileIndex, int rentMultiplier = 1)
        {
            if (_propertiesByTileIndex.TryGetValue(tileIndex, out PropertyState property))
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
                    ResolveRent(property, rentMultiplier);
                }
            }
            else if (_specialSpacesByTileIndex.TryGetValue(tileIndex, out SpecialSpaceSnapshot specialSpace))
            {
                ResolveSpecialSpace(specialSpace);
            }
            else
            {
                State.Turn.CompleteLanding();
            }
        }

        private int FindNearestTileOfKind(int fromPosition, BoardTileKind kind)
        {
            for (int step = 1; step <= _boardTileCount; step++)
            {
                int candidate = (fromPosition + step) % _boardTileCount;
                if (_propertiesByTileIndex.TryGetValue(candidate, out PropertyState property) &&
                    property.Kind == kind)
                {
                    return candidate;
                }
            }

            return -1;
        }

        private int CalculatePropertyRepairCost(int playerIndex, int costPerHouse, int costPerHotel)
        {
            int totalHouses = 0;
            int totalHotels = 0;
            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex && property.Kind == BoardTileKind.City)
                {
                    if (property.UpgradeLevel >= 1 && property.UpgradeLevel <= 4)
                    {
                        totalHouses += property.UpgradeLevel;
                    }
                    else if (property.UpgradeLevel == 5)
                    {
                        totalHotels += 1;
                    }
                }
            }

            return (totalHouses * costPerHouse) + (totalHotels * costPerHotel);
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

            if (receiverPlayerIndex >= 0 && receiverPlayerIndex < State.Players.Count)
            {
                State.MutablePlayers[receiverPlayerIndex].AddCash(amount);
            }
            else if (_vacationRewardEnabled)
            {
                _vacationPool = checked(_vacationPool + amount);
            }

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

        public int CalculatePlayerNetWorth(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= State.Players.Count)
            {
                return 0;
            }

            PlayerState player = State.Players[playerIndex];
            int netWorth = player.Cash;
            netWorth += player.HeldDetentionWaivers * 50;

            foreach (PropertyState property in State.Properties)
            {
                if (property.OwnerPlayerIndex == playerIndex)
                {
                    netWorth += property.IsMortgaged
                        ? property.MortgageValue
                        : property.PurchasePrice;

                    netWorth += property.UpgradeLevel * property.UpgradeCost;
                }
            }

            return netWorth;
        }

        public MatchResult GetMatchResult()
        {
            int winnerIndex = State.WinnerPlayerIndex;
            if (winnerIndex < 0)
            {
                winnerIndex = GetSoleActivePlayerIndex();
            }

            List<PlayerStanding> standings = new List<PlayerStanding>();

            // 1. Winner is rank 1
            if (winnerIndex >= 0 && winnerIndex < State.Players.Count)
            {
                PlayerState winner = State.Players[winnerIndex];
                int winnerProps = 0;
                foreach (PropertyState prop in State.Properties)
                {
                    if (prop.OwnerPlayerIndex == winnerIndex)
                    {
                        winnerProps++;
                    }
                }

                standings.Add(new PlayerStanding(
                    rank: 1,
                    playerIndex: winnerIndex,
                    playerId: winner.PlayerId,
                    displayName: winner.DisplayName,
                    creatureId: winner.CreatureId,
                    cash: winner.Cash,
                    netWorth: CalculatePlayerNetWorth(winnerIndex),
                    propertiesCount: winnerProps,
                    isWinner: true,
                    eliminationReason: null,
                    eliminationTurn: 0));
            }

            // 2. Add eliminated players in reverse order of elimination
            for (int i = _eliminations.Count - 1; i >= 0; i--)
            {
                PlayerEliminationRecord rec = _eliminations[i];
                if (rec.PlayerIndex == winnerIndex)
                {
                    continue;
                }

                PlayerState p = State.Players[rec.PlayerIndex];
                int rank = standings.Count + 1;
                standings.Add(new PlayerStanding(
                    rank: rank,
                    playerIndex: rec.PlayerIndex,
                    playerId: p.PlayerId,
                    displayName: p.DisplayName,
                    creatureId: p.CreatureId,
                    cash: rec.CashAtElimination,
                    netWorth: rec.NetWorthAtElimination,
                    propertiesCount: rec.PropertiesCountAtElimination,
                    isWinner: false,
                    eliminationReason: rec.Reason,
                    eliminationTurn: rec.TurnNumber));
            }

            // 3. Any uneliminated non-winner players
            HashSet<int> includedSeats = new HashSet<int>();
            for (int i = 0; i < standings.Count; i++)
            {
                includedSeats.Add(standings[i].PlayerIndex);
            }

            var remainingSeats = new List<int>();
            for (int i = 0; i < State.Players.Count; i++)
            {
                if (!includedSeats.Contains(i))
                {
                    remainingSeats.Add(i);
                }
            }

            if (remainingSeats.Count > 0)
            {
                remainingSeats.Sort((seatA, seatB) =>
                {
                    int nwA = CalculatePlayerNetWorth(seatA);
                    int nwB = CalculatePlayerNetWorth(seatB);
                    int cmp = nwB.CompareTo(nwA);
                    return cmp != 0 ? cmp : State.Players[seatB].Cash.CompareTo(State.Players[seatA].Cash);
                });

                foreach (int seat in remainingSeats)
                {
                    PlayerState p = State.Players[seat];
                    int props = 0;
                    foreach (PropertyState prop in State.Properties)
                    {
                        if (prop.OwnerPlayerIndex == seat) props++;
                    }

                    int rank = standings.Count + 1;
                    standings.Add(new PlayerStanding(
                        rank: rank,
                        playerIndex: seat,
                        playerId: p.PlayerId,
                        displayName: p.DisplayName,
                        creatureId: p.CreatureId,
                        cash: p.Cash,
                        netWorth: CalculatePlayerNetWorth(seat),
                        propertiesCount: props,
                        isWinner: rank == 1,
                        eliminationReason: null,
                        eliminationTurn: 0));
                }
            }

            string winnerName = winnerIndex >= 0 && winnerIndex < State.Players.Count
                ? State.Players[winnerIndex].DisplayName
                : "None";
            string winnerCreature = winnerIndex >= 0 && winnerIndex < State.Players.Count
                ? State.Players[winnerIndex].CreatureId
                : string.Empty;
            int totalTurns = State.Turn?.TurnNumber ?? 1;

            return new MatchResult(
                winnerIndex,
                winnerName,
                winnerCreature,
                totalTurns,
                standings);
        }
    }
}
