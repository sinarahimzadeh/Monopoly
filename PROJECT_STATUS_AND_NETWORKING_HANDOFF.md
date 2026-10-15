# Ponomoly (Ropoly) — Project Status & Networking Handoff

**Date**: October 14, 2026  
**Document Purpose**: Summary of completed features, current architectural state, roadmap adjustment request, and technical execution plan for online multiplayer synchronization.

---

## 1. Executive Summary

The project is a 3D board game built in Unity (Universal Render Pipeline, .NET Standard 2.1).  
All foundational board mechanics, turn loops, debt/mortgage management, property upgrades, detention systems, and chance/event cards have been implemented, tested, and visually animated.

The user has requested an **order-of-operations change**:  
Instead of building **P-15 (Trading)** in a local hotseat environment first, we will **implement Networking & Online Multiplayer Synchronization first**, and then build trading natively on top of the networked architecture.

---

## 2. Completed Milestones Breakdown

### P-11 — Debt Resolution, Mortgages, and Bankruptcy
- **Core Engine**:
  - `AssetLiquidationResult`, `RentPaymentResult`, and mortgage status tracked on `PropertyState`.
  - Properties can be mortgaged to the bank for 50% value or unmortgaged for 55% value (with 10% bank interest).
  - Mortgaged properties earn zero rent.
  - When cash is insufficient for rent, tax, or bail, the game pauses turn progression and enters `TurnPhase.DebtResolution`.
  - Player can selectively mortgage or sell properties to raise the required funds.
  - Automatic `DeclareBankruptcy`: liquidates remaining assets, returns properties to the bank, eliminates the player, and skips them in all future turns.
  - When only one active player remains, the match automatically finishes with a declared winner.
- **Presentation**:
  - `DebtResolutionView.cs`: Interactive UI displaying amount owed, creditor (opponent or "THE BANK"), owned properties with mortgage buttons, and Bankruptcy declaration.

### P-12 — Inactivity / Disconnection Timer & Auto-Elimination
- **Core Engine**:
  - Configurable turn timer (`turnDurationSeconds`, default 60s) and disconnect grace timer (`disconnectGraceSeconds`, default 60s).
  - `CheckTurnTimeout(DateTimeOffset now)`: Automatically mortgaging assets to pay pending debts, or declaring bankruptcy and eliminating inactive/disconnected players when their timer expires.
- **Presentation**:
  - Dynamic countdown timer displayed on match HUD and debt resolution banners.

### P-13 — Property Upgrades & Buildings
- **Core Engine**:
  - Color-group monopoly rule: Upgrades are only unlocked when a player owns all cities in a color group.
  - Even-building rule enforced across all cities in the group (difference between max and min upgrade level $\le 1$).
  - 5 upgrade tiers per property:
    - Levels 1–4: Houses (costs configured per board: $50, $100, $150, $200 tiers).
    - Level 5: Hotel.
  - Rent progression scaling: Tiered rent based on house count and hotel.
  - Selling buildings: Houses/hotels can be sold back to the bank for 50% construction cost.
- **Presentation**:
  - 3D visual indicators on board tiles reflecting building level.
  - In-game upgrade prompts when landing on an unimproved property you already own.

### P-14 — Taxes/Fees, Detention, Vacation Jackpot, and Card Decks
- **Core Engine**:
  - **Special Spaces**:
    - *Start*: Lap cash bonus ($200) awarded on landing or passing.
    - *Income Fee / Luxury Fee*: Direct cash deduction; enters debt resolution if cash is insufficient.
    - *Vacation Space*: Optional jackpot pool collecting all paid taxes/fees/bail, awarded when a player lands on Vacation.
    - *Detention Space*: "Just Visiting" for normal landing; holds arrested players for up to 3 turns.
    - *Go to Detention (Tile 30)*: Sends player directly to Detention (Tile 10) without collecting Start bonus.
  - **Detention Escape Mechanics**:
    - Pay $50 bail before rolling.
    - Use a "Get Out of Detention Free" waiver card (recycled back to deck).
    - Roll doubles on turn 1, 2, or 3 (escapes and moves the rolled distance; cannot roll again on that turn).
    - Forced $50 bail on 3rd failed turn (triggers debt resolution if broke).
    - Rolling 3 consecutive doubles sends the player directly to Detention.
    - Imprisoned players continue collecting rent from owned properties per official rules.
  - **Decks & Cards**:
    - Two separate 12-card decks: *Local Opportunity* and *World Event* (24 cards total).
    - Effects supported: `CashBank`, `CashFromEachPlayer`, `CashToEachPlayer`, `MoveToTile`, `MoveRelative`, `MoveToNearest`, `PropertyRepair`, `GoToDetention`, `DetentionWaiver`.
