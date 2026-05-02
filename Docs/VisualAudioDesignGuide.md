# Visual/Audio Presentation Architecture Plan

## Context

The game is moving from pure UI-based combat presentation toward a hybrid setup:

- Combat units and battlefield will use 3D GameObjects.
- HUD, dice UI, action preview, combat logs, and tooltips remain Unity UI.
- Camera style: 45-degree axonometric/isometric-like view with low FOV perspective.
- Camera should allow limited orbiting later.
- Visual/audio presentation should be decoupled from gameplay logic.

## Core Principle

Gameplay logic should not directly spawn particles, play sounds, or trigger animations.

Instead:

Combat Logic
→ Combat Result / Combat Visual Event
→ Battle Presentation Manager
→ VFX / SFX / Animation / Floating Text / UI Feedback

## Constraints

- Documentation only.
- Do not change gameplay code.
- Do not create scripts yet.
- Do not create prefabs/assets.
- Keep the document practical and implementation-oriented.

## Sections

### 1. Presentation Goals

Include:
- Improve combat readability.
- Make actor, target, action impact, tags, DoTs, cleanse, death, and future Break events visually clear.
- Support 3D units, VFX, SFX, and simple animations.
- Keep gameplay logic deterministic and presentation-independent.
- Avoid over-polishing early.

### 2. Rendering Setup

Document:
- 3D battlefield rendered by main camera.
- 45-degree axonometric/isometric-like camera.
- Low FOV perspective camera.
- Limited orbiting allowed later.
- Screen-space UI Canvas for dice/action panel, bottom preview, combat log, and tooltips.
- Optional world-space UI for HP/AP/tag indicators, floating text, and status icons.

### 3. Responsibility Split

Gameplay layer:
- Calculates action results.
- Applies damage/heal/shield.
- Applies/removes UnitTags.
- Produces combat result summaries / visual events.
- Does not play visuals/audio directly.

Presentation layer:
- Receives combat result / visual events.
- Looks up feedback profiles.
- Plays animations.
- Spawns VFX.
- Plays SFX.
- Shows floating text.
- Updates visual highlights.
- Sequences delays if needed.

### 4. Proposed Main Components

Describe:

#### BattlePresentationManager
- Central coordinator for visual/audio feedback.
- Receives visual events from combat flow.
- Sequences action start → impact → result → cleanup.
- Resolves actor/target visual controllers through UnitVisualRegistry.
- Queries feedback databases.
- Calls VFX/SFX/animation handlers.
- Keeps presentation timing separate from gameplay resolution.

#### UnitVisualRegistry
- Maps `UnitRuntimeId` to `UnitVisualController`.
- Registers unit visuals when spawned.
- Unregisters unit visuals on death/despawn.
- Provides lookup for presentation events.

#### UnitVisualController
Attached to each 3D unit prefab.

Responsibilities:
- Stores visual anchor points:
  - CastPoint
  - HitPoint
  - OverheadPoint
  - GroundPoint
- Plays animation triggers.
- Sets highlight state.
- Plays hit flash / heal pulse / shield pulse.
- Optionally manages unit-local status VFX.

#### UnitPrefabDatabase
Maps unit identity/class/subclass to visual prefab.

Possible keys:
- UnitType
- Subclass
- UnitSetup id/name
- fallback prefab

#### ActionFeedbackDatabase
Maps BattleAction or action id to feedback profile.

#### ActionFeedbackProfile
Suggested fields:
- action reference or action id
- cast animation trigger
- impact animation trigger
- cast VFX prefab
- projectile VFX prefab
- hit VFX prefab
- cast SFX
- hit SFX
- impact delay
- camera shake strength
- floating text style override

#### TagFeedbackDatabase
Maps ActionTag / UnitTagType to fallback feedback.

Examples:
- Poisoned → green tick popup / poison puff.
- Burning → flame tick / orange-red flash.
- Bleeding → red slash pulse.
- Cleanse → white/blue sparkle.
- Purify → stronger cleanse effect.

