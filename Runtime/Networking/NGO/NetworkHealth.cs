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

                health.events.OnHealthChangeEvent.AddListener(value => netHealth.Value = value);
                health.events.OnShieldChangeEvent.AddListener(value => netShield.Value = value);
                health.events.OnDieEvent.AddListener(_ => netIsDead.Value = true);
                health.events.OnReviveEvent.AddListener(() => netIsDead.Value = false);
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

                // Stop the regen coroutines Health.Start() triggered locally —
                // regen runs on the server and replicates via netHealth.
                health.StopRegen();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                netHealth.OnValueChanged -= OnNetHealthChanged;
                netShield.OnValueChanged -= OnNetShieldChanged;
                netIsDead.OnValueChanged -= OnNetIsDeadChanged;
                netMaxHealthBonus.OnValueChanged -= OnNetMaxHealthBonusChanged;
            }
        }

        private void OnNetHealthChanged(float _, float newValue) => health.CurrentHealth = newValue;
        private void OnNetShieldChanged(float _, float newValue) => health.CurrentShield = newValue;
        private void OnNetMaxHealthBonusChanged(float _, float newValue) => health.MaxHealthBonus = newValue;

        private void OnNetIsDeadChanged(bool _, bool newValue)
        {
            if (health == null) return;
            if (newValue)
                health.events.OnDieEvent?.Invoke(0f);
            else
                health.events.OnReviveEvent?.Invoke();
        }

        // ServerRpcs — called by any client, executed on the server.
        [ServerRpc(RequireOwnership = false)]
        public void DamageServerRpc(float amount) => health.Damage(amount);

        [ServerRpc(RequireOwnership = false)]
        public void HealHealthServerRpc(float amount) => health.HealHealth(amount);

        [ServerRpc(RequireOwnership = false)]
        public void ChargeShieldServerRpc(float amount) => health.ChargeShield(amount);

        [ServerRpc(RequireOwnership = false)]
        public void ReviveServerRpc() => health.Revive();

        [ServerRpc(RequireOwnership = false)]
        public void SetMaxHealthBonusServerRpc(float bonus) => MaxHealthBonus = bonus;
    }
}
#endif
