using System;
using UnityEngine;

namespace GameFramework.RemoteConfig.Providers.Mock
{
    /// <summary>One authored value <see cref="MockRemoteConfigProvider"/> serves on a successful
    /// simulated fetch - the Editor-authored stand-in for a real backend's response.</summary>
    [Serializable]
    public sealed class MockRemoteConfigEntry
    {
        [SerializeField] private string _key;
        [SerializeField] private RemoteConfigTypedValue _value;

        public string Key => _key;
        public object BoxedValue => _value.BoxedValue;
    }
}
