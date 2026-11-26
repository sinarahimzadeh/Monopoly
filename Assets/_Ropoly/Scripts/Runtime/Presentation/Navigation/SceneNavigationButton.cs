using Ropoly.Application.Navigation;
using Ropoly.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

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

        private void Awake()
        {
            Button button = GetComponent<Button>();
            if (button != null)
            {
                Color tint = string.Equals(_destinationScene, AppSceneNames.MainMenu, System.StringComparison.OrdinalIgnoreCase)
                    ? RopolyUiTheme.SecondaryActionColor
                    : RopolyUiTheme.PrimaryActionColor;
                RopolyUiTheme.ApplyButton(button, tint, usePill: true);
            }
        }

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
