using UnityEngine;

namespace GameFramework.Tutorials.Steps
{
    /// <summary>Completes automatically after <see cref="_durationSeconds"/> of unscaled time has
    /// elapsed while active - see <see cref="ITutorialStep.Tick"/>'s remarks on why unscaled.</summary>
    public sealed class WaitStep : TutorialStepBase
    {
        private readonly float _durationSeconds;
        private float _elapsedSeconds;

        public WaitStep(string id, float durationSeconds) : base(id)
        {
            _durationSeconds = Mathf.Max(0f, durationSeconds);
        }

        protected override void OnBegin()
        {
            _elapsedSeconds = 0f;

            if (_durationSeconds <= 0f)
            {
                Complete();
            }
        }

        public override void Tick(float unscaledDeltaTime)
        {
            if (State != TutorialStepState.Active)
            {
                return;
            }

            _elapsedSeconds += unscaledDeltaTime;
            if (_elapsedSeconds >= _durationSeconds)
            {
                Complete();
            }
        }
    }
}
