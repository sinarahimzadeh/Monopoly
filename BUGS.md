# Ropoly — Known Bugs

Last reviewed: 2026-10-21

This file records confirmed problems in the current multiplayer, trade, and match-ending implementation. These items should be fixed before adding more gameplay features.

## BUG-001 — Any online player can control match-ending actions

- **Severity:** Critical
- **Status:** Open
- **Area:** Multiplayer / match results
- **Problem:** `Play Again` and `Change Players` requests are accepted without verifying that the sender is the room host or that the match is actually in the results phase.
- **What players see:** A guest can reset the match or send everyone away from the results screen. A modified client could attempt the same commands while a match is still active.
- **Expected behavior:** Only the host may start a rematch or return the room to player selection, and only after the match has ended.
- **Likely fix:** Validate the sender, room-host ownership, and current match phase on the authoritative host before applying either command.

## BUG-002 — `Change Players` does not return everyone to one shared online lobby

- **Severity:** Critical
- **Status:** Open
- **Area:** Multiplayer / lobby flow
- **Problem:** The online results screen calls the local-lobby flow. Each computer creates its own independent character-selection screen while the real online session remains in its previous started/locked state.
- **What players see:** Players appear to return to a lobby, but they are actually in separate local lobbies and cannot make shared selections or start the next online match correctly.
- **Expected behavior:** The host changes the authoritative online room back to a lobby phase. Every connected client sees the same seats, creature selections, ready states, and settings.
- **Likely fix:** Add a networked `ReturnToLobby` transition that resets shared lobby state without leaving or duplicating the online session.

## BUG-003 — A third player can become stuck when another trade is active

- **Severity:** High
- **Status:** Open
- **Area:** Trading / multiplayer UI
- **Problem:** While two players already have an active trade, another player can still open the trade screen and submit a proposal. The host rejects it, but no rejection response is sent back and the proposing client immediately opens a waiting modal.
- **What players see:** The third player remains stuck on `waiting` even though their trade was never created.
- **Expected behavior:** Only one active trade may exist at a time. Other players must either be prevented from opening/submitting a trade or receive an immediate visible rejection and return to normal play.
- **Likely fix:** Include the active-trade state in client-side validation and always return an authoritative success/failure response for a submitted proposal.

## BUG-004 — Four newly added EditMode tests are invalid and failing

- **Severity:** High
- **Status:** Open
- **Area:** Automated tests / test setup
- **Problem:** Three match-ending tests and one trade test create a session without a board, then expect board properties to exist. One test explicitly passes `board: null` even though the constructor requires a valid board.
- **What developers see:** The EditMode suite reports **109 tests: 94 passed, 15 failed**. Eleven failures were already present; four failures were introduced by the recent match-ending/trade work.
- **Expected behavior:** Tests must construct a valid test board before performing property ownership, mortgage, transfer, or bankruptcy assertions.
- **Likely fix:** Reuse or create a shared board fixture, pass it into each session, and verify that the intended property tile IDs exist before arranging ownership.

### Newly failing test groups

- `MatchEndingTests`: 3 failures
- `TradeTests`: 1 failure

## Recommended repair order

1. Fix host authorization and match-phase validation (`BUG-001`).
2. Implement the real shared online return-to-lobby transition (`BUG-002`).
3. Fix active-trade rejection and waiting-modal recovery (`BUG-003`).
4. Repair the four invalid tests and rerun the complete EditMode suite (`BUG-004`).

