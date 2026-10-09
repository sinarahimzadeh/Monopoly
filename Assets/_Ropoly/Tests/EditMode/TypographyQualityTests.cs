using NUnit.Framework;
using Ropoly.Presentation.Board;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class TypographyQualityTests
    {
        private const string FontPath =
            "Assets/_Ropoly/Art/Fonts/Ropoly UI SDF.asset";
        private const string GameScenePath =
            "Assets/_Ropoly/Scenes/Gameplay/Game.unity";
        private const string ThemePath =
            "Assets/_Ropoly/Settings/Presentation/BoardPreviewTheme.asset";

        [Test]
        public void RopolyFont_UsesHighResolutionSdfAtlas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            Assert.That(font, Is.Not.Null);
            Assert.That(font.atlasWidth, Is.EqualTo(2048));
            Assert.That(font.atlasHeight, Is.EqualTo(2048));
            Assert.That(font.faceInfo.pointSize, Is.GreaterThanOrEqualTo(170));
            Assert.That(font.HasCharacter('A'), Is.True);
            Assert.That(font.HasCharacter(0x2026), Is.True, "The ellipsis glyph prevents TMP fallback warnings.");
        }

        [Test]
        public void GameScene_UsesRopolyFontAndPixelPerfectCanvas()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(texts, Is.Not.Empty);
            Assert.That(canvases, Is.Not.Empty);
            Assert.That(texts, Has.All.Matches<TMP_Text>(text => text.font == font));
            Assert.That(canvases, Has.All.Matches<Canvas>(canvas => canvas.pixelPerfect));
        }

        [Test]
        public void BoardTheme_UsesRopolyFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            BoardPreviewTheme theme = AssetDatabase.LoadAssetAtPath<BoardPreviewTheme>(ThemePath);

            Assert.That(theme, Is.Not.Null);
            Assert.That(theme.Font, Is.SameAs(font));
        }
    }
}
