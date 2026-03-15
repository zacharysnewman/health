# Quantum 3 Health System Port — Plan

## Phase 1 — Discovery Inventory

### Source Files Read

| File | Type |
|------|------|
| `Runtime/Health.cs` | MonoBehaviour — core runtime |
| `Runtime/HealthData.cs` | ScriptableObject — data container |
| `Runtime/HealthTraits.cs` | Serializable class — config fields |
| `Runtime/HealthEvents.cs` | Serializable class — UnityEvent wrappers |
| `Runtime/HealthUtils.cs` | Static utility — damage split calculation |
| `Runtime/RegenTrigger.cs` | Enum — regen ordering |

Networking adapters (Mirror, Fusion, NGO) and sample files were read for
context only; they are not ported.

---

### 1. State Fields and Their Types

From `Health.cs` (runtime state, not config):

| Field | Original Type | Notes |
|-------|--------------|-------|
| `currentHealth` | `float` | Private backing field |
| `currentShield` | `float` | Private backing field |
| `isDead` | `bool` | |
| `MaxHealthBonus` | `float` | Public additive bonus on top of config MaxHealth |

Derived/computed (not stored separately, must be re-derived in Quantum):

| Expression | Notes |
|-----------|-------|
| `MaxHealth = healthData.Traits.MaxHealth + MaxHealthBonus` | Stored on component as computed field |

---

### 2. Config Fields (HealthTraits → lives in config asset)

| Field | Type | Default |
|-------|------|---------|
| `MaxHealth` | `float` | `100` |
| `MaxShield` | `float` | `100` |
| `RegenTrigger` | `RegenTrigger` (enum) | `HealthThenShield` |
| `HealthRegenRate` | `float` | `10` |
| `ShieldRegenRate` | `float` | `20` |
| `HealthRegenDelay` | `float` | `1` |
| `ShieldRegenDelay` | `float` | `2` |
| `ShieldBleedThrough` | `bool` | `true` |
| `HasShields` | `bool` | `true` |
| `ShouldHealthRegen` | `bool` | `true` |
| `ShouldShieldRegen` | `bool` | `true` |

---

### 3. Methods and Signatures

| Method | Signature | Notes |
|--------|-----------|-------|
| `InitializeValues` | `void ()` | Sets health/shield to max, isDead=false |
| `Damage` | `void (float amount)` | Throws on negative; fires OnDie or OnDamage; calls StopRegen / StartRegen |
| `HealHealth` | `void (float amount)` | Throws on negative; no-op if dead; fires events |
| `ChargeShield` | `void (float amount)` | Throws on negative; no-op if dead or no shields; fires events |
| `Revive()` | `void ()` | Full health, zero shield |
| `Revive(float healthPct, float shieldPct)` | `void (float, float)` | Percent-based |
| `Revive(int health, int shield)` | `void (int, int)` | Absolute amounts |
| `StartRegen` | `void (bool withDelay)` | Restarts all coroutines |
| `StopRegen` | `void ()` | Stops all coroutines |
| `CalculateDamageSplit` (static) | `(float shield, float health) (float dmg, float shield, float health, bool bleedThrough)` | Pure math util |

Revive overloads all funnel into `ReviveInternal(float health, float shield)`.

---

### 4. Events / Callbacks Fired

All are `UnityEvent` or `UnityEvent<float>` from `HealthEvents`:

| Event | Payload | When Fired |
|-------|---------|-----------|
| `OnHealthChangeEvent` | `float` (absolute) | Any time `CurrentHealth` setter is called |
| `OnHealthChangeNormalizedEvent` | `float` (0–1) | Same as above |
| `OnShieldChangeEvent` | `float` (absolute) | Any time `CurrentShield` setter is called |
| `OnShieldChangeNormalizedEvent` | `float` (0–1) | Same as above |
| `OnDamageEvent` | `float` (amount dealt) | After damage, if not dead |
| `OnDieEvent` | `float` (killing blow amount) | When health drops to ≤ 0 |
| `OnOverkillEvent` | `float` (amount) | Damage received while already dead |
| `OnHealHealthEvent` | `float` (amount healed) | After HealHealth |
| `OnMaxHealthEvent` | *(none)* | When health reaches MaxHealth (heal or regen finish) |
| `OnOverhealEvent` | `float` (overage) | When heal would exceed max |
| `OnChargeShieldEvent` | `float` (amount charged) | After ChargeShield |
| `OnMaxShieldEvent` | *(none)* | When shield reaches MaxShield (charge or regen finish) |
| `OnOverchargeShieldEvent` | `float` (overage) | When charge would exceed max |
| `OnReviveEvent` | *(none)* | After successful revive |
| `OnRegenHealthStartEvent` | *(none)* | When health regen loop begins |
| `OnRegenShieldStartEvent` | *(none)* | When shield regen loop begins |

