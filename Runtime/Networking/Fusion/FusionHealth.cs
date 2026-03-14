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
    /// Setup:
    ///   1. Install Photon Fusion and add HEALTH_FUSION to Project Settings > Player > Scripting Define Symbols.
    ///   2. Add Health and FusionHealth to your networked GameObject.
    ///   3. Spawn the object via NetworkRunner.Spawn (required for [Networked] to work).
    ///   4. From non-authority peers, call Rpc_Damage / Rpc_HealHealth /
    ///      Rpc_ChargeShield / Rpc_Revive instead of calling Health methods directly.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class FusionHealth : NetworkBehaviour, IHealthReplicator
    {
        private Health health;

        [Networked, OnChangedRender(nameof(OnHealthChanged))]
        private float SyncedHealth { get; set; }

        [Networked, OnChangedRender(nameof(OnShieldChanged))]
        private float SyncedShield { get; set; }

        [Networked, OnChangedRender(nameof(OnIsDeadChanged))]
        private NetworkBool SyncedIsDead { get; set; }

        public override void Spawned()
        {
            health = GetComponent<Health>();

            if (HasStateAuthority)
            {
                // Seed [Networked] properties with the current state.
                SyncedHealth = health.CurrentHealth;
                SyncedShield = health.CurrentShield;
                SyncedIsDead = health.IsDead;

                // Keep [Networked] properties in sync with core Health changes.
                health.events.OnHealthChangeEvent.AddListener(value => SyncedHealth = value);
                health.events.OnShieldChangeEvent.AddListener(value => SyncedShield = value);
                health.events.OnDieEvent.AddListener(_ => SyncedIsDead = true);
                health.events.OnReviveEvent.AddListener(() => SyncedIsDead = false);
            }
        }

        // IHealthReplicator — writes network values back to the local Health component.
        public void SetHealth(float value) { if (health != null) health.CurrentHealth = value; }
        public void SetShield(float value) { if (health != null) health.CurrentShield = value; }

        // OnChangedRender callbacks — called on proxies when [Networked] values change.
        private void OnHealthChanged() => SetHealth(SyncedHealth);
        private void OnShieldChanged() => SetShield(SyncedShield);

        private void OnIsDeadChanged()
        {
            if (health == null) return;
            if (SyncedIsDead)
                health.events.OnDieEvent?.Invoke(0f);
            else
                health.events.OnReviveEvent?.Invoke();
        }

        // RPCs — callable by any peer, executed on state authority.
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_Damage(float amount) => health.Damage(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_HealHealth(float amount) => health.HealHealth(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_ChargeShield(float amount) => health.ChargeShield(amount);

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_Revive() => health.Revive();
    }
}
#endif
