using System;
using Ropoly.Core.Board;

namespace Ropoly.Core.Cards
{
    [Serializable]
    public sealed class CardSnapshot
    {
        public CardSnapshot(
            string cardId,
            string deckId,
            string title,
            string description,
            CardEffectKind effectKind,
            int amount = 0,
            int targetTileIndex = -1,
            int relativeSteps = 0,
            BoardTileKind targetTileKind = BoardTileKind.Special,
            int costPerHouse = 0,
            int costPerHotel = 0,
            bool canCollectPassStart = true,
            int rentMultiplier = 1)
        {
            CardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            DeckId = deckId ?? throw new ArgumentNullException(nameof(deckId));
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            EffectKind = effectKind;
            Amount = amount;
            TargetTileIndex = targetTileIndex;
            RelativeSteps = relativeSteps;
            TargetTileKind = targetTileKind;
            CostPerHouse = costPerHouse;
            CostPerHotel = costPerHotel;
            CanCollectPassStart = canCollectPassStart;
            RentMultiplier = rentMultiplier;
        }

        public string CardId { get; }
        public string DeckId { get; }
        public string Title { get; }
        public string Description { get; }
        public CardEffectKind EffectKind { get; }
        public int Amount { get; }
        public int TargetTileIndex { get; }
        public int RelativeSteps { get; }
        public BoardTileKind TargetTileKind { get; }
        public int CostPerHouse { get; }
        public int CostPerHotel { get; }
        public bool CanCollectPassStart { get; }
        public int RentMultiplier { get; }
    }
}
