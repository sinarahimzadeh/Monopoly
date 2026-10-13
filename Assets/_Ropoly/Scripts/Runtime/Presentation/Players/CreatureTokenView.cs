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
        private SpriteRenderer[] _pupilHighlights;

        [SerializeField]
        private SphereCollider _bodyCollider;

        [Header("Breathing")]
        [SerializeField]
        [Range(0f, 0.08f)]
        private float _breathAmount = 0.065f;

        [SerializeField]
        [Range(0.1f, 6f)]
        private float _breathSpeed = 2f;

        [SerializeField]
        [Range(0f, 0.08f)]
        private float _breathLift = 0.03f;

        [Header("Directional gaze")]
        [SerializeField]
        [Range(0f, 0.12f)]
        private float _lookDistance = 0.04f;

        [SerializeField]
        [Range(1f, 30f)]
        private float _lookResponsiveness = 16f;

        [SerializeField]
        private string _creatureId;

        [SerializeField]
        private int _playerIndex = -1;

        private Camera _boardCamera;
        private Vector3 _visualRestPosition;
        private Vector3[] _pupilRestPositions;
        private Vector3[] _highlightRestPositions;
        private Vector2 _targetLookDirection;
        private Vector2 _currentLookDirection;
        private float _animationPhase;

        public string CreatureId => _creatureId;
        public int PlayerIndex => _playerIndex;
        public SphereCollider BodyCollider => _bodyCollider;
        public Transform VisualRoot => _visualRoot;
        public SpriteRenderer BodySprite => _bodySprite;
        public Vector2 TargetLookDirection => _targetLookDirection;

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

            float wave = Mathf.Sin((Time.time * _breathSpeed) + _animationPhase);
            _visualRoot.localPosition =
                _visualRestPosition + (Vector3.up * (wave * _breathLift));
            float breath = wave * _breathAmount;
            _visualRoot.localScale = new Vector3(
                1f + (breath * 0.65f),
                1f + breath,
                1f);

            float lookBlend = 1f - Mathf.Exp(-_lookResponsiveness * Time.deltaTime);
            _currentLookDirection = Vector2.Lerp(
                _currentLookDirection,
                _targetLookDirection,
                lookBlend);
            Vector3 lookOffset = new Vector3(
                _currentLookDirection.x,
                _currentLookDirection.y,
                0f) * _lookDistance;
            ApplyLookOffset(_pupilSprites, _pupilRestPositions, lookOffset);
            ApplyLookOffset(_pupilHighlights, _highlightRestPositions, lookOffset);
        }

        public void LookTowards(Vector3 worldMovement)
        {
            if (_boardCamera == null || !_boardCamera.isActiveAndEnabled)
            {
                _boardCamera = Camera.main;
            }

            Vector2 direction = GetCardinalLookDirection(worldMovement, _boardCamera);
            if (direction != Vector2.zero)
            {
                _targetLookDirection = direction;
            }
        }

        public static Vector2 GetCardinalLookDirection(
            Vector3 worldMovement,
            Camera boardCamera)
        {
            worldMovement.y = 0f;
            if (worldMovement.sqrMagnitude <= 0.000001f)
            {
                return Vector2.zero;
            }

            Vector2 screenMovement = boardCamera == null
                ? new Vector2(worldMovement.x, worldMovement.z)
                : new Vector2(
                    Vector3.Dot(worldMovement, boardCamera.transform.right),
                    Vector3.Dot(worldMovement, boardCamera.transform.up));

            return Mathf.Abs(screenMovement.x) >= Mathf.Abs(screenMovement.y)
                ? new Vector2(Mathf.Sign(screenMovement.x), 0f)
                : new Vector2(0f, Mathf.Sign(screenMovement.y));
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
            _pupilRestPositions = CacheLocalPositions(_pupilSprites);
            _highlightRestPositions = CacheLocalPositions(_pupilHighlights);
        }

        private static Vector3[] CacheLocalPositions(SpriteRenderer[] renderers)
        {
            if (renderers == null)
            {
                return System.Array.Empty<Vector3>();
            }

            Vector3[] positions = new Vector3[renderers.Length];
            for (int index = 0; index < renderers.Length; index++)
            {
                if (renderers[index] != null)
                {
                    positions[index] = renderers[index].transform.localPosition;
                }
            }

            return positions;
        }

        private static void ApplyLookOffset(
            SpriteRenderer[] renderers,
            Vector3[] restPositions,
            Vector3 offset)
        {
            if (renderers == null || restPositions == null)
            {
                return;
            }

            int count = Mathf.Min(renderers.Length, restPositions.Length);
            for (int index = 0; index < count; index++)
            {
                if (renderers[index] != null)
                {
                    renderers[index].transform.localPosition = restPositions[index] + offset;
                }
            }
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
