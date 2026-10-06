using Ropoly.Application.Navigation;
using Ropoly.Presentation.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ropoly.Bootstrap
{
    /// <summary>
    /// Owns all application scene transitions and injects itself into scene navigation views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SceneFlowService : MonoBehaviour, ISceneFlowService
    {
        private bool _isInitialized;

        public bool IsLoading { get; private set; }

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            InitializeNavigation(SceneManager.GetActiveScene());
        }

        public void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            IsLoading = false;
            _isInitialized = false;
        }

        public bool TryLoad(string sceneName)
        {
            if (!_isInitialized)
            {
                Debug.LogError("SceneFlowService must be initialized by AppRoot before use.", this);
                return false;
            }

            if (IsLoading)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("A destination scene has not been configured.", this);
                return false;
            }

            if (!UnityEngine.Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is not available in Build Settings.", this);
                return false;
            }

            if (ActiveSceneName == sceneName)
            {
                return false;
            }

            IsLoading = true;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                Debug.LogError($"Unity could not begin loading scene '{sceneName}'.", this);
                return false;
            }

            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            IsLoading = false;
            InitializeNavigation(scene);
        }

        private void InitializeNavigation(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                SceneNavigationButton[] navigationButtons =
                    rootObject.GetComponentsInChildren<SceneNavigationButton>(true);

                foreach (SceneNavigationButton navigationButton in navigationButtons)
                {
                    navigationButton.Initialize(this);
                }
            }
        }
    }
}
