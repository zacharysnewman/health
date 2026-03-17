#if HEALTH_NGO
using Unity.Netcode;
using UnityEngine;

namespace Healthy.Networking.NGO
{
    /// <summary>
    /// Netcode for GameObjects (NGO) adapter for the Health component.
    ///
    /// Place this alongside a Health component on a networked GameObject.
    /// The server is authoritative: clients call ServerRpcs and the server applies
    /// them to the core Health component. NetworkVariables replicate state to all
    /// clients so their local Health values stay in sync.
    ///
    /// Semantic events (OnDamageEvent, OnDieEvent, OnHealHealthEvent, etc.) are
    /// broadcast to all clients via ClientRpcs so effects like floating damage
    /// numbers, death animations, and shield sounds work on remote players.
    ///
    /// Setup:
    ///   1. Install com.unity.netcode.gameobjects — HEALTH_NGO is defined automatically.
    ///   2. Add Health, NetworkHealth, and NetworkObject to your networked GameObject.
    ///   3. From client code, call DamageServerRpc / HealHealthServerRpc /
    ///      ChargeShieldServerRpc / ReviveServerRpc instead of calling Health methods directly.
    ///   4. To change MaxHealthBonus at runtime (e.g. on level-up), call
    ///      SetMaxHealthBonusServerRpc from a client, or set MaxHealthBonus directly
    ///      from server code.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkHealth : NetworkBehaviour
    {
        private Health health;

        private readonly NetworkVariable<float> netHealth = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> netShield = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> netIsDead = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<float> netMaxHealthBonus = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>
        /// Server-side setter for MaxHealthBonus. Propagates to all clients automatically.
        /// </summary>
        public float MaxHealthBonus
        {
            get => health != null ? health.MaxHealthBonus : netMaxHealthBonus.Value;
            set
            {
                if (!IsServer) return;
                health.MaxHealthBonus = value;
                netMaxHealthBonus.Value = value;
            }
        }

        public override void OnNetworkSpawn()
        {
            health = GetComponent<Health>();

            if (IsServer)
            {
                netHealth.Value = health.CurrentHealth;
                netShield.Value = health.CurrentShield;
                netIsDead.Value = health.IsDead;
                netMaxHealthBonus.Value = health.MaxHealthBonus;

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
            else
            {
                netHealth.OnValueChanged += OnNetHealthChanged;
                netShield.OnValueChanged += OnNetShieldChanged;
                netIsDead.OnValueChanged += OnNetIsDeadChanged;
                netMaxHealthBonus.OnValueChanged += OnNetMaxHealthBonusChanged;

                // Apply current server state immediately on spawn.
                health.MaxHealthBonus = netMaxHealthBonus.Value;
                health.CurrentHealth = netHealth.Value;
                health.CurrentShield = netShield.Value;

                // Fire OnDieEvent for late joiners who spawn into an already-dead state.
                // OnNetIsDeadChanged is empty (RPCs handle live events), so we prime this
                // initial visual state explicitly.
                if (netIsDead.Value)
                    health.events.OnDieEvent?.Invoke(0f);

                // Stop the regen coroutines Health.Start() triggered locally —
                // regen runs on the server and replicates via netHealth.
                health.StopRegen();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
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
            else
            {
                netHealth.OnValueChanged -= OnNetHealthChanged;
                netShield.OnValueChanged -= OnNetShieldChanged;
                netIsDead.OnValueChanged -= OnNetIsDeadChanged;
                netMaxHealthBonus.OnValueChanged -= OnNetMaxHealthBonusChanged;
            }
        }

        // NetworkVariable callbacks (clients).
        private void OnNetHealthChanged(float _, float newValue)      => health.CurrentHealth = newValue;
        private void OnNetShieldChanged(float _, float newValue)      => health.CurrentShield = newValue;
        private void OnNetMaxHealthBonusChanged(float _, float v)     => health.MaxHealthBonus = v;
        private void OnNetIsDeadChanged(bool _, bool newValue)        { } // State sync only — events come via ClientRpcs.

        // NetworkVariable forwarding (server).
        private void OnServerHealthChanged(float v) => netHealth.Value = v;
        private void OnServerShieldChanged(float v) => netShield.Value = v;
        private void OnServerDied(float _)          => netIsDead.Value = true;
        private void OnServerRevived()              => netIsDead.Value = false;

        // Broadcast forwarding (server → ClientRpc).
        private void OnServerDamage(float a)           => BroadcastDamageClientRpc(a);
        private void OnServerDie(float a)              => BroadcastDieClientRpc(a);
        private void OnServerOverkill(float a)         => BroadcastOverkillClientRpc(a);
        private void OnServerHealHealth(float a)       => BroadcastHealHealthClientRpc(a);
        private void OnServerOverheal(float a)         => BroadcastOverhealClientRpc(a);
        private void OnServerChargeShield(float a)     => BroadcastChargeShieldClientRpc(a);
        private void OnServerOverchargeShield(float a) => BroadcastOverchargeShieldClientRpc(a);
        private void OnServerRevive()                  => BroadcastReviveClientRpc();
        private void OnServerRegenHealthStart()        => BroadcastRegenHealthStartClientRpc();
        private void OnServerRegenShieldStart()        => BroadcastRegenShieldStartClientRpc();
        private void OnServerMaxHealth()               => BroadcastMaxHealthClientRpc();
        private void OnServerMaxShield()               => BroadcastMaxShieldClientRpc();

        // ClientRpcs — broadcast semantic events to all non-server clients.
        // IsServer guard prevents double-firing on the host, which already ran the event locally.
        [ClientRpc] private void BroadcastDamageClientRpc(float a)           { if (IsServer) return; health.events.OnDamageEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastDieClientRpc(float a)              { if (IsServer) return; health.events.OnDieEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastOverkillClientRpc(float a)         { if (IsServer) return; health.events.OnOverkillEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastHealHealthClientRpc(float a)       { if (IsServer) return; health.events.OnHealHealthEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastOverhealClientRpc(float a)         { if (IsServer) return; health.events.OnOverhealEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastChargeShieldClientRpc(float a)     { if (IsServer) return; health.events.OnChargeShieldEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastOverchargeShieldClientRpc(float a) { if (IsServer) return; health.events.OnOverchargeShieldEvent?.Invoke(a); }
        [ClientRpc] private void BroadcastReviveClientRpc()                  { if (IsServer) return; health.events.OnReviveEvent?.Invoke(); }
        [ClientRpc] private void BroadcastRegenHealthStartClientRpc()        { if (IsServer) return; health.events.OnRegenHealthStartEvent?.Invoke(); }
        [ClientRpc] private void BroadcastRegenShieldStartClientRpc()        { if (IsServer) return; health.events.OnRegenShieldStartEvent?.Invoke(); }
        [ClientRpc] private void BroadcastMaxHealthClientRpc()               { if (IsServer) return; health.events.OnMaxHealthEvent?.Invoke(); }
        [ClientRpc] private void BroadcastMaxShieldClientRpc()               { if (IsServer) return; health.events.OnMaxShieldEvent?.Invoke(); }

        // ServerRpcs — called by any client, executed on the server.
        [ServerRpc(RequireOwnership = false)] public void DamageServerRpc(float amount)          => health.TakeDamage(new DamageInfo { Amount = amount });
        [ServerRpc(RequireOwnership = false)] public void HealHealthServerRpc(float amount)      => health.HealHealth(amount);
        [ServerRpc(RequireOwnership = false)] public void ChargeShieldServerRpc(float amount)    => health.ChargeShield(amount);
        [ServerRpc(RequireOwnership = false)] public void ReviveServerRpc()                      => health.Revive();
        [ServerRpc(RequireOwnership = false)] public void SetMaxHealthBonusServerRpc(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
