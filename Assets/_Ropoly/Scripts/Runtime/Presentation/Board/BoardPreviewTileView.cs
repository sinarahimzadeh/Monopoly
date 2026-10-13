using Ropoly.Core.Board;
using TMPro;
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

        [SerializeField]
        private TMP_Text _upgradeText;

        public int Index => _index;
        public BoardTileKind Kind => _kind;
        public string ContentId => _contentId;
        public int UpgradeLevel { get; private set; }

        public void Initialize(
            int index,
            BoardTileKind kind,
            string contentId,
            GameObject ownerMarker,
            MeshRenderer ownerMarkerRenderer,
            TMP_Text upgradeText = null)
        {
            _index = index;
            _kind = kind;
            _contentId = contentId;
            _ownerMarker = ownerMarker;
            _ownerMarkerRenderer = ownerMarkerRenderer;
            _upgradeText = upgradeText;
            _ownerMarker.SetActive(false);
            SetUpgradeLevel(0);
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

        public void SetUpgradeLevel(int level)
        {
            UpgradeLevel = Mathf.Max(0, level);
            if (_upgradeText != null)
            {
                if (UpgradeLevel <= 0)
                {
                    _upgradeText.text = string.Empty;
                }
                else if (UpgradeLevel >= 5)
                {
                    _upgradeText.text = "HTL";
                }
                else
                {
                    _upgradeText.text = $"H{UpgradeLevel}";
                }
            }
        }

        public void ClearOwner()
        {
            if (_ownerMarker != null)
            {
                _ownerMarker.SetActive(false);
            }

            SetUpgradeLevel(0);
        }
    }
}
