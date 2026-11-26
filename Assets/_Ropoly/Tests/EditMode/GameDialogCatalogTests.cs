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
        public void RoundReportLog_CapsEntriesAndIgnoresBlankOrConsecutiveDuplicates()
        {
            RoundReportLog log = new RoundReportLog(maxEntries: 3);

            Assert.That(log.AddEntry(null), Is.False);
            Assert.That(log.AddEntry("   "), Is.False);
            Assert.That(log.AddEntry("• Player 1 bought Delhi ($60)"), Is.True);
            Assert.That(log.AddEntry("• Player 1 bought Delhi ($60)"), Is.False);
            Assert.That(log.AddEntry("• Player 2 bought Brasília ($100)"), Is.True);
            Assert.That(log.AddEntry("• Player 3 paid $80 rent to Player 1"), Is.True);
            Assert.That(log.AddEntry("• Player 4 was sent to Detention"), Is.True);

            Assert.That(log.Entries.Count, Is.EqualTo(3));
            Assert.That(log.Entries[0], Is.EqualTo("• Player 2 bought Brasília ($100)"));
            Assert.That(log.Entries[2], Is.EqualTo("• Player 4 was sent to Detention"));

            log.Clear();
            Assert.That(log.Entries, Is.Empty);
            Assert.That(log.FormatReport(), Is.EqualTo(string.Empty));
        }
    }
}
