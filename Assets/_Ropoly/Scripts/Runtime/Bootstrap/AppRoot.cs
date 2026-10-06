using Ropoly.Application.Navigation;
using UnityEngine;

namespace Ropoly.Bootstrap
{
    /// <summary>
    /// The single persistent composition root for application-wide services.
    /// Match and scene-specific objects must not be parented below this object.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SceneFlowService))]
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField]
        private SceneFlowService _sceneFlowService;

        public static AppRoot Instance { get; private set; }

        public ISceneFlowService SceneFlow => _sceneFlowService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_sceneFlowService == null)
            {
                TryGetComponent(out _sceneFlowService);
            }

            if (_sceneFlowService == null)
            {
                Debug.LogError("AppRoot requires a SceneFlowService.", this);
                enabled = false;
                return;
            }

            DontDestroyOnLoad(gameObject);
            _sceneFlowService.Initialize();
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            _sceneFlowService?.Shutdown();
            Instance = null;
        }
    }
}
