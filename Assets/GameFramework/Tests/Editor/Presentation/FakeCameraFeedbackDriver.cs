using System.Collections.Generic;

namespace GameFramework.Presentation.Tests
{
    internal sealed class FakeCameraFeedbackDriver : ICameraFeedbackDriver
    {
        public readonly List<CameraShakeRequest> Requests = new List<CameraShakeRequest>();
        public int CancelAllCallCount { get; private set; }

        public void RequestShake(CameraShakeRequest request) => Requests.Add(request);
        public void CancelAll() => CancelAllCallCount++;
    }
}
