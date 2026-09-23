using System;
using System.Collections.Generic;
using GameFramework.RemoteConfig.Providers;

namespace GameFramework.RemoteConfig.Tests
{
    /// <summary>Fully controllable <see cref="IRemoteConfigProvider"/> test double - see
    /// <c>Monetization.Tests.FakePurchaseProvider</c>'s remarks for why
    /// <c>RemoteConfigService</c>'s own orchestration tests use this instead of
    /// <see cref="Providers.Mock.MockRemoteConfigProvider"/>: it lets a single test enqueue an exact
    /// result (invalid data, a version/schema mismatch, ...) without needing a matching simulation
    /// mode to exist on the shipped mock.</summary>
    internal sealed class FakeRemoteConfigProvider : IRemoteConfigProvider
    {
        public bool InitializeSucceeds { get; set; } = true;
        public int FetchCallCount { get; private set; }
        public bool SuppressCallback { get; set; }
        public Queue<RemoteConfigProviderResult> NextResults { get; } = new Queue<RemoteConfigProviderResult>();

        private Action<RemoteConfigProviderResult> _pendingCallback;

        public void Initialize(Action<bool> onComplete) => onComplete?.Invoke(InitializeSucceeds);

        public void Fetch(int currentSchemaVersion, Action<RemoteConfigProviderResult> onComplete)
        {
            FetchCallCount++;

            if (SuppressCallback)
            {
                _pendingCallback = onComplete;
                return;
            }

            RemoteConfigProviderResult result = NextResults.Count > 0
                ? NextResults.Dequeue()
                : RemoteConfigProviderResult.Successful(FetchCallCount, currentSchemaVersion, new Dictionary<string, object>());

            onComplete?.Invoke(result);
        }

        /// <summary>Completes a fetch previously suppressed via <see cref="SuppressCallback"/> -
        /// used to simulate a slow provider responding after the service's own fetch-timeout already
        /// fired, verifying the late callback is ignored.</summary>
        public void CompleteSuppressedFetch(RemoteConfigProviderResult result)
        {
            Action<RemoteConfigProviderResult> callback = _pendingCallback;
            _pendingCallback = null;
            callback?.Invoke(result);
        }
    }
}
