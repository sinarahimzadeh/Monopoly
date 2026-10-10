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

        [SerializeField]
        private GameObject _ownerMarker;

        [SerializeField]
        private MeshRenderer _ownerMarkerRenderer;

        public int Index => _index;
        public BoardTileKind Kind => _kind;
        public string ContentId => _contentId;

        public void Initialize(
            int index,
            BoardTileKind kind,
            string contentId,
            GameObject ownerMarker,
            MeshRenderer ownerMarkerRenderer)
        {
            _index = index;
            _kind = kind;
            _contentId = contentId;
            _ownerMarker = ownerMarker;
            _ownerMarkerRenderer = ownerMarkerRenderer;
            _ownerMarker.SetActive(false);
        }

        public void SetOwnerMaterial(Material material)
        {
            if (_ownerMarker == null || _ownerMarkerRenderer == null || material == null)
            {
                return;
            }

            _ownerMarkerRenderer.sharedMaterial = material;
            _ownerMarker.SetActive(true);
        }
    }
}
