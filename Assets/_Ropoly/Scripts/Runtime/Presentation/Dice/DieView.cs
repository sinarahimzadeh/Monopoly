using UnityEngine;

namespace Ropoly.Presentation.Dice
{
    [DisallowMultipleComponent]
    public sealed class DieView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int ShadeColorId = Shader.PropertyToID("_ShadeColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        private static readonly int RimPowerId = Shader.PropertyToID("_RimPower");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        [SerializeField]
        private MeshRenderer _bodyRenderer;

        [SerializeField]
        private MeshRenderer[] _pipRenderers;

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private BoxCollider _bodyCollider;

        [SerializeField]
        private int _displayedValue = 1;

        public Rigidbody Rigidbody => _rigidbody;
        public BoxCollider BodyCollider => _bodyCollider;
        public MeshRenderer BodyRenderer => _bodyRenderer;
        public int PipCount => _pipRenderers == null ? 0 : _pipRenderers.Length;
        public int DisplayedValue => _displayedValue;

        public void ApplySettings(DicePresentationSettings settings)
        {
            if (settings == null)
            {
                throw new System.ArgumentNullException(nameof(settings));
            }

            transform.localScale = Vector3.one * settings.DieSize;
            ApplyStyle(
                _bodyRenderer,
                settings.BodyColor,
                settings.BodyShadeColor,
                settings.RimColor,
                settings.RimStrength,
                rimPower: 2.6f,
                smoothness: 0.62f,
                emissionColor: Color.black,
                emissionStrength: 0f);
            foreach (MeshRenderer pip in _pipRenderers)
            {
                ApplyStyle(
                    pip,
                    settings.PipColor,
                    settings.PipColor * 0.55f,
                    settings.PipGlowColor,
                    rimStrength: 0.72f,
                    rimPower: 2f,
                    smoothness: 0.42f,
                    emissionColor: settings.PipGlowColor,
                    emissionStrength: settings.PipGlowStrength);
            }

            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
        }

        public void SetDisplayedValue(int value, float yawDegrees)
        {
            _displayedValue = value;
            transform.rotation = GetRotationForValue(value, yawDegrees);
        }

        public void CompleteRoll(int value, Vector3 position, float yawDegrees)
        {
            _displayedValue = value;
            SetPose(position, GetRotationForValue(value, yawDegrees));
        }

        public bool IsShowingValue(int value, float minimumUpDot = 0.995f)
        {
            Vector3 worldDirection = transform.TransformDirection(GetLocalFaceDirection(value));
            return Vector3.Dot(worldDirection.normalized, Vector3.up) >= minimumUpDot;
        }

        public static Quaternion GetRotationForValue(int value, float yawDegrees)
        {
            Quaternion faceRotation = value switch
            {
                1 => Quaternion.identity,
                2 => Quaternion.Euler(-90f, 0f, 0f),
                3 => Quaternion.Euler(0f, 0f, 90f),
                4 => Quaternion.Euler(0f, 0f, -90f),
                5 => Quaternion.Euler(90f, 0f, 0f),
                6 => Quaternion.Euler(180f, 0f, 0f),
                _ => throw new System.ArgumentOutOfRangeException(nameof(value)),
            };
            return Quaternion.AngleAxis(yawDegrees, Vector3.up) * faceRotation;
        }

        public static Vector3 GetLocalFaceDirection(int value)
        {
            return value switch
            {
                1 => Vector3.up,
                2 => Vector3.forward,
                3 => Vector3.right,
                4 => Vector3.left,
                5 => Vector3.back,
                6 => Vector3.down,
                _ => throw new System.ArgumentOutOfRangeException(nameof(value)),
            };
        }

        private static void ApplyStyle(
            Renderer renderer,
            Color baseColor,
            Color shadeColor,
            Color rimColor,
            float rimStrength,
            float rimPower,
            float smoothness,
            Color emissionColor,
            float emissionStrength)
        {
            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, baseColor);
            properties.SetColor(ColorId, baseColor);
            properties.SetColor(ShadeColorId, shadeColor);
            properties.SetColor(RimColorId, rimColor);
            properties.SetFloat(RimStrengthId, rimStrength);
            properties.SetFloat(RimPowerId, rimPower);
            properties.SetFloat(SmoothnessId, smoothness);
            properties.SetColor(EmissionColorId, emissionColor);
            properties.SetFloat(EmissionStrengthId, emissionStrength);
            renderer.SetPropertyBlock(properties);
        }
    }
}