#### VfxHandler
- Spawns VFX at world anchors.
- Supports attach-to-unit vs one-shot world position.
- Later can add pooling.

#### SfxHandler
- Plays one-shot clips.
- Routes through mixer group if available.
- Can support volume/pitch variation later.

#### FloatingTextHandler
- Shows damage/heal/shield/tag text.
- Uses world-space or screen-space anchored text.
- Different styles for damage, heal, shield, DoT, cleanse, death.

### 5. Visual Event Model

Document proposed event types:
- ActionStarted
- ProjectileLaunched
- ActionImpact
- DamageApplied
- HealApplied
- ShieldApplied
- UnitTagApplied
- UnitTagRemoved
- UnitTagTickDamage
- CleanseApplied
- PurifyApplied
- UnitDied
- ActionEnded
- Future: BreakDamageApplied
- Future: UnitBroken
- Future: ActionProgressPushedBack

Example event fields:
- Type
- ActorUnitId
- TargetUnitId
- ActionId
- Amount
- UnitTagType
- Reason

Mention:
- This is a documentation proposal only.
- Exact implementation can evolve.

### 6. Highlight States

Document current/desired highlight hierarchy:

- Normal
- CanActNext: white/light-gray static border.
- SelectedActor: blue/cyan sine-wave glow.
- SelectedTarget: orange/gold sine-wave glow.
- Invalid/unavailable: dim/no highlight.

Priority:

SelectedTarget > SelectedActor > CanActNext > Normal

Mention:
- Actor and target should both be visible if different units.
- If self-targeting, SelectedTarget can override actor highlight for now unless dual-border is easy.

### 7. First-Pass Placeholder Effects

Document lightweight initial effects:

- Attack:
  - actor small forward punch / weapon swing
  - target hit flash
  - hit SFX
- Heal:
  - green/white pulse on target
  - heal floating text
- Shield:
  - blue pulse/barrier flash
- Poison:
  - green puff or green floating tick
- Fire/Burning:
  - orange/red flash or small flame burst
- Bleed:
  - red slash/pulse
- Cleanse:
  - white/blue sparkle, removed stacks floating text
- Death:
  - fade, fall, or shrink placeholder

State clearly:
- Use placeholder assets first.
- Avoid full polish until gameplay feel is validated.

### 8. Integration with Existing Combat Flow

Document intended flow:

1. Combat resolver calculates results.
2. Resolver emits structured result summary / visual events.
3. BattlePresentationManager plays feedback.
4. Gameplay state remains authoritative.
5. Presentation may delay UI progression if desired, but must not change results.

Mention:
- Current debug result text can continue to exist.
- Visual events should complement, not replace, result summaries.

### 9. Future Break Presentation Requirements

Break will need strong visual clarity:
- Break damage cue.
- Shield shatter if shield is lost.
- Broken state VFX/status icon.
- AP/action-progress pushback feedback.
- Channel interruption feedback.

Do not implement Break now; only document needs.

### 10. Risks / Design Warnings

Include:
- Do not let presentation logic mutate gameplay state.
- Do not block gameplay resolution on fragile animation events.
- Avoid direct references from combat resolver to particle/audio prefabs.
- Avoid building too much polish before core combat is balanced.
- Keep camera orbit limited to preserve board readability.
- Ensure VFX does not obscure tags/AP/target highlights.

### 11. Recommended Implementation Phases

Phase 1:
- Create documentation and architecture plan.
- Keep current UI working.

Phase 2:
- Add placeholder 3D unit prefabs and UnitVisualController.
- Add UnitVisualRegistry.
- Keep gameplay unchanged.

Phase 3:
- Add BattlePresentationManager.
- Add basic ActionStarted / Impact / FloatingText events.

Phase 4:
- Add ActionFeedbackDatabase and basic profiles.
- Add simple VFX/SFX handlers.

Phase 5:
- Add status/tag feedback.
- Add better animations and polish.

Phase 6:
- Implement Break system presentation support after basic VFX pipeline exists.