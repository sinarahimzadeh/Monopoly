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
            Assert.That(snapshot.AuctionsEnabled, Is.False);
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

        [Test]
        public void GameRulesetData_RoundTripsSnapshotAccurately()
        {
            var original = new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                "custom-ruleset",
                "My Rules",
                3,
                2500,
                300,
                90,
                60,
                false,
                true,
                3,
                true,
                500,
                20);

            var data = GameRulesetData.FromSnapshot(original);
            Assert.That(data.rulesetId, Is.EqualTo("custom-ruleset"));
            Assert.That(data.startingCash, Is.EqualTo(2500));
            Assert.That(data.passStartCash, Is.EqualTo(300));
            Assert.That(data.turnDurationSeconds, Is.EqualTo(90));
            Assert.That(data.mortgageEnabled, Is.False);
            Assert.That(data.auctionsEnabled, Is.True);
            Assert.That(data.fullSetRentMultiplier, Is.EqualTo(3));
            Assert.That(data.vacationRewardEnabled, Is.True);
            Assert.That(data.vacationReward, Is.EqualTo(500));
            Assert.That(data.auctionBidTimeoutSeconds, Is.EqualTo(20));

            var roundTripped = data.ToSnapshot();
            Assert.That(roundTripped.RulesetId, Is.EqualTo(original.RulesetId));
            Assert.That(roundTripped.StartingCash, Is.EqualTo(original.StartingCash));
            Assert.That(roundTripped.PassStartCash, Is.EqualTo(original.PassStartCash));
            Assert.That(roundTripped.TurnDurationSeconds, Is.EqualTo(original.TurnDurationSeconds));
            Assert.That(roundTripped.MortgageEnabled, Is.EqualTo(original.MortgageEnabled));
            Assert.That(roundTripped.AuctionsEnabled, Is.EqualTo(original.AuctionsEnabled));
            Assert.That(roundTripped.FullSetRentMultiplier, Is.EqualTo(original.FullSetRentMultiplier));
            Assert.That(roundTripped.VacationRewardEnabled, Is.EqualTo(original.VacationRewardEnabled));
            Assert.That(roundTripped.VacationReward, Is.EqualTo(original.VacationReward));
            Assert.That(roundTripped.AuctionBidTimeoutSeconds, Is.EqualTo(original.AuctionBidTimeoutSeconds));
        }

        [Test]
        public void GameRulesetData_SerializesAndDeserializesJson()
        {
            var data = new GameRulesetData
            {
                rulesetId = "json-rules",
                displayName = "JSON Test",
                playerCount = 4,
                startingCash = 2000,
                passStartCash = 400,
                turnDurationSeconds = 30,
                disconnectGraceSeconds = 90,
                mortgageEnabled = true,
                auctionsEnabled = true,
                fullSetRentMultiplier = 3,
                vacationRewardEnabled = false,
                vacationReward = 0,
                auctionBidTimeoutSeconds = 10
            };

            string json = data.ToJson();
            Assert.That(string.IsNullOrEmpty(json), Is.False);

            GameRulesetData deserialized = GameRulesetData.FromJson(json);
            Assert.That(deserialized.rulesetId, Is.EqualTo("json-rules"));
            Assert.That(deserialized.startingCash, Is.EqualTo(2000));
            Assert.That(deserialized.passStartCash, Is.EqualTo(400));
            Assert.That(deserialized.turnDurationSeconds, Is.EqualTo(30));
            Assert.That(deserialized.auctionsEnabled, Is.True);
            Assert.That(deserialized.fullSetRentMultiplier, Is.EqualTo(3));
            Assert.That(deserialized.auctionBidTimeoutSeconds, Is.EqualTo(10));
        }

        [Test]
        public void GameRulesetData_PlayerPrefsPersistenceRoundTrip()
        {
            GameRulesetData.ClearPlayerPrefs();

            var fallback = new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                "fallback",
                "Fallback",
                4,
                1500,
                200,
                60,
                120,
                true,
                false,
                2,
                false,
                0,
                15);

            // With no saved key, fallback is returned
            var loaded = GameRulesetData.LoadFromPlayerPrefs(fallback);
            Assert.That(loaded.RulesetId, Is.EqualTo("fallback"));

            // Save custom rules
            var custom = new GameRulesetSnapshot(
                GameRulesetLimits.CurrentSchemaVersion,
                "custom",
                "Custom Config",
                4,
                2500,
                400,
                90,
                120,
                false,
                true,
                3,
                true,
                500,
                15);

            GameRulesetData.SaveToPlayerPrefs(custom);

            var savedResult = GameRulesetData.LoadFromPlayerPrefs(fallback);
            Assert.That(savedResult.StartingCash, Is.EqualTo(2500));
            Assert.That(savedResult.PassStartCash, Is.EqualTo(400));
            Assert.That(savedResult.TurnDurationSeconds, Is.EqualTo(90));
            Assert.That(savedResult.MortgageEnabled, Is.False);
            Assert.That(savedResult.AuctionsEnabled, Is.True);
            Assert.That(savedResult.FullSetRentMultiplier, Is.EqualTo(3));
            Assert.That(savedResult.VacationRewardEnabled, Is.True);

            // Clean up
            GameRulesetData.ClearPlayerPrefs();
            var afterClear = GameRulesetData.LoadFromPlayerPrefs(fallback);
            Assert.That(afterClear.RulesetId, Is.EqualTo("fallback"));
        }

        [Test]
        public void GameRulesetData_HandlesNullAndCorruptedJsonGracefully()
        {
            GameRulesetData nullResult = GameRulesetData.FromJson(null);
            Assert.That(nullResult, Is.Not.Null);

            GameRulesetData emptyResult = GameRulesetData.FromJson(string.Empty);
            Assert.That(emptyResult, Is.Not.Null);

            GameRulesetData corruptResult = GameRulesetData.FromJson("{ invalid json: [");
            Assert.That(corruptResult, Is.Not.Null);
        }
    }
}
