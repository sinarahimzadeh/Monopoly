using Ropoly.Application.Navigation;
using UnityEngine;

namespace Ropoly.Presentation.Navigation
{
    /// <summary>
    /// Inspector-configurable scene navigation for UI buttons.
    /// </summary>
    public sealed class SceneNavigationButton : MonoBehaviour
    {
        [SerializeField]
        private string _destinationScene = string.Empty;

        private ISceneFlowService _sceneFlow;

        public void Initialize(ISceneFlowService sceneFlow)
        {
            _sceneFlow = sceneFlow;
        }

        public void LoadDestination()
        {
            if (_sceneFlow == null)
            {
                Debug.LogError("Scene navigation has not been initialized by AppRoot.", this);
                return;
            }

            _sceneFlow.TryLoad(_destinationScene);
        }
    }
}
