# Design Pattern: Config-Driven Reactive Domain Object

This document describes the core architectural pattern used in this package and how to apply it to other systems.

---

## Pattern Name

**Config-Driven Reactive Domain Object**

---

## Intent

Encapsulate a domain concept's state and behavior in a single authoritative object. All state mutations happen through a validated command API. Configuration is externalized into a reusable asset. Every state change automatically notifies a declared event contract, which consumers wire to externally — in the inspector or in code — without the domain object knowing anything about them.

---

## The Three Layers

### 1. Trait Asset (Configuration)

A `ScriptableObject` (or serializable class nested inside one) that holds all tunable parameters. It is read-only at runtime and shared across any number of domain object instances.

```
HealthData (ScriptableObject)
  └── HealthTraits (Serializable)
        MaxHealth, MaxShield, RegenRate, RegenDelay,
        ShieldBleedThrough, HasShields, RegenTrigger, ...
```

**Rules:**
- All fields private with read-only public properties.
- No behavior — pure data.
- Swappable per entity type (player, NPC, environment) without touching code.
- Runtime mutations (e.g. `MaxHealthBonus`) live on the domain object, not the asset.

---

### 2. Domain Object (State + Behavior)

The `MonoBehaviour` (or simulation component) that owns all runtime state. It has two surfaces:

**Command API** — the only way to mutate state:
```csharp
Damage(float amount)
HealHealth(float amount)
ChargeShield(float amount)
Revive()
```
Commands validate inputs, apply pure utility functions, update state, and fire events. They guard against invalid transitions (e.g. healing a dead entity is a no-op).

**Reactive Properties** — state with built-in notification:
```csharp
public float CurrentHealth
{
    get => currentHealth;
    set
    {
        currentHealth = Mathf.Clamp(value, 0, MaxHealth);
        events.OnHealthChangeEvent?.Invoke(currentHealth);
        events.OnHealthChangeNormalizedEvent?.Invoke(currentHealth / MaxHealth);
    }
}
```
The property setter clamps, assigns, and pushes — mutation and notification are inseparable. There is no way to change state without notifying.

**Time-based behavior** uses coroutines (singleplayer) or a per-tick state machine (deterministic multiplayer). Both read from the trait asset and write only through the reactive property setters, so regen notifications are identical to manual mutation notifications.

---

### 3. Event Contract (Declared Observable Interface)

A serializable class (`HealthEvents`) that contains every `UnityEvent` the domain object can emit. It is a manifest — it declares what this object communicates, not who receives it.

```
HealthEvents
  OnHealthChangeEvent<float>          — every health mutation
  OnHealthChangeNormalizedEvent<float> — same, normalized 0-1
  OnDamageEvent<float>                — non-lethal damage applied
  OnDieEvent<float>                   — entity died, with killing blow amount
  OnOverkillEvent<float>              — damage to already-dead entity
  OnHealHealthEvent<float>            — heal applied
  OnOverhealEvent<float>              — excess heal amount
  OnMaxHealthEvent                    — health reached cap
  OnReviveEvent                       — entity revived
  OnRegenHealthStartEvent             — regen delay elapsed, regen begins
  OnChargeShieldEvent<float>          — shield charge applied
  OnOverchargeShieldEvent<float>      — excess charge amount
  OnMaxShieldEvent                    — shield reached cap
  OnShieldChangeEvent<float>          — every shield mutation
  OnShieldChangeNormalizedEvent<float>
  OnRegenShieldStartEvent
```

**Two event types, by purpose:**

| Type | Examples | Purpose |
|---|---|---|
| **Value events** | `OnHealthChangeEvent`, `OnShieldChangeNormalizedEvent` | Continuous state — wire to UI bars/labels that must always reflect current value |
| **Semantic events** | `OnDieEvent`, `OnOverkillEvent`, `OnOverhealEvent` | Intent — wire to VFX, SFX, animations that respond to what happened, not just the new value |

Value events fire on every mutation including regen ticks. Semantic events fire once per action. Consumers choose which they need.

---

## Data Flow (Singleplayer)

```
External caller
  │
  ▼
Health.TakeDamage(info)
  │
  ├─► HealthUtils.CalculateDamageSplit()   [pure function, no side effects]
  │     returns (shieldRemaining, healthRemaining)
  │
  ├─► CurrentShield = result               [reactive property]
  │     └─► OnShieldChangeEvent.Invoke()
  │     └─► OnShieldChangeNormalizedEvent.Invoke()
  │
  ├─► CurrentHealth = result               [reactive property]
  │     └─► OnHealthChangeEvent.Invoke()
  │     └─► OnHealthChangeNormalizedEvent.Invoke()
  │
  └─► if dead:
  │     IsDead = true
  │     OnDieEvent.Invoke(amount)
  │     StopRegen()
  └─► if alive:
        OnDamageEvent.Invoke(amount)
        StopRegen()
        StartRegen(withDelay: true)
          └─► coroutine: delay → tick CurrentHealth/CurrentShield
                each tick fires value events → UI updates continuously
              → on max: OnMaxHealthEvent / OnMaxShieldEvent
```

---

## Multiplayer: Two Approaches

The core domain object has zero networking code. Multiplayer is handled by two distinct strategies, each appropriate for a different scenario.

---

### Strategy A: Server-Authoritative Adapter (NGO / Mirror / Fusion)

