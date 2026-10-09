using System.Collections;
using NUnit.Framework;
using Ropoly.Core.Match;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using Ropoly.Presentation.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ropoly.Tests.PlayMode
{
    public sealed class DicePlayModeTests
    {
        [UnityTest]
        public IEnumerator DiceRoll_AnimatesToAuthoritativeValuesAndAdvancesTurns()
        {
            SceneManager.LoadScene(AppSceneNames.Game, LoadSceneMode.Single);
            yield return null;
            yield return null;

            LocalLobbyController lobby = Object.FindFirstObjectByType<LocalLobbyController>();
            DiceController dice = Object.FindFirstObjectByType<DiceController>();
            Assert.That(lobby, Is.Not.Null);
            Assert.That(dice, Is.Not.Null);
            Assert.That(dice.DiceRoot.activeSelf, Is.False);

            lobby.SetPlayerCount(2);
            lobby.SelectPlayer(0);
            lobby.SelectCreature(lobby.CreatureChoices[0].Definition);
            lobby.SelectCreature(lobby.CreatureChoices[1].Definition);
            lobby.StartMatch();
            yield return null;

            Assert.That(dice.DiceRoot.activeSelf, Is.True);
            Assert.That(dice.PrimaryButton.interactable, Is.True);
            Assert.That(lobby.Session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));

            dice.RollDice();
            Assert.That(dice.IsRolling, Is.True);
            Assert.That(lobby.Session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingMovement));

            float timeoutAt = Time.time + 4f;
            while (dice.IsRolling && Time.time < timeoutAt)
            {
                yield return null;
            }

            Assert.That(dice.IsRolling, Is.False, "The dice animation exceeded its timeout.");
            Assert.That(dice.LastCompletedRoll, Is.Not.Null);
            Assert.That(dice.LastCompletedRoll.FirstDie, Is.InRange(1, 6));
            Assert.That(dice.LastCompletedRoll.SecondDie, Is.InRange(1, 6));
            Assert.That(
                dice.FirstDie.IsShowingValue(dice.LastCompletedRoll.FirstDie),
                Is.True);
            Assert.That(
                dice.SecondDie.IsShowingValue(dice.LastCompletedRoll.SecondDie),
                Is.True);
            Assert.That(dice.FirstDie.Rigidbody, Is.Not.Null);
            Assert.That(dice.SecondDie.BodyCollider, Is.Not.Null);
            Assert.That(dice.FirstDie.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(dice.SecondDie.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);

            dice.AdvanceTurn();
            Assert.That(lobby.Session.State.Turn.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(lobby.Session.State.Turn.TurnNumber, Is.EqualTo(2));
            Assert.That(lobby.Session.State.Turn.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
        }
    }
}
