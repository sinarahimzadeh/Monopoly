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

        private PropertyStructureView _structureView;
        private MeshRenderer _ownerBandRenderer;
        private Material _neutralOwnerBandMaterial;
        private TMP_Text _ownerBandUpgradeText;
        private bool _isOwned;

        public int Index => _index;
        public BoardTileKind Kind => _kind;
        public string ContentId => _contentId;
        public int UpgradeLevel { get; private set; }
        public PropertyStructureView StructureView => _structureView;

        public void Initialize(
            int index,
            BoardTileKind kind,
            string contentId,
            GameObject ownerMarker,
            MeshRenderer ownerMarkerRenderer,
            TMP_Text upgradeText = null,
            PropertyStructureView structureView = null)
        {
            _index = index;
            _kind = kind;
            _contentId = contentId;
            _ownerMarker = ownerMarker;
            _ownerMarkerRenderer = ownerMarkerRenderer;
            _upgradeText = upgradeText;
            _structureView = structureView;

            if (_ownerMarker != null)
            {
                _ownerMarker.SetActive(false);
            }

            if (_structureView != null)
            {
                _structureView.Clear();
            }

            SetUpgradeLevel(0);
        }

        public void SetOwnerBand(
            MeshRenderer bandRenderer,
            Material neutralMaterial,
            TMP_Text upgradeText)
        {
            _ownerBandRenderer = bandRenderer;
            _neutralOwnerBandMaterial = neutralMaterial;
            _ownerBandUpgradeText = upgradeText;
            if (_ownerBandRenderer != null)
            {
                _ownerBandRenderer.sharedMaterial = neutralMaterial;
            }

            RefreshOwnerBandUpgradeText();
        }

        public void SetOwnerMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            _isOwned = true;
            if (_ownerBandRenderer != null)
            {
                _ownerBandRenderer.sharedMaterial = material;
            }

            if (_structureView != null)
            {
                _structureView.SetOwnerMaterial(material);
                if (_ownerMarker != null)
                {
                    _ownerMarker.SetActive(false);
                }
            }
            else if (_ownerBandRenderer == null && _ownerMarker != null && _ownerMarkerRenderer != null)
            {
                _ownerMarkerRenderer.sharedMaterial = material;
                _ownerMarker.SetActive(true);
            }

            RefreshOwnerBandUpgradeText();
        }

        public void SetUpgradeLevel(int level)
        {
            UpgradeLevel = Mathf.Max(0, level);

            if (_structureView != null)
            {
                _structureView.SetUpgradeLevel(UpgradeLevel);
            }

            if (_upgradeText != null)
            {
                _upgradeText.text = string.Empty;
            }

            RefreshOwnerBandUpgradeText();
        }

        public void ClearOwner()
        {
            _isOwned = false;
            if (_ownerBandRenderer != null)
            {
                _ownerBandRenderer.sharedMaterial = _neutralOwnerBandMaterial;
            }

            if (_structureView != null)
            {
                _structureView.Clear();
            }

            if (_ownerMarker != null)
            {
                _ownerMarker.SetActive(false);
            }

            SetUpgradeLevel(0);
        }

        private void RefreshOwnerBandUpgradeText()
        {
            if (_ownerBandUpgradeText == null)
            {
                return;
            }

            if (!_isOwned || UpgradeLevel == 0)
            {
                _ownerBandUpgradeText.text = string.Empty;
            }
            else if (UpgradeLevel >= 5)
            {
                _ownerBandUpgradeText.text = "HOTEL";
            }
            else
            {
                _ownerBandUpgradeText.text = UpgradeLevel == 1
                    ? "1 HOUSE"
                    : $"{UpgradeLevel} HOUSES";
            }
        }
    }
}
