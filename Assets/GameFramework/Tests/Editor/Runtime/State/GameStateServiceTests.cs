using System;
using System.Collections.Generic;
using GameFramework.Runtime.State;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.State
{
    public class GameStateServiceTests
    {
        private class RecordingState : IGameState
        {
            public int EnterCount;
            public int ExitCount;

            public void Enter() => EnterCount++;
            public void Exit() => ExitCount++;
        }

        private GameStateService _service;

        [SetUp]
        public void SetUp()
        {
            _service = new GameStateService();
        }

        [Test]
        public void InitialState_HasNoCurrentOrPreviousState()
        {
            Assert.IsFalse(_service.HasCurrentState);
            Assert.IsNull(_service.CurrentState);
            Assert.IsNull(_service.PreviousState);
        }

        [Test]
        public void RegisterState_DuplicateKey_ThrowsInvalidOperationException()
        {
            _service.RegisterState("MainMenu", new RecordingState());

            Assert.Throws<InvalidOperationException>(() => _service.RegisterState("MainMenu", new RecordingState()));
        }

        [Test]
        public void RegisterState_NullOrEmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.RegisterState(null, new RecordingState()));
            Assert.Throws<ArgumentException>(() => _service.RegisterState(string.Empty, new RecordingState()));
        }

        [Test]
        public void TransitionTo_UnregisteredKey_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _service.TransitionTo("Unknown"));
        }

        [Test]
        public void TransitionTo_FirstState_CallsEnterOnlyAndUpdatesCurrent()
        {
            var mainMenu = new RecordingState();
            _service.RegisterState("MainMenu", mainMenu);

            _service.TransitionTo("MainMenu");

            Assert.AreEqual(1, mainMenu.EnterCount);
            Assert.AreEqual(0, mainMenu.ExitCount);
            Assert.AreEqual("MainMenu", _service.CurrentState);
            Assert.IsNull(_service.PreviousState);
            Assert.IsTrue(_service.HasCurrentState);
        }

        [Test]
        public void TransitionTo_SecondState_ExitsPreviousAndEntersNext()
        {
            var mainMenu = new RecordingState();
            var gameplay = new RecordingState();
            _service.RegisterState("MainMenu", mainMenu);
            _service.RegisterState("Gameplay", gameplay);
            _service.TransitionTo("MainMenu");

            _service.TransitionTo("Gameplay");

            Assert.AreEqual(1, mainMenu.ExitCount);
            Assert.AreEqual(1, gameplay.EnterCount);
            Assert.AreEqual("Gameplay", _service.CurrentState);
            Assert.AreEqual("MainMenu", _service.PreviousState);
        }

        [Test]
        public void TransitionTo_SameStateAgain_ThrowsInvalidOperationException()
        {
            _service.RegisterState("MainMenu", new RecordingState());
            _service.TransitionTo("MainMenu");

            Assert.Throws<InvalidOperationException>(() => _service.TransitionTo("MainMenu"));
        }

        [Test]
        public void CanTransitionTo_UnregisteredKey_ReturnsFalse()
        {
            Assert.IsFalse(_service.CanTransitionTo("Unknown"));
        }

        [Test]
        public void CanTransitionTo_CurrentState_ReturnsFalse()
        {
            _service.RegisterState("MainMenu", new RecordingState());
            _service.TransitionTo("MainMenu");

            Assert.IsFalse(_service.CanTransitionTo("MainMenu"));
        }

        [Test]
        public void CanTransitionTo_RegisteredNonCurrentState_ReturnsTrue()
        {
            _service.RegisterState("MainMenu", new RecordingState());
            _service.RegisterState("Gameplay", new RecordingState());
            _service.TransitionTo("MainMenu");

            Assert.IsTrue(_service.CanTransitionTo("Gameplay"));
        }

        [Test]
        public void StateChanged_RaisedWithPreviousAndCurrentKeys()
        {
            _service.RegisterState("MainMenu", new RecordingState());
            _service.RegisterState("Gameplay", new RecordingState());
            _service.TransitionTo("MainMenu");

            var seen = new List<(string previous, string current)>();
            _service.StateChanged += (previous, current) => seen.Add((previous, current));

            _service.TransitionTo("Gameplay");

            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(("MainMenu", "Gameplay"), seen[0]);
        }

        [Test]
        public void Shutdown_ExitsCurrentStateAndClearsRegisteredStates()
        {
            var mainMenu = new RecordingState();
            _service.RegisterState("MainMenu", mainMenu);
            _service.TransitionTo("MainMenu");

            _service.Shutdown();

            Assert.AreEqual(1, mainMenu.ExitCount);
            Assert.IsFalse(_service.HasCurrentState);
            Assert.IsNull(_service.CurrentState);
            Assert.Throws<InvalidOperationException>(() => _service.TransitionTo("MainMenu"));
        }
    }
}
