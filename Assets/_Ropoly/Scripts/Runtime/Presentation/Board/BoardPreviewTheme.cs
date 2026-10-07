using TMPro;
using UnityEngine;

namespace Ropoly.Presentation.Board
{
    [CreateAssetMenu(fileName = "BoardPreviewTheme", menuName = "Ropoly/Presentation/Board Preview Theme")]
    public sealed class BoardPreviewTheme : ScriptableObject
    {
        [Header("Board geometry")]
        [SerializeField]
        [Min(10f)]
        private float _boardSize = 18f;

        [SerializeField]
        [Range(1.5f, 4f)]
        private float _cornerSize = 2.4f;

        [SerializeField]
        [Range(0f, 0.2f)]
        private float _tileGap = 0.06f;

        [SerializeField]
        [Range(0.05f, 0.5f)]
        private float _tileDepth = 0.18f;

        [SerializeField]
        [Range(0.15f, 0.65f)]
        private float _accentDepth = 0.34f;

        [Header("Board colors")]
        [SerializeField]
        private Color _boardColor = new Color(0.055f, 0.039f, 0.10f, 1f);

        [SerializeField]
        private Color _centerColor = new Color(0.082f, 0.059f, 0.15f, 1f);

        [SerializeField]
        private Color _tileColor = new Color(0.145f, 0.11f, 0.23f, 1f);

        [SerializeField]
        private Color _tileAlternateColor = new Color(0.12f, 0.09f, 0.20f, 1f);

        [Header("Category colors")]
        [SerializeField]
        private Color _airportColor = new Color(0.20f, 0.55f, 0.92f, 1f);

        [SerializeField]
        private Color _utilityColor = new Color(0.10f, 0.72f, 0.72f, 1f);

        [SerializeField]
        private Color _eventColor = new Color(0.94f, 0.31f, 0.66f, 1f);

        [SerializeField]
        private Color _feeColor = new Color(0.91f, 0.31f, 0.34f, 1f);

        [SerializeField]
        private Color _cornerColor = new Color(0.48f, 0.31f, 0.91f, 1f);

        [Header("Typography")]
        [SerializeField]
        private TMP_FontAsset _font;

        [SerializeField]
        private Color _primaryTextColor = new Color(0.96f, 0.95f, 1f, 1f);

        [SerializeField]
        private Color _secondaryTextColor = new Color(0.60f, 0.57f, 0.72f, 1f);

        [SerializeField]
        private Color _priceTextColor = new Color(0.50f, 0.91f, 0.84f, 1f);

        public float BoardSize => _boardSize;
        public float CornerSize => _cornerSize;
        public float TileGap => _tileGap;
        public float TileDepth => _tileDepth;
        public float AccentDepth => _accentDepth;
        public Color BoardColor => _boardColor;
        public Color CenterColor => _centerColor;
        public Color TileColor => _tileColor;
        public Color TileAlternateColor => _tileAlternateColor;
        public Color AirportColor => _airportColor;
        public Color UtilityColor => _utilityColor;
        public Color EventColor => _eventColor;
        public Color FeeColor => _feeColor;
        public Color CornerColor => _cornerColor;
        public TMP_FontAsset Font => _font;
        public Color PrimaryTextColor => _primaryTextColor;
        public Color SecondaryTextColor => _secondaryTextColor;
        public Color PriceTextColor => _priceTextColor;

        private void OnValidate()
        {
            _boardSize = Mathf.Max(10f, _boardSize);
            _cornerSize = Mathf.Clamp(_cornerSize, 1.5f, _boardSize / 3f);
            _tileGap = Mathf.Clamp(_tileGap, 0f, 0.2f);
            _tileDepth = Mathf.Clamp(_tileDepth, 0.05f, 0.5f);
            _accentDepth = Mathf.Clamp(_accentDepth, 0.15f, _cornerSize * 0.4f);
        }
    }
}
