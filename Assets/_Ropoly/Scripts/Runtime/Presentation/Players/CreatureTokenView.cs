using Ropoly.Infrastructure.Content.Players;
using UnityEngine;

namespace Ropoly.Presentation.Players
{
    /// <summary>
    /// A flat, camera-facing version of the creature portrait used in the lobby.
    /// The token still lives and moves in the 3D board world and keeps a 3D collider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CreatureTokenView : MonoBehaviour
    {
        [Header("2D portrait visual")]
        [SerializeField]
        private Transform _visualRoot;

        [SerializeField]
        private SpriteRenderer _bodySprite;

        [SerializeField]
        private SpriteRenderer[] _eyeSprites;

        [SerializeField]
        private SpriteRenderer[] _pupilSprites;

        [SerializeField]
        private SphereCollider _bodyCollider;

        [Header("Idle motion")]
        [SerializeField]
        [Range(0f, 0.12f)]
        private float _bobHeight = 0.02f;

        [SerializeField]
        [Range(0.1f, 6f)]
        private float _bobSpeed = 2.1f;

        [SerializeField]
        [Range(0f, 0.12f)]
        private float _squashAmount = 0.015f;

        [SerializeField]
        private string _creatureId;

        [SerializeField]
        private int _playerIndex = -1;

        private Camera _boardCamera;
        private Vector3 _visualRestPosition;
        private float _animationPhase;

        public string CreatureId => _creatureId;
        public int PlayerIndex => _playerIndex;
        public SphereCollider BodyCollider => _bodyCollider;
        public Transform VisualRoot => _visualRoot;
        public SpriteRenderer BodySprite => _bodySprite;

        private void Awake()
        {
            CacheRestPose();
            _boardCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_visualRoot == null)
            {
                return;
            }

            if (_boardCamera == null || !_boardCamera.isActiveAndEnabled)
            {
                _boardCamera = Camera.main;
            }

            if (_boardCamera != null)
            {
                // Sprite artwork lies in local XY. Matching the camera rotation keeps
                // the portrait circular instead of foreshortening it on the 3D board.
                _visualRoot.rotation = _boardCamera.transform.rotation;
            }

            float wave = Mathf.Sin((Time.time * _bobSpeed) + _animationPhase);
            _visualRoot.localPosition = _visualRestPosition + (Vector3.up * (wave * _bobHeight));
            float squash = wave * _squashAmount;
            _visualRoot.localScale = new Vector3(1f + squash, 1f - squash, 1f);
        }

        public void Configure(CreatureDefinition definition, int playerIndex)
        {
            if (definition == null)
            {
                throw new System.ArgumentNullException(nameof(definition));
            }

            _creatureId = definition.CreatureId;
            _playerIndex = playerIndex;
            _animationPhase = playerIndex * 1.73f;

            SetColor(_bodySprite, definition.BodyColor);
            SetColor(_eyeSprites, definition.EyeColor);
            SetColor(_pupilSprites, definition.PupilColor);

            CacheRestPose();
            gameObject.name = $"Player {playerIndex + 1} - {definition.DisplayName}";
        }

        private void CacheRestPose()
        {
            if (_visualRoot == null)
            {
                return;
            }

            _visualRestPosition = _visualRoot.localPosition;
            _visualRoot.localScale = Vector3.one;
        }

        private static void SetColor(SpriteRenderer renderer, Color color)
        {
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        private static void SetColor(SpriteRenderer[] renderers, Color color)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in renderers)
            {
                SetColor(renderer, color);
            }
        }
    }
}
