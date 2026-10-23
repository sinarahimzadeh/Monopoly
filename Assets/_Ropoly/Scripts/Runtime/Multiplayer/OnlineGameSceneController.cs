using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ropoly.Core.Auctions;
using Ropoly.Core.Match;
using Ropoly.Core.Rules;
using Ropoly.Core.Trading;
using Ropoly.Infrastructure.Content;
using Ropoly.Multiplayer.Protocol;
using Ropoly.Presentation.Dice;
using Ropoly.Presentation.Lobby;
using UnityEngine;

namespace Ropoly.Multiplayer
{
    /// <summary>
    /// Connects the existing deterministic local match presentation to online authority.
    /// The host validates every command and is the only peer allowed to produce game events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OnlineGameSceneController : MonoBehaviour
    {
        private readonly HashSet<ulong> _readyClients = new();
        private OnlineGameService _service;
        private LocalLobbyController _lobby;
        private DiceController _dice;
        private bool _matchReady;
        private bool _initialized;

        public static void Install(OnlineGameService service)
        {
            if (service == null || FindFirstObjectByType<OnlineGameSceneController>() != null)
            {
                return;
            }

            var controllerObject = new GameObject("Online Game Scene Controller");
            var controller = controllerObject.AddComponent<OnlineGameSceneController>();
            controller._service = service;
            controller.StartCoroutine(controller.InitializeWhenSceneIsReady());
        }

        private IEnumerator InitializeWhenSceneIsReady()
        {
            yield return null;

            _lobby = FindFirstObjectByType<LocalLobbyController>();
            _dice = FindFirstObjectByType<DiceController>();
            if (_lobby == null || _dice == null)
            {
                Debug.LogError("The online match could not find the Game scene controllers.", this);
                yield break;
            }

            OnlineLobbyPlayerData[] players = (_service.LobbyState.Players ??
                                                Array.Empty<OnlineLobbyPlayerData>())
                .OrderBy(player => player.SeatIndex)
                .ToArray();
            string[] creatureIds = players.Select(player => player.CreatureId).ToArray();
            GameRulesetSnapshot authoritativeRules = !string.IsNullOrEmpty(_service.LobbyState?.RulesJson)
                ? GameRulesetData.FromJson(_service.LobbyState.RulesJson).ToSnapshot()
                : _service.CurrentRules;

            if (players.Length < LocalMatchSession.MinimumPlayers ||
                creatureIds.Any(string.IsNullOrWhiteSpace) ||
                !_lobby.StartOnlineMatch(creatureIds, authoritativeRules))
            {
                Debug.LogError("The authoritative online lobby could not be applied to the Game scene.", this);
                yield break;
            }

            _dice.SetLocalSeatIndex(_service.LocalSeatIndex);
            _dice.ConfigureExternalAuthority(HandleLocalAction, _service.IsHost);
            _service.MatchCommandReceived += HandleMatchCommand;
            _service.MatchEventReceived += HandleMatchEvent;
            _service.OnlineClientDisconnected += HandleClientDisconnected;
            _lobby.MatchStarted += HandleLobbyMatchStarted;
            if (_service.IsHost)
            {
                _lobby.Session.PlayerEliminated += HandleAuthoritativePlayerEliminated;
                _lobby.Session.AuctionStarted += HandleAuthoritativeAuctionStarted;
                _lobby.Session.AuctionBidPlaced += HandleAuthoritativeAuctionBidPlaced;
                _lobby.Session.AuctionPlayerPassed += HandleAuthoritativeAuctionPlayerPassed;
                _lobby.Session.AuctionCompleted += HandleAuthoritativeAuctionCompleted;
            }
            _service.MarkMatchActive();
            _initialized = true;
            _service.FlushPendingMatchCommands();
            _service.SendMatchCommand(OnlineMatchCommandKind.SceneReady);
        }

        private void LateUpdate()
        {
            if (!_initialized || _dice == null || _lobby?.Session?.State?.Turn == null)
            {
                return;
            }

            bool ownsTurn = _service.LocalSeatIndex ==
                            _lobby.Session.State.Turn.CurrentPlayerIndex;
            _dice.SetExternalInputAllowed(_matchReady && ownsTurn);
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.MatchCommandReceived -= HandleMatchCommand;
                _service.MatchEventReceived -= HandleMatchEvent;
                _service.OnlineClientDisconnected -= HandleClientDisconnected;
            }

            if (_lobby != null)
            {
                _lobby.MatchStarted -= HandleLobbyMatchStarted;
            }

            if (_service?.IsHost == true && _lobby?.Session != null)
            {
                _lobby.Session.PlayerEliminated -= HandleAuthoritativePlayerEliminated;
                _lobby.Session.AuctionStarted -= HandleAuthoritativeAuctionStarted;
                _lobby.Session.AuctionBidPlaced -= HandleAuthoritativeAuctionBidPlaced;
                _lobby.Session.AuctionPlayerPassed -= HandleAuthoritativeAuctionPlayerPassed;
                _lobby.Session.AuctionCompleted -= HandleAuthoritativeAuctionCompleted;
            }

            _dice?.SetLocalSeatIndex(-1);
            _dice?.ClearExternalAuthority();
        }

