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

        [TestCase("ELIMINA", true)]
        [TestCase("  elimina ", true)]
        [TestCase("ELIMIN", false)]
        [TestCase("ELIMINA!", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void ConfirmWord(string typed, bool expected)
        {
            Assert.AreEqual(expected, Project51.UIV2.Core.DeleteAccountModalV2.IsConfirmWord(typed));
        }

        // iPhone 12 (1170x2532, area sicura alta 2391, 2.7121 px per unita'): finestra da 1848.6 a 872.2 px.
        [TestCase(0f, 0f)]        // tastiera chiusa o Editor
        [TestCase(600f, 0f)]      // tastiera piu' bassa della finestra
        [TestCase(1008f, 62.07f)] // tastiera da 336 pt: sale di 50 + 12 di stacco
        [TestCase(3000f, 188f)]   // misura assurda: si ferma al bordo alto dell'area sicura
        public void KeyboardLift(float keyboardPx, float expected)
        {
            Assert.AreEqual(expected, Project51.UIV2.Core.DeleteAccountModalV2.KeyboardLift(keyboardPx, 872.2f, 1848.6f, 2391f, 2.7121f), 0.1f);
        }

        [TestCase("giocatore@mail.com", "g•••••@mail.com")]
        [TestCase("@mail.com", "")]
        [TestCase("senzachiocciola", "")]
        [TestCase(null, "")]
        public void MaskEmail(string email, string expected)
        {
            Assert.AreEqual(expected, Project51.UIV2.Core.SettingsV2Integration.MaskEmail(email));
        }

        [Test]
        public void MarkRegistered_TurnsTheSessionIntoARealLoginWithEmail()
        {
            const string realKey = "Project51_HasRealLogin", registeredKey = "Project51_IsRegistered";
            int savedReal = PlayerPrefs.GetInt(realKey, 0), savedRegistered = PlayerPrefs.GetInt(registeredKey, 0);
            try
            {
                PlayerPrefs.SetInt(realKey, 0);
                var auth = new PlayFabAuthService();
                Assert.IsFalse(auth.HasRealLogin, "Control: guest before registering.");

                auth.MarkRegistered("giocatore@mail.com");

                Assert.IsTrue(auth.HasRealLogin, "Account UI (profile editor, delete account) is gated by this flag.");
                Assert.AreEqual("giocatore@mail.com", auth.Email);
            }
            finally
            {
                PlayerPrefs.SetInt(realKey, savedReal);
                PlayerPrefs.SetInt(registeredKey, savedRegistered);
                PlayerPrefs.Save();
            }
        }
    }
}
#endif
