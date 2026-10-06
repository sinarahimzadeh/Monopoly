using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "Utility", menuName = "Ropoly/Board/Utility", order = 22)]
    public sealed class UtilityDefinition : BoardTileDefinition
    {
        [Header("Economy")]
        [SerializeField]
        [Min(1)]
        private int _purchasePrice = 150;

        [SerializeField]
        [Min(0)]
        private int _mortgageValue = 75;

        [SerializeField]
        [Tooltip("Dice-total rent multipliers when the owner controls 1 or 2 utilities.")]
        private int[] _rentMultiplierByOwnedCount = { 4, 10 };

        public override BoardTileKind Kind => BoardTileKind.Utility;

        public UtilitySnapshot CreateSnapshot()
        {
            return new UtilitySnapshot(
                ContentId,
                DisplayName,
                _purchasePrice,
                _mortgageValue,
                _rentMultiplierByOwnedCount);
        }

        private void OnValidate()
        {
            _purchasePrice = Mathf.Clamp(_purchasePrice, 1, BoardContentLimits.MaximumMoneyValue);
            _mortgageValue = Mathf.Clamp(_mortgageValue, 0, _purchasePrice);
            _rentMultiplierByOwnedCount = EnsureLength(
                _rentMultiplierByOwnedCount,
                BoardContentLimits.UtilityRentMultiplierCount);

            for (int index = 0; index < _rentMultiplierByOwnedCount.Length; index++)
            {
                _rentMultiplierByOwnedCount[index] = Mathf.Clamp(
                    _rentMultiplierByOwnedCount[index],
                    1,
                    BoardContentLimits.MaximumMoneyValue);
            }
        }

        private static int[] EnsureLength(int[] values, int requiredLength)
        {
            int[] resized = new int[requiredLength];
            if (values != null)
            {
                System.Array.Copy(values, resized, Mathf.Min(values.Length, resized.Length));
            }

            return resized;
        }
    }
}
