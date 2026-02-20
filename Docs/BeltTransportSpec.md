# Belt Transport Specification (Deterministic Contract)

This document defines the authoritative behavior of factory belt transport in FactoryMustScale.

This spec is normative. Any refactor must preserve these semantics unless explicitly revised.

---

# 1. Simulation Cadence

- Unit tick: 32 Hz
- Factory tick: every 4 unit ticks
- Belt transport logic runs **only on factory ticks**.

Let `t` be the current unit tick.
Belt logic executes if and only if:

```
t % 4 == 0
```

---

# 2. Authoritative Model

Each factory cell has:

- A **payload queue** with fixed capacity `C` 
- Transport progress (integer)
- Cell type (belt, merger, miner, crafter, storage, etc.)
- Direction / output configuration

Current implementation:

```
C = 1
```

The system is designed to support larger capacities (e.g., 3 or 4) without changing external semantics.

Belts never drop items.
If output is blocked, the cell stalls (clogs).

---

# 3. Payload Queue Model

Each cell owns a FIFO queue with:

- Fixed capacity `C`
- Deterministic order
- No dynamic resizing
- No allocations during steady-state

Required operations:

- `IsEmpty`
- `IsFull`
- `Count`
- `PeekFront`
- `TryEnqueue`
- `TryDequeue`
- `Clear`

For current capacity `C = 1`, the queue behaves as a single-slot container.

---

# 4. Factory Tick Phases

On each factory tick, belt transport proceeds in three logical phases:

---

## Phase A – Apply Scheduled Transfers

Apply transport transfers scheduled during the previous factory tick.

- All winning moves from tick `t-4` are applied now.
- Payload ownership is transferred atomically:
  - Source: `Dequeue`
  - Target: `Enqueue`
- Progress for cells that successfully transferred payload is reset.

No new arbitration occurs in this phase.

Only this phase mutates payload ownership.

---

## Phase B – Compute Move Intents

For each factory cell (in deterministic index order):

If:

- `Payload.IsEmpty == false`
- `TransportProgress >= threshold`
- Cell type allows output

Then:

- Determine target output cell.
- If target exists and `target.Payload.IsFull == false`
  - Emit move intent: `(sourceIndex, targetIndex)`

No payload mutation occurs in this phase.
Transport progress is not modified in this phase.

---

## Phase C – Arbitration & Scheduling

For each target cell:

- Collect all move intents targeting that cell.
- Select winner using deterministic rule (see Section 5).
- Schedule the winning move to be applied in next factory tick (Phase A of next cycle).

Losers remain unchanged.

No immediate mutation occurs in this phase.

---

# 5. Arbitration Rules

## 5.1 Default Belts

If multiple sources push into the same target:

- Lower source cell index wins.

Iteration order must be deterministic and stable.

---

## 5.2 Merger Cells

Mergers use round-robin arbitration:

- Each merger maintains an internal cursor.
- Among valid incoming intents, the next in round-robin order wins.
- Cursor advances only when a transfer succeeds.

Round-robin state must be part of authoritative state.

---

# 6. Transport Progress Rules

Each belt cell has an integer `TransportProgress`.

- Progress increments by fixed amount per factory tick.
- When `Progress >= threshold` AND transfer succeeds:
  - Progress resets to 0.
- When `Progress >= threshold` BUT transfer fails (blocked):
  - Progress remains >= threshold.
- When payload queue is empty:
  - Progress remains 0.

No floating-point progress allowed.

---

# 7. Blocking Rules

A move from source to target is valid only if:

- Target cell exists.
- Target cell can accept payload:
  - `target.Payload.IsFull == false`
- No other winning move has been scheduled for target in this tick.

If target is occupied or scheduled by a higher-priority move:

- Source remains unchanged.
- Progress remains unchanged.
- No payload is dropped.

---

# 8. Determinism Requirements

All belt transport behavior must be:

- Independent of frame rate.
- Independent of iteration order outside defined cell index ordering.
- Free of runtime allocations during steady-state.
- Free of floating-point drift.
- Fully reproducible given identical initial state and command stream.

All loops must iterate in deterministic index order.

All arbitration decisions must use stable tie-break rules.

---

# 9. Invariants

At any time:

- A payload item exists in exactly one cell queue.
- Scheduled transfers do not mutate state until next factory tick.
- Only Phase A mutates payload ownership.
- Compute and Arbitration phases are read-only with respect to payload state.
- Queue ordering is preserved (FIFO).

---

# 10. Non-Goals (For Now)

- No partial transfers.
- No variable queue capacity at runtime.
- No physics-based movement.
- No time-sliced or asynchronous arbitration.

---

End of specification.
