using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "City", menuName = "Ropoly/Board/City", order = 20)]
    public sealed class CityDefinition : BoardTileDefinition
    {
        [Header("Economy")]
        [SerializeField]
        [Min(1)]
        private int _purchasePrice = 60;

        [SerializeField]
        [Min(0)]
        private int _mortgageValue = 30;

        [SerializeField]
        [Min(0)]
        private int _upgradeCost = 50;

        [SerializeField]
        [Tooltip("Rent at upgrade levels 0 through 5. Level 0 is the base rent.")]
        private int[] _rentByUpgradeLevel = { 2, 10, 30, 90, 160, 250 };

        public override BoardTileKind Kind => BoardTileKind.City;

        public CitySnapshot CreateSnapshot()
        {
            return new CitySnapshot(
                ContentId,
                DisplayName,
                _purchasePrice,
                _mortgageValue,
                _upgradeCost,
                _rentByUpgradeLevel);
        }

        private void OnValidate()
        {
            _purchasePrice = Mathf.Clamp(_purchasePrice, 1, BoardContentLimits.MaximumMoneyValue);
            _mortgageValue = Mathf.Clamp(_mortgageValue, 0, _purchasePrice);
            _upgradeCost = Mathf.Clamp(_upgradeCost, 0, BoardContentLimits.MaximumMoneyValue);
            _rentByUpgradeLevel = EnsureLength(
                _rentByUpgradeLevel,
                BoardContentLimits.CityRentLevelCount);

            for (int index = 0; index < _rentByUpgradeLevel.Length; index++)
            {
                _rentByUpgradeLevel[index] = Mathf.Clamp(
                    _rentByUpgradeLevel[index],
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
