using System;

namespace Ropoly.Multiplayer.Protocol
{
    public enum OnlineConnectionState
    {
        Offline,
        Initializing,
        Authenticating,
        CreatingRoom,
        JoiningRoom,
        InLobby,
        StartingMatch,
        InMatch,
        Leaving,
        Failed
    }

    public enum OnlineLobbyCommandKind
    {
        RequestState,
        SelectCreature,
        SetReady,
        StartMatch
    }

    public enum OnlineMatchCommandKind
    {
        SceneReady,
        RollDice,
        AdvanceTurn,
        PurchaseProperty,
        DeclineProperty,
        UpgradeProperty,
        UseDetentionWaiver,
        PayDetentionBail,
        MortgageProperty,
        SellProperty,
        DeclareBankruptcy
    }

    public enum OnlineMatchEventKind
    {
        MatchReady,
        DiceRolled,
        TurnAdvanced,
        PropertyPurchased,
        PropertyDeclined,
        PropertyUpgraded,
        DetentionWaiverUsed,
        DetentionBailPaid,
        PropertyMortgaged,
        PropertySold,
        BankruptcyDeclared,
        PlayerEliminated
    }

    [Serializable]
    public sealed class OnlineLobbyPlayerData
    {
        public string ClientId;
        public int SeatIndex;
        public string DisplayName;
        public string CreatureId;
        public bool IsReady;
        public bool IsHost;
    }

    [Serializable]
    public sealed class OnlineLobbyStateData
    {
        public string JoinCode;
        public bool MatchStarted;
        public OnlineLobbyPlayerData[] Players = Array.Empty<OnlineLobbyPlayerData>();
    }

    [Serializable]
    public sealed class OnlineLobbyCommandData
    {
        public OnlineLobbyCommandKind Kind;
        public string StringValue;
        public bool BoolValue;
    }

    [Serializable]
    public sealed class OnlineMatchCommandData
    {
        public OnlineMatchCommandKind Kind;
        public int IntValue;
    }

    [Serializable]
    public sealed class OnlineMatchEventData
    {
        public OnlineMatchEventKind Kind;
        public int FirstDie;
        public int SecondDie;
        public int IntValue;
        public int SecondaryIntValue;
    }
}
