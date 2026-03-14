#if HEALTH_FUSION
using Fusion;
using UnityEngine;

namespace Healthy.Networking.Fusion
{
    /// <summary>
    /// Photon Fusion adapter for the Health component.
    ///
    /// Place this alongside a Health component on a networked GameObject.
    /// State authority is authoritative: other peers call RPCs and state authority applies
    /// them to the core Health component. [Networked] properties replicate state to all
    /// peers, with OnChangedRender callbacks keeping local Health values in sync.
    ///
    /// Semantic events (OnDamageEvent, OnDieEvent, OnHealHealthEvent, etc.) are
    /// broadcast to all proxies via RPCs so effects like floating damage numbers,
    /// death animations, and shield sounds work on remote players.
    ///
    /// Setup:
    ///   1. Install Photon Fusion and add HEALTH_FUSION to Project Settings > Player > Scripting Define Symbols.
    ///   2. Add Health and FusionHealth to your networked GameObject.
    ///   3. Spawn the object via NetworkRunner.Spawn (required for [Networked] to work).
    ///   4. From non-authority peers, call Rpc_Damage / Rpc_HealHealth /
    ///      Rpc_ChargeShield / Rpc_Revive instead of calling Health methods directly.
    ///   5. To change MaxHealthBonus at runtime (e.g. on level-up), call
    ///      Rpc_SetMaxHealthBonus from a non-authority peer, or set MaxHealthBonus
    ///      directly from state-authority code.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class FusionHealth : NetworkBehaviour
    {
        private Health health;

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        private float SyncedHealth { get; set; }

        [Networked, OnChangedRender(nameof(OnShieldChanged))]
        private float SyncedShield { get; set; }

        [Networked, OnChangedRender(nameof(OnIsDeadChanged))]
        private NetworkBool SyncedIsDead { get; set; }

        [Networked, OnChangedRender(nameof(OnMaxHealthBonusChanged))]
        private float SyncedMaxHealthBonus { get; set; }

        /// <summary>
        /// State-authority setter for MaxHealthBonus. Propagates to all proxies automatically.
        /// </summary>
        public float MaxHealthBonus
        {
            get => health != null ? health.MaxHealthBonus : SyncedMaxHealthBonus;
            set
            {
                if (!HasStateAuthority) return;
                health.MaxHealthBonus = value;
                SyncedMaxHealthBonus = value;
            }
        }

        public override void Spawned()
        {
            health = GetComponent<Health>();

            if (HasStateAuthority)
            {
                SyncedHealth = health.CurrentHealth;
                SyncedShield = health.CurrentShield;
                SyncedIsDead = health.IsDead;
                SyncedMaxHealthBonus = health.MaxHealthBonus;

                health.events.OnHealthChangeEvent.AddListener(value => SyncedHealth = value);
                health.events.OnShieldChangeEvent.AddListener(value => SyncedShield = value);
                health.events.OnDieEvent.AddListener(_ => SyncedIsDead = true);
                health.events.OnReviveEvent.AddListener(() => SyncedIsDead = false);

                health.events.OnDamageEvent.AddListener(amount => Rpc_BroadcastDamage(amount));
                health.events.OnDieEvent.AddListener(amount => Rpc_BroadcastDie(amount));
                health.events.OnOverkillEvent.AddListener(amount => Rpc_BroadcastOverkill(amount));
                health.events.OnHealHealthEvent.AddListener(amount => Rpc_BroadcastHealHealth(amount));
                health.events.OnOverhealEvent.AddListener(amount => Rpc_BroadcastOverheal(amount));
                health.events.OnChargeShieldEvent.AddListener(amount => Rpc_BroadcastChargeShield(amount));
                health.events.OnOverchargeShieldEvent.AddListener(amount => Rpc_BroadcastOverchargeShield(amount));
                health.events.OnReviveEvent.AddListener(() => Rpc_BroadcastRevive());
                health.events.OnRegenHealthStartEvent.AddListener(() => Rpc_BroadcastRegenHealthStart());
                health.events.OnRegenShieldStartEvent.AddListener(() => Rpc_BroadcastRegenShieldStart());
            }
            else
            {
                // Apply current state authority values immediately on spawn.
                health.MaxHealthBonus = SyncedMaxHealthBonus;
                health.CurrentHealth = SyncedHealth;
                health.CurrentShield = SyncedShield;

                // Fire OnDieEvent for late joiners who spawn into an already-dead state.
                // OnIsDeadChanged is empty (broadcast RPCs handle live events), so we
                // prime this initial visual state explicitly.
                if (SyncedIsDead)
                    health.events.OnDieEvent?.Invoke(0f);

                // Stop the regen coroutines Health.Start() triggered locally —
                // regen runs on state authority and replicates via SyncedHealth.
                health.StopRegen();
            }
        }

        // OnChangedRender callbacks — called on proxies when [Networked] values change.
        private void OnHealthChanged() => health.CurrentHealth = SyncedHealth;
        private void OnShieldChanged() => health.CurrentShield = SyncedShield;
        private void OnMaxHealthBonusChanged() => health.MaxHealthBonus = SyncedMaxHealthBonus;
        private void OnIsDeadChanged() { } // State sync only — events come via broadcast RPCs.

        // Broadcast RPCs — sent from state authority to proxies only, so no double-fire guard needed.
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastDamage(float amount)           => health.events.OnDamageEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastDie(float amount)              => health.events.OnDieEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverkill(float amount)         => health.events.OnOverkillEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastHealHealth(float amount)       => health.events.OnHealHealthEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverheal(float amount)         => health.events.OnOverhealEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastChargeShield(float amount)     => health.events.OnChargeShieldEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverchargeShield(float amount) => health.events.OnOverchargeShieldEvent?.Invoke(amount);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRevive()                       => health.events.OnReviveEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRegenHealthStart()             => health.events.OnRegenHealthStartEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRegenShieldStart()             => health.events.OnRegenShieldStartEvent?.Invoke();

        // RPCs — callable by any peer, executed on state authority.
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_Damage(float amount) => health.Damage(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_HealHealth(float amount) => health.HealHealth(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_ChargeShield(float amount) => health.ChargeShield(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_Revive() => health.Revive();

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SetMaxHealthBonus(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