        private void HandleLocalAction(ExternalTurnAction action, int intValue, string stringValue)
        {
            OnlineMatchCommandKind command = action switch
            {
                ExternalTurnAction.RollDice => OnlineMatchCommandKind.RollDice,
                ExternalTurnAction.AdvanceTurn => OnlineMatchCommandKind.AdvanceTurn,
                ExternalTurnAction.PurchaseProperty => OnlineMatchCommandKind.PurchaseProperty,
                ExternalTurnAction.DeclineProperty => OnlineMatchCommandKind.DeclineProperty,
                ExternalTurnAction.UpgradeProperty => OnlineMatchCommandKind.UpgradeProperty,
                ExternalTurnAction.UseDetentionWaiver => OnlineMatchCommandKind.UseDetentionWaiver,
                ExternalTurnAction.PayDetentionBail => OnlineMatchCommandKind.PayDetentionBail,
                ExternalTurnAction.MortgageProperty => OnlineMatchCommandKind.MortgageProperty,
                ExternalTurnAction.SellProperty => OnlineMatchCommandKind.SellProperty,
                ExternalTurnAction.DeclareBankruptcy => OnlineMatchCommandKind.DeclareBankruptcy,
                ExternalTurnAction.ProposeTrade => OnlineMatchCommandKind.ProposeTrade,
                ExternalTurnAction.AcceptTrade => OnlineMatchCommandKind.AcceptTrade,
                ExternalTurnAction.RejectTrade => OnlineMatchCommandKind.RejectTrade,
                ExternalTurnAction.CancelTrade => OnlineMatchCommandKind.CancelTrade,
                ExternalTurnAction.RequestRematch => OnlineMatchCommandKind.RequestRematch,
                ExternalTurnAction.ReturnToLobby => OnlineMatchCommandKind.ReturnToLobby,
                ExternalTurnAction.PlaceAuctionBid => OnlineMatchCommandKind.PlaceAuctionBid,
                ExternalTurnAction.PassAuction => OnlineMatchCommandKind.PassAuction,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
            };
            _service.SendMatchCommand(command, intValue, stringValue);
        }

