using System.Collections;
using System.Linq;
using NUnit.Framework;
using Ropoly.Bootstrap;
using Ropoly.Presentation.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Ropoly.Tests.PlayMode
{
    public sealed class P1SceneFlowTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_PersistsOneAppRoot_ThroughGuardedSceneFlow()
        {
            SceneManager.LoadScene(AppSceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(AppSceneNames.MainMenu);

            AppRoot appRoot = AppRoot.Instance;
            Assert.That(appRoot, Is.Not.Null, "Bootstrap did not create an AppRoot.");
            Assert.That(appRoot.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            int appRootInstanceId = appRoot.GetInstanceID();

            SceneNavigationButton playNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(playNavigation, Is.Not.Null, "MainMenu is missing its Play navigation component.");

            Button playButton = playNavigation.GetComponent<Button>();
            Assert.That(playButton, Is.Not.Null, "MainMenu navigation is not connected to a UI Button.");
            playButton.onClick.Invoke();
            playButton.onClick.Invoke();
            Assert.That(appRoot.SceneFlow.IsLoading, Is.True, "Scene loading was not guarded immediately.");
            yield return WaitForScene(AppSceneNames.Game);

            AssertSinglePersistentAppRoot(appRootInstanceId);

            SceneNavigationButton backNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(backNavigation, Is.Not.Null, "Game is missing its Back navigation component.");

            Button backButton = backNavigation.GetComponent<Button>();
            Assert.That(backButton, Is.Not.Null, "Game navigation is not connected to a UI Button.");
            backButton.onClick.Invoke();
            yield return WaitForScene(AppSceneNames.MainMenu);

            AssertSinglePersistentAppRoot(appRootInstanceId);

            GameObject duplicateRoot = new GameObject("DuplicateAppRoot");
            duplicateRoot.AddComponent<SceneFlowService>();
            duplicateRoot.AddComponent<AppRoot>();
            yield return null;

            AssertSinglePersistentAppRoot(appRootInstanceId);

            Object.Destroy(AppRoot.Instance.gameObject);
            yield return null;
        }

        private static void AssertSinglePersistentAppRoot(int expectedInstanceId)
        {
            AppRoot[] roots = Object.FindObjectsByType<AppRoot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(roots.Select(root => root.GetInstanceID()), Is.EqualTo(new[] { expectedInstanceId }));
            Assert.That(AppRoot.Instance.GetInstanceID(), Is.EqualTo(expectedInstanceId));
            Assert.That(AppRoot.Instance.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
        }

        private static IEnumerator WaitForScene(string expectedScene)
        {
            const int maximumFrames = 600;
            for (int frame = 0;
                 frame < maximumFrames && SceneManager.GetActiveScene().name != expectedScene;
                 frame++)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(expectedScene),
                $"Timed out while loading scene '{expectedScene}'.");
        }
    }
}