- **Presentation & Animation Polish**:
  - Added `AnimateRelocation(...)` in `DiceController.cs`: Visually hops the 3D creature token to the target tile when a card relocates them (e.g. *Advance to Start*, *Nearest Airport*, or *Move back 3 spaces*).
  - Added `AnimateFlyToDetention(...)`: Direct arc leap across the board into Detention (Tile 10) for Tile 30, card effects, or 3 consecutive doubles.
  - Evaluates destination tile after relocation (prompting purchase if unowned, rent/debt if owned, Start rewards if Start).

### Quality & Test Coverage
- 16+ EditMode unit tests covering all core mechanics in `SpecialSpaceTests.cs`.
- All assemblies compile with **0 warnings and 0 errors**:
  - `Ropoly.Core.dll`
  - `Ropoly.Presentation.dll`
  - `Ropoly.Infrastructure.dll`
  - `Ropoly.Application.dll`
  - `Ropoly.EditModeTests.dll`
  - `Assembly-CSharp.dll`

---

## 3. Original Roadmap vs. User's Requested Adjustment

### Original Roadmap
1. P-11: Debt, mortgages, bankruptcy *(Done)*
2. P-12: Inactivity timer & auto-elimination *(Done)*
3. P-13: Property upgrades/buildings *(Done)*
4. P-14: Special spaces, detention, cards *(Done)*
5. **P-15: Player-to-player deals and trading** *(Originally next)*
6. **P-16: Match-end screen and rematch flow**
7. **Online multiplayer synchronization**

### User's Adjustment
> *"you think its possible to first do the networking thing and then create trade things ? i think that would make more sense"*

**Decision**: Switch the order. Proceed directly to **Online Multiplayer Synchronization**, then implement **P-15 (Trading)** and **P-16 (Match End / Rematch)** natively over the network.

---

## 4. Architectural Rationale for Doing Networking First

1. **Trading is inherently distributed**:
   - In a local hotseat match, trading requires passing the device or exposing negotiation details on a single screen.
   - In online multiplayer, Player A constructs an offer on their screen and clicks "Propose", which sends an RPC to Player B's screen displaying an interactive "Accept / Decline / Counter" dialog.
   - Building trading locally first would mean writing temporary hotseat UI and state logic that must be refactored or rewritten for networking immediately after.
2. **The core game is 100% playable and ready to network**:
   - The entire board game loop (dice, token movement, purchases, upgrades, rent, fees, mortgages, detention, cards, debt, bankruptcy, win condition) is already fully working and verified in `LocalMatchSession`.
   - Wiring this up to networking enables real 2+ player matches across separate game instances immediately.
3. **P-16 (Match End & Rematch) is also network-dependent**:
   - Managing game-over screens, returning players to room lobbies, and handling host migration/rematches are fundamentally multiplayer flows.

---

## 5. Technical Plan for Online Multiplayer Synchronization

### Recommended Stack
- **Framework**: **Unity Netcode for GameObjects (NGO)** (`com.unity.netcode.gameobjects`) with **Unity Transport (UTP)** (`com.unity.transport`).
- **Network Topology**: **Host-Authoritative / Client-Server**.
  - **Host**: Runs the authoritative `MatchSession` domain model. Only the host executes game state transitions and validates rules.
  - **Clients**: Pure presentation and input dispatchers.

### Implementation Steps

