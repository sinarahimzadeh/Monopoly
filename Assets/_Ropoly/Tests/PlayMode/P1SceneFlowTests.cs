using System.Collections;
using NUnit.Framework;
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
        public IEnumerator Bootstrap_PlayAndBackButtons_CompleteTheP1SceneFlow()
        {
            SceneManager.LoadScene(AppSceneNames.Bootstrap, LoadSceneMode.Single);
            yield return WaitForScene(AppSceneNames.MainMenu);

            SceneNavigationButton playNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(playNavigation, Is.Not.Null, "MainMenu is missing its Play navigation component.");

            Button playButton = playNavigation.GetComponent<Button>();
            Assert.That(playButton, Is.Not.Null, "MainMenu navigation is not connected to a UI Button.");
            playButton.onClick.Invoke();
            yield return WaitForScene(AppSceneNames.Game);

            SceneNavigationButton backNavigation = Object.FindFirstObjectByType<SceneNavigationButton>();
            Assert.That(backNavigation, Is.Not.Null, "Game is missing its Back navigation component.");

            Button backButton = backNavigation.GetComponent<Button>();
            Assert.That(backButton, Is.Not.Null, "Game navigation is not connected to a UI Button.");
            backButton.onClick.Invoke();
            yield return WaitForScene(AppSceneNames.MainMenu);
        }

        private static IEnumerator WaitForScene(string expectedScene)
        {
            float timeoutAt = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != expectedScene &&
                   Time.realtimeSinceStartup < timeoutAt)
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
