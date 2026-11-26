using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Ropoly.Presentation.Localization;
using UnityEngine;

namespace Ropoly.Tests.EditMode
{
    public sealed class GameDialogCatalogTests
    {
        [TearDown]
        public void TearDown()
        {
            GameDialogCatalog.ResetToDefaults();
        }

        [Test]
        public void AllDialogKeys_ExistInCatalogAndEnglishJson()
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("Localization/dialogs.en");
            Assert.That(jsonAsset, Is.Not.Null, "Resources/Localization/dialogs.en.json is missing.");

            FieldInfo[] keyFields = typeof(GameDialogKeys).GetFields(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            Assert.That(keyFields, Is.Not.Empty);

            GameDialogCatalog.SetLanguage("en");
            foreach (FieldInfo field in keyFields)
            {
                string key = field.GetValue(null) as string;
                Assert.That(string.IsNullOrWhiteSpace(key), Is.False, field.Name);
                string value = GameDialogCatalog.Get(key);
                Assert.That(value, Is.Not.EqualTo(key), $"Missing catalog template for '{key}'.");
                Assert.That(string.IsNullOrWhiteSpace(value), Is.False, $"Blank template for '{key}'.");
                Assert.That(
                    jsonAsset.text,
                    Does.Contain($"\"{key}\""),
                    $"Key '{key}' missing from dialogs.en.json.");
            }
        }

        [Test]
        public void Format_SubstitutesTokensAndSupportsLanguageOverrides()
        {
            string formatted = GameDialogCatalog.Format(
                GameDialogKeys.UnownedPropertyPrompt,
                ("property", "BRASÍLIA"),
                ("reward", string.Empty));
            Assert.That(formatted, Is.EqualTo("UNOWNED BRASÍLIA  •  BUY OR SKIP"));

            GameDialogCatalog.ApplyJsonOverrides(
                "{\"language\":\"es\",\"entries\":[{\"key\":\"report.bought_property\",\"text\":\"• {player} compró {property} (${amount})\"}]}");
            string localized = GameDialogCatalog.Format(
                GameDialogKeys.ReportBoughtProperty,
                ("player", "PLAYER 1"),
                ("property", "Delhi"),
                ("amount", "60"));
            Assert.That(localized, Is.EqualTo("• PLAYER 1 compró Delhi ($60)"));
        }

        [Test]
        public void RoundReportLog_KeepsFullHistoryByDefaultAndOptionallyCaps()
        {
            RoundReportLog unlimitedLog = new RoundReportLog();
            Assert.That(unlimitedLog.AddEntry(null), Is.False);
            Assert.That(unlimitedLog.AddEntry("   "), Is.False);

            for (int i = 1; i <= 12; i++)
            {
                Assert.That(unlimitedLog.AddEntry($"• Player {(i - 1) % 4 + 1} action {i}"), Is.True);
            }

            Assert.That(unlimitedLog.AddEntry("• Player 4 action 12"), Is.False);
            Assert.That(unlimitedLog.Entries.Count, Is.EqualTo(12));
            Assert.That(unlimitedLog.Entries[0], Is.EqualTo("• Player 1 action 1"));
            Assert.That(unlimitedLog.Entries[11], Is.EqualTo("• Player 4 action 12"));

            RoundReportLog cappedLog = new RoundReportLog(maxEntries: 3);
            Assert.That(cappedLog.AddEntry("• A"), Is.True);
            Assert.That(cappedLog.AddEntry("• B"), Is.True);
            Assert.That(cappedLog.AddEntry("• C"), Is.True);
            Assert.That(cappedLog.AddEntry("• D"), Is.True);
            Assert.That(cappedLog.Entries.Count, Is.EqualTo(3));
            Assert.That(cappedLog.Entries[0], Is.EqualTo("• B"));

            unlimitedLog.Clear();
            Assert.That(unlimitedLog.Entries, Is.Empty);
            Assert.That(unlimitedLog.FormatReport(), Is.EqualTo(string.Empty));
        }
    }
}
