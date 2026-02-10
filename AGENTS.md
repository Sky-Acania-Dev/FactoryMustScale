# AGENTS.md — Rules for Agent-Assisted Development

This file defines **hard constraints** for any AI agent (e.g., Codex) working in this repository.

Agents are expected to **follow these rules strictly**.  
If a requirement is unclear, make a reasonable assumption and document it in the Pull Request.

---

## Project Intent

FactoryMustScale is:
- an **engineering exploration** of scalable factory simulation in Unity
- an **agent-assisted development experiment** with strict human validation

Agents are helpers, not decision-makers.

---

## General Rules

- Work on a **feature branch**.
- Make **small, reviewable commits**.
- Do not modify unrelated systems.
- Prefer **clarity and correctness over cleverness**.
- When in doubt, **ask or document assumptions** in the PR.

---

## Architecture Rules (NON-NEGOTIABLE)

### Simulation
- **Simulation does NOT run in `Update()`**
- Use:
  - `FixedUpdate()` for combat/projectile collision detection, and
  - a custom fixed-step loop (e.g., 4 ticks/s) for factory logic
- Simulation must be **deterministic**:
  - stable iteration order
  - no frame-rate dependence
  - no unordered data structures

### Performance
- **NO allocations in simulation hot paths**
- **NO LINQ**
- **NO `foreach` that allocates**
- **NO `new` inside per-tick logic**
- **NO `GetComponent` calls in simulation**

Violations will be rejected.

---

## Data-Oriented Design Rules

- Definitions:
  - Use `ScriptableObject` assets for authoring
- Runtime:
  - Bake definitions into **plain structs / arrays**
  - Simulation code must NOT reference ScriptableObjects
- MonoBehaviours:
  - Allowed only as adapters (setup, debug, visualization)
  - Must not contain simulation logic

---

## DOTS / ECS Policy

- **DO NOT introduce DOTS, ECS, or Burst**
- Only allowed if:
  - explicitly requested in the Issue
  - profiling evidence is provided
  - rationale is documented in the PR

---

## Save / Load

- Do not introduce save/load implicitly
- Do not change serialization format unless explicitly requested
- All runtime state should be designed to be serializable later

---

## Testing Expectations

- Prefer **EditMode tests** for simulation logic
- Tests must be:
  - deterministic
  - fast
  - independent of rendering
- If PlayMode tests are added, keep them minimal

---

## PR Requirements

Each PR must include:
- Summary of changes
- How to test
- Known limitations
- Next logical steps (if any)

---

## Tone & Scope

- Do not over-engineer
- Do not introduce frameworks
- Do not “future-proof” beyond the task
- This repo values **measured progress**, not abstractions

Follow these rules or do not make changes.
