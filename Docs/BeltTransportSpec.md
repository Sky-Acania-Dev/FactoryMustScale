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

### 1.2 Applied Events vs Published Events

- **Applied events** are transport events published in tick `N-1` and consumed in `PreCompute` of tick `N`.
- **Published events** are transport events produced in `Commit` of tick `N` and queued for `PreCompute` of tick `N+1`.

Cadence rule:

- Events emitted during `Commit(N)` MUST NOT mutate authoritative transport state in tick `N`.
- Events emitted during `Commit(N)` MUST be applied exactly once in `PreCompute(N+1)`.

### 1.3 Phase Purposes

- `PreCompute` prepares deterministic read-only inputs for planning by applying prior tick events and deriving flags.
- `Compute` resolves all transfer intents into a deterministic move plan without mutating authoritative transport state.
- `Commit` publishes transfer events from the resolved move plan in deterministic order and updates allowed persistent arbitration state.

### 1.4 Determinism Requirement

Given identical initial authoritative state and identical input event stream, every tick MUST produce:

- identical resolved move plan,
- identical published events,
- identical next authoritative state.

The transport loop MUST be independent of render/update frame rate.

---

## 2) Phase Contracts

### 2.1 PreCompute Contract

#### Reads

- Authoritative per-cell state arrays from the start of tick `N`.
- Applied-event queue containing events published at `Commit(N-1)`.

#### Applies

For each applied `ItemMoveEvent`:

- Remove the item from `sourceIndex` authoritative payload slot.
- Insert the item into `targetIndex` authoritative payload slot.
- Update authoritative progress values required by this spec:
  - source progress resets to `0` after a successful transfer apply,
  - target progress remains unchanged unless explicitly defined elsewhere.

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

- MUST NOT publish new transport events.
- MUST NOT perform arbitration among competing sources.
- MUST NOT read or write transient move-plan results from previous ticks.

#### PreCompute Pseudocode

```text
for i in 0..CellCount-1:
    moveIntentSource[i] = INVALID
    moveIntentTarget[i] = INVALID
    resolvedSourceByTarget[i] = INVALID
    resolvedTargetBySource[i] = INVALID

for event in appliedEventsInStableOrder:
    assert event.type == ItemMoveEvent
    assert payloadItemId[event.sourceIndex] == event.itemId
    assert payloadCount[event.targetIndex] < capacity[event.targetIndex]

    payloadItemId[event.sourceIndex] = EMPTY
    payloadCount[event.sourceIndex] = 0
    payloadItemId[event.targetIndex] = event.itemId
    payloadCount[event.targetIndex] += 1
    progress[event.sourceIndex] = 0

for i in 0..CellCount-1:
    target = outputTargetIndex(i)
    hasPayload = (payloadCount[i] > 0)
    thresholdReady = (progress[i] >= transferThreshold)
    targetExists = (target != INVALID)
    targetCanReceive = targetExists and (payloadCount[target] < capacity[target]) and isTransportReceiver[target]

    canSend[i] = hasPayload and thresholdReady and targetExists and isTransportSender[i]
    canReceive[i] = (payloadCount[i] < capacity[i]) and isTransportReceiver[i]
    isBlocked[i] = canSend[i] and (not targetCanReceive)
```

---

### 2.2 Compute Contract

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

- MUST NOT mutate authoritative payload/progress/capacity/direction arrays.
- MUST NOT publish events.
- MUST NOT mutate applied-event queue.

Compute produces only a deterministic transfer plan for this tick.

#### Compute Pseudocode

```text
# Step 1: build one intent per source in deterministic source order
for source in 0..CellCount-1:
    if not canSend[source]:
        continue

    target = outputTargetIndex(source)
    if target == INVALID:
        continue

    if not canReceive[target]:
        continue

    moveIntentSource[source] = source
    moveIntentTarget[source] = target

# Step 2: resolve one winner per target in deterministic target order
for target in 0..CellCount-1:
    winningSource = INVALID

    # Collect candidates in deterministic source order
    # Candidate set: all source where moveIntentTarget[source] == target
    if isMerger[target]:
        winningSource = resolveMergerWinner(target, candidates, mergerPointer[target])
    else:
        winningSource = resolveDefaultWinner(candidates)

    if winningSource != INVALID:
        resolvedSourceByTarget[target] = winningSource
        resolvedTargetBySource[winningSource] = target
```

---

### 2.3 Commit Contract

#### Reads

- Resolved move plan buffers from `Compute`.

#### Emits

For every resolved move `(source, target)`, Commit MUST publish exactly one `ItemMoveEvent` containing:

- `sourceIndex = source`
- `targetIndex = target`
- `itemId = payloadItemId[source]` (authoritative item id at commit time)

#### Emission Order

Commit MUST emit events in stable deterministic order:

1. iterate `target` from `0..CellCount-1`,
2. if `resolvedSourceByTarget[target] != INVALID`, emit that event.

No other ordering is allowed.

#### Persistent Arbitration Updates

Commit MAY update persistent arbitration state only for nodes that successfully emitted a move event.

