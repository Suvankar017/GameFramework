using GameFramework.Runtime.Security;
using NUnit.Framework;

namespace GameFramework.Runtime.Tests.Security
{
    public class SensitiveDataRedactorTests
    {
        [TestCase("access_token")]
        [TestCase("RefreshToken")]
        [TestCase("api_key")]
        [TestCase("apiKey")]
        [TestCase("client_secret")]
        [TestCase("Authorization")]
        [TestCase("password")]
        [TestCase("purchase_receipt")]
        [TestCase("user_email")]
        public void IsSensitiveKey_KnownSensitiveKeys_ReturnsTrue(string key)
        {
            Assert.IsTrue(SensitiveDataRedactor.IsSensitiveKey(key));
        }

        [TestCase("level")]
        [TestCase("screen")]
        [TestCase("device_model")]
        [TestCase("")]
        [TestCase(null)]
        public void IsSensitiveKey_OrdinaryKeys_ReturnsFalse(string key)
        {
            Assert.IsFalse(SensitiveDataRedactor.IsSensitiveKey(key));
        }

        [Test]
        public void Redact_QueryString_MasksOnlySensitiveValues()
        {
            string result = SensitiveDataRedactor.Redact("mygame://login?token=eyJhbGciOi.abc&source=email_campaign&level=3");

            StringAssert.DoesNotContain("eyJhbGciOi", result);
            StringAssert.Contains("token=***", result);
            StringAssert.Contains("level=3", result);
        }

        [Test]
        public void Redact_JsonStylePair_MasksValue()
        {
            string result = SensitiveDataRedactor.Redact("{\"api_key\":\"AIzaSyFAKE123\",\"count\":2}");

            StringAssert.DoesNotContain("AIzaSyFAKE123", result);
            StringAssert.Contains("\"count\":2", result);
        }

        [Test]
        public void Redact_BearerCredential_Masked()
        {
            string result = SensitiveDataRedactor.Redact("Request failed with header Bearer abc.def.ghi");

            StringAssert.DoesNotContain("abc.def.ghi", result);
            StringAssert.Contains("Bearer ***", result);
        }

        [Test]
        public void Redact_TextWithNothingSensitive_ReturnsSameInstance()
        {
            const string text = "Section Progression reset to defaults";

            Assert.AreSame(text, SensitiveDataRedactor.Redact(text));
        }

        [Test]
        public void RedactValue_SensitiveKey_MasksWholeValue()
        {
            Assert.AreEqual(SensitiveDataRedactor.Mask, SensitiveDataRedactor.RedactValue("auth_token", "secret-value"));
            Assert.AreEqual("1-1", SensitiveDataRedactor.RedactValue("level", "1-1"));
        }

        [Test]
        public void RedactUri_TruncatesOversizedInput()
        {
            string uri = "mygame://x?q=" + new string('a', 5000);

            string result = SensitiveDataRedactor.RedactUri(uri, 100);

            Assert.Less(result.Length, 200);
        }
    }
}
