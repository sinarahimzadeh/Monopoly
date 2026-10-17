using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Core.Match;
using Ropoly.Core.Trading;

namespace Ropoly.Tests.EditMode
{
    [TestFixture]
    public sealed class TradeTests
    {
        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean"
        };

        private LocalMatchSession CreateStartedSession(int startingCash = 1500)
        {
            var session = new LocalMatchSession(2, startingCash, CreatureIds);
            session.TrySelectCreature(0, "pip");
            session.TrySelectCreature(1, "bubu");
            session.TryStartMatch();
            return session;
        }

        [Test]
        public void CanProposeTrade_RejectsEmptyTrade()
        {
            var session = CreateStartedSession();
            var offer = new TradeOffer(
                "t1",
                proposerPlayerIndex: 0,
                targetPlayerIndex: 1,
                offeredCash: 0,
                offeredPropertyTileIndices: null,
                offeredWaivers: 0,
                requestedCash: 0,
                requestedPropertyTileIndices: null,
                requestedWaivers: 0);

            bool can = session.CanProposeTrade(offer, out TradeOfferResult error);

            Assert.That(can, Is.False);
            Assert.That(error, Is.EqualTo(TradeOfferResult.EmptyTrade));
        }

        [Test]
        public void CanProposeTrade_RejectsSelfTrade()
        {
            var session = CreateStartedSession();
            var offer = new TradeOffer(
                "t1",
                proposerPlayerIndex: 0,
                targetPlayerIndex: 0,
                offeredCash: 100,
                offeredPropertyTileIndices: null,
                offeredWaivers: 0,
                requestedCash: 50,
                requestedPropertyTileIndices: null,
                requestedWaivers: 0);

            bool can = session.CanProposeTrade(offer, out TradeOfferResult error);

            Assert.That(can, Is.False);
            Assert.That(error, Is.EqualTo(TradeOfferResult.InvalidPlayers));
        }

        [Test]
        public void CanProposeTrade_RejectsInsufficientCash()
        {
            var session = CreateStartedSession(startingCash: 500);

            // Proposer offers more than they have
            var offer1 = new TradeOffer("t1", 0, 1, offeredCash: 600, null, 0, 100, null, 0);
            Assert.That(session.CanProposeTrade(offer1, out var err1), Is.False);
            Assert.That(err1, Is.EqualTo(TradeOfferResult.InsufficientCash));

            // Proposer requests more than target has
            var offer2 = new TradeOffer("t2", 0, 1, offeredCash: 100, null, 0, 600, null, 0);
            Assert.That(session.CanProposeTrade(offer2, out var err2), Is.False);
            Assert.That(err2, Is.EqualTo(TradeOfferResult.InsufficientCash));
        }

        [Test]
        public void CanProposeTrade_RejectsInsufficientWaivers()
        {
            var session = CreateStartedSession();

            var offer = new TradeOffer("t1", 0, 1, 0, null, offeredWaivers: 1, 100, null, 0);
            Assert.That(session.CanProposeTrade(offer, out var err), Is.False);
            Assert.That(err, Is.EqualTo(TradeOfferResult.InsufficientWaivers));

            // Give proposer a waiver, then propose requesting 1 from target who has 0
            session.State.Players[0].AddDetentionWaivers(1);
            var offer2 = new TradeOffer("t2", 0, 1, 0, null, 1, 0, null, requestedWaivers: 1);
            Assert.That(session.CanProposeTrade(offer2, out var err2), Is.False);
            Assert.That(err2, Is.EqualTo(TradeOfferResult.InsufficientWaivers));
        }

        [Test]
        public void CanProposeTrade_RejectsUnownedProperty()
        {
            var session = CreateStartedSession();

            // Tile 1 is an unowned property on default board
            var offer = new TradeOffer("t1", 0, 1, 0, new[] { 1 }, 0, 100, null, 0);
            Assert.That(session.CanProposeTrade(offer, out var err), Is.False);
            Assert.That(err, Is.EqualTo(TradeOfferResult.PropertyNotOwned));
        }

        [Test]
        public void CanProposeTrade_RejectsPropertyWithBuildings()
        {
            var session = CreateStartedSession();

            // Give property to player 0 and simulate upgrade
            if (session.TryGetPropertyAt(1, out PropertyState prop))
            {
                prop.TryAssignOwner(0);
                prop.TryUpgrade();

                var offer = new TradeOffer("t1", 0, 1, 0, new[] { 1 }, 0, 100, null, 0);
                Assert.That(session.CanProposeTrade(offer, out var err), Is.False);
                Assert.That(err, Is.EqualTo(TradeOfferResult.PropertyHasBuildings));
            }
        }

