using Ropoly.Presentation.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ropoly.Bootstrap
{
    /// <summary>
    /// P-1's small entry point. Persistent application services are introduced in P-2.
    /// </summary>
    public sealed class BootstrapSceneLoader : MonoBehaviour
    {
        [SerializeField]
        private string _firstScene = AppSceneNames.MainMenu;

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(_firstScene))
            {
                Debug.LogError("The bootstrap first scene has not been configured.", this);
                return;
            }

            SceneManager.LoadSceneAsync(_firstScene, LoadSceneMode.Single);
        }
    }
}
