using System;
using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Core.Board;
using Ropoly.Core.Cards;
using Ropoly.Core.Match;
using Ropoly.Infrastructure.Content.Board;
using UnityEditor;

namespace Ropoly.Tests.EditMode
{
    public sealed class SpecialSpaceTests
    {
        private const string DefaultBoardPath =
            "Assets/_Ropoly/Content/Boards/ClassicWorld/ClassicWorldBoard.asset";

        private static readonly string[] CreatureIds = { "creature-1", "creature-2", "creature-3", "creature-4" };

        private static BoardSnapshot LoadBoard()
        {
            BoardDefinition definition = AssetDatabase.LoadAssetAtPath<BoardDefinition>(DefaultBoardPath);
            Assert.That(definition, Is.Not.Null, "Default world board asset is missing.");
            return definition.CreateSnapshot();
        }

        private static LocalMatchSession CreateSession(
            int playerCount = 2,
            int startingCash = 1500,
            bool vacationRewardEnabled = false,
            int vacationReward = 0,
            IReadOnlyDictionary<string, CardDeck> customDecks = null)
        {
            BoardSnapshot board = LoadBoard();
            LocalMatchSession session = new LocalMatchSession(
                playerCount,
                startingCash,
                CreatureIds,
                board,
                passStartCash: 200,
                mortgageEnabled: true,
                turnDurationSeconds: 60,
                disconnectGraceSeconds: 60,
                clock: null,
                fullSetRentMultiplier: 2,
                vacationRewardEnabled: vacationRewardEnabled,
                vacationReward: vacationReward,
                cardDecks: customDecks);

            for (int i = 0; i < playerCount; i++)
            {
                session.TrySelectCreature(i, CreatureIds[i]);
            }

            session.TryStartMatch();
            return session;
        }

