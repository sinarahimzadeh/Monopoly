using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Auctions;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using Ropoly.Core.Time;

namespace Ropoly.Tests.EditMode
{
    [TestFixture]
    public sealed class AuctionTests
    {
        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean"
        };

        private static BoardSnapshot CreateTestBoard(int cityCount = 2, int rentMultiplier = 2)
        {
            var cities = new List<CitySnapshot>();
            var tiles = new List<BoardTileSnapshot>
            {
                new BoardTileSnapshot(0, BoardTileKind.Special, "start")
            };

            var cityIds = new List<string>();
            int[] rents = { 10, 35, 100, 250, 400, 600 };
            for (int i = 1; i <= cityCount; i++)
            {
                string id = $"city-{i}";
                cityIds.Add(id);
                cities.Add(new CitySnapshot(id, $"Test City {i}", 100, 50, 50, rents));
                tiles.Add(new BoardTileSnapshot(i, BoardTileKind.City, id));
            }

            for (int i = cityCount + 1; i < 8; i++)
            {
                tiles.Add(new BoardTileSnapshot(i, BoardTileKind.Special, "special"));
            }

            var country = new CountrySnapshot(
                "country-test",
                "Test Country",
                cityCount,
                10000,
                2024,
                "Source",
                "#FF0000",
                cityIds.ToArray());

            return new BoardSnapshot(
                schemaVersion: 1,
                boardId: "auction-test-board",
                displayName: "Auction Test Board",
                countries: new[] { country },
                cities: cities.ToArray(),
                airports: Array.Empty<AirportSnapshot>(),
                utilities: Array.Empty<UtilitySnapshot>(),
                specialSpaces: new[]
                {
                    new SpecialSpaceSnapshot("start", "Start", SpecialSpaceKind.Start, string.Empty, 0),
                    new SpecialSpaceSnapshot("special", "Special", SpecialSpaceKind.Vacation, string.Empty, 0),
                },
                tiles: tiles.ToArray());
        }

        private static LocalMatchSession CreateStartedSession(
            bool auctionsEnabled = false,
            int startingCash = 1500,
            int playerCount = 2,
            IClock clock = null,
            int fullSetRentMultiplier = 2)
        {
            var board = CreateTestBoard();
            var session = new LocalMatchSession(
                playerCount: playerCount,
                startingCash: startingCash,
                availableCreatureIds: CreatureIds,
                board: board,
                passStartCash: 200,
                mortgageEnabled: true,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: clock,
                fullSetRentMultiplier: fullSetRentMultiplier,
                vacationRewardEnabled: false,
                vacationReward: 0,
                cardDecks: null,
                auctionsEnabled: auctionsEnabled,
                auctionBidTimeoutSeconds: 15);

            for (int i = 0; i < playerCount; i++)
            {
                session.TrySelectCreature(i, CreatureIds[i]);
            }

            session.TryStartMatch();
            return session;
        }

        private sealed class FixedDiceRollSource : IDiceRollSource
        {
            private readonly int _first;
            private readonly int _second;
            private int _count;

            public FixedDiceRollSource(int first, int second)
            {
                _first = first;
                _second = second;
            }

            public int NextDieValue() => _count++ == 0 ? _first : _second;
        }