A separate `NetworkBehaviour` component sits alongside the core `Health` component. The core is completely unchanged.

**On the server:**
1. Commands arrive via `ServerRpc` from clients.
2. Server calls the core command API (`health.TakeDamage(info)`).
3. Core fires events normally.
4. The adapter listens to those events and does two things:
   - Writes the new value to a `NetworkVariable` (state replication — continuous)
   - Broadcasts a `ClientRpc` for semantic events (intent replication — one-shot)

**On clients:**
1. `NetworkVariable` `OnValueChanged` callbacks write directly to `health.CurrentHealth` / `health.CurrentShield` (bypassing the command API — these are replicated values, not new commands).
2. `ClientRpc` handlers invoke the semantic events directly on `health.events`.
3. Regen does not run on clients — it runs on the server and replicates via `NetworkVariable`.

**Late joiner handling:**
`NetworkVariable` carries current state, so a client joining mid-game receives correct values immediately. Semantic state (dead/alive) requires an explicit initialization step — if `netIsDead.Value` is true on spawn, the adapter manually fires `OnDieEvent(0f)` so visuals initialize correctly. This is intentional: value and meaning are different things, and the value alone is insufficient to reconstruct semantic state.

```
[Client] DamageServerRpc(25)
  │
  ▼
[Server] health.TakeDamage(info)
  ├─► core events fire (server-local UI updates immediately)
  ├─► OnHealthChangeEvent → netHealth.Value = newValue   [NetworkVariable → all clients]
  └─► OnDamageEvent       → BroadcastDamageClientRpc()  [ClientRpc → all clients]

[Client] OnNetHealthChanged → health.CurrentHealth = newValue  [value event fires → UI]
[Client] BroadcastDamageClientRpc → health.events.OnDamageEvent.Invoke()  [semantic event → VFX]
```

---

### Strategy B: Deterministic Simulation (Quantum)

A complete parallel implementation — not an adapter. Used when deterministic, rollback-safe multiplayer is required.

**Simulation layer** (no `UnityEngine` references, runs identically on all peers):
- `HealthComponent` (`.qtn` schema) stores state as fixed-point (`FP`) values.
- `HealthConfig` (Quantum asset) replaces `HealthData` / `HealthTraits`.
- `HealthSystem` replaces coroutines with a **per-tick state machine**: delay countdown flags and regen-active flags are stored on the component and ticked each `Update()`.
- Signals (`ISignalOnHealthDamage`, etc.) replace the command API — other systems invoke `f.Signals.OnHealthDamage(entity, amount)`.
- `f.Events.*` replaces `UnityEvent.Invoke()` — events are deterministic and confirmed across all peers before firing.

**View layer** (Unity-side, per client):
- `HealthView` polls `VerifiedFrame` each tick to update UI continuously.
- `HealthUIEventEmitter` subscribes to Quantum events and re-emits them as `UnityEvent<T>` — the same inspector-wirable interface as the singleplayer `HealthEvents`, so UI setup is identical regardless of backend.

**Key difference from the adapter approach:** there is no core `Health` MonoBehaviour in the loop. The simulation is self-contained. The view layer is a read-only consumer of confirmed simulation state.

```
[Any peer simulation] f.Signals.OnHealthDamage(entity, 25)
  │
  ▼
[HealthSystem.OnHealthDamage]
  ├─► ApplyDamageSplit()               [pure FP calculation]
  ├─► h->CurrentHealth = result        [struct field mutation]
  ├─► f.Events.HealthChanged(...)      [deterministic event — fires on all peers after confirmation]
  └─► f.Events.HealthDied(...)         [synced event — rollback-safe]

[View — all peers] HealthUIEventEmitter.HandleDied()
  └─► OnDied.Invoke()                  [UnityEvent → animator, VFX, etc.]
```

---

## Applying This Pattern to Other Systems

When building a new domain system, follow this structure:

1. **Trait asset** — `MySystemData` (ScriptableObject) containing `MySystemTraits` (Serializable). All parameters here, read-only.

2. **Domain object** — `MySystem` (MonoBehaviour). Private state fields. Public reactive properties that clamp/validate and fire events on set. Public command methods that validate, call a pure utility function, update properties, and fire semantic events. Zero knowledge of UI, networking, or consumers.

3. **Event contract** — `MySystemEvents` (Serializable). Every value event and every semantic event declared up front. Attached as a public field on the domain object, wired in inspector by consumers.

4. **Pure utility** — `MySystemUtils` (static class). Stateless calculations only. Input in, output out. No side effects. Usable in both singleplayer and deterministic simulation.

5. **Multiplayer** — choose adapter (NGO/Mirror/Fusion) if you need server-authority with minimal setup, or deterministic simulation (Quantum) if you need rollback and cross-platform consistency. Either way, the core domain object does not change.

---

## What This Pattern Is Not

- **Not Observable streams** — state is not a stream. Events are push notifications about mutations, not composable value sequences. There are no operators, no subscription lifecycle tokens, no completion semantics. This is simpler and sufficient when consumers react independently to individual events.

- **Not Event Sourcing** — events are notifications, not the source of truth. State is the source of truth. Events are ephemeral; they are not replayed to reconstruct state (late joiners read current state from `NetworkVariable` / `VerifiedFrame`, not from a replay of events).

- **Not MVC/MVVM** — there is no view model or controller. The domain object is not aware of views. The event contract is the boundary; views wire themselves to it.
