# Simulation Structure & Namespace Contract

This document defines the canonical folder structure, namespaces, and responsibilities for the deterministic simulation layer of **FactoryMustScale**.

The simulation follows a strict **3-phase loop**:

1. **ExternalIngest** — read external commands/intents. No authoritative state mutation (except explicitly allowed fast writes).
2. **Compute** — read-only logic. Produce intents/deltas.
3. **Commit** — apply deltas to authoritative state.

Only **Commit** may mutate authoritative simulation state.

---

# Root Namespace

```
FactoryMustScale.Simulation
```

All deterministic gameplay logic lives under this namespace.

---

# Folder & Namespace Structure

```
Assets/Scripts/FactoryMustScale/Simulation/
```

---

## 1. Core

**Folder**
```
Simulation/Core/
```

**Namespace**
```
FactoryMustScale.Simulation.Core
```

**Purpose:** Owns the global tick clock and 3-phase execution contract.

### Files

- `SimClock.cs`  
  Maintains authoritative tick counters (UnitTick = 32 Hz; FactoryTick = every 4; EnvTick = every 32).

- `SimLoop.cs`  
  Executes ExternalIngest → Compute → Commit in deterministic order.

- `ISimSystem.cs`  
  Defines the 3-phase system interface contract.

- `SimContext.cs`  
  Provides shared access to world state and buffers during a tick.

- `SimHash.cs`  
  Computes deterministic checksum of authoritative state for desync detection.

---

## 2. Common

**Folder**
```
Simulation/Common/
```

**Namespace**
```
FactoryMustScale.Simulation.Common
```

**Purpose:** Shared primitives used across domains.

---

### 2.1 Grid

**Folder**
```
Simulation/Common/Grid/
```

**Namespace**
```
FactoryMustScale.Simulation.Common.Grid
```

- `GridIndex.cs`  
  Utility for converting XY ↔ index.

- `GridBounds.cs`  
  Grid dimension and boundary utilities.

- `LayerBase.cs`  
  Base grid storage container for domain layers.

---

### 2.2 Buffers

**Folder**
```
Simulation/Common/Buffers/
```

**Namespace**
```
FactoryMustScale.Simulation.Common.Buffers
```

- `EventBuffer.cs`  
  Deterministic double-buffered event storage.

- `IntentBuffer.cs`  
  Per-tick transient intent storage.

- `DeltaBuffer.cs`  
  Commit-phase mutation container.

---

### 2.3 Items

**Folder**
```
Simulation/Common/Items/
```

**Namespace**
```
FactoryMustScale.Simulation.Common.Items
```

- `FactoryItem.cs`  
  Lightweight immutable definition of factory-type items.

- `ItemStack.cs`  
  Item + quantity struct used in transport/storage.

---

# Domains

Each domain follows the same structure:

```
Domains/<DomainName>/
  State/
  Intents/
  Deltas/
  Systems/
```

Each domain:
- Owns its authoritative state.
- Emits intents in Compute.
- Applies deltas in Commit.
- Does not mutate other domains directly.

---

# 3. Terrain Domain

**Folder**
```
Simulation/Domains/Terrain/
```

**Namespace**
```
FactoryMustScale.Simulation.Domains.Terrain
```

**Purpose:** Manages terrain tiles, environment vectors, and dropped items.

---

## State

```
Terrain/State/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Terrain.State
```

- `TerrainTileState.cs`  
  Tile type, ore presence, static properties.

- `EnvironmentState.cs`  
  Per-tile environment vector (temperature, wind, lighting, charge, etc.).

- `TerrainDropGrid.cs`  
  Grid-bound dropped item storage with deterministic spill rules.

---

## Intents

```
Terrain/Intents/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Terrain.Intents
```

- `DropIntent.cs`  
  External intent for item drops.

- `ModifyTileIntent.cs`  
  Terrain modification requests.

---

## Deltas

```
Terrain/Deltas/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Terrain.Deltas
```

- `TerrainDelta.cs`  
  Mutation payload applied in Commit.

---

## Systems

```
Terrain/Systems/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Terrain.Systems
```

### Fast

- `TerrainExternalIngestSystem.cs`  
  Applies fast external terrain writes (e.g., item drops).

### Slow

- `EnvironmentComputeSystem.cs`  
  Computes environment simulation (1 Hz cadence).

- `EnvironmentCommitSystem.cs`  
  Applies environment deltas.

---

## Generation

```
Terrain/Generation/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Terrain.Generation
```

- `TerrainGenerator.cs`  
  Deterministic terrain/ore generation.

---

# 4. Factory Domain

**Folder**
```
Simulation/Domains/Factory/
```

**Namespace**
```
FactoryMustScale.Simulation.Domains.Factory
```

**Purpose:** Handles buildings, belts, transport, and production logic.

---

## State

```
Factory/State/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Factory.State
```

- `FactoryCellState.cs`  
  Per-cell building type and metadata.

- `TransportState.cs`  
  Belt payload storage and progress values.

---

## Intents

```
Factory/Intents/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Factory.Intents
```

- `BuildIntent.cs`  
  Building placement request.

- `RotateIntent.cs`  
  Building rotation request.

- `TransportMoveIntent.cs`  
  Proposed belt payload movement.

---

## Deltas

```
Factory/Deltas/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Factory.Deltas
```

- `FactoryDelta.cs`  
  Mutation payload applied in Commit.

---

## Systems

```
Factory/Systems/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Factory.Systems
```

### Transport

- `TransportComputeSystem.cs`  
  Determines movement arbitration for belts.

- `TransportCommitSystem.cs`  
  Applies winning belt moves.

### Build

- `FactoryBuildSystem.cs`  
  Applies building placement/removal logic.

---

# 5. Units Domain (Future / In Progress)

**Folder**
```
Simulation/Domains/Units/
```

**Namespace**
```
FactoryMustScale.Simulation.Domains.Units
```

**Purpose:** Handles deterministic unit movement and combat.

---

## State

```
Units/State/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Units.State
```

- `UnitState.cs`  
  Authoritative unit data arrays.

- `UnitOccupancyGrid.cs`  
  Ground/air occupancy layers with 4/1/2×2 footprint rules.

---

## Intents

```
Units/Intents/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Units.Intents
```

- `MoveIntent.cs`  
  Proposed 1-cell movement.

- `AttackIntent.cs`  
  Proposed attack action.

---

## Deltas

```
Units/Deltas/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Units.Deltas
```

- `UnitDelta.cs`  
  HP changes, state transitions, movement commit.

---

## Systems

```
Units/Systems/
```

Namespace:
```
FactoryMustScale.Simulation.Domains.Units.Systems
```

- `UnitsComputeSystem.cs`  
  Emits Move/Attack intents.

- `UnitsCommitSystem.cs`  
  Resolves reservations and applies deltas.

---

# Simulation Cadence

- Unit tick: **32 Hz**
- Factory tick: every **4 unit ticks**
- Environment tick: every **32 unit ticks**

All ticks derive from `SimClock.UnitTick`.

---

# Invariants

1. Compute must not mutate authoritative state.
2. Only Commit mutates authoritative state.
3. All iteration order must be deterministic.
4. No runtime allocations during steady-state simulation.
5. All randomness must use deterministic seeded RNG.

---

# Legacy Code Policy

Any obsolete systems must be moved to:

```
Simulation/Legacy/
```

Namespace:
```
FactoryMustScale.Simulation.Legacy
```

Legacy systems must not be referenced by new code.
