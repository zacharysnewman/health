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

## Samples

An **Example UI Usage** sample is included. Import it from the Package Manager to get a demo scene showing health and shield bars driven by `SetImageFill` and `SetText` helper scripts.
