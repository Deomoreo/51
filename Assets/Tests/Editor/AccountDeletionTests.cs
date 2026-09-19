#if UNITY_EDITOR
using NUnit.Framework;
using Project51.Auth;
using Project51.Core;
using UnityEngine;

namespace Project51.Tests
{
    /// <summary>H7: l'eliminazione account non deve mai risultare riuscita senza una conferma vera del backend.</summary>
    public class AccountDeletionTests
    {
        private string savedBackend;

        [SetUp]
        public void SetUp()
        {
            if (AppConfig.Instance != null) savedBackend = AppConfig.Instance.BackendBaseUrl;
        }

        [TearDown]
        public void TearDown()
        {
            if (AppConfig.Instance != null) AppConfig.Instance.BackendBaseUrl = savedBackend;
        }

        [Test]
        public void EmptyBackend_ReportsNotAvailable_NeverDeleted()
        {
            Assert.IsNotNull(AppConfig.Instance, "Resources/AppConfig mancante");
            AppConfig.Instance.BackendBaseUrl = "  ";

            AccountDeletionOutcome? outcome = null;
            AccountDeletionService.RequestDeletion(o => outcome = o);

            Assert.AreEqual(AccountDeletionOutcome.NotAvailable, outcome);
            Assert.IsFalse(AccountDeletionService.IsConfigured);
            Assert.IsFalse(AccountDeletionService.ConsumeDeletedNotice());
        }

        [TestCase(null, true, null)]
        [TestCase("", true, null)]
        [TestCase("not a url", true, null)]
        [TestCase("ftp://example.com", true, null)]
        [TestCase("https://api.example.com", false, "https://api.example.com/api/delete-account")]
        [TestCase(" https://api.example.com/ ", false, "https://api.example.com/api/delete-account")]
        [TestCase("http://localhost:8787", true, "http://localhost:8787/api/delete-account")]
        [TestCase("http://api.example.com", false, null)]
        public void BuildRequestUrl(string baseUrl, bool allowHttp, string expected)
        {
            Assert.AreEqual(expected, AccountDeletionService.BuildRequestUrl(baseUrl, allowHttp));
        }

        [TestCase(200, false, AccountDeletionOutcome.Deleted)]
        [TestCase(204, false, AccountDeletionOutcome.Deleted)]
        [TestCase(0, false, AccountDeletionOutcome.Failed)]
        [TestCase(200, true, AccountDeletionOutcome.Failed)]
        [TestCase(301, false, AccountDeletionOutcome.Failed)]
        [TestCase(401, false, AccountDeletionOutcome.NotSignedIn)]
        [TestCase(403, false, AccountDeletionOutcome.NotSignedIn)]
        [TestCase(404, false, AccountDeletionOutcome.Failed)]
        [TestCase(500, false, AccountDeletionOutcome.Failed)]
        public void Classify(long code, bool networkError, AccountDeletionOutcome expected)
        {
            Assert.AreEqual(expected, AccountDeletionService.Classify(code, networkError));
        }
    }
}
#endif