#### Step 1: Package Installation & Assembly Configuration
- Add `com.unity.netcode.gameobjects` and `com.unity.transport` to `Packages/manifest.json`.
- Configure assembly definition references (`Ropoly.Multiplayer` or extending `Ropoly.Presentation`).

#### Step 2: Network Manager & Lobby Scene
- Create `NetworkManager` setup with `UnityTransport`.
- Support two connection modes:
  1. **LAN / Direct IP**: Fast testing with two local instances (e.g. ParrelSync or Standalone + Editor).
  2. **Room Code / Relay**: For internet connectivity without port forwarding.
- Lobby screen: Host creates match with player slots; Clients join and pick their creature character.

#### Step 3: Networked Match Controller (`NetworkMatchSession`)
- Wrap or interface with the core `MatchSession` logic:
  - **Client-to-Host RPCs (`[ServerRpc]`)**:
    - `RequestRollDiceServerRpc()`
    - `RequestPurchasePropertyServerRpc()`
    - `RequestDeclinePropertyServerRpc()`
    - `RequestUpgradePropertyServerRpc(tileIndex)`
    - `RequestMortgagePropertyServerRpc(tileIndex)`
    - `RequestPayDebtServerRpc()`
    - `RequestDeclareBankruptcyServerRpc()`
    - `RequestEndTurnServerRpc()`
  - **Host-to-Client RPCs / NetworkVariables (`[ClientRpc]`)**:
    - Syncing turn phase, current player index, deadline countdown.
    - Broadcasting dice roll results (animates dice simultaneously on all clients).
    - Broadcasting player movement & relocation events (animates creature tokens on all clients).
    - Broadcasting card draw events and board ownership updates (colors, house models).

#### Step 4: Native Multiplayer Trading (P-15 on Network)
- Built directly using network messaging:
  - `ProposeTradeServerRpc(TradeOfferPayload)` $\rightarrow$ Host validates assets.
  - Host sends `ReceiveTradeOfferClientRpc` to target player client only.
  - Target player responds with `AcceptTradeServerRpc` or `DeclineTradeServerRpc`.
  - Host atomically executes transfer and updates both players.

#### Step 5: Match End & Rematch (P-16 on Network)
- When 1 player remains, host broadcasts `MatchFinishedClientRpc(winnerIndex)`.
- Victory/Defeat screen shown with "Rematch" or "Return to Lobby".

---

## 6. Key Codebase File Locations

| Component | File Path |
| :--- | :--- |
| **Match Session Engine** | `Assets/_Ropoly/Scripts/Runtime/Core/Match/LocalMatchSession.cs` |
| **Player State** | `Assets/_Ropoly/Scripts/Runtime/Core/Match/PlayerState.cs` |
| **Turn State** | `Assets/_Ropoly/Scripts/Runtime/Core/Match/TurnState.cs` |
| **Property State** | `Assets/_Ropoly/Scripts/Runtime/Core/Match/PropertyState.cs` |
| **Special Space Model** | `Assets/_Ropoly/Scripts/Runtime/Core/Match/SpecialSpaceSnapshot.cs` |
| **Card System & Decks** | `Assets/_Ropoly/Scripts/Runtime/Core/Cards/DefaultCardDecks.cs` |
| **Dice & Token Controller** | `Assets/_Ropoly/Scripts/Runtime/Presentation/Dice/DiceController.cs` |
| **Board Presentation** | `Assets/_Ropoly/Scripts/Runtime/Presentation/Board/BoardPreviewController.cs` |
| **Creature Token Visual** | `Assets/_Ropoly/Scripts/Runtime/Presentation/Players/CreatureTokenView.cs` |
| **Debt Resolution UI** | `Assets/_Ropoly/Scripts/Runtime/Presentation/Debt/DebtResolutionView.cs` |
| **Lobby Controller** | `Assets/_Ropoly/Scripts/Runtime/Presentation/Lobby/LocalLobbyController.cs` |
| **Automated Tests** | `Assets/_Ropoly/Tests/EditMode/SpecialSpaceTests.cs` |

---
*Ready for handoff and next phase execution.*
