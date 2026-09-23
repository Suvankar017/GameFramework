using System.Text;
using GameFramework.Runtime.Events;
using GameFramework.Runtime.Services;
using NUnit.Framework;

namespace GameFramework.DeepLinks.Tests
{
    /// <summary>Phase 19: structural validation of untrusted inbound URIs - see
    /// <see cref="DeepLinkValidationOptions"/>.</summary>
    public class DeepLinkValidationTests
    {
        private ServiceRegistry _registry;
        private FakeDeepLinkHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _registry = TestRegistryFactory.Build(out EventService _);
            _handler = new FakeDeepLinkHandler();
        }

        private DeepLinkService Build(DeepLinkValidationOptions options = null)
        {
            var service = new DeepLinkService(options);
            service.Initialize(_registry);
            service.SetReady(true);
            service.RegisterHandler(_handler);
            return service;
        }

        [Test]
        public void Process_OversizedUri_RejectedBeforeDispatch()
        {
            DeepLinkService service = Build();
            string uri = "mygame://shop?x=" + new string('a', DeepLinkValidationOptions.DefaultMaxUriLength);

            DeepLinkResult result = service.Process(uri);

            Assert.AreEqual(DeepLinkResultKind.Rejected, result.Kind);
            Assert.AreEqual(0, _handler.HandleCallCount);
            Assert.IsNull(service.GetDiagnostics().LastProcessedRawUri, "An oversized URI must not be retained.");
            service.Shutdown();
        }

        [Test]
        public void Process_ControlCharacters_Rejected()
        {
            DeepLinkService service = Build();

            Assert.AreEqual(DeepLinkResultKind.Rejected, service.Process("mygame://shop\n?x=1").Kind);
            service.Shutdown();
        }

        [Test]
        public void Process_SchemeNotInAllowList_Rejected()
        {
            DeepLinkService service = Build(new DeepLinkValidationOptions(allowedSchemes: new[] { "mygame" }));

            Assert.AreEqual(DeepLinkResultKind.Rejected, service.Process("othergame://shop").Kind);
            Assert.AreEqual(DeepLinkResultKind.Handled, service.Process("MYGAME://shop").Kind, "Scheme matching is case-insensitive.");
            service.Shutdown();
        }

        [Test]
        public void Process_TooManyQueryParameters_Rejected()
        {
            DeepLinkService service = Build(new DeepLinkValidationOptions(maxQueryParameters: 3));

            Assert.AreEqual(DeepLinkResultKind.Rejected, service.Process("mygame://shop?a=1&b=2&c=3&d=4").Kind);
            service.Shutdown();
        }

        [Test]
        public void Process_OversizedQueryValue_Rejected()
        {
            DeepLinkService service = Build(new DeepLinkValidationOptions(maxQueryValueLength: 8));

            Assert.AreEqual(DeepLinkResultKind.Rejected, service.Process("mygame://shop?id=123456789").Kind);
            service.Shutdown();
        }

        [Test]
        public void Process_DefaultOptions_AcceptOrdinaryLinks()
        {
            DeepLinkService service = Build();
            var builder = new StringBuilder("mygame://daily-reward?source=notification");
            for (int i = 0; i < 20; i++)
            {
                builder.Append("&p").Append(i).Append("=value");
            }

            Assert.AreEqual(DeepLinkResultKind.Handled, service.Process(builder.ToString()).Kind);
            service.Shutdown();
        }

        [Test]
        public void Process_RejectedLinkThenValidLink_ValidOneStillDispatched()
        {
            DeepLinkService service = Build();

            service.Process("mygame://shop\u0000");
            DeepLinkResult result = service.Process("mygame://shop");

            Assert.AreEqual(DeepLinkResultKind.Handled, result.Kind);
            service.Shutdown();
        }
    }
}
