using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UI51.EditorTools
{
    /// <summary>
    /// Fase 1: prefab dei componenti SPEC §3 in Assets/UI51/Prefabs (misure, testi e stati dai mockup).
    /// Bottoni oro (principale, pannello, piccolo), contorno, pericolo, tondo con icona; pannello; cornice avatar;
    /// banner giocatore (proprio, avversario, verticale); badge "+N" e pallino; bottom sheet; dialog; tab segmentate;
    /// barra di navigazione; interruttore; radice UI51_Root (Canvas 390x844 match .5 + SafeArea).
    /// I colori scritti qui sono solo default sensati: Paint/Refresh dei componenti li riscrivono a runtime.
    /// Idempotente: rieseguire aggiorna gli stessi prefab.
    /// </summary>
    public static class UI51PrefabBuilder
    {
        // Sheet e dialog stanno nella SafeArea: scrim e fondo dello sheet sforano di tanto oltre gli inset (notch, home indicator).
        const float SafeBleed = 120f;

        [MenuItem("Tools/UI51/Component Prefabs")]
        private static void Menu() => Build();

        public static string PrefabPath(string name) =>$"{UI51Build.PrefabRoot}/{name}.prefab";

        public static void Build()
        {
            BuildButtons();
            UI51Build.EditPrefab(PrefabPath("Panel"), BuildPanel);
            UI51Build.EditPrefab(PrefabPath("AvatarFrame"), root =>
                BuildAvatar(root, 64f, 4f, FrameStyle.Oro, UI51Build.Sprite("Avatars", "avatar_1"), 34f));
            UI51Build.EditPrefab(PrefabPath("PlayerBanner_Own"), BuildBannerOwn);
            UI51Build.EditPrefab(PrefabPath("PlayerBanner_Opponent"), BuildBannerOpponent);
            UI51Build.EditPrefab(PrefabPath("PlayerBanner_Vertical"), BuildBannerVertical);
            UI51Build.EditPrefab(PrefabPath("Badge_More"), BuildBadgeMore);
            UI51Build.EditPrefab(PrefabPath("Badge_Dot"), BuildBadgeDot);
            UI51Build.EditPrefab(PrefabPath("BottomSheet"), BuildBottomSheet);
            UI51Build.EditPrefab(PrefabPath("Dialog"), BuildDialog);
            UI51Build.EditPrefab(PrefabPath("SegmentedTabs"), BuildTabs);
            UI51Build.EditPrefab(PrefabPath("BottomNav"), BuildBottomNav);
            UI51Build.EditPrefab(PrefabPath("Toggle"), BuildToggle);
            UI51Build.EditPrefab(PrefabPath("UI51_Root"), BuildRoot);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI51 Prefab] Prefab dei componenti completati.");
        }

        // --- Bottoni (SPEC §3.1)

        static void BuildButtons()
        {
            // Principale (CTA Home "GIOCA"): 342x54 r16, Cinzel 700 15 ls 2, ombra 0 8 18 nero .35.
            UI51Build.EditPrefab(PrefabPath("GoldButton"), root =>
                GoldBody(root, 342f, 54f, 16f, FontFace.CinzelBold, 15f, 2f, "GIOCA",
                    new UI51Shadow(0f, 8f, 18f, UI51Tokens.BlackA(0.35f))));
            // Dentro i pannelli ("CONFERMA"): 342x52 r16, Cinzel 700 14 ls 2, senza ombra.
            UI51Build.EditPrefab(PrefabPath("GoldButton_Panel"), root =>
                GoldBody(root, 342f, 52f, 16f, FontFace.CinzelBold, 14f, 2f, "CONFERMA"));
            // Piccolo (Amici "Aggiungi amico"): 160x38 r12, Nunito 800 12.
            UI51Build.EditPrefab(PrefabPath("GoldButton_Small"), root =>
                GoldBody(root, 160f, 38f, 12f, FontFace.NunitoExtraBold, 12f, 0f, "Aggiungi amico"));

            // Contorno oro: trasparente, bordo oro .45, testo oro Nunito 800 13.
            UI51Build.EditPrefab(PrefabPath("OutlineButton"), root =>
                ButtonBody(root, 342f, 44f, UI51Shape.Solid(Color.clear), UI51Tokens.Radii(14f), 1f, UI51Tokens.GoldA(0.45f),
                    FontFace.NunitoExtraBold, 13f, 0f, UI51Tokens.Gold, "Annulla"));

            // Pericolo ("Abbandona"): rosso .08, bordo rosso .5, testo #F08A8D Nunito 800 14.
            UI51Build.EditPrefab(PrefabPath("DangerButton"), root =>
                ButtonBody(root, 342f, 46f, UI51Shape.Solid(UI51Tokens.Rgba(229, 72, 77, 0.08f)), UI51Tokens.Radii(14f), 1f,
                    UI51Tokens.Rgba(229, 72, 77, 0.5f), FontFace.NunitoExtraBold, 14f, 0f, UI51Tokens.DangerText, "Abbandona"));

            // Tondo 40 con icona 20 (impostazioni, indietro, chiudi).
            UI51Build.EditPrefab(PrefabPath("RoundIconButton"), root =>
            {
                var rt = UI51Build.Center((RectTransform)root.transform, UI51Tokens.RoundButtonSize, UI51Tokens.RoundButtonSize);
                var shape = UI51Build.Shape(rt, UI51Tokens.RoundButtonFill(), 180f, UI51Tokens.Radii(UI51Tokens.RoundButtonSize * 0.5f),
                    1f, UI51Tokens.GoldA(0.45f), true);
                UI51Build.Button(shape, shape);
                UI51Build.GetOrAdd<UI51Press>(root);
                var icon = UI51Build.Center(UI51Build.Child(root.transform, "Icon"), 20f, 20f);
                UI51Build.Image(icon, UI51Build.Sprite("Common", "ic_settings_cream"), Color.white);
            });
        }

        internal static void GoldBody(GameObject root, float w, float h, float radius, FontFace face, float size, float spacing,
            string label, params UI51Shadow[] shadows) =>
            ButtonBody(root, w, h, UI51Tokens.GoldButtonFill(), UI51Tokens.Radii(radius), 1f, UI51Tokens.GoldButtonBorder,
                face, size, spacing, UI51Tokens.OnGold, label, shadows);

        /// <summary>Corpo comune: UI51Shape cliccabile + Button + UI51Press, etichetta centrata a tutta misura.</summary>
        internal static void ButtonBody(GameObject root, float w, float h, Gradient fill, Vector4 radii, float borderWidth, Color borderColor,
            FontFace face, float size, float spacing, Color textColor, string label, params UI51Shadow[] shadows)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, w, h);
            var shape = UI51Build.Shape(rt, fill, 180f, radii, borderWidth, borderColor, true, shadows);
            UI51Build.Button(shape, shape);
            UI51Build.GetOrAdd<UI51Press>(root);
            var text = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(root.transform, "Label")), label, face, size, textColor,
                TextAlignmentOptions.Center, spacing);
            text.enableWordWrapping = false;
        }

        // --- Pannello (SPEC §3.2)

        static void BuildPanel(GameObject root)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, 342f, 200f);
            UI51Build.Shape(rt, UI51Tokens.PanelFill(), 180f, UI51Tokens.Radii(UI51Tokens.RadiusPanel), 1f,
                UI51Tokens.BorderGoldSoft, false, UI51Tokens.ShadowSm);
        }

        // --- Cornice avatar (SPEC §3.6)

        /// <summary>
        /// Face (Ring, Timer, Inner con maschera e Avatar) + Emoticon (cerchio crema con fotogrammi animati).
        /// emoSize: 34 per il proprio banner, 33 per l'avversario in alto (il viso si vede ~27 come il 30 del mockup; a 36 il vapore di
        /// "arrabbiato" uscirebbe dall'anello d'oro), 30 per i laterali.
        /// </summary>
        internal static AvatarFrame BuildAvatar(GameObject root, float size, float ringWidth, FrameStyle frame, Sprite sprite, float emoSize)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, size, size);
            var avatar = UI51Build.GetOrAdd<AvatarFrame>(root);

            var face = UI51Build.Stretch(UI51Build.Child(rt, "Face"));
            var ring = UI51Build.Shape(UI51Build.Stretch(UI51Build.Child(face, "Ring")), AvatarFrame.FrameFill(frame), 110f,
                UI51Tokens.Radii(size * 0.5f), 0f, Color.clear);

            var timerRt = UI51Build.Stretch(UI51Build.Child(face, "Timer"));
            var timer = UI51Build.Image(timerRt, UI51Build.Sprite("Shapes", "circle"), UI51Tokens.Gold, false, false);
            timer.type = Image.Type.Filled;
            timer.fillMethod = Image.FillMethod.Radial360;
            timer.fillOrigin = (int)Image.Origin360.Top;
            timer.fillClockwise = true;
            timer.fillAmount = 1f;

            var innerRt = UI51Build.Stretch(UI51Build.Child(face, "Inner"), ringWidth, ringWidth, ringWidth, ringWidth);
            var inner = UI51Build.Solid(innerRt, UI51Tokens.Navy, size * 0.5f - ringWidth);
            UI51Build.GetOrAdd<Mask>(inner).showMaskGraphic = true;
            var avatarImg = UI51Build.Image(UI51Build.Child(innerRt, "Avatar"), sprite, Color.white, false, false);

            var emoRt = UI51Build.Center(UI51Build.Child(rt, "Emoticon"), size + 4f, size + 4f);
            var emo = UI51Build.Solid(emoRt, UI51Tokens.CreamA(0.95f), size * 0.5f + 2f, 2f, UI51Tokens.Gold);
            var emoImgRt = UI51Build.Center(UI51Build.Child(emoRt, "Image"), emoSize, emoSize);
            UI51Build.Image(emoImgRt, null, Color.white);
            var player = UI51Build.GetOrAdd<EmoticonPlayer>(emoImgRt);

            UI51Build.Wire(avatar, so =>
            {
                UI51Build.Ref(so, "m_Face", face);
                UI51Build.Ref(so, "m_Ring", ring);
                UI51Build.Ref(so, "m_Timer", timer);
                UI51Build.Ref(so, "m_Inner", inner);
                UI51Build.Ref(so, "m_Avatar", avatarImg);
                UI51Build.Ref(so, "m_Emoticon", emo);
                UI51Build.Ref(so, "m_EmoticonPlayer", player);
                UI51Build.Float(so, "m_RingWidth", ringWidth);
            });
            avatar.Layout();

            // Stato di riposo: timer ed emoticon spenti (si accendono da SetTimer / ShowEmoticon).
            timerRt.gameObject.SetActive(false);
            emoRt.gameObject.SetActive(false);
            return avatar;
        }

        // --- Banner giocatore (SPEC §3.5, mockup Tavolo)

        static readonly UI51Shadow BannerShadow = new UI51Shadow(0f, 6f, 14f, UI51Tokens.BlackA(0.45f));

        /// <summary>Radice con PlayerBanner, anello pulsante (oro trasparente) e fondo Notte con ombra.</summary>
        static (PlayerBanner banner, UI51Shape pulse, UI51Shape bg, RectTransform content) BannerShell(GameObject root,
            float w, float h, float radius)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, w, h);
            var banner = UI51Build.GetOrAdd<PlayerBanner>(root);

            var pulse = UI51Build.Solid(UI51Build.Center(UI51Build.Child(rt, "PulseRing"), w, h), Color.white, radius);
            pulse.color = UI51Tokens.WithAlpha(UI51Tokens.Gold, 0f);

            var bg = UI51Build.Solid(UI51Build.Stretch(UI51Build.Child(rt, "Bg")), Color.white, radius, 1f,
                UI51Tokens.GoldA(0.4f), false, BannerShadow);
            UI51Banners.Apply(bg, BannerStyle.Notte);

            var content = UI51Build.Stretch(UI51Build.Child(rt, "Content"));
            return (banner, pulse, bg, content);
        }

        /// <summary>Chip delle carte prese: mini dorso + numero, fondo bianco .06.</summary>
        static (RectTransform chip, Image back, TextMeshProUGUI count) CaptureChip(Transform parent, float h, int padL, int padR,
            float gap, float backW, float backH, float textSize, string value)
        {
            var chip = UI51Build.Child(parent, "Chip");
            UI51Build.Row(chip, gap, new RectOffset(padL, padR, 0, 0), TextAnchor.MiddleCenter, true, false);
            UI51Build.Fit(chip, true, false);
            UI51Build.Solid(chip, UI51Tokens.WhiteA(0.06f), h * 0.5f);
            chip.sizeDelta = new Vector2(chip.sizeDelta.x, h);
            UI51Build.Layout(chip, -1f, h);

            var backRt = UI51Build.Size(UI51Build.Child(chip, "CardBack"), backW, backH);
            var back = UI51Build.Image(backRt, UI51Build.Sprite("Cards", "back_giada"), Color.white);
            UI51Build.Layout(back, backW, backH);

            var countRt = UI51Build.Size(UI51Build.Child(chip, "Count"), 0f, h);
            var count = UI51Build.Text(countRt, value, FontFace.NunitoExtraBold, textSize, UI51Tokens.CreamA(0.8f),
                TextAlignmentOptions.Center);
            count.enableWordWrapping = false;
            count.overflowMode = TextOverflowModes.Overflow;
            return (chip, back, count);
        }

        static void WireBanner(PlayerBanner banner, UI51Shape pulse, UI51Shape bg, AvatarFrame avatar, TMP_Text name,
            TMP_Text level, GameObject chip, Image back, TMP_Text captures)
        {
            UI51Build.Wire(banner, so =>
            {
                UI51Build.Ref(so, "m_PulseRing", pulse);
                UI51Build.Ref(so, "m_Background", bg);
                UI51Build.Ref(so, "m_Avatar", avatar);
                UI51Build.Ref(so, "nameText", name);
                UI51Build.Ref(so, "m_Level", level);
                UI51Build.Ref(so, "m_Chip", chip);
                UI51Build.Ref(so, "m_CardBack", back);
                UI51Build.Ref(so, "m_Captures", captures);
                UI51Build.Int(so, "m_Style", (int)BannerStyle.Notte);
            });
        }

        /// <summary>Proprio (in basso): 186x50 pill, avatar 38 con timer, nome + livello, chip 22.</summary>
        static void BuildBannerOwn(GameObject root)
        {
            var (banner, pulse, bg, content) = BannerShell(root, 186f, 50f, UI51Tokens.RadiusPill);
            UI51Build.Row(content, 8f, UI51Build.Pad(0, 6, 0, 6), TextAnchor.MiddleLeft, true, false);

            var avatarRt = UI51Build.Child(content, "Avatar");
            var avatar = BuildAvatar(avatarRt.gameObject, 38f, 2f, FrameStyle.Oro, UI51Build.Sprite("Avatars", "avatar_1"), 34f);
            UI51Build.Layout(avatar, 38f, 38f);
            // Demo del timer nel prefab: arco al 70% (a runtime SetTimer(-1) lo nasconde).
            var timer = avatarRt.Find("Face/Timer");
            if (timer != null)
            {
                timer.gameObject.SetActive(true);
                timer.GetComponent<Image>().fillAmount = 0.7f;
            }

            var info = UI51Build.Child(content, "Info");
            UI51Build.Column(info, 1f, null, TextAnchor.MiddleLeft, true, true);
            UI51Build.Layout(info, 0f, -1f, 1f); // larghezza preferita 0: un nome lungo va in "..." invece di schiacciare avatar e chip
            var name = UI51Build.Text(UI51Build.Child(info, "Name"), "GiocatoreNapo", FontFace.NunitoExtraBold, 12f, UI51Tokens.Cream);
            name.enableWordWrapping = false;
            var level = UI51Build.Text(UI51Build.Child(info, "Level"), string.Format(PlayerBanner.LevelFormat, 12),
                FontFace.NunitoRegular, 10f, UI51Tokens.CreamA(0.55f));
            level.enableWordWrapping = false;

            var (chip, back, count) = CaptureChip(content, 22f, 5, 7, 4f, 11f, 16f, 11f, "12");
            WireBanner(banner, pulse, bg, avatar, name, level, chip.gameObject, back, count);
        }

        /// <summary>Avversario (in alto, 1v1): 120x50 pill, avatar 36 cornice blu, nome 11, livello 9 + chip 16 sulla stessa riga.</summary>
        static void BuildBannerOpponent(GameObject root)
        {
            var (banner, pulse, bg, content) = BannerShell(root, 120f, 50f, UI51Tokens.RadiusPill);
            UI51Build.Row(content, 7f, UI51Build.Pad(0, 6, 0, 6), TextAnchor.MiddleLeft, true, false);

            var avatarRt = UI51Build.Child(content, "Avatar");
            var avatar = BuildAvatar(avatarRt.gameObject, 36f, 2f, FrameStyle.Blu, UI51Build.Sprite("Avatars", "av_4"), 33f);
            UI51Build.Layout(avatar, 36f, 36f);

            var info = UI51Build.Child(content, "Info");
            UI51Build.Column(info, 3f, null, TextAnchor.MiddleLeft, true, true);
            UI51Build.Layout(info, 0f, -1f, 1f); // larghezza preferita 0: un nome lungo va in "..." invece di schiacciare avatar e chip
            var name = UI51Build.Text(UI51Build.Child(info, "Name"), "Marco_93", FontFace.NunitoExtraBold, 11f, UI51Tokens.Cream);
            name.enableWordWrapping = false;

            var row = UI51Build.Child(info, "Row");
            UI51Build.Row(row, 4f, null, TextAnchor.MiddleLeft, true, false);
            UI51Build.Layout(row, -1f, 16f);
            var level = UI51Build.Text(UI51Build.Child(row, "Level"), string.Format(PlayerBanner.LevelFormat, 18),
                FontFace.NunitoRegular, 9f, UI51Tokens.CreamA(0.55f));
            level.enableWordWrapping = false;
            // "Liv. 100" con due cifre di carte prese sfora di 2: si stringe fino a 8 invece di diventare "Liv. 1...".
            level.enableAutoSizing = true;
            level.fontSizeMin = 8f;
            level.fontSizeMax = 9f;
            level.rectTransform.sizeDelta = new Vector2(0f, 16f);

            var (chip, back, count) = CaptureChip(row, 16f, 4, 5, 3f, 8f, 12f, 10f, "9");
            WireBanner(banner, pulse, bg, avatar, name, level, chip.gameObject, back, count);
        }

        /// <summary>Verticale (lati del 2v2 / 1v3): 64x100 r20, avatar 40, nome 10 (max 58), chip piccolo. Senza livello.</summary>
        static void BuildBannerVertical(GameObject root)
        {
            var (banner, pulse, bg, content) = BannerShell(root, 64f, 100f, 20f);
            UI51Build.Column(content, 4f, UI51Build.Pad(9, 4, 9, 4), TextAnchor.MiddleCenter, false, false);

            var avatarRt = UI51Build.Child(content, "Avatar");
            var avatar = BuildAvatar(avatarRt.gameObject, 40f, 2f, FrameStyle.Blu, UI51Build.Sprite("Avatars", "av_2"), 30f);

            var nameRt = UI51Build.Size(UI51Build.Child(content, "Name"), 58f, 14f);
            var name = UI51Build.Text(nameRt, "Sara", FontFace.NunitoExtraBold, 10f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            name.enableWordWrapping = false;
            // Nomi lunghi (utente 01/10): prima scende fino a 8 (circa 12 lettere), poi i puntini; il nome intero e' nel profilo rapido.
            name.enableAutoSizing = true;
            name.fontSizeMin = 8f;
            name.fontSizeMax = 10f;

            var (chip, back, count) = CaptureChip(content, 16f, 4, 5, 3f, 8f, 12f, 10f, "4");
            WireBanner(banner, pulse, bg, avatar, name, null, chip.gameObject, back, count);
        }

        // --- Badge (SPEC §3.9)

        /// <summary>"+N": pillola oro h22 min 22, bordo 2 scuro, Nunito 800 10 #25160A.</summary>
        static void BuildBadgeMore(GameObject root)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, 30f, 22f);
            var shape = UI51Build.Solid(rt, UI51Tokens.Gold, 11f, 2f, UI51Tokens.BadgeRing);
            UI51Build.Row(rt, 0f, UI51Build.Pad(0, 5, 0, 5), TextAnchor.MiddleCenter, true, false);
            UI51Build.Fit(rt, true, false);
            UI51Build.Layout(rt, -1f, 22f, -1f, 22f);

            var labelRt = UI51Build.Size(UI51Build.Child(rt, "Label"), 0f, 22f);
            var label = UI51Build.Text(labelRt, "+2", FontFace.NunitoExtraBold, 10f, UI51Tokens.OnGold, TextAlignmentOptions.Center);
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;

            var badge = UI51Build.GetOrAdd<UI51Badge>(root);
            UI51Build.Wire(badge, so =>
            {
                UI51Build.Int(so, "m_Mode", (int)UI51Badge.Mode.More);
                UI51Build.Ref(so, "m_Shape", shape);
                UI51Build.Ref(so, "m_Label", label);
            });
        }

        /// <summary>Pallino notifica 9x9 rosso con bordo 1.5 scuro.</summary>
        static void BuildBadgeDot(GameObject root)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, 9f, 9f);
            var shape = UI51Build.Solid(rt, UI51Tokens.Danger, 4.5f, 1.5f, UI51Tokens.BadgeRing);
            var badge = UI51Build.GetOrAdd<UI51Badge>(root);
            UI51Build.Wire(badge, so =>
            {
                UI51Build.Int(so, "m_Mode", (int)UI51Badge.Mode.Dot);
                UI51Build.Ref(so, "m_Shape", shape);
                UI51Build.Ref(so, "m_Label", null);
            });
        }

        // --- Bottom sheet (SPEC §3.3)

        static void BuildBottomSheet(GameObject root)
        {
            var rt = UI51Build.Stretch((RectTransform)root.transform);
            var sheetComp = UI51Build.GetOrAdd<BottomSheet>(root);

            var scrim = UI51Build.Stretch(UI51Build.Child(rt, "Scrim"), -SafeBleed, -SafeBleed, -SafeBleed, -SafeBleed);
            var scrimShape = UI51Build.Solid(scrim, UI51Tokens.Scrim, 0f, 0f, default, true);
            UI51Build.Button(scrim, scrimShape);

            // Il fondo scende di SafeBleed sotto la SafeArea fino al bordo schermo; il padding lo ricompensa.
            var sheet = UI51Build.Child(rt, "Sheet");
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.anchoredPosition = new Vector2(0f, -SafeBleed);
            sheet.sizeDelta = new Vector2(0f, 300f);
            UI51Build.Shape(sheet, UI51Tokens.SheetFill(), 180f, UI51Tokens.RadiiTop(UI51Tokens.RadiusSheet), 1f,
                UI51Tokens.GoldA(0.4f), true, new UI51Shadow(0f, -12f, 40f, UI51Tokens.BlackA(0.5f)));
            UI51Build.Column(sheet, 0f, UI51Build.Pad(12, 20, 24 + (int)SafeBleed, 20), TextAnchor.UpperCenter, true, true);
            UI51Build.Fit(sheet, false, true);

            // Maniglia 40x4 crema .25 in cima.
            var handleArea = UI51Build.Child(sheet, "HandleArea");
            UI51Build.Layout(handleArea, -1f, 18f);
            var handle = UI51Build.Place(UI51Build.Child(handleArea, "Handle"), new Vector2(0.5f, 1f), new Vector2(40f, 4f), Vector2.zero);
            UI51Build.Solid(handle, UI51Tokens.CreamA(0.25f), 2f);

            var header = UI51Build.Child(sheet, "Header");
            UI51Build.Row(header, 12f, null, TextAnchor.MiddleLeft, true, true);

            var titles = UI51Build.Child(header, "Titles");
            UI51Build.Column(titles, 3f, null, TextAnchor.UpperLeft, true, true);
            UI51Build.Layout(titles, -1f, -1f, 1f);
            var title = UI51Build.Text(UI51Build.Child(titles, "Title"), "Titolo", FontFace.CinzelBold, 18f, UI51Tokens.Cream);
            title.enableWordWrapping = false;
            var subtitle = UI51Build.Text(UI51Build.Child(titles, "Subtitle"), "Sottotitolo", FontFace.NunitoRegular, 12f,
                UI51Tokens.CreamA(0.55f));

            var close = UI51Build.Child(header, "Close");
            var closeShape = UI51Build.Solid(close, UI51Tokens.WhiteA(0.06f), 18f, 1f, UI51Tokens.GoldA(0.35f), true);
            UI51Build.Layout(close, 36f, 36f);
            var closeButton = UI51Build.Button(close, closeShape);
            UI51Build.GetOrAdd<UI51Press>(close);
            var closeIcon = UI51Build.Center(UI51Build.Child(close, "Icon"), 14f, 14f);
            UI51Build.Image(closeIcon, UI51Build.Sprite("Common", "ic_close_cream"), Color.white);

            var gap = UI51Build.Child(sheet, "HeaderGap");
            UI51Build.Layout(gap, -1f, 14f);

            var content = UI51Build.Child(sheet, "Content");
            UI51Build.Column(content, 10f, null, TextAnchor.UpperCenter, true, true);

            UI51Build.Wire(sheetComp, so =>
            {
                UI51Build.Ref(so, "m_Scrim", scrim);
                UI51Build.Ref(so, "m_Sheet", sheet);
                UI51Build.Ref(so, "m_Content", content);
                UI51Build.Ref(so, "m_Title", title);
                UI51Build.Ref(so, "m_Subtitle", subtitle);
                UI51Build.Ref(so, "m_CloseButton", closeButton);
            });
            root.SetActive(false);
        }

        // --- Dialog di conferma (SPEC §3.4)

        static void BuildDialog(GameObject root)
        {
            var rt = UI51Build.Stretch((RectTransform)root.transform);
            var dialog = UI51Build.GetOrAdd<UI51Dialog>(root);

            var scrim = UI51Build.Stretch(UI51Build.Child(rt, "Scrim"), -SafeBleed, -SafeBleed, -SafeBleed, -SafeBleed);
            UI51Build.Solid(scrim, UI51Tokens.BlackA(0.55f), 0f, 0f, default, true);

            var card = UI51Build.Child(rt, "Card");
            card.anchorMin = new Vector2(0f, 1f);
            card.anchorMax = new Vector2(1f, 1f);
            card.pivot = new Vector2(0.5f, 1f);
            card.offsetMin = new Vector2(24f, card.offsetMin.y);
            card.offsetMax = new Vector2(-24f, card.offsetMax.y);
            card.anchoredPosition = new Vector2(0f, -200f);
            var cardShape = UI51Build.Shape(card, UI51Tokens.DialogFill(), 180f, UI51Tokens.Radii(UI51Tokens.RadiusDialog), 1f,
                UI51Tokens.GoldA(0.4f), true, UI51Tokens.ShadowDialog);
            UI51Build.Column(card, 0f, UI51Build.Pad(22, 20, 20, 20), TextAnchor.UpperCenter, true, true);
            UI51Build.Fit(card, false, true);

            // Cerchio icona 56 + 14 di stacco nella stessa riga, cosi' Show la spegne intera senza icona.
            // Bianco tinto da Paint (oro/rosso .12, bordo .5).
            var iconRow = UI51Build.Child(card, "IconRow");
            UI51Build.Layout(iconRow, -1f, 70f);
            var circleRt = UI51Build.Place(UI51Build.Child(iconRow, "IconCircle"), new Vector2(0.5f, 1f), new Vector2(56f, 56f), Vector2.zero);
            var circle = UI51Build.Solid(circleRt, Color.white, 28f, 1f, UI51Tokens.GoldA(0.5f));
            circle.color = UI51Tokens.GoldA(0.12f);
            var iconRt = UI51Build.Center(UI51Build.Child(circleRt, "Icon"), 28f, 26f);
            var icon = UI51Build.Image(iconRt, UI51Build.Sprite("Common", "ic_warn_cream"), UI51Tokens.Gold);

            var title = UI51Build.Text(UI51Build.Child(card, "Title"), "Abbandonare la partita?", FontFace.CinzelBold, 19f,
                UI51Tokens.Cream, TextAlignmentOptions.Center);
            Spacer(card, "GapText", 10f);
            var text = UI51Build.Text(UI51Build.Child(card, "Text"), "Perderai la puntata e la partita verrà contata come sconfitta.",
                FontFace.NunitoRegular, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            Spacer(card, "GapButtons", 18f);

            var buttons = UI51Build.Child(card, "Buttons");
            UI51Build.Row(buttons, 10f, null, TextAnchor.MiddleCenter, true, true);
            UI51Build.GetOrAdd<HorizontalLayoutGroup>(buttons).childForceExpandWidth = true;
            UI51Build.Layout(buttons, -1f, 48f);

            var cancelRt = UI51Build.Child(buttons, "Cancel");
            var cancelShape = UI51Build.Solid(cancelRt, Color.clear, 13f, 1f, UI51Tokens.CreamA(0.35f), true);
            UI51Build.Layout(cancelRt, -1f, 48f, 1f);
            var cancel = UI51Build.Button(cancelRt, cancelShape);
            UI51Build.GetOrAdd<UI51Press>(cancelRt);
            var cancelLabel = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(cancelRt, "Label")), "Annulla",
                FontFace.NunitoExtraBold, 14f, UI51Tokens.Cream, TextAlignmentOptions.Center);
            cancelLabel.enableWordWrapping = false;

            var confirmRt = UI51Build.Child(buttons, "Confirm");
            var confirmShape = UI51Build.Shape(confirmRt, UI51Tokens.GoldButtonFill(), 180f, UI51Tokens.Radii(13f), 1f,
                UI51Tokens.GoldButtonBorder, true);
            UI51Build.Layout(confirmRt, -1f, 48f, 1f);
            var confirm = UI51Build.Button(confirmRt, confirmShape);
            UI51Build.GetOrAdd<UI51Press>(confirmRt);
            var confirmLabel = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(confirmRt, "Label")), "Abbandona",
                FontFace.NunitoExtraBold, 14f, UI51Tokens.OnGold, TextAlignmentOptions.Center);
            confirmLabel.enableWordWrapping = false;

            UI51Build.Wire(dialog, so =>
            {
                UI51Build.Ref(so, "m_Scrim", scrim);
                UI51Build.Ref(so, "m_Card", card);
                UI51Build.Ref(so, "m_CardShape", cardShape);
                UI51Build.Ref(so, "m_IconRow", iconRow.gameObject);
                UI51Build.Ref(so, "m_IconCircle", circle);
                UI51Build.Ref(so, "m_Icon", icon);
                UI51Build.Ref(so, "m_Title", title);
                UI51Build.Ref(so, "m_Text", text);
                UI51Build.Ref(so, "m_Confirm", confirm);
                UI51Build.Ref(so, "m_ConfirmShape", confirmShape);
                UI51Build.Ref(so, "m_ConfirmLabel", confirmLabel);
                UI51Build.Ref(so, "m_Cancel", cancel);
                UI51Build.Ref(so, "m_CancelLabel", cancelLabel);
            });
            root.SetActive(false);
        }

        static void Spacer(Transform parent, string name, float height) =>
            UI51Build.Layout(UI51Build.Child(parent, name), -1f, height);

        // --- Tab segmentate (SPEC §3.7)

        internal static void BuildTabs(GameObject root)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, 342f, 44f);
            UI51Build.Solid(rt, UI51Tokens.WhiteA(0.04f), UI51Tokens.RadiusTabs, 1f, UI51Tokens.GoldA(0.18f));
            UI51Build.Row(rt, 4f, UI51Build.Pad(4, 4, 4, 4), TextAnchor.MiddleCenter, true, true);
            var tabsComp = UI51Build.GetOrAdd<SegmentedTabs>(root);

            const int count = 3;
            var shapes = new UI51Shape[count];
            var buttons = new Button[count];
            var labels = new TMP_Text[count];
            for (int i = 0; i < count; i++)
            {
                var tab = UI51Build.Child(rt, "Tab" + (i + 1));
                UI51Build.Layout(tab, -1f, 36f, 1f);
                shapes[i] = UI51Build.Solid(tab, Color.white, 9f, 1f, Color.clear, true);
                shapes[i].color = Color.clear;
                buttons[i] = UI51Build.Button(tab, shapes[i]);
                var label = UI51Build.Text(UI51Build.Stretch(UI51Build.Child(tab, "Label")), "Tab " + (i + 1),
                    FontFace.NunitoBold, 13f, UI51Tokens.CreamA(0.65f), TextAlignmentOptions.Center);
                label.enableWordWrapping = false;
                labels[i] = label;
            }

            UI51Build.Wire(tabsComp, so =>
            {
                var list = so.FindProperty("m_Tabs");
                list.arraySize = count;
                for (int i = 0; i < count; i++)
                {
                    var el = list.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("shape").objectReferenceValue = shapes[i];
                    el.FindPropertyRelative("button").objectReferenceValue = buttons[i];
                    el.FindPropertyRelative("label").objectReferenceValue = labels[i];
                }
                UI51Build.Int(so, "m_SelectedIndex", 0);
            });
        }

        // --- Barra di navigazione (SPEC §3.8)

        static void BuildBottomNav(GameObject root)
        {
            var rt = (RectTransform)root.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 72f);
            UI51Build.Solid(rt, UI51Tokens.NavBar, 0f);
            UI51Build.Row(rt, 0f, UI51Build.Pad(0, 12, 0, 12), TextAnchor.MiddleCenter, true, true);
            var nav = UI51Build.GetOrAdd<BottomNav>(root);

            // Filo oro .3 sul bordo alto.
            var line = UI51Build.Child(rt, "TopLine");
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(1f, 1f);
            line.pivot = new Vector2(0.5f, 1f);
            line.anchoredPosition = Vector2.zero;
            line.sizeDelta = new Vector2(0f, 1f);
            UI51Build.Solid(line, UI51Tokens.GoldA(0.3f), 0f);
            UI51Build.Layout(line, -1f, -1f, -1f, -1f, true);

            // La barra sta nella SafeArea: questo prolungamento copre l'inset in basso (home indicator) fino al bordo schermo.
            var fill = UI51Build.Child(rt, "SafeBottomFill");
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(1f, 0f);
            fill.pivot = new Vector2(0.5f, 1f);
            fill.anchoredPosition = Vector2.zero;
            fill.sizeDelta = new Vector2(0f, 60f);
            UI51Build.Solid(fill, UI51Tokens.NavBar, 0f).raycastTarget = false;
            UI51Build.Layout(fill, -1f, -1f, -1f, -1f, true);
            fill.SetAsFirstSibling();

            string[] icons = { "ic_home_cream", "ic_cards_cream", "ic_chest_cream", "ic_person_cream" };
            string[] names = { "Gioca", "Collezione", "Negozio", "Profilo" };
            var items = new (Button button, GameObject dash, Image icon, TMP_Text label)[icons.Length];
            for (int i = 0; i < icons.Length; i++)
            {
                var item = UI51Build.Child(rt, "Item_" + names[i]);
                UI51Build.Layout(item, -1f, -1f, 1f);
                UI51Build.Column(item, 5f, null, TextAnchor.MiddleCenter, false, false);
                // Superficie cliccabile trasparente su tutta la voce.
                var hit = UI51Build.Solid(item, Color.clear, 0f, 0f, default, true);
                var button = UI51Build.Button(item, hit);

                var dash = UI51Build.Child(item, "Dash");
                UI51Build.Place(dash, new Vector2(0.5f, 1f), new Vector2(24f, 2f), Vector2.zero);
                UI51Build.Shape(dash, UI51Shape.Solid(UI51Tokens.Gold), 180f, new Vector4(0f, 0f, 2f, 2f), 0f, Color.clear);
                UI51Build.Layout(dash, -1f, -1f, -1f, -1f, true);

                float iconW = i == BottomNav.Profilo ? 19f : 22f;
                var iconRt = UI51Build.Size(UI51Build.Child(item, "Icon"), iconW, 22f);
                var icon = UI51Build.Image(iconRt, UI51Build.Sprite("Common", icons[i]), UI51Tokens.WhiteA(0.55f));

                var labelRt = UI51Build.Size(UI51Build.Child(item, "Label"), 80f, 14f);
                var label = UI51Build.Text(labelRt, names[i], FontFace.NunitoSemiBold, 11f, UI51Tokens.CreamA(0.55f),
                    TextAlignmentOptions.Center);
                label.enableWordWrapping = false;

                items[i] = (button, dash.gameObject, icon, label);
            }

            UI51Build.Wire(nav, so =>
            {
                var list = so.FindProperty("m_Items");
                list.arraySize = items.Length;
                for (int i = 0; i < items.Length; i++)
                {
                    var el = list.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("button").objectReferenceValue = items[i].button;
                    el.FindPropertyRelative("dash").objectReferenceValue = items[i].dash;
                    el.FindPropertyRelative("icon").objectReferenceValue = items[i].icon;
                    el.FindPropertyRelative("label").objectReferenceValue = items[i].label;
                }
                UI51Build.Int(so, "m_SelectedIndex", BottomNav.Gioca);
            });
        }

        // --- Interruttore (mockup Impostazioni)

        internal static void BuildToggle(GameObject root)
        {
            var rt = UI51Build.Center((RectTransform)root.transform, 46f, 26f);
            // Riempimento, non tinta: la tinta spegnerebbe anche il bordo (vedi UI51Toggle.Apply).
            var track = UI51Build.Solid(rt, UI51Tokens.WhiteA(0.08f), 13f, 1f, UI51Tokens.GoldA(0.4f), true);

            var knobRt = UI51Build.Place(UI51Build.Child(rt, "Knob"), new Vector2(0f, 0.5f), new Vector2(18f, 18f), new Vector2(13f, 0f));
            knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = new Vector2(13f, 0f);
            var knob = UI51Build.Solid(knobRt, Color.white, 9f);
            knob.color = UI51Tokens.CreamA(0.6f);

            var toggle = UI51Build.GetOrAdd<UI51Toggle>(root);
            UI51Build.Wire(toggle, so =>
            {
                UI51Build.Ref(so, "m_Track", track);
                UI51Build.Ref(so, "m_Knob", knob);
            });
        }

        // --- Radice UI51 (SPEC §1)

        /// <summary>Canvas overlay 390x844 match .5, canali extra per UI51Shape, raycaster e figlio SafeArea.</summary>
        static void BuildRoot(GameObject root)
        {
            var canvas = UI51Build.GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                              AdditionalCanvasShaderChannels.TexCoord2 |
                                              AdditionalCanvasShaderChannels.TexCoord3;
            var scaler = UI51Build.GetOrAdd<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = UI51Tokens.ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UI51Tokens.MatchWidthOrHeight;
            UI51Build.GetOrAdd<GraphicRaycaster>(root);

            var safe = UI51Build.Stretch(UI51Build.Child(root.transform, "SafeArea"));
            UI51Build.GetOrAdd<SafeAreaFitter>(safe);
        }
    }
}
