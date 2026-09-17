using System;
using System.Collections.Generic;

namespace GameFramework.Progression.Economy
{
    /// <summary>Persisted shape for <see cref="EconomyService"/> — <see cref="JsonUtility"/> cannot
    /// serialize a <see cref="Dictionary{TKey,TValue}"/>, so balances are stored as parallel lists,
    /// the same technique <c>SettingsSnapshot</c> already uses for the same reason.</summary>
    [Serializable]
    internal sealed class EconomySaveData
    {
        public List<string> CurrencyIds = new List<string>();
        public List<int> Balances = new List<int>();
    }
}
