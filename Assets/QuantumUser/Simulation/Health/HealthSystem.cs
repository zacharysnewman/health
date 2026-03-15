#if HEALTH_QUANTUM
// ────────────────────────────────────────────────────────────────────────────
// HealthSystem.cs  —  Quantum.Simulation  (no UnityEngine references)
//
// Deterministic multiplayer port of Healthy.Health (MonoBehaviour).
//
// Responsibilities:
//   • Per-tick regeneration state machine (replaces Unity coroutines)
//   • ISignalOnHealthDamage   → Health.Damage(float)
//   • ISignalOnHealthHeal     → Health.HealHealth(float)
//   • ISignalOnHealthChargeShield → Health.ChargeShield(float)
//   • ISignalOnHealthRevive   → Health.Revive() overloads (unified)
//
// Callers in other simulation systems invoke signals via f.Signals.*
// ────────────────────────────────────────────────────────────────────────────

namespace Quantum
{
    using Photon.Deterministic;

    public unsafe class HealthSystem : SystemMainThreadFilter<HealthSystem.Filter>,
        ISignalOnHealthDamage,
        ISignalOnHealthHeal,
        ISignalOnHealthChargeShield,
        ISignalOnHealthRevive
    {
        // ── Filter ────────────────────────────────────────────────────────────

        public struct Filter
        {
            public EntityRef       Entity;
            public HealthComponent* Health;
        }

        // ── Update (regen tick) ───────────────────────────────────────────────

        public override void Update(Frame f, ref Filter filter)
        {
            var h = filter.Health;
            if (h->IsDead) return;

            var config = f.FindAsset(h->Config);
            TickRegen(f, filter.Entity, h, config);
        }

        // ── Signal: Damage ────────────────────────────────────────────────────
        // Mirrors Health.Damage(float amount)

        public void OnHealthDamage(Frame f, EntityRef entity, FP amount)
        {
            if (!f.Unsafe.TryGetPointer<HealthComponent>(entity, out var h)) return;

            if (amount < FP._0)
            {
                Log.Warn($"HealthSystem: Damage amount cannot be negative (got {amount}). Ignoring.");
                return;
            }

            if (h->IsDead)
            {
                f.Events.HealthOverkill(entity, amount);
                return;
            }

            StopRegen(h);

            var config  = f.FindAsset(h->Config);
            FP maxHealth = config.MaxHealth + h->MaxHealthBonus;

            // Apply shield/health damage split — mirrors HealthUtils.CalculateDamageSplit
            ApplyDamageSplit(amount, ref h->CurrentShield, ref h->CurrentHealth, config.ShieldBleedThrough);
            h->CurrentShield = FPMath.Clamp(h->CurrentShield, FP._0, config.MaxShield);
            h->CurrentHealth = FPMath.Clamp(h->CurrentHealth, FP._0, maxHealth);

            f.Events.ShieldChanged(entity, h->CurrentShield, config.MaxShield);
            f.Events.HealthChanged(entity, h->CurrentHealth, maxHealth);

            if (h->CurrentHealth <= FP._0)
            {
                h->IsDead = true;
                f.Events.HealthDied(entity, amount);   // synced — confirmed death
            }
            else
            {
                StartRegen(f, entity, h, config, withDelay: true);
                f.Events.HealthDamaged(entity, amount);
            }
        }

        // ── Signal: Heal ──────────────────────────────────────────────────────
        // Mirrors Health.HealHealth(float amount)

        public void OnHealthHeal(Frame f, EntityRef entity, FP amount)
        {
            if (!f.Unsafe.TryGetPointer<HealthComponent>(entity, out var h)) return;

            if (amount < FP._0)
            {
                Log.Warn($"HealthSystem: Heal amount cannot be negative (got {amount}). Ignoring.");
                return;
            }

            if (h->IsDead) return;

            var config   = f.FindAsset(h->Config);
            FP maxHealth  = config.MaxHealth + h->MaxHealthBonus;
            bool wasAtMax = h->CurrentHealth >= maxHealth;
            FP overage    = FPMath.Max(FP._0, h->CurrentHealth + amount - maxHealth);

            h->CurrentHealth = FPMath.Clamp(h->CurrentHealth + amount, FP._0, maxHealth);

            f.Events.HealthChanged(entity, h->CurrentHealth, maxHealth);
            f.Events.HealthHealed(entity, amount);

            if (!wasAtMax && h->CurrentHealth >= maxHealth)
                f.Events.HealthMaxReached(entity);

            if (overage > FP._0)
                f.Events.HealthOverhealed(entity, overage);
        }

        // ── Signal: ChargeShield ──────────────────────────────────────────────
        // Mirrors Health.ChargeShield(float amount)

        public void OnHealthChargeShield(Frame f, EntityRef entity, FP amount)
        {
            if (!f.Unsafe.TryGetPointer<HealthComponent>(entity, out var h)) return;

            if (amount < FP._0)
            {
                Log.Warn($"HealthSystem: ChargeShield amount cannot be negative (got {amount}). Ignoring.");
                return;
            }

            var config = f.FindAsset(h->Config);
            if (h->IsDead || !config.HasShields) return;

            bool wasAtMax = h->CurrentShield >= config.MaxShield;
            FP overage    = FPMath.Max(FP._0, h->CurrentShield + amount - config.MaxShield);

            h->CurrentShield = FPMath.Clamp(h->CurrentShield + amount, FP._0, config.MaxShield);

            f.Events.ShieldChanged(entity, h->CurrentShield, config.MaxShield);
            f.Events.ShieldCharged(entity, amount);

            if (!wasAtMax && h->CurrentShield >= config.MaxShield)
                f.Events.ShieldMaxReached(entity);

            if (overage > FP._0)
                f.Events.ShieldOvercharged(entity, overage);
        }

        // ── Signal: Revive ────────────────────────────────────────────────────
        // Unified form of Health.Revive() / Revive(float,float) / Revive(int,int).
        // Pass healthPercent=1, shieldPercent=0 for the parameterless overload.
        // Mirrors Health.ReviveInternal(float health, float shield).

        public void OnHealthRevive(Frame f, EntityRef entity, FP healthPercent, FP shieldPercent)
        {
            if (!f.Unsafe.TryGetPointer<HealthComponent>(entity, out var h)) return;
            if (!h->IsDead) return;

            var config   = f.FindAsset(h->Config);
            FP maxHealth  = config.MaxHealth + h->MaxHealthBonus;

            h->IsDead        = false;
            h->CurrentHealth = FPMath.Clamp(maxHealth   * FPMath.Clamp01(healthPercent), FP._0, maxHealth);
            h->CurrentShield = FPMath.Clamp(config.MaxShield * FPMath.Clamp01(shieldPercent), FP._0, config.MaxShield);

            f.Events.HealthChanged(entity, h->CurrentHealth, maxHealth);
            f.Events.ShieldChanged(entity, h->CurrentShield, config.MaxShield);

            StartRegen(f, entity, h, config, withDelay: false);

            f.Events.HealthRevived(entity);   // synced — confirmed revival
        }

        // ── Regen state machine ───────────────────────────────────────────────
        // Replaces Health.StartRegenCoroutine / RegenHealthCoroutine /
        // RegenShieldCoroutine.  Runs every Update tick for living entities.

        private static void TickRegen(Frame f, EntityRef entity, HealthComponent* h, HealthConfig config)
        {
            FP dt       = f.DeltaTime;
            FP maxHealth = config.MaxHealth + h->MaxHealthBonus;

            // ── Health delay countdown ────────────────────────────────────────
            if (h->HealthRegenDelayActive)
            {
                h->HealthRegenDelayTimer -= dt;
                if (h->HealthRegenDelayTimer <= FP._0)
                {
                    h->HealthRegenDelayActive  = false;
                    h->IsHealthRegenerating    = true;
                    f.Events.HealthRegenStarted(entity);  // mirrors OnRegenHealthStartEvent
                }
            }

            // ── Shield delay countdown ────────────────────────────────────────
            if (h->ShieldRegenDelayActive)
            {
                h->ShieldRegenDelayTimer -= dt;
                if (h->ShieldRegenDelayTimer <= FP._0)
                {
                    h->ShieldRegenDelayActive  = false;
                    h->IsShieldRegenerating    = true;
                    f.Events.ShieldRegenStarted(entity);  // mirrors OnRegenShieldStartEvent
                }
            }

            // ── Health regen tick ─────────────────────────────────────────────
            if (h->IsHealthRegenerating && config.ShouldHealthRegen)
            {
                h->CurrentHealth = FPMath.Clamp(h->CurrentHealth + config.HealthRegenRate * dt, FP._0, maxHealth);
                f.Events.HealthChanged(entity, h->CurrentHealth, maxHealth);

                if (h->CurrentHealth >= maxHealth)
                {
                    h->IsHealthRegenerating = false;
                    f.Events.HealthMaxReached(entity);   // mirrors OnMaxHealthEvent

                    // HealthThenShield: chain into shield regen (withDelay: false)
                    if (config.RegenTrigger == RegenTrigger.HealthThenShield
                        && config.HasShields
                        && config.ShouldShieldRegen
                        && h->CurrentShield < config.MaxShield)
                    {
                        h->IsShieldRegenerating = true;
                        f.Events.ShieldRegenStarted(entity);
                    }
                }
            }

            // ── Shield regen tick ─────────────────────────────────────────────
            if (h->IsShieldRegenerating && config.ShouldShieldRegen && config.HasShields)
            {
                h->CurrentShield = FPMath.Clamp(h->CurrentShield + config.ShieldRegenRate * dt, FP._0, config.MaxShield);
                f.Events.ShieldChanged(entity, h->CurrentShield, config.MaxShield);

                if (h->CurrentShield >= config.MaxShield)
                {
                    h->IsShieldRegenerating = false;
                    f.Events.ShieldMaxReached(entity);   // mirrors OnMaxShieldEvent

                    // ShieldThenHealth: chain into health regen (withDelay: false)
                    if (config.RegenTrigger == RegenTrigger.ShieldThenHealth
                        && config.ShouldHealthRegen
                        && h->CurrentHealth < maxHealth)
                    {
                        h->IsHealthRegenerating = true;
                        f.Events.HealthRegenStarted(entity);
                    }
                }
            }
        }

        // ── Regen arming helpers ──────────────────────────────────────────────
        // Mirror Health.StartRegen(bool withDelay) / StopRegen().

        private static void StartRegen(Frame f, EntityRef entity, HealthComponent* h, HealthConfig config, bool withDelay)
        {
            StopRegen(h);

            switch (config.RegenTrigger)
            {
                case RegenTrigger.HealthThenShield:
                    // Health delay armed; shield will chain after health completes.
                    ArmHealthRegen(f, entity, h, config, withDelay);
                    break;

                case RegenTrigger.ShieldThenHealth:
                    // Shield delay armed; health will chain after shield completes.
                    ArmShieldRegen(f, entity, h, config, withDelay);
                    break;

                case RegenTrigger.HealthAndShield:
                    // Both arm simultaneously.
                    ArmHealthRegen(f, entity, h, config, withDelay);
                    ArmShieldRegen(f, entity, h, config, withDelay);
                    break;
            }
        }

        private static void ArmHealthRegen(Frame f, EntityRef entity, HealthComponent* h, HealthConfig config, bool withDelay)
        {
            if (!config.ShouldHealthRegen) return;

            if (withDelay && config.HealthRegenDelay > FP._0)
            {
                h->HealthRegenDelayActive = true;
                h->HealthRegenDelayTimer  = config.HealthRegenDelay;
            }
            else
            {
                // No delay path — mirrors RegenHealthCoroutine(withDelay: false)
                h->IsHealthRegenerating = true;
                f.Events.HealthRegenStarted(entity);
            }
        }

        private static void ArmShieldRegen(Frame f, EntityRef entity, HealthComponent* h, HealthConfig config, bool withDelay)
        {
            if (!config.ShouldShieldRegen || !config.HasShields) return;

            if (withDelay && config.ShieldRegenDelay > FP._0)
            {
                h->ShieldRegenDelayActive = true;
                h->ShieldRegenDelayTimer  = config.ShieldRegenDelay;
            }
            else
            {
                // No delay path — mirrors RegenShieldCoroutine(withDelay: false)
                h->IsShieldRegenerating = true;
                f.Events.ShieldRegenStarted(entity);
            }
        }

        /// <summary>Stops all active regen.  Mirrors Health.StopRegen() / StopAllCoroutines().</summary>
        private static void StopRegen(HealthComponent* h)
        {
            h->HealthRegenDelayActive = false;
            h->ShieldRegenDelayActive = false;
            h->HealthRegenDelayTimer  = FP._0;
            h->ShieldRegenDelayTimer  = FP._0;
            h->IsHealthRegenerating   = false;
            h->IsShieldRegenerating   = false;
        }

        // ── Damage split ──────────────────────────────────────────────────────
        // Pure FP port of HealthUtils.CalculateDamageSplit().
        // Logic is identical; types changed from float to FP.

        private static void ApplyDamageSplit(
            FP   damage,
            ref FP currentShield,
            ref FP currentHealth,
            bool shieldBleedThrough)
        {
            bool damageOnlyHitsShield = currentShield >= damage;
            bool damageOnlyHitsHealth = currentShield <= FP._0;

            if (damageOnlyHitsShield)
            {
                currentShield -= damage;
            }
            else if (damageOnlyHitsHealth)
            {
                currentHealth -= damage;
            }
            else if (shieldBleedThrough)
            {
                currentHealth -= damage - currentShield;
                currentShield  = FP._0;
            }
            else
            {
                currentShield = FP._0;
            }
        }
    }
}

#endif
