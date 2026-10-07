using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Presentation.Board;
using Ropoly.Presentation.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ropoly.Tests.PlayMode
{
    public sealed class BoardPreviewPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameScene_GeneratesFortyVisibleThreeDimensionalTiles()
        {
            SceneManager.LoadScene(AppSceneNames.Game, LoadSceneMode.Single);
            yield return null;
            yield return null;

            BoardPreviewController controller = Object.FindFirstObjectByType<BoardPreviewController>();
            BoardPreviewTileView[] tiles = Object.FindObjectsByType<BoardPreviewTileView>(
                FindObjectsSortMode.None);

            Assert.That(controller, Is.Not.Null);
            Assert.That(tiles, Has.Length.EqualTo(40));
            HashSet<int> indexes = new HashSet<int>();
            foreach (BoardPreviewTileView tile in tiles)
            {
                Assert.That(tile.GetComponent<BoxCollider>(), Is.Not.Null, tile.ContentId);
                Assert.That(indexes.Add(tile.Index), Is.True, $"Duplicate tile index {tile.Index}.");
            }

            Assert.That(indexes, Does.Contain(0));
            Assert.That(indexes, Does.Contain(39));

            TMP_Text[] sharpTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            Assert.That(sharpTexts.Length, Is.GreaterThanOrEqualTo(50));
            Assert.That(Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(
                Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None),
                Is.Empty);
        }
    }
}
