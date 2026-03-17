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

            health.events.OnHealthChangeEvent.AddListener(OnServerHealthChanged);
            health.events.OnShieldChangeEvent.AddListener(OnServerShieldChanged);
            health.events.OnDieEvent.AddListener(OnServerDied);
            health.events.OnReviveEvent.AddListener(OnServerRevived);

            health.events.OnDamageEvent.AddListener(OnServerDamage);
            health.events.OnDieEvent.AddListener(OnServerDie);
            health.events.OnOverkillEvent.AddListener(OnServerOverkill);
            health.events.OnHealHealthEvent.AddListener(OnServerHealHealth);
            health.events.OnOverhealEvent.AddListener(OnServerOverheal);
            health.events.OnChargeShieldEvent.AddListener(OnServerChargeShield);
            health.events.OnOverchargeShieldEvent.AddListener(OnServerOverchargeShield);
            health.events.OnReviveEvent.AddListener(OnServerRevive);
            health.events.OnRegenHealthStartEvent.AddListener(OnServerRegenHealthStart);
            health.events.OnRegenShieldStartEvent.AddListener(OnServerRegenShieldStart);
            health.events.OnMaxHealthEvent.AddListener(OnServerMaxHealth);
            health.events.OnMaxShieldEvent.AddListener(OnServerMaxShield);
        }

        public override void OnStopServer()
        {
            health.events.OnHealthChangeEvent.RemoveListener(OnServerHealthChanged);
            health.events.OnShieldChangeEvent.RemoveListener(OnServerShieldChanged);
            health.events.OnDieEvent.RemoveListener(OnServerDied);
            health.events.OnReviveEvent.RemoveListener(OnServerRevived);

            health.events.OnDamageEvent.RemoveListener(OnServerDamage);
            health.events.OnDieEvent.RemoveListener(OnServerDie);
            health.events.OnOverkillEvent.RemoveListener(OnServerOverkill);
            health.events.OnHealHealthEvent.RemoveListener(OnServerHealHealth);
            health.events.OnOverhealEvent.RemoveListener(OnServerOverheal);
            health.events.OnChargeShieldEvent.RemoveListener(OnServerChargeShield);
            health.events.OnOverchargeShieldEvent.RemoveListener(OnServerOverchargeShield);
            health.events.OnReviveEvent.RemoveListener(OnServerRevive);
            health.events.OnRegenHealthStartEvent.RemoveListener(OnServerRegenHealthStart);
            health.events.OnRegenShieldStartEvent.RemoveListener(OnServerRegenShieldStart);
            health.events.OnMaxHealthEvent.RemoveListener(OnServerMaxHealth);
            health.events.OnMaxShieldEvent.RemoveListener(OnServerMaxShield);
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

            // Fire OnDieEvent for late joiners who spawn into an already-dead state.
            // OnSyncedIsDeadChanged is empty (ClientRpcs handle live events), so we
            // prime this initial visual state explicitly.
            if (syncedIsDead)
                health.events.OnDieEvent?.Invoke(0f);

            // Stop the regen coroutines Health.Start() triggered locally —
            // regen runs on the server and replicates via syncedHealth.
            health.StopRegen();
        }

        // SyncVar hooks — called on clients when the server changes the value.
        private void OnSyncedHealthChanged(float _, float v)      => health.CurrentHealth = v;
        private void OnSyncedShieldChanged(float _, float v)      => health.CurrentShield = v;
        private void OnSyncedMaxHealthBonusChanged(float _, float v) => health.MaxHealthBonus = v;
        private void OnSyncedIsDeadChanged(bool _, bool newValue) { } // State sync only — events come via ClientRpcs.

        // SyncVar forwarding (server).
        private void OnServerHealthChanged(float v) => syncedHealth = v;
        private void OnServerShieldChanged(float v) => syncedShield = v;
        private void OnServerDied(float _)          => syncedIsDead = true;
        private void OnServerRevived()              => syncedIsDead = false;

        // Broadcast forwarding (server → ClientRpc).
        private void OnServerDamage(float a)           => RpcBroadcastDamage(a);
        private void OnServerDie(float a)              => RpcBroadcastDie(a);
        private void OnServerOverkill(float a)         => RpcBroadcastOverkill(a);
        private void OnServerHealHealth(float a)       => RpcBroadcastHealHealth(a);
        private void OnServerOverheal(float a)         => RpcBroadcastOverheal(a);
        private void OnServerChargeShield(float a)     => RpcBroadcastChargeShield(a);
        private void OnServerOverchargeShield(float a) => RpcBroadcastOverchargeShield(a);
        private void OnServerRevive()                  => RpcBroadcastRevive();
        private void OnServerRegenHealthStart()        => RpcBroadcastRegenHealthStart();
        private void OnServerRegenShieldStart()        => RpcBroadcastRegenShieldStart();
        private void OnServerMaxHealth()               => RpcBroadcastMaxHealth();
        private void OnServerMaxShield()               => RpcBroadcastMaxShield();

        // ClientRpcs — broadcast semantic events to all non-server clients.
        // isServer guard prevents double-firing on the host, which already ran the event locally.
        [ClientRpc] private void RpcBroadcastDamage(float a)           { if (isServer) return; health.events.OnDamageEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastDie(float a)              { if (isServer) return; health.events.OnDieEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastOverkill(float a)         { if (isServer) return; health.events.OnOverkillEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastHealHealth(float a)       { if (isServer) return; health.events.OnHealHealthEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastOverheal(float a)         { if (isServer) return; health.events.OnOverhealEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastChargeShield(float a)     { if (isServer) return; health.events.OnChargeShieldEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastOverchargeShield(float a) { if (isServer) return; health.events.OnOverchargeShieldEvent?.Invoke(a); }
        [ClientRpc] private void RpcBroadcastRevive()                  { if (isServer) return; health.events.OnReviveEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastRegenHealthStart()        { if (isServer) return; health.events.OnRegenHealthStartEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastRegenShieldStart()        { if (isServer) return; health.events.OnRegenShieldStartEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastMaxHealth()               { if (isServer) return; health.events.OnMaxHealthEvent?.Invoke(); }
        [ClientRpc] private void RpcBroadcastMaxShield()               { if (isServer) return; health.events.OnMaxShieldEvent?.Invoke(); }

        // Commands — called by any client, executed on the server.
        [Command(requiresAuthority = false)] public void CmdDamage(float amount)           => health.TakeDamage(new DamageInfo { Amount = amount });
        [Command(requiresAuthority = false)] public void CmdHealHealth(float amount)       => health.HealHealth(amount);
        [Command(requiresAuthority = false)] public void CmdChargeShield(float amount)     => health.ChargeShield(amount);
        [Command(requiresAuthority = false)] public void CmdRevive()                       => health.Revive();
        [Command(requiresAuthority = false)] public void CmdSetMaxHealthBonus(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
