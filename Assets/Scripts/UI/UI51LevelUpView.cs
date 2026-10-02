using DG.Tweening;
using Project51.Auth;
using Project51.Core;
using Project51.UI51;
using Project51.UIV2.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 15: LivelloSu (mockup LivelloSu) al ritorno in Home dopo partite che hanno fatto salire di livello
    /// (ProfileService.LevelUpFrom/To, dal server). Solo cose vere (scelta utente 02/10): titolo nuovo se cambia, sblocchi veri
    /// (oggi solo il banner Porpora al 10), barra verso il livello dopo; niente monete ne' forziere. Grafica: UI51ProgressBuilder.
    /// </summary>
    public sealed class UI51LevelUpView : MonoBehaviour
    {
        [SerializeField] private RectTransform rays, burst, header, medal, titleLine, body, buttons, xpFill;
        [SerializeField] private GameObject unlocks;
        [SerializeField] private TMP_Text level, titleText, xpLevel, xpValue;
        [SerializeField] private Button next;

        public static bool Pending => ProfileService.LevelUpTo > 0;

        private void Awake() => next.onClick.AddListener(() => gameObject.SetActive(false));

        public void Open()
        {
            int from = ProfileService.LevelUpFrom, to = ProfileService.LevelUpTo;
            ProfileService.LevelUpFrom = ProfileService.LevelUpTo = 0;
            if (to <= 0) return;
            var profile = AuthBootstrapper.Instance != null ? AuthBootstrapper.Instance.Profile : null;
            int xp = profile != null ? profile.XP : PlayerXp.TotalForLevel(to);

            gameObject.SetActive(true);
            level.text = from.ToString();
            DOVirtual.DelayedCall(GamePreferences.Scaled(0.65f), () => { if (level != null) level.text = to.ToString(); })
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            string before = PlayerXp.Title(from), after = PlayerXp.Title(to);
            titleLine.gameObject.SetActive(before != after);
            titleText.text = "Titolo nuovo: " + before + " → " + after;
            unlocks.SetActive(from < ProfileCosmetics.PorporaLevel && to >= ProfileCosmetics.PorporaLevel);
            xpLevel.text = "Liv. " + to;
            int inLevel = Mathf.Max(0, xp - PlayerXp.TotalForLevel(to)), need = PlayerXp.XpToNext(to);
            xpValue.text = inLevel + " / " + need + " XP";
            LayoutRebuilder.ForceRebuildLayoutImmediate(body);

            UIAnim.FadeIn((RectTransform)transform, 0.25f);
            UIAnim.Rays(rays);
            UIAnim.RewardBurst(burst);
            UIAnim.Pop(header);
            UIAnim.PopTitle(medal, 0.1f);
            if (titleLine.gameObject.activeSelf) UIAnim.FadeUp(titleLine, 0.6f);
            UIAnim.FadeUp(body, 0.9f);
            UIAnim.FadeUp(buttons, 1.1f);
            UIAnim.Fill(xpFill, 0f, (float)inLevel / need, 0.8f, 1.2f);
        }
    }
}
