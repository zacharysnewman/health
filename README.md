# Health

A Unity package providing drag-and-drop health and shield management via a `Health` MonoBehaviour and a `HealthData` ScriptableObject.

## Installation

Add via Unity Package Manager using the git URL for this repository, or copy the `Runtime/` folder into your project.

## Setup

1. Create a `HealthData` asset: **Assets > Create > Health > HealthData**
2. Configure the traits in the Inspector (max health, shields, regen, etc.)
3. Add the `Health` component to your GameObject and assign the `HealthData` asset
4. Wire up any `HealthEvents` in the Inspector

## HealthData (ScriptableObject)

All configuration lives in `HealthTraits`, serialized inside a `HealthData` asset.

| Field | Type | Default | Description |
|---|---|---|---|
| `MaxHealth` | float | 100 | Maximum health points |
| `MaxShield` | float | 100 | Maximum shield points |
| `HasShields` | bool | true | Whether shields are active |
| `ShieldBleedThrough` | bool | true | Excess damage carries through to health when shield breaks |
| `ShouldHealthRegen` | bool | true | Whether health regenerates |
| `ShouldShieldRegen` | bool | true | Whether shield regenerates |
| `HealthRegenRate` | float | 10 | Health regenerated per second |
| `ShieldRegenRate` | float | 20 | Shield regenerated per second |
| `HealthRegenDelay` | float | 1 | Seconds after damage before health regen starts |
| `ShieldRegenDelay` | float | 2 | Seconds after damage before shield regen starts |
| `RegenTrigger` | enum | HealthThenShield | Order in which health and shield regenerate |

### RegenTrigger

| Value | Behavior |
|---|---|
| `HealthThenShield` | Health regenerates fully, then shield begins |
| `ShieldThenHealth` | Shield regenerates fully, then health begins |
| `HealthAndShield` | Health and shield regenerate simultaneously |

## Health (MonoBehaviour)

### Public API

| Method / Property | Description |
|---|---|
| `CurrentHealth` | Current health value (clamped 0–MaxHealth) |
| `CurrentShield` | Current shield value (clamped 0–MaxShield) |
| `IsDead` | Whether the entity is currently dead |
| `MaxHealthBonus` | Flat bonus added to MaxHealth at runtime |
| `InitializeValues()` | Resets health and shield to max, clears dead state |
| `Damage(float amount)` | Applies damage, respecting shields and bleed-through |
| `HealHealth(float amount)` | Heals health (no-op if dead) |
| `ChargeShield(float amount)` | Adds shield charge (no-op if dead or shields disabled) |
| `Revive()` | Revives at full health and zero shield |
| `Revive(float healthPercent, float shieldPercent)` | Revives at given percentages |
| `Revive(int healthAmount, int shieldAmount)` | Revives at given flat amounts |
| `StartRegen(bool withDelay)` | Manually starts regeneration |
| `StopRegen()` | Stops all regeneration coroutines |

## HealthEvents

Assign listeners in the Inspector or via code. All events are `UnityEvent<float>` unless noted.

| Event | Payload | Fires when... |
|---|---|---|
| `OnHealthChangeEvent` | current health | Health value changes |
| `OnHealthChangeNormalizedEvent` | 0–1 | Health value changes (normalized) |
| `OnShieldChangeEvent` | current shield | Shield value changes |
| `OnShieldChangeNormalizedEvent` | 0–1 | Shield value changes (normalized) |
| `OnDamageEvent` | damage amount | Entity takes damage and survives |
| `OnDieEvent` | damage amount | Entity dies |
| `OnOverkillEvent` | damage amount | Damage applied while already dead |
| `OnHealHealthEvent` | heal amount | Health is healed |
| `OnOverhealEvent` | overage amount | Heal exceeds max health |
| `OnChargeShieldEvent` | charge amount | Shield is charged |
| `OnOverchargeShieldEvent` | overage amount | Charge exceeds max shield |
| `OnReviveEvent` | _(none)_ | Entity is revived |
| `OnRegenHealthStartEvent` | _(none)_ | Health regen begins |
| `OnRegenShieldStartEvent` | _(none)_ | Shield regen begins |

## Networking Integration

The core `Health` component has no networking dependencies and works standalone. Optional networking adapters live in `Runtime/Networking/` as separate assemblies that only compile when the corresponding framework is present.

### Available adapters

| Adapter | Class | Namespace | Framework |
|---|---|---|---|
| Mirror | `MirrorHealth` | `Healthy.Networking.Mirror` | [Mirror](https://mirror-networking.gitbook.io/) |
| Netcode for GameObjects | `NetworkHealth` | `Healthy.Networking.NGO` | [Unity NGO](https://docs-multiplayer.unity3d.com/) |
| Photon Fusion | `FusionHealth` | `Healthy.Networking.Fusion` | [Photon Fusion](https://doc.photonengine.com/fusion/) |

### Setup

**Netcode for GameObjects** — no extra steps. Installing `com.unity.netcode.gameobjects` via Package Manager automatically defines `HEALTH_NGO` and compiles the adapter.

**Mirror / Photon Fusion** — after installing the framework, add the corresponding define to **Project Settings > Player > Scripting Define Symbols**:

| Framework | Define symbol |
|---|---|
| Mirror | `HEALTH_MIRROR` |
| Photon Fusion | `HEALTH_FUSION` |

### Usage

Add the adapter component alongside `Health` on the same GameObject (plus any component required by the framework, e.g. `NetworkIdentity` for Mirror or `NetworkObject` for NGO). The adapter is server-authoritative: instead of calling `Health` methods directly from clients, call the adapter's network method instead.

**Mirror example**
```csharp
// Client-side — route through the Command instead of calling Health directly.
GetComponent<MirrorHealth>().CmdDamage(25f);
```

**NGO example**
```csharp
GetComponent<NetworkHealth>().DamageServerRpc(25f);
```

**Fusion example**
```csharp
GetComponent<FusionHealth>().Rpc_Damage(25f);
```

The server applies the call to the core `Health` component. State and events are replicated to all clients, so UI, audio, and visual-effect hooks on `HealthEvents` work identically on remote players.

### What fires on clients

All `HealthEvents` fire on every client, including the server/host:

| How it reaches clients | Events |
|---|---|
| NetworkVariable / SyncVar / `[Networked]` property | `OnHealthChangeEvent`, `OnHealthChangeNormalizedEvent`, `OnShieldChangeEvent`, `OnShieldChangeNormalizedEvent` |
| Broadcast RPC (server → all clients) | `OnDamageEvent`, `OnDieEvent`, `OnOverkillEvent`, `OnHealHealthEvent`, `OnOverhealEvent`, `OnChargeShieldEvent`, `OnOverchargeShieldEvent`, `OnReviveEvent`, `OnRegenHealthStartEvent`, `OnRegenShieldStartEvent` |

**Late joiners:** clients who join while a player is already dead receive `OnDieEvent(0f)` immediately on spawn so death visuals (ragdolls, UI, etc.) initialize correctly. The damage amount is unavailable at that point, hence `0f`.

**`OnDieEvent` amount:** carries the real damage amount for live kills. For late-joining clients initializing into a pre-existing dead state it is always `0f`.

## Samples

An **Example UI Usage** sample is included. Import it from the Package Manager to get a demo scene showing health and shield bars driven by `SetImageFill` and `SetText` helper scripts.
