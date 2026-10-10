using System.Collections;
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
        private const int CameraReadinessFrameLimit = 120;

        [SerializeField]
        private SceneLoadingView _loadingView;

        private bool _isInitialized;
        private Coroutine _cameraReadinessRoutine;

        public bool IsLoading { get; private set; }

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public SceneLoadingView LoadingView => _loadingView;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            if (_loadingView == null)
            {
                _loadingView = GetComponentInChildren<SceneLoadingView>(true);
            }

            if (_loadingView == null)
            {
                Debug.LogError("SceneFlowService requires a persistent SceneLoadingView.", this);
            }

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
            if (_cameraReadinessRoutine != null)
            {
                StopCoroutine(_cameraReadinessRoutine);
                _cameraReadinessRoutine = null;
            }

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

            CancelCameraReadinessWait();
            IsLoading = true;
            _loadingView?.Show(sceneName);
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                _loadingView?.Hide();
                Debug.LogError($"Unity could not begin loading scene '{sceneName}'.", this);
                return false;
            }

            return true;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            InitializeNavigation(scene);
            if (scene.name == AppSceneNames.Bootstrap)
            {
                // AppRoot is moved to DontDestroyOnLoad during Awake, so its loading
                // camera no longer belongs to the Bootstrap scene being reported here.
                // Keep the cover visible until BootstrapSceneLoader requests MainMenu.
                return;
            }

            if (HasActiveCamera(scene))
            {
                CancelCameraReadinessWait();
                CompleteTransition();
                return;
            }

            if (_cameraReadinessRoutine != null)
            {
                StopCoroutine(_cameraReadinessRoutine);
            }

            _cameraReadinessRoutine = StartCoroutine(WaitForDestinationCamera(scene));
        }

        private void CancelCameraReadinessWait()
        {
            if (_cameraReadinessRoutine == null)
            {
                return;
            }

            StopCoroutine(_cameraReadinessRoutine);
            _cameraReadinessRoutine = null;
        }

        private IEnumerator WaitForDestinationCamera(Scene scene)
        {
            for (int frame = 0; frame < CameraReadinessFrameLimit; frame++)
            {
                if (HasActiveCamera(scene))
                {
                    CompleteTransition();
                    yield break;
                }

                yield return null;
            }

            _cameraReadinessRoutine = null;
            Debug.LogError(
                $"Scene '{scene.name}' loaded without an active camera. " +
                "The loading cover will remain visible to avoid exposing a blank display.",
                this);
        }

        private void CompleteTransition()
        {
            _cameraReadinessRoutine = null;
            _loadingView?.Hide();
            IsLoading = false;
        }

        private static bool HasActiveCamera(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                Camera[] cameras = rootObject.GetComponentsInChildren<Camera>(true);
                foreach (Camera camera in cameras)
                {
                    if (camera.isActiveAndEnabled && camera.targetDisplay == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
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
