using System;
using Ropoly.Core.Rules;
using UnityEngine;

namespace Ropoly.Infrastructure.Content
{
    [Serializable]
    public sealed class GameRulesetData
    {
        private const string PrefsKey = "ropoly_custom_ruleset_v1";

        public int schemaVersion = GameRulesetLimits.CurrentSchemaVersion;
        public string rulesetId = "custom-ropoly";
        public string displayName = "Custom Rules";
        public int playerCount = 4;
        public int startingCash = 1500;
        public int passStartCash = 200;
        public int turnDurationSeconds = 60;
        public int disconnectGraceSeconds = 120;
        public bool mortgageEnabled = true;
        public bool auctionsEnabled = false;
        public int fullSetRentMultiplier = 2;
        public bool vacationRewardEnabled = false;
        public int vacationReward = 0;
        public int auctionBidTimeoutSeconds = 15;

        public static GameRulesetData FromSnapshot(GameRulesetSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return new GameRulesetData();
            }

            return new GameRulesetData
            {
                schemaVersion = snapshot.SchemaVersion,
                rulesetId = snapshot.RulesetId,
                displayName = snapshot.DisplayName,
                playerCount = snapshot.PlayerCount,
                startingCash = snapshot.StartingCash,
                passStartCash = snapshot.PassStartCash,
                turnDurationSeconds = snapshot.TurnDurationSeconds,
                disconnectGraceSeconds = snapshot.DisconnectGraceSeconds,
                mortgageEnabled = snapshot.MortgageEnabled,
                auctionsEnabled = snapshot.AuctionsEnabled,
                fullSetRentMultiplier = snapshot.FullSetRentMultiplier,
                vacationRewardEnabled = snapshot.VacationRewardEnabled,
                vacationReward = snapshot.VacationReward,
                auctionBidTimeoutSeconds = snapshot.AuctionBidTimeoutSeconds
            };
        }

        public GameRulesetSnapshot ToSnapshot()
        {
            return new GameRulesetSnapshot(
                schemaVersion,
                string.IsNullOrWhiteSpace(rulesetId) ? "custom-ropoly" : rulesetId,
                string.IsNullOrWhiteSpace(displayName) ? "Custom Rules" : displayName,
                Mathf.Clamp(playerCount, GameRulesetLimits.MinimumPlayers, GameRulesetLimits.MaximumPlayers),
                Mathf.Max(100, startingCash),
                Mathf.Max(0, passStartCash),
                turnDurationSeconds,
                disconnectGraceSeconds,
                mortgageEnabled,
                auctionsEnabled,
                Mathf.Clamp(fullSetRentMultiplier, 1, 5),
                vacationRewardEnabled,
                vacationReward,
                auctionBidTimeoutSeconds > 0 ? auctionBidTimeoutSeconds : 15);
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static GameRulesetData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new GameRulesetData();
            }

            try
            {
                return JsonUtility.FromJson<GameRulesetData>(json) ?? new GameRulesetData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize ruleset data: {ex.Message}");
                return new GameRulesetData();
            }
        }

        public static void SaveToPlayerPrefs(GameRulesetSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            string json = FromSnapshot(snapshot).ToJson();
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }

        public static GameRulesetSnapshot LoadFromPlayerPrefs(GameRulesetSnapshot fallback)
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                return fallback;
            }

            string json = PlayerPrefs.GetString(PrefsKey);
            if (string.IsNullOrEmpty(json))
            {
                return fallback;
            }

            try
            {
                return FromJson(json).ToSnapshot();
            }
            catch
            {
                return fallback;
            }
        }

        public static void ClearPlayerPrefs()
        {
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                PlayerPrefs.DeleteKey(PrefsKey);
                PlayerPrefs.Save();
            }
        }
    }
}
