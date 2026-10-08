using System.Collections;
using NUnit.Framework;
using Ropoly.Core.Match;
using Ropoly.Presentation.Lobby;
using Ropoly.Presentation.Navigation;
using Ropoly.Presentation.Players;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ropoly.Tests.PlayMode
{
    public sealed class LocalLobbyPlayModeTests
    {
        [UnityTest]
        public IEnumerator LocalLobby_SelectsUniqueCreaturesAndSpawnsThreeDimensionalTokens()
        {
            SceneManager.LoadScene(AppSceneNames.Game, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalLobbyController lobby = Object.FindFirstObjectByType<LocalLobbyController>();
            Assert.That(lobby, Is.Not.Null);
            Assert.That(lobby.Session, Is.Not.Null);
            Assert.That(lobby.Session.State.Phase, Is.EqualTo(MatchPhase.Lobby));
            Assert.That(lobby.LobbyOverlay.activeSelf, Is.True);
            Assert.That(lobby.CreatureChoices.Count, Is.EqualTo(8));

            lobby.SetPlayerCount(2);
            lobby.SelectPlayer(0);
            lobby.SelectCreature(lobby.CreatureChoices[0].Definition);
            lobby.SelectCreature(lobby.CreatureChoices[1].Definition);

            Assert.That(lobby.Session.CanStart, Is.True);
            lobby.StartMatch();
            yield return null;

            Assert.That(lobby.Session.State.Phase, Is.EqualTo(MatchPhase.InProgress));
            Assert.That(lobby.LobbyOverlay.activeSelf, Is.False);
            Assert.That(lobby.MatchHudPanel.activeSelf, Is.True);

            CreatureTokenView[] tokens = Object.FindObjectsByType<CreatureTokenView>(
                FindObjectsSortMode.None);
            Assert.That(tokens, Has.Length.EqualTo(2));
            foreach (CreatureTokenView token in tokens)
            {
                Assert.That(token.BodyCollider, Is.Not.Null);
                Assert.That(token.GetComponentsInChildren<Collider2D>(true), Is.Empty);
                Assert.That(token.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);
            }
        }
    }
}
