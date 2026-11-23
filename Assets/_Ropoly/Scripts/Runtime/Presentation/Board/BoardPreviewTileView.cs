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
        private string _bandPriceText;
        private bool _isOwned;

        [Header("Dynamic Shake & Bounce Physics")]
        [SerializeField]
        private Transform _tileRoot;

        [Tooltip("Overall cushion intensity.")]
        [SerializeField]
        [Range(0.02f, 0.30f)]
        private float _shakeIntensity = 0.08f;

        [Tooltip("Natural oscillation speed.")]
        [SerializeField]
        [Range(8f, 30f)]
        private float _shakeFrequency = 16f;

        [Tooltip("Decay speed of the cushion (higher settles faster).")]
        [SerializeField]
        [Range(4f, 15f)]
        private float _shakeDecay = 7.5f;

        [Tooltip("Vertical dip compression depth (meters).")]
        [SerializeField]
        [Range(0.01f, 0.15f)]
        private float _verticalDip = 0.05f;

        private Vector3 _rootBasePosition;
        private Vector3 _rootBaseScale = Vector3.one;
        private Vector3 _inward = Vector3.forward;
        private Vector3 _perp = Vector3.left;
        private float _shakeTrauma;
        private float _shakeTimer;
        private bool _isShaking;

        public int Index => _index;
        public BoardTileKind Kind => _kind;
        public string ContentId => _contentId;
        public bool IsOwned => _isOwned;
        public int UpgradeLevel { get; private set; }
        public PropertyStructureView StructureView => _structureView;

        public void Initialize(
            int index,
            BoardTileKind kind,
            string contentId,
            GameObject ownerMarker,
            MeshRenderer ownerMarkerRenderer,
            TMP_Text upgradeText = null,
            PropertyStructureView structureView = null,
            Transform tileRoot = null,
            Vector3 inward = default)
        {
            _index = index;
            _kind = kind;
            _contentId = contentId;
            _ownerMarker = ownerMarker;
            _ownerMarkerRenderer = ownerMarkerRenderer;
            _upgradeText = upgradeText;
            _structureView = structureView;
            _tileRoot = tileRoot != null ? tileRoot : (transform.parent != null ? transform.parent : transform);
            _rootBasePosition = _tileRoot.localPosition;
            _rootBaseScale = _tileRoot.localScale;
            if (inward.sqrMagnitude > 0.001f)
            {
                _inward = inward.normalized;
            }
            else
            {
                _inward = Vector3.forward;
            }
            _perp = new Vector3(-_inward.z, 0f, _inward.x).normalized;
            _shakeTrauma = 0f;
            _shakeTimer = 0f;
            _isShaking = false;

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

        public void ConfigureBounce(float intensity, float frequency, float decay, float verticalDip = 0.10f)
        {
            _shakeIntensity = intensity;
            _shakeFrequency = frequency;
            _shakeDecay = decay;
            _verticalDip = verticalDip;
        }

        [ContextMenu("Test Shake This Tile")]
        public void TestShake()
        {
            Bounce(1.5f);
        }

        public void Bounce(float strength = 1f)
        {
            if (_tileRoot == null)
            {
                _tileRoot = transform.parent != null ? transform.parent : transform;
                _rootBasePosition = _tileRoot.localPosition;
                _rootBaseScale = _tileRoot.localScale;
            }

            // Add smooth trauma scaled by strength (capped for natural response)
            float addedTrauma = Mathf.Clamp(strength, 0.3f, 2.0f);
            _shakeTrauma = Mathf.Min(1.2f, _shakeTrauma + (0.7f * addedTrauma));
            _shakeTimer = 0f;
            _isShaking = true;
        }

        private void Update()
        {
            if (!_isShaking || _tileRoot == null)
            {
                return;
            }

            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            _shakeTimer += dt;
            _shakeTrauma = Mathf.Max(0f, _shakeTrauma - (_shakeDecay * dt));

            if (_shakeTrauma <= 0.001f || _shakeTimer > 0.40f)
            {
                _shakeTrauma = 0f;
                _shakeTimer = 0f;
                _isShaking = false;
                _tileRoot.localPosition = _rootBasePosition;
                _tileRoot.localScale = _rootBaseScale;
                return;
            }

            // Smooth exponential decay envelope
            float envelope = Mathf.Exp(-_shakeDecay * _shakeTimer) * _shakeTrauma;
            float wave = Mathf.Sin(_shakeTimer * _shakeFrequency);

            // Responsive physical cushion: visible inward dip towards center, subtle lateral give
            float inward = -wave * (_shakeIntensity * 1.5f) * envelope;
            float lateral = Mathf.Sin(_shakeTimer * (_shakeFrequency * 0.8f)) * (_shakeIntensity * 0.5f) * envelope;
            float vertical = -wave * _verticalDip * envelope;

            // Subtle squash-and-stretch scale response visible from top-down orthographic camera
            float scale = 1f - (0.045f * wave * envelope);

            Vector3 offset = (_perp * lateral) + (_inward * inward) + new Vector3(0f, vertical, 0f);
            _tileRoot.localPosition = _rootBasePosition + offset;
            _tileRoot.localScale = new Vector3(_rootBaseScale.x * scale, _rootBaseScale.y, _rootBaseScale.z * scale);
        }

        private void OnDisable()
        {
            if (_tileRoot != null)
            {
                _tileRoot.localPosition = _rootBasePosition;
                _tileRoot.localScale = _rootBaseScale;
            }
            _shakeTrauma = 0f;
            _shakeTimer = 0f;
            _isShaking = false;
        }

        public void SetOwnerBand(
            MeshRenderer bandRenderer,
            Material neutralMaterial,
            TMP_Text upgradeText,
            string priceText = null)
        {
            _ownerBandRenderer = bandRenderer;
            _neutralOwnerBandMaterial = neutralMaterial;
            _ownerBandUpgradeText = upgradeText;
            _bandPriceText = priceText;
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

            if (UpgradeLevel >= 5)
            {
                _ownerBandUpgradeText.text = "HOTEL";
            }
            else if (UpgradeLevel >= 1)
            {
                _ownerBandUpgradeText.text = UpgradeLevel == 1
                    ? "1 HOUSE"
                    : $"{UpgradeLevel} HOUSES";
            }
            else
            {
                // At level 0 (base property, whether unowned or owned), show price if configured
                _ownerBandUpgradeText.text = !string.IsNullOrEmpty(_bandPriceText)
                    ? _bandPriceText
                    : string.Empty;
            }
        }
    }
}
