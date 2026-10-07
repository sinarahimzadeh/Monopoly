using System.Linq;
using NUnit.Framework;
using Ropoly.Presentation.Board;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class BoardPreviewAssetTests
    {
        private const string BootstrapScenePath = "Assets/_Ropoly/Scenes/App/Bootstrap.unity";
        private const string MainMenuScenePath = "Assets/_Ropoly/Scenes/Frontend/MainMenu.unity";
        private const string GameScenePath = "Assets/_Ropoly/Scenes/Gameplay/Game.unity";
        private const string ThemePath = "Assets/_Ropoly/Settings/Presentation/BoardPreviewTheme.asset";
        private const string PrefabPath = "Assets/_Ropoly/Prefabs/Board/BoardPreviewRoot.prefab";
        private const string FontPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [Test]
        public void OrganizedScenes_ArePresentAndEnabledInBuildOrder()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapScenePath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath), Is.Not.Null);

            string[] enabledPaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            Assert.That(
                enabledPaths,
                Is.EqualTo(new[] { BootstrapScenePath, MainMenuScenePath, GameScenePath }));
        }

        [Test]
        public void PreviewPrefab_HasDirectBoardAndThemeReferences()
        {
            BoardPreviewTheme theme = AssetDatabase.LoadAssetAtPath<BoardPreviewTheme>(ThemePath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(theme, Is.Not.Null);
            Assert.That(prefab, Is.Not.Null);
            BoardPreviewController controller = prefab.GetComponent<BoardPreviewController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.BoardDefinition, Is.Not.Null);
            Assert.That(controller.Theme, Is.SameAs(theme));
            Assert.That(theme.Font, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(theme.Font), Is.EqualTo(FontPath));
            Assert.That(theme.Font.atlasWidth, Is.GreaterThanOrEqualTo(1024));
        }
    }
}
