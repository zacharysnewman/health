using System;
using System.Collections;
using UnityEngine;

namespace Healthy
{
    public class Health : MonoBehaviour
    {
        private float currentHealth;
        private float currentShield;
        private bool isDead = false;

        public HealthData healthData;
        public HealthEvents events;
        public float MaxHealthBonus = 0f;
        private float MaxHealth => healthData.Traits.MaxHealth + MaxHealthBonus;
        public float CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = Mathf.Clamp(value, 0, MaxHealth);
                events.OnHealthChangeEvent?.Invoke(currentHealth);
                events.OnHealthChangeNormalizedEvent?.Invoke(currentHealth / MaxHealth);
            }
        }
        public float CurrentShield
        {
            get => currentShield;
            set
            {
                if (!healthData.Traits.HasShields)
                    return;
                currentShield = Mathf.Clamp(value, 0, healthData.Traits.MaxShield);
                events.OnShieldChangeEvent?.Invoke(currentShield);
                events.OnShieldChangeNormalizedEvent?.Invoke(currentShield / healthData.Traits.MaxShield);
            }
        }
        public bool IsDead { get => isDead; private set => isDead = value; }

        void Start()
        {
            InitializeValues();
        }

        public void InitializeValues()
        {
            CurrentHealth = MaxHealth;
            CurrentShield = healthData.Traits.MaxShield;
            IsDead = false;
        }

        public void Revive()
        {
            ReviveInternal(1f, 0f);
        }

        public void Revive(float healthPercent, float shieldPercent = 0f)
        {
            ReviveInternal(
                MaxHealth * Mathf.Clamp01(healthPercent),
                healthData.Traits.MaxShield * Mathf.Clamp01(shieldPercent)
            );
        }

        public void Revive(int healthAmount, int shieldAmount = 0)
        {
            ReviveInternal(healthAmount, shieldAmount);
        }

        private void ReviveInternal(float health, float shield)
        {
            if (!IsDead)
                return;

            IsDead = false;
            CurrentHealth = health;
            CurrentShield = shield;
            StartRegen(withDelay: false);
            events.OnReviveEvent?.Invoke();
        }

        public void Damage(float amount)
        {
            if (amount < 0)
                throw new ArgumentException("Damage value cannot be negative");

            if (IsDead)
            {
                events.OnOverkillEvent?.Invoke(amount);
                return;
            }

            StopRegen();

            (CurrentShield, CurrentHealth) = HealthUtils.CalculateDamageSplit(amount, CurrentShield, CurrentHealth, healthData.Traits.ShieldBleedThrough);

            if (CurrentHealth <= 0)
            {
                IsDead = true;
                StopRegen();
                events.OnDieEvent?.Invoke(amount);
            }
            else
            {
                StartRegen(withDelay: true);
                events.OnDamageEvent?.Invoke(amount);
            }
        }

        public void ChargeShield(float amount)
        {
            if (amount < 0)
                throw new ArgumentException("Charge value cannot be negative");

            if (IsDead || !healthData.Traits.HasShields)
                return;

            float shieldOverage = Mathf.Max(0, currentShield + amount - healthData.Traits.MaxShield);
            CurrentShield += amount;
            events.OnChargeShieldEvent?.Invoke(amount);
            if (shieldOverage > 0)
                events.OnOverchargeShieldEvent?.Invoke(shieldOverage);
        }

        public void HealHealth(float amount)
        {
            if (amount < 0)
                throw new ArgumentException("Heal value cannot be negative");

            if (IsDead)
                return;

            float healthOverage = Mathf.Max(0, currentHealth + amount - MaxHealth);
            CurrentHealth += amount;
            events.OnHealHealthEvent?.Invoke(amount);
            if (healthOverage > 0)
                events.OnOverhealEvent?.Invoke(healthOverage);
        }

        public void StartRegen(bool withDelay)
        {
            StopAllCoroutines();
            StartCoroutine(StartRegenCoroutine(withDelay));
        }

        public void StopRegen()
        {
            StopAllCoroutines();
        }

        private IEnumerator StartRegenCoroutine(bool withDelay = false)
        {
            switch (healthData.Traits.RegenTrigger)
            {
                case RegenTrigger.HealthThenShield:
                    yield return RegenHealthCoroutine(withDelay);
                    yield return RegenShieldCoroutine(withDelay: false);
                    break;
                case RegenTrigger.ShieldThenHealth:
                    yield return RegenShieldCoroutine(withDelay);
                    yield return RegenHealthCoroutine(withDelay: false);
                    break;
                case RegenTrigger.HealthAndShield:
                    StartCoroutine(RegenHealthCoroutine(withDelay));
                    StartCoroutine(RegenShieldCoroutine(withDelay));
                    break;
            }
        }

        private IEnumerator RegenShieldCoroutine(bool withDelay)
        {
            if (!healthData.Traits.HasShields || !healthData.Traits.ShouldShieldRegen)
                yield break;

            if (withDelay)
                yield return new WaitForSeconds(healthData.Traits.ShieldRegenDelay);

            events.OnRegenShieldStartEvent?.Invoke();

            while (CurrentShield < healthData.Traits.MaxShield)
            {
                CurrentShield += healthData.Traits.ShieldRegenRate * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator RegenHealthCoroutine(bool withDelay)
        {
            if (!healthData.Traits.ShouldHealthRegen)
                yield break;

            if (withDelay)
                yield return new WaitForSeconds(healthData.Traits.HealthRegenDelay);

            events.OnRegenHealthStartEvent?.Invoke();

            while (CurrentHealth < MaxHealth)
            {
                CurrentHealth += healthData.Traits.HealthRegenRate * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
