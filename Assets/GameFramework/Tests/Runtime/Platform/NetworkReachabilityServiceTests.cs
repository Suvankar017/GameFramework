using NUnit.Framework;
using UnityEngine;

namespace GameFramework.Platform.Tests
{
    public class NetworkReachabilityServiceTests
    {
        [Test]
        public void Current_MatchesApplicationInternetReachability()
        {
            var service = new NetworkReachabilityService();

            Assert.AreEqual(Application.internetReachability, service.Current);
        }

        [Test]
        public void IsOnline_MatchesCurrentNotEqualToNotReachable()
        {
            var service = new NetworkReachabilityService();

            Assert.AreEqual(service.Current != NetworkReachability.NotReachable, service.IsOnline);
        }
    }
}
