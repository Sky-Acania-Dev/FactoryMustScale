# FactoryMustScale

**FactoryMustScale** is a **Unity 2D factory-simulation tech demo** focused on **deterministic simulation, performance at scale, and disciplined architecture**.

This project is not a finished game.  
It is an **engineering exploration**: how far a **classic MonoBehaviour architecture**, reinforced with **explicit data layers and fixed-step simulation**, can be pushed **before DOTS/ECS becomes unavoidable**. 
It is also an **agent-assisted development exploration**: how far a **solo indie developer**, empowered with **agentic AI tooling**, can progress within a **limited time and minimal manual overhead**, while maintaining architectural discipline.

> If it can’t scale, it doesn’t belong.

---

## Core Principles

- **Deterministic simulation**
  - Fixed-step logic
  - Order-stable execution
  - No frame-rate dependence

- **Performance first**
  - Zero-GC hot paths
  - No per-entity GameObjects
  - No hidden allocations in simulation

- **Data-oriented design (without DOTS by default)**
  - ScriptableObject definitions
  - Baked runtime data (plain structs / arrays)
  - MonoBehaviours as adapters, not logic containers

- **Replaceable systems**
  - Everything is designed to be measured, refactored, or thrown away
  - No premature abstraction “frameworks”

---

## What This Is

- A **technical prototype**
- A **reference architecture** for scalable factory mechanics
- A **sandbox** for belts, splitters, machines, and throughput problems
- A Codex-driven development experiment (agent-assisted, human-validated)

---

## What This Is Not

- ❌ A content-complete game  
- ❌ A Unity tutorial project  
- ❌ A DOTS showcase  
- ❌ An animation-driven simulation  

DOTS / ECS / Burst may be introduced **only if profiling proves they are necessary**.

---

## Technical Stack

- **Unity 6** (PC / Steam target)
- Classic MonoBehaviour front-end
- Explicit simulation layer (pure C#)
- ScriptableObject → baked runtime data
- GitHub Actions for test automation
- MIT License

---

## Repository Structure (planned)

