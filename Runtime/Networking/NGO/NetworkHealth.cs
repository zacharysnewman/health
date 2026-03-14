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
    /// </summary>
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkHealth : NetworkBehaviour, IHealthReplicator
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

        public override void OnNetworkSpawn()
        {
            health = GetComponent<Health>();

            if (IsServer)
            {
                // Seed NetworkVariables with the current state.
                netHealth.Value = health.CurrentHealth;
                netShield.Value = health.CurrentShield;
                netIsDead.Value = health.IsDead;

                // Keep NetworkVariables in sync with core Health changes.
                health.events.OnHealthChangeEvent.AddListener(value => netHealth.Value = value);
                health.events.OnShieldChangeEvent.AddListener(value => netShield.Value = value);
                health.events.OnDieEvent.AddListener(_ => netIsDead.Value = true);
                health.events.OnReviveEvent.AddListener(() => netIsDead.Value = false);
            }
            else
            {
                // Subscribe to NetworkVariable changes to update the local Health component.
                netHealth.OnValueChanged += OnNetHealthChanged;
                netShield.OnValueChanged += OnNetShieldChanged;
                netIsDead.OnValueChanged += OnNetIsDeadChanged;

                // Apply current server values immediately on spawn.
                SetHealth(netHealth.Value);
                SetShield(netShield.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsServer)
            {
                netHealth.OnValueChanged -= OnNetHealthChanged;
                netShield.OnValueChanged -= OnNetShieldChanged;
                netIsDead.OnValueChanged -= OnNetIsDeadChanged;
            }
        }

        // IHealthReplicator — writes network values back to the local Health component.
        public void SetHealth(float value) { if (health != null) health.CurrentHealth = value; }
        public void SetShield(float value) { if (health != null) health.CurrentShield = value; }

        // NetworkVariable change callbacks — invoked on clients when the server updates a value.
        private void OnNetHealthChanged(float _, float newValue) => SetHealth(newValue);
        private void OnNetShieldChanged(float _, float newValue) => SetShield(newValue);

        private void OnNetIsDeadChanged(bool _, bool newValue)
        {
            if (health == null) return;
            if (newValue)
                health.events.OnDieEvent?.Invoke(0f);
            else
                health.events.OnReviveEvent?.Invoke();
        }

        // ServerRpcs — called by clients, executed on the server.
        [ServerRpc(RequireOwnership = false)]
        public void DamageServerRpc(float amount) => health.Damage(amount);

        [ServerRpc(RequireOwnership = false)]
        public void HealHealthServerRpc(float amount) => health.HealHealth(amount);

        [ServerRpc(RequireOwnership = false)]
        public void ChargeShieldServerRpc(float amount) => health.ChargeShield(amount);

        [ServerRpc(RequireOwnership = false)]
        public void ReviveServerRpc() => health.Revive();
    }
}
#endif
