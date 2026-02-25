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
        public float CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = Mathf.Clamp(value, 0, healthData.Traits.MaxHealth);
                events.OnHealthChangeEvent?.Invoke(currentHealth);
                events.OnHealthChangeNormalizedEvent?.Invoke(currentHealth / healthData.Traits.MaxHealth);
            }
        }
        public float CurrentShield
        {
            get => currentShield;
            set
            {
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
            CurrentHealth = healthData.Traits.MaxHealth;
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
                healthData.Traits.MaxHealth * Mathf.Clamp01(healthPercent),
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

            if (IsDead)
                return;

            CurrentShield += amount;
            events.OnChargeShieldEvent?.Invoke(amount);
        }

        public void HealHealth(float amount)
        {
            if (amount < 0)
                throw new ArgumentException("Heal value cannot be negative");

            if (IsDead)
                return;

            CurrentHealth += amount;
            events.OnHealHealthEvent?.Invoke(amount);
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
            if (withDelay)
                yield return new WaitForSeconds(healthData.Traits.HealthRegenDelay);

            events.OnRegenHealthStartEvent?.Invoke();

            while (CurrentHealth < healthData.Traits.MaxHealth)
            {
                CurrentHealth += healthData.Traits.HealthRegenRate * Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