---

### 5. Regen Logic Detail

The regen runs as coroutines. Quantum replaces coroutines with per-tick
accumulator logic using timer fields on the component.

Ordering (controlled by `RegenTrigger` enum):

| Mode | Behaviour |
|------|-----------|
| `HealthThenShield` | Health regen runs first; shield regen starts only when health is full |
| `ShieldThenHealth` | Shield regen runs first; health regen starts only when shield is full |
| `HealthAndShield` | Both run simultaneously |

Each regen phase:
1. Optionally waits a delay (`HealthRegenDelay` / `ShieldRegenDelay`)
2. Fires `OnRegenHealthStartEvent` / `OnRegenShieldStartEvent`
3. Ticks the value up at the configured rate per second until capped
4. Fires `OnMaxHealthEvent` / `OnMaxShieldEvent` when done

Regen is **stopped** by any `Damage()` call and **restarted** (with delay) after
non-lethal damage. Regen starts **without delay** on `Revive`.

---

### 6. External Dependencies (Inbound / Outbound)

**Inbound** (things that call into Health):
- Any script calling `Damage()`, `HealHealth()`, `ChargeShield()`, `Revive()`
- No interfaces are defined; callers hold direct component references

**Outbound** (things Health calls into):
- `HealthUtils.CalculateDamageSplit()` — pure static math, no external dep
- `HealthData` / `HealthTraits` — data-only, no side effects
- All events — consumers are UI, VFX, audio (not ported, view-layer only)

No interfaces (`IDamageable` etc.) are defined in this codebase. Health is
accessed directly.

---

### 7. ScriptableObject Data Asset

`HealthData` (ScriptableObject) wraps one `HealthTraits` instance.
`HealthTraits` is a plain serializable class — all fields listed in §2 above.

---

## Phase 2 — DSL + Structure Mapping

### Feature Name

**`Health`**

Paths:
- Simulation: `Assets/QuantumUser/Simulation/Health/`
- View: `Assets/QuantumUser/View/Health/`
- Editor: `Assets/QuantumUser/Editor/Health/`
- Sample: `Assets/QuantumUser/Health/Sample/`

---

### State Field Mapping → QTN Component

| Original | Quantum Type | QTN Field |
|----------|-------------|-----------|
| `currentHealth` (float) | `FP` | `CurrentHealth` |
| `currentShield` (float) | `FP` | `CurrentShield` |
| `isDead` (bool) | `bool` | `IsDead` |
| `MaxHealthBonus` (float) | `FP` | `MaxHealthBonus` |
| regen delay timer (health) | `FP` | `HealthRegenDelayTimer` |
| regen delay timer (shield) | `FP` | `ShieldRegenDelayTimer` |
| regen active flag (health) | `bool` | `IsHealthRegenerating` |
| regen active flag (shield) | `bool` | `IsShieldRegenerating` |
| regen delay armed (health) | `bool` | `HealthRegenDelayActive` |
| regen delay armed (shield) | `bool` | `ShieldRegenDelayActive` |
| Config reference | `asset_ref<HealthConfig>` | `Config` |

Note: The coroutine state machine is replaced by timer fields + flags.
`HealthRegenDelayActive` = delay counting down; `IsHealthRegenerating` = actively
ticking health up. Same pattern for shield.

---

### Enum Mapping

| Original | QTN |
|----------|-----|
| `RegenTrigger` (3 values) | `enum RegenTrigger : Byte { HealthThenShield = 0, ShieldThenHealth = 1, HealthAndShield = 2 }` |

3 values fits in Byte.

---

### ScriptableObject → AssetObject

| Original | Quantum |
|----------|---------|
| `HealthData` (ScriptableObject wrapping `HealthTraits`) | `HealthConfig` (AssetObject, all traits flattened in) |

`HealthData` and `HealthTraits` are merged into one `HealthConfig` AssetObject
since `HealthTraits` is only a serialization helper with no separate identity.
All defaults from `HealthTraits()` are preserved as FP literals.

QTN declaration: `asset HealthConfig;`

---

### Method → Signal / System Method Mapping

