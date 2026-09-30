#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Project51.Auth;
using Project51.Core;
using Project51.UI51;
using Project51.Unity.UI;
using Project51.UIV2.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Tests
{
    /// <summary>UI51 Fase 5 (tavolo): logica pura dietro alla nuova grafica.</summary>
    public class UI51TableTests
    {
        private static string Pill(GameState state, int local)
        {
            TableTopBarController.ScorePill(state, local, p => p + "-nome molto lungo", // il posto davanti: sopravvive al taglio a 12
                out string me, out string mine, out string rival, out string theirs);
            return me + " " + mine + " | " + rival + " " + theirs;
        }

        [Test]
        public void ScorePillOneVsOneShowsMatchTotalsFromTheLocalSeat()
        {
            var s = new GameState(2) { MatchTotals = new[] { 30, 20 } };
            s.Players[0].TotalScore = 4;
            s.Players[1].TotalScore = 9;
            Assert.AreEqual("TU 34 | 1-NOME MOLTO 29", Pill(s, 0));
            Assert.AreEqual("TU 29 | 0-NOME MOLTO 34", Pill(s, 1));
        }

        [Test]
        public void ScorePillTeamsShowUsAndThem()
        {
            var s = new GameState(4) { TeamMode = true, MatchTotals = new[] { 12, 40 } };
            Assert.AreEqual("NOI 12 | LORO 40", Pill(s, 2));
            Assert.AreEqual("NOI 40 | LORO 12", Pill(s, 3));
        }

        [Test]
        public void ScorePillFreeForAllShowsTheLeadingRival()
        {
            var s = new GameState(4) { MatchTotals = new[] { 10, 25, 31, 25 } };
            Assert.AreEqual("TU 10 | 2-NOME MOLTO 31", Pill(s, 0));
            // Io in testa: tra i due rivali a pari punti, il primo dopo di me nel giro (posto 3).
            Assert.AreEqual("TU 31 | 3-NOME MOLTO 25", Pill(s, 2));
        }

        [Test]
        public void ScorePillWritesCappottoInsteadOfTheSymbolicScore()
        {
            var s = new GameState(2) { MatchTotals = new[] { 30, 20 } };
            s.Players[1].TotalScore = MatchScore.CappottoScore;
            Assert.AreEqual("TU 30 | 1-NOME MOLTO CAPPOTTO", Pill(s, 0));
        }

        [Test]
        public void RemoteLookFromPhotonPropertiesIsTypeCheckedAndClamped()
        {
            int f, s, l;
            string Look() => f + " " + s + " " + l;
            Assert.IsFalse(ProfileCosmetics.ReadLook(null, out f, out s, out l));
            var props = new ExitGames.Client.Photon.Hashtable();
            Assert.IsFalse(ProfileCosmetics.ReadLook(props, out f, out s, out l)); // bot, ospite, versione vecchia
            props[ProfileService.LookFrameKey] = 2;                               // tipo sbagliato = niente aspetto
            Assert.IsFalse(ProfileCosmetics.ReadLook(props, out f, out s, out l));

            props[ProfileService.LookFrameKey] = "smeraldo";
            props[ProfileService.LookBannerKey] = "porpora";
            props[ProfileService.LookLevelKey] = 12;
            Assert.IsTrue(ProfileCosmetics.ReadLook(props, out f, out s, out l));
            Assert.AreEqual("2 2 12", Look());

            props[ProfileService.LookFrameKey] = "hack";  // id sconosciuto = Oro
            props[ProfileService.LookBannerKey] = 7;      // non stringa = Notte, mai un BannerStyle fuori elenco
            props[ProfileService.LookLevelKey] = 5000;
            Assert.IsTrue(ProfileCosmetics.ReadLook(props, out f, out s, out l));
            Assert.AreEqual("1 0 100", Look());

            props[ProfileService.LookLevelKey] = -3;
            ProfileCosmetics.ReadLook(props, out f, out s, out l);
            Assert.AreEqual(1, l);
            props[ProfileService.LookLevelKey] = "12";
            ProfileCosmetics.ReadLook(props, out f, out s, out l);
            Assert.AreEqual(1, l);
        }

        private static string Grid(int n, float w, float h)
        {
            Project51.Unity.CardViewManager.TableGrid(n, w, h, out int columns, out int rows, out float cardW, out float gap);
            return columns + "x" + rows + " " + cardW + " " + gap;
        }

        [Test]
        public void TableGridFollowsTheMockupRuleAndShrinksIntoTheBand()
        {
            // Regola del mockup Partita: 70 fino a 4, 60 fino a 8 (4 per riga), poi 54 con righe bilanciate.
            Assert.AreEqual("4x1 70 10", Grid(4, 390f, 320f));
            Assert.AreEqual("3x2 60 10", Grid(5, 390f, 320f));
            Assert.AreEqual("4x2 60 10", Grid(8, 390f, 320f));
            Assert.AreEqual("4x3 54 8", Grid(12, 390f, 320f));
            // Fascia del tavolo su iPhone 12 (350x249) e iPhone SE (350x179,7): piu' colonne invece di carte minuscole.
            Assert.AreEqual("6x2 51 8", Grid(12, 350f, 249f));
            Assert.AreEqual("5x1 60 10", Grid(5, 350f, 179.7f));
            Assert.AreEqual("3x2 54 10", Grid(6, 350f, 179.7f));
            Assert.AreEqual("6x2 51 8", Grid(12, 350f, 179.7f));
            Assert.AreEqual("5x2 54 8", Grid(9, 350f, 179.7f)); // lo spazio 12 tra le righe ci sta ancora su SE
            Assert.AreEqual("0x0 0 10", Grid(0, 350f, 179.7f));
            Assert.AreEqual("6x2 50 8", Grid(12, 340f, 179.7f)); // fascia vera del 1v1: 6 dal feltro
        }

        private static string Rim(float topBanner, float ownBanner)
        {
            // Unita' del mockup con y in su (= meno la y dall'alto), centro dello schermo a 195.
            var r = Project51.Unity.CardViewManager.TableRim(195f, -topBanner, -ownBanner, 1f);
            var d = Project51.Unity.CardViewManager.DeckCenter(r, 1f);
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "x {0:0.#}..{1:0.#} y {2:0.#}..{3:0.#} deck {4:0.#},{5:0.#}",
                r.xMin, r.xMax, -r.yMax, -r.yMin, d.x, -d.y);
        }

        [Test]
        public void TableRimAndDeckFollowTheBannersOnBothPhones()
        {
            Assert.AreEqual("x 10..380 y 124..704 deck 52,92", Rim(107f, 801f));    // mockup Partita
            Assert.AreEqual("x 10..380 y 171..670 deck 52,139", Rim(154f, 767f));   // iPhone 12: banner spostati dalla safe area
            Assert.AreEqual("x 10..380 y 124..553.7 deck 52,92", Rim(107f, 650.7f)); // iPhone SE
        }

        [Test]
        public void EllipticalCornersFollowTheCssRadii()
        {
            var half = new Vector2(185f, 290f);
            // border-radius 150px/120px: il punto a 45 gradi sull'ellisse dell'angolo e il centro del lato alto stanno sul bordo.
            Assert.AreEqual(0f, Project51.Unity.TableFeltRenderer.EllipseBoxSdf(new Vector2(141.066f, 254.853f), half, 150f, 120f), 0.01f);
            Assert.AreEqual(0f, Project51.Unity.TableFeltRenderer.EllipseBoxSdf(new Vector2(0f, 290f), half, 150f, 120f), 0.01f);
            Assert.Less(Project51.Unity.TableFeltRenderer.EllipseBoxSdf(new Vector2(0f, 0f), half, 150f, 120f), 0f);
            Assert.Greater(Project51.Unity.TableFeltRenderer.EllipseBoxSdf(new Vector2(180f, 285f), half, 150f, 120f), 0f); // fuori, nell'angolo
            Assert.AreEqual(0f, Project51.Unity.TableFeltRenderer.EllipseBoxSdf(new Vector2(185f, 0f), half, 100f, 100f), 0.01f);
        }

        [Test]
        public void TopSeatFollowsTheSafeTopLikeTheTopBar()
        {
            Assert.AreEqual(78.46f, LocalSeatBottomShift.ComputeTopShift(1170f, 2532f, 141f), 0.01f); // iPhone 12
            Assert.AreEqual(0.48f, LocalSeatBottomShift.ComputeTopShift(750f, 1334f, 0f), 0.01f);    // iPhone SE
            Assert.AreEqual(-60f, LocalSeatBottomShift.ComputeTopShift(1080f, 1920f, 60f), 0.01f);   // 9:16 con barra di stato: scende
            Assert.AreEqual(0f, LocalSeatBottomShift.ComputeTopShift(1536f, 2048f, 0f), 0.01f);      // tablet: nessuno spazio in piu'
        }

        [Test]
        public void BannerScopeRowShowsOneCardPerSlotThenTheMoreBadge()
        {
            var root = new GameObject("Banner");
            try
            {
                var banner = root.AddComponent<Project51.UI51.PlayerBanner>();
                var slots = new Image[4];
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = new GameObject("Card" + i).AddComponent<Image>();
                    slots[i].transform.SetParent(root.transform);
                    slots[i].gameObject.SetActive(false);
                }
                var label = new GameObject("Label").AddComponent<TMPro.TextMeshProUGUI>();
                var more = new GameObject("More").AddComponent<UI51Badge>();
                more.transform.SetParent(root.transform);
                label.transform.SetParent(more.transform);
                var so = new SerializedObject(more);
                so.FindProperty("m_Mode").intValue = (int)UI51Badge.Mode.More;
                so.FindProperty("m_Label").objectReferenceValue = label;
                so.FindProperty("m_Animate").boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                more.gameObject.SetActive(false);
                so = new SerializedObject(banner);
                so.FindProperty("m_ScopeMore").objectReferenceValue = more;
                var list = so.FindProperty("m_Scope");
                list.arraySize = slots.Length;
                for (int i = 0; i < slots.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                var cards = new List<Sprite>();
                for (int i = 0; i < 6; i++) cards.Add(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero));

                banner.SetScope(cards.GetRange(0, 2));
                Assert.AreEqual("1100 -", Shown(slots, more, label));
                Assert.AreSame(cards[1], slots[1].sprite);

                banner.SetScope(cards);
                Assert.AreEqual("1111 +2", Shown(slots, more, label));

                banner.SetScope(null);
                Assert.AreEqual("0000 -", Shown(slots, more, label));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void PickerHidesNameLevelAndChipEvenWhileCapturesRefresh()
        {
            var root = new GameObject("Banner");
            try
            {
                var banner = root.AddComponent<Project51.UI51.PlayerBanner>();
                var info = new GameObject("Info");
                info.transform.SetParent(root.transform);
                var name = new GameObject("Name").AddComponent<TMPro.TextMeshProUGUI>();
                name.transform.SetParent(info.transform);
                var chip = new GameObject("Chip");
                chip.transform.SetParent(root.transform);
                var so = new SerializedObject(banner);
                so.FindProperty("nameText").objectReferenceValue = name;
                so.FindProperty("m_Chip").objectReferenceValue = chip;
                so.ApplyModifiedPropertiesWithoutUndo();
                string State() => (info.activeSelf ? "info" : "-") + " " + (chip.activeSelf ? "chip" : "-");

                banner.SetCaptures(3);
                banner.SetPicking(true);
                Assert.AreEqual("- -", State());
                banner.SetCaptures(4); // il refresh a 5 Hz non riaccende il chip sotto alla fila di emoticon
                Assert.AreEqual("- -", State());
                banner.SetPicking(false);
                Assert.AreEqual("info chip", State());
                banner.SetCaptures(-1);
                Assert.AreEqual("info -", State());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TableShakeFollowsTheMockupKeyframes()
        {
            // @keyframes shake: 20% (-5, 2), 40% (4, -3), fermo agli estremi (px CSS, y in giu').
            void At(float t, float x, float y) => Assert.Less(Vector2.Distance(new Vector2(x, y), UIAnim.ShakeOffset(t)), 1e-3f, "t " + t);
            At(0f, 0f, 0f);
            At(0.2f, -5f, 2f);
            At(0.4f, 4f, -3f);
            At(1f, 0f, 0f);
        }

        [Test]
        public void AccusoRevealShrinksOnlyOnShortTables()
        {
            // Tavolo del mockup alto 499: iPhone 12 1:1, iPhone SE (429.7) piu' piccolo per non coprire la mano.
            Assert.AreEqual(1f, Project51.UIV2.Core.AccusoImpactV2.RevealScale(499f), 1e-5f);
            Assert.AreEqual(1f, Project51.UIV2.Core.AccusoImpactV2.RevealScale(580f), 1e-5f);
            Assert.AreEqual(0.861f, Project51.UIV2.Core.AccusoImpactV2.RevealScale(429.7f), 1e-3f);
        }

        [Test]
        public void ScopeFanKeepsTheMockupUpToSixThenTheSameOutline()
        {
            float step, degrees, drop;
            foreach (int n in new[] { 1, 2, 6 })
            {
                PlayerBannerManager.FanPose(n, out step, out degrees, out drop);
                Assert.AreEqual(new Vector3(40f, 9f, 6f), new Vector3(step, degrees, drop), n + " carte");
            }
            // Oltre 6 le carte esterne restano dove stanno con 6: centro a +-100, +-22.5 gradi, 15 in giu'.
            for (int n = 7; n <= 20; n++)
            {
                PlayerBannerManager.FanPose(n, out step, out degrees, out drop);
                float half = (n - 1) * 0.5f;
                Assert.AreEqual(100f, step * half, 0.01f, n + " carte");
                Assert.AreEqual(22.5f, degrees * half, 0.01f, n + " carte");
                Assert.AreEqual(15f, drop * half, 0.01f, n + " carte");
            }
        }

        [Test]
        public void ScopeViewerTexts()
        {
            string title, chipA, chipB;
            PlayerBannerManager.ScopeTexts(true, "x", 12, 6, out title, out chipA, out chipB);
            Assert.AreEqual("Le tue scope|12 carte prese|6 scope", title + "|" + chipA + "|" + chipB);
            PlayerBannerManager.ScopeTexts(false, "Marco_93", 14, 1, out title, out chipA, out chipB);
            Assert.AreEqual("Le scope di Marco_93|14 carte prese|1 scopa", title + "|" + chipA + "|" + chipB);
            PlayerBannerManager.ScopeTexts(false, "Bot 2", 1, 2, out title, out chipA, out chipB);
            Assert.AreEqual("1 carta presa|2 scope", chipA + "|" + chipB);
        }

        [Test]
        public void CaptureOptionColorsFollowTheMockupAndCycle()
        {
            string[] hex = { "F3C969", "4FD1C5", "F08A8D", "A78BFA" };
            for (int i = 0; i < hex.Length; i++)
                Assert.AreEqual(hex[i], ColorUtility.ToHtmlStringRGB(Project51.Unity.MoveSelectionUI.OptionColor(i)), "presa " + (i + 1));
            Assert.AreEqual(Project51.Unity.MoveSelectionUI.OptionColor(0), Project51.Unity.MoveSelectionUI.OptionColor(4), "la quinta riparte dall'oro");
        }

        [Test]
        public void CaptureOptionsPerTableCardKeepFirstOptionAndStackOrder()
        {
            var perCard = Project51.Unity.MoveSelectionUI.OptionsPerItem(new List<IReadOnlyList<string>>
                { new[] { "2d", "5s" }, null, new[] { "1b", "2d", "5s", null } });
            var s = new List<string>();
            foreach (var kv in perCard) s.Add(kv.Key + ":" + string.Join(",", kv.Value));
            Assert.AreEqual("2d:0,2 5s:0,2 1b:2", string.Join(" ", s));

            // Fila intera se entra, poi fino al 75%, poi scorre.
            Assert.AreEqual(1f, Project51.Unity.MoveSelectionUI.OptionsScale(282f, 390f));
            Assert.AreEqual(0.8f, Project51.Unity.MoveSelectionUI.OptionsScale(487.5f, 390f), 1e-4f);
            Assert.AreEqual(0.75f, Project51.Unity.MoveSelectionUI.OptionsScale(1338f, 390f));
        }

        [Test]
        public void ScopaChipFollowsTheGameRule()
        {
            var s = new GameState(2);
            s.Table.AddRange(new[] { new Card(Suit.Bastoni, 1), new Card(Suit.Denari, 2), new Card(Suit.Spade, 5) });
            s.Players[0].Hand.AddRange(new[] { new Card(Suit.Denari, 7), new Card(Suit.Spade, 10) });
            var seven = s.Players[0].Hand[0];
            var all = new Move(0, seven, MoveType.Capture15, new List<Card>(s.Table));
            var part = new Move(0, seven, MoveType.CaptureSum, new List<Card> { s.Table[1], s.Table[2] });
            Assert.IsTrue(Project51.Unity.TurnController.IsScopaCapture(s, all), "prende tutto il tavolo");
            Assert.IsFalse(Project51.Unity.TurnController.IsScopaCapture(s, part), "ne lascia una");

            // Ultima giocata della smazzata (mazzo vuoto, una sola carta in mano a tutti): niente scopa.
            s.Players[0].Hand.RemoveAt(1);
            Assert.AreEqual(0, s.Deck.Count);
            Assert.IsFalse(Project51.Unity.TurnController.IsScopaCapture(s, all), "ultima giocata");
        }

        [Test]
        public void LeaveDialogTextSaysWhoKeepsPlaying()
        {
            string Name(int p) => p == 1 ? "Marco_93" : p == 3 ? "Giulia" : "Bot " + (p + 1);
            string Leave(int local, int players, bool teams, bool online, params int[] humans) =>
                Project51.UIV2.Core.InGameSettingsV2.LeaveMessage(local, players, teams, online,
                    p => p == local || System.Array.IndexOf(humans, p) >= 0, Name);
            const string lost = "La partita verrà contata come persa.";

            Assert.AreEqual(lost + " La vittoria andrà a Marco_93.", Leave(0, 2, false, false), "allenamento 1v1");
            Assert.AreEqual(lost + " Il tuo posto verrà preso da un bot e Marco_93 continuerà la partita.", Leave(0, 2, false, true, 1),
                "online 1v1 contro una persona");
            Assert.AreEqual(lost + " La vittoria andrà a Marco_93.", Leave(0, 2, false, true), "online 1v1 contro un bot: la stanza chiude");
            Assert.AreEqual(lost + " Il tuo posto verrà preso da un bot e Giulia continuerà la partita.", Leave(1, 4, true, true, 3, 0),
                "2v2 col compagno persona (testo del mockup)");
            Assert.AreEqual(lost + " Il tuo posto verrà preso da un bot e la partita continuerà.", Leave(3, 4, true, true, 0),
                "2v2 col compagno bot e un avversario persona");
            Assert.AreEqual(lost + " Il tuo posto verrà preso da un bot e la partita continuerà.", Leave(0, 4, false, true, 2), "4 giocatori online");
            Assert.AreEqual(lost, Leave(0, 4, false, false), "allenamento a 4");
            Assert.AreEqual(lost, Leave(0, 4, true, false), "allenamento 2v2");
        }

        [Test]
        public void DragThresholdIsAboutOnePointSixMillimetres()
        {
            Assert.AreEqual(10, Project51.UIV2.Animations.UIV2MotionInstaller.DragThreshold(0f), "DPI sconosciuti: default Unity");
            Assert.AreEqual(10, Project51.UIV2.Animations.UIV2MotionInstaller.DragThreshold(96f), "Editor");
            Assert.AreEqual(20, Project51.UIV2.Animations.UIV2MotionInstaller.DragThreshold(326f), "iPhone SE");
            Assert.AreEqual(29, Project51.UIV2.Animations.UIV2MotionInstaller.DragThreshold(460f), "iPhone 12");
        }

        [Test]
        public void HoverKeepsTheCardWhileThePointerIsOnItsRestPlace()
        {
            // Carta della mano 1v1 misurata dal vivo (iPhone 12): collider 1.222x1.8 a scala 1.086, hover +0.12 e x1.08.
            var size = new Vector2(1.222f, 1.8f);
            var rest = new Vector3(0f, -3.108f, 0f);
            var scale = new Vector3(1.086f, 1.086f, 1f);
            var bottomEdge = new Vector3(0.2f, rest.y - 0.9f * 1.086f + 0.02f, 0f); // l'ultimo tocco rimasto sul bordo basso
            Assert.IsTrue(Project51.Unity.CardView.BoxContains(bottomEdge, rest, Quaternion.identity, scale, Vector2.zero, size));
            Assert.IsFalse(Project51.Unity.CardView.BoxContains(bottomEdge, rest + Vector3.up * 0.12f, Quaternion.identity, scale * 1.08f,
                Vector2.zero, size), "sollevata sfugge al puntatore: senza il posto a riposo entrava e usciva in continuo");
            // Ventaglio a 4: il punto si misura nella rotazione della carta.
            var fan = Quaternion.Euler(0f, 0f, 30f);
            var corner = rest + fan * new Vector3(0.6f * 1.086f, 0.89f * 1.086f, 0f);
            Assert.IsTrue(Project51.Unity.CardView.BoxContains(corner, rest, fan, scale, Vector2.zero, size));
            Assert.IsFalse(Project51.Unity.CardView.BoxContains(corner, rest, Quaternion.identity, scale, Vector2.zero, size));
        }

        [Test]
        public void SorteggioStopsTheDealerUnderThePointer()
        {
            // Posto w della ruota (0 io, 1 avversario) a w*180+90 gradi CSS: a ruota ferma dentro lo spicchio in alto.
            for (int n = 0; n < 50; n++)
                for (int w = 0; w < 2; w++)
                    Assert.LessOrEqual(Mathf.Abs(Mathf.DeltaAngle(0f, w * 180f + 90f + UIAnim.WheelTarget(w, 2))), 45f);

            Project51.Unity.UI.SorteggioView.Texts(true, "Marco_93", out string name, out string note);
            Assert.AreEqual("Sei tu!", name);
            Assert.AreEqual("Distribuisci tu: Marco_93 gioca per primo", note);
            Project51.Unity.UI.SorteggioView.Texts(false, "Marco_93", out name, out note);
            Assert.AreEqual("Marco_93", name);
            Assert.AreEqual("Distribuisce Marco_93: giochi tu per primo", note);
        }

        private static string Shown(Image[] slots, UI51Badge more, TMPro.TMP_Text label)
        {
            string s = "";
            foreach (var slot in slots) s += slot.gameObject.activeSelf ? "1" : "0";
            return s + " " + (more.gameObject.activeSelf ? label.text : "-");
        }
    }
}
#endif