        private void HandleMatchCommand(ulong senderClientId, OnlineMatchCommandData command)
        {
            if (!_service.IsHost || command == null)
            {
                return;
            }

            if (command.Kind == OnlineMatchCommandKind.SceneReady)
            {
                if (_service.TryGetSeatIndex(senderClientId, out _))
                {
                    _readyClients.Add(senderClientId);
                }

                int expectedCount = _service.LobbyState.Players?.Length ?? 0;
                if (!_matchReady && expectedCount >= 2 && _readyClients.Count >= expectedCount)
                {
                    _matchReady = true;
                    _service.BroadcastMatchEvent(new OnlineMatchEventData
                    {
                        Kind = OnlineMatchEventKind.MatchReady
                    });
                }

                return;
            }

            if (!_matchReady ||
                !_service.TryGetSeatIndex(senderClientId, out int senderSeat) ||
                _lobby.Session.State.Turn == null)
            {
                return;
            }

            bool isTradeCommand = command.Kind is OnlineMatchCommandKind.ProposeTrade
                or OnlineMatchCommandKind.AcceptTrade
                or OnlineMatchCommandKind.RejectTrade
                or OnlineMatchCommandKind.CancelTrade;

            bool isMatchEndCommand = command.Kind is OnlineMatchCommandKind.RequestRematch
                or OnlineMatchCommandKind.ReturnToLobby;

            bool isAuctionCommand = command.Kind is OnlineMatchCommandKind.PlaceAuctionBid
                or OnlineMatchCommandKind.PassAuction;

            if (!isTradeCommand && !isMatchEndCommand && !isAuctionCommand && senderSeat != _lobby.Session.State.Turn.CurrentPlayerIndex)
            {
                return;
            }

            OnlineMatchEventData matchEvent = command.Kind switch
            {
                OnlineMatchCommandKind.RollDice => ApplyHostDiceRoll(),
                OnlineMatchCommandKind.AdvanceTurn => ApplyHostAdvanceTurn(),
                OnlineMatchCommandKind.PurchaseProperty => ApplyHostPropertyPurchase(),
                OnlineMatchCommandKind.DeclineProperty => ApplyHostPropertyDecline(),
                OnlineMatchCommandKind.UpgradeProperty => ApplyHostPropertyUpgrade(command.IntValue),
                OnlineMatchCommandKind.UseDetentionWaiver => ApplyHostDetentionWaiver(),
                OnlineMatchCommandKind.PayDetentionBail => ApplyHostDetentionBail(),
                OnlineMatchCommandKind.MortgageProperty => ApplyHostMortgage(command.IntValue),
                OnlineMatchCommandKind.SellProperty => ApplyHostSale(command.IntValue),
                OnlineMatchCommandKind.DeclareBankruptcy => ApplyHostBankruptcy(),
                OnlineMatchCommandKind.ProposeTrade => ApplyHostProposeTrade(senderSeat, command.StringValue),
                OnlineMatchCommandKind.AcceptTrade => ApplyHostAcceptTrade(senderSeat),
                OnlineMatchCommandKind.RejectTrade => ApplyHostRejectTrade(senderSeat),
                OnlineMatchCommandKind.CancelTrade => ApplyHostCancelTrade(senderSeat),
                OnlineMatchCommandKind.RequestRematch => ApplyHostRematch(),
                OnlineMatchCommandKind.ReturnToLobby => ApplyHostReturnToLobby(),
                OnlineMatchCommandKind.PlaceAuctionBid => ApplyHostAuctionBid(senderSeat, command.IntValue),
                OnlineMatchCommandKind.PassAuction => ApplyHostAuctionPass(senderSeat),
                _ => null
            };

            if (matchEvent != null)
            {
                _service.BroadcastMatchEvent(matchEvent);
            }
        }

        private OnlineMatchEventData ApplyHostDiceRoll()
        {
            int first = UnityEngine.Random.Range(1, 7);
            int second = UnityEngine.Random.Range(1, 7);
            return _dice.ApplyExternalDiceRoll(first, second)
                ? new OnlineMatchEventData
                {
                    Kind = OnlineMatchEventKind.DiceRolled,
                    FirstDie = first,
                    SecondDie = second
                }
                : null;
        }

        private OnlineMatchEventData ApplyHostAdvanceTurn()
        {
            return _dice.ApplyExternalAdvanceTurn()
                ? new OnlineMatchEventData { Kind = OnlineMatchEventKind.TurnAdvanced }
                : null;
        }