Callers in other simulation systems need to trigger damage, heal, shield charge,
and revive. These become **signals** so any system can call them without a
direct dependency on `HealthSystem`.

| Original Method | Quantum Signal |
|----------------|---------------|
| `Damage(float amount)` | `signal OnHealthDamage(entity_ref entity, FP amount);` |
| `HealHealth(float amount)` | `signal OnHealthHeal(entity_ref entity, FP amount);` |
| `ChargeShield(float amount)` | `signal OnHealthChargeShield(entity_ref entity, FP amount);` |
| `Revive()` | `signal OnHealthRevive(entity_ref entity, FP healthPercent, FP shieldPercent);` |

Passing `healthPercent = FP._1, shieldPercent = FP._0` from outside is the
full-revive equivalent. The original's three Revive overloads are unified into
one signal with percent params; the system applies them to MaxHealth/MaxShield.

`InitializeValues` → handled in `HealthInitSystem` via
`ISignalOnComponentAdded<HealthComponent>`.

`CalculateDamageSplit` → static helper method inside `HealthSystem.cs` (pure FP
math, no Quantum frame reference needed).

---

### Event Mapping → Quantum Events / Signals

Events that other **simulation systems** need to react to → **signals**:

| Original Event | Quantum |
|---------------|---------|
| *(none)* — no simulation system reacts to health events in this codebase | — |

All original events are purely view-layer (UI, VFX, audio). Therefore they map
to Quantum **events** (not signals), consumed by the View script.

| Original UnityEvent | Quantum Event | Type | Payload |
|--------------------|---------------|------|---------|
| `OnHealthChangeEvent` | `event HealthChanged` | plain | `entity_ref Entity; FP CurrentHealth; FP MaxHealth` |
| `OnHealthChangeNormalizedEvent` | *(carried in HealthChanged payload — see HealthUIEventEmitter)* | — | — |
| `OnShieldChangeEvent` | `event ShieldChanged` | plain | `entity_ref Entity; FP CurrentShield; FP MaxShield` |
| `OnShieldChangeNormalizedEvent` | *(carried in ShieldChanged payload — see HealthUIEventEmitter)* | — | — |
| `OnDamageEvent` | `event HealthDamaged` | plain | `entity_ref Entity; FP Amount` |
| `OnDieEvent` | `synced event HealthDied` | synced | `entity_ref Entity; FP KillingBlow` |
| `OnOverkillEvent` | `event HealthOverkill` | plain | `entity_ref Entity; FP Amount` |
| `OnHealHealthEvent` | `event HealthHealed` | plain | `entity_ref Entity; FP Amount` |
| `OnMaxHealthEvent` | `event HealthMaxReached` | plain | `entity_ref Entity` |
| `OnOverhealEvent` | `event HealthOverhealed` | plain | `entity_ref Entity; FP Overage` |
| `OnChargeShieldEvent` | `event ShieldCharged` | plain | `entity_ref Entity; FP Amount` |
| `OnMaxShieldEvent` | `event ShieldMaxReached` | plain | `entity_ref Entity` |
| `OnOverchargeShieldEvent` | `event ShieldOvercharged` | plain | `entity_ref Entity; FP Overage` |
| `OnReviveEvent` | `synced event HealthRevived` | synced | `entity_ref Entity` |
| `OnRegenHealthStartEvent` | `event HealthRegenStarted` | plain | `entity_ref Entity` |
| `OnRegenShieldStartEvent` | `event ShieldRegenStarted` | plain | `entity_ref Entity` |

`synced` is used for `HealthDied` and `HealthRevived` because these represent
definitive state transitions (death, resurrection) that must be confirmed and
are commonly used to trigger persistent gameplay effects.

`HealthChangeNormalized` and `ShieldChangeNormalized` are omitted as separate
events; the view computes `current / max` from the payload of `HealthChanged`
/ `ShieldChanged`.

---

### View MonoBehaviour → QuantumEntityViewComponent

Two view scripts are generated:

**`HealthView.cs`** — `QuantumEntityViewComponent`
Polls `VerifiedFrame` in `OnUpdateView()` for continuous bar/text updates.
Subscribes to synced events (`HealthDied`, `HealthRevived`) for confirmed
reactions (VFX, animation triggers). Exposes `[SerializeField]` references for
TMP_Text labels and Image fill bars.

**`HealthUIEventEmitter.cs`** — `QuantumEntityViewComponent`
Bridges all Quantum simulation events to `UnityEvent<T>` fields wirable
in the Unity inspector. Exposes:
- `UnityEvent<float>` for absolute health/shield values
- `UnityEvent<float>` for normalized (0–1) health/shield values (ratio computed
  from `CurrentHealth / MaxHealth` payload, not a separate simulation event)