        [Test]
        public void TryProposeTrade_SetsActiveTradeAndFiresEvent()
        {
            var session = CreateStartedSession();
            TradeOffer proposedEventOffer = null;
            session.TradeProposed += o => proposedEventOffer = o;

            var offer = new TradeOffer("t1", 0, 1, offeredCash: 200, null, 0, requestedCash: 100, null, 0);
            bool success = session.TryProposeTrade(offer, out var result);

            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(TradeOfferResult.Success));
            Assert.That(session.ActiveTradeOffer, Is.Not.Null);
            Assert.That(session.ActiveTradeOffer.TradeId, Is.EqualTo("t1"));
            Assert.That(session.ActiveTradeOffer.Status, Is.EqualTo(TradeOfferStatus.Pending));
            Assert.That(proposedEventOffer, Is.EqualTo(offer));
        }

        [Test]
        public void TryProposeTrade_RejectsIfAnotherTradeAlreadyActive()
        {
            var session = CreateStartedSession();
            var offer1 = new TradeOffer("t1", 0, 1, 200, null, 0, 100, null, 0);
            var offer2 = new TradeOffer("t2", 1, 0, 50, null, 0, 50, null, 0);

            Assert.That(session.TryProposeTrade(offer1, out _), Is.True);
            bool second = session.TryProposeTrade(offer2, out var err);

            Assert.That(second, Is.False);
            Assert.That(err, Is.EqualTo(TradeOfferResult.TradeAlreadyActive));
        }

        [Test]
        public void TryAcceptActiveTrade_TransfersCashAtomically()
        {
            var session = CreateStartedSession(startingCash: 1000);
            TradeOffer completedEventOffer = null;
            session.TradeCompleted += o => completedEventOffer = o;

            var offer = new TradeOffer("t1", 0, 1, offeredCash: 300, null, 0, requestedCash: 100, null, 0);
            session.TryProposeTrade(offer, out _);

            bool accepted = session.TryAcceptActiveTrade(1, out var acceptResult);

            Assert.That(accepted, Is.True);
            Assert.That(acceptResult, Is.EqualTo(TradeOfferResult.Success));
            Assert.That(session.ActiveTradeOffer, Is.Null);
            Assert.That(offer.Status, Is.EqualTo(TradeOfferStatus.Accepted));
            Assert.That(completedEventOffer, Is.EqualTo(offer));

            // Player 0 gave 300, received 100 => net -200 => 800
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(800));
            // Player 1 received 300, gave 100 => net +200 => 1200
            Assert.That(session.State.Players[1].Cash, Is.EqualTo(1200));
        }

        [Test]
        public void TryAcceptActiveTrade_TransfersPropertiesAndPreservesMortgage()
        {
            var session = CreateStartedSession();

            // Give tile 1 to player 0, tile 3 to player 1 (and mortgage tile 3)
            Assert.That(session.TryGetPropertyAt(1, out PropertyState prop1), Is.True);
            Assert.That(session.TryGetPropertyAt(3, out PropertyState prop3), Is.True);

            prop1.TryAssignOwner(0);
            prop3.TryAssignOwner(1);
            prop3.TryMortgage(1);

            var offer = new TradeOffer(
                "trade-props",
                proposerPlayerIndex: 0,
                targetPlayerIndex: 1,
                offeredCash: 50,
                offeredPropertyTileIndices: new[] { 1 },
                offeredWaivers: 0,
                requestedCash: 0,
                requestedPropertyTileIndices: new[] { 3 },
                requestedWaivers: 0);

            Assert.That(session.TryProposeTrade(offer, out _), Is.True);
            Assert.That(session.TryAcceptActiveTrade(1, out _), Is.True);

            // Ownership transferred
            Assert.That(prop1.OwnerPlayerIndex, Is.EqualTo(1));
            Assert.That(prop3.OwnerPlayerIndex, Is.EqualTo(0));

            // Tile 3 should still be mortgaged
            Assert.That(prop3.IsMortgaged, Is.True);
            Assert.That(prop1.IsMortgaged, Is.False);
        }

        [Test]
        public void TryAcceptActiveTrade_TransfersDetentionWaivers()
        {
            var session = CreateStartedSession();
            session.State.Players[0].AddDetentionWaivers(2);
            session.State.Players[1].AddDetentionWaivers(1);

            var offer = new TradeOffer("t-waiver", 0, 1, 0, null, offeredWaivers: 1, 0, null, requestedWaivers: 1);
            session.TryProposeTrade(offer, out _);
            session.TryAcceptActiveTrade(1, out _);

            // Swapped 1 for 1
            Assert.That(session.State.Players[0].HeldDetentionWaivers, Is.EqualTo(2));
            Assert.That(session.State.Players[1].HeldDetentionWaivers, Is.EqualTo(1));
        }

        [Test]
        public void TryAcceptActiveTrade_RejectsNonTargetPlayer()
        {
            var session = CreateStartedSession();
            var offer = new TradeOffer("t1", 0, 1, 100, null, 0, 50, null, 0);
            session.TryProposeTrade(offer, out _);

            // Proposer (0) tries to accept their own offer
            bool accepted = session.TryAcceptActiveTrade(0, out var err);

            Assert.That(accepted, Is.False);
            Assert.That(err, Is.EqualTo(TradeOfferResult.Unauthorized));
            Assert.That(session.ActiveTradeOffer, Is.Not.Null);
        }

        [Test]
        public void TryRejectActiveTrade_ClearsActiveTradeAndFiresEvent()
        {
            var session = CreateStartedSession();
            TradeOffer rejectedEventOffer = null;
            session.TradeRejected += o => rejectedEventOffer = o;

            var offer = new TradeOffer("t1", 0, 1, 100, null, 0, 50, null, 0);
            session.TryProposeTrade(offer, out _);

            bool rejected = session.TryRejectActiveTrade(1, out var result);

            Assert.That(rejected, Is.True);
            Assert.That(result, Is.EqualTo(TradeOfferResult.Success));
            Assert.That(session.ActiveTradeOffer, Is.Null);
            Assert.That(offer.Status, Is.EqualTo(TradeOfferStatus.Rejected));
            Assert.That(rejectedEventOffer, Is.EqualTo(offer));
        }

        [Test]
        public void TryCancelActiveTrade_ClearsActiveTradeAndFiresEvent()
        {
            var session = CreateStartedSession();
            TradeOffer cancelledEventOffer = null;
            session.TradeCancelled += o => cancelledEventOffer = o;

            var offer = new TradeOffer("t1", 0, 1, 100, null, 0, 50, null, 0);
            session.TryProposeTrade(offer, out _);

            // Target (1) cannot cancel
            Assert.That(session.TryCancelActiveTrade(1, out var err), Is.False);
            Assert.That(err, Is.EqualTo(TradeOfferResult.Unauthorized));

            // Proposer (0) cancels
            bool cancelled = session.TryCancelActiveTrade(0, out var result);

            Assert.That(cancelled, Is.True);
            Assert.That(result, Is.EqualTo(TradeOfferResult.Success));
            Assert.That(session.ActiveTradeOffer, Is.Null);
            Assert.That(offer.Status, Is.EqualTo(TradeOfferStatus.Cancelled));
            Assert.That(cancelledEventOffer, Is.EqualTo(offer));
        }

        [Test]
        public void CreateCounterOffer_SwapsAssetsAndRoles()
        {
            var original = new TradeOffer(
                "orig",
                proposerPlayerIndex: 0,
                targetPlayerIndex: 1,
                offeredCash: 250,
                offeredPropertyTileIndices: new[] { 1, 3 },
                offeredWaivers: 1,
                requestedCash: 400,
                requestedPropertyTileIndices: new[] { 6 },
                requestedWaivers: 2);

            var counter = original.CreateCounterOffer("counter-1");

            Assert.That(counter.TradeId, Is.EqualTo("counter-1"));
            Assert.That(counter.ProposerPlayerIndex, Is.EqualTo(1));
            Assert.That(counter.TargetPlayerIndex, Is.EqualTo(0));
            Assert.That(counter.OfferedCash, Is.EqualTo(400));
            Assert.That(counter.OfferedPropertyTileIndices, Is.EquivalentTo(new[] { 6 }));
            Assert.That(counter.OfferedWaivers, Is.EqualTo(2));
            Assert.That(counter.RequestedCash, Is.EqualTo(250));
            Assert.That(counter.RequestedPropertyTileIndices, Is.EquivalentTo(new[] { 1, 3 }));
            Assert.That(counter.RequestedWaivers, Is.EqualTo(1));
        }

        [Test]
        public void PlayerElimination_AutomaticallyCancelsPendingTrade()
        {
            var session = CreateStartedSession();
            TradeOffer cancelledEventOffer = null;
            session.TradeCancelled += o => cancelledEventOffer = o;

            var offer = new TradeOffer("t1", 0, 1, 100, null, 0, 50, null, 0);
            session.TryProposeTrade(offer, out _);

            Assert.That(session.ActiveTradeOffer, Is.Not.Null);

            // Eliminate player 1
            session.ApplyAuthoritativeElimination(1, EliminationReason.Bankruptcy);

            Assert.That(session.ActiveTradeOffer, Is.Null);
            Assert.That(offer.Status, Is.EqualTo(TradeOfferStatus.Cancelled));
            Assert.That(cancelledEventOffer, Is.EqualTo(offer));
        }
    }
}
