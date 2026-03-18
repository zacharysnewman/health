using UnityEngine;

namespace Healthy
{
    /// <summary>
    /// FX adapter for the Health component.
    ///
    /// Drop this alongside a Health component to drive audio and visual effects from
    /// health events without writing any code. Assign prefabs in the inspector — one slot
    /// per semantic event for SFX, one slot per semantic event for VFX.
    ///
    /// SFX prefabs should carry a configured AudioSource (Play On Awake = true) so that
    /// 3D spatial settings (blend, rolloff, doppler, reverb zone mix) are fully controlled
    /// in the prefab rather than in code.
    ///
    /// VFX prefabs can carry a ParticleSystem, Animator, or any combination.
    ///
    /// Both SFX and VFX can be independently toggled at runtime via sfxEnabled / vfxEnabled.
    /// Disable this component entirely to suppress all FX without clearing prefab slots.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HealthFXAdapter : MonoBehaviour
    {
        [Header("SFX")]
        public bool sfxEnabled = true;

        [Header("SFX — Damage")]
        public GameObject sfx_onDamage;
        public GameObject sfx_onDie;
        public GameObject sfx_onOverkill;

        [Header("SFX — Health")]
        public GameObject sfx_onHealHealth;
        public GameObject sfx_onOverheal;
        public GameObject sfx_onMaxHealth;
        public GameObject sfx_onRevive;
        public GameObject sfx_onRegenHealthStart;

        [Header("SFX — Shield")]
        public GameObject sfx_onChargeShield;
        public GameObject sfx_onOverchargeShield;
        public GameObject sfx_onMaxShield;
        public GameObject sfx_onRegenShieldStart;

        [Header("VFX")]
        public bool vfxEnabled = true;

        [Header("VFX — Damage")]
        public GameObject vfx_onDamage;
        public GameObject vfx_onDie;
        public GameObject vfx_onOverkill;

        [Header("VFX — Health")]
        public GameObject vfx_onHealHealth;
        public GameObject vfx_onOverheal;
        public GameObject vfx_onMaxHealth;
        public GameObject vfx_onRevive;
        public GameObject vfx_onRegenHealthStart;

        [Header("VFX — Shield")]
        public GameObject vfx_onChargeShield;
        public GameObject vfx_onOverchargeShield;
        public GameObject vfx_onMaxShield;
        public GameObject vfx_onRegenShieldStart;

        private Health health;

        private void Awake() => health = GetComponent<Health>();

        private void OnEnable()
        {
            health.events.OnDamageEvent.AddListener(OnDamage);
            health.events.OnDieEvent.AddListener(OnDie);
            health.events.OnOverkillEvent.AddListener(OnOverkill);
            health.events.OnHealHealthEvent.AddListener(OnHealHealth);
            health.events.OnOverhealEvent.AddListener(OnOverheal);
            health.events.OnMaxHealthEvent.AddListener(OnMaxHealth);
            health.events.OnReviveEvent.AddListener(OnRevive);
            health.events.OnRegenHealthStartEvent.AddListener(OnRegenHealthStart);
            health.events.OnChargeShieldEvent.AddListener(OnChargeShield);
            health.events.OnOverchargeShieldEvent.AddListener(OnOverchargeShield);
            health.events.OnMaxShieldEvent.AddListener(OnMaxShield);
            health.events.OnRegenShieldStartEvent.AddListener(OnRegenShieldStart);
        }

        private void OnDisable()
        {
            health.events.OnDamageEvent.RemoveListener(OnDamage);
            health.events.OnDieEvent.RemoveListener(OnDie);
            health.events.OnOverkillEvent.RemoveListener(OnOverkill);
            health.events.OnHealHealthEvent.RemoveListener(OnHealHealth);
            health.events.OnOverhealEvent.RemoveListener(OnOverheal);
            health.events.OnMaxHealthEvent.RemoveListener(OnMaxHealth);
            health.events.OnReviveEvent.RemoveListener(OnRevive);
            health.events.OnRegenHealthStartEvent.RemoveListener(OnRegenHealthStart);
            health.events.OnChargeShieldEvent.RemoveListener(OnChargeShield);
            health.events.OnOverchargeShieldEvent.RemoveListener(OnOverchargeShield);
            health.events.OnMaxShieldEvent.RemoveListener(OnMaxShield);
            health.events.OnRegenShieldStartEvent.RemoveListener(OnRegenShieldStart);
        }

        private void SpawnSFX(GameObject prefab)
        {
            if (!sfxEnabled || prefab == null) return;
            Instantiate(prefab, transform.position, transform.rotation);
        }

        private void SpawnVFX(GameObject prefab)
        {
            if (!vfxEnabled || prefab == null) return;
            Instantiate(prefab, transform.position, transform.rotation);
        }

        private void OnDamage(float _)           { SpawnSFX(sfx_onDamage);           SpawnVFX(vfx_onDamage); }
        private void OnDie(float _)              { SpawnSFX(sfx_onDie);              SpawnVFX(vfx_onDie); }
        private void OnOverkill(float _)         { SpawnSFX(sfx_onOverkill);         SpawnVFX(vfx_onOverkill); }
        private void OnHealHealth(float _)       { SpawnSFX(sfx_onHealHealth);       SpawnVFX(vfx_onHealHealth); }
        private void OnOverheal(float _)         { SpawnSFX(sfx_onOverheal);         SpawnVFX(vfx_onOverheal); }
        private void OnMaxHealth()               { SpawnSFX(sfx_onMaxHealth);        SpawnVFX(vfx_onMaxHealth); }
        private void OnRevive()                  { SpawnSFX(sfx_onRevive);           SpawnVFX(vfx_onRevive); }
        private void OnRegenHealthStart()        { SpawnSFX(sfx_onRegenHealthStart); SpawnVFX(vfx_onRegenHealthStart); }
        private void OnChargeShield(float _)     { SpawnSFX(sfx_onChargeShield);     SpawnVFX(vfx_onChargeShield); }
        private void OnOverchargeShield(float _) { SpawnSFX(sfx_onOverchargeShield); SpawnVFX(vfx_onOverchargeShield); }
        private void OnMaxShield()               { SpawnSFX(sfx_onMaxShield);        SpawnVFX(vfx_onMaxShield); }
        private void OnRegenShieldStart()        { SpawnSFX(sfx_onRegenShieldStart); SpawnVFX(vfx_onRegenShieldStart); }
    }
}