- `UnityEvent<string>` for pre-formatted text (rounded integer)
- `UnityEvent` / `UnityEvent<float>` for all status events (died, revived,
  max health, overheal, damage, heal, regen start, etc.)

This component allows designers to wire Image.fillAmount, TMP_Text, Animator
parameters, and AudioSource triggers directly in the inspector without any
additional code, equivalent to the original `SetImageFill.cs` / `SetText.cs`
sample helpers but integrated and self-contained.

---

### Items with No Clean Quantum Equivalent — Decisions

| Issue | Decision |
|-------|----------|
| **Coroutines for regen** — Quantum has no coroutines | Replace with per-tick accumulator: delay timer counts down on component, then regen ticks the value each frame at `rate * DeltaTime`. Sequencing (`HealthThenShield`, etc.) is handled by conditional logic in Update using the component flags. |
| **Three Revive overloads** — method overloads can't be signals | Unified into one signal `OnHealthRevive(entity_ref, FP healthPercent, FP shieldPercent)`. Full revive = `(1, 0)`, absolute amounts must be pre-converted to percent by the caller (e.g., `amount / config.MaxHealth`). |
| **`MaxHealthBonus` additive modifier** — mutable float on component | Kept as `FP MaxHealthBonus` on the component. Any system wishing to grant a max-health bonus writes this field directly. `MaxHealth` is always computed as `Config.MaxHealth + MaxHealthBonus`. |
| **Negative-argument guards** (throw ArgumentException) | Quantum simulation must not throw exceptions at runtime (breaks determinism). Replace with `Log.Warn` + early return. |

---

## Phase 3 — File Generation Plan

### Files to Generate (in order)

| Step | File | Assembly |
|------|------|----------|
| 3a | `Assets/QuantumUser/Simulation/Health/Health.qtn` | Quantum.Simulation (codegen) |
| 3b | `Assets/QuantumUser/Simulation/Health/HealthConfig.cs` | Quantum.Simulation |
| 3c | `Assets/QuantumUser/Simulation/Health/HealthSystem.cs` | Quantum.Simulation |
| 3d | `Assets/QuantumUser/Simulation/Health/HealthInitSystem.cs` | Quantum.Simulation |
| 3e | `Assets/QuantumUser/Simulation/SystemSetup.User.cs` | Quantum.Simulation |
| 3f | `Assets/QuantumUser/View/Health/HealthView.cs` | Quantum.Unity |
| 3f | `Assets/QuantumUser/View/Health/HealthUIEventEmitter.cs` | Quantum.Unity |
| 3g | `Assets/QuantumUser/Editor/Health/HealthConfigSampleSetup.cs` | Quantum.Unity.Editor |
| 3g | `Assets/QuantumUser/Health/Sample/README.md` | — |

---

## Phase 4 — Verification Checklist (to be completed after generation)

### Assembly boundaries
- [ ] No file under `QuantumUser/Simulation/` contains `using UnityEngine;`
- [ ] No file under `QuantumUser/View/` is referenced from any Simulation file
- [ ] `SystemSetup.User.cs` is at root of `QuantumUser/Simulation/`, not in subfolder

### QTN file
- [ ] No C# types appear inside the `.qtn` file
- [ ] Every `asset X;` has a corresponding `.cs` AssetObject file
- [ ] Every `signal` has at least one `ISignalOnX` implementation in a System
- [ ] Every `event` is fired at least once in simulation code
- [ ] No `list<T>` fields present (none needed, so no allocation/free needed)

### Systems
- [ ] All systems added to `SystemSetup.User.cs` in correct order (Init before main)
- [ ] No system has mutable instance fields
- [ ] All signal interface method signatures match codegen output
- [ ] All math uses FPMath / FP literals — no float arithmetic anywhere

### Fidelity
- [ ] Every state field from original has counterpart in component or asset
- [ ] Every public method has counterpart signal, system method, or asset virtual method
- [ ] Every UnityEvent has a Quantum event or signal
- [ ] All default values match original HealthTraits defaults

### Package completeness
- [ ] Core files under `QuantumUser/Simulation/Health/` and `QuantumUser/View/Health/`
- [ ] Sample files under `QuantumUser/Health/Sample/` and documented in README
- [ ] No files placed under `Assets/Photon/`
- [ ] `Generated/` folders not manually edited
