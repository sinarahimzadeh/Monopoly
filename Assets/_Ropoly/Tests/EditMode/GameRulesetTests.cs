using NUnit.Framework;
using Ropoly.Core.Rules;
using Ropoly.Infrastructure.Content;
using UnityEditor;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class GameRulesetTests
    {
        private const string DefaultRulesetPath =
            "Assets/_Ropoly/Content/Rulesets/ClassicRopoly.asset";

        [Test]
        public void DefaultDefinition_CreatesExpectedValidSnapshot()
        {
            GameRulesetDefinition definition = ScriptableObject.CreateInstance<GameRulesetDefinition>();

            GameRulesetSnapshot snapshot = definition.CreateSnapshot();
            GameRulesetValidationResult validation = definition.Validate();

            Assert.That(validation.IsValid, Is.True);
            Assert.That(snapshot.SchemaVersion, Is.EqualTo(GameRulesetLimits.CurrentSchemaVersion));
            Assert.That(snapshot.RulesetId, Is.EqualTo("classic-ropoly"));
            Assert.That(snapshot.DisplayName, Is.EqualTo("Classic Ropoly"));
            Assert.That(snapshot.PlayerCount, Is.EqualTo(4));
            Assert.That(snapshot.StartingCash, Is.EqualTo(1500));
            Assert.That(snapshot.PassStartCash, Is.EqualTo(200));
            Assert.That(snapshot.TurnDurationSeconds, Is.EqualTo(60));
            Assert.That(snapshot.DisconnectGraceSeconds, Is.EqualTo(120));
            Assert.That(snapshot.MortgageEnabled, Is.True);
            Assert.That(snapshot.AuctionsEnabled, Is.True);
            Assert.That(snapshot.FullSetRentMultiplier, Is.EqualTo(2));
            Assert.That(snapshot.VacationRewardEnabled, Is.False);
            Assert.That(snapshot.VacationReward, Is.Zero);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validator_ReportsEveryInvalidValue()
        {
            GameRulesetSnapshot snapshot = new GameRulesetSnapshot(
                schemaVersion: 999,
                rulesetId: "Invalid ID",
                displayName: string.Empty,
                playerCount: 1,
                startingCash: 0,
                passStartCash: -1,
                turnDurationSeconds: 0,
                disconnectGraceSeconds: -1,
                mortgageEnabled: true,
                auctionsEnabled: true,
                fullSetRentMultiplier: 0,
                vacationRewardEnabled: true,
                vacationReward: -1);

            GameRulesetValidationResult result = GameRulesetValidator.Validate(snapshot);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Contains(RulesetValidationErrorCode.UnsupportedSchemaVersion), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.InvalidRulesetId), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.InvalidDisplayName), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.PlayerCountOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.StartingCashOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.PassStartCashOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.TurnDurationOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.DisconnectGraceOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.FullSetRentMultiplierOutOfRange), Is.True);
            Assert.That(result.Contains(RulesetValidationErrorCode.VacationRewardOutOfRange), Is.True);
        }

        [Test]
        public void SerializedDefinition_MapsInspectorValuesIntoSnapshot()
        {
            GameRulesetDefinition definition = ScriptableObject.CreateInstance<GameRulesetDefinition>();
            SerializedObject serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("_rulesetId").stringValue = "quick-test";
            serializedDefinition.FindProperty("_displayName").stringValue = "Quick Test";
            serializedDefinition.FindProperty("_playerCount").intValue = 2;
            serializedDefinition.FindProperty("_startingCash").intValue = 900;
            serializedDefinition.FindProperty("_passStartCash").intValue = 175;
            serializedDefinition.FindProperty("_turnDurationSeconds").intValue = 30;
            serializedDefinition.FindProperty("_disconnectGraceSeconds").intValue = 45;
            serializedDefinition.FindProperty("_mortgageEnabled").boolValue = false;
            serializedDefinition.FindProperty("_auctionsEnabled").boolValue = false;
            serializedDefinition.FindProperty("_fullSetRentMultiplier").intValue = 3;
            serializedDefinition.FindProperty("_vacationRewardEnabled").boolValue = true;
            serializedDefinition.FindProperty("_vacationReward").intValue = 250;
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();

            GameRulesetSnapshot snapshot = definition.CreateSnapshot();

            Assert.That(definition.Validate().IsValid, Is.True);
            Assert.That(snapshot.RulesetId, Is.EqualTo("quick-test"));
            Assert.That(snapshot.DisplayName, Is.EqualTo("Quick Test"));
            Assert.That(snapshot.PlayerCount, Is.EqualTo(2));
            Assert.That(snapshot.StartingCash, Is.EqualTo(900));
            Assert.That(snapshot.PassStartCash, Is.EqualTo(175));
            Assert.That(snapshot.TurnDurationSeconds, Is.EqualTo(30));
            Assert.That(snapshot.DisconnectGraceSeconds, Is.EqualTo(45));
            Assert.That(snapshot.MortgageEnabled, Is.False);
            Assert.That(snapshot.AuctionsEnabled, Is.False);
            Assert.That(snapshot.FullSetRentMultiplier, Is.EqualTo(3));
            Assert.That(snapshot.VacationRewardEnabled, Is.True);
            Assert.That(snapshot.VacationReward, Is.EqualTo(250));

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ClassicRulesetAsset_ExistsAndIsValid()
        {
            GameRulesetDefinition definition =
                AssetDatabase.LoadAssetAtPath<GameRulesetDefinition>(DefaultRulesetPath);

            Assert.That(definition, Is.Not.Null, "Classic Ropoly ruleset asset is missing.");
            Assert.That(definition.Validate().IsValid, Is.True);
        }
    }
}
