#if HEALTH_MIRROR
using UnityEngine;
using Healthy.Networking.Mirror;

/// <summary>
/// Demo input for Mirror. Sends Commands to MirrorHealth instead of
/// calling Health methods directly, so all actions are server-authoritative.
/// Attach this to the same GameObject as Health and MirrorHealth.
/// </summary>
public class MirrorDemoInput : MonoBehaviour
{
    private MirrorHealth mirrorHealth;

    private void Awake()
    {
        mirrorHealth = GetComponent<MirrorHealth>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) mirrorHealth.CmdDamage(25);
        if (Input.GetKeyDown(KeyCode.H)) mirrorHealth.CmdHealHealth(25);
        if (Input.GetKeyDown(KeyCode.S)) mirrorHealth.CmdChargeShield(25);
        if (Input.GetKeyDown(KeyCode.R)) mirrorHealth.CmdRevive();
    }
}
#endif
