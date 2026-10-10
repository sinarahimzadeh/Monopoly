using TMPro;
using UnityEngine;

namespace Ropoly.Presentation.Navigation
{
    /// <summary>
    /// Persistent camera and overlay shown while the application changes scenes.
    /// It guarantees that the player never sees Unity's no-camera fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneLoadingView : MonoBehaviour
    {
        [SerializeField]
        private Camera _loadingCamera;

        [SerializeField]
        private Canvas _loadingCanvas;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [SerializeField]
        private TMP_Text _statusLabel;

        [SerializeField]
        private string _startupMessage = "PREPARING THE WORLD";

        public bool IsVisible =>
            _loadingCamera != null &&
            _loadingCamera.enabled &&
            _loadingCanvas != null &&
            _loadingCanvas.enabled;

        public Camera LoadingCamera => _loadingCamera;
        public Canvas LoadingCanvas => _loadingCanvas;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            Show(_startupMessage);
        }

        public void Show(string destinationName)
        {
            if (!ValidateConfiguration())
            {
                return;
            }

            _statusLabel.text = string.IsNullOrWhiteSpace(destinationName)
                ? _startupMessage
                : $"LOADING  •  {FormatSceneName(destinationName)}";
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _loadingCamera.enabled = true;
            _loadingCanvas.enabled = true;
        }

        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_loadingCanvas != null)
            {
                _loadingCanvas.enabled = false;
            }

            if (_loadingCamera != null)
            {
                _loadingCamera.enabled = false;
            }
        }

        private bool ValidateConfiguration()
        {
            bool valid =
                _loadingCamera != null &&
                _loadingCanvas != null &&
                _canvasGroup != null &&
                _statusLabel != null;
            if (!valid)
            {
                Debug.LogError("Scene loading view configuration is incomplete.", this);
            }

            return valid;
        }

        private static string FormatSceneName(string sceneName)
        {
            return sceneName switch
            {
                AppSceneNames.MainMenu => "MAIN MENU",
                AppSceneNames.Game => "GAME",
                _ => sceneName.ToUpperInvariant(),
            };
        }
    }
}
