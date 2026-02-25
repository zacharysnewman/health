using System;
using UnityEngine;
using UnityEngine.Events;

namespace Healthy
{
    [Serializable]
    public class HealthEvents
    {
        public UnityEvent<float> OnShieldChangeEvent = new UnityEvent<float>();
        public UnityEvent<float> OnHealthChangeEvent = new UnityEvent<float>();
        public UnityEvent<float> OnShieldChangeNormalizedEvent = new UnityEvent<float>();
        public UnityEvent<float> OnHealthChangeNormalizedEvent = new UnityEvent<float>();
        public UnityEvent<float> OnDamageEvent = new UnityEvent<float>();
        public UnityEvent<float> OnChargeShieldEvent = new UnityEvent<float>();
        public UnityEvent<float> OnHealHealthEvent = new UnityEvent<float>();
        public UnityEvent OnReviveEvent = new UnityEvent();
        public UnityEvent<float> OnDieEvent = new UnityEvent<float>();
        public UnityEvent<float> OnOverkillEvent = new UnityEvent<float>();
        public UnityEvent OnMaxShieldEvent = new UnityEvent();
        public UnityEvent OnMaxHealthEvent = new UnityEvent();
        public UnityEvent OnRegenShieldStartEvent = new UnityEvent();
        public UnityEvent OnRegenHealthStartEvent = new UnityEvent();
    }
}