        [Test]
        public void GoToDetention_SendsPlayerDirectlyToDetentionAndEndsTurn()
        {
            LocalMatchSession session = CreateSession();

            // Player 0 rolls to land on tile 30 (Go to Detention)
            // Roll 1: from 0, (6, 5) -> 11
            Assert.That(session.TryRollDice(new FixedDiceRollSource(6, 5), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Player 1 roll: (1, 2) -> 3
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 2), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Player 0 roll 2: from 11, (5, 6) -> 22
            Assert.That(session.TryRollDice(new FixedDiceRollSource(5, 6), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Player 1 roll: (1, 2) -> 6
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 2), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Player 0 roll 3: from 22, (4, 4) -> lands on 30!
            Assert.That(session.TryRollDice(new FixedDiceRollSource(4, 4), out _), Is.True);
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(30));

            Assert.That(session.TryCompleteMovement(), Is.True);

            // Sent to detention (tile 10), turn ends
            PlayerState player0 = session.State.Players[0];
            Assert.That(player0.IsInDetention, Is.True);
            Assert.That(player0.DetentionTurns, Is.Zero);
            Assert.That(player0.BoardPosition, Is.EqualTo(10));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
            Assert.That(session.State.Turn.CanRollAgain, Is.False);
        }

        [Test]
        public void ThreeConsecutiveDoubles_SendsPlayerDirectlyToDetentionWithoutMoving()
        {
            LocalMatchSession session = CreateSession();

            // Double 1: (1, 1) -> moves to 2
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 1), out _), Is.True);
            Assert.That(session.State.Turn.ConsecutiveDoublesCount, Is.EqualTo(1));
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.State.Turn.CanRollAgain, Is.True);

            // Double 2: (2, 2) -> moves to 6
            Assert.That(session.TryRollDice(new FixedDiceRollSource(2, 2), out _), Is.True);
            Assert.That(session.State.Turn.ConsecutiveDoublesCount, Is.EqualTo(2));
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.State.Turn.CanRollAgain, Is.True);

            // Double 3: (3, 3) -> 3rd double penalty!
            Assert.That(session.TryRollDice(new FixedDiceRollSource(3, 3), out _), Is.True);

            PlayerState player0 = session.State.Players[0];
            Assert.That(player0.IsInDetention, Is.True);
            Assert.That(player0.BoardPosition, Is.EqualTo(10));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
            Assert.That(session.State.Turn.ConsecutiveDoublesCount, Is.Zero);
            Assert.That(session.State.Turn.CanRollAgain, Is.False);
        }

        [Test]
        public void Detention_RollDoubles_EscapesFreelyAndMovesWithoutExtraTurn()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);

            Assert.That(session.State.Players[0].IsInDetention, Is.True);
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(10));

            // Player rolls doubles (4, 4) in detention
            Assert.That(session.TryRollDice(new FixedDiceRollSource(4, 4), out DiceRoll roll), Is.True);
            Assert.That(roll.IsDouble, Is.True);

            // Escaped!
            Assert.That(session.State.Players[0].IsInDetention, Is.False);
            Assert.That(session.State.Turn.RolledDoublesToEscapeDetention, Is.True);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingMovement));

            // Moves 8 spaces (from 10 to 18)
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(18));
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(18));

            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
            Assert.That(session.State.Turn.CanRollAgain, Is.False, "Escaping jail with doubles should not give an extra roll.");
        }

        [Test]
        public void Detention_RollNonDoubles_IncrementsTurnCounterAndEndsTurnWithoutMoving()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);

            // Turn 1 in detention: rolls non-doubles (2, 3)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(2, 3), out _), Is.True);
            Assert.That(session.State.Players[0].IsInDetention, Is.True);
            Assert.That(session.State.Players[0].DetentionTurns, Is.EqualTo(1));
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(10));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));

            // Advance to Player 1, Player 1 takes turn, then advance back to Player 0
            Assert.That(session.TryAdvanceTurn(), Is.True);
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 2), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Turn 2 in detention: rolls non-doubles (1, 4)
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 4), out _), Is.True);
            Assert.That(session.State.Players[0].IsInDetention, Is.True);
            Assert.That(session.State.Players[0].DetentionTurns, Is.EqualTo(2));
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(10));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void Detention_ThirdTurnNonDoubles_ForcesBailPaymentAndReleasesToMove()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);
            session.State.MutablePlayers[0].IncrementDetentionTurns();
            session.State.MutablePlayers[0].IncrementDetentionTurns();
            Assert.That(session.State.Players[0].DetentionTurns, Is.EqualTo(2));

            int cashBefore = session.State.Players[0].Cash;

            // 3rd failed roll: (1, 2)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 2), out _), Is.True);

            // Forced bail of $50 deducted, released, and ready to move!
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore - 50));
            Assert.That(session.State.Players[0].IsInDetention, Is.False);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingMovement));

            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(13)); // 10 + 3 = 13
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(13));
        }

        [Test]
        public void Detention_ThirdTurnNonDoubles_EntersDebtResolutionIfCannotAffordBail()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);
            session.State.MutablePlayers[0].IncrementDetentionTurns();
            session.State.MutablePlayers[0].IncrementDetentionTurns();

            // Set player cash to $20 (less than $50 bail)
            session.State.MutablePlayers[0].TrySpendCash(1480);
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(20));

            // Give player an owned property to liquidate
            session.State.Properties[0].TryAssignOwner(0);

            // 3rd roll fails doubles
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 2), out _), Is.True);

            // Enters debt resolution owing $50 bail to the bank
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.DebtResolution));
            Assert.That(session.State.Turn.RentPayment, Is.Not.Null);
            Assert.That(session.State.Turn.RentPayment.Amount, Is.EqualTo(50));
            Assert.That(session.State.Turn.RentPayment.OwnerPlayerIndex, Is.EqualTo(PropertyState.UnownedPlayerIndex));

            // Mortgage property to raise cash
            Assert.That(session.TryMortgageProperty(session.State.Properties[0].TileIndex), Is.EqualTo(AssetLiquidationResult.Success));
            Assert.That(session.State.Players[0].Cash, Is.GreaterThanOrEqualTo(50));

            // Pay debt
            Assert.That(session.TryPayCurrentDebt(), Is.EqualTo(RentPaymentResult.Success));
            Assert.That(session.State.Players[0].IsInDetention, Is.False);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingMovement));

            // Moves forward 3 spaces
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(13));
        }

        [Test]
        public void Detention_PayBailAtStartOfTurn_AllowsNormalMovement()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);

            Assert.That(session.State.Players[0].IsInDetention, Is.True);
            int cashBefore = session.State.Players[0].Cash;

            // Pay bail before rolling
            Assert.That(session.TryPayDetentionBail(), Is.True);
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore - 50));
            Assert.That(session.State.Players[0].IsInDetention, Is.False);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));

            // Now roll and move normally
            Assert.That(session.TryRollDice(new FixedDiceRollSource(2, 3), out _), Is.True);
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(15));
        }

        [Test]
        public void Detention_UseWaiverCard_AllowsNormalMovement()
        {
            LocalMatchSession session = CreateSession();
            session.SendPlayerToDetention(0);
            session.State.MutablePlayers[0].AddDetentionWaiver();

            Assert.That(session.State.Players[0].HeldDetentionWaivers, Is.EqualTo(1));
            Assert.That(session.State.Players[0].IsInDetention, Is.True);

            // Use waiver card
            Assert.That(session.TryUseDetentionWaiver(), Is.True);
            Assert.That(session.State.Players[0].HeldDetentionWaivers, Is.Zero);
            Assert.That(session.State.Players[0].IsInDetention, Is.False);
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));

            // Rolls and moves normally
            Assert.That(session.TryRollDice(new FixedDiceRollSource(3, 4), out _), Is.True);
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(17));
        }

        [Test]
        public void Detention_PlayerCanCollectRentWhileInDetention()
        {
            LocalMatchSession session = CreateSession();

            // Give Player 0 ownership of Delhi (tile 1)
            session.State.Properties[0].TryAssignOwner(0);
            int rentAmount = session.State.Properties[0].RentValues[0];

            // Send Player 0 to detention
            session.SendPlayerToDetention(0);
            int p0CashBefore = session.State.Players[0].Cash;
            int p1CashBefore = session.State.Players[1].Cash;

            // Advance to Player 1's turn
            Assert.That(session.TryAdvanceTurn(), Is.True);
            Assert.That(session.State.Turn.CurrentPlayerIndex, Is.EqualTo(1));

            // Player 1 rolls (0, 1) -> moves to tile 1 (Delhi)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 1), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Player 0 receives rent despite being in detention!
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(p0CashBefore + rentAmount));
            Assert.That(session.State.Players[1].Cash, Is.EqualTo(p1CashBefore - rentAmount));
        }

        [Test]
        public void FeeSpace_DeductsFeeDirectlyWhenPlayerHasCash()
        {
            LocalMatchSession session = CreateSession();

            // Tile 4 is Income Fee ($200)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 3), out _), Is.True);
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(4));

            Assert.That(session.TryCompleteMovement(), Is.True);

            Assert.That(session.State.Players[0].Cash, Is.EqualTo(1300)); // 1500 - 200
            Assert.That(session.State.Turn.PaidFeeAmount, Is.EqualTo(200));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void FeeSpace_TriggersDebtResolutionWhenPlayerCannotAffordFee()
        {
            LocalMatchSession session = CreateSession();

            // Set Player 0 cash to $100
            session.State.MutablePlayers[0].TrySpendCash(1400);
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(100));

            // Give property to Player 0
            session.State.Properties[0].TryAssignOwner(0);

            // Move to Income Fee (tile 4, $200)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(2, 2), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.DebtResolution));
            Assert.That(session.State.Turn.RentPayment.Amount, Is.EqualTo(200));
            Assert.That(session.State.Turn.RentPayment.OwnerPlayerIndex, Is.EqualTo(PropertyState.UnownedPlayerIndex));

            // Mortgage property to raise cash
            session.TryMortgageProperty(session.State.Properties[0].TileIndex);
            Assert.That(session.State.Players[0].Cash, Is.GreaterThanOrEqualTo(200));

            Assert.That(session.TryPayCurrentDebt(), Is.EqualTo(RentPaymentResult.Success));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void VacationSpace_CollectsAccumulatedPoolWhenEnabled()
        {
            LocalMatchSession session = CreateSession(vacationRewardEnabled: true, vacationReward: 100);

            Assert.That(session.VacationPool, Is.EqualTo(100));

            // Player 0 pays Income Fee of $200
            Assert.That(session.TryRollDice(new FixedDiceRollSource(2, 2), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Vacation pool received $200 fee -> now $300!
            Assert.That(session.VacationPool, Is.EqualTo(300));
            Assert.That(session.TryAdvanceTurn(), Is.True);

            // Player 1 moves to Vacation (tile 20): from 0 to 20
            // Roll (5, 5) -> 10, then roll (5, 5) -> 20!
            Assert.That(session.TryRollDice(new FixedDiceRollSource(5, 5), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);
            Assert.That(session.State.Turn.CanRollAgain, Is.True);

            Assert.That(session.TryRollDice(new FixedDiceRollSource(5, 5), out _), Is.True);
            Assert.That(session.TryBeginMovement(out PlayerMovement m), Is.True);
            Assert.That(m.DestinationIndex, Is.EqualTo(20));

            int p1CashBefore = session.State.Players[1].Cash;
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Player 1 collects $300 vacation pool!
            Assert.That(session.State.Players[1].Cash, Is.EqualTo(p1CashBefore + 300));
            Assert.That(session.State.Turn.VacationRewardAwarded, Is.EqualTo(300));
            Assert.That(session.VacationPool, Is.EqualTo(100), "Pool should reset to base reward.");
        }

        [Test]
        public void OpportunityCard_MoveToTile_CollectsPassStartIfPassingStart()
        {
            CardSnapshot card = new CardSnapshot(
                "card-london",
                DefaultCardDecks.LocalOpportunityDeckId,
                "Express Highway to London",
                "Advance to London. If you pass Start, collect $200.",
                CardEffectKind.MoveToTile,
                targetTileIndex: 34,
                canCollectPassStart: true);

            CardDeck customDeck = new CardDeck(DefaultCardDecks.LocalOpportunityDeckId, new[] { card });
            Dictionary<string, CardDeck> decks = new Dictionary<string, CardDeck>(StringComparer.Ordinal)
            {
                { DefaultCardDecks.LocalOpportunityDeckId, customDeck }
            };

            LocalMatchSession session = CreateSession(customDecks: decks);

            // Move Player 0 to tile 7 (Local Opportunity)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(3, 4), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Player draws card and moves directly to London (tile 34)
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(34));
            Assert.That(session.State.Turn.DrawnCard, Is.SameAs(card));
        }

        [Test]
        public void OpportunityCard_AdvanceToStart_Collects200AndSetsPositionToZero()
        {
            CardSnapshot card = new CardSnapshot(
                "opp-1",
                DefaultCardDecks.LocalOpportunityDeckId,
                "High-Speed Transit",
                "Advance directly to Start. Collect $200.",
                CardEffectKind.MoveToTile,
                targetTileIndex: 0,
                canCollectPassStart: true);

            CardDeck customDeck = new CardDeck(DefaultCardDecks.LocalOpportunityDeckId, new[] { card });
            Dictionary<string, CardDeck> decks = new Dictionary<string, CardDeck>(StringComparer.Ordinal)
            {
                { DefaultCardDecks.LocalOpportunityDeckId, customDeck }
            };

            LocalMatchSession session = CreateSession(customDecks: decks);
            int cashBefore = session.State.Players[0].Cash;

            // Move to tile 7 (Local Opportunity)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(3, 4), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Player position should be 0 (Start) and cash increased by $200
            Assert.That(session.State.Players[0].BoardPosition, Is.EqualTo(0));
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore + 200));
            Assert.That(session.State.Turn.DrawnCard, Is.SameAs(card));
            Assert.That(session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingTurnEnd));
        }

        [Test]
        public void OpportunityCard_PropertyRepair_ChargesPerHouseAndHotel()
        {
            CardSnapshot repairCard = new CardSnapshot(
                "card-repair",
                DefaultCardDecks.LocalOpportunityDeckId,
                "Metropolitan Renovation",
                "Make repairs: $25 per house, $100 per hotel.",
                CardEffectKind.PropertyRepair,
                costPerHouse: 25,
                costPerHotel: 100);

            CardDeck customDeck = new CardDeck(DefaultCardDecks.LocalOpportunityDeckId, new[] { repairCard });
            Dictionary<string, CardDeck> decks = new Dictionary<string, CardDeck>(StringComparer.Ordinal)
            {
                { DefaultCardDecks.LocalOpportunityDeckId, customDeck }
            };

            LocalMatchSession session = CreateSession(customDecks: decks);

            // Give Player 0 two properties: one with 2 houses, one with 1 hotel (level 5)
            session.State.Properties[0].TryAssignOwner(0);
            session.State.Properties[0].TryUpgrade(); // level 1
            session.State.Properties[0].TryUpgrade(); // level 2 (2 houses)

            session.State.Properties[1].TryAssignOwner(0);
            for (int i = 0; i < 5; i++)
            {
                session.State.Properties[1].TryUpgrade(); // level 5 (1 hotel)
            }

            // Expected repair cost: (2 * 25) + (1 * 100) = $150
            int cashBefore = session.State.Players[0].Cash;

            // Move to tile 7 (Local Opportunity)
            Assert.That(session.TryRollDice(new FixedDiceRollSource(3, 4), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore - 150));
            Assert.That(session.State.Turn.PaidFeeAmount, Is.EqualTo(150));
        }

        [Test]
        public void WorldEventCard_BankError_AwardsCash()
        {
            CardSnapshot grantCard = new CardSnapshot(
                "card-bank-error",
                DefaultCardDecks.WorldEventDeckId,
                "Bank Error",
                "Collect $200.",
                CardEffectKind.CashBank,
                amount: 200);

            CardDeck customDeck = new CardDeck(DefaultCardDecks.WorldEventDeckId, new[] { grantCard });
            Dictionary<string, CardDeck> decks = new Dictionary<string, CardDeck>(StringComparer.Ordinal)
            {
                { DefaultCardDecks.WorldEventDeckId, customDeck }
            };

            LocalMatchSession session = CreateSession(customDecks: decks);

            int cashBefore = session.State.Players[0].Cash;

            // Tile 2 is World Event
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 1), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            Assert.That(session.State.Players[0].Cash, Is.EqualTo(cashBefore + 200));
            Assert.That(session.State.Turn.DrawnCard, Is.SameAs(grantCard));
        }

        [Test]
        public void WorldEventCard_CollectFromAllPlayers_TransfersCash()
        {
            CardSnapshot birthdayCard = new CardSnapshot(
                "card-birthday",
                DefaultCardDecks.WorldEventDeckId,
                "Birthday Reception",
                "Collect $10 from every player.",
                CardEffectKind.CashFromEachPlayer,
                amount: 10);

            CardDeck customDeck = new CardDeck(DefaultCardDecks.WorldEventDeckId, new[] { birthdayCard });
            Dictionary<string, CardDeck> decks = new Dictionary<string, CardDeck>(StringComparer.Ordinal)
            {
                { DefaultCardDecks.WorldEventDeckId, customDeck }
            };

            LocalMatchSession session = CreateSession(playerCount: 4, customDecks: decks);

            int p0Before = session.State.Players[0].Cash;
            int p1Before = session.State.Players[1].Cash;
            int p2Before = session.State.Players[2].Cash;
            int p3Before = session.State.Players[3].Cash;

            // Tile 2 is World Event
            Assert.That(session.TryRollDice(new FixedDiceRollSource(1, 1), out _), Is.True);
            Assert.That(session.TryBeginMovement(out _), Is.True);
            Assert.That(session.TryCompleteMovement(), Is.True);

            // Player 0 receives $10 from each of the 3 other players (total $30)
            Assert.That(session.State.Players[0].Cash, Is.EqualTo(p0Before + 30));
            Assert.That(session.State.Players[1].Cash, Is.EqualTo(p1Before - 10));
            Assert.That(session.State.Players[2].Cash, Is.EqualTo(p2Before - 10));
            Assert.That(session.State.Players[3].Cash, Is.EqualTo(p3Before - 10));
        }

        private sealed class FixedDiceRollSource : IDiceRollSource
        {
            private readonly Queue<int> _values;

            public FixedDiceRollSource(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int NextDieValue()
            {
                return _values.Dequeue();
            }
        }
    }
}