For merger nodes, the round-robin pointer MUST advance exactly once when that merger emitted a move event, and MUST NOT advance when no event was emitted.

#### MUST NOT

- MUST NOT directly move payload between cells.
- MUST NOT apply newly emitted events in the same tick.

#### Commit Pseudocode

```text
for target in 0..CellCount-1:
    source = resolvedSourceByTarget[target]
    if source == INVALID:
        continue

    event.sourceIndex = source
    event.targetIndex = target
    event.itemId = payloadItemId[source]
    publish(event)

    if isMerger[target]:
        mergerPointer[target] = nextMergerPointer(target, source)
```

---

## 3) Data Layout Definition

All arrays are fixed-size, index-addressable, and use cell index domain `[0, CellCount)`.

### 3.1 Authoritative Per-Cell State

- `payloadItemId[i]`: item id currently occupying cell `i` slot; `EMPTY` if none.
- `progress[i]`: integer transport progress for cell `i`.
- `capacity[i]`: integer slot capacity for cell `i`.
- `direction[i]`: output direction/rotation for routing from cell `i`.

### 3.2 Derived Flags (PreCompute Output)

- `canSend[i]`
- `canReceive[i]`
- `isBlocked[i]`

Derived flags are valid only for tick `N` after `PreCompute(N)` completes.

### 3.3 Transient Buffers (Cleared Every Tick)

- `moveIntentSource[source]`: source index if source generated an intent; otherwise `INVALID`.
- `moveIntentTarget[source]`: target index chosen by source intent; otherwise `INVALID`.
- `resolvedSourceByTarget[target]`: winning source for target; otherwise `INVALID`.
- `resolvedTargetBySource[source]`: resolved target for source; otherwise `INVALID`.

### 3.4 Persistent Arbitration State

- `mergerPointer[i]`: round-robin pointer for merger cell `i`.

This state persists across ticks and is updated only in `Commit` when a merger transfer is emitted.

### 3.5 Phase Validity Matrix

- `PreCompute`:
  - reads/writes authoritative state,
  - writes derived flags,
  - clears transient buffers,
  - reads applied events.
- `Compute`:
  - reads authoritative state + derived flags + persistent arbitration state,
  - writes transient buffers only.
- `Commit`:
  - reads resolved transient buffers + authoritative `payloadItemId`,
  - publishes events,
  - updates persistent arbitration state.

---

## 4) Deterministic Scan Order

1. Cell scan order is always ascending index `0..CellCount-1`.
2. Intent creation order follows ascending source index.
3. Conflict resolution is per target in ascending target index.
4. Default tie-break is lowest source index among candidates.
5. Merger tie-break uses merger round-robin pointer first; source index is the fallback tie-break when pointer does not distinguish.
6. Merger round-robin pointer advancement occurs only on successful emitted transfer for that merger.
7. Event emission order is ascending target index of resolved targets.

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
4. Advance `mergerPointer[T]` in `Commit` only when a move event for `T` is emitted.

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
4. A cell MAY both send and receive in the same tick because send effects are applied next tick and receive effects are also applied next tick.
5. Capacity rule: `payloadCount[i]` MUST remain within `[0, capacity[i]]` after every `PreCompute` apply.
6. Current single-slot operation is represented by `capacity[i] = 1`; the contract remains valid for larger fixed capacities.

---

## 7) Invariants (Testable)

1. **No duplication**: sum of live items in authoritative state plus queued published move events is conserved across ticks unless explicit non-transport delete events are applied.
2. **No loss**: every emitted `ItemMoveEvent` applied at `PreCompute(N+1)` removes item from exactly one source and inserts into exactly one target.
3. **Single winner per target per tick**: `resolvedSourceByTarget[target]` is either `INVALID` or exactly one source.
4. **Single target per source per tick**: `resolvedTargetBySource[source]` is either `INVALID` or exactly one target.
5. **Commit/apply cadence**: events emitted at `Commit(N)` are not applied before `PreCompute(N+1)` and are applied exactly once.
6. **Deterministic ordering**: event list published in `Commit` is identical for identical initial state and applied events.

---

## 8) Event Contract

### 8.1 Event Type

`ItemMoveEvent`

Fields:

- `sourceIndex : int`
- `targetIndex : int`
- `itemId : int`

### 8.2 Emit Timing

`ItemMoveEvent` is emitted only in `Commit` for each resolved `(source, target)` pair.

### 8.3 Apply Timing

`ItemMoveEvent` emitted in tick `N` MUST be applied in `PreCompute` of tick `N+1`.

### 8.4 Validation on Apply

On apply, the implementation MUST validate:

- source currently contains `itemId`,
- target has available capacity,
- source and target indices are in range.

Invalid events MUST fail deterministically according to global simulation error policy; silent drops are forbidden.

---

## 9) Non-Goals

This specification does not define:

- rendering,
- UI,
- animation,
- visual interpolation,
- camera behavior,
- audio.

Only deterministic transport state transitions and transport event publication are in scope.
