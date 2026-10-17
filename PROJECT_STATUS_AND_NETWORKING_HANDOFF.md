# Ponomoly (Ropoly) — Project Status & Networking Handoff

**Date**: October 17, 2026
**Document Purpose**: Summary of completed features, current architecture, the implemented online multiplayer slice, validation status, and the remaining roadmap.

---

## 1. Executive Summary

The project is a 3D board game built in Unity (Universal Render Pipeline, .NET Standard 2.1).  
All foundational board mechanics, turn loops, debt/mortgage management, property upgrades, detention systems, and chance/event cards have been implemented, tested, and visually animated.

The user requested an **order-of-operations change**: networking was moved ahead of **P-15 (Trading)** so trading can be designed as an online-native system. The first real online multiplayer slice is now implemented: private Relay rooms, anonymous authentication, synchronized lobby/creature selection, host-authoritative gameplay commands, and disconnect handling. See Sections 7–12 for the current implementation and handoff details.

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

## 7. Online Multiplayer — Implemented State (October 17, 2026)

This section supersedes the future-tense networking plan above. The following is implemented in the working tree. **No commit was created by Codex.**

### Installed Unity packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `com.unity.services.multiplayer` | `2.4.0` | Unity Multiplayer Services sessions and Relay integration |
| `com.unity.netcode.gameobjects` | `2.13.3` | Netcode transport lifecycle and custom messages |
| `com.unity.transport` | `2.7.4` | Network transport used by NGO and Relay |
| `com.unity.multiplayer.playmode` | `2.0.2` | Runs multiple editor players for local multiplayer testing |
| `com.unity.multiplayer.tools` | `2.2.12` | Network diagnostics and visualization |

### Connection and room flow

- `OnlineGameService` is created automatically before the first scene loads and persists with `DontDestroyOnLoad`.
- A runtime `NetworkManager` and `UnityTransport` are created and configured explicitly.
- Unity Services initializes on demand.
- Every running game signs in anonymously using a process-specific authentication profile, allowing multiple editor instances on the same PC.
- The host creates a private Unity Multiplayer Services session backed by Relay and receives a room code.
- Other players join with that room code; no router configuration or port forwarding is required.
- The supported room size is 2–4 players.
- Leaving the room shuts down the network session and resets local online state.

### Main menu and online lobby

- The original Play button is changed to **LOCAL GAME**.
- A separate **ONLINE GAME** button is added at runtime.
- The online screen supports:
  - Create room.
  - Join by room code.
  - Room status and room code display.
  - Connected player list.
  - One creature selection per connected player.
  - Duplicate-creature prevention.
  - Ready/not-ready state.
  - Host-only Start Match button.
- The host can start only when at least two players have selected unique creatures and all players are ready.
- The room is locked when the match starts.

### Host-authoritative design

The host owns the authoritative `LocalMatchSession`. Clients do not directly modify match state.

1. A client presses a gameplay button.
2. The client sends a command to the host.
3. The host confirms that the sender owns the current turn and that the action is legal.
4. The host applies the action to its match session.
5. The host broadcasts the resulting event.
6. Every client applies the same event to its local presentation/session.

This prevents a client from choosing its own dice result, spending another player's money, acting outside its turn, or directly assigning property ownership.

### Currently synchronized commands/events

- Scene-ready handshake before controls are enabled.
- Dice rolls, with the host generating both die values.
- Turn advancement.
- Property purchase and decline.
- Property upgrades.
- Detention waiver usage.
- Detention bail payment.
- Property mortgage.
- Property/building sale through the existing sale action.
- Bankruptcy declaration.
- Authoritative player elimination.

The online scene controller only enables gameplay input for the client whose seat matches the current player index.

### Scene transition

- When the host starts the match, all peers load the existing `Game` scene.
- The local four-player selection overlay is bypassed for online matches.
- The synchronized lobby snapshot determines the online player count, seat order, and creature assignments.
- Each peer reports `SceneReady`; the host waits for all expected peers before broadcasting `MatchReady` and enabling input.

### Disconnect/inactivity behavior

- If a client disconnects during a match, the host marks that player's seat as disconnected.
- The existing authoritative disconnect grace timer is used.
- The configured default grace time is currently 60 seconds.
- On timeout, the existing elimination/bankruptcy rules apply and the host broadcasts the elimination to clients.

---

## 8. Multiplayer Files Added or Modified

