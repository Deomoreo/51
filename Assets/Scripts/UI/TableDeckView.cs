using System.Collections;
using Project51.Core;
using Project51.UI51;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project51.Unity.UI
{
    /// <summary>
    /// UI51 Fase 5 (S4, mockup Partita): cuscino rosso col mazzo in alto a sinistra del tavolo, su un canvas nel mondo
    /// (ordine 5: sopra al feltro, sotto a tutte le carte). Segue il bordo del tavolo (CardViewManager.TryGetDeckPosition);
    /// il mazzo si vede finche' ci sono carte da distribuire. Toccandolo il mazzo si solleva e compare il medaglione
    /// con le carte rimaste, che sparisce 2,4 s dopo l'ultimo tocco. Tutto locale, niente rete.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class TableDeckView : MonoBehaviour
    {
        [SerializeField] RectTransform m_Stack;
        [SerializeField] Image m_Back;
        [SerializeField] RectTransform m_Medal;
        [SerializeField] TMP_Text m_Count;

        const float StackHeight = 50f; // mockup: dorso 34x50, qui largo quanto il dorso del mazzo in uso

        Canvas m_Canvas;
        GraphicRaycaster m_Raycaster;
        CardViewManager m_Cards;
        TurnController m_Turn;
        Coroutine m_Hide;

        void Awake()
        {
            m_Canvas = GetComponent<Canvas>();
            m_Raycaster = GetComponent<GraphicRaycaster>();
        }

        void Start()
        {
            m_Cards = FindObjectOfType<CardViewManager>();
            m_Turn = FindObjectOfType<TurnController>();
            if (m_Canvas.worldCamera == null) m_Canvas.worldCamera = Camera.main;
            var deck = CardDecks.LoadForMatch();
            var back = deck != null ? deck.Back : Resources.Load<Sprite>("Cards/CardBack");
            if (back != null && m_Back != null && m_Stack != null)
            {
                m_Back.sprite = back;
                m_Stack.sizeDelta = new Vector2(StackHeight * back.rect.width / back.rect.height, StackHeight);
            }
            if (m_Medal != null) m_Medal.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            // Senza tavolo misurabile il cuscino si spegne: mai un canvas a scala 1 davanti alle carte.
            Vector3 position = default;
            float unit = 0f;
            bool placed = m_Cards != null && m_Cards.TryGetDeckPosition(out position, out unit);
            if (m_Canvas.enabled != placed) m_Canvas.enabled = placed;
            if (m_Raycaster != null && m_Raycaster.enabled != placed) m_Raycaster.enabled = placed;
            if (!placed) return;

            var t = transform;
            if (t.position != position) t.position = position;
            var scale = new Vector3(unit, unit, 1f);
            if (t.localScale != scale) t.localScale = scale;

            var deck = m_Turn != null && m_Turn.GameState != null ? m_Turn.GameState.Deck : null;
            bool stack = deck != null && (deck.Count > 0 || m_Turn.IsDealInProgress);
            if (m_Stack != null && m_Stack.gameObject.activeSelf != stack) m_Stack.gameObject.SetActive(stack);
        }

        /// <summary>Tocco sul cuscino (Button collegato dal builder).</summary>
        public void Tap()
        {
            if (m_Medal == null || m_Count == null) return;
            // Con il medaglione gia' aperto un nuovo tocco lo tiene aperto e basta: prima sollevamento e conteggio
            // ripartivano da zero a ogni tocco (utente, 01/10).
            if (!m_Medal.gameObject.activeSelf)
            {
                var deck = m_Turn != null && m_Turn.GameState != null ? m_Turn.GameState.Deck : null;
                if (m_Stack != null && m_Stack.gameObject.activeInHierarchy) UIAnim.DeckLift(m_Stack);
                m_Medal.gameObject.SetActive(true);
                UIAnim.DeckPop(m_Medal);
                UIAnim.CountSteps(m_Count, 0, deck != null ? deck.Count : 0);
            }
            if (m_Hide != null) StopCoroutine(m_Hide);
            m_Hide = StartCoroutine(HideLater());
        }

        IEnumerator HideLater()
        {
            yield return new WaitForSecondsRealtime(2.4f);
            m_Medal.gameObject.SetActive(false);
            m_Hide = null;
        }
    }
}
