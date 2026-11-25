using System;
using UnityEngine;

namespace Duck
{
    /// <summary>Coins of the current battle.</summary>
    public class Wallet : MonoBehaviour
    {
        public static Wallet Instance { get; private set; }
        public int Coins { get; private set; }
        public event Action<int> Changed;

        void Awake() { Instance = this; }

        public void Set(int value)
        {
            Coins = Mathf.Max(0, value);
            Changed?.Invoke(Coins);
        }

        public void Add(int amount)
        {
            if (amount == 0) return;
            Coins += amount;
            Changed?.Invoke(Coins);
        }

        public bool TrySpend(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            Changed?.Invoke(Coins);
            return true;
        }
    }
}
