using System.Collections.Generic;
using NUnit.Framework;
using Ropoly.Infrastructure.Content.Players;
using Ropoly.Presentation.Players;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class CreatureRosterAssetTests
    {
        [Test]
        public void DefaultRoster_HasEightUniqueValidCreatures()
        {
            CreatureRosterDefinition roster =
                AssetDatabase.LoadAssetAtPath<CreatureRosterDefinition>(
                    "Assets/_Ropoly/Content/Players/DefaultCreatureRoster.asset");

            Assert.That(roster, Is.Not.Null);
            Assert.That(roster.IsValid, Is.True);
            Assert.That(roster.Creatures, Has.Count.EqualTo(8));

            HashSet<string> ids = new HashSet<string>();
            foreach (CreatureDefinition creature in roster.Creatures)
            {
                Assert.That(creature.IsValid, Is.True, creature == null ? "null" : creature.name);
                Assert.That(ids.Add(creature.CreatureId), Is.True, creature.CreatureId);
                Assert.That(creature.TokenPrefab, Is.Not.Null, creature.CreatureId);
            }
        }

        [Test]
        public void CreaturePrefab_UsesOnlyThreeDimensionalPresentationAndPhysics()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Ropoly/Prefabs/Players/CreatureToken.prefab");

            Assert.That(prefab, Is.Not.Null);
            CreatureTokenView view = prefab.GetComponent<CreatureTokenView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.BodyCollider, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Collider2D>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Rigidbody2D>(true), Is.Empty);
        }
    }
}
