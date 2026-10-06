using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "Airport", menuName = "Ropoly/Board/Airport", order = 21)]
    public sealed class AirportDefinition : BoardTileDefinition
    {
        [Header("Economy")]
        [SerializeField]
        [Min(1)]
        private int _purchasePrice = 200;

        [SerializeField]
        [Min(0)]
        private int _mortgageValue = 100;

        [SerializeField]
        [Tooltip("Rent when the owner controls 1, 2, 3, or 4 airports.")]
        private int[] _rentByOwnedCount = { 25, 50, 100, 200 };

        public override BoardTileKind Kind => BoardTileKind.Airport;

        public AirportSnapshot CreateSnapshot()
        {
            return new AirportSnapshot(
                ContentId,
                DisplayName,
                _purchasePrice,
                _mortgageValue,
                _rentByOwnedCount);
        }

        private void OnValidate()
        {
            _purchasePrice = Mathf.Clamp(_purchasePrice, 1, BoardContentLimits.MaximumMoneyValue);
            _mortgageValue = Mathf.Clamp(_mortgageValue, 0, _purchasePrice);
            _rentByOwnedCount = EnsureLength(
                _rentByOwnedCount,
                BoardContentLimits.AirportRentLevelCount);

            for (int index = 0; index < _rentByOwnedCount.Length; index++)
            {
                _rentByOwnedCount[index] = Mathf.Clamp(
                    _rentByOwnedCount[index],
                    0,
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
