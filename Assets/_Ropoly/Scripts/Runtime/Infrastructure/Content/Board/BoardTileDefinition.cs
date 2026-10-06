using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Infrastructure.Content.Board
{
    public abstract class BoardTileDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable lowercase ID used by saves, networking, and board references.")]
        private string _contentId;

        [SerializeField]
        private string _displayName;

        [Header("Presentation")]
        [SerializeField]
        [Tooltip("Optional visual. Gameplay never depends on this reference.")]
        private Sprite _artwork;

        public string ContentId => _contentId;
        public string DisplayName => _displayName;
        public Sprite Artwork => _artwork;
        public abstract BoardTileKind Kind { get; }
    }
}