        [Test]
        public void DeclineProperty_WhenAuctionsDisabled_SkipsPropertyWithoutAuction()
        {
            var session = CreateStartedSession(auctionsEnabled: false);

            // Player 0 rolls 1, lands on tile 1 (unowned city-1)
            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();

            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingPropertyDecision));
            Assert.That(session.CurrentPurchaseOffer, Is.Not.Null);

            // Player declines property
            bool declined = session.TryDeclineCurrentProperty();

            Assert.That(declined, Is.True);
            Assert.That(session.ActiveAuction, Is.Null);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
            Assert.That(session.TryGetPropertyAt(1, out PropertyState prop), Is.True);
            Assert.That(prop.IsOwned, Is.False);
        }

        [Test]
        public void DeclineProperty_WhenAuctionsEnabled_EntersAuction()
        {
            var session = CreateStartedSession(auctionsEnabled: true);

            // Player 0 rolls 1, lands on tile 1
            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();

            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingPropertyDecision));

            bool auctionFired = false;
            session.AuctionStarted += auction => auctionFired = true;

            // Player declines property
            bool declined = session.TryDeclineCurrentProperty();

            Assert.That(declined, Is.True);
            Assert.That(auctionFired, Is.True);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.Auction));
            Assert.That(session.ActiveAuction, Is.Not.Null);
            Assert.That(session.ActiveAuction.IsActive, Is.True);
            Assert.That(session.ActiveAuction.TileIndex, Is.EqualTo(1));
            Assert.That(session.ActiveAuction.CurrentBid, Is.Zero);
            Assert.That(session.ActiveAuction.HighestBidderPlayerIndex, Is.EqualTo(-1));
        }

        [Test]
        public void AuctionBidding_ValidatesIncrementsAndRules()
        {
            var session = CreateStartedSession(auctionsEnabled: true);
            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryDeclineCurrentProperty();

            // Opening bid: Player 0 bids $1
            bool bid1 = session.TryPlaceAuctionBid(0, 1, out AuctionBidResult res1);
            Assert.That(bid1, Is.True);
            Assert.That(res1, Is.EqualTo(AuctionBidResult.Success));
            Assert.That(session.ActiveAuction.CurrentBid, Is.EqualTo(1));
            Assert.That(session.ActiveAuction.HighestBidderPlayerIndex, Is.EqualTo(0));

            // Player 0 tries to outbid themselves -> fails
            bool selfBid = session.TryPlaceAuctionBid(0, 5, out AuctionBidResult resSelf);
            Assert.That(selfBid, Is.False);
            Assert.That(resSelf, Is.EqualTo(AuctionBidResult.AlreadyHighestBidder));

            // Player 1 raises with +$10 (bids 11) -> success
            bool bid10 = session.TryPlaceAuctionBid(1, 11, out AuctionBidResult res10);
            Assert.That(bid10, Is.True);
            Assert.That(res10, Is.EqualTo(AuctionBidResult.Success));
            Assert.That(session.ActiveAuction.CurrentBid, Is.EqualTo(11));
            Assert.That(session.ActiveAuction.HighestBidderPlayerIndex, Is.EqualTo(1));

            // Player 0 raises with +$100 (bids 111) -> success
            bool bid100 = session.TryPlaceAuctionBid(0, 111, out AuctionBidResult res100);
            Assert.That(bid100, Is.True);
            Assert.That(res100, Is.EqualTo(AuctionBidResult.Success));
            Assert.That(session.ActiveAuction.CurrentBid, Is.EqualTo(111));
            Assert.That(session.ActiveAuction.HighestBidderPlayerIndex, Is.EqualTo(0));

            // Player 1 tries to bid lower than current bid -> fails
            bool lowBid = session.TryPlaceAuctionBid(1, 50, out AuctionBidResult resLow);
            Assert.That(lowBid, Is.False);
            Assert.That(resLow, Is.EqualTo(AuctionBidResult.BidTooLow));

            // Player with insufficient cash -> fails
            session.State.MutablePlayers[1].TrySpendCash(session.State.Players[1].Cash - 5);
            Assert.That(session.State.Players[1].Cash, Is.EqualTo(5));
            bool poorBid = session.TryPlaceAuctionBid(1, 200, out AuctionBidResult resPoor);
            Assert.That(poorBid, Is.False);
            Assert.That(resPoor, Is.EqualTo(AuctionBidResult.InsufficientCash));
        }

        [Test]
        public void AuctionPassing_ConcludesImmediatelyWhenAllOthersPass()
        {
            var session = CreateStartedSession(auctionsEnabled: true, startingCash: 1500, playerCount: 2);
            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryDeclineCurrentProperty();

            // Player 0 bids $25
            session.TryPlaceAuctionBid(0, 25);

            bool auctionCompleted = false;
            session.AuctionCompleted += auction => auctionCompleted = true;

            // Player 1 passes -> Player 0 is the only active bidder left
            bool passed = session.TryPassAuction(1, out AuctionPassResult passRes);
            Assert.That(passed, Is.True);
            Assert.That(passRes, Is.EqualTo(AuctionPassResult.Success));

            // Auction concludes immediately!
            Assert.That(auctionCompleted, Is.True);
            Assert.That(session.ActiveAuction.IsActive, Is.False);
            Assert.That(session.ActiveAuction.WinnerPlayerIndex, Is.EqualTo(0));
            Assert.That(session.ActiveAuction.WinningBid, Is.EqualTo(25));

            // Player 0 paid $25 and owns property 1
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(1500 - 25));
            Assert.That(session.TryGetPropertyAt(1, out PropertyState prop), Is.True);
            Assert.That(prop.OwnerPlayerIndex, Is.EqualTo(0));

            // Turn phase returns to AwaitingTurnEnd
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void AuctionPassing_ConcludesWithoutWinner_WhenAllPassWithNoBids()
        {
            var session = CreateStartedSession(auctionsEnabled: true, playerCount: 2);
            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryDeclineCurrentProperty();

            // Player 0 passes
            session.TryPassAuction(0);
            Assert.That(session.ActiveAuction.IsActive, Is.True);

            // Player 1 passes
            session.TryPassAuction(1);

            // Concluded with no winner
            Assert.That(session.ActiveAuction.IsActive, Is.False);
            Assert.That(session.ActiveAuction.WinnerPlayerIndex, Is.EqualTo(-1));
            Assert.That(session.ActiveAuction.WinningBid, Is.Zero);

            Assert.That(session.TryGetPropertyAt(1, out PropertyState prop), Is.True);
            Assert.That(prop.IsOwned, Is.False);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void AuctionTimeout_ConcludesAuctionWithHighestBidder()
        {
            var initialTime = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
            var testClock = new TestClock(initialTime);
            var session = CreateStartedSession(auctionsEnabled: true, clock: testClock, playerCount: 3);

            session.TryRollDice(new FixedDiceRollSource(1, 0), out _);
            session.TryBeginMovement(out _);
            session.TryCompleteMovement();
            session.TryDeclineCurrentProperty();

            // Player 1 bids $50
            session.TryPlaceAuctionBid(1, 50);

            // Advance time past the 15-second deadline
            testClock.AdvanceSeconds(16);

            // Session ticks
            bool changed = session.Tick();

            Assert.That(changed, Is.True);
            Assert.That(session.ActiveAuction.IsActive, Is.False);
            Assert.That(session.ActiveAuction.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(session.ActiveAuction.WinningBid, Is.EqualTo(50));
            Assert.That(session.TryGetPropertyAt(1, out PropertyState prop), Is.True);
            Assert.That(prop.OwnerPlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void PropertyEconomics_FullCountrySetRentMultiplier_CalculatesCorrectly()
        {
            var session = CreateStartedSession(fullSetRentMultiplier: 3);
            Assert.That(session.TryGetPropertyAt(1, out PropertyState city1), Is.True);
            Assert.That(session.TryGetPropertyAt(2, out PropertyState city2), Is.True);

            // Initially unowned -> rent is 0
            Assert.That(session.GetPropertyRent(1), Is.Zero);

            // Player 0 buys city 1
            city1.TryAssignOwner(0);

            // Single city owned: base unimproved rent is 10
            Assert.That(session.GetPropertyRent(1), Is.EqualTo(10));

            // Player 0 buys city 2 (completes country set!)
            city2.TryAssignOwner(0);

            // Full country set unmortgaged: base rent (10) * multiplier (3) = 30
            Assert.That(session.GetPropertyRent(1), Is.EqualTo(30));
            Assert.That(session.GetPropertyRent(2), Is.EqualTo(30));

            // If city 2 is mortgaged -> rent returns to base rent 10
            city2.TryMortgage(0);
            Assert.That(session.GetPropertyRent(1), Is.EqualTo(10));
            Assert.That(session.GetPropertyRent(2), Is.Zero); // mortgaged property pays 0

            // Unmortgage city 2
            city2.ReleaseToBank();
            city2.TryAssignOwner(0);
            Assert.That(session.GetPropertyRent(1), Is.EqualTo(30));

            // Upgrade city 1 to level 1 (improved rent = 35) -> multiplier does NOT apply to houses/hotels
            city1.TryUpgrade();
            Assert.That(city1.UpgradeLevel, Is.EqualTo(1));
            Assert.That(session.GetPropertyRent(1), Is.EqualTo(35));
        }
    }
}
