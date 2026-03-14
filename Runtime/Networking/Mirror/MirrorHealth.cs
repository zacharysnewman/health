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
    /// Semantic events (OnDamageEvent, OnDieEvent, OnHealHealthEvent, etc.) are
    /// broadcast to all clients via ClientRpcs so effects like floating damage
    /// numbers, death animations, and shield sounds work on remote players.
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

            health.events.OnDamageEvent.AddListener(amount => RpcBroadcastDamage(amount));
            health.events.OnDieEvent.AddListener(amount => RpcBroadcastDie(amount));
            health.events.OnOverkillEvent.AddListener(amount => RpcBroadcastOverkill(amount));
            health.events.OnHealHealthEvent.AddListener(amount => RpcBroadcastHealHealth(amount));
            health.events.OnOverhealEvent.AddListener(amount => RpcBroadcastOverheal(amount));
            health.events.OnChargeShieldEvent.AddListener(amount => RpcBroadcastChargeShield(amount));
            health.events.OnOverchargeShieldEvent.AddListener(amount => RpcBroadcastOverchargeShield(amount));
            health.events.OnReviveEvent.AddListener(() => RpcBroadcastRevive());
            health.events.OnRegenHealthStartEvent.AddListener(() => RpcBroadcastRegenHealthStart());
            health.events.OnRegenShieldStartEvent.AddListener(() => RpcBroadcastRegenShieldStart());
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
        private void OnSyncedIsDeadChanged(bool _, bool newValue) { } // State sync only — events come via ClientRpcs.

        // ClientRpcs — broadcast semantic events to all non-server clients.
        // isServer guard prevents double-firing on the host, which already ran the event locally.
        [ClientRpc] private void RpcBroadcastDamage(float amount)           { if (isServer) return; health.events.OnDamageEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastDie(float amount)              { if (isServer) return; health.events.OnDieEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastOverkill(float amount)         { if (isServer) return; health.events.OnOverkillEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastHealHealth(float amount)       { if (isServer) return; health.events.OnHealHealthEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastOverheal(float amount)         { if (isServer) return; health.events.OnOverhealEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastChargeShield(float amount)     { if (isServer) return; health.events.OnChargeShieldEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastOverchargeShield(float amount) { if (isServer) return; health.events.OnOverchargeShieldEvent?.Invoke(amount); }
        [ClientRpc] private void RpcBroadcastRevive()                       { if (isServer) return; health.events.OnReviveEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastRegenHealthStart()             { if (isServer) return; health.events.OnRegenHealthStartEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastRegenShieldStart()             { if (isServer) return; health.events.OnRegenShieldStartEvent?.Invoke(); }

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
