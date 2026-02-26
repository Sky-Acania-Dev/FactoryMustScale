# Belt Transport Specification (PreCompute → Compute → Commit)

This document defines the authoritative, deterministic contract for item transport.

This specification is normative and implementation-ready.

---

## 1) High-Level Model

### 1.1 Tick Definition

A **factory tick** is one execution of the transport loop:

1. `PreCompute`
2. `Compute`
3. `Commit`

Transport state changes are governed only by this loop.

### 1.2 External/Input Events vs Optional Debug/Replay Records

- **External/Input Events** are non-transport inputs for the current tick and are applied in `PreCompute` of that same tick. These events mutate topology and cell state (for example build, remove, rotate, inject, remove, capacity, routing).
- **Optional Debug/Replay Records** are optional outputs emitted in `Commit`. They are observational only, are not applied back into simulation state, and are not required for correctness.

Cadence rule:

- External/Input Events for tick `N` are applied in `PreCompute(N)`.
- Transport moves resolved in tick `N` are applied immediately in `Commit(N)` via two-buffer commit.
- Debug/replay records emitted during `Commit(N)` MUST NOT drive authoritative state transitions.

### 1.3 Phase Purposes

- `PreCompute` prepares deterministic planning inputs by applying only external/input events, resetting transient buffers, and deriving flags.
- `Compute` resolves all transfer intents into a deterministic move plan without mutating authoritative transport state.
- `Commit` applies the resolved move plan immediately in the same tick using two-buffer semantics and updates allowed persistent arbitration state.

### 1.4 Determinism Requirement

Given identical initial authoritative state and identical input event stream, every tick MUST produce:

- identical resolved move plan,
- identical optional debug/replay records (if enabled),
- identical next authoritative state.

The transport loop MUST be independent of render/update frame rate.

---

## 2) Data Layout Definition

All arrays are fixed-size, index-addressable, and use cell index domain `[0, CellCount)`.

### 2.1 Authoritative Per-Cell State (Primary/Secondary Buffers)

- `payloadItemId[i]`: item id currently occupying cell `i` slot; `EMPTY` if none.
- `payloadCount[i]`: integer occupancy count for cell `i`.
- `progress[i]`: integer transport progress for cell `i`.
- `capacity[i]`: integer slot capacity for cell `i`.
- `direction[i]`: output direction/rotation for routing from cell `i`.

### 2.2 Derived Flags (PreCompute Output)

- `canSend[i]`
- `canReceive[i]`
- `isBlocked[i]`

Derived flags are valid only for tick `N` after `PreCompute(N)` completes.

### 2.3 Transient Buffers (Cleared Every Tick)

- `moveIntentSource[source]`: source index if source generated an intent; otherwise `INVALID`.
- `moveIntentTarget[source]`: target index chosen by source intent; otherwise `INVALID`.
- `resolvedSourceByTarget[target]`: winning source for target; otherwise `INVALID`.
- `resolvedTargetBySource[source]`: resolved target for source; otherwise `INVALID`.

### 2.4 Persistent Arbitration State

- `mergerPointer[i]`: round-robin pointer for merger cell `i`.

This state persists across ticks and is updated only in `Commit` when a merger successfully receives an item.

---

## 3) Phase Contracts

### 3.1 PreCompute

#### Reads

- Authoritative per-cell state at the start of tick `N`.
- External/input event stream for tick `N`.

#### Applies

PreCompute MUST deterministically apply external/input events only. Supported classes include topology and cell-state mutations (build, remove, rotate, inject, remove, capacity, routing).

PreCompute MUST NOT apply transport move events.

#### Resets

PreCompute MUST clear all transient buffers before computing derived flags:

- `moveIntentSource[i] = INVALID`
- `moveIntentTarget[i] = INVALID`
- `resolvedSourceByTarget[i] = INVALID`
- `resolvedTargetBySource[i] = INVALID`

#### Derived Flags

For each cell index `i`, PreCompute MUST recompute:

- `canSend[i]`
- `canReceive[i]`
- `isBlocked[i]`

Definitions:

- `canSend[i] = true` iff cell `i` has payload, has a valid output connection, and progress meets transfer threshold.
- `canReceive[i] = true` iff cell `i` has available capacity and is a valid transport receiver.
- `isBlocked[i] = true` iff `canSend[i] = true` and the current output target is absent or `canReceive[target] = false`.

#### MUST NOT

- MUST NOT perform arbitration among competing sources.
- MUST NOT mutate persistent arbitration state.
- MUST NOT read or write transient move-plan results from previous ticks.

### 3.2 Compute

#### Reads

- Authoritative per-cell state produced by `PreCompute`.
- Derived flags produced by `PreCompute`.
- Persistent arbitration state (for example merger round-robin pointer).

#### Writes

- Transient intent buffers:
  - `moveIntentSource[source]`
  - `moveIntentTarget[source]`
- Transient resolved plan buffers:
  - `resolvedSourceByTarget[target]`
  - `resolvedTargetBySource[source]`

#### MUST NOT

- MUST NOT mutate authoritative payload/count/progress/capacity/direction arrays.
- MUST NOT mutate external/input event queues.
- MUST NOT apply resolved moves.

