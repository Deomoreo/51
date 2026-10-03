using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using DG.Tweening;
using Project51.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.UIV2.Core
{
    public sealed class LegalModalV2 : MonoBehaviour
    {
        public AnimatedModalV2 Modal;
        public TMP_Text Title;
        public TMP_Text Body;
        public ScrollRect Scroll;

        [Header("Documenti (Assets/Legal)")]
        public TextAsset Terms;
        public TextAsset Privacy;

        [Header("UI51 (opzionali)")]
        public TMP_Text Subtitle;               // "Ultimo aggiornamento: ..."
        public RectTransform IndexRowTemplate;  // spento; figli TMP "Num" e "Label", Button sulla radice
        public RectTransform SectionTemplate;   // spento; figli TMP "Heading" e "Text"
        public Button Understood;

        public struct Section { public string Number, Title, Body; }

        private static readonly Regex Heading = new Regex(@"^##\s*(?:(\d+)\.\s*)?(.+)$");
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly Dictionary<string, RectTransform> byNumber = new Dictionary<string, RectTransform>();

        private void Awake()
        {
            if (Understood != null && Modal != null) Understood.onClick.AddListener(Modal.Close);
        }

        public void ShowTerms() => Show("Termini di servizio", Terms);
        public void ShowPrivacy() => Show("Privacy Policy", Privacy);

        /// <summary>Termini aperti direttamente su una sezione ("3" = Regole di comportamento, dalla Sospensione).</summary>
        public void ShowTerms(string section)
        {
            ShowTerms();
            if (byNumber.TryGetValue(section, out var target) && target != null)
                // Dopo l'apertura del pannello: prima le sezioni appena create non hanno ancora l'altezza finale.
                DOVirtual.DelayedCall(0.4f, () => ScrollTo(target), true).SetLink(target.gameObject);
        }

        private void Show(string title, TextAsset document)
        {
            if (Modal == null) return;
            if (Title != null) Title.text = title;
            if (SectionTemplate != null && document != null) Fill(document.text);
            else if (Body != null) Body.text = document != null ? LegalDocuments.Format(document) : "Testo non disponibile in questa versione dell'app.";
            if (UIV2ModalHost.Instance != null) UIV2ModalHost.Instance.Open(Modal); else Modal.Open();
            if (Scroll != null) { Canvas.ForceUpdateCanvases(); Scroll.verticalNormalizedPosition = 1f; }
        }

        /// <summary>
        /// Divide il documento: la riga "Ultimo aggiornamento" va nel sottotitolo, il testo prima del primo "##"
        /// e' l'introduzione, ogni "## N. Titolo" diventa una sezione (e una riga dell'indice).
        /// </summary>
        public static List<Section> ParseSections(string raw, out string updated, out string intro)
        {
            var sections = new List<Section>();
            var introLines = new StringBuilder();
            var bodyLines = new StringBuilder();
            updated = "";
            Section current = default;
            bool inSection = false;
            foreach (string line in (raw ?? "").Replace("\r\n", "\n").Split('\n'))
            {
                string t = line.TrimEnd();
                if (t.StartsWith("## "))
                {
                    if (inSection) { current.Body = LegalDocuments.Format(bodyLines.ToString()); sections.Add(current); }
                    var m = Heading.Match(t);
                    current = new Section { Number = m.Groups[1].Value, Title = m.Groups[2].Value.Trim() };
                    bodyLines.Clear();
                    inSection = true;
                }
                else if (inSection) bodyLines.Append(t).Append('\n');
                else if (t.StartsWith("Ultimo aggiornamento")) updated = t;
                else if (!t.StartsWith("#")) introLines.Append(t).Append('\n');
            }
            if (inSection) { current.Body = LegalDocuments.Format(bodyLines.ToString()); sections.Add(current); }
            intro = LegalDocuments.Format(introLines.ToString());
            return sections;
        }

        private void Fill(string raw)
        {
            foreach (var go in spawned) if (go != null) Destroy(go);
            spawned.Clear();
            byNumber.Clear();
            var sections = ParseSections(raw, out string updated, out string intro);
            if (Subtitle != null) Subtitle.text = updated;
            if (Body != null) { Body.text = intro; Body.gameObject.SetActive(intro.Length > 0); }
            foreach (var s in sections)
            {
                var section = Spawn(SectionTemplate);
                SetText(section, "Heading", string.IsNullOrEmpty(s.Number) ? s.Title : $"<color=#F3C969>{s.Number}.</color> {s.Title}");
                SetText(section, "Text", s.Body);
                byNumber[s.Number] = section;
                if (IndexRowTemplate == null) continue;
                var row = Spawn(IndexRowTemplate);
                SetText(row, "Num", s.Number);
                SetText(row, "Label", s.Title);
                var button = row.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => ScrollTo(section));
            }
        }

        private RectTransform Spawn(RectTransform template)
        {
            var rt = Instantiate(template, template.parent);
            rt.gameObject.SetActive(true);
            spawned.Add(rt.gameObject);
            return rt;
        }

        private static void SetText(Transform root, string child, string text)
        {
            var t = root.Find(child);
            var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
            if (tmp != null) tmp.text = text;
        }

        /// <summary>Porta la cima della sezione in cima al viewport (contenuto con pivot in alto).</summary>
        private void ScrollTo(RectTransform section)
        {
            if (Scroll == null || Scroll.content == null) return;
            var content = Scroll.content;
            var viewport = Scroll.viewport != null ? Scroll.viewport : (RectTransform)Scroll.transform;
            Scroll.StopMovement();
            content.DOKill();
            float from = content.anchoredPosition.y;
            // Bersaglio ricalcolato a ogni passo: alla prima apertura il frame lungo di Fill consuma il ritardo e le sezioni si assestano mentre scorre.
            DOVirtual.Float(0f, 1f, .35f, k =>
                {
                    float top = -content.InverseTransformPoint(section.TransformPoint(new Vector3(0, section.rect.yMax))).y;
                    float max = Mathf.Max(0, content.rect.height - viewport.rect.height);
                    content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.LerpUnclamped(from, Mathf.Clamp(top, 0, max), k));
                })
                .SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(content).SetLink(content.gameObject);
        }
    }
}
