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
    ///   4. To change MaxHealthBonus at runtime (e.g. on level-up), call
    ///      CmdSetMaxHealthBonus from a client, or set MaxHealthBonus directly from server code.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NetworkIdentity))]
    public class MirrorHealth : NetworkBehaviour
    {
        private Health health;

        [SyncVar(hook = nameof(OnSyncedHealthChanged))]
        private float syncedHealth;

        [SyncVar(hook = nameof(OnSyncedShieldChanged))]
        private float syncedShield;

        [SyncVar(hook = nameof(OnSyncedIsDeadChanged))]
        private bool syncedIsDead;

        [SyncVar(hook = nameof(OnSyncedMaxHealthBonusChanged))]
        private float syncedMaxHealthBonus;

        /// <summary>
        /// Server-side setter for MaxHealthBonus. Propagates to all clients automatically.
        /// </summary>
        public float MaxHealthBonus
        {
            get => health != null ? health.MaxHealthBonus : syncedMaxHealthBonus;
            set
            {
                if (!isServer) return;
                health.MaxHealthBonus = value;
                syncedMaxHealthBonus = value;
            }
        }

        public override void OnStartServer()
        {
            health = GetComponent<Health>();

            syncedHealth = health.CurrentHealth;
            syncedShield = health.CurrentShield;
            syncedIsDead = health.IsDead;
            syncedMaxHealthBonus = health.MaxHealthBonus;

            health.events.OnHealthChangeEvent.AddListener(value => syncedHealth = value);
            health.events.OnShieldChangeEvent.AddListener(value => syncedShield = value);
            health.events.OnDieEvent.AddListener(_ => syncedIsDead = true);
            health.events.OnReviveEvent.AddListener(() => syncedIsDead = false);
        }

        public override void OnStartClient()
        {
            if (isServer) return;
            health = GetComponent<Health>();

            // Apply current server state. SyncVar hooks do not fire for the initial
            // value when a client joins, so we apply them explicitly here.
            health.MaxHealthBonus = syncedMaxHealthBonus;
            health.CurrentHealth = syncedHealth;
            health.CurrentShield = syncedShield;

            // Stop the regen coroutines Health.Start() triggered locally —
            // regen runs on the server and replicates via syncedHealth.
            health.StopRegen();
        }

        // SyncVar hooks — called on clients when the server changes the value.
        private void OnSyncedHealthChanged(float _, float newValue) => health.CurrentHealth = newValue;
        private void OnSyncedShieldChanged(float _, float newValue) => health.CurrentShield = newValue;
        private void OnSyncedMaxHealthBonusChanged(float _, float newValue) => health.MaxHealthBonus = newValue;

        private void OnSyncedIsDeadChanged(bool _, bool newValue)
        {
            if (health == null) return;
            if (newValue)
                health.events.OnDieEvent?.Invoke(0f);
            else
                health.events.OnReviveEvent?.Invoke();
        }

        // Commands — called by any client, executed on the server.
        [Command(requiresAuthority = false)]
        public void CmdDamage(float amount) => health.Damage(amount);

        [Command(requiresAuthority = false)]
        public void CmdHealHealth(float amount) => health.HealHealth(amount);

        [Command(requiresAuthority = false)]
        public void CmdChargeShield(float amount) => health.ChargeShield(amount);

        [Command(requiresAuthority = false)]
        public void CmdRevive() => health.Revive();

        [Command(requiresAuthority = false)]
        public void CmdSetMaxHealthBonus(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
