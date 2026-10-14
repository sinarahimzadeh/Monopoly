using System;
using System.Collections.Generic;

namespace Ropoly.Core.Cards
{
    public sealed class CardDeck
    {
        private readonly Queue<CardSnapshot> _cards;
        private readonly List<CardSnapshot> _originalCards;

        public CardDeck(string deckId, IEnumerable<CardSnapshot> cards)
        {
            DeckId = deckId ?? throw new ArgumentNullException(nameof(deckId));
            if (cards == null)
            {
                throw new ArgumentNullException(nameof(cards));
            }

            _originalCards = new List<CardSnapshot>(cards);
            _cards = new Queue<CardSnapshot>(_originalCards);
        }

        public string DeckId { get; }
        public int RemainingCount => _cards.Count;
        public IReadOnlyList<CardSnapshot> OriginalCards => _originalCards;

        public CardSnapshot Draw()
        {
            if (_cards.Count == 0)
            {
                foreach (CardSnapshot card in _originalCards)
                {
                    _cards.Enqueue(card);
                }
            }

            if (_cards.Count == 0)
            {
                return null;
            }

            CardSnapshot drawn = _cards.Dequeue();
            // Regular cards cycle back to the bottom of the deck.
            // Detention waivers are held by the player until used.
            if (drawn.EffectKind != CardEffectKind.DetentionWaiver)
            {
                _cards.Enqueue(drawn);
            }

            return drawn;
        }

        public void ReturnWaiverCard(CardSnapshot waiverCard)
        {
            if (waiverCard != null)
            {
                _cards.Enqueue(waiverCard);
            }
        }
    }
}
