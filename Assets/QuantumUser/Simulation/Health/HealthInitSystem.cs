#if HEALTH_QUANTUM
// ────────────────────────────────────────────────────────────────────────────
// HealthInitSystem.cs  —  Quantum.Simulation  (no UnityEngine references)
//
// Replaces the Awake / Start / OnEnable pattern of Healthy.Health.
//
// Fires when a HealthComponent is first added to an entity (e.g. when an
// EntityPrototype is instantiated) and initialises all fields to their correct
// starting values — mirrors Health.InitializeValues().
// ────────────────────────────────────────────────────────────────────────────

namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class HealthInitSystem : SystemSignalsOnly,
        ISignalOnComponentAdded<HealthComponent>
    {
        /// <summary>
        /// Initialise all HealthComponent fields when the component is first added.
        /// Mirrors Health.InitializeValues(): health = max, shield = max, isDead = false.
        /// </summary>
        public void OnAdded(Frame f, EntityRef entity, HealthComponent* component)
        {
            var config = f.FindAsset(component->Config);

            component->CurrentHealth = config.MaxHealth;
            component->CurrentShield = config.HasShields ? config.MaxShield : FP._0;
            component->IsDead        = false;

            // MaxHealthBonus starts at zero; other systems may modify it later.
            component->MaxHealthBonus = FP._0;

            // All regen state cleared — no regen is active on spawn.
            component->HealthRegenDelayActive = false;
            component->HealthRegenDelayTimer  = FP._0;
            component->IsHealthRegenerating   = false;

            component->ShieldRegenDelayActive = false;
            component->ShieldRegenDelayTimer  = FP._0;
            component->IsShieldRegenerating   = false;
        }
    }
}

#endif
