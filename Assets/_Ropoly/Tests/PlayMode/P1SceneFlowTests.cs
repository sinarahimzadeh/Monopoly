using System.Collections;
using System.Linq;
using NUnit.Framework;
using Ropoly.Bootstrap;
using Ropoly.Presentation.Navigation;
using TMPro;
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
            yield return LoadScene(AppSceneNames.Bootstrap);
            SceneLoadingView loadingView = Object.FindFirstObjectByType<SceneLoadingView>();
            Assert.That(loadingView, Is.Not.Null, "Bootstrap is missing its persistent loading view.");
            Assert.That(loadingView.IsVisible, Is.True);
            Assert.That(loadingView.LoadingCamera, Is.Not.Null);
            Assert.That(loadingView.LoadingCamera.isActiveAndEnabled, Is.True);
            Assert.That(loadingView.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(loadingView.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);

            yield return WaitForScene(AppSceneNames.MainMenu);
            Assert.That(loadingView.IsVisible, Is.False);
            Assert.That(
                Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None),
                Is.Not.Empty,
                "MainMenu is missing its SDF text components.");
            Assert.That(
                Object.FindObjectsByType<Text>(FindObjectsSortMode.None),
                Is.Empty,
                "MainMenu still contains legacy blurry UI text.");

            AppRoot appRoot = AppRoot.Instance;
            Assert.That(appRoot, Is.Not.Null, "Bootstrap did not create an AppRoot.");
            Assert.That(appRoot.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            int appRootInstanceId = appRoot.GetInstanceID();

            SceneNavigationButton playNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(playNavigation, Is.Not.Null, "MainMenu is missing its Play navigation component.");

            Button playButton = playNavigation.GetComponent<Button>();
            Assert.That(playButton, Is.Not.Null, "MainMenu navigation is not connected to a UI Button.");
            playButton.onClick.Invoke();
            Assert.That(loadingView.IsVisible, Is.True);
            playButton.onClick.Invoke();
            Assert.That(appRoot.SceneFlow.IsLoading, Is.True, "Scene loading was not guarded immediately.");
            yield return WaitForScene(AppSceneNames.Game);

            AssertSinglePersistentAppRoot(appRootInstanceId);
            Assert.That(loadingView.IsVisible, Is.False);

            SceneNavigationButton backNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(backNavigation, Is.Not.Null, "Game is missing its Back navigation component.");

            Button backButton = backNavigation.GetComponent<Button>();
            Assert.That(backButton, Is.Not.Null, "Game navigation is not connected to a UI Button.");
            backButton.onClick.Invoke();
            Assert.That(loadingView.IsVisible, Is.True);
            yield return WaitForScene(AppSceneNames.MainMenu);

            AssertSinglePersistentAppRoot(appRootInstanceId);
            Assert.That(loadingView.IsVisible, Is.False);

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

        private static IEnumerator LoadScene(string sceneName)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
