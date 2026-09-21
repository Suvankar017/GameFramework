using System;
using System.Collections.Generic;
using GameFramework.GameFlow;
using GameFramework.GameFlow.Checkpoints;
using GameFramework.GameFlow.Session;
using GameFramework.Gameplay.Objectives;
using GameFramework.Runtime.Services;

namespace GameFramework.UI.Navigation.Tests
{
    /// <summary>Minimal test double for <see cref="IGameFlowService"/> - only
    /// <see cref="PauseGameplay"/> is exercised by <see cref="NavigationService"/>'s
    /// <c>PausesGameplay</c> popup option; every other member is an unused stub.</summary>
    internal sealed class FakeGameFlowService : IGameFlowService
    {
        private sealed class FakePauseToken : IPauseToken
        {
            private readonly FakeGameFlowService _owner;
            public string Reason { get; }
            public bool IsActive { get; private set; } = true;

            public FakePauseToken(FakeGameFlowService owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            public void Release()
            {
                if (!IsActive)
                {
                    return;
                }

                IsActive = false;
                _owner.ReleasedTokenCount++;
            }
        }

        public int PauseGameplayCallCount;
        public int ReleasedTokenCount;
        public string LastPauseReason;
        private readonly List<FakePauseToken> _issuedTokens = new List<FakePauseToken>();

        public void Initialize(IServiceRegistry registry)
        {
        }

        public void Shutdown()
        {
        }

        public LevelFlowState State => LevelFlowState.Unloaded;
        public IReadOnlyList<LevelFlowState> StateHistory { get; } = new List<LevelFlowState>();
        public LevelDefinition CurrentLevel => null;
        public GameplaySession CurrentSession => null;
        public bool IsPaused => _issuedTokens.Count > 0;
        public IReadOnlyCollection<string> ActivePauseReasons { get; } = new List<string>();
        public bool PersistCheckpoints { get; set; }

        public event Action<LevelFlowState, LevelFlowState> StateChanged;

        public LevelLoadResult LoadLevel(LevelDefinition level, string mode = null) => LevelLoadResult.Rejected;
        public TransitionResult StartLevel() => TransitionResult.InvalidTransition;
        public TransitionResult CompleteLevel(GameplayResult result = default) => TransitionResult.InvalidTransition;
        public TransitionResult FailLevel(GameplayResult result = default) => TransitionResult.InvalidTransition;
        public TransitionResult Retry(bool reloadScene = true) => TransitionResult.InvalidTransition;
        public RespawnResult Respawn() => RespawnResult.NoActiveSession;
        public TransitionResult ExitLevel() => TransitionResult.InvalidTransition;

        public IPauseToken PauseGameplay(string reason = null)
        {
            PauseGameplayCallCount++;
            LastPauseReason = reason;
            var token = new FakePauseToken(this, reason);
            _issuedTokens.Add(token);
            return token;
        }

        public bool RegisterCheckpoint(string id, CheckpointData? transform = null, IGameplaySnapshot snapshot = null) => false;
        public bool ActivateCheckpoint(string id) => false;
        public bool TryLoadPersistedCheckpoint(out string checkpointId, out CheckpointData? transform)
        {
            checkpointId = null;
            transform = null;
            return false;
        }
    }
}
