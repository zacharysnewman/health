#if HEALTH_MIRROR
using Mirror;
using UnityEngine;

namespace Healthy.Networking.Mirror
{
    /// <summary>
    /// Mirror networking adapter for the Health component.
    ///
    /// Place this alongside a Health component on a networked GameObject.
    /// The server is authoritative: clients send Commands and the server applies
    /// them to the core Health component. SyncVar hooks replicate state back to
    /// all clients so their local Health values stay in sync.
    ///
    /// Setup:
    ///   1. Install Mirror and add HEALTH_MIRROR to Project Settings > Player > Scripting Define Symbols.
    ///   2. Add Health, MirrorHealth, and NetworkIdentity to your networked GameObject.
    ///   3. From client code, call CmdDamage / CmdHealHealth / CmdChargeShield / CmdRevive
    ///      instead of calling Health methods directly.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class MirrorHealth : NetworkBehaviour, IHealthReplicator
    {
        private Health health;

        [SyncVar(hook = nameof(OnSyncedHealthChanged))]
        private float syncedHealth;

        [SyncVar(hook = nameof(OnSyncedShieldChanged))]
        private float syncedShield;

        [SyncVar(hook = nameof(OnSyncedIsDeadChanged))]
        private bool syncedIsDead;

        public override void OnStartServer()
        {
            health = GetComponent<Health>();

            // Seed SyncVars with the current state so joining clients receive correct values.
            syncedHealth = health.CurrentHealth;
            syncedShield = health.CurrentShield;
            syncedIsDead = health.IsDead;

            // Mirror core Health state changes to SyncVars so clients stay in sync.
            health.events.OnHealthChangeEvent.AddListener(value => syncedHealth = value);
            health.events.OnShieldChangeEvent.AddListener(value => syncedShield = value);
            health.events.OnDieEvent.AddListener(_ => syncedIsDead = true);
            health.events.OnReviveEvent.AddListener(() => syncedIsDead = false);
        }

        public override void OnStartClient()
        {
            if (isServer) return;
            health = GetComponent<Health>();
        }

        // IHealthReplicator — writes network values back to the local Health component.
        public void SetHealth(float value) { if (health != null) health.CurrentHealth = value; }
        public void SetShield(float value) { if (health != null) health.CurrentShield = value; }

        // SyncVar hooks — called on clients when the server changes the value.
        private void OnSyncedHealthChanged(float _, float newValue) => SetHealth(newValue);
        private void OnSyncedShieldChanged(float _, float newValue) => SetShield(newValue);

        private void OnSyncedIsDeadChanged(bool _, bool newValue)
        {
            if (health == null) return;
            if (newValue)
                health.events.OnDieEvent?.Invoke(0f);
            else
                health.events.OnReviveEvent?.Invoke();
        }

        // Commands — called by clients, executed on the server.
        [Command]
        public void CmdDamage(float amount) => health.Damage(amount);

        [Command]
        public void CmdHealHealth(float amount) => health.HealHealth(amount);

        [Command]
        public void CmdChargeShield(float amount) => health.ChargeShield(amount);

        [Command]
        public void CmdRevive() => health.Revive();
    }
}
#endif
