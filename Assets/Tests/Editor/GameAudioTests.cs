#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Project51.EditorTools;
using Project51.Unity;
using UnityEngine;

namespace Project51.Tests
{
    public class GameAudioTests
    {
        [Test]
        public void ButtonSoundsFollowTheirRole()
        {
            Assert.AreEqual(SoundId.UiBack, GameAudio.ClassifyButton("CloseButton", "PanelFrame"));
            Assert.AreEqual(SoundId.UiBack, GameAudio.ClassifyButton("GuestBackButton", "GuestPanel"));
            Assert.AreEqual(SoundId.UiBack, GameAudio.ClassifyButton("Dim", "Panel"));
            Assert.AreEqual(SoundId.UiTab, GameAudio.ClassifyButton("GiocaSlot", "UIV2_BottomNav"));
            Assert.AreEqual(SoundId.UiTab, GameAudio.ClassifyButton("MazziTab", "TabsRow"));
            Assert.AreEqual(SoundId.UiTab, GameAudio.ClassifyButton("Tab_1", "PanelFrame"));
            Assert.AreEqual(SoundId.UiConfirm, GameAudio.ClassifyButton("Continue", "Design"));
            Assert.AreEqual(SoundId.UiConfirm, GameAudio.ClassifyButton("PlayAsGuestButton", "GuestPanel"));
            Assert.AreEqual(SoundId.UiConfirm, GameAudio.ClassifyButton("UIV2_PrimaryGoldButton", "PlayButtonSlot"));
            Assert.AreEqual(SoundId.UiClick, GameAudio.ClassifyButton("Toggle", "Music"));
            Assert.AreEqual(SoundId.UiClick, GameAudio.ClassifyButton("AccusoButton", "TableActionButtons"));
        }

        [Test]
        public void PanelsAndErrorsBeatTheButtonClick()
        {
            Assert.Greater(GameAudio.UiPriority(SoundId.PopupOpen), GameAudio.UiPriority(SoundId.UiClick));
            Assert.Greater(GameAudio.UiPriority(SoundId.PopupClose), GameAudio.UiPriority(SoundId.UiBack));
            Assert.Greater(GameAudio.UiPriority(SoundId.UiError), GameAudio.UiPriority(SoundId.PopupOpen));
        }

        [Test]
        public void MeasureFindsSilenceAndLoudestHit()
        {
            // 1 s a 100 campioni/s: silenzio fino a 0.20 s, fruscio a 0.20, colpo a 0.45.
            var data = new float[100];
            data[20] = 0.4f;
            data[45] = 1f;
            data[60] = 0.1f;
            SoundLibraryBuilder.MeasureSamples(data, 1, 100, false, out float onset, out float hit);
            Assert.AreEqual(0.20f, onset, 0.001f);
            Assert.AreEqual(0.45f, hit, 0.001f);
        }

        [Test]
        public void CardLandingIsTheLastStrongHit()
        {
            // Carta presa in mano (0.08, il piu' forte) e poi posata sul tavolo (0.45).
            var data = new float[100];
            data[8] = 1f;
            data[45] = 0.8f;
            data[70] = 0.2f;
            SoundLibraryBuilder.MeasureSamples(data, 1, 100, true, out _, out float hit);
            Assert.AreEqual(0.45f, hit, 0.001f);
            SoundLibraryBuilder.MeasureSamples(data, 1, 100, false, out _, out float loudest);
            Assert.AreEqual(0.08f, loudest, 0.001f);
        }

        [Test]
        public void LibraryHasEverySound()
        {
            var library = Resources.Load<SoundLibrary>(SoundLibrary.ResourcesPath);
            Assert.IsNotNull(library, "Manca Resources/Audio/SoundLibrary: lanciare Tools/Audio/Build Sound Library");
            Assert.IsNotNull(library.Music, "musica");
            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                var sound = library.Get(id);
                Assert.IsNotNull(sound, id.ToString());
                Assert.IsNotEmpty(sound.Variants, id.ToString());
                foreach (var variant in sound.Variants)
                {
                    Assert.IsNotNull(variant.Clip, id.ToString());
                    Assert.LessOrEqual(variant.Onset, variant.Hit, id + ": l'inizio viene prima del colpo");
                    Assert.Less(variant.Hit, variant.Clip.length, id.ToString());
                }
                Assert.That(sound.Volume, Is.InRange(0.05f, 1f), id.ToString());
            }
        }
    }
}
#endif
