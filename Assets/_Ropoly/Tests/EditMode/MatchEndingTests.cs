using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Core.Cards;
using Ropoly.Core.Match;
using Ropoly.Core.Time;

namespace Ropoly.Tests.EditMode
{
    [TestFixture]
    public sealed class MatchEndingTests
    {
        private static readonly string[] CreatureIds =
        {
            "pip", "bubu", "sunny", "peach", "mochi", "nibi", "poppy", "bean"
        };

        private LocalMatchSession CreateStartedSession(int playerCount = 2, int startingCash = 1500)
        {
            var session = new LocalMatchSession(playerCount, startingCash, CreatureIds);
            for (int i = 0; i < playerCount; i++)
            {
                session.TrySelectCreature(i, CreatureIds[i]);
            }

            session.TryStartMatch();
            return session;
        }

        [Test]
        public void SoleActivePlayerWinsMatch_WhenOpponentEliminated()
        {
            var session = CreateStartedSession(playerCount: 2);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));

            // Force player 1 to be eliminated via authoritative elimination
            bool eliminated = session.ApplyAuthoritativeElimination(1, EliminationReason.Bankruptcy);
            Assert.That(eliminated, Is.True);

            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(session.State.WinnerPlayerIndex, Is.EqualTo(0));
            Assert.That(session.State.Players[0].IsEliminated, Is.False);
            Assert.That(session.State.Players[1].IsEliminated, Is.True);
        }

        [Test]
        public void MatchEndedEventDispatched_WithAuthoritativeMatchResult()
        {
            var session = CreateStartedSession(playerCount: 2);
            MatchResult capturedResult = null;
            session.MatchEnded += result => capturedResult = result;

            session.ApplyAuthoritativeElimination(1, EliminationReason.InactivityTimeout);

            Assert.That(capturedResult, Is.Not.Null);
            Assert.That(capturedResult.WinnerPlayerIndex, Is.EqualTo(0));
            Assert.That(capturedResult.WinnerDisplayName, Is.EqualTo(session.State.Players[0].DisplayName));
            Assert.That(capturedResult.WinnerCreatureId, Is.EqualTo("pip"));
            Assert.That(capturedResult.TotalTurns, Is.GreaterThanOrEqualTo(1));
            Assert.That(capturedResult.Standings, Has.Count.EqualTo(2));

            PlayerStanding winnerStanding = capturedResult.Standings[0];
            Assert.That(winnerStanding.Rank, Is.EqualTo(1));
            Assert.That(winnerStanding.PlayerIndex, Is.EqualTo(0));
            Assert.That(winnerStanding.IsWinner, Is.True);
            Assert.That(winnerStanding.EliminationReason, Is.Null);

            PlayerStanding secondStanding = capturedResult.Standings[1];
            Assert.That(secondStanding.Rank, Is.EqualTo(2));
            Assert.That(secondStanding.PlayerIndex, Is.EqualTo(1));
            Assert.That(secondStanding.IsWinner, Is.False);
            Assert.That(secondStanding.EliminationReason, Is.EqualTo(EliminationReason.InactivityTimeout));
        }

        [Test]
        public void StandingsFollowReverseEliminationOrder_InFourPlayerMatch()
        {
            var session = CreateStartedSession(playerCount: 4, startingCash: 1500);

            // Elimination sequence: Player 2 first, then Player 0 second, then Player 3 third
            // Sole remaining player is Player 1 -> Winner (Rank 1)
            session.ApplyAuthoritativeElimination(2, EliminationReason.Bankruptcy);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));

            session.ApplyAuthoritativeElimination(0, EliminationReason.DebtTimeout);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.InProgress));

            session.ApplyAuthoritativeElimination(3, EliminationReason.DisconnectTimeout);
            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Finished));

            MatchResult result = session.GetMatchResult();
            Assert.That(result, Is.Not.Null);
            Assert.That(result.WinnerPlayerIndex, Is.EqualTo(1));
            Assert.That(result.Standings, Has.Count.EqualTo(4));

            // Rank 1: Player 1 (Winner)
            Assert.That(result.Standings[0].Rank, Is.EqualTo(1));
            Assert.That(result.Standings[0].PlayerIndex, Is.EqualTo(1));
            Assert.That(result.Standings[0].IsWinner, Is.True);

            // Rank 2: Player 3 (Last eliminated)
            Assert.That(result.Standings[1].Rank, Is.EqualTo(2));
            Assert.That(result.Standings[1].PlayerIndex, Is.EqualTo(3));
            Assert.That(result.Standings[1].IsWinner, Is.False);
            Assert.That(result.Standings[1].EliminationReason, Is.EqualTo(EliminationReason.DisconnectTimeout));

            // Rank 3: Player 0 (Second eliminated)
            Assert.That(result.Standings[2].Rank, Is.EqualTo(3));
            Assert.That(result.Standings[2].PlayerIndex, Is.EqualTo(0));
            Assert.That(result.Standings[2].IsWinner, Is.False);
            Assert.That(result.Standings[2].EliminationReason, Is.EqualTo(EliminationReason.DebtTimeout));

            // Rank 4: Player 2 (First eliminated)
            Assert.That(result.Standings[3].Rank, Is.EqualTo(4));
            Assert.That(result.Standings[3].PlayerIndex, Is.EqualTo(2));
            Assert.That(result.Standings[3].IsWinner, Is.False);
            Assert.That(result.Standings[3].EliminationReason, Is.EqualTo(EliminationReason.Bankruptcy));
        }

        [Test]
        public void CalculatePlayerNetWorth_IncludesCashPropertiesUpgradesAndWaivers()
        {
            var session = CreateStartedSession(playerCount: 2, startingCash: 2000);

            // Baseline net worth is starting cash
            int initialNetWorth = session.CalculatePlayerNetWorth(0);
            Assert.That(initialNetWorth, Is.EqualTo(2000));

            // Give player 0 a property
            PropertyState prop = session.State.Properties.First(p => p.Kind == BoardTileKind.City);
            prop.TryAssignOwner(0);

            int netWorthWithProp = session.CalculatePlayerNetWorth(0);
            Assert.That(netWorthWithProp, Is.EqualTo(2000 + prop.PurchasePrice));

            // Upgrade property
            prop.TryUpgrade();
            int netWorthWithUpgrade = session.CalculatePlayerNetWorth(0);
            Assert.That(netWorthWithUpgrade, Is.EqualTo(2000 + prop.PurchasePrice + prop.UpgradeCost));

            // Add detention waiver ($50)
            session.State.MutablePlayers[0].AddDetentionWaiver();
            int netWorthWithWaiver = session.CalculatePlayerNetWorth(0);
            Assert.That(netWorthWithWaiver, Is.EqualTo(2000 + prop.PurchasePrice + prop.UpgradeCost + 50));

            // Mortgage property: replaces PurchasePrice with MortgageValue (and downgrade resets upgrades)
            prop.TryDowngrade();
            prop.TryMortgage(0);
            int netWorthMortgaged = session.CalculatePlayerNetWorth(0);
            Assert.That(netWorthMortgaged, Is.EqualTo(2000 + prop.MortgageValue + 50));
        }

        [Test]
        public void EliminatedPlayerFinalAssetsSnapshot_PreservedInStandingRecord()
        {
            var session = CreateStartedSession(playerCount: 2, startingCash: 1200);

            // Player 1 owns a property before elimination
            PropertyState prop = session.State.Properties.First(p => p.Kind == BoardTileKind.City);
            prop.TryAssignOwner(1);

            int expectedNetWorth = 1200 + prop.PurchasePrice;

            // Eliminate player 1
            session.ApplyAuthoritativeElimination(1, EliminationReason.Bankruptcy);

            // Properties should now be released to bank in active session
            Assert.That(prop.IsOwned, Is.False);
            Assert.That(prop.OwnerPlayerIndex, Is.EqualTo(-1));

            // But the standing record must capture the state at elimination
            MatchResult result = session.GetMatchResult();
            PlayerStanding p1Standing = result.Standings.First(s => s.PlayerIndex == 1);

            Assert.That(p1Standing.Cash, Is.EqualTo(1200));
            Assert.That(p1Standing.PropertiesCount, Is.EqualTo(1));
            Assert.That(p1Standing.NetWorth, Is.EqualTo(expectedNetWorth));
            Assert.That(p1Standing.EliminationReason, Is.EqualTo(EliminationReason.Bankruptcy));
        }

        [Test]
        public void MatchEnding_TickDetectsDisconnectTimeoutAndConcludesMatch()
        {
            var testClock = new TestClock(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
            var session = new LocalMatchSession(
                playerCount: 2,
                startingCash: 1500,
                availableCreatureIds: CreatureIds,
                board: null,
                passStartCash: 200,
                mortgageEnabled: true,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 30,
                clock: testClock);

            session.TrySelectCreature(0, "pip");
            session.TrySelectCreature(1, "bubu");
            session.TryStartMatch();

            // Disconnect player 1
            bool disconnected = session.TryDisconnectPlayer(1);
            Assert.That(disconnected, Is.True);
            Assert.That(session.State.Players[1].IsDisconnected, Is.True);

            // Advance clock past grace period
            testClock.Advance(TimeSpan.FromSeconds(35));
            session.Tick();

            Assert.That(session.State.Phase, Is.EqualTo(MatchPhase.Finished));
            Assert.That(session.State.WinnerPlayerIndex, Is.EqualTo(0));

            MatchResult result = session.GetMatchResult();
            Assert.That(result.WinnerPlayerIndex, Is.EqualTo(0));
            Assert.That(result.Standings[1].EliminationReason, Is.EqualTo(EliminationReason.DisconnectTimeout));
        }
    }
}