| Purpose | File Path |
| :--- | :--- |
| Multiplayer assembly | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/Ropoly.Multiplayer.asmdef` |
| Persistent services, authentication, Relay rooms, messages | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/OnlineGameService.cs` |
| Runtime-generated main menu and online lobby UI | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/OnlineMenuPresenter.cs` |
| Host authority and online Game-scene adapter | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/OnlineGameSceneController.cs` |
| Multiplayer configuration ScriptableObject type | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/OnlineMultiplayerConfig.cs` |
| Commands, events, lobby DTOs, connection states | `Assets/_Ropoly/Scripts/Runtime/Multiplayer/Protocol/OnlineProtocol.cs` |
| Multiplayer configuration asset | `Assets/_Ropoly/Resources/Multiplayer/OnlineMultiplayerConfig.asset` |
| Two-editor test scenario | `Assets/Settings/PlayMode/NewPlayModeScenario.asset` |
| Applies an online lobby snapshot to the existing game | `Assets/_Ropoly/Scripts/Runtime/Presentation/Lobby/LocalLobbyController.cs` |
| Routes UI actions through external/online authority | `Assets/_Ropoly/Scripts/Runtime/Presentation/Dice/DiceController.cs` |
| Applies host-authoritative eliminations | `Assets/_Ropoly/Scripts/Runtime/Core/Match/LocalMatchSession.cs` |
| Default disconnect grace changed to 60 seconds | `Assets/_Ropoly/Content/Rulesets/ClassicRopoly.asset` |
| Bootstrap references multiplayer assembly | `Assets/_Ropoly/Scripts/Runtime/Bootstrap/Ropoly.Bootstrap.asmdef` |

---

## 9. Network Protocol Notes

The current implementation uses NGO **named custom messages**, not `NetworkBehaviour`, `NetworkObject`, `ServerRpc`, or `ClientRpc` components.

Message channels:

- `ropoly/lobby/command/v1`: client-to-host lobby requests.
- `ropoly/lobby/state/v1`: authoritative host-to-client lobby snapshot.
- `ropoly/match/command/v1`: client-to-host gameplay requests.
- `ropoly/match/event/v1`: authoritative host-to-client gameplay results.

Payloads are small serializable DTOs converted with `JsonUtility`. Messages use `ReliableSequenced` delivery. Any future protocol-breaking payload change should increment the `/v1` suffix and/or the NGO protocol version.

Important security/rules rule: never trust a client-supplied state result. A client may request an action, but the host must validate the sender's seat, current turn, phase, asset ownership, and legality before applying it.

---

## 10. Multiplayer Bugs Already Found and Fixed

### Online menu did not appear

**Symptom:** The main menu showed only one Play button and entered the local four-player lobby.

**Cause:** A `NetworkManager` added at runtime has no serialized `NetworkConfig`. `OnlineGameService.Awake()` dereferenced a null configuration and stopped before installing the online UI.

**Fix:** `OnlineGameService` now explicitly creates and assigns a `NetworkConfig` with `UnityTransport`, scene management disabled, connection approval disabled, and protocol version 1.

### Joining a room threw `OverflowException`

**Symptom:** `COULD NOT JOIN ROOM: System.OverflowException: Writing past the end of the buffer`.

**Cause:** The custom-message buffer capacity was calculated using a UTF-8 byte count, while `FastBufferWriter.WriteValueSafe(string)` writes UTF-16 characters by default.

**Fix:** Capacity now uses `FastBufferWriter.GetWriteSize(json)`, which exactly matches Netcode's serialization format.

---

## 11. Validation Performed

### Confirmed

- `Ropoly.Multiplayer.csproj` builds with 0 errors and 0 warnings.
- `Ropoly.Bootstrap.csproj` previously built with 0 errors and 0 warnings after adding the multiplayer reference.
- Unity successfully imports and compiles the multiplayer assembly.
- The main menu visibly displays **LOCAL GAME** and **ONLINE GAME**.
- A real private Relay room can be created.
- A second Multiplayer Play Mode editor instance can enter the code and reach **CONNECTED**.
- Both host and client show the same two lobby players.
- The host receives the client's lobby-state request after joining.
- `git diff --check` is clean.

### Test caveat

The full online gameplay matrix has **not** been exhaustively exercised. Room creation/joining and shared lobby membership were tested manually with two editor players. Every command path compiles, but a future AI should manually validate a complete two-player match, especially cards, debt-resolution branches, detention, disconnect timeout, bankruptcy, and match completion.

An earlier EditMode run reported 87 total tests: 76 passed and 11 existing failures around timeout/detention/special-space/property behavior. These were not treated as multiplayer regressions and were not changed during this slice, but they should be audited before calling the entire game test suite green.

---

## 12. How to Test Multiplayer in Unity

1. Open `Assets/_Ropoly/Scenes/App/Bootstrap.unity`.
2. Open **Window → Play Mode → Scenarios**.
3. Select `NewPlayModeScenario`; it contains the main editor plus one additional editor instance named Player 2.
4. Press Play and wait until both windows reach the Ropoly main menu.
5. In both windows, click **ONLINE GAME**. Do not click Local Game.
6. In the first window, click **CREATE ROOM** and read the room code.
7. In Player 2, enter that code and click **JOIN ROOM**.
8. Confirm Player 2 says **CONNECTED** and both windows list Player 1 and Player 2.
9. Each window selects one different creature and presses **READY**.
10. The host presses **START MATCH**.
11. Confirm both windows load the same match and only the current player's window can perform the turn action.

If the Game scene displays `LOCAL MATCH SETUP` and asks one window to choose several creatures, that window entered **LOCAL GAME** and is not in an online session.

---

## 13. Recommended Next Work

1. Run and document a complete two-player online match through multiple turns.
2. Fix any state divergence found in complex branches (cards, debt, detention, sale/downgrade, timeout, and bankruptcy).
3. Add reconnect behavior if the same player returns before the 60-second grace period expires. The current code detects disconnection but does not implement identity-based reconnection into the same seat.
4. Add clearer copy/share behavior for the room code.
5. Add dedicated multiplayer tests for protocol serialization, host validation, duplicate creatures, ready-state rules, and out-of-turn command rejection.
6. Implement P-15 trading as host-validated offer/counter/accept/decline commands and targeted trade notifications.
7. Implement P-16 match-end and rematch/return-to-lobby flow.
8. Decide later whether host migration is required. It is not currently implemented.

### Important limitations for the next AI

- Do not convert gameplay authority to client-authoritative logic.
- Do not let clients submit dice results or completed state snapshots.
- Trading is not implemented yet.
- Accounts, leaderboards, bots, mobile-specific networking, matchmaking, reconnection, host migration, and rematches are not implemented yet.
- The online UI is generated at runtime; it is not stored as a prefab or serialized scene hierarchy.
- Start testing from `Bootstrap.unity`, not directly from `Game.unity`.
- Preserve the project's rule that this is a 3D Unity project: do not introduce 2D colliders or 2D rigidbodies.

---
*Ready for handoff and continued multiplayer validation, followed by network-native trading.*
