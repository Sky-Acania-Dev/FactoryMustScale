# Simulation Rules — FactoryMustScale

This document defines the **authoritative rules** for simulation logic in FactoryMustScale.

These rules exist to preserve:
- determinism
- performance at scale
- architectural clarity

They are not suggestions.

---

## 1. Simulation Model

- The simulation runs on a **fixed-step tick**
- Tick duration is constant
- Simulation is decoupled from rendering

Valid approaches:
- `FixedUpdate()` for combat/hit detection
- Custom fixed-step loop (e.g., 4 tick/s) for factory logic
- All combat state changes will be applied to the next factory ticks

Invalid approaches:
- `Update()`-driven logic (besides player input)
- frame-delta accumulation
- animation-driven simulation

---

## 2. Determinism

Given identical initial state and inputs:
- the simulation must produce identical results

Rules:
- Stable iteration order (arrays, indexed loops)
- No reliance on hash order or unordered collections
- Floating-point usage must be controlled and consistent
- No time-based randomness

---

## 3. Performance Constraints

### Hot Path Rules
The following are **forbidden** inside per-tick simulation:

- Memory allocation
- LINQ
- `foreach` over allocating collections
- `new` object creation
- `GetComponent`
- UnityEngine API calls (except explicitly allowed adapters)

### Memory Strategy
- Preallocate all simulation memory (terrain layer, factory layer, etc...)
- Use pooling where growth is required
- Prefer flat arrays over object graphs

---

## 4. Data Separation

### Definitions (Authoring)
- Use `ScriptableObject` assets
- Editable in the Unity Editor
- Never mutated at runtime

### Runtime State
- Plain C# structs and arrays
- No Unity references
- Owned by the simulation layers (terrain, factory, etc...)

### Adapters
- MonoBehaviours may:
  - initialize runtime data
  - forward input
  - visualize state
- MonoBehaviours may NOT:
  - contain simulation rules
  - own authoritative state

---

## 5. Tick Order

Simulation order must be a strict two-phase pipeline:
1. **Commit / Ingest**: apply previous tick events to authoritative state (the only mutation phase)
2. **Compute / Propose**: read authoritative state and emit next tick events only (no authoritative mutation)

External input buffering/translation must feed into the Commit phase so all authoritative writes happen in one place.

Rendering reads from simulation state but does not affect it.

---

## 6. Save / Load (Future-Facing)

- All runtime state must be:
  - explicit
  - serializable
  - versionable
- No hidden state in MonoBehaviours
- No reliance on Unity serialization for simulation state

Save/load is **out of scope for now**, but the design must allow it.

---

## 7. Extension Guidelines

When adding new systems (belts, splitters, machines):
- Prefer composition over inheritance
- Avoid event-driven fan-out
- Keep logic local and inspectable
- Optimize only after profiling

---

## 8. Measuring Success

A system is acceptable if:
- it behaves correctly
- it is deterministic
- it produces zero GC allocations per tick
- it can be reasoned about without a debugger

If it fails any of these, it should be rewritten.

---

## 9. Final Rule

If performance or determinism is unclear:
**stop and measure**.

Assumptions are cheaper than bugs, but measurements are cheaper than assumptions.
