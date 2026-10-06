using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ropoly.Presentation.Navigation
{
    /// <summary>
    /// Inspector-configurable scene navigation for UI buttons.
    /// </summary>
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField]
        private string _destinationScene = string.Empty;

        private bool _isLoading;

        public void LoadDestination()
        {
            if (_isLoading)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_destinationScene))
            {
                Debug.LogError("A destination scene has not been configured.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(_destinationScene))
            {
                Debug.LogError($"Scene '{_destinationScene}' is not available in Build Settings.", this);
                return;
            }

            _isLoading = true;
            SceneManager.LoadSceneAsync(_destinationScene, LoadSceneMode.Single);
        }
    }
}
