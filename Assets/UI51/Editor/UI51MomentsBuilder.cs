using Project51.UIV2.Core;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 14 su GameScene, dentro "Tools/UI51/Build Fase 5 (Tavolo 1v1)": momenti di partita (mockup MomentiPartita).
    /// GameCanvas/UI51Moments (UI51TableMoments): chip della mano, avviso sui giocatori, SCOPA!; nei banner UI51 il velo
    /// "Offline" col Wi-Fi barrato del giocatore disconnesso. Gli avvisi di connessione li mostra UI51Moments.
    /// Il turno blu e' in PlayerBanner.SetTurn.
    /// </summary>
    public static partial class UI51TableBuilder
    {
        // Mockup: scintille dal centro del SCOPA! (left 193 top 396) verso (dx, dy).
        static readonly Vector2[] SparkTargets =
        {
            new Vector2(-110f, -60f), new Vector2(110f, -60f), new Vector2(-90f, 50f), new Vector2(90f, 50f),
            new Vector2(0f, -100f), new Vector2(-140f, 0f), new Vector2(140f, 0f), new Vector2(0f, 90f),
        };

        static void BuildMoments(PlayerBannerManager banners, Transform canvas, params Transform[] seats)
        {
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("AvatarFrame"));
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            if (avatarPrefab == null || glow == null)
            {
                Debug.LogError($"{Tag} Manca AvatarFrame o Bagliore_morbido. Momenti di partita non costruiti.");
                return;
            }

            var root = UI51Build.Stretch(UI51Build.Child(canvas, "UI51Moments"));
            root.SetAsLastSibling();
            var view = UI51Build.GetOrAdd<UI51TableMoments>(root);

            // Chip della mano (top 232): alta 30 r15, fondo .92, bordo oro .55, Cinzel 12 spaziato 2, larga quanto il testo.
            var chipHolder = Holder(root, "ChipHolder");
            var chip = UI51Build.Place(UI51Build.Child(chipHolder, "Chip"), new Vector2(0.5f, 1f), new Vector2(150f, 30f), Vector2.zero);
            chip.pivot = new Vector2(0.5f, 1f);
            var chipShape = UI51Build.Solid(chip, UI51Tokens.Rgba(6, 13, 27, 0.92f), 15f, 1f, UI51Tokens.GoldA(0.55f), false,
                new UI51Shadow(0f, 8f, 18f, UI51Tokens.BlackA(0.45f)));
            UI51Build.Row(chip, 8f, UI51Build.Pad(0, 14, 0, 14), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(chip, true, false);
            var chipText = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(chip, "Text"), "MANO 3 DI 6", FontFace.CinzelBold, 12f,
                UI51Tokens.Gold, TextAlignmentOptions.Center, 2f));

            // Avviso (top 150): alto 46 r23, fondo .95, bordo rosso .55 (oro per gli altri avvisi), avatar 32 e due righe.
            var noticeHolder = Holder(root, "NoticeHolder");
            var notice = UI51Build.Place(UI51Build.Child(noticeHolder, "Notice"), new Vector2(0.5f, 1f), new Vector2(240f, 46f), Vector2.zero);
            notice.pivot = new Vector2(0.5f, 1f);
            var noticeShape = UI51Build.Solid(notice, UI51Tokens.Rgba(6, 13, 27, 0.95f), 23f, 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.55f),
                false, new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.5f)));
            var noticeRow = UI51Build.Row(notice, 10f, UI51Build.Pad(0, 16, 0, 8), TextAnchor.MiddleLeft, true, false);
            UI51Build.Fit(notice, true, false);
            var avatar = AvatarAt(notice, avatarPrefab, "Avatar", 32f, 2f);
            UI51Build.Layout(avatar, 32f, 32f);
            var lines = UI51Build.Child(notice, "Lines");
            UI51Build.Column(lines, 1f, UI51Build.Pad(0, 0, 0, 0), TextAnchor.MiddleLeft, true, true);
            var noticeTitle = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(lines, "Title"), "Marco_93 si è disconnesso",
                FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
            var noticeSub = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(lines, "Sub"), "Gioca un bot finché non rientra",
                FontFace.NunitoRegular, 10f, UI51Tokens.CreamA(0.6f), TextAlignmentOptions.MidlineLeft));

            // SCOPA! (blocco 390x320 a top 250, centro 12 sopra il centro dello schermo come sull'iPhone del mockup).
            var scopa = UI51Build.Place(UI51Build.Child(root, "Scopa"), new Vector2(0.5f, 0.5f), new Vector2(390f, 320f), new Vector2(0f, 12f * Unit));
            scopa.localScale = new Vector3(Unit, Unit, 1f);
            // radial-gradient(ellipse, nero-blu .7 -> 0 al 70%): il bagliore morbido tinto scuro.
            UI51Build.Image(UI51Build.Stretch(UI51Build.Child(scopa, "Veil")), glow, UI51Tokens.Rgba(3, 8, 18, 0.85f), false, false);
            var burst = UI51Build.Image(UI51Build.Place(UI51Build.Child(scopa, "Burst"), new Vector2(0f, 1f), new Vector2(320f, 320f),
                new Vector2(195f, -147.2f)), glow, Color.white, false, false).rectTransform;
            burst.pivot = new Vector2(0.5f, 0.5f);
            var sparks = new RectTransform[SparkTargets.Length];
            for (int i = 0; i < sparks.Length; i++)
            {
                sparks[i] = UI51Build.Place(UI51Build.Child(scopa, "Spark" + i), new Vector2(0f, 1f), new Vector2(8f, 8f), new Vector2(197f, -150f));
                sparks[i].pivot = new Vector2(0.5f, 0.5f);
                UI51Build.Solid(sparks[i], UI51Tokens.GoldLight, 4f);
            }
            // Parole a top 358 (108 nel blocco): SCOPA! 44 con l'ombra dura #6B4418 4 sotto, "Tu · +1" 14. Pivot al centro per l'entrata.
            var words = UI51Build.Place(UI51Build.Child(scopa, "Words"), new Vector2(0.5f, 1f), new Vector2(390f, 78f), new Vector2(0f, -147f));
            words.pivot = new Vector2(0.5f, 0.5f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(words, "Shade"), 0f, 0f, 4f, 56f), "SCOPA!",
                FontFace.CinzelBold, 44f, UI51Tokens.Hex("#6B4418"), TextAlignmentOptions.Center, 4f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(words, "Title"), 0f, 0f, 0f, 56f), "SCOPA!",
                FontFace.CinzelBold, 44f, UI51Tokens.GoldLight, TextAlignmentOptions.Center, 4f));
            var who = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(words, "Who"), 0f, 0f, 58f, 20f), "Tu · +1",
                FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            who.richText = true;

            // Tempo che scade (SPEC: il timer compare solo negli ultimi 5 s). Di un altro: alta 26 r13, fondo .9, bordo rosso .7, "5s" Cinzel
            // 13 bianco e il nome 11 #F08A8D, gap 6. Mio: alta 30 r15, fondo rosso .18, secondi Cinzel 15 e testo 12, gap 8. Le mette
            // UI51TableMoments vicino al banner di chi gioca.
            var timerHolder = Holder(root, "TimerHolder");
            var timer = TimePill(timerHolder, "TimerPill", 26f, UI51Tokens.Rgba(6, 13, 27, 0.9f), 6f, 10);
            var timerSecs = PillText(timer, "Secs", "5s", FontFace.CinzelBold, 13f, Color.white);
            var timerWho = PillText(timer, "Who", "Marco", FontFace.NunitoExtraBold, 11f, UI51Tokens.DangerText);
            var tempoHolder = Holder(root, "TempoHolder");
            var tempo = TimePill(tempoHolder, "TempoPill", 30f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.18f), 8f, 12);
            var tempoSecs = PillText(tempo, "Secs", "5", FontFace.CinzelBold, 15f, Color.white);
            var tempoText = PillText(tempo, "Text", "Gioca adesso, o la carta verrà scelta per te", FontFace.NunitoExtraBold, 12f, UI51Tokens.DangerText);

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "banners", banners);
                UI51Build.Ref(so, "timerHolder", timerHolder);
                UI51Build.Ref(so, "timerPill", timer);
                UI51Build.Ref(so, "timerSecs", timerSecs);
                UI51Build.Ref(so, "timerWho", timerWho);
                UI51Build.Ref(so, "tempoHolder", tempoHolder);
                UI51Build.Ref(so, "tempoPill", tempo);
                UI51Build.Ref(so, "tempoSecs", tempoSecs);
                UI51Build.Ref(so, "tempoText", tempoText);
                UI51Build.Ref(so, "chipHolder", chipHolder);
                UI51Build.Ref(so, "chip", chip);
                UI51Build.Ref(so, "chipShape", chipShape);
                UI51Build.Ref(so, "chipText", chipText);
                UI51Build.Ref(so, "noticeHolder", noticeHolder);
                UI51Build.Ref(so, "notice", notice);
                UI51Build.Ref(so, "noticeShape", noticeShape);
                UI51Build.Ref(so, "noticeRow", noticeRow);
                UI51Build.Ref(so, "noticeAvatar", avatar);
                UI51Build.Ref(so, "noticeTitle", noticeTitle);
                UI51Build.Ref(so, "noticeSub", noticeSub);
                UI51Build.Ref(so, "scopa", scopa);
                UI51Build.Ref(so, "scopaVeil", scopa.Find("Veil"));
                UI51Build.Ref(so, "scopaBurst", burst);
                UI51Build.Ref(so, "scopaWords", words);
                UI51Build.Ref(so, "scopaWho", who);
                UI51AccessBuilder.SetArray(so, "sparks", sparks);
                var targets = so.FindProperty("sparkTargets");
                targets.arraySize = SparkTargets.Length;
                for (int i = 0; i < SparkTargets.Length; i++) targets.GetArrayElementAtIndex(i).vector2Value = SparkTargets[i];
            });
            chip.gameObject.SetActive(false);
            notice.gameObject.SetActive(false);
            scopa.gameObject.SetActive(false);
            timer.gameObject.SetActive(false);
            tempo.gameObject.SetActive(false);

            foreach (var seat in seats) BuildOffline(seat);
        }

        /// <summary>Contenitore in scala del mockup, pivot in alto al centro; la y la mette UI51TableMoments sotto al banner in alto.</summary>
        static RectTransform Holder(RectTransform root, string name)
        {
            var rt = UI51Build.Place(UI51Build.Child(root, name), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.localScale = new Vector3(Unit, Unit, 1f);
            return rt;
        }

        /// <summary>Pillola del tempo, larga quanto il contenuto, bordo rosso .7.</summary>
        static RectTransform TimePill(RectTransform holder, string name, float height, Color fill, float gap, int pad)
        {
            var pill = UI51Build.Place(UI51Build.Child(holder, name), new Vector2(0.5f, 1f), new Vector2(120f, height), Vector2.zero);
            pill.pivot = new Vector2(0.5f, 1f);
            UI51Build.Solid(pill, fill, height * 0.5f, 1f, UI51Tokens.WithAlpha(UI51Tokens.Danger, 0.7f));
            UI51Build.Row(pill, gap, UI51Build.Pad(0, pad, 0, pad), TextAnchor.MiddleCenter, true, true).childForceExpandHeight = true;
            UI51Build.Fit(pill, true, false);
            return pill;
        }

        static TextMeshProUGUI PillText(RectTransform pill, string name, string text, FontFace face, float size, Color color) =>
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(pill, name), text, face, size, color, TextAlignmentOptions.Center));

        /// <summary>
        /// Mockup MomentoDisconnesso: velo .6 sulla pillola del banner e Wi-Fi barrato bianco in un tondo rosso 22 (a 10 dal bordo
        /// destro; sui banner verticali in alto a destra, sull'avatar).
        /// </summary>
        static void BuildOffline(Transform seat)
        {
            // Banner_*/UI51Banner/PlayerBanner_* (istanza del prefab): velo e icona come ultimi figli della pillola.
            var holder = seat.Find("UI51Banner");
            var component = holder != null ? holder.GetComponentInChildren<PlayerBanner>(true) : null;
            var banner = component != null ? component.transform : null;
            var bg = banner != null ? banner.Find("Bg") : null;
            if (bg == null)
            {
                Debug.LogError($"{Tag} {seat.name}: manca UI51Banner/PlayerBanner/Bg. Icona del disconnesso non messa.");
                return;
            }
            var shape = bg.GetComponent<UI51Shape>();
            var rt = (RectTransform)banner;
            bool vertical = rt.sizeDelta.y > rt.sizeDelta.x;
            var offline = UI51Build.Stretch(UI51Build.Child(banner, "Offline"));
            offline.SetAsLastSibling();
            UI51Build.Solid(offline, UI51Tokens.Rgba(3, 7, 16, 0.6f), shape != null ? shape.radii.x : 25f);
            var icon = vertical
                ? UI51Build.Place(UI51Build.Child(offline, "Icon"), new Vector2(1f, 1f), new Vector2(22f, 22f), new Vector2(-4f, -4f))
                : UI51Build.Place(UI51Build.Child(offline, "Icon"), new Vector2(1f, 0.5f), new Vector2(22f, 22f), new Vector2(-10f, 0f));
            icon.pivot = vertical ? new Vector2(1f, 1f) : new Vector2(1f, 0.5f);
            UI51Build.Solid(icon, UI51Tokens.Danger, 11f);
            // SVG 24: M2 8 Q12 0 22 8, M6 13 Q12 8 18 13, M3 3 L21 21; tratto bianco 2.6 a 12 px.
            var wifi = UI51Build.Center(UI51Build.Child(icon, "Wifi"), 12f, 12f);
            var box = new Vector2(24f, 24f);
            Line(wifi, "W1", box, UI51Polyline.Quad(new Vector2(2f, 8f), new Vector2(12f, 0f), new Vector2(22f, 8f), 16));
            Line(wifi, "W2", box, UI51Polyline.Quad(new Vector2(6f, 13f), new Vector2(12f, 8f), new Vector2(18f, 13f)));
            Line(wifi, "Slash", box, new Vector2(3f, 3f), new Vector2(21f, 21f));
            UI51Build.Wire(component, so => UI51Build.Ref(so, "m_Offline", offline.gameObject));
            offline.gameObject.SetActive(false);
        }

        static void Line(RectTransform parent, string name, Vector2 viewBox, params Vector2[] points)
        {
            var line = UI51Build.GetOrAdd<UI51Polyline>(UI51Build.Stretch(UI51Build.Child(parent, name)));
            line.Set(viewBox, 2.6f, points);
            line.color = Color.white;
            line.raycastTarget = false;
        }
    }
}
