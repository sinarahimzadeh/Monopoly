using Ropoly.Presentation.Navigation;
using UnityEngine;

namespace Ropoly.Bootstrap
{
    /// <summary>
    /// Requests the first application scene after AppRoot initializes its services.
    /// </summary>
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        [SerializeField]
        private AppRoot _appRoot;

        [SerializeField]
        private string _firstScene = AppSceneNames.MainMenu;

        private void Start()
        {
            if (_appRoot == null)
            {
                TryGetComponent(out _appRoot);
            }

            if (_appRoot == null || AppRoot.Instance != _appRoot)
            {
                return;
            }

            _appRoot.SceneFlow.TryLoad(_firstScene);
        }
    }
}