        private OnlineMatchEventData ApplyHostPropertyPurchase()
        {
            return _dice.ApplyExternalPropertyPurchase()
                ? new OnlineMatchEventData { Kind = OnlineMatchEventKind.PropertyPurchased }
                : null;
        }

        private OnlineMatchEventData ApplyHostPropertyDecline()
        {
            if (!_dice.ApplyExternalPropertyDecline())
            {
                return null;
            }

            if (_lobby.Session.ActiveAuction != null && _lobby.Session.ActiveAuction.IsActive)
            {
                return null;
            }

            return new OnlineMatchEventData { Kind = OnlineMatchEventKind.PropertyDeclined };
        }

        private OnlineMatchEventData ApplyHostAuctionBid(int senderSeat, int amount)
        {
            _lobby.Session.TryPlaceAuctionBid(senderSeat, amount);
            return null;
        }

        private OnlineMatchEventData ApplyHostAuctionPass(int senderSeat)
        {
            _lobby.Session.TryPassAuction(senderSeat);
            return null;
        }

        private OnlineMatchEventData ApplyHostPropertyUpgrade(int tileIndex)
        {
            return _dice.ApplyExternalPropertyUpgrade(tileIndex)
                ? new OnlineMatchEventData
                {
                    Kind = OnlineMatchEventKind.PropertyUpgraded,
                    IntValue = tileIndex
                }
                : null;
        }

        private OnlineMatchEventData ApplyHostDetentionWaiver()
        {
            return _dice.ApplyExternalDetentionWaiver()
                ? new OnlineMatchEventData { Kind = OnlineMatchEventKind.DetentionWaiverUsed }
                : null;
        }

        private OnlineMatchEventData ApplyHostDetentionBail()
        {
            return _dice.ApplyExternalDetentionBail()
                ? new OnlineMatchEventData { Kind = OnlineMatchEventKind.DetentionBailPaid }
                : null;
        }

        private OnlineMatchEventData ApplyHostMortgage(int tileIndex)
        {
            return _dice.ApplyExternalMortgageProperty(tileIndex)
                ? new OnlineMatchEventData
                {
                    Kind = OnlineMatchEventKind.PropertyMortgaged,
                    IntValue = tileIndex
                }
                : null;
        }

        private OnlineMatchEventData ApplyHostSale(int tileIndex)
        {
            return _dice.ApplyExternalSellProperty(tileIndex)
                ? new OnlineMatchEventData
                {
                    Kind = OnlineMatchEventKind.PropertySold,
                    IntValue = tileIndex
                }
                : null;
        }

        private OnlineMatchEventData ApplyHostBankruptcy()
        {
            _dice.ApplyExternalBankruptcy();
            return null;
        }

        private OnlineMatchEventData ApplyHostProposeTrade(int senderSeat, string payloadJson)
        {
            if (string.IsNullOrEmpty(payloadJson))
            {
                return null;
            }

            OnlineTradePayload payload;
            try
            {
                payload = JsonUtility.FromJson<OnlineTradePayload>(payloadJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse trade payload: {ex.Message}");
                return null;
            }

            if (payload == null || payload.ProposerSeat != senderSeat)
            {
                return null;
            }

            var offer = new TradeOffer(
                payload.TradeId,
                payload.ProposerSeat,
                payload.TargetSeat,
                payload.OfferedCash,
                payload.OfferedPropertyTileIndices,
                payload.OfferedWaivers,
                payload.RequestedCash,
                payload.RequestedPropertyTileIndices,
                payload.RequestedWaivers);

            if (!_dice.ApplyExternalProposeTrade(offer))
            {
                return null;
            }

            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.TradeProposed,
                StringValue = payloadJson
            };
        }

