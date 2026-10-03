using System;
using NUnit.Framework;
using Project51.Auth;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 8, Amici: formato degli inviti mandati con Photon Chat.</summary>
    public class FriendsChatTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Invite_RoundTrip_KeepsNameWithBars()
        {
            var msg = FriendsChat.FormatInvite("AB12C", "2v2", "Tore|NA", Now);
            Assert.IsTrue(FriendsChat.TryParseInvite("pf1", msg, Now.AddSeconds(30), out var inv));
            Assert.AreEqual("pf1", inv.FromId);
            Assert.AreEqual("AB12C", inv.RoomCode);
            Assert.AreEqual("2v2", inv.Format);
            Assert.AreEqual("Tore|NA", inv.FromName);
        }

        [Test]
        public void Invite_TooOld_OrNotAnInvite_IsIgnored()
        {
            var msg = FriendsChat.FormatInvite("AB12C", "1v1", "Giulia", Now);
            Assert.IsFalse(FriendsChat.TryParseInvite("pf1", msg, Now.AddMinutes(3), out _));
            Assert.IsFalse(FriendsChat.TryParseInvite("pf1", "ciao", Now, out _));
            Assert.IsFalse(FriendsChat.TryParseInvite("pf1", "51|invito||1v1|x|Giulia", Now, out _));
            Assert.IsFalse(FriendsChat.TryParseInvite("pf1", 42, Now, out _));
        }

        [Test]
        public void LastSeen_HoursThenDays()
        {
            Assert.AreEqual("Visto poco fa", Project51.Unity.UI.UI51FriendsView.LastSeen(Now.AddMinutes(-20), Now));
            Assert.AreEqual("Visto 1 ora fa", Project51.Unity.UI.UI51FriendsView.LastSeen(Now.AddMinutes(-70), Now));
            Assert.AreEqual("Visto 2 ore fa", Project51.Unity.UI.UI51FriendsView.LastSeen(Now.AddHours(-2), Now));
            Assert.AreEqual("Visto ieri", Project51.Unity.UI.UI51FriendsView.LastSeen(Now.AddDays(-1), Now));
            Assert.AreEqual("Visto 3 giorni fa", Project51.Unity.UI.UI51FriendsView.LastSeen(Now.AddDays(-3), Now));
        }
    }
}
