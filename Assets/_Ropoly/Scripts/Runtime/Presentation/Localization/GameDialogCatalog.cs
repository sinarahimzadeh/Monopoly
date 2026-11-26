using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ropoly.Presentation.Localization
{
    /// <summary>
    /// Centralized dialog keys for board prompts, match notifications, and round reports.
    /// Add or remove keys here alongside <c>Resources/Localization/dialogs.*.json</c>.
    /// </summary>
    public static class GameDialogKeys
    {
        public const string LobbyChooseCreature = "status.lobby_choose_creature";
        public const string MatchReady = "status.match_ready";
        public const string PlayerRollTurn = "status.player_roll_turn";
        public const string RollingDice = "status.rolling_dice";
        public const string PlayerMoving = "status.player_moving";
        public const string PassedStartSuffix = "status.passed_start_suffix";

        public const string DetentionWaiverOrDoubles = "status.detention_waiver_or_doubles";
        public const string DetentionBailOrDoubles = "status.detention_bail_or_doubles";
        public const string DetentionDoublesOnly = "status.detention_doubles_only";
        public const string DetentionUsedWaiver = "status.detention_used_waiver";
        public const string DetentionPaidBail = "status.detention_paid_bail";
        public const string DetentionRemains = "status.detention_remains";
        public const string DetentionThreeDoubles = "status.detention_three_doubles";
        public const string DetentionGoDirectly = "status.detention_go_directly";
        public const string DetentionNowIn = "status.detention_now_in";

        public const string UnownedPropertyPrompt = "status.unowned_property_prompt";
        public const string OwnPropertyUpgradePrompt = "status.own_property_upgrade_prompt";
        public const string BoughtProperty = "status.bought_property";
        public const string SkippedProperty = "status.skipped_property";
        public const string CannotAffordSkip = "status.cannot_afford_skip";
        public const string CannotUpgrade = "status.cannot_upgrade";
        public const string BuiltUpgradeMore = "status.built_upgrade_more";
        public const string UpgradeComplete = "status.upgrade_complete";

        public const string CardDrawn = "status.card_drawn";
        public const string VacationReward = "status.vacation_reward";
        public const string LandedOnTile = "status.landed_on_tile";

        public const string DebtNeeded = "status.debt_needed";
        public const string RentPaid = "status.rent_paid";
        public const string MortgageFailed = "status.mortgage_failed";
        public const string DowngradeFailed = "status.downgrade_failed";
        public const string SaleFailed = "status.sale_failed";
        public const string MoreCashNeeded = "status.more_cash_needed";
        public const string ActionPropertyMortgaged = "action.property_mortgaged";
        public const string ActionBuildingSold = "action.building_sold";
        public const string ActionPropertySoldToBank = "action.property_sold_to_bank";

        public const string MovementErrorStart = "status.movement_error_start";
        public const string MovementErrorComplete = "status.movement_error_complete";

        public const string TradeNoOpponents = "status.trade_no_opponents";
        public const string TradeProposed = "status.trade_proposed";
        public const string TradeCompleted = "status.trade_completed";
        public const string TradeRejected = "status.trade_rejected";
        public const string TradeCancelled = "status.trade_cancelled";

        public const string AuctionHighestBidder = "auction.highest_bidder";
        public const string AuctionSoldTo = "auction.sold_to";
        public const string AuctionEndedUnowned = "auction.ended_unowned";

        public const string PlayerEliminated = "status.player_eliminated";
        public const string MatchWinner = "status.match_winner";
        public const string ReasonDebtTimeout = "reason.debt_timeout";
        public const string ReasonInactivityTimeout = "reason.inactivity_timeout";
        public const string ReasonDisconnectTimeout = "reason.disconnect_timeout";
        public const string ReasonBankruptcy = "reason.bankruptcy";
        public const string NounTheBank = "noun.the_bank";
        public const string NounProperty = "noun.property";
        public const string NounHotel = "noun.hotel";
        public const string NounHouseLevel = "noun.house_level";

        public const string ReportBoughtProperty = "report.bought_property";
        public const string ReportSkippedProperty = "report.skipped_property";
        public const string ReportUpgradedProperty = "report.upgraded_property";
        public const string ReportPaidRent = "report.paid_rent";
        public const string ReportPaidFee = "report.paid_fee";
        public const string ReportAuctionWon = "report.auction_won";
        public const string ReportAuctionUnowned = "report.auction_unowned";
        public const string ReportCardDrawn = "report.card_drawn";
        public const string ReportVacationBonus = "report.vacation_bonus";
        public const string ReportPassedStart = "report.passed_start";
        public const string ReportSentToDetention = "report.sent_to_detention";
        public const string ReportEscapedDetentionWaiver = "report.escaped_detention_waiver";
        public const string ReportEscapedDetentionBail = "report.escaped_detention_bail";
        public const string ReportEscapedDetentionDoubles = "report.escaped_detention_doubles";
        public const string ReportTradeCompleted = "report.trade_completed";
        public const string ReportPropertyMortgaged = "report.property_mortgaged";
        public const string ReportBuildingSold = "report.building_sold";
        public const string ReportPropertySold = "report.property_sold";
        public const string ReportPlayerEliminated = "report.player_eliminated";
        public const string ReportMatchWinner = "report.match_winner";
    }

    /// <summary>
    /// Centralized localization and formatting catalog for all board status prompts and round report entries.
    /// Loads optional overrides from <c>Resources/Localization/dialogs.&lt;lang&gt;.json</c> with built-in English fallbacks.
    /// </summary>
    public static class GameDialogCatalog
    {
        [Serializable]
        private sealed class DialogFilePayload
        {
            public string language = string.Empty;
            public DialogEntryPayload[] entries = Array.Empty<DialogEntryPayload>();
        }

        [Serializable]
        private sealed class DialogEntryPayload
        {
            public string key = string.Empty;
            public string text = string.Empty;
        }

        private static readonly Dictionary<string, string> DefaultTemplates = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GameDialogKeys.LobbyChooseCreature] = "LOCAL LOBBY  •  CHOOSE YOUR CREATURE",
            [GameDialogKeys.MatchReady] = "MATCH READY  •  ROLL THE DICE",
            [GameDialogKeys.PlayerRollTurn] = "{player}  •  ROLL THE DICE",
            [GameDialogKeys.RollingDice] = "ROLLING THE DICE...",
            [GameDialogKeys.PlayerMoving] = "{player}  •  MOVING {spaces} SPACES",
            [GameDialogKeys.PassedStartSuffix] = "  •  PASSED START +${amount}",

            [GameDialogKeys.DetentionWaiverOrDoubles] = "{player}  •  IN DETENTION  •  ROLL DOUBLES OR USE WAIVER",
            [GameDialogKeys.DetentionBailOrDoubles] = "{player}  •  IN DETENTION  •  ROLL DOUBLES OR PAY $50 BAIL",
            [GameDialogKeys.DetentionDoublesOnly] = "{player}  •  IN DETENTION  •  ROLL DOUBLES TO ESCAPE",
            [GameDialogKeys.DetentionUsedWaiver] = "{player} USED WAIVER  •  ROLL THE DICE",
            [GameDialogKeys.DetentionPaidBail] = "{player} PAID $50 BAIL  •  ROLL THE DICE",
            [GameDialogKeys.DetentionRemains] = "{player} REMAINS IN DETENTION (TURN {turn}/3)",
            [GameDialogKeys.DetentionThreeDoubles] = "{player} ROLLED 3 DOUBLES  •  SENT DIRECTLY TO DETENTION",
            [GameDialogKeys.DetentionGoDirectly] = "{player}  •  GO DIRECTLY TO DETENTION",
            [GameDialogKeys.DetentionNowIn] = "{player} IS NOW IN DETENTION",

            [GameDialogKeys.UnownedPropertyPrompt] = "UNOWNED {property}  •  BUY OR SKIP{reward}",
            [GameDialogKeys.OwnPropertyUpgradePrompt] = "{player}  •  UPGRADE {property} FOR ${cost} OR FINISH TURN{reward}",
            [GameDialogKeys.BoughtProperty] = "BOUGHT {property}",
            [GameDialogKeys.SkippedProperty] = "SKIPPED {property}",
            [GameDialogKeys.CannotAffordSkip] = "NOT ENOUGH CASH  •  SKIP THIS PROPERTY",
            [GameDialogKeys.CannotUpgrade] = "CANNOT UPGRADE  •  {reason}",
            [GameDialogKeys.BuiltUpgradeMore] = "BUILT {building} ON {property}  •  UPGRADE AGAIN OR FINISH TURN",
            [GameDialogKeys.UpgradeComplete] = "UPGRADE COMPLETE  •  END TURN",

            [GameDialogKeys.CardDrawn] = "{player}  •  {description}{reward}",
            [GameDialogKeys.VacationReward] = "{player}  •  COLLECTED VACATION REWARD +${amount}{reward}",
            [GameDialogKeys.LandedOnTile] = "LANDED ON {tile}{reward}",

            [GameDialogKeys.DebtNeeded] = "{player} NEEDS ${amount} FOR {property}{reward}",
            [GameDialogKeys.RentPaid] = "{player} PAID {owner} ${amount} FOR {property}{reward}",
            [GameDialogKeys.MortgageFailed] = "MORTGAGE FAILED  •  {reason}",
            [GameDialogKeys.DowngradeFailed] = "DOWNGRADE FAILED  •  {reason}",
            [GameDialogKeys.SaleFailed] = "SALE FAILED  •  {reason}",
            [GameDialogKeys.MoreCashNeeded] = "{action}  •  MORE CASH IS STILL NEEDED",
            [GameDialogKeys.ActionPropertyMortgaged] = "PROPERTY MORTGAGED",
            [GameDialogKeys.ActionBuildingSold] = "BUILDING SOLD (+${amount})",
            [GameDialogKeys.ActionPropertySoldToBank] = "PROPERTY SOLD TO BANK",

            [GameDialogKeys.MovementErrorStart] = "COULD NOT MOVE THE CURRENT PLAYER",
            [GameDialogKeys.MovementErrorComplete] = "COULD NOT COMPLETE PLAYER MOVEMENT",

            [GameDialogKeys.TradeNoOpponents] = "NO OPPONENTS AVAILABLE TO TRADE",
            [GameDialogKeys.TradeProposed] = "{proposer} PROPOSED A TRADE TO {target}",
            [GameDialogKeys.TradeCompleted] = "TRADE COMPLETED: {proposer} & {target}",
            [GameDialogKeys.TradeRejected] = "{target} REJECTED THE TRADE OFFER",
            [GameDialogKeys.TradeCancelled] = "TRADE OFFER WAS CANCELLED",

            [GameDialogKeys.AuctionHighestBidder] = "YOU ARE THE CURRENT HIGHEST BIDDER",
            [GameDialogKeys.AuctionSoldTo] = "SOLD TO {winner} FOR ${amount}!",
            [GameDialogKeys.AuctionEndedUnowned] = "AUCTION ENDED  •  PROPERTY REMAINS UNOWNED",

            [GameDialogKeys.PlayerEliminated] = "{player} ELIMINATED  •  {reason}",
            [GameDialogKeys.MatchWinner] = "{winner} WINS THE MATCH",
            [GameDialogKeys.ReasonDebtTimeout] = "DEBT TIMEOUT",
            [GameDialogKeys.ReasonInactivityTimeout] = "INACTIVITY",
            [GameDialogKeys.ReasonDisconnectTimeout] = "DISCONNECTED",
            [GameDialogKeys.ReasonBankruptcy] = "BANKRUPTCY",
            [GameDialogKeys.NounTheBank] = "THE BANK",
            [GameDialogKeys.NounProperty] = "PROPERTY",
            [GameDialogKeys.NounHotel] = "HOTEL",
            [GameDialogKeys.NounHouseLevel] = "HOUSE {level}",

            [GameDialogKeys.ReportBoughtProperty] = "• {player} bought {property} (${amount})",
            [GameDialogKeys.ReportSkippedProperty] = "• {player} skipped {property}",
            [GameDialogKeys.ReportUpgradedProperty] = "• {player} built {building} on {property}",
            [GameDialogKeys.ReportPaidRent] = "• {player} paid ${amount} rent to {owner} ({property})",
            [GameDialogKeys.ReportPaidFee] = "• {player} paid ${amount} {property}",
            [GameDialogKeys.ReportAuctionWon] = "• {winner} won {property} at auction (${amount})",
            [GameDialogKeys.ReportAuctionUnowned] = "• {property} stayed unowned after auction",
            [GameDialogKeys.ReportCardDrawn] = "• {player} drew {card}",
            [GameDialogKeys.ReportVacationBonus] = "• {player} collected ${amount} vacation bonus",
            [GameDialogKeys.ReportPassedStart] = "• {player} passed Start (+${amount})",
            [GameDialogKeys.ReportSentToDetention] = "• {player} was sent to Detention",
            [GameDialogKeys.ReportEscapedDetentionWaiver] = "• {player} used a waiver to leave Detention",
            [GameDialogKeys.ReportEscapedDetentionBail] = "• {player} paid $50 bail to leave Detention",
            [GameDialogKeys.ReportEscapedDetentionDoubles] = "• {player} rolled doubles to leave Detention",
            [GameDialogKeys.ReportTradeCompleted] = "• {proposer} traded with {target}",
            [GameDialogKeys.ReportPropertyMortgaged] = "• {player} mortgaged {property}",
            [GameDialogKeys.ReportBuildingSold] = "• {player} sold building on {property} (+${amount})",
            [GameDialogKeys.ReportPropertySold] = "• {player} sold {property} to bank",
            [GameDialogKeys.ReportPlayerEliminated] = "• {player} eliminated ({reason})",
            [GameDialogKeys.ReportMatchWinner] = "• {winner} won the match!",
        };

        private static readonly Dictionary<string, string> ActiveTemplates =
            new Dictionary<string, string>(DefaultTemplates, StringComparer.Ordinal);

        private static bool _initialized;

        public static string CurrentLanguage { get; private set; } = "en";

        public static IReadOnlyCollection<string> AllKeys => DefaultTemplates.Keys;

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            SetLanguage("en");
        }

        /// <summary>
        /// Loads dialog translations from <c>Resources/Localization/dialogs.&lt;languageCode&gt;.json</c>.
        /// Falls back to built-in English defaults for any missing key.
        /// </summary>
        public static bool SetLanguage(string languageCode)
        {
            _initialized = true;
            ResetToDefaults();

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                CurrentLanguage = "en";
                return false;
            }

            string normalized = languageCode.Trim().ToLowerInvariant();
            CurrentLanguage = normalized;

            TextAsset asset = Resources.Load<TextAsset>($"Localization/dialogs.{normalized}");
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return normalized == "en";
            }

            return ApplyJsonOverrides(asset.text);
        }

        /// <summary>
        /// Applies a JSON string containing localized dialog entries on top of the active table.
        /// </summary>
        public static bool ApplyJsonOverrides(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            DialogFilePayload payload = JsonUtility.FromJson<DialogFilePayload>(json);
            if (payload?.entries == null)
            {
                return false;
            }

            foreach (DialogEntryPayload entry in payload.entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.key) && entry.text != null)
                {
                    ActiveTemplates[entry.key] = entry.text;
                }
            }

            return true;
        }

        public static void RegisterOverrides(IReadOnlyDictionary<string, string> overrides)
        {
            EnsureInitialized();
            if (overrides == null)
            {
                return;
            }

            foreach (KeyValuePair<string, string> pair in overrides)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
                {
                    ActiveTemplates[pair.Key] = pair.Value;
                }
            }
        }

        public static void ResetToDefaults()
        {
            ActiveTemplates.Clear();
            foreach (KeyValuePair<string, string> pair in DefaultTemplates)
            {
                ActiveTemplates[pair.Key] = pair.Value;
            }

            CurrentLanguage = "en";
        }

        public static string Get(string key)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            return ActiveTemplates.TryGetValue(key, out string template)
                ? template
                : key;
        }

        public static string Format(string key, params (string Token, object Value)[] arguments)
        {
            string template = Get(key);
            if (arguments == null || arguments.Length == 0 || string.IsNullOrEmpty(template))
            {
                return template;
            }

            string result = template;
            for (int i = 0; i < arguments.Length; i++)
            {
                (string token, object value) = arguments[i];
                if (string.IsNullOrEmpty(token))
                {
                    continue;
                }

                string placeholder = "{" + token + "}";
                string replacement = value?.ToString() ?? string.Empty;
                result = result.Replace(placeholder, replacement);
            }

            return result;
        }
    }
}
