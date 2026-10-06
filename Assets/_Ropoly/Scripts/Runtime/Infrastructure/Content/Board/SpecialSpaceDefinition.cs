using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    [CreateAssetMenu(fileName = "SpecialSpace", menuName = "Ropoly/Board/Special Space", order = 23)]
    public sealed class SpecialSpaceDefinition : BoardTileDefinition
    {
        [Header("Behavior")]
        [SerializeField]
        private SpecialSpaceKind _specialKind;

        [SerializeField]
        [Tooltip("Required only for Event spaces. This references a future card-deck ID.")]
        private string _eventDeckId;

        [SerializeField]
        [Min(0)]
        [Tooltip("Required only for Fee spaces.")]
        private int _feeAmount;

        public override BoardTileKind Kind => BoardTileKind.Special;

        public SpecialSpaceSnapshot CreateSnapshot()
        {
            return new SpecialSpaceSnapshot(
                ContentId,
                DisplayName,
                _specialKind,
                _eventDeckId,
                _feeAmount);
        }

        private void OnValidate()
        {
            _feeAmount = Mathf.Clamp(_feeAmount, 0, BoardContentLimits.MaximumMoneyValue);
        }
    }
}
