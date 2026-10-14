using System.Collections.Generic;
using Ropoly.Core.Board;

namespace Ropoly.Core.Cards
{
    public static class DefaultCardDecks
    {
        public const string LocalOpportunityDeckId = "deck-local-opportunity";
        public const string WorldEventDeckId = "deck-world-event";

        public static CardDeck CreateLocalOpportunityDeck()
        {
            return new CardDeck(LocalOpportunityDeckId, CreateLocalOpportunityCards());
        }

        public static CardDeck CreateWorldEventDeck()
        {
            return new CardDeck(WorldEventDeckId, CreateWorldEventCards());
        }

        public static Dictionary<string, CardDeck> CreateDefaultDecks()
        {
            return new Dictionary<string, CardDeck>(System.StringComparer.Ordinal)
            {
                { LocalOpportunityDeckId, CreateLocalOpportunityDeck() },
                { WorldEventDeckId, CreateWorldEventDeck() },
            };
        }

        public static IReadOnlyList<CardSnapshot> CreateLocalOpportunityCards()
        {
            return new[]
            {
                new CardSnapshot(
                    "opp-1",
                    LocalOpportunityDeckId,
                    "High-Speed Transit",
                    "Advance directly to Start. Collect $200.",
                    CardEffectKind.MoveToTile,
                    targetTileIndex: 0,
                    canCollectPassStart: true),
                new CardSnapshot(
                    "opp-2",
                    LocalOpportunityDeckId,
                    "Flight Connection",
                    "Advance to the nearest Airport. If owned, pay double rent.",
                    CardEffectKind.MoveToNearest,
                    targetTileKind: BoardTileKind.Airport,
                    rentMultiplier: 2),
                new CardSnapshot(
                    "opp-3",
                    LocalOpportunityDeckId,
                    "Grid Interconnect",
                    "Advance to the nearest Utility. If owned, pay 10 times the dice roll.",
                    CardEffectKind.MoveToNearest,
                    targetTileKind: BoardTileKind.Utility,
                    rentMultiplier: 10),
                new CardSnapshot(
                    "opp-4",
                    LocalOpportunityDeckId,
                    "Border Control Issue",
                    "Go directly to Detention. Do not pass Start, do not collect $200.",
                    CardEffectKind.GoToDetention),
                new CardSnapshot(
                    "opp-5",
                    LocalOpportunityDeckId,
                    "Diplomatic Waiver",
                    "Get Out of Detention Free. This card may be kept until needed or traded.",
                    CardEffectKind.DetentionWaiver),
                new CardSnapshot(
                    "opp-6",
                    LocalOpportunityDeckId,
                    "Detour Ahead",
                    "Move back 3 spaces.",
                    CardEffectKind.MoveRelative,
                    relativeSteps: -3),
                new CardSnapshot(
                    "opp-7",
                    LocalOpportunityDeckId,
                    "Express Highway to London",
                    "Advance to London. If you pass Start, collect $200.",
                    CardEffectKind.MoveToTile,
                    targetTileIndex: 34,
                    canCollectPassStart: true),
                new CardSnapshot(
                    "opp-8",
                    LocalOpportunityDeckId,
                    "Metropolitan Renovation",
                    "Make general repairs: Pay $25 per house and $100 per hotel owned.",
                    CardEffectKind.PropertyRepair,
                    costPerHouse: 25,
                    costPerHotel: 100),
                new CardSnapshot(
                    "opp-9",
                    LocalOpportunityDeckId,
                    "Traffic Violation",
                    "Pay a speeding fine of $50 to the Bank.",
                    CardEffectKind.CashBank,
                    amount: -50),
                new CardSnapshot(
                    "opp-10",
                    LocalOpportunityDeckId,
                    "Venture Dividend",
                    "Bank pays you a dividend of $150.",
                    CardEffectKind.CashBank,
                    amount: 150),
                new CardSnapshot(
                    "opp-11",
                    LocalOpportunityDeckId,
                    "Host City Gala",
                    "You host the municipal festival. Collect $20 from every other player.",
                    CardEffectKind.CashFromEachPlayer,
                    amount: 20),
                new CardSnapshot(
                    "opp-12",
                    LocalOpportunityDeckId,
                    "Philanthropy Banquet",
                    "Treat other delegates: Pay $25 to every other player.",
                    CardEffectKind.CashToEachPlayer,
                    amount: 25),
            };
        }

        public static IReadOnlyList<CardSnapshot> CreateWorldEventCards()
        {
            return new[]
            {
                new CardSnapshot(
                    "event-1",
                    WorldEventDeckId,
                    "Bank Accounting Correction",
                    "Bank error in your favor. Collect $200.",
                    CardEffectKind.CashBank,
                    amount: 200),
                new CardSnapshot(
                    "event-2",
                    WorldEventDeckId,
                    "Trade Surplus Grant",
                    "Government export incentive. Collect $100.",
                    CardEffectKind.CashBank,
                    amount: 100),
                new CardSnapshot(
                    "event-3",
                    WorldEventDeckId,
                    "Consular Release Order",
                    "Get Out of Detention Free. This card may be kept until needed or traded.",
                    CardEffectKind.DetentionWaiver),
                new CardSnapshot(
                    "event-4",
                    WorldEventDeckId,
                    "Extradition Warrant",
                    "Go directly to Detention. Do not pass Start, do not collect $200.",
                    CardEffectKind.GoToDetention),
                new CardSnapshot(
                    "event-5",
                    WorldEventDeckId,
                    "Global Infrastructure Assessment",
                    "Building safety inspection: Pay $40 per house and $115 per hotel owned.",
                    CardEffectKind.PropertyRepair,
                    costPerHouse: 40,
                    costPerHotel: 115),
                new CardSnapshot(
                    "event-6",
                    WorldEventDeckId,
                    "Tax Clearance Refund",
                    "Annual corporate tax rebate. Collect $50.",
                    CardEffectKind.CashBank,
                    amount: 50),
                new CardSnapshot(
                    "event-7",
                    WorldEventDeckId,
                    "Holiday Savings Maturity",
                    "Your tourism fund matures. Collect $100.",
                    CardEffectKind.CashBank,
                    amount: 100),
                new CardSnapshot(
                    "event-8",
                    WorldEventDeckId,
                    "Customs Tariff Surcharge",
                    "Pay import duty of $50 to the Bank.",
                    CardEffectKind.CashBank,
                    amount: -50),
                new CardSnapshot(
                    "event-9",
                    WorldEventDeckId,
                    "Emergency Medical Grant",
                    "Medical insurance pays out. Collect $50.",
                    CardEffectKind.CashBank,
                    amount: 50),
                new CardSnapshot(
                    "event-10",
                    WorldEventDeckId,
                    "Research & Development Subsidy",
                    "Collect $25 innovation grant.",
                    CardEffectKind.CashBank,
                    amount: 25),
                new CardSnapshot(
                    "event-11",
                    WorldEventDeckId,
                    "Municipal Audit Penalty",
                    "Discrepancies found in records. Pay $100 to the Bank.",
                    CardEffectKind.CashBank,
                    amount: -100),
                new CardSnapshot(
                    "event-12",
                    WorldEventDeckId,
                    "Birthday Reception",
                    "It's your birthday! Collect $10 from every player.",
                    CardEffectKind.CashFromEachPlayer,
                    amount: 10),
            };
        }
    }
}