        private OnlineMatchEventData ApplyHostAcceptTrade(int senderSeat)
        {
            TradeOffer active = _lobby.Session.ActiveTradeOffer;
            if (active == null || active.TargetPlayerIndex != senderSeat)
            {
                return null;
            }

            if (!_dice.ApplyExternalAcceptTrade(senderSeat))
            {
                return null;
            }

            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.TradeAccepted,
                IntValue = senderSeat
            };
        }

        private OnlineMatchEventData ApplyHostRejectTrade(int senderSeat)
        {
            TradeOffer active = _lobby.Session.ActiveTradeOffer;
            if (active == null || active.TargetPlayerIndex != senderSeat)
            {
                return null;
            }

            if (!_dice.ApplyExternalRejectTrade(senderSeat))
            {
                return null;
            }

            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.TradeRejected,
                IntValue = senderSeat
            };
        }

        private OnlineMatchEventData ApplyHostCancelTrade(int senderSeat)
        {
            TradeOffer active = _lobby.Session.ActiveTradeOffer;
            if (active == null || active.ProposerPlayerIndex != senderSeat)
            {
                return null;
            }

            if (!_dice.ApplyExternalCancelTrade(senderSeat))
            {
                return null;
            }

            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.TradeCancelled,
                IntValue = senderSeat
            };
        }

        private void HandleMatchEvent(OnlineMatchEventData matchEvent)
        {
            if (matchEvent == null || _service.IsHost)
            {
                return;
            }

            switch (matchEvent.Kind)
            {
                case OnlineMatchEventKind.MatchReady:
                    _matchReady = true;
                    break;
                case OnlineMatchEventKind.DiceRolled:
                    _dice.ApplyExternalDiceRoll(matchEvent.FirstDie, matchEvent.SecondDie);
                    break;
                case OnlineMatchEventKind.TurnAdvanced:
                    _dice.ApplyExternalAdvanceTurn();
                    break;
                case OnlineMatchEventKind.PropertyPurchased:
                    _dice.ApplyExternalPropertyPurchase();
                    break;
                case OnlineMatchEventKind.PropertyDeclined:
                    _dice.ApplyExternalPropertyDecline();
                    break;
                case OnlineMatchEventKind.PropertyUpgraded:
                    _dice.ApplyExternalPropertyUpgrade(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.DetentionWaiverUsed:
                    _dice.ApplyExternalDetentionWaiver();
                    break;
                case OnlineMatchEventKind.DetentionBailPaid:
                    _dice.ApplyExternalDetentionBail();
                    break;
                case OnlineMatchEventKind.PropertyMortgaged:
                    _dice.ApplyExternalMortgageProperty(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.PropertySold:
                    _dice.ApplyExternalSellProperty(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.PlayerEliminated:
                    _lobby.Session.ApplyAuthoritativeElimination(
                        matchEvent.IntValue,
                        (EliminationReason)matchEvent.SecondaryIntValue);
                    break;
                case OnlineMatchEventKind.TradeProposed:
                    if (!string.IsNullOrEmpty(matchEvent.StringValue))
                    {
                        var payload = JsonUtility.FromJson<OnlineTradePayload>(matchEvent.StringValue);
                        if (payload != null)
                        {
                            var offer = new TradeOffer(
                                payload.TradeId,
                                payload.ProposerSeat,
                                payload.TargetSeat,
                                payload.OfferedCash,
                                payload.OfferedPropertyTileIndices,
                                payload.OfferedWaivers,
                                payload.RequestedCash,
                                payload.RequestedPropertyTileIndices,
                                payload.RequestedWaivers);
                            _dice.ApplyExternalProposeTrade(offer);
                        }
                    }
                    break;
                case OnlineMatchEventKind.TradeAccepted:
                    _dice.ApplyExternalAcceptTrade(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.TradeRejected:
                    _dice.ApplyExternalRejectTrade(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.TradeCancelled:
                    _dice.ApplyExternalCancelTrade(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.RematchStarted:
                    _dice.ApplyExternalRematch();
                    break;
                case OnlineMatchEventKind.ReturnedToLobby:
                    _dice.ApplyExternalReturnToLobby();
                    break;
                case OnlineMatchEventKind.AuctionStarted:
                    _lobby.Session.ApplyAuthoritativeAuctionStarted(matchEvent.IntValue, matchEvent.SecondaryIntValue);
                    break;
                case OnlineMatchEventKind.AuctionBidPlaced:
                    _dice.ApplyExternalAuctionBid(matchEvent.IntValue, matchEvent.SecondaryIntValue);
                    break;
                case OnlineMatchEventKind.AuctionPlayerPassed:
                    _dice.ApplyExternalAuctionPass(matchEvent.IntValue);
                    break;
                case OnlineMatchEventKind.AuctionCompleted:
                    _dice.ApplyExternalAuctionCompleted(matchEvent.IntValue, matchEvent.SecondaryIntValue);
                    break;
            }
        }

        private OnlineMatchEventData ApplyHostRematch()
        {
            _dice.ApplyExternalRematch();
            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.RematchStarted
            };
        }

        private OnlineMatchEventData ApplyHostReturnToLobby()
        {
            _dice.ApplyExternalReturnToLobby();
            return new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.ReturnedToLobby
            };
        }

        private void HandleLobbyMatchStarted(LocalMatchSession session)
        {
            if (_service != null && _service.IsHost && session != null)
            {
                session.PlayerEliminated -= HandleAuthoritativePlayerEliminated;
                session.PlayerEliminated += HandleAuthoritativePlayerEliminated;
                session.AuctionStarted -= HandleAuthoritativeAuctionStarted;
                session.AuctionStarted += HandleAuthoritativeAuctionStarted;
                session.AuctionBidPlaced -= HandleAuthoritativeAuctionBidPlaced;
                session.AuctionBidPlaced += HandleAuthoritativeAuctionBidPlaced;
                session.AuctionPlayerPassed -= HandleAuthoritativeAuctionPlayerPassed;
                session.AuctionPlayerPassed += HandleAuthoritativeAuctionPlayerPassed;
                session.AuctionCompleted -= HandleAuthoritativeAuctionCompleted;
                session.AuctionCompleted += HandleAuthoritativeAuctionCompleted;
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_service.IsHost && _service.TryGetSeatIndex(clientId, out int seatIndex))
            {
                _lobby.Session.TryDisconnectPlayer(seatIndex);
            }
        }

        private void HandleAuthoritativePlayerEliminated(
            int playerIndex,
            EliminationReason reason)
        {
            _service.BroadcastMatchEvent(new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.PlayerEliminated,
                IntValue = playerIndex,
                SecondaryIntValue = (int)reason
            });
        }

        private void HandleAuthoritativeAuctionStarted(PropertyAuctionState auction)
        {
            _service.BroadcastMatchEvent(new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.AuctionStarted,
                IntValue = auction.TileIndex,
                SecondaryIntValue = auction.TimeoutDurationSeconds,
                StringValue = auction.PropertyDisplayName
            });
        }

        private void HandleAuthoritativeAuctionBidPlaced(PropertyAuctionState auction, int bidderSeat, int amount)
        {
            _service.BroadcastMatchEvent(new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.AuctionBidPlaced,
                IntValue = bidderSeat,
                SecondaryIntValue = amount
            });
        }

        private void HandleAuthoritativeAuctionPlayerPassed(PropertyAuctionState auction, int playerSeat)
        {
            _service.BroadcastMatchEvent(new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.AuctionPlayerPassed,
                IntValue = playerSeat
            });
        }

        private void HandleAuthoritativeAuctionCompleted(PropertyAuctionState auction)
        {
            _service.BroadcastMatchEvent(new OnlineMatchEventData
            {
                Kind = OnlineMatchEventKind.AuctionCompleted,
                IntValue = auction.WinnerPlayerIndex,
                SecondaryIntValue = auction.WinningBid,
                StringValue = auction.PropertyDisplayName
            });
        }
    }
}
