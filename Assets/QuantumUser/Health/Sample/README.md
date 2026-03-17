# Health — Quantum 3 Port  |  Sample Files

Everything in this folder (`Assets/QuantumUser/Health/Sample/`) is **optional**.
You can delete the entire `Assets/QuantumUser/Health/` folder hierarchy without
breaking the system.  The core files are listed separately below.

---

## Which files are core vs sample?

### Core — required for the system to function

| File | Purpose |
|------|---------|
| `Assets/QuantumUser/Simulation/Health/Health.qtn` | DSL: component, enum, signals, events |
| `Assets/QuantumUser/Simulation/Health/HealthConfig.cs` | AssetObject replacing HealthData + HealthTraits |
| `Assets/QuantumUser/Simulation/Health/HealthSystem.cs` | Damage, heal, shield, revive, regen logic |
| `Assets/QuantumUser/Simulation/Health/HealthInitSystem.cs` | Initialises component fields on spawn |
| `Assets/QuantumUser/Simulation/SystemSetup.User.cs` | Registers systems with the Quantum runner |
| `Assets/QuantumUser/View/Health/HealthView.cs` | Entity view: polls bars, reacts to death/revive |
| `Assets/QuantumUser/View/Health/HealthUIEventEmitter.cs` | Inspector-wirable UnityEvent bridge for all health events |

### Sample — delete freely

| File | Purpose |
|------|---------|
| `Assets/QuantumUser/Editor/Health/HealthConfigSampleSetup.cs` | Creates `SampleHealthConfig.asset` on first compile, then self-deletes |
| `Assets/QuantumUser/Health/Sample/SampleHealthConfig.asset` | Pre-configured HealthConfig with default values |
| `Assets/QuantumUser/Health/Sample/README.md` | This file |

---

## How to wire a HealthComponent on a prefab

1. **Create a HealthConfig asset**
   - Right-click in the Project window → `Create > Quantum > Asset`.
   - Alternatively, the sample `SampleHealthConfig.asset` is already created for
     you with default values matching the original HealthTraits defaults:
     MaxHealth 100, MaxShield 100, HealthRegenRate 10, ShieldRegenRate 20, etc.

2. **Add the component to an EntityPrototype**
   - Select your entity prefab.
   - In the `Quantum Entity Prototype` component, click `Add Component`.
   - Add `Health Component`.
   - Assign your `HealthConfig` asset to the `Config` field.

3. **Bake Quantum assets**
   - Menu: `Quantum > Bake Assets` (or it runs automatically on Play).

4. **Run** — `HealthInitSystem` sets `CurrentHealth` and `CurrentShield` to their
   configured max values when the entity spawns.

---

## What HealthView expects

Add `HealthView` to the view root of your entity (the GameObject with
`QuantumEntityView`).

| Inspector field | Assign |
|----------------|--------|
| `_healthText` | `TMP_Text` to show absolute health number |
| `_healthBarFill` | `Image` (fill mode) for health bar |
| `_shieldText` | `TMP_Text` to show absolute shield number |
| `_shieldBarFill` | `Image` (fill mode) for shield bar |
| `_deadIndicator` | `GameObject` shown when `IsDead == true` |

All fields are optional — leave unassigned if not needed.

To react to death/revive with VFX or audio, subclass `HealthView` and override
`OnHealthDied` / `OnHealthRevived`.

---

## What HealthUIEventEmitter provides

Add `HealthUIEventEmitter` to the same view root (alongside or instead of
`HealthView`) to wire health events to any Unity component in the inspector
without writing code.

| Event | Type | Typical target |
|-------|------|----------------|
| `OnHealthChangedNormalized` | `UnityEvent<float>` | `Image.fillAmount` |
| `OnShieldChangedNormalized` | `UnityEvent<float>` | `Image.fillAmount` |
| `OnHealthChangedText` | `UnityEvent<string>` | `TMP_Text.SetText` |
| `OnShieldChangedText` | `UnityEvent<string>` | `TMP_Text.SetText` |
| `OnHealthChanged` | `UnityEvent<float>` | custom scripts |
| `OnDied` | `UnityEvent` | `Animator.SetTrigger`, `AudioSource.Play` |
| `OnRevived` | `UnityEvent` | `Animator.SetTrigger` |
| `OnDamaged` | `UnityEvent<float>` | floating damage number spawner |
| `OnMaxHealthReached` | `UnityEvent` | particle effect |
| `OnOverhealed` | `UnityEvent<float>` | overheal VFX |
| `OnShieldCharged` | `UnityEvent<float>` | shield recharge sound |
| `OnShieldOvercharged` | `UnityEvent<float>` | overcharge VFX |
| `OnHealthRegenStarted` | `UnityEvent` | regen indicator |
| `OnShieldRegenStarted` | `UnityEvent` | regen indicator |

---

## Signals external systems can listen to / call

### Calling into the health system (write to simulation)

Other simulation systems dispatch these via `f.Signals.*`:

| Signal | Equivalent original method |
|--------|---------------------------|
| `f.Signals.OnHealthDamage(entity, amount)` | `Health.TakeDamage(DamageInfo)` |
| `f.Signals.OnHealthHeal(entity, amount)` | `Health.HealHealth(float)` |
| `f.Signals.OnHealthChargeShield(entity, amount)` | `Health.ChargeShield(float)` |
| `f.Signals.OnHealthRevive(entity, healthPct, shieldPct)` | `Health.Revive(...)` overloads |

For a full revive (original `Revive()`): pass `healthPercent = FP._1, shieldPercent = FP._0`.
For absolute amounts: divide by `config.MaxHealth` / `config.MaxShield` before passing.

### Reacting to health state changes (read from simulation)

Implement `ISignalOnHealthDamage`, `ISignalOnHealthHeal`, etc. in your own
`SystemSignalsOnly` subclass if another simulation system needs to react
deterministically to health events (e.g., a kill-streak system).

For view-layer reactions, subscribe to `QuantumEvent` in a
`QuantumEntityViewComponent` as shown in `HealthUIEventEmitter`.
