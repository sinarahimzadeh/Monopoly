using Ropoly.Core.Board;
using UnityEngine;

namespace Ropoly.Presentation.Board
{
    [DisallowMultipleComponent]
    public sealed class BoardPreviewTileView : MonoBehaviour
    {
        [SerializeField]
        private int _index;

        [SerializeField]
        private BoardTileKind _kind;

        [SerializeField]
        private string _contentId;

        public int Index => _index;
        public BoardTileKind Kind => _kind;
        public string ContentId => _contentId;

        public void Initialize(int index, BoardTileKind kind, string contentId)
        {
            _index = index;
            _kind = kind;
            _contentId = contentId;
        }
    }
}
