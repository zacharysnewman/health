#if HEALTH_FUSION
using UnityEngine;
using Healthy.Networking.Fusion;

/// <summary>
/// Demo input for Photon Fusion. Sends RPCs to FusionHealth instead of
/// calling Health methods directly, so all actions are state-authority-authoritative.
/// Attach this to the same GameObject as Health and FusionHealth.
/// </summary>
public class FusionDemoInput : MonoBehaviour
{
    private FusionHealth fusionHealth;

    private void Awake()
    {
        fusionHealth = GetComponent<FusionHealth>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) fusionHealth.Rpc_Damage(25);
        if (Input.GetKeyDown(KeyCode.H)) fusionHealth.Rpc_HealHealth(25);
        if (Input.GetKeyDown(KeyCode.S)) fusionHealth.Rpc_ChargeShield(25);
        if (Input.GetKeyDown(KeyCode.R)) fusionHealth.Rpc_Revive();
    }
}
#endif
