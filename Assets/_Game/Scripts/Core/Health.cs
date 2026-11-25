using System;
using UnityEngine;

namespace Duck
{
    /// <summary>HP for the duck and buildings (enemies keep their own lightweight HP).</summary>
    public class Health : MonoBehaviour
    {
        public float max = 100f;
        public bool invulnerable;

        public float Current { get; private set; }
        public bool IsAlive => Current > 0f;
        public float Normalized => max > 0f ? Current / max : 0f;

        public event Action<float> Damaged;
        public event Action Died;
        public event Action Changed;

        void Awake()
        {
            if (Current <= 0f) Current = max;
        }

        public void ResetTo(float newMax)
        {
            max = newMax;
            Current = max;
            Changed?.Invoke();
        }

        public void HealFull()
        {
            Current = max;
            Changed?.Invoke();
        }

        public bool TakeDamage(float amount)
        {
            if (!IsAlive || invulnerable || amount <= 0f) return false;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            Changed?.Invoke();
            if (Current <= 0f) Died?.Invoke();
            return true;
        }
    }
}