Compute is plan-only: it generates intents and resolves arbitration into the deterministic move plan for this tick.

### 3.3 Commit (Two-Buffer Immediate Apply)

- Commit MUST read primary buffers and write ONLY to secondary buffers.
- Commit MUST apply resolved moves in deterministic TARGET INDEX order.
- Commit MUST swap buffers exactly once at the end of the phase.

#### Step 1: Initialize Secondary Buffers

- Copy primary payload/progress into secondary.
- Copy any other authoritative per-cell fields required to preserve unchanged state this tick.

#### Step 2: Apply Resolved Moves (Target Order)

- For each target in `0..CellCount-1`:
  - `source = resolvedSourceByTarget[target]`
  - if `source` invalid, continue
  - `itemId` is read from primary `payloadItemId[source]`
  - write removals/insertions ONLY to secondary:
    - source: clear payload + reset progress
    - target: increment `payloadCount` and set `payloadItemId` if needed

Capacity MUST NOT be exceeded. `Compute` MUST prevent resolving into full targets.

#### Step 3: Update Persistent Arbitration State

- Merger round-robin pointer advances ONLY if that merger successfully received an item this tick.
- Pointer advances exactly once per successful transfer.

#### Step 4: Swap Buffers

- Atomic swap of primary and secondary buffers exactly once.

#### Commit MUST NOT

- Mutate primary buffers during move application.
- Apply moves in source order.
- Swap more than once.
- Partially apply moves.

---

## 4) Deterministic Scan Order

1. Cell scan order is always ascending index `0..CellCount-1`.
2. Intent creation order follows ascending source index.
3. Conflict resolution is per target in ascending target index.
4. Default tie-break is lowest source index among candidates.
5. Merger tie-break uses merger round-robin pointer first; source index is the fallback tie-break when pointer does not distinguish.
6. Merger round-robin pointer advancement occurs only on successful transfer for that merger.
7. Optional debug/replay record emission order is ascending target index of resolved targets.

All ordering rules are mandatory.

---

## 5) Arbitration Rules

### 5.1 Single Source → Single Target

If source `S` has a valid intent to target `T` and no other source targets `T`, then `S` is resolved for `T`.

### 5.2 Multiple Sources → One Target (Non-Merger)

If sources `{S1..Sk}` all target `T` and `T` is not a merger, winner is source with minimum index.

All losing sources remain unresolved for the tick.

### 5.3 Merger Node

If `T` is merger:

1. Determine candidate list in deterministic incoming-port order anchored by `mergerPointer[T]`.
2. Select first candidate in that rotated order.
3. If no candidate exists, no winner.
4. Advance `mergerPointer[T]` in `Commit` only when `T` successfully received an item.

### 5.4 Blocked Target

If target is invalid, absent, or cannot receive, source intent is not created.

### 5.5 Insufficient Progress

If `progress[source] < transferThreshold`, source intent is not created.

### 5.6 Multiple Outputs

If a source supports multiple possible outputs, routing MUST use one deterministic output-selection function returning exactly one target for the tick before arbitration.

The selected target MUST be stable for identical input state.

---

## 6) Throughput Model

1. A transfer requires `progress[source] >= transferThreshold` at `PreCompute` time.
2. A source may send at most one item per tick.
3. A target may receive at most one item per tick.
4. A cell MAY both send and receive in the same tick because two-buffer commit semantics allow send+receive within one tick without read/write hazards.
5. Capacity rule: `payloadCount[i]` MUST remain within `[0, capacity[i]]` after every `Commit` swap.
6. Current single-slot operation is represented by `capacity[i] = 1`; the contract remains valid for larger fixed capacities.

---

## 7) Invariants (Testable)

1. **No duplication**: total live items in authoritative state before `Commit(N)` equals total live items after `Commit(N)` unless explicit non-transport create/delete external events were applied in `PreCompute(N)`.
2. **No loss**: each resolved move in tick `N` removes one item from exactly one source cell and inserts one item into exactly one target cell during `Commit(N)`.
3. **Single winner per target per tick**: `resolvedSourceByTarget[target]` is either `INVALID` or exactly one source.
4. **Single target per source per tick**: `resolvedTargetBySource[source]` is either `INVALID` or exactly one target.
5. **Immediate apply semantics**: resolved moves are applied in `Commit(N)` and visible in authoritative state immediately after the single buffer swap of `Commit(N)`.
6. **Deterministic ordering**: resolved target-order application (and optional records, if enabled) is identical for identical initial state and input events.

---

## 8) Optional Debug/Replay Records

### 8.1 Record Type

Optional `ItemMoveRecord`

Fields:

- `sourceIndex : int`
- `targetIndex : int`
- `itemId : int`

### 8.2 Emit Timing

If enabled, records are emitted only in `Commit` for each resolved `(source, target)` pair.

### 8.3 Emit Order

Records are emitted in deterministic ascending target index order.

### 8.4 Correctness Role

Records reflect resolved moves only.

Records are not applied, do not feed back into transport state, and are not required for simulation correctness.

---

## 9) Non-Goals

This specification does not define:

- rendering,
- UI,
- animation,
- visual interpolation,
- camera behavior,
- audio.

Only deterministic transport state transitions are in scope.
