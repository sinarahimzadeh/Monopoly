using UnityEngine;

namespace Ropoly.Presentation.Dice
{
    [CreateAssetMenu(
        fileName = "DicePresentationSettings",
        menuName = "Ropoly/Presentation/Dice Settings")]
    public sealed class DicePresentationSettings : ScriptableObject
    {
        [Header("Appearance")]
        [SerializeField]
        [Range(0.65f, 1.5f)]
        private float _dieSize = 1.05f;

        [SerializeField]
        private Color _bodyColor = new Color(0.96f, 0.95f, 1f, 1f);

        [SerializeField]
        private Color _pipColor = new Color(0.055f, 0.035f, 0.09f, 1f);

        [SerializeField]
        private Color _bodyShadeColor = new Color(0.48f, 0.42f, 0.68f, 1f);

        [SerializeField]
        private Color _rimColor = new Color(0.43f, 1f, 0.88f, 1f);

        [SerializeField]
        private Color _pipGlowColor = new Color(0.94f, 0.28f, 0.72f, 1f);

        [SerializeField]
        [Range(0f, 2f)]
        private float _rimStrength = 0.48f;

        [SerializeField]
        [Range(0f, 3f)]
        private float _pipGlowStrength = 0.14f;

        [Header("Board placement")]
        [SerializeField]
        private Vector3 _boardCenter = new Vector3(0f, 0.84f, 0.45f);

        [SerializeField]
        [Range(1f, 3f)]
        private float _dieSeparation = 1.55f;

        [Header("Roll animation")]
        [SerializeField]
        [Range(0.4f, 3f)]
        private float _rollDuration = 1.2f;

        [SerializeField]
        [Range(0.5f, 5f)]
        private float _throwHeight = 2.25f;

        [SerializeField]
        [Range(1, 8)]
        private int _minimumSpinTurns = 3;

        [SerializeField]
        [Range(2, 12)]
        private int _maximumSpinTurns = 6;

        [SerializeField]
        [Range(0f, 0.8f)]
        private float _sidewaysMotion = 0.28f;

        public float DieSize => _dieSize;
        public Color BodyColor => _bodyColor;
        public Color PipColor => _pipColor;
        public Color BodyShadeColor => _bodyShadeColor;
        public Color RimColor => _rimColor;
        public Color PipGlowColor => _pipGlowColor;
        public float RimStrength => _rimStrength;
        public float PipGlowStrength => _pipGlowStrength;
        public Vector3 BoardCenter => _boardCenter;
        public float DieSeparation => _dieSeparation;
        public float RollDuration => _rollDuration;
        public float ThrowHeight => _throwHeight;
        public int MinimumSpinTurns => _minimumSpinTurns;
        public int MaximumSpinTurns => _maximumSpinTurns;
        public float SidewaysMotion => _sidewaysMotion;

        private void OnValidate()
        {
            _dieSize = Mathf.Clamp(_dieSize, 0.65f, 1.5f);
            _dieSeparation = Mathf.Clamp(_dieSeparation, 1f, 3f);
            _rollDuration = Mathf.Clamp(_rollDuration, 0.4f, 3f);
            _throwHeight = Mathf.Clamp(_throwHeight, 0.5f, 5f);
            _minimumSpinTurns = Mathf.Clamp(_minimumSpinTurns, 1, 8);
            _maximumSpinTurns = Mathf.Clamp(
                _maximumSpinTurns,
                _minimumSpinTurns,
                12);
            _sidewaysMotion = Mathf.Clamp(_sidewaysMotion, 0f, 0.8f);
            _rimStrength = Mathf.Clamp(_rimStrength, 0f, 2f);
            _pipGlowStrength = Mathf.Clamp(_pipGlowStrength, 0f, 3f);
        }
    }
}
