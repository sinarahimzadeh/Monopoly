using UnityEngine;

namespace Ropoly.Infrastructure.Content.Players
{
    [CreateAssetMenu(
        fileName = "Creature",
        menuName = "Ropoly/Content/Players/Creature",
        order = 20)]
    public sealed class CreatureDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField]
        [Tooltip("Stable lowercase ID used by match state, saves, and networking.")]
        private string _creatureId = "creature";

        [SerializeField]
        private string _displayName = "Creature";

        [Header("Simple round appearance")]
        [SerializeField]
        private Color _bodyColor = Color.cyan;

        [SerializeField]
        private Color _eyeColor = Color.white;

        [SerializeField]
        private Color _pupilColor = new Color(0.025f, 0.02f, 0.04f, 1f);

        [Header("Presentation")]
        [SerializeField]
        [Tooltip("Shared 3D creature prefab. Appearance is applied from this definition at runtime.")]
        private GameObject _tokenPrefab;

        public string CreatureId => _creatureId;
        public string DisplayName => _displayName;
        public Color BodyColor => _bodyColor;
        public Color EyeColor => _eyeColor;
        public Color PupilColor => _pupilColor;
        public GameObject TokenPrefab => _tokenPrefab;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(_creatureId) &&
            !string.IsNullOrWhiteSpace(_displayName) &&
            _tokenPrefab != null;
    }
}
