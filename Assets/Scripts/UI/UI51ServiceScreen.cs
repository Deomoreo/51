using System;
using Project51.Auth;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// Schermate a tutto schermo che fermano l'app (mockup Aggiornamento e Manutenzione), sopra la Home e il caricamento.
    /// Aggiornamento: obbligatorio, solo AGGIORNA ORA. Manutenzione: conto alla rovescia fino alla fine prevista (a zero ricontrolla
    /// da sola), RIPROVA ricontrolla, "Leggi le novita'" apre Notizie sopra (la schermata torna quando Notizie si chiude).
    /// Prefab in Resources (UI51/Service, costruito da UI51SystemBuilder); i dati li porta ServiceGate.
    /// </summary>
    public sealed class UI51ServiceScreen : MonoBehaviour
    {
        public const string ResourcePath = "UI51/Service";

        [Header("Aggiornamento")]
        [SerializeField] private GameObject updateView;
        [SerializeField] private TMP_Text versionLabel;
        [SerializeField] private GameObject newsPanel;
        [SerializeField] private GameObject[] newsRows = new GameObject[0];
        [SerializeField] private TMP_Text[] newsTexts = new TMP_Text[0];
        [SerializeField] private TMP_Text installedLabel;
        [SerializeField] private Button updateNow;

        [Header("Manutenzione")]
        [SerializeField] private GameObject maintenanceView;
        [SerializeField] private RectTransform gear;
        [SerializeField] private TMP_Text countdown, endLabel;
        [SerializeField] private Button retry, readNews;

        private static UI51ServiceScreen s_Instance;
        private ServiceGate gate = ServiceGate.Open;
        private UI51NewsView news;
        private bool checking;

        public static bool IsShown => s_Instance != null && s_Instance.gate.state != ServiceGate.State.Open;

        /// <summary>Controlla e mostra (o toglie) la schermata giusta.</summary>
        public static void Check() => ServiceGate.Check(Apply);

        public static void Apply(ServiceGate gate)
        {
            if (gate == null || gate.state == ServiceGate.State.Open)
            {
                if (s_Instance != null) s_Instance.Show(ServiceGate.Open);
                return;
            }
            if (s_Instance == null)
            {
                var prefab = Resources.Load<UI51ServiceScreen>(ResourcePath);
                if (prefab == null) return;
                s_Instance = Instantiate(prefab);
                s_Instance.name = prefab.name;
                DontDestroyOnLoad(s_Instance.gameObject);
                Project51.UIV2.Animations.UIV2MotionInstaller.AddHaptics(s_Instance.gameObject); // 63b: vibra come gli altri pulsanti
            }
            s_Instance.Show(gate);
        }

        private void Awake()
        {
            updateNow.onClick.AddListener(OpenStore);
            retry.onClick.AddListener(Recheck);
            readNews.onClick.AddListener(ReadNews);
            updateView.SetActive(false);
            maintenanceView.SetActive(false);
        }

        private void Show(ServiceGate next)
        {
            gate = next;
            updateView.SetActive(gate.state == ServiceGate.State.Update);
            ShowMaintenance(gate.state == ServiceGate.State.Maintenance && !NewsOpen);
            if (gate.state == ServiceGate.State.Update)
            {
                versionLabel.text = "Versione " + gate.version + " disponibile";
                installedLabel.text = "Versione installata " + Application.version + " · I tuoi progressi sono salvati";
                newsPanel.SetActive(gate.news.Length > 0);
                for (int i = 0; i < newsRows.Length; i++)
                {
                    newsRows[i].SetActive(i < gate.news.Length);
                    if (i < gate.news.Length) newsTexts[i].text = gate.news[i];
                }
            }
            else if (gate.state == ServiceGate.State.Maintenance)
            {
                endLabel.text = "Fine prevista alle " + gate.endUtc.ToLocalTime().ToString("H:mm");
                readNews.gameObject.SetActive(FindObjectOfType<UI51NewsView>(true) != null);
                Tick();
            }
        }

        /// <summary>L'ingranaggio gira (6 s a giro) mentre la schermata si vede; disattivata, il giro si ferma da se'.</summary>
        private void ShowMaintenance(bool visible)
        {
            if (visible == maintenanceView.activeSelf) return;
            maintenanceView.SetActive(visible);
            if (visible) UIAnim.Spin(gear, 6f);
        }

        private void Update()
        {
            if (gate.state != ServiceGate.State.Maintenance) return;
            if (!NewsOpen) ShowMaintenance(true); // Notizie aperta da qui: la schermata torna quando si chiude
            Tick();
        }

        private void Tick()
        {
            var left = gate.endUtc - DateTime.UtcNow;
            countdown.text = ServiceGate.Countdown(left);
            if (left <= TimeSpan.Zero) Recheck(); // fine prevista: si riprova da soli (una volta, finche' la risposta non arriva)
        }

        private bool NewsOpen => news != null && news.IsOpen;

        private void Recheck()
        {
            if (checking) return;
            checking = true;
            // Una fine gia' passata vale "aperto" (ServiceGate.Parse): a zero la schermata se ne va, o riparte se la fine e' stata spostata.
            ServiceGate.Check(g =>
            {
                checking = false;
                if (this != null) Show(g);
            });
        }

        private void ReadNews()
        {
            news = FindObjectOfType<UI51NewsView>(true);
            if (news == null) return;
            news.Open();
            ShowMaintenance(false);
        }

        private void OpenStore()
        {
            string url = gate.link;
            if (string.IsNullOrEmpty(url) && Application.platform == RuntimePlatform.Android)
                url = "market://details?id=" + Application.identifier;
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }
    }
}
