using Project51.UIV2.Core;
using Project51.Unity;
using Project51.Unity.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// UI51 Fase 7 su GameScene, dentro "Tools/UI51/Build Fase 5 (Tavolo 1v1)": fine smazzata (mockup FineSmazzata) in
    /// GamePresentationV2/RoundResults/UI51 e fine partita (FinePartita) in MatchResults/UI51, schermi 390x844 come il
    /// Sorteggio. Le colonne e le posizioni che cambiano fra 1v1, 2v2 e 1v3 le mette UI51ResultsView; qui i nodi con le
    /// misure del mockup. Il vecchio aspetto resta in scena spento; MatchResultsV2 usa la nuova grafica quando View e' collegato.
    /// </summary>
    public static partial class UI51TableBuilder
    {
        static readonly Color PanelTop = UI51Tokens.Rgba(12, 26, 50, 0.92f), PanelBottom = UI51Tokens.Rgba(6, 13, 27, 0.96f);
        static readonly string[] RowIcons = { null, "ic_coin", null, "medal_sun", "medal_trophy", "medal_club", null, "pugno" };
        static readonly string[] RaceColors = { "#C4922F", "#FCE29A", "#1B3A7A", "#4F80E8", "#0E6B4F", "#27B585", "#7A151D", "#E5484D" };
        static readonly string[] ConfettiColors = { "#F3C969", "#FCE29A", "#27B585", "#4F80E8", "#E5484D", "#FFF6DC" };

        static void BuildResults(PlayerBannerManager banners)
        {
            var results = UnityEngine.Object.FindObjectOfType<MatchResultsV2>(true);
            var cards = UnityEngine.Object.FindObjectOfType<CardViewManager>(true);
            var bg = UI51Build.Sprite("Backgrounds", "home_bg_blur");
            var avatarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI51PrefabBuilder.PrefabPath("AvatarFrame"));
            var ribbonGold = UI51Build.Sprite("Common", "ribbon");
            var ribbonGray = UI51Build.Sprite("Common", "ribbon_gray");
            var crown = UI51Build.Sprite("Common", "crown_gold");
            var raysSprite = UI51Build.Sprite("Common", "rays_conic");
            var glow = UI51Build.Sprite("Common", "Bagliore_morbido");
            var icons = new Sprite[RowIcons.Length];
            for (int i = 0; i < icons.Length; i++) if (RowIcons[i] != null) icons[i] = UI51Build.Sprite("Common", RowIcons[i]);
            if (results == null || cards == null || bg == null || avatarPrefab == null || ribbonGold == null || ribbonGray == null ||
                crown == null || raysSprite == null || glow == null || icons[1] == null || icons[3] == null || icons[4] == null ||
                icons[5] == null || icons[7] == null)
            {
                Debug.LogError($"{Tag} Manca un pezzo dei risultati (MatchResultsV2, CardViewManager, home_bg_blur, AvatarFrame, ribbon, " +
                               "ribbon_gray, crown_gold, rays_conic, Bagliore_morbido, icone delle righe). Fase 7 non costruita.");
                return;
            }
            bool roundWas = results.RoundPanel.activeSelf, matchWas = results.MatchPanel.activeSelf;
            results.RoundPanel.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            results.MatchPanel.SetActive(true);

            var view = UI51Build.GetOrAdd<UI51ResultsView>(results);
            var roundRoot = BuildRoundEnd(results, view, bg, avatarPrefab, icons);
            BuildTie(results, view, roundRoot, avatarPrefab);
            var matchRoot = BuildMatchEnd(results, view, bg, avatarPrefab, ribbonGold, ribbonGray, crown, raysSprite, glow);
            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "banners", banners);
                UI51Build.Ref(so, "cards", cards);
                UI51Build.Ref(so, "round", roundRoot.gameObject);
                UI51Build.Ref(so, "match", matchRoot.gameObject);
            });
            UI51Build.Wire(results, so =>
            {
                UI51Build.Ref(so, "View", view);
                UI51Build.Ref(so, "RoundBlur", null); // niente foto del tavolo: fondo del mockup
                UI51Build.Ref(so, "TrophyBurst", null);
            });
            foreach (var name in new[] { "Blur", "Veil", "Dim", "Design" })
            {
                var t = results.RoundPanel.transform.Find(name);
                if (t != null) t.gameObject.SetActive(false);
                t = results.MatchPanel.transform.Find(name);
                if (t != null) t.gameObject.SetActive(false);
            }
            results.RoundPanel.SetActive(roundWas);
            results.MatchPanel.SetActive(matchWas);
        }

        /// <summary>Schermo del mockup FineSmazzata: titolo, teste di colonna, 8 righe + IN QUESTA MANO, corsa al 51, pulsante.</summary>
        static RectTransform BuildRoundEnd(MatchResultsV2 results, UI51ResultsView view, Sprite bg, GameObject avatarPrefab, Sprite[] icons)
        {
            var safe = UI51AccessBuilder.BuildScreen(results.RoundPanel.transform, bg, null);
            var root = (RectTransform)safe.parent;
            root.Find("Bg").GetComponent<Image>().color = new Color(0.78f, 0.78f, 0.78f, 1f); // brightness .32 del mockup, lo sfocato e' a .41

            var mode = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Mode"), 20f, 20f, 34f, 15f),
                "PARTITA 1 VS 1 · MANO 1", FontFace.CinzelSemiBold, 11f, UI51Tokens.GoldA(0.8f), TextAlignmentOptions.Top, 3f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Title"), 20f, 20f, 53f, 33f),
                "Fine smazzata", FontFace.CinzelBold, 24f, UI51Tokens.Cream, TextAlignmentOptions.Top));

            // Teste di colonna (top 104): avatar 40 (34 a quattro) con anello, nome sotto; UI51ResultsView le mette in colonna.
            var headsRt = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Heads"), 16f, 16f, 104f, 62f);
            var heads = new RectTransform[4];
            var headAvatars = new AvatarFrame[8];
            var headNames = new TextMeshProUGUI[4];
            for (int j = 0; j < 4; j++)
            {
                var head = heads[j] = UI51Build.Place(UI51Build.Child(headsRt, "Head" + j), new Vector2(1f, 1f), new Vector2(92f, 62f),
                    new Vector2(-46f - (3 - j) * 98f, 0f));
                head.pivot = new Vector2(0.5f, 1f);
                headAvatars[j * 2] = AvatarAt(head, avatarPrefab, "AvatarA", 40f, 2f);
                headAvatars[j * 2 + 1] = AvatarAt(head, avatarPrefab, "AvatarB", 40f, 2f);
                headNames[j] = UI51Build.Text(TopAt(UI51Build.Child(head, "Name"), 92f, 16f, 45f), "Tu", FontFace.NunitoExtraBold, 11f,
                    UI51Tokens.Cream, TextAlignmentOptions.Top);
                headNames[j].enableWordWrapping = false; // nomi lunghi: "..." in fondo
            }

            // Pannello delle righe (top 172): bordo 1 + padding 4/10, righe 44 con la riga d'oro dentro (border-box), totale 50.
            var panel = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Panel"), 16f, 16f, 172f, 412f);
            UI51Build.Shape(panel, UI51Shape.Linear((PanelTop, 0f), (PanelBottom, 1f)), 180f, UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldA(0.35f));
            var rows = new RectTransform[8];
            var rowIcons = new Image[8];
            var rowDetails = new TextMeshProUGUI[8];
            var cells = new RectTransform[36];
            var pills = new RectTransform[32];
            var pillTexts = new TextMeshProUGUI[32];
            var dashes = new GameObject[32];
            for (int r = 0; r < 8; r++)
            {
                var row = rows[r] = UI51AccessBuilder.TopBand(UI51Build.Child(panel, "Row" + r), 11f, 11f, 5f + r * 44f, 44f);
                UI51Build.Solid(UI51AccessBuilder.TopBand(UI51Build.Child(row, "Line"), -10f, -10f, 43f, 1f), UI51Tokens.GoldA(0.1f), 0f);
                bool card = icons[r] == null; // Carte, Settebello e Scope: carte del mazzo in uso (17x26), le altre tonde 24
                float iw = card ? 17f : 24f, ih = card ? 26f : 24f;
                rowIcons[r] = UI51Build.Image(Box(UI51Build.Child(row, "Icon"), 0f, (44f - ih) * 0.5f, iw, ih), icons[r], Color.white, false, !card);
                float lx = iw + 10f;
                UI51AccessBuilder.NoWrap(UI51Build.Text(Box(UI51Build.Child(row, "Label"), lx, 5f, 120f, 18f),
                    Project51.Core.ResultsSheet.Labels[r], FontFace.NunitoExtraBold, 13f, UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft));
                var detail = UI51Build.Place(UI51Build.Child(row, "Detail"), new Vector2(0f, 1f), new Vector2(140f - lx, 14f), new Vector2(lx, -24f));
                rowDetails[r] = UI51Build.Text(detail, "", FontFace.NunitoRegular, 10f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.MidlineLeft);
                rowDetails[r].enableWordWrapping = false;
                for (int j = 0; j < 4; j++)
                {
                    var cell = cells[r * 4 + j] = Cell(row, j);
                    var pill = pills[r * 4 + j] = UI51Build.Center(UI51Build.Child(cell, "Pill"), 38f, 26f);
                    UI51Build.Shape(pill, UI51Shape.Linear((UI51Tokens.GoldLight, 0f), (UI51Tokens.GoldDark, 1f)), 180f, UI51Tokens.Radii(13f), 0f, Color.clear);
                    pillTexts[r * 4 + j] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(pill, "Text")), "+1",
                        FontFace.CinzelBold, 13f, UI51Tokens.OnGold, TextAlignmentOptions.Center));
                    var dash = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Center(UI51Build.Child(cell, "Dash"), 30f, 26f), "—",
                        FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.25f), TextAlignmentOptions.Center));
                    dashes[r * 4 + j] = dash.gameObject;
                }
            }
            var totalsRow = UI51AccessBuilder.TopBand(UI51Build.Child(panel, "Totals"), 11f, 11f, 5f + 8 * 44f, 50f);
            UI51AccessBuilder.NoWrap(UI51Build.Text(Box(UI51Build.Child(totalsRow, "Label"), 0f, 0f, 150f, 50f), "IN QUESTA MANO",
                FontFace.CinzelBold, 12f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            var handTotals = new TextMeshProUGUI[4];
            for (int j = 0; j < 4; j++)
            {
                var cell = cells[32 + j] = Cell(totalsRow, j);
                cell.sizeDelta = new Vector2(92f, 30f);
                handTotals[j] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(cell, "Value")), "+0",
                    FontFace.CinzelBold, 20f, UI51Tokens.Cream, TextAlignmentOptions.Center));
            }

            // Corsa al 51 (top 600, a quattro 596): posizioni e altezza da UI51ResultsView.
            var race = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Race"), 16f, 16f, 600f, 143f);
            UI51Build.Shape(race, UI51Shape.Linear((PanelTop, 0f), (PanelBottom, 1f)), 180f, UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldA(0.35f));
            var raceTitle = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(race, "Title"), 16f, 16f, 12f, 15f),
                "CORSA AL 51", FontFace.CinzelSemiBold, 11f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            var raceNote = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(race, "Note"), 120f, 16f, 12f, 15f),
                "", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineRight));
            var raceRows = new RectTransform[4];
            var raceNames = new TextMeshProUGUI[4];
            var raceTotals = new TextMeshProUGUI[4];
            var raceFills = new RectTransform[4];
            for (int j = 0; j < 4; j++)
            {
                var row = raceRows[j] = UI51AccessBuilder.TopBand(UI51Build.Child(race, "Row" + j), 16f, 16f, 39f + j * 30f, 18f);
                raceNames[j] = UI51Build.Text(Box(UI51Build.Child(row, "Name"), 0f, 0f, 44f, 18f), "Tu", FontFace.NunitoExtraBold, 12f,
                    UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
                raceNames[j].enableWordWrapping = false;
                var track = UI51Build.Stretch(UI51Build.Child(row, "Track"), 54f, 4f, 72f, 4f);
                UI51Build.Solid(track, UI51Tokens.WhiteA(0.1f), 5f);
                var fill = raceFills[j] = UI51Build.Child(track, "Fill");
                fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.6f, 1f); fill.pivot = new Vector2(0f, 0.5f);
                fill.offsetMin = fill.offsetMax = Vector2.zero;
                UI51Build.Shape(fill, UI51Shape.Linear((UI51Tokens.Hex(RaceColors[j * 2]), 0f), (UI51Tokens.Hex(RaceColors[j * 2 + 1]), 1f)), 90f,
                    UI51Tokens.Radii(5f), 0f, Color.clear);
                raceTotals[j] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(row, "Total"), new Vector2(1f, 0.5f),
                    new Vector2(62f, 18f), Vector2.zero), "0", FontFace.CinzelBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.MidlineRight));
            }

            // PROSSIMA SMAZZATA (54) e "Si riparte da sola tra N secondi", 26 dal fondo.
            var footer = BottomBand(UI51Build.Child(safe, "Footer"), 16f, 26f, 77f);
            var next = UI51Build.Child(footer, "Next");
            UI51PrefabBuilder.GoldBody(next.gameObject, 358f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "PROSSIMA SMAZZATA",
                new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.45f)));
            UI51AccessBuilder.TopBand(next, 0f, 0f, 0f, 54f);
            var caption = UI51AccessBuilder.NoWrap(UI51Build.Text(BottomBand(UI51Build.Child(footer, "Caption"), 0f, 0f, 15f),
                "Si riparte da sola tra 10 secondi", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "roundMode", mode);
                UI51AccessBuilder.SetArray(so, "heads", heads);
                UI51AccessBuilder.SetArray(so, "headAvatars", headAvatars);
                UI51AccessBuilder.SetArray(so, "headNames", headNames);
                UI51AccessBuilder.SetArray(so, "rows", rows);
                UI51AccessBuilder.SetArray(so, "rowIcons", rowIcons);
                UI51AccessBuilder.SetArray(so, "rowDetails", rowDetails);
                UI51AccessBuilder.SetArray(so, "cells", cells);
                UI51AccessBuilder.SetArray(so, "pills", pills);
                UI51AccessBuilder.SetArray(so, "pillTexts", pillTexts);
                UI51AccessBuilder.SetArray(so, "dashes", dashes);
                UI51AccessBuilder.SetArray(so, "handTotals", handTotals);
                UI51Build.Ref(so, "race", race);
                UI51Build.Ref(so, "raceTitle", raceTitle);
                UI51Build.Ref(so, "raceNote", raceNote);
                UI51AccessBuilder.SetArray(so, "raceRows", raceRows);
                UI51AccessBuilder.SetArray(so, "raceNames", raceNames);
                UI51AccessBuilder.SetArray(so, "raceTotals", raceTotals);
                UI51AccessBuilder.SetArray(so, "raceFills", raceFills);
                UI51Build.Ref(so, "roundFooter", footer);
                UI51Build.Ref(so, "roundCaption", caption);
            });
            UI51Build.Wire(results, so =>
            {
                UI51Build.Ref(so, "RoundContinue", next.GetComponent<Button>());
                UI51Build.Ref(so, "RoundContinueLabel", next.Find("Label").GetComponent<TextMeshProUGUI>());
            });
            return root;
        }

        /// <summary>Schermo del mockup FinePartita: raggi, coriandoli, nastro, sfida a due o classifica, XP, statistiche, pulsanti.</summary>
        static RectTransform BuildMatchEnd(MatchResultsV2 results, UI51ResultsView view, Sprite bg, GameObject avatarPrefab, Sprite ribbonGold,
            Sprite ribbonGray, Sprite crown, Sprite raysSprite, Sprite glow)
        {
            var safe = UI51AccessBuilder.BuildScreen(results.MatchPanel.transform, bg, null);
            var root = (RectTransform)safe.parent;
            root.Find("Bg").GetComponent<Image>().color = new Color(0.78f, 0.78f, 0.78f, 1f);

            // Raggi (centro 195,170 nel mockup, 560 sfumati al 65%): l'immagine e' gia' sfumata, 420 basta. Girano in 18 s.
            var rays = UI51AccessBuilder.CenterAt(UI51Build.Child(safe, "Rays"), 195f, 170f, 420f, 420f);
            rays.SetAsFirstSibling();
            var raysImage = UI51Build.Image(rays, raysSprite, UI51Tokens.WhiteA(0.16f));

            // Coriandoli del mockup: 18 pezzi 8x12 r2, x = (i*53) % 380, sei colori. Li fa cadere MatchResultsV2.PlayConfetti.
            var confetti = UI51Build.Stretch(UI51Build.Child(safe, "Confetti"));
            for (int i = 0; i < 18; i++)
                UI51Build.Solid(UI51Build.Place(UI51Build.Child(confetti, "Piece" + i), new Vector2(0f, 1f), new Vector2(8f, 12f),
                    new Vector2((i * 53) % 380 + 4f, 20f)), UI51Tokens.Hex(ConfettiColors[i % ConfettiColors.Length]), 2f);
            BuildInstant(view, safe, confetti, avatarPrefab, glow);

            // Titolo (top 44): modo, nastro 260x84 con VITTORIA/SCONFITTA a 19. Pivot al centro per il pop.
            var title = UI51Build.Place(UI51Build.Child(safe, "Title"), new Vector2(0.5f, 1f), new Vector2(260f, 100f), new Vector2(0f, -94f));
            title.pivot = new Vector2(0.5f, 0.5f);
            var matchMode = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(title, "Mode"), -40f, -40f, 0f, 14f),
                "PARTITA 1 VS 1", FontFace.CinzelSemiBold, 10f, UI51Tokens.GoldA(0.8f), TextAlignmentOptions.Top, 3f));
            var ribbon = UI51Build.Image(UI51AccessBuilder.TopBand(UI51Build.Child(title, "Ribbon"), 0f, 0f, 16f, 84f), ribbonGold, Color.white, false, false);
            var titleText = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(ribbon.rectTransform, "Text"), 40f, 40f, 19f, 36f),
                "VITTORIA", FontFace.CinzelBold, 24f, UI51Tokens.Hex("#FFF6DC"), TextAlignmentOptions.Center, 3f));
            // Dentro alla faccia del nastro (le pieghe del grigio sono piu' larghe): SCONFITTA scende fino a 18.
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 18f;
            titleText.fontSizeMax = 24f;

            // Sfida a due (top 150): lati 130, passo 18; avatar 84 (68 accavallati per le coppie), corona e alone al vincitore.
            var duel = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Duel"), 0f, 0f, 150f, 150f);
            var sides = new RectTransform[2];
            var sideAvatars = new AvatarFrame[4];
            var crowns = new GameObject[2];
            var glows = new GameObject[2];
            var sideNames = new TextMeshProUGUI[2];
            var sideScores = new TextMeshProUGUI[2];
            for (int s = 0; s < 2; s++)
            {
                var side = sides[s] = UI51Build.Place(UI51Build.Child(duel, "Side" + s), new Vector2(0.5f, 1f), new Vector2(130f, 150f),
                    new Vector2(s == 0 ? -74f : 74f, 0f));
                var g = UI51Build.Image(UI51Build.Place(UI51Build.Child(side, "Glow"), new Vector2(0.5f, 1f), new Vector2(136f, 136f),
                    new Vector2(0f, -42f)), glow, UI51Tokens.GoldA(0.45f), false, false); // box-shadow 0 0 26px oro .6
                g.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                glows[s] = g.gameObject;
                sideAvatars[s * 2] = AvatarAt(side, avatarPrefab, "AvatarA", 84f, 3f);
                sideAvatars[s * 2 + 1] = AvatarAt(side, avatarPrefab, "AvatarB", 84f, 3f);
                var c = UI51Build.Image(UI51Build.Place(UI51Build.Child(side, "Crown"), new Vector2(0.5f, 1f), new Vector2(40f, 37f),
                    new Vector2(0f, 14f)), crown, Color.white);
                c.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                c.transform.SetAsLastSibling();
                crowns[s] = c.gameObject;
                sideNames[s] = UI51Build.Text(TopAt(UI51Build.Child(side, "Name"), 130f, 18f, 90f), "Tu", FontFace.NunitoExtraBold, 13f,
                    UI51Tokens.Cream, TextAlignmentOptions.Top);
                sideNames[s].enableWordWrapping = false;
                sideScores[s] = UI51AccessBuilder.NoWrap(UI51Build.Text(TopAt(UI51Build.Child(side, "Score"), 130f, 34f, 114f), "0",
                    FontFace.CinzelBold, 32f, UI51Tokens.GoldLight, TextAlignmentOptions.Top));
            }

            // Classifica a quattro (top 150): righe 52 con medaglia 26, avatar 38, nome, punti.
            var rank = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Rank"), 20f, 20f, 150f, 226f);
            UI51Build.Shape(rank, UI51Shape.Linear((PanelTop, 0f), (PanelBottom, 1f)), 180f, UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldA(0.35f));
            var medals = new UI51Shape[4];
            var rankPos = new TextMeshProUGUI[4];
            var rankNames = new TextMeshProUGUI[4];
            var rankScores = new TextMeshProUGUI[4];
            var rankAvatars = new AvatarFrame[4];
            for (int i = 0; i < 4; i++)
            {
                var row = UI51AccessBuilder.TopBand(UI51Build.Child(rank, "Row" + i), 11f, 11f, 7f + i * 53f, 52f);
                UI51Build.Solid(UI51AccessBuilder.TopBand(UI51Build.Child(row, "Line"), -10f, -10f, 52f, 1f), UI51Tokens.GoldA(0.1f), 0f);
                var medal = Box(UI51Build.Child(row, "Medal"), 0f, 13f, 26f, 26f);
                medals[i] = UI51Build.Solid(medal, UI51Tokens.Gold, 13f);
                rankPos[i] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(medal, "Pos")), (i + 1).ToString(),
                    FontFace.CinzelBold, 12f, UI51Tokens.BadgeRing, TextAlignmentOptions.Center));
                rankAvatars[i] = AvatarAt(row, avatarPrefab, "Avatar", 38f, 2f);
                var art = (RectTransform)rankAvatars[i].transform;
                art.anchorMin = art.anchorMax = new Vector2(0f, 0.5f);
                art.anchoredPosition = new Vector2(55f, 0f);
                rankNames[i] = UI51Build.Text(Box(UI51Build.Child(row, "Name"), 84f, 0f, 180f, 52f), "Tu", FontFace.NunitoBold, 14f,
                    UI51Tokens.Cream, TextAlignmentOptions.MidlineLeft);
                rankNames[i].enableWordWrapping = false;
                rankScores[i] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(row, "Score"), new Vector2(1f, 0.5f),
                    new Vector2(70f, 52f), Vector2.zero), "0", FontFace.CinzelBold, 18f, UI51Tokens.Cream, TextAlignmentOptions.MidlineRight));
            }

            // Ricompense (top 372, 392 con la classifica): XP e, per chi ha un account, le monete date dal server (UI51RewardsExtra).
            var rewards = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Rewards"), 20f, 20f, 372f, 77f);
            UI51Build.Shape(rewards, UI51Shape.Linear((PanelTop, 0f), (PanelBottom, 1f)), 180f, UI51Tokens.Radii(18f), 1f, UI51Tokens.GoldA(0.35f));
            UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rewards, "Caption"), 16f, 16f, 15f, 15f), "RICOMPENSE",
                FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.MidlineLeft, 2f));
            var levelText = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(rewards, "Level"), 120f, 16f, 15f, 15f),
                "Livello 1", FontFace.NunitoRegular, 11f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.MidlineRight));
            var xpRow = UI51AccessBuilder.TopBand(UI51Build.Child(rewards, "Xp"), 16f, 16f, 42f, 18f);
            var xpGain = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(xpRow, "Gain"), new Vector2(0f, 0.5f),
                new Vector2(64f, 18f), Vector2.zero), "+40 XP", FontFace.CinzelBold, 13f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineLeft));
            var xpTrack = UI51Build.Stretch(UI51Build.Child(xpRow, "Track"), 74f, 5f, 72f, 5f);
            UI51Build.Solid(xpTrack, UI51Tokens.WhiteA(0.12f), 4f);
            var xpFill = UI51Build.Child(xpTrack, "Fill");
            xpFill.anchorMin = Vector2.zero; xpFill.anchorMax = new Vector2(0.62f, 1f); xpFill.pivot = new Vector2(0f, 0.5f);
            xpFill.offsetMin = xpFill.offsetMax = Vector2.zero;
            UI51Build.Shape(xpFill, UI51Shape.Linear((UI51Tokens.GoldDark, 0f), (UI51Tokens.GoldLight, 1f)), 90f, UI51Tokens.Radii(4f), 0f, Color.clear);
            var xpText = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Place(UI51Build.Child(xpRow, "Text"), new Vector2(1f, 0.5f),
                new Vector2(62f, 18f), Vector2.zero), "0/100", FontFace.NunitoBold, 11f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.MidlineRight));
            var coinsRow = UI51AccessBuilder.TopBand(UI51Build.Child(rewards, "UI51RewardsExtra"), 16f, 16f, 72f, 18f);
            UI51Build.Image(UI51Build.Place(UI51Build.Child(coinsRow, "Icon"), new Vector2(0f, 0.5f), new Vector2(18f, 18f), Vector2.zero),
                UI51Build.Sprite("Common", "ic_coin"), Color.white);
            var coinsLabel = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Stretch(UI51Build.Child(coinsRow, "Label"), 26f, 0f, 0f, 0f), "+40 monete",
                FontFace.CinzelBold, 13f, UI51Tokens.GoldLight, TextAlignmentOptions.MidlineLeft));
            coinsRow.gameObject.SetActive(false);

            // Statistiche: 3 riquadri (Scope, Accusi, Settebelli), passo 8; la y la mette UI51ResultsView sotto alle ricompense.
            var stats = UI51AccessBuilder.TopBand(UI51Build.Child(safe, "Stats"), 20f, 20f, 463f, 60f);
            var statValues = new TextMeshProUGUI[3];
            string[] statLabels = { "Scope", "Accusi", "Settebelli" };
            for (int i = 0; i < 3; i++)
            {
                // Terzi della fila col passo 8, cosi' seguono la larghezza dell'area sicura.
                var t = UI51Build.Child(stats, "Tile" + i);
                t.anchorMin = new Vector2(i / 3f, 0f);
                t.anchorMax = new Vector2((i + 1) / 3f, 1f);
                t.pivot = new Vector2(0.5f, 0.5f);
                t.offsetMin = new Vector2(i * 8f / 3f, 0f);
                t.offsetMax = new Vector2((i * 8f - 16f) / 3f, 0f);
                UI51Build.Shape(t, UI51Shape.Linear((PanelTop, 0f), (PanelBottom, 1f)), 180f, UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.35f));
                statValues[i] = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(t, "Value"), 0f, 0f, 9f, 25f), "0",
                    FontFace.CinzelBold, 18f, UI51Tokens.Cream, TextAlignmentOptions.Center));
                UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(t, "Label"), 0f, 0f, 35f, 14f), statLabels[i],
                    FontFace.NunitoBold, 10f, UI51Tokens.CreamA(0.55f), TextAlignmentOptions.Center));
            }

            // RIVINCITA (54) e Torna alla Home (46), 26 dal fondo.
            var footer = BottomBand(UI51Build.Child(safe, "Footer"), 20f, 26f, 110f);
            var again = UI51Build.Child(footer, "Rematch");
            UI51PrefabBuilder.GoldBody(again.gameObject, 350f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "RIVINCITA",
                new UI51Shadow(0f, 10f, 24f, UI51Tokens.BlackA(0.45f)));
            UI51AccessBuilder.TopBand(again, 0f, 0f, 0f, 54f);
            var home = UI51Build.Child(footer, "Home");
            UI51PrefabBuilder.ButtonBody(home.gameObject, 350f, 46f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.5f),
                FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.Gold, "Torna alla Home");
            BottomBand(home, 0f, 0f, 46f);

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "rays", rays);
                UI51Build.Ref(so, "raysImage", raysImage);
                UI51Build.Ref(so, "title", title);
                UI51Build.Ref(so, "matchMode", matchMode);
                UI51Build.Ref(so, "titleText", titleText);
                UI51Build.Ref(so, "ribbon", ribbon);
                UI51Build.Ref(so, "ribbonGold", ribbonGold);
                UI51Build.Ref(so, "ribbonGray", ribbonGray);
                UI51Build.Ref(so, "duel", duel);
                UI51AccessBuilder.SetArray(so, "sides", sides);
                UI51AccessBuilder.SetArray(so, "sideAvatars", sideAvatars);
                UI51AccessBuilder.SetArray(so, "crowns", crowns);
                UI51AccessBuilder.SetArray(so, "glows", glows);
                UI51AccessBuilder.SetArray(so, "sideNames", sideNames);
                UI51AccessBuilder.SetArray(so, "sideScores", sideScores);
                UI51Build.Ref(so, "rank", rank);
                UI51AccessBuilder.SetArray(so, "medals", medals);
                UI51AccessBuilder.SetArray(so, "rankPos", rankPos);
                UI51AccessBuilder.SetArray(so, "rankNames", rankNames);
                UI51AccessBuilder.SetArray(so, "rankScores", rankScores);
                UI51AccessBuilder.SetArray(so, "rankAvatars", rankAvatars);
                UI51Build.Ref(so, "rewards", rewards);
                UI51Build.Ref(so, "levelText", levelText);
                UI51Build.Ref(so, "xpGain", xpGain);
                UI51Build.Ref(so, "coinsRow", coinsRow.gameObject);
                UI51Build.Ref(so, "coinsLabel", coinsLabel);
                UI51Build.Ref(so, "xpText", xpText);
                UI51Build.Ref(so, "xpTrack", xpTrack);
                UI51Build.Ref(so, "xpFill", xpFill);
                UI51Build.Ref(so, "stats", stats);
                UI51AccessBuilder.SetArray(so, "statValues", statValues);
                UI51Build.Ref(so, "matchFooter", footer);
            });
            UI51Build.Wire(results, so =>
            {
                UI51Build.Ref(so, "Rematch", again.GetComponent<Button>());
                UI51Build.Ref(so, "RematchLabel", again.Find("Label").GetComponent<TextMeshProUGUI>());
                UI51Build.Ref(so, "MatchMenu", home.GetComponent<Button>());
                UI51Build.Ref(so, "ConfettiRoot", confetti);
            });
            confetti.gameObject.SetActive(false);
            return root;
        }

        /// <summary>
        /// Spareggio (mockup MomentiPartita) sopra al fine smazzata: velo .55, scheda 342 col bordo oro .5, padding 22 20 20:
        /// SPAREGGIO, "Parità a N!" 22, avatar 62 dei primi con "=" e punti 24, testo 13, pulsante d'oro 50.
        /// </summary>
        static void BuildTie(MatchResultsV2 results, UI51ResultsView view, RectTransform roundRoot, GameObject avatarPrefab)
        {
            var root = UI51Build.Stretch(UI51Build.Child(roundRoot, "Tie"));
            root.SetAsLastSibling();
            root.gameObject.SetActive(true); // TMP su oggetti spenti lancia eccezioni
            var scrim = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(root, "Scrim")), UI51Tokens.Rgba(3, 7, 16, 0.55f), 0f, 0f, default, true);
            var scrimButton = UI51Build.Button(scrim, scrim);
            var safe = UI51Build.Child(root, "Safe");
            var fit = UI51Build.GetOrAdd<DesignCanvasFit>(safe);
            fit.Reference = UI51Tokens.ReferenceResolution;
            fit.Fill = true;
            safe.anchorMin = safe.anchorMax = safe.pivot = new Vector2(0.5f, 0.5f);
            safe.anchoredPosition = Vector2.zero;
            safe.sizeDelta = UI51Tokens.ReferenceResolution;

            var card = UI51Build.Place(UI51Build.Child(safe, "Card"), new Vector2(0.5f, 0.5f), new Vector2(342f, 344f), Vector2.zero);
            UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(22f), 1f, UI51Tokens.GoldA(0.5f), true, UI51Tokens.ShadowDialog);
            UI51MetaBuilder.Stack(card, 0f, UI51Build.Pad(23, 21, 21, 21)); // 22 20 20 + 1 di bordo
            UI51Build.Fit(card, false, true);
            TextLine(card, "Cap", "SPAREGGIO", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, 14f, 3f);
            UI51MetaBuilder.Gap(card, "Gap1", 6f);
            var title = TextLine(card, "Title", "Parità a 53!", FontFace.CinzelBold, 22f, UI51Tokens.Cream, 30f, 0f);
            UI51MetaBuilder.Gap(card, "Gap2", 16f);

            // Fila centrata, passo 18: lati larghi quanto gli avatar (UI51ResultsView), "=" in mezzo all'altezza della colonna.
            var row = UI51Build.Child(card, "Row");
            UI51Build.Layout(row, -1f, 100f);
            var rowGroup = UI51Build.Row(row, 18f, null, TextAnchor.MiddleCenter, true, false);
            var sides = new RectTransform[4];
            var avatars = new AvatarFrame[8];
            var scores = new TextMeshProUGUI[4];
            var equals = new GameObject[3];
            for (int j = 0; j < 4; j++)
            {
                var side = sides[j] = UI51Build.Size(UI51Build.Child(row, "Side" + j), 62f, 100f);
                UI51Build.Layout(side, 62f, 100f);
                avatars[j * 2] = AvatarAt(side, avatarPrefab, "AvatarA", 62f, 2f);
                avatars[j * 2 + 1] = AvatarAt(side, avatarPrefab, "AvatarB", 62f, 2f);
                scores[j] = UI51AccessBuilder.NoWrap(UI51Build.Text(TopAt(UI51Build.Child(side, "Score"), 120f, 32f, 68f), "53",
                    FontFace.CinzelBold, 24f, UI51Tokens.GoldLight, TextAlignmentOptions.Top));
                if (j == 3) continue;
                var eq = UI51Build.Size(UI51Build.Child(row, "Equals" + j), 12f, 100f);
                UI51Build.Layout(eq, 12f, 100f);
                UI51AccessBuilder.NoWrap(UI51Build.Text(eq, "=", FontFace.CinzelSemiBold, 16f, UI51Tokens.CreamA(0.5f), TextAlignmentOptions.Center));
                equals[j] = eq.gameObject;
            }

            UI51MetaBuilder.Gap(card, "Gap3", 12.9f); // 4 + 8 del mockup + mezza interlinea
            var text = UI51Build.Text(UI51Build.Child(card, "Text"),
                "Avete superato 51 a pari punti: si gioca un’altra smazzata. Vince chi resta in testa da solo.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.7f), TextAlignmentOptions.Center);
            UI51MetaBuilder.Wrap(text, 13.6f); // line-height 1.5
            UI51MetaBuilder.Gap(card, "Gap4", 16.9f);
            var go = UI51Build.Child(card, "Go");
            UI51PrefabBuilder.GoldBody(go.gameObject, 300f, 50f, 14f, FontFace.CinzelBold, 14f, 2f, "SMAZZATA DI SPAREGGIO");
            UI51Build.Layout(go, -1f, 50f);

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "tie", root);
                UI51Build.Ref(so, "tieCard", card);
                UI51Build.Ref(so, "tieTitle", title);
                UI51Build.Ref(so, "tieText", text);
                UI51Build.Ref(so, "tieRow", rowGroup);
                UI51AccessBuilder.SetArray(so, "tieSides", sides);
                UI51AccessBuilder.SetArray(so, "tieAvatars", avatars);
                UI51AccessBuilder.SetArray(so, "tieScores", scores);
                UI51AccessBuilder.SetArray(so, "tieEquals", equals);
                UI51Build.Ref(so, "tieGo", go.GetComponent<Button>());
                UI51Build.Ref(so, "tieScrim", scrimButton);
                UI51Build.Ref(so, "roundNext", results.RoundContinue);
            });
            root.gameObject.SetActive(false);
        }

        // Pioggia di monete del mockup Cappotto: left, larghezza, durata, ritardo, gradi di rotazione.
        static readonly float[,] CoinDrops =
        {
            { 155, 28, 3.11f, 0.17f, -360 }, { 177, 22, 4.04f, 0.75f, -360 }, { 212, 40, 2.53f, 0.32f, 360 }, { 20, 22, 4.11f, 2.21f, -360 },
            { 285, 40, 2.49f, 0.77f, -180 }, { 138, 40, 2.66f, 0.41f, 180 }, { 276, 28, 2.59f, 2.00f, -180 }, { 180, 22, 3.39f, 0.22f, -360 },
            { 306, 28, 3.29f, 1.86f, 180 }, { 228, 40, 3.05f, 0.87f, -180 }, { 347, 28, 2.55f, 1.05f, 360 }, { 165, 40, 2.92f, 3.43f, -360 },
            { 252, 40, 2.70f, 1.20f, 360 }, { 205, 22, 4.13f, 0.27f, 180 }, { 164, 34, 3.47f, 2.03f, 360 }, { 25, 22, 4.10f, 1.66f, -360 },
            { 21, 34, 3.56f, 3.48f, 360 }, { 135, 40, 4.00f, 1.21f, 360 }, { 171, 28, 3.50f, 1.73f, -180 }, { 137, 28, 3.73f, 1.39f, 360 },
            { 31, 28, 3.21f, 1.92f, -180 }, { 210, 34, 3.67f, 3.45f, 360 }, { 108, 28, 2.55f, 0.53f, -180 }, { -4, 40, 3.90f, 0.64f, 180 },
            { -8, 28, 3.15f, 1.29f, 180 }, { 54, 22, 3.22f, 3.05f, 360 }, { 193, 40, 3.11f, 1.69f, 360 }, { 21, 28, 2.52f, 0.73f, -180 },
        };

        /// <summary>
        /// Vittoria immediata (mockup Cappotto / TreAssi) nell'area sicura del fine partita, subito sopra ai coriandoli:
        /// bagliore 420 a (195, 300), monete, testa a 110 (titolo 44 con l'ombra piena #6B4418 a 4), avatar 92 a 262 o tre assi 72x112 a 250.
        /// </summary>
        static void BuildInstant(UI51ResultsView view, RectTransform safe, RectTransform after, GameObject avatarPrefab, Sprite glow)
        {
            var root = UI51Build.Stretch(UI51Build.Child(safe, "Instant"));
            root.SetSiblingIndex(after.GetSiblingIndex() + 1);
            root.gameObject.SetActive(true);
            var burst = UI51Build.Image(UI51AccessBuilder.CenterAt(UI51Build.Child(root, "Burst"), 195f, 300f, 420f, 420f), glow, Color.white, false, false)
                .rectTransform;

            var rain = UI51Build.Stretch(UI51Build.Child(root, "Coins"));
            var coinSprite = UI51Build.Sprite("Common", "ic_coin");
            int n = CoinDrops.GetLength(0);
            var coins = new RectTransform[n];
            var times = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float left = CoinDrops[i, 0], w = CoinDrops[i, 1];
                var c = coins[i] = UI51Build.Place(UI51Build.Child(rain, "Coin" + i), new Vector2(0.5f, 1f), new Vector2(w, w), Vector2.zero);
                c.pivot = new Vector2(0.5f, 0.5f);
                c.anchoredPosition = new Vector2(left + w * 0.5f - 195f, -w * 0.5f);
                UI51Build.Image(c, coinSprite, Color.white);
                UI51Build.GetOrAdd<CanvasGroup>(c).alpha = 0f; // a riposo non si vede: compare all'8% della caduta
                times[i] = new Vector3(CoinDrops[i, 2], CoinDrops[i, 3], CoinDrops[i, 4]);
            }

            // Testa (top 110, gap 4): VITTORIA IMMEDIATA 10, titolo 44, riga 14 a capo entro 30 dai lati. Pivot al centro per il pop.
            var head = UI51Build.Place(UI51Build.Child(root, "Head"), new Vector2(0.5f, 1f), new Vector2(390f, 126f), Vector2.zero);
            head.pivot = new Vector2(0.5f, 0.5f);
            head.anchoredPosition = new Vector2(0f, -(110f + 63f));
            var cap = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(head, "Cap"), 0f, 0f, 0f, 14f),
                "VITTORIA IMMEDIATA", FontFace.CinzelSemiBold, 10f, UI51Tokens.Gold, TextAlignmentOptions.Center, 3f));
            var shade = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(head, "Shade"), -20f, -20f, 22f, 59f),
                "CAPPOTTO!", FontFace.CinzelBold, 44f, UI51Tokens.Hex("#6B4418"), TextAlignmentOptions.Center, 3f));
            var title = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(head, "Title"), -20f, -20f, 18f, 59f),
                "CAPPOTTO!", FontFace.CinzelBold, 44f, UI51Tokens.GoldLight, TextAlignmentOptions.Center, 3f));
            var sub = UI51Build.Text(UI51AccessBuilder.TopBand(UI51Build.Child(head, "Sub"), 30f, 30f, 81f, 40f), "Hai preso tutti e 10 i denari",
                FontFace.NunitoBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.Top);
            sub.enableWordWrapping = true;

            // Cappotto: avatar 92 col bordo d'oro e l'alone (a coppie accavallati di 14).
            var duo = UI51AccessBuilder.TopBand(UI51Build.Child(root, "Duo"), 0f, 0f, 262f, 92f);
            var halo = UI51Build.Image(UI51Build.Place(UI51Build.Child(duo, "Glow"), new Vector2(0.5f, 1f), new Vector2(152f, 152f), new Vector2(0f, -46f)),
                glow, UI51Tokens.GoldA(0.6f), false, false).rectTransform;
            halo.pivot = new Vector2(0.5f, 0.5f);
            var avatars = new[] { AvatarAt(duo, avatarPrefab, "AvatarA", 92f, 2f), AvatarAt(duo, avatarPrefab, "AvatarB", 92f, 2f) };

            // Tre assi: carte 72x112 r7 col filo d'oro di 2, ruotate -14 / 0 / 14 (centrate: nel mockup sono 9 piu' a destra).
            var aces = UI51AccessBuilder.TopBand(UI51Build.Child(root, "Aces"), 0f, 0f, 250f, 150f);
            var aceCards = new RectTransform[3];
            var aceImages = new Image[3];
            float[] aceX = { -48f, 0f, 48f }, aceTop = { 22f, 10f, 22f }, aceAngle = { -14f, 0f, 14f };
            for (int i = 0; i < 3; i++)
            {
                var card = aceCards[i] = UI51Build.Place(UI51Build.Child(aces, "Ace" + i), new Vector2(0.5f, 1f), new Vector2(76f, 116f), Vector2.zero);
                card.pivot = new Vector2(0.5f, 0.5f);
                card.anchoredPosition = new Vector2(aceX[i], -(aceTop[i] + 56f));
                card.localEulerAngles = new Vector3(0f, 0f, -aceAngle[i]);
                UI51Build.Solid(card, UI51Tokens.Gold, 9f, 0f, default, false, new UI51Shadow(0f, 10f, 22f, UI51Tokens.BlackA(0.6f)));
                UI51Build.GetOrAdd<CanvasGroup>(card);
                aceImages[i] = UI51Build.Image(UI51Build.Center(UI51Build.Child(card, "Face"), 72f, 112f), null, Color.white, false, false);
            }

            UI51Build.Wire(view, so =>
            {
                UI51Build.Ref(so, "instant", root);
                UI51Build.Ref(so, "instantBurst", burst);
                UI51Build.Ref(so, "instantHead", head);
                UI51Build.Ref(so, "instantDuo", duo);
                UI51Build.Ref(so, "instantGlow", halo);
                UI51Build.Ref(so, "instantAces", aces);
                UI51Build.Ref(so, "coinRain", rain);
                UI51Build.Ref(so, "instantCap", cap);
                UI51Build.Ref(so, "instantTitle", title);
                UI51Build.Ref(so, "instantShade", shade);
                UI51Build.Ref(so, "instantSub", sub);
                UI51AccessBuilder.SetArray(so, "instantAvatars", avatars);
                UI51AccessBuilder.SetArray(so, "aceCards", aceCards);
                UI51AccessBuilder.SetArray(so, "aceImages", aceImages);
                UI51AccessBuilder.SetArray(so, "coins", coins);
                var p = so.FindProperty("coinTimes");
                p.arraySize = n;
                for (int i = 0; i < n; i++) p.GetArrayElementAtIndex(i).vector3Value = times[i];
            });
            root.gameObject.SetActive(false);
        }

        /// <summary>Testo su una riga alto h dentro un layout (rect almeno font x 1.37 per l'Ellipsis).</summary>
        static TextMeshProUGUI TextLine(RectTransform parent, string name, string text, FontFace face, float size, Color color, float h, float spacing)
        {
            var t = UI51AccessBuilder.NoWrap(UI51Build.Text(UI51Build.Child(parent, name), text, face, size, color, TextAlignmentOptions.Center, spacing));
            UI51Build.Layout(t, -1f, h);
            return t;
        }

        /// <summary>Cella di una colonna di punti: centrata in verticale nella riga, la x la mette UI51ResultsView.</summary>
        static RectTransform Cell(RectTransform row, int j)
        {
            var rt = UI51Build.Place(UI51Build.Child(row, "Cell" + j), new Vector2(1f, 0.5f), new Vector2(92f, 26f), new Vector2(-46f - (3 - j) * 98f, 0f));
            rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        /// <summary>Fascia larga quanto il genitore meno side per lato, col bordo basso a bottom dal fondo, alta h.</summary>
        static RectTransform BottomBand(RectTransform rt, float side, float bottom, float h)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(side, bottom);
            rt.offsetMax = new Vector2(-side, bottom + h);
            return rt;
        }

        /// <summary>Rettangolo largo w alto h col bordo alto a top dal bordo alto del genitore, centrato in orizzontale.</summary>
        static RectTransform TopAt(RectTransform rt, float w, float h, float top)
        {
            UI51Build.Place(rt, new Vector2(0.5f, 1f), new Vector2(w, h), new Vector2(0f, -top));
            return rt;
        }

        /// <summary>AvatarFrame del prefab per nome (piu' d'uno nello stesso genitore), ancorato in alto al centro.</summary>
        static AvatarFrame AvatarAt(RectTransform parent, GameObject prefab, string name, float size, float ring)
        {
            var t = parent.Find(name);
            var go = t != null ? t.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            var rt = UI51Build.Place((RectTransform)go.transform, new Vector2(0.5f, 1f), new Vector2(size, size), new Vector2(0f, -size * 0.5f));
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            var frame = go.GetComponent<AvatarFrame>();
            UI51Build.Wire(frame, so => UI51Build.Float(so, "m_RingWidth", ring));
            frame.Layout();
            return frame;
        }
    }
}
