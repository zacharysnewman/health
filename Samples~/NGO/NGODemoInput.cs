#if HEALTH_NGO
using UnityEngine;
using Healthy.Networking.NGO;

/// <summary>
/// Demo input for NGO. Calls ServerRpcs on NetworkHealth instead of
/// calling Health methods directly, so all actions are server-authoritative.
/// Attach this to the same GameObject as Health and NetworkHealth.
/// </summary>
public class NGODemoInput : MonoBehaviour
{
    private NetworkHealth networkHealth;

    private void Awake()
    {
        networkHealth = GetComponent<NetworkHealth>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) networkHealth.DamageServerRpc(25);
        if (Input.GetKeyDown(KeyCode.H)) networkHealth.HealHealthServerRpc(25);
        if (Input.GetKeyDown(KeyCode.S)) networkHealth.ChargeShieldServerRpc(25);
        if (Input.GetKeyDown(KeyCode.R)) networkHealth.ReviveServerRpc();
    }
}
#endif
