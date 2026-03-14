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

                health.events.OnHealthChangeEvent.AddListener(OnAuthorityHealthChanged);
                health.events.OnShieldChangeEvent.AddListener(OnAuthorityShieldChanged);
                health.events.OnDieEvent.AddListener(OnAuthorityDied);
                health.events.OnReviveEvent.AddListener(OnAuthorityRevived);

                health.events.OnDamageEvent.AddListener(OnAuthorityDamage);
                health.events.OnDieEvent.AddListener(OnAuthorityDie);
                health.events.OnOverkillEvent.AddListener(OnAuthorityOverkill);
                health.events.OnHealHealthEvent.AddListener(OnAuthorityHealHealth);
                health.events.OnOverhealEvent.AddListener(OnAuthorityOverheal);
                health.events.OnChargeShieldEvent.AddListener(OnAuthorityChargeShield);
                health.events.OnOverchargeShieldEvent.AddListener(OnAuthorityOverchargeShield);
                health.events.OnReviveEvent.AddListener(OnAuthorityRevive);
                health.events.OnRegenHealthStartEvent.AddListener(OnAuthorityRegenHealthStart);
                health.events.OnRegenShieldStartEvent.AddListener(OnAuthorityRegenShieldStart);
                health.events.OnMaxHealthEvent.AddListener(OnAuthorityMaxHealth);
                health.events.OnMaxShieldEvent.AddListener(OnAuthorityMaxShield);
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

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (!HasStateAuthority || health == null) return;

            health.events.OnHealthChangeEvent.RemoveListener(OnAuthorityHealthChanged);
            health.events.OnShieldChangeEvent.RemoveListener(OnAuthorityShieldChanged);
            health.events.OnDieEvent.RemoveListener(OnAuthorityDied);
            health.events.OnReviveEvent.RemoveListener(OnAuthorityRevived);

            health.events.OnDamageEvent.RemoveListener(OnAuthorityDamage);
            health.events.OnDieEvent.RemoveListener(OnAuthorityDie);
            health.events.OnOverkillEvent.RemoveListener(OnAuthorityOverkill);
            health.events.OnHealHealthEvent.RemoveListener(OnAuthorityHealHealth);
            health.events.OnOverhealEvent.RemoveListener(OnAuthorityOverheal);
            health.events.OnChargeShieldEvent.RemoveListener(OnAuthorityChargeShield);
            health.events.OnOverchargeShieldEvent.RemoveListener(OnAuthorityOverchargeShield);
            health.events.OnReviveEvent.RemoveListener(OnAuthorityRevive);
            health.events.OnRegenHealthStartEvent.RemoveListener(OnAuthorityRegenHealthStart);
            health.events.OnRegenShieldStartEvent.RemoveListener(OnAuthorityRegenShieldStart);
            health.events.OnMaxHealthEvent.RemoveListener(OnAuthorityMaxHealth);
            health.events.OnMaxShieldEvent.RemoveListener(OnAuthorityMaxShield);
        }

        // OnChangedRender callbacks — called on proxies when [Networked] values change.
        private void OnHealthChanged()      => health.CurrentHealth = SyncedHealth;
        private void OnShieldChanged()      => health.CurrentShield = SyncedShield;
        private void OnMaxHealthBonusChanged() => health.MaxHealthBonus = SyncedMaxHealthBonus;
        private void OnIsDeadChanged()      { } // State sync only — events come via broadcast RPCs.

        // Networked property forwarding (state authority).
        private void OnAuthorityHealthChanged(float v) => SyncedHealth = v;
        private void OnAuthorityShieldChanged(float v) => SyncedShield = v;
        private void OnAuthorityDied(float _)          => SyncedIsDead = true;
        private void OnAuthorityRevived()              => SyncedIsDead = false;

        // Broadcast forwarding (state authority → proxies).
        private void OnAuthorityDamage(float a)           => Rpc_BroadcastDamage(a);
        private void OnAuthorityDie(float a)              => Rpc_BroadcastDie(a);
        private void OnAuthorityOverkill(float a)         => Rpc_BroadcastOverkill(a);
        private void OnAuthorityHealHealth(float a)       => Rpc_BroadcastHealHealth(a);
        private void OnAuthorityOverheal(float a)         => Rpc_BroadcastOverheal(a);
        private void OnAuthorityChargeShield(float a)     => Rpc_BroadcastChargeShield(a);
        private void OnAuthorityOverchargeShield(float a) => Rpc_BroadcastOverchargeShield(a);
        private void OnAuthorityRevive()                  => Rpc_BroadcastRevive();
        private void OnAuthorityRegenHealthStart()        => Rpc_BroadcastRegenHealthStart();
        private void OnAuthorityRegenShieldStart()        => Rpc_BroadcastRegenShieldStart();
        private void OnAuthorityMaxHealth()               => Rpc_BroadcastMaxHealth();
        private void OnAuthorityMaxShield()               => Rpc_BroadcastMaxShield();

        // Broadcast RPCs — sent from state authority to proxies only, so no double-fire guard needed.
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastDamage(float a)           => health.events.OnDamageEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastDie(float a)              => health.events.OnDieEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverkill(float a)         => health.events.OnOverkillEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastHealHealth(float a)       => health.events.OnHealHealthEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverheal(float a)         => health.events.OnOverhealEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastChargeShield(float a)     => health.events.OnChargeShieldEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastOverchargeShield(float a) => health.events.OnOverchargeShieldEvent?.Invoke(a);
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRevive()                  => health.events.OnReviveEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRegenHealthStart()        => health.events.OnRegenHealthStartEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastRegenShieldStart()        => health.events.OnRegenShieldStartEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastMaxHealth()               => health.events.OnMaxHealthEvent?.Invoke();
        [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)] private void Rpc_BroadcastMaxShield()               => health.events.OnMaxShieldEvent?.Invoke();

        // RPCs — callable by any peer, executed on state authority.
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void Rpc_Damage(float amount)           => health.Damage(amount);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void Rpc_HealHealth(float amount)       => health.HealHealth(amount);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void Rpc_ChargeShield(float amount)     => health.ChargeShield(amount);
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void Rpc_Revive()                       => health.Revive();
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void Rpc_SetMaxHealthBonus(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
