using System;

namespace Runavelle.Core
{
    /// <summary>
    /// The ONLY two spendable resources in Runavelle. Do not add a third (design constraint #1).
    /// </summary>
    public enum Currency { Gold = 0, Mana = 1 }

    [Serializable]
    public struct Cost
    {
        public int Gold;
        public int Mana;

        public Cost(int gold, int mana = 0) { Gold = gold; Mana = mana; }

        public static readonly Cost Free = new Cost(0, 0);
        public bool IsFree => Gold == 0 && Mana == 0;

        public static Cost operator +(Cost a, Cost b) => new Cost(a.Gold + b.Gold, a.Mana + b.Mana);
        public static Cost operator -(Cost a, Cost b) => new Cost(a.Gold - b.Gold, a.Mana - b.Mana);

        public Cost Scaled(float f) => new Cost((int)Math.Round(Gold * f), (int)Math.Round(Mana * f));

        public override string ToString()
        {
            if (IsFree) return "Free";
            if (Mana == 0) return Gold + "g";
            if (Gold == 0) return Mana + "m";
            return Gold + "g " + Mana + "m";
        }
    }

    /// <summary>Gold/Mana wallet. Balances can never become negative.</summary>
    public sealed class Economy
    {
        public int Gold { get; private set; }
        public int Mana { get; private set; }

        /// <summary>(currency, delta, reason) for every change. Used by analytics/UI.</summary>
        public event Action<Currency, int, string> Changed;

        public int LifetimeGoldEarned { get; private set; }
        public int LifetimeManaEarned { get; private set; }

        public Economy(int gold, int mana)
        {
            Gold = Math.Max(0, gold);
            Mana = Math.Max(0, mana);
        }

        public int Get(Currency c) => c == Currency.Gold ? Gold : Mana;

        public bool CanAfford(Cost cost) => cost.Gold <= Gold && cost.Mana <= Mana && cost.Gold >= 0 && cost.Mana >= 0;

        public bool TrySpend(Cost cost, string reason)
        {
            if (cost.Gold < 0 || cost.Mana < 0) return false;
            if (!CanAfford(cost)) return false;
            if (cost.Gold > 0) { Gold -= cost.Gold; Changed?.Invoke(Currency.Gold, -cost.Gold, reason); }
            if (cost.Mana > 0) { Mana -= cost.Mana; Changed?.Invoke(Currency.Mana, -cost.Mana, reason); }
            return true;
        }

        /// <summary>Adds non-negative amounts. Negative components are ignored (use TrySpend).</summary>
        public void Add(Cost amount, string reason)
        {
            if (amount.Gold > 0) { Gold += amount.Gold; LifetimeGoldEarned += amount.Gold; Changed?.Invoke(Currency.Gold, amount.Gold, reason); }
            if (amount.Mana > 0) { Mana += amount.Mana; LifetimeManaEarned += amount.Mana; Changed?.Invoke(Currency.Mana, amount.Mana, reason); }
        }

        /// <summary>Restores exact balances from a save file.</summary>
        public void Restore(int gold, int mana, int lifetimeGold, int lifetimeMana)
        {
            Gold = Math.Max(0, gold);
            Mana = Math.Max(0, mana);
            LifetimeGoldEarned = Math.Max(0, lifetimeGold);
            LifetimeManaEarned = Math.Max(0, lifetimeMana);
        }
    }
}
