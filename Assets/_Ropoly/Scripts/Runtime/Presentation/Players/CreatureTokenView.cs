using Ropoly.Infrastructure.Content.Players;
using UnityEngine;

namespace Ropoly.Presentation.Players
{
    [DisallowMultipleComponent]
    public sealed class CreatureTokenView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private Renderer _bodyRenderer;

        [SerializeField]
        private Renderer[] _eyeRenderers;

        [SerializeField]
        private Renderer[] _pupilRenderers;

        [SerializeField]
        private SphereCollider _bodyCollider;

        [SerializeField]
        private string _creatureId;

        [SerializeField]
        private int _playerIndex = -1;

        public string CreatureId => _creatureId;
        public int PlayerIndex => _playerIndex;
        public SphereCollider BodyCollider => _bodyCollider;

        public void Configure(CreatureDefinition definition, int playerIndex)
        {
            if (definition == null)
            {
                throw new System.ArgumentNullException(nameof(definition));
            }

            _creatureId = definition.CreatureId;
            _playerIndex = playerIndex;
            ApplyColor(_bodyRenderer, definition.BodyColor);
            ApplyColor(_eyeRenderers, definition.EyeColor);
            ApplyColor(_pupilRenderers, definition.PupilColor);
            gameObject.name = $"Player {playerIndex + 1} - {definition.DisplayName}";
        }

        private static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            renderer.SetPropertyBlock(properties);
        }

        private static void ApplyColor(Renderer[] renderers, Color color)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (Renderer renderer in renderers)
            {
                ApplyColor(renderer, color);
            }
        }
    }
}
