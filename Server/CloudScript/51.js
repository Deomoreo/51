// 51 - CloudScript (PlayFab Legacy CloudScript, JavaScript ES5).
// Caricarlo da Game Manager: Automation -> CloudScript -> Revisions (Legacy) -> Upload new revision -> Deploy.
// Il telefono non da' mai monete o gemme da solo: chiama queste funzioni (ExecuteCloudScript) e il server decide.
// Da caricare e' 51.carica.js, non questo file: lo scrive "node Server/CloudScript/carica.js" mettendo al posto di __HOOK__ il
// segreto dei nomi dei webhook (Server/CloudScript/segreto.txt, fuori da git: il repository e' pubblico).
// Ordine: Upload e Deploy di 51.carica.js, subito dopo i nomi nel pannello Photon (al contrario Photon chiamerebbe nomi che la
// revisione in uso non ha, e l'errore che arriva al telefono puo' contenere l'URL del webhook). segreto.txt non si perde: senza,
// carica.js si ferma (vedi carica.js).
//
// Dati usati (Game Manager):
//   Economy -> Currencies: "CO" (monete) e "GE" (gemme).
//   Players -> Player Data -> Read Only: "Posta" (messaggi), "Premi" (serie dei premi giornalieri), "PostaGlobaleRicevuta",
//     "Cons_<id>" (consegne di monete e gemme, vedi deliver), "Partite" (biglietti delle partite, vedi inizioPartita),
//     "PartiteOggi" (monete date oggi dalle partite, anche quelle non verificabili per tipo), "RisultatiOggi" (risultati contati oggi),
//     "SenzaBiglietto" (risultati senza biglietto di oggi): scritti insieme alla consegna (prima del tetto D11 erano in Internal Data).
//   Players -> Internal Data: moderazione: "SegnalazioniPartite", "Abbandoni",
//     "Sospensione" ({"volte":1,"fine":"2026-10-03T18:00:00Z","motivo":"abbandoni"}: si puo' anche scrivere a mano per sospendere
//     qualcuno, motivo "segnalazioni", "abbandoni" o un testo libero), "SilenzioEmoticon" (emoticon spente, stesso formato),
//     "StoricoModerazione" (tutte le sanzioni, non si azzera mai) ed "EsitoSegnalazione" (lista, per chi aveva segnalato).
//     Dalla 2.62 le chiavi della moderazione hanno nomi nuovi ("Sanzioni", "Silenzi", "Segnalazioni", "StoricoSanzioni" non contano
//     piu'): un telefono
//     puo' far girare le revisioni vecchie di questo script (RevisionSelection), che scrivevano quelle senza i controlli di adesso.
//   Players -> Statistics: "TotalGames", "Wins", "XP", "Level", "TotalScope" le scrive solo premioPartita (in Title settings ->
//     API Features togliere "Allow client to post player statistics").
//   Content -> Title Data (facoltativi): "Economia" (valori qui sotto), "PostaGlobale" (messaggi per tutti, stesso formato di "Posta"),
//     "Moderazione" (soglie e durate, come MOD_DEFAULTS).

// In PlayFab "handlers" esiste gia'; con Node (test.js) no.
if (typeof handlers === "undefined") var handlers = {};

var DEFAULTS = {
    forzieri: {
        verde: { monete: [150, 300], gemme: [3, 8] },
        viola: { monete: [400, 800], gemme: [10, 25] }
    },
    // Giorni 1-7 (giorno 7 = gran premio). Stesso ordine della pagina Premi.
    settimana: [
        [{ tipo: "monete", quantita: 50 }],
        [{ tipo: "monete", quantita: 100 }],
        [{ tipo: "gemme", quantita: 5 }],
        [{ tipo: "monete", quantita: 150 }],
        [{ tipo: "forziere", colore: "verde" }],
        [{ tipo: "gemme", quantita: 10 }],
        [{ tipo: "forziere", colore: "viola" }, { tipo: "gemme", quantita: 25 }]
    ],
    partita: { vittoria: 40, sconfitta: 20, tetto: 400, abbandoni: 3, partiteGiorno: 60 },
    // B32 (TU3, scelta 07/10): fine del tutorial, una volta per account.
    tutorial: [{ tipo: "monete", quantita: 200 }]
};

// Scelte dell'utente 01/10: 5 account diversi da almeno 3 partite diverse, o 5 abbandoni, nei 7 giorni. Sospensione 24 ore, 3 giorni,
// 3 giorni, poi 7; emoticon spente 24 ore, 3 giorni, poi 7. Dopo 30 giorni senza sanzioni si riparte dalla prima durata.
var MOD_DEFAULTS = { segnalazioni: 5, partite: 3, abbandoni: 5, giorni: 7, ore: [24, 72, 72, 168], silenzio: [24, 72, 168], azzeramento: 30 };
var REPORT_REASONS = { emoticon: 1, gioco: 1, nome: 1 };
var SOSP = "Sospensione", MUTE = "SilenzioEmoticon", REPORTS = "SegnalazioniPartite";
var HISTORY = "StoricoModerazione", HISTORY_MAX = 100;

var MAIL_DAYS = 30;

// --- Utilita'

function titleData(keys) {
    var r = server.GetTitleData({ Keys: keys });
    return (r && r.Data) || {};
}

function economy() {
    var raw = titleData(["Economia"]).Economia;
    if (!raw) return DEFAULTS;
    try {
        var e = JSON.parse(raw);
        return {
            forzieri: e.forzieri || DEFAULTS.forzieri,
            settimana: e.settimana || DEFAULTS.settimana,
            partita: e.partita || DEFAULTS.partita,
            tutorial: e.tutorial || DEFAULTS.tutorial
        };
    } catch (ex) {
        log.error("Economia non valida: " + ex);
        return DEFAULTS;
    }
}

function dataValues(r, keys) {
    var out = {};
    var data = (r && r.Data) || {};
    for (var i = 0; i < keys.length; i++) out[keys[i]] = data[keys[i]] ? data[keys[i]].Value : null;
    return out;
}

function readOnly(keys) {
    return dataValues(server.GetUserReadOnlyData({ PlayFabId: currentPlayerId, Keys: keys }), keys);
}

function saveReadOnly(values) {
    server.UpdateUserReadOnlyData({ PlayFabId: currentPlayerId, Data: values });
}

/** Internal Data (il telefono non la vede): del giocatore corrente o di un altro (id). */
function internal(keys, id) {
    return dataValues(server.GetUserInternalData({ PlayFabId: id || currentPlayerId, Keys: keys }), keys);
}

function saveInternal(data, id) {
    server.UpdateUserInternalData({ PlayFabId: id || currentPlayerId, Data: data });
}

function parseObject(raw) {
    try {
        var v = raw ? JSON.parse(raw) : null;
        return v && typeof v === "object" && !Array.isArray(v) ? v : null;
    } catch (ex) {
        return null;
    }
}

function parseList(raw) {
    if (!raw) return [];
    try {
        var v = JSON.parse(raw);
        if (v && v.messaggi) v = v.messaggi; // anche {"messaggi":[...]}
        return Array.isArray(v) ? v : [];
    } catch (ex) {
        return [];
    }
}

function randomInt(range) {
    if (!Array.isArray(range)) return range | 0;
    var lo = range[0] | 0, hi = range[1] | 0;
    return lo + Math.floor(Math.random() * (hi - lo + 1));
}

/** Data italiana "AAAA-MM-GG": ora legale dall'ultima domenica di marzo all'ultima di ottobre (01:00 UTC). */
function italianDate(ms) {
    var d = new Date(ms);
    var y = d.getUTCFullYear();
    var start = lastSundayUtc(y, 2), end = lastSundayUtc(y, 9);
    var offset = ms >= start && ms < end ? 2 : 1;
    return new Date(ms + offset * 3600000).toISOString().substring(0, 10);
}

function lastSundayUtc(year, month) {
    var d = new Date(Date.UTC(year, month + 1, 0, 1, 0, 0)); // ultimo giorno del mese, 01:00 UTC
    d.setUTCDate(d.getUTCDate() - d.getUTCDay());
    return d.getTime();
}

function previousDate(date) {
    var d = new Date(date + "T12:00:00Z");
    d.setUTCDate(d.getUTCDate() - 1);
    return d.toISOString().substring(0, 10);
}

/** Secondi fino alla prossima mezzanotte italiana. */
function secondsToItalianMidnight(ms) {
    var today = italianDate(ms), s = 0;
    // al massimo 25 ore: si avanza di 15 minuti e poi si rifinisce al secondo
    while (italianDate(ms + (s + 900) * 1000) === today && s < 90000) s += 900;
    while (italianDate(ms + (s + 1) * 1000) === today && s < 90000) s += 1;
    return s + 1;
}

// --- Premi: monete, gemme, forzieri (aperti subito, scelta dell'utente 01/10)

/** Cosa vale un premio: monete e gemme in tutto, con i forzieri gia' aperti ([{tipo:"forziere", colore, monete, gemme}]). */
function resolve(gifts, eco) {
    var coins = 0, gems = 0, got = [];
    for (var i = 0; i < gifts.length; i++) {
        var g = gifts[i] || {};
        var tipo = String(g.tipo || "").toLowerCase();
        if (tipo === "monete") coins += Math.max(0, g.quantita | 0);
        else if (tipo === "gemme") gems += Math.max(0, g.quantita | 0);
        else if (tipo === "forziere") {
            var colore = g.colore === "verde" ? "verde" : "viola";
            var chest = eco.forzieri[colore] || DEFAULTS.forzieri[colore];
            var n = Math.max(1, g.quantita | 0);
            for (var k = 0; k < n; k++) {
                var c = randomInt(chest.monete), m = randomInt(chest.gemme);
                coins += c; gems += m;
                got.push({ tipo: "forziere", colore: colore, monete: c, gemme: m });
            }
        }
    }
    return { monete: coins, gemme: gems, forzieri: got };
}

/**
 * Secondo giro 08/10: il saldo vero di monete e gemme nella risposta (saldoMonete/saldoGemme), anche quando non si e' pagato niente
 * ("gia' riscattato", errore): il telefono mostra sempre quello del server, mai uno suo. Solo le valute non ancora note.
 */
function balance(out) {
    if (out.saldoMonete >= 0 && out.saldoGemme >= 0) return out;
    try {
        var vc = server.GetUserInventory({ PlayFabId: currentPlayerId }).VirtualCurrency || {};
        if (!(out.saldoMonete >= 0)) out.saldoMonete = typeof vc.CO === "number" ? vc.CO : -1;
        if (!(out.saldoGemme >= 0)) out.saldoGemme = typeof vc.GE === "number" ? vc.GE : -1;
    } catch (ex) {
        log.error("GetUserInventory: " + apiError(ex));
    }
    return out;
}

/** Messaggio di un errore delle API server di PlayFab (ex.apiErrorInfo), o il testo dell'eccezione. */
function apiError(ex) {
    var e = ex && ex.apiErrorInfo && ex.apiErrorInfo.apiError;
    return e ? e.error + " " + e.errorCode + ": " + e.errorMessage : String(ex);
}

// --- Consegna dei premi (09/10)
//
// AddUserVirtualCurrency (Economy legacy) non ha un id di transazione: se la risposta si perde nessuna API dice se quell'accredito e'
// arrivato, e il saldo non lo prova (altri accrediti, spese future). Quindi "esattamente una volta" non si puo' garantire: si garantisce
// "al massimo una volta", e ogni accredito senza esito certo resta scritto come "incerto" per la riconciliazione (Server/QA/LEGGIMI.md).
// Per arrivare a "esattamente una volta" serve Economy v2 (operazioni con IdempotencyId).

/**
 * Lucchetto del giocatore: consumo dei premi (Premi, Posta, Tutorial, Posta per tutti) e accrediti, una chiamata alla volta per account,
 * anche da due telefoni o con Home e fine partita insieme. CreateSharedGroup con un id scelto fallisce se il gruppo c'e' gia'
 * (documentato da PlayFab): e' l'unica scrittura condizionata delle API legacy. Un id per minuto; nei primi 15 s di un minuto si prende
 * anche quello del minuto prima, che chi e' partito da meno di 15 s puo' ancora tenere (uno script dura al massimo pochi secondi).
 * Uno script fermato da PlayFab non cancella il suo gruppo (nessuno lo cancella: resta come avanzo), ma quell'id non lo usa piu' nessuno
 * dopo il suo minuto e i 15 s del successivo: blocca al massimo 75 s. Esclusione: due chiamate partite a meno di 15 s l'una dall'altra
 * chiedono almeno un id uguale, e CreateSharedGroup lo da' a una sola (provato su 10A53D: 20 creazioni insieme, 1 riuscita).
 * Ritorna gli id presi, o null se e' occupato.
 */
var LOCK_MS = 60000, LOCK_GUARD_MS = 15000;

function acquire() {
    var now = Date.now(), w = Math.floor(now / LOCK_MS), held = [];
    var ids = ["consegne_" + currentPlayerId + "_" + w];
    if (now - w * LOCK_MS < LOCK_GUARD_MS) ids.push("consegne_" + currentPlayerId + "_" + (w - 1));
    for (var i = 0; i < ids.length; i++) {
        try { server.CreateSharedGroup({ SharedGroupId: ids[i] }); held.push(ids[i]); }
        catch (ex) { release(held); return null; } // c'e' gia' (o PlayFab non risponde): occupato, non si tocca niente
    }
    return held;
}

function release(held) {
    for (var i = 0; held && i < held.length; i++) {
        try { server.DeleteSharedGroup({ SharedGroupId: held[i] }); } catch (ex) { log.error("DeleteSharedGroup: " + apiError(ex)); }
    }
}

/** fn sotto il lucchetto; occupato: busy(args) se c'e', altrimenti {ok:false, occupato:true}. */
function locked(fn, busy) {
    return function (args, context) {
        var held = acquire();
        if (!held) return busy ? busy(args) : { ok: false, occupato: true, errore: "occupato: riprova tra poco" };
        try { return fn(args, context); } finally { release(held); }
    };
}

// MAX_RETRY: consegne vecchie ritentate per chiamata. PlayFab ha un tetto di chiamate API per esecuzione (Title settings -> Limits):
// superarlo a meta' di un accredito lascerebbe "incerto" un premio che si poteva ritentare.
// MAX_TENTATIVI: rifiuti certi di fila dopo cui una consegna diventa "fermo" (mai accreditata, non si ritenta piu' da sola: errore
// permanente, si rimette in attesa con "qa.js riconcilia" dopo averne tolto la causa).
var CONS = "Cons_", CODES = ["CO", "GE"], MAX_RETRY = 1, MAX_WRITE = 10, MAX_TENTATIVI = 10;

/**
 * Consegna: chiave Read Only "Cons_<id>" (una per premio, nessuno la riscrive intera da una lettura vecchia) = {id, t, CO, GE, stato}
 * con monete e gemme ancora da accreditare. stato: "attesa" (da accreditare), "inCorso" (accredito partito), "incerto" (da
 * riconciliare: dubbio = la valuta senza esito; le valute dopo non sono state tentate), "fermo" (rifiutata MAX_TENTATIVI volte: di
 * sicuro non accreditata, non si ritenta da sola).
 */
function entryOf(id, prize) {
    return { id: String(id), t: new Date(Date.now()).toISOString(), CO: prize.monete | 0, GE: prize.gemme | 0, stato: "attesa" };
}

/** Id unico di una consegna senza un premio "una volta sola" dietro (Posta, partite). */
function newId(kind) { return kind + Date.now().toString(36) + Math.floor(Math.random() * 46656).toString(36); }

function pending(e) { return !!e && (e.CO > 0 || e.GE > 0); }

/** Valute ancora da accreditare di una consegna ("CO,GE"). */
function owed(e) { return CODES.filter(function (c) { return e[c] > 0; }).join(","); }

/** Solo sul titolo di sviluppo (QA = true lo mette "node Server/QA/qa.js deploy"): guasto finto per l'account di prova. */
var QA = false;

function qaFault() {
    return QA ? internal(["QAGuasto"]).QAGuasto || null : null;
}

function spin(ms) { var end = Date.now() + ms; while (Date.now() < end) { /* QA: risposta lenta o script fermato da PlayFab */ } }

function saveData(write, remove) {
    if (Object.keys(write).length === 0 && remove.length === 0) return;
    var r = { PlayFabId: currentPlayerId, Data: write };
    if (remove.length) r.KeysToRemove = remove;
    server.UpdateUserReadOnlyData(r);
}

/**
 * Consegna sicura, solo col lucchetto preso. Il premio consumato (consume: "Premi", "Posta", "Tutorial") si scrive in una sola chiamata
 * con la sua consegna gia' "inCorso"; poi l'accredito; poi l'esito: arrivata = chiave tolta; "no" certo di PlayFab (apiErrorInfo, anche
 * una valuta che non esiste) = "attesa", si ritenta alla chiamata dopo (al massimo MAX_RETRY per chiamata); nessuna risposta = "incerto".
 * Una consegna trovata "inCorso" e' di una chiamata morta a meta' (col lucchetto nessun'altra e' viva): diventa "incerto". Le incerte
 * non si ritentano mai da sole. Le consegne della lista "Consegne" (revisione del terzo giro 08/10): un "no" certo torna in attesa, il
 * resto e' incerto (quella revisione decideva dal saldo, che non e' una prova). maxRetry: quante consegne vecchie ritentare.
 */
function deliver(entry, consume, maxRetry) {
    var out = { monete: 0, gemme: 0, recuperoMonete: 0, recuperoGemme: 0, saldoMonete: -1, saldoGemme: -1, inConsegna: false, incerto: false, errore: null };
    var data = server.GetUserReadOnlyData({ PlayFabId: currentPlayerId }).Data || {};
    var write = {}, remove = [], todo = [], dead = [], k, e, i, c;
    for (k in data) {
        if (!data.hasOwnProperty(k) || k.indexOf(CONS) !== 0) continue;
        e = parseObject(data[k].Value);
        if (!e) continue;
        if (e.stato === "inCorso") dead.push({ k: k, e: e });
        else if (e.stato === "attesa" && pending(e)) todo.push({ k: k, e: e });
    }
    // Prima le meno rifiutate: una consegna che PlayFab rifiuta sempre non blocca le altre (stabile: a parita' l'ordine delle chiavi).
    todo = todo.map(function (t, n) { return { t: t, n: n }; })
        .sort(function (a, b) { return (a.t.e.tentativi | 0) - (b.t.e.tentativi | 0) || a.n - b.n; })
        .slice(0, maxRetry === undefined ? MAX_RETRY : maxRetry).map(function (x) { return x.t; });
    if (pending(entry)) todo.unshift({ k: CONS + entry.id, e: entry, mine: true });
    for (i = 0; i < todo.length; i++) { todo[i].e.stato = "inCorso"; write[todo[i].k] = JSON.stringify(todo[i].e); }
    merge(write, consume || {});
    // PlayFab scrive al massimo 10 chiavi per chiamata: il resto al giro dopo.
    function room() { return Object.keys(write).length + remove.length < MAX_WRITE; }
    for (i = 0; i < dead.length && room(); i++) {
        e = dead[i].e;
        e.stato = "incerto"; e.errore = "accredito interrotto: esito sconosciuto"; e.dubbio = owed(e);
        write[dead[i].k] = JSON.stringify(e);
    }
    if (data.Consegne) {
        var old = parseList(data.Consegne.Value), now36 = Date.now().toString(36);
        while (old.length && room()) {
            e = old.shift();
            var sure = true;
            for (i = 0; i < CODES.length; i++) if (e[CODES[i]] > 0 && !(e.rifiutato && e.rifiutato[CODES[i]])) sure = false;
            var moved = { id: String(e.id), t: e.t, CO: e.CO | 0, GE: e.GE | 0, stato: sure ? "attesa" : "incerto" };
            if (!sure) { moved.errore = "registro del terzo giro: esito non certo"; moved.dubbio = owed(moved); }
            if (pending(moved)) write[CONS + "v" + now36 + old.length] = JSON.stringify(moved);
        }
        if (old.length) write.Consegne = JSON.stringify(old); else remove.push("Consegne");
    }
    saveData(write, remove);
    var fault = todo.length ? qaFault() : null;
    if (fault === "lento") spin(3000);
    if (fault === "timeout") spin(60000); // PlayFab ferma lo script: consegne "inCorso", al giro dopo "incerto"
    write = {}; remove = [];
    for (i = 0; i < todo.length; i++) {
        var t = todo[i], refused = null, lost = null;
        e = t.e;
        for (var j = 0; j < CODES.length && !lost; j++) {
            c = CODES[j];
            var n = e[c] | 0;
            if (n <= 0) continue;
            try {
                if (fault === "rifiuto") throw { apiErrorInfo: { apiError: { error: "QARifiuto", errorCode: 0, errorMessage: "rifiuto di prova" } } };
                var r = server.AddUserVirtualCurrency({ PlayFabId: currentPlayerId, VirtualCurrency: c, Amount: n });
                if (fault === "incerto") throw new Error("QA: risposta persa dopo l'accredito");
                e[c] = 0;
                if (r && typeof r.Balance === "number") out[c === "CO" ? "saldoMonete" : "saldoGemme"] = r.Balance;
                if (t.mine) out[c === "CO" ? "monete" : "gemme"] += n;
                else out[c === "CO" ? "recuperoMonete" : "recuperoGemme"] += n;
            } catch (ex) {
                var msg = "AddUserVirtualCurrency " + c + ": " + apiError(ex);
                if (ex && ex.apiErrorInfo) refused = msg; else lost = msg;
                out.errore = msg;
                log.error(msg);
            }
        }
        if (lost) { e.stato = "incerto"; e.errore = lost; e.dubbio = c; } // le valute dopo c non sono state tentate
        else if (pending(e)) {
            e.errore = refused; e.tentativi = (e.tentativi | 0) + 1;
            e.stato = e.tentativi >= MAX_TENTATIVI ? "fermo" : "attesa";
        }
        if (pending(e)) write[t.k] = JSON.stringify(e); else remove.push(t.k);
        if (t.mine) { out.inConsegna = pending(e); out.incerto = !!lost; }
    }
    saveData(write, remove);
    return balance(out);
}

/**
 * Risposta dopo un premio: quanto e' arrivato, forzieri (solo se e' arrivato tutto), saldo vero, premi recuperati da prima. inConsegna =
 * premio consumato ma non ancora accreditato, niente festa sul telefono (ok=false): arriva a un tentativo successivo, oppure, se
 * incerto, dopo la riconciliazione.
 */
function paid(base, got, prize) {
    base.monete = got.monete; base.gemme = got.gemme; base.forzieri = prize && !got.inConsegna ? prize.forzieri : [];
    base.saldoMonete = got.saldoMonete; base.saldoGemme = got.saldoGemme;
    base.recuperoMonete = got.recuperoMonete; base.recuperoGemme = got.recuperoGemme;
    if (got.errore) base.errore = got.errore;
    if (got.inConsegna) { base.ok = false; base.inConsegna = true; }
    if (got.incerto) base.incerto = true;
    return base;
}

// --- Posta

function mailDate(m) {
    var t = Date.parse(m && m.data);
    return isNaN(t) ? 0 : t;
}

function canClaim(m, now) {
    if (!m || m.riscattato || !(Array.isArray(m.allegati)) || m.allegati.length === 0) return false;
    var scade = Date.parse(m.scade);
    if (!isNaN(scade) && scade + 86400000 <= now) return false; // valido fino a fine giornata
    var d = mailDate(m); // senza data (scritto a mano in Game Manager): niente limite d'eta', come lo mostra il telefono
    return !d || now - d <= MAIL_DAYS * 86400000;
}

/** Copia nella Posta i messaggi di "PostaGlobale" non ancora ricevuti (Benvenuto, risarcimenti...). */
function deliverGlobalMail(now) {
    var globals = parseList(titleData(["PostaGlobale"]).PostaGlobale);
    if (globals.length === 0) return 0;
    // Agli ospiti niente (scelta dell'utente 07/10): il loro account e' usa e getta; la ricevono dal primo avvio con un account.
    var me = server.GetUserAccountInfo({ PlayFabId: currentPlayerId });
    if (!(me && me.UserInfo && me.UserInfo.Username)) return 0;
    var data = readOnly(["Posta", "PostaGlobaleRicevuta"]);
    var mail = parseList(data.Posta);
    var received = parseList(data.PostaGlobaleRicevuta);
    var added = 0;
    for (var i = 0; i < globals.length; i++) {
        var g = globals[i];
        if (!g || !g.id || received.indexOf(g.id) >= 0) continue;
        var scade = Date.parse(g.scade);
        if (!isNaN(scade) && scade + 86400000 <= now) continue;
        var copy = JSON.parse(JSON.stringify(g));
        copy.riscattato = false;
        if (!copy.data) copy.data = new Date(now).toISOString().substring(0, 10);
        mail.push(copy);
        received.push(g.id);
        added++;
    }
    if (added > 0) saveReadOnly({ Posta: JSON.stringify(pruneMail(mail, now)), PostaGlobaleRicevuta: JSON.stringify(received) });
    return added;
}

/** Toglie i messaggi piu' vecchi di 30 giorni (come fa gia' il telefono). */
function pruneMail(mail, now) {
    var out = [];
    for (var i = 0; i < mail.length; i++) {
        var d = mailDate(mail[i]);
        if (!d || now - d <= MAIL_DAYS * 86400000) out.push(mail[i]);
    }
    return out;
}

function claimMail(ids, now) {
    if (isGuest()) return { ok: false, ospite: true };
    var eco = economy(), mail = parseList(readOnly(["Posta"]).Posta);
    var gifts = [], claimed = [], done = [];
    for (var i = 0; i < mail.length; i++) {
        var m = mail[i];
        var id = m && (m.id || m.titolo); // senza id il telefono usa il titolo (MailService)
        if (ids !== null && ids.indexOf(id) >= 0 && m.riscattato) done.push(id);
        if ((ids === null || ids.indexOf(id) >= 0) && canClaim(m, now)) {
            gifts = gifts.concat(m.allegati);
            m.riscattato = true;
            claimed.push(id);
        }
    }
    // Secondo giro 08/10: un secondo tocco o un nuovo tentativo dopo un riscatto andato a buon fine dice "gia'" con l'id, cosi' il
    // telefono segna il messaggio riscattato invece di riproporlo.
    // Il no porta comunque il saldo e ritenta le consegne rimaste (deliver).
    if (claimed.length === 0) return paid(done.length ? { ok: false, gia: true, riscattati: done } : { ok: false, errore: "niente da riscattare" }, deliver(null, null));
    // Messaggi riscattati e consegna scritti insieme, poi l'accredito (deliver): un errore a meta' non fa riscattare due volte ne' perde
    // il premio; due riscatti insieme (anche da due telefoni) li mette in fila il lucchetto (claimMail gira solo sotto locked).
    var prize = resolve(gifts, eco);
    return paid({ ok: true, riscattati: claimed }, deliver(entryOf(newId("m"), prize), { Posta: JSON.stringify(mail) }), prize);
}

// --- Premi giornalieri

function dailyState(now) {
    var raw = readOnly(["Premi"]).Premi, state = null;
    try { state = raw ? JSON.parse(raw) : null; } catch (ex) { state = null; }
    var today = italianDate(now);
    var last = state && state.ultimo, day = state && state.giorno | 0;
    var claimed = last === today;
    var current = claimed ? day : last === previousDate(today) ? day % 7 + 1 : 1;
    return { giorno: Math.max(1, Math.min(7, current)), riscattato: claimed, secondi: secondsToItalianMidnight(now) };
}

// --- Moderazione: segnalazioni e abbandoni sospendono il gioco online (contro i bot si gioca sempre).

function moderation() {
    var raw = titleData(["Moderazione"]).Moderazione, m = parseObject(raw);
    if (raw && !m) log.error("Moderazione non valida");
    m = m || {};
    function count(k) { return m[k] > 0 ? m[k] | 0 : MOD_DEFAULTS[k]; }
    function hours(k) { return Array.isArray(m[k]) && m[k].length > 0 ? m[k] : MOD_DEFAULTS[k]; }
    return {
        segnalazioni: count("segnalazioni"), partite: count("partite"), abbandoni: count("abbandoni"),
        giorni: m.giorni > 0 ? +m.giorni : MOD_DEFAULTS.giorni, azzeramento: m.azzeramento > 0 ? +m.azzeramento : MOD_DEFAULTS.azzeramento,
        ore: hours("ore"), silenzio: hours("silenzio")
    };
}

/** Solo quelli degli ultimi "giorni" giorni (t in millisecondi). */
function recent(list, now, days) {
    var out = [];
    for (var i = 0; i < list.length; i++) if (list[i] && now - (list[i].t || 0) < days * 86400000) out.push(list[i]);
    return out;
}

/** Quante "chiavi" diverse nella lista (giocatori, partite). */
function distinct(list, key) {
    var seen = {}, n = 0;
    for (var i = 0; i < list.length; i++) if (!seen["k" + list[i][key]]) { seen["k" + list[i][key]] = true; n++; }
    return n;
}

/** Sospensione (o silenzio) in corso per il telefono, oppure null. */
function activeSuspension(s, now) {
    var fine = s ? Date.parse(s.fine) : NaN;
    if (isNaN(fine) || fine <= now) return null;
    return { secondi: Math.ceil((fine - now) / 1000), motivo: String(s.motivo || ""), volte: s.volte | 0, fine: String(s.fine) };
}

/**
 * Nuova sanzione: la durata cresce con le volte (l'ultima della lista vale per tutte le successive) e riparte dalla prima dopo
 * "azzeramento" giorni puliti. Ogni sanzione va anche nello storico, che non si azzera mai. Ritorna i dati da salvare.
 */
function sanction(prev, motivo, now, mod, kind, id) {
    var last = Date.parse(prev.fine), hours = kind === MUTE ? mod.silenzio : mod.ore;
    var volte = (!isNaN(last) && now - last > mod.azzeramento * 86400000 ? 0 : prev.volte | 0) + 1;
    var ore = +hours[Math.min(volte, hours.length) - 1] || 24;
    var s = { volte: volte, fine: new Date(now + ore * 3600000).toISOString(), motivo: motivo };
    var history = parseList(internal([HISTORY], id)[HISTORY]);
    history.push({ tipo: kind === MUTE ? "emoticon" : "online", motivo: motivo, inizio: new Date(now).toISOString(), fine: s.fine, ore: ore });
    // ponytail: storico tagliato agli ultimi HISTORY_MAX (i dati di PlayFab hanno un limite di dimensione per chiave).
    var out = {};
    out[HISTORY] = JSON.stringify(history.slice(-HISTORY_MAX));
    out[kind] = JSON.stringify(s);
    return out;
}

function merge(a, b) {
    for (var k in b) if (b.hasOwnProperty(k)) a[k] = b[k];
    return a;
}

// --- Funzioni chiamate dal telefono

/**
 * Solo per il telefono che chiama, per se stesso. Anche queste funzioni si possono raggiungere dall'URL dei webhook di Photon (col suo
 * segreto, che Photon stesso dice di non poter tenere nascosto del tutto): da li' il giocatore lo sceglie il campo UserId, che il
 * telefono non manda mai. Con UserId, o senza giocatore, non si fa niente.
 */
function phoneOnly(fn) {
    return function (args, context) {
        if (typeof currentPlayerId !== "string" || currentPlayerId === "" || (args && args.UserId !== undefined))
            return { ok: false, errore: "non consentito" };
        return fn(args, context);
    };
}

/** All'avvio: consegna la Posta per tutti, ritenta le consegne rimaste (recuperoMonete/recuperoGemme) e ritorna lo stato dei premi. */
handlers.inizio = phoneOnly(function (args) {
    var now = Date.now();
    try { pruneRooms(currentPlayerId, internal([ROOMS])[ROOMS], now, null, 5); } catch (ex) { log.error("pruneRooms: " + ex); }
    // Occupato (un'altra chiamata dei premi in corso): solo lo stato; Posta per tutti e consegne alla prossima chiamata.
    return locked(function () {
        var nuovi = deliverGlobalMail(now), s = dailyState(now);
        return paid({ ok: true, postaNuova: nuovi, giorno: s.giorno, riscattato: s.riscattato, secondi: s.secondi }, deliver(null, null));
    }, function () {
        var s = dailyState(now);
        return { ok: true, postaNuova: 0, giorno: s.giorno, riscattato: s.riscattato, secondi: s.secondi, occupato: true };
    })(args);
});

/**
 * B32 (TU3, scelte 07/10): premio di fine tutorial, una volta per account (chiave interna "Tutorial"). Ospiti niente: il telefono dice
 * "registrati per riscattarla" e la riscattano rifacendo il tutorial con l'account. Che il tutorial sia finito lo dice il telefono
 * (come il premio giornaliero); una volta sola per account lo garantisce il server.
 */
handlers.premioTutorial = phoneOnly(locked(function (args) {
    if (isGuest()) return { ok: false, ospite: true };
    var eco = economy(), prize = resolve(eco.tutorial, eco), mark = { Tutorial: italianDate(Date.now()) };
    if (readOnly(["Tutorial"]).Tutorial) return paid({ ok: false, gia: true }, deliver(null, null));
    // Il segno degli script fino all'08/10 (dati interni) poteva arrivare senza monete (accredito fallito dopo il segno), ma il saldo non
    // prova niente (scelta dell'utente 09/10): mai pagato da solo. Si considera dato e resta una consegna "incerto" da riconciliare.
    var legacy = internal(["Tutorial"]).Tutorial;
    if (legacy) {
        var e = entryOf("tutorial", prize);
        e.stato = "incerto"; e.dubbio = owed(e); e.errore = "segno del tutorial degli script fino all'08/10 (" + legacy + "): pagato?";
        if (pending(e)) mark[CONS + e.id] = JSON.stringify(e);
        return paid({ ok: false, gia: true }, deliver(null, mark));
    }
    // Segno e consegna insieme (Read Only, deliver): consumato solo con la consegna nel registro.
    return paid({ ok: true }, deliver(entryOf("tutorial", prize), mark), prize);
}));

/** Ospite = account senza nome utente (come premioPartita e premioTutorial). Build 3 (26c): niente premio giornaliero ne' Posta. */
function isGuest() {
    var me = server.GetUserAccountInfo({ PlayFabId: currentPlayerId });
    return !(me && me.UserInfo && me.UserInfo.Username);
}

handlers.statoPremi = phoneOnly(function (args) {
    var s = dailyState(Date.now());
    return { ok: true, giorno: s.giorno, riscattato: s.riscattato, secondi: s.secondi };
});

handlers.riscattaPremio = phoneOnly(locked(function (args) {
    if (isGuest()) return { ok: false, ospite: true };
    var now = Date.now();
    var s = dailyState(now);
    if (s.riscattato) return paid({ ok: false, gia: true, giorno: s.giorno, riscattato: true, secondi: s.secondi }, deliver(null, null));
    // Terzo giro 08/10: il premio non torna piu' riscattabile se l'accredito fallisce (sembrava riscattabile all'infinito): resta
    // consumato con la consegna nel registro, che arriva al tentativo dopo (inConsegna).
    var eco = economy(), prize = resolve(eco.settimana[s.giorno - 1] || [], eco);
    var got = deliver(entryOf("premio" + italianDate(now), prize), { Premi: JSON.stringify({ giorno: s.giorno, ultimo: italianDate(now) }) });
    return paid({ ok: true, giorno: s.giorno, riscattato: true, secondi: secondsToItalianMidnight(now) }, got, prize);
}));

handlers.riscattaPosta = phoneOnly(locked(function (args) {
    var id = args && args.id;
    if (!id) return { ok: false, errore: "id mancante" };
    return claimMail([String(id)], Date.now());
}));

handlers.riscattaTuttaPosta = phoneOnly(locked(function (args) {
    return claimMail(null, Date.now());
}));

/** XP di fine partita, come PlayerXp.MatchAward: 40 vittoria / 20 sconfitta, +2 per scopa e +5 per accuso (bonus max +20), meta' coi bot. */
function matchXp(won, scope, accusi, training) {
    var xp = (won ? 40 : 20) + Math.min(20, 2 * Math.max(0, scope | 0) + 5 * Math.max(0, accusi | 0));
    return training ? Math.floor(xp / 2) : xp;
}

/** Livello dagli XP, come PlayerXp.LevelOf: dal livello L al L+1 servono 100 + 20*(L-1) XP, massimo 100. */
function levelOf(xp) {
    var level = 1;
    while (level < 100 && xp >= 100 * level + 10 * level * (level - 1)) level++;
    return level;
}

// XPSettimana: XP della settimana per la Classifica (UI51 Fase 15); l'azzeramento settimanale si imposta nel Game Manager
// (Leaderboards, Reset frequency: Weekly). Dopo l'azzeramento PlayFab la restituisce vuota: si riparte da 0.
// XPConcordato (D6, 08/10): XP delle sole partite confermate fra persone, senza bot all'inizio e senza nessuno uscito (quindi senza
// bot sostitutivi). "Concordato" = i telefoni hanno dichiarato lo stesso risultato: NON e' una validazione autorevole (due account
// d'accordo lo ottengono e ogni telefono conosce le carte, #151). Non basta per classifiche con premi: servono i risultati del server
// di partita (docs/build3/PIANO_RISULTATI_AUTOREVOLI.md, sezione 6).
var STATS = ["TotalGames", "Wins", "XP", "Level", "TotalScope", "XPSettimana", "XPConcordato"];
var MAX_SCOPE = 30; // scope contate per partita: oltre e' un numero inventato

/**
 * Partite premiabili (#141, #142 Fase A, #143, #148-#150; decisioni dell'utente D1-D7 dell'08/10).
 *
 * Biglietto: ogni partita ha un id che genera il server (inizioPartita), mai il telefono. Read Only "Partite" = {aperte, riesame,
 * chiuse, ultimo}:
 *   aperte  = biglietti senza risultato [{id, t, k, stanza, rec, n, umani}]: k = "s:<stanza>:<id record>" online, "a:<id del telefono>"
 *             in allenamento (uno per partita: mai lo stesso biglietto per due partite); n = numero Photon del giocatore;
 *   riesame = risultati dichiarati ma non ancora pagati per intero: "inVerifica" (si aspettano le altre persone), "incompleta" (attesa
 *             finita: pagata solo la partecipazione), "contestata" (dichiarazioni diverse: solo la partecipazione, decide l'amministratore);
 *   chiuse  = ultimi MAX_CLOSED esiti finali {id, s, m, x, f} (s = stato, m = monete, x = XP, f = ora); i piu' vecchi passano nei dati
 *             interni "ArchivioPartite" (ultimi MAX_ARCHIVE). Le monete hanno anche la loro consegna (Cons_, stati attesa/incerto/fermo);
 *   ultimo  = ora dell'ultimo biglietto nuovo: al massimo uno al minuto (niente biglietti in parallelo da incassare insieme).
 * Nessuna scadenza a tempo (D1). Oltre MAX_OPEN biglietti aperti il piu' vecchio diventa "scaduta" (era una partita mai finita: app
 * chiusa a meta'); se il suo risultato arriva dopo, resta registrato da riconciliare, mai pagato da solo ne' perso in silenzio.
 *
 * Persone presenti (#143, D5): non basta essere entrati nella stanza. Conta chi ha chiesto il biglietto di QUELLA partita mentre sedeva
 * nel record (b<numero> nel record, scritto qui dal server) e chi esce per sempre lo dice Photon (u<numero>, webhook di uscita). Il
 * telefono chiede il biglietto alla prima distribuzione: un account entrato e uscito prima dell'inizio non ha il biglietto e non conta.
 * Nessuna soglia di tempo. Un secondo account dello stesso giocatore che chiede il biglietto e poi esce conta come un abbandono (tetti
 * degli abbandoni); se resta e dichiara e' un accordo fra account, che nessuna prova del server distingue (tetti e registro).
 *
 * Fase A (D3, D4): ogni persona dichiara il risultato (punteggi di tutti, smazzate, vincitore, posto) e il server lo scrive nel record
 * (d<numero>). Pagata per intero solo quando tutte le persone rimaste hanno dichiarato lo stesso risultato ("confermata"), oppure quando
 * le altre sono uscite per sempre senza dichiarare ("abbandono": tetti delle vittorie per abbandono). Altrimenti niente vincitore:
 * "inVerifica" finche' dura l'attesa, poi "incompleta"; dichiarazioni diverse = "contestata". Incompleta e contestata pagano solo la
 * partecipazione (le prove del server dicono che ha giocato, non chi ha vinto), niente XP, statistiche ne' classifica; il telefono
 * ritenta le incomplete per RIESAME_MS e diventano confermate se arrivano le dichiarazioni mancanti; le contestate le decide solo
 * l'amministratore (Server/QA/qa.js). Ogni incongruenza va nel registro "Incongruenze" e in PlayStream, con una categoria indicativa
 * (bug, rete, sospetta). Nessuna sanzione automatica.
 *
 * Senza biglietto (#149, #150, D2): il telefono nuovo che non l'ha avuto per la rete manda "senzaBiglietto" (id suo, contro i doppioni):
 * meta' premio come l'allenamento. Le app vecchie (revisione 19) non mandano niente: ramo di compatibilita' acceso finche'
 * Economia.partita.senzaBiglietto.attiva non diventa false (lo decide l'utente). Entrambi: uno al minuto, al massimo
 * senzaBiglietto.giorno al giorno, evento PlayStream "partita_senza_biglietto" per il controllo.
 *
 * Premi non verificabili (D11, 08/10): allenamento (e ogni partita senza prova di altre persone), app vecchie senza biglietto, risultati
 * senza biglietto del telefono nuovo e partecipazione alle partite non confermate hanno UN tetto comune di monete al giorno,
 * Economia.partita.tettoNonVerificabili (100), dentro il tetto generale. Le partite confermate non lo toccano: la partecipazione gia'
 * data si scala dal premio pieno, quella tagliata dal tetto no. Monete per tipo in PartiteOggi.nonVerificabili. I contatori del giorno
 * si scrivono nella stessa chiamata della consegna (deliver): uno script fermato a meta' non lascia monete fuori dai contatori, e il
 * lucchetto del giocatore mette in fila le richieste insieme (prova in concorrenza.js).
 */
var MATCHES = "Partite", ARCHIVE = "ArchivioPartite", ODD = "Incongruenze", NOTICKET = "SenzaBiglietto";
var MIN_MATCH_MS = 60 * 1000, RIESAME_MS = 7 * 24 * 60 * 60 * 1000;
var MAX_OPEN = 10, MAX_REVIEW = 10, MAX_CLOSED = 30, MAX_ARCHIVE = 150, MAX_ODD = 50, MAX_LOCAL_IDS = 30;
var DAY = "PartiteOggi", RESULTS = "RisultatiOggi";
var PARTITA_DEFAULTS = { partecipazione: 10, attesaMinuti: 10, senzaBiglietto: { attiva: true, giorno: 20 }, tettoNonVerificabili: 100 };

function arr(v) { return Array.isArray(v) ? v : []; }

function tickets(raw) {
    var t = parseObject(raw) || {};
    return { aperte: arr(t.aperte), riesame: arr(t.riesame), chiuse: arr(t.chiuse), ultimo: +t.ultimo || 0 };
}

function findIn(list, id) {
    for (var i = 0; i < list.length; i++) if (list[i] && list[i].id === id) return list[i];
    return null;
}

function without(list, id) { return list.filter(function (x) { return x && x.id !== id; }); }

/** Account diversi tra i posti ({numero: PlayFabId}). */
function humans(g) {
    var seen = {}, n = 0;
    for (var a in g) if (g.hasOwnProperty(a) && g[a] && !seen["k" + g[a]]) { seen["k" + g[a]] = true; n++; }
    return n;
}

/** Posti delle persone presenti all'inizio: chi ha chiesto il biglietto di questa partita mentre sedeva. */
function participants(match) {
    var p = {};
    for (var a in match.biglietti) if (match.biglietti.hasOwnProperty(a) && match.giocatori[a]) p[a] = match.giocatori[a];
    return p;
}

/** Valore di Economia.partita, con i valori di prima per le chiavi che il Title Data non ha. */
function partitaValue(p, k) { return p && p[k] != null ? p[k] : DEFAULTS.partita[k] != null ? DEFAULTS.partita[k] : PARTITA_DEFAULTS[k]; }

/** Dichiarazione del telefono (Fase A): punti finali di ogni posto (o squadra), smazzate, vincitore, posto, squadre, versione, rientri. */
function declaration(args, now) {
    function num(v) { return Math.max(0, Math.min(999, v | 0)); }
    return { t: now, v: !!args.vinta, p: arr(args.punti).slice(0, 4).map(num), s: num(args.smazzate), w: args.vincitore | 0, n: args.posto | 0,
        q: !!args.squadre, g: num(args.giocatori), a: String(args.versione || "").substring(0, 12), r: num(args.rientri) };
}

function sameResult(a, b) { return JSON.stringify([a.p, a.s, a.w, a.q, a.g]) === JSON.stringify([b.p, b.s, b.w, b.q, b.g]); }

/** La vittoria dichiarata torna col vincitore dichiarato e il proprio posto (in 2v2 le squadre sono posti pari e dispari). */
function coherent(d) { return d.p.length > 0 && d.v === ((d.q ? d.n % 2 : d.n) === d.w); }

/**
 * Esito di una partita online dalle prove del server (record della stanza). Per account: dichiarazione (d), uscita per sempre (u), o
 * nessuna delle due (assente). since = ora della propria dichiarazione, per l'attesa.
 * Ritorna {stato, cat, competitiva (fra sole persone, nessuno uscito: conta per XPConcordato), uscite: [account usciti senza dichiarare]}.
 */
function verdict(match, since, now, waitMs) {
    var part = participants(match), acc = {}, a, id;
    for (a in part) {
        if (!part.hasOwnProperty(a)) continue;
        var e = acc[part[a]] = acc[part[a]] || { d: null, u: false };
        if (match.dichiarazioni[a]) e.d = match.dichiarazioni[a];
        if (match.uscite[a]) e.u = true;
    }
    var ds = [], gone = [], absent = 0, total = 0;
    for (id in acc) {
        if (!acc.hasOwnProperty(id)) continue;
        total++;
        if (acc[id].d && !acc[id].d.x) ds.push(acc[id].d);
        else if (acc[id].d || acc[id].u) gone.push(id); // uscita dichiarata o uscita per sempre senza risultato
        else absent++;
    }
    var out = { stato: "", cat: "", competitiva: false, uscite: gone };
    var seats = {}, agree = true;
    for (var i = 0; i < ds.length; i++) {
        if (!coherent(ds[i]) || seats["n" + ds[i].n] || !sameResult(ds[i], ds[0])) agree = false;
        seats["n" + ds[i].n] = true;
    }
    if (!agree) {
        out.stato = "contestata";
        var sameAll = ds.every(function (d) { return sameResult(d, ds[0]); });
        var versions = ds.some(function (d) { return d.a !== ds[0].a; }), rejoined = ds.some(function (d) { return d.r > 0; });
        out.cat = sameAll ? "sospetta" : versions ? "bug" : rejoined ? "rete" : "sospetta";
    } else if (absent > 0) {
        out.stato = now - since < waitMs ? "inVerifica" : "incompleta";
        out.cat = "rete";
    } else if (ds.length < 2) {
        out.stato = "abbandono"; // le altre persone sono tutte uscite senza risultato
    } else {
        out.stato = "confermata";
        out.competitiva = gone.length === 0 && total === ds[0].g;
    }
    return out;
}

/** Contatori del giorno (dati Read Only) e configurazione delle partite. nv = monete non verificabili date oggi, per tipo. */
function dayCounters(data, now) {
    var eco = economy(), p = eco.partita, today = italianDate(now);
    var s = parseObject(data[DAY]), n = parseObject(data[RESULTS]);
    var c = { p: p, today: today, given: 0, forfeits: [], nv: {}, played: n && n.data === today ? n.partite | 0 : 0 };
    if (s && s.data === today) {
        c.given = s.monete | 0;
        if (s.abbandoni instanceof Array) c.forfeits = s.abbandoni;
        if (s.nonVerificabili) c.nv = s.nonVerificabili;
    }
    c.maxGames = partitaValue(p, "partiteGiorno") | 0;
    return c;
}

/** Monete davvero date: dentro il tetto del giorno e, se il premio non e' verificabile (r.nv = tipo), dentro il tetto comune D11. */
function coinsFor(c, r) {
    var amount = Math.max(0, Math.min(r.coins | 0, (c.p.tetto | 0) - c.given));
    if (!r.nv) return amount;
    var nv = 0;
    for (var k in c.nv) if (c.nv.hasOwnProperty(k)) nv += c.nv[k] | 0;
    return Math.min(amount, Math.max(0, (partitaValue(c.p, "tettoNonVerificabili") | 0) - nv));
}

/** Vittoria per abbandono: monete e XP al massimo "abbandoni" volte al giorno e una per avversario (lista dei giorno in c.forfeits). */
function forfeitAllowed(c, who) {
    who = String(who).substring(0, 32);
    var max = partitaValue(c.p, "abbandoni") | 0;
    if (c.forfeits.length >= max || c.forfeits.indexOf(who) >= 0) return false;
    c.forfeits.push(who);
    return true;
}

/**
 * Paga e registra. r = {coins, xp, won, scope, stats, count, agreed, nv}; consume = dati Read Only scritti insieme alla consegna (anche i
 * contatori del giorno); internals = dati interni da scrivere dopo. Monete dentro i tetti del giorno (coinsFor).
 */
function settle(c, r, consume, internals, reply) {
    var amount = coinsFor(c, r);
    if (r.nv) c.nv[r.nv] = (c.nv[r.nv] | 0) + amount;
    if (r.closed) r.closed.m = (r.closed.m | 0) + amount;
    consume = consume || {};
    if (r.closed) consume[MATCHES] = JSON.stringify(r.tickets); // l'esito chiuso con le monete vere
    consume[DAY] = JSON.stringify({ data: c.today, monete: c.given + amount, abbandoni: c.forfeits, nonVerificabili: c.nv });
    consume[RESULTS] = JSON.stringify({ data: c.today, partite: c.played + (r.count ? 1 : 0) });
    var got = deliver(amount > 0 ? entryOf(newId("g"), { monete: amount, gemme: 0 }) : null, consume, 0);
    if (internals && Object.keys(internals).length) saveInternal(internals);
    reply.ok = true; reply.monete = got.monete; reply.inConsegna = got.inConsegna; reply.saldoMonete = got.saldoMonete;
    reply.saldoGemme = got.saldoGemme; reply.recuperoMonete = got.recuperoMonete; reply.recuperoGemme = got.recuperoGemme;
    reply.xp = r.xp | 0;
    reply.tetto = (r.coins | 0) > 0 && amount === 0; // tetto generale o dei non verificabili: il telefono dice "tetto raggiunto"
    if (r.nv && amount < Math.min(r.coins | 0, (c.p.tetto | 0) - c.given)) reply.tettoNonVerificabili = true;
    if (!r.stats) return reply;
    var st = {}, stats = server.GetPlayerStatistics({ PlayFabId: currentPlayerId, StatisticNames: STATS });
    var list = stats && stats.Statistics || [];
    for (var i = 0; i < list.length; i++) st[list[i].StatisticName] = list[i].Value | 0;
    st.TotalGames = (st.TotalGames | 0) + 1;
    st.Wins = (st.Wins | 0) + (r.won ? 1 : 0);
    st.XP = (st.XP | 0) + (r.xp | 0);
    st.TotalScope = (st.TotalScope | 0) + Math.min(MAX_SCOPE, Math.max(0, r.scope | 0));
    st.Level = levelOf(st.XP);
    st.XPSettimana = (st.XPSettimana | 0) + (r.xp | 0);
    st.XPConcordato = (st.XPConcordato | 0) + (r.agreed ? r.xp | 0 : 0);
    var upd = [];
    for (var k = 0; k < STATS.length; k++) upd.push({ StatisticName: STATS[k], Value: st[STATS[k]] });
    server.UpdatePlayerStatistics({ PlayFabId: currentPlayerId, Statistics: upd });
    reply.statistiche = true; reply.partite = st.TotalGames; reply.vittorie = st.Wins; reply.esperienza = st.XP;
    reply.scopeTotali = st.TotalScope; reply.livello = st.Level;
    return reply;
}

/** Evento PlayStream per il controllo (Game Manager -> PlayStream o Data Explorer); un errore non ferma il premio. */
function monitor(name, body) {
    try { server.WritePlayerEvent({ PlayFabId: currentPlayerId, EventName: name, Body: body }); } catch (ex) { log.error(name + ": " + apiError(ex)); }
}

/** Chiuse oltre MAX_CLOSED nell'archivio interno (ultimi MAX_ARCHIVE). Ritorna l'archivio da scrivere, o null se non cambia. */
function archiveOverflow(t, rawArchive) {
    if (t.chiuse.length <= MAX_CLOSED) return null;
    var archive = parseList(rawArchive).concat(t.chiuse.slice(0, t.chiuse.length - MAX_CLOSED));
    t.chiuse = t.chiuse.slice(-MAX_CLOSED);
    return JSON.stringify(archive.slice(-MAX_ARCHIVE));
}

/** Riga del registro delle incongruenze (dati interni, ultimi MAX_ODD) + evento PlayStream. */
function oddEntry(raw, ticket, v, match, now) {
    var ds = [];
    for (var a in match.dichiarazioni) if (match.dichiarazioni.hasOwnProperty(a)) {
        var d = match.dichiarazioni[a];
        ds.push({ n: a, id: match.giocatori[a], x: !!d.x, v: d.v, p: d.p, s: d.s, w: d.w, a: d.a, r: d.r });
    }
    var e = { t: now, partita: ticket.id, rec: ticket.rec, stanza: ticket.stanza, stato: v.stato, cat: v.cat, uscite: v.uscite, dichiarazioni: ds };
    monitor("partita_incongruenza", { partita: ticket.id, rec: ticket.rec, stato: v.stato, cat: v.cat });
    return JSON.stringify(parseList(raw).concat([e]).slice(-MAX_ODD));
}

/** Inizio partita, anche dopo un rientro: {stanza} online, {locale} = id della partita sul telefono in allenamento; dopo = biglietti delle
 * partite gia' finite sul telefono (anche quelle in sospeso): mai restituiti per una partita nuova. */
handlers.inizioPartita = phoneOnly(locked(function (args) {
    args = args || {};
    var now = Date.now(), room = String(args.stanza || ""), key = "", seat = 0, match = null, a;
    var done = [].concat(args.dopo || []).map(String);
    if (room) {
        match = readMatch(room);
        if (match) for (a in match.giocatori)
            if (match.giocatori.hasOwnProperty(a) && match.giocatori[a] === currentPlayerId && !match.uscite[a] && !seat) seat = Number(a);
        // Stanza senza record o senza di lui: si gioca lo stesso, ma vale come allenamento (nessuna prova di altre persone).
        if (seat) key = "s:" + room + ":" + match.id;
    }
    if (!key) key = "a:" + String(args.locale || "").substring(0, 32);
    var t = tickets(readOnly([MATCHES])[MATCHES]), cur = null, i;
    for (i = t.aperte.length - 1; i >= 0 && !cur; i--) if (t.aperte[i].k === key && done.indexOf(t.aperte[i].id) < 0) cur = t.aperte[i];
    if (!cur) {
        if (now - t.ultimo < MIN_MATCH_MS) return { ok: false, attendi: Math.ceil((MIN_MATCH_MS - (now - t.ultimo)) / 1000) };
        cur = { id: newId("p"), t: now, k: key };
        if (seat) { cur.stanza = room; cur.rec = match.id; cur.n = seat; }
        t.aperte.push(cur);
        t.ultimo = now;
        // Oltre il limite: la piu' vecchia senza risultato (partita mai finita) diventa "scaduta", registrata, mai cancellata.
        while (t.aperte.length > MAX_OPEN) { var old = t.aperte.shift(); t.chiuse.push({ id: old.id, s: "scaduta", m: 0, x: 0, f: now }); }
    }
    if (seat && !match.biglietti[seat]) {
        // Prova di presenza: ha chiesto il biglietto mentre sedeva (un errore qui toglie solo la prova: la partita vale meno, mai di piu').
        try { var b = {}; b["b" + seat] = String(now); server.UpdateSharedGroupData({ SharedGroupId: matchGroup(room), Data: b }); match.biglietti[seat] = now; }
        catch (ex) { log.error("inizioPartita, presenza: " + apiError(ex)); }
    }
    cur.umani = seat && cur.rec === match.id ? humans(participants(match)) : 0;
    saveReadOnly((function (d) { d[MATCHES] = JSON.stringify(t); return d; })({}));
    return { ok: true, partita: cur.id, online: cur.umani >= 2 };
}, function () { return { ok: false, occupato: true }; }));

/**
 * Fine partita (scelte dell'utente 01/10, 02/10, 08/10). Monete: 40 vittoria, 20 sconfitta; meta' (anche gli XP) senza altre persone
 * al tavolo; tetto giornaliero; "partiteGiorno" risultati al giorno. Il telefono manda il biglietto e la dichiarazione (vinta, scope,
 * accusi, punti, smazzate, vincitore, posto, squadre, giocatori, versione, rientri); chi ha vinto non lo decide lui (vedi sopra).
 * Risposta: stato = "confermata", "abbandono", "allenamento", "inVerifica" (non definitivo: il telefono ritenta), "incompleta" (ritenta
 * per il riesame), "contestata", "uscita", "sospeso", "limite", "scaduta"/"daRiconciliare", "senzaBiglietto", "nonValida". Lo stesso
 * biglietto chiuso risponde "gia" con quanto aveva dato. Uscita a meta' ({"uscita":true}, anche senza biglietto): partita persa, senza XP
 * ne' monete. Ospiti: niente. Tutto sotto il lucchetto del giocatore.
 */
handlers.premioPartita = phoneOnly(locked(function (args) {
    args = args || {};
    if (isGuest()) return { ok: true, monete: 0, ospite: true, stato: "ospite" };
    var now = Date.now(), quit = !!args.uscita, id = String(args.partita || "");
    var ro = readOnly([MATCHES, DAY, RESULTS, NOTICKET]), t = tickets(ro[MATCHES]);
    var data = internal([SOSP, ARCHIVE, ODD]);
    var done = id ? findIn(t.chiuse, id) || findIn(parseList(data[ARCHIVE]), id) : null;
    if (done && done.s === "scaduta" && !quit) {
        // Risultato di una partita tolta per il limite dei biglietti: si registra per la riconciliazione, mai pagato da solo.
        var late = {}; late[ODD] = JSON.stringify(parseList(data[ODD]).concat([{ t: now, partita: id, stato: "daRiconciliare", cat: "limite",
            dichiarazioni: [declaration(args, now)] }]).slice(-MAX_ODD));
        done.s = "daRiconciliare";
        saveInternal(late);
        saveReadOnly((function (d) { d[MATCHES] = JSON.stringify(t); return d; })({}));
        return balance({ ok: true, monete: 0, stato: "daRiconciliare", saldoMonete: -1, saldoGemme: -1 });
    }
    if (done) return balance({ ok: true, gia: true, stato: done.s, monete: done.m | 0, xp: done.x | 0, saldoMonete: -1, saldoGemme: -1 });
    var ticket = id ? findIn(t.aperte, id) || findIn(t.riesame, id) : null;
    if (id && !ticket) return { ok: false, partitaNonValida: true, stato: "nonValida", errore: "partita senza biglietto del server" };
    var c = dayCounters(ro, now);
    if (!ticket && !quit) return noTicket(args, ro, c, now);
    if (ticket && !quit && now - ticket.t < MIN_MATCH_MS) return { ok: false, partitaNonValida: true, stato: "troppoCorta", errore: "partita troppo corta" };

    var match = ticket && ticket.rec ? readMatch(ticket.stanza) : null;
    if (match && match.id !== ticket.rec) match = null;
    if (match) ticket.umani = humans(participants(match));
    var mine = !!(match && match.biglietti[ticket.n]);
    var online = !!ticket && (match ? mine && ticket.umani >= 2 : ticket.umani >= 2);
    var internals = {}, reply = {}, r = { coins: 0, xp: 0, stats: false, count: false }, a, prev = ticket ? ticket.stato || "" : "";

    function close(stato) {
        t.aperte = without(t.aperte, ticket.id);
        t.riesame = without(t.riesame, ticket.id);
        r.closed = { id: ticket.id, s: stato, m: ticket.m | 0, x: (ticket.x | 0) + (r.xp | 0), f: now };
        t.chiuse.push(r.closed);
    }
    function review(stato) {
        t.aperte = without(t.aperte, ticket.id);
        ticket.stato = stato;
        if (!findIn(t.riesame, ticket.id)) t.riesame.push(ticket);
    }

    if (quit) {
        // Uscita volontaria: persa, senza XP ne' monete. Online resta nel record (non e' un abbandono non dichiarato).
        if (match && mine && !match.dichiarazioni[ticket.n]) {
            try { var q = {}; q["d" + ticket.n] = JSON.stringify({ t: now, x: true }); server.UpdateSharedGroupData({ SharedGroupId: matchGroup(ticket.stanza), Data: q }); }
            catch (ex) { log.error("uscita: " + apiError(ex)); }
        }
        r.stats = true; r.count = true;
        if (ticket) close("uscita");
        reply.stato = "uscita";
    } else if (online && activeSuspension(parseObject(data[SOSP]), now)) {
        close("sospeso");
        reply.sospeso = true; reply.stato = "sospeso";
    } else if (!online) {
        // Allenamento, partita rapida coi soli bot, o nessuna prova di altre persone: meta', come l'allenamento.
        var won = !!args.vinta;
        if (c.played >= c.maxGames) { close("limite"); reply.limitePartite = true; reply.stato = "limite"; }
        else {
            r = { coins: Math.floor((won ? c.p.vittoria | 0 : c.p.sconfitta | 0) / 2), xp: matchXp(won, args.scope, args.accusi, true), won: won,
                scope: args.scope, stats: true, count: true, nv: "allenamento" };
            close("allenamento");
            reply.allenamento = true; reply.stato = "allenamento";
        }
    } else {
        // Online con altre persone: dichiarazione scritta una volta sola (un tentativo dopo non la cambia).
        var d = ticket.d;
        if (!d) {
            d = ticket.d = declaration(args, now);
            d.scope = Math.min(MAX_SCOPE, Math.max(0, args.scope | 0)); d.accusi = Math.max(0, args.accusi | 0);
            if (match) {
                var w = {}; w["d" + ticket.n] = JSON.stringify(d);
                server.UpdateSharedGroupData({ SharedGroupId: matchGroup(ticket.stanza), Data: w });
                match.dichiarazioni[ticket.n] = d;
            }
        }
        var wait = (partitaValue(c.p, "attesaMinuti") | 0) * 60 * 1000;
        var v = match ? verdict(match, d.t, now, wait) : { stato: "incompleta", cat: "rete", uscite: [] }; // record non piu' disponibile
        var full = d.v ? c.p.vittoria | 0 : c.p.sconfitta | 0, paidBefore = ticket.m | 0, counted = !!ticket.contata;
        if (!counted && c.played >= c.maxGames && v.stato !== "inVerifica") {
            close("limite"); reply.limitePartite = true; reply.stato = "limite";
        } else if (v.stato === "confermata" || v.stato === "abbandono") {
            var allowed = true;
            // Vittoria con le altre persone uscite: tetti degli abbandoni (una sconfitta resta una sconfitta normale).
            if (v.stato === "abbandono" && d.v) for (a = 0; a < v.uscite.length; a++) if (!forfeitAllowed(c, v.uscite[a])) allowed = false;
            r = { coins: allowed ? Math.max(0, full - paidBefore) : 0, xp: allowed ? matchXp(d.v, d.scope, d.accusi, false) : 0, won: d.v,
                scope: d.scope, stats: true, count: !counted, agreed: v.competitiva };
            close(v.stato);
            reply.stato = v.stato; reply.limiteAbbandoni = !allowed; reply.concordata = v.competitiva;
        } else if (v.stato === "inVerifica") {
            review("inVerifica");
            reply.stato = "inVerifica";
        } else {
            // Incompleta o contestata: solo la partecipazione, una volta; il risultato competitivo resta non verificato.
            if (!paidBefore) r = { coins: partitaValue(c.p, "partecipazione") | 0, xp: 0, stats: false, count: !counted, nv: "partecipazione" };
            ticket.contata = true;
            if (prev !== v.stato && match) internals[ODD] = oddEntry(data[ODD], ticket, v, match, now);
            if (prev === "incompleta" && v.stato === "incompleta" && now - ticket.t >= RIESAME_MS) close("incompleta");
            else review(v.stato);
            reply.stato = v.stato; reply.categoria = v.cat;
        }
    }
    // La partecipazione gia' data resta nel biglietto (ticket.m), per pagare solo la differenza se poi si conferma.
    if (!r.closed && ticket) ticket.m = (ticket.m | 0) + coinsFor(c, r);
    // Riesame scaduto (RIESAME_MS) o oltre il limite: chiuso con quanto aveva gia' avuto (l'amministratore lo puo' riconciliare).
    t.riesame = t.riesame.filter(function (x) {
        if (x !== ticket && now - x.t >= RIESAME_MS) { t.chiuse.push({ id: x.id, s: x.stato, m: x.m | 0, x: 0, f: now }); return false; }
        return true;
    });
    while (t.riesame.length > MAX_REVIEW) { var o = t.riesame.shift(); t.chiuse.push({ id: o.id, s: o.stato, m: o.m | 0, x: 0, f: now }); }
    var archived = archiveOverflow(t, data[ARCHIVE]);
    if (archived) internals[ARCHIVE] = archived;
    r.tickets = t;
    var consume = {}; consume[MATCHES] = JSON.stringify(t);
    return settle(c, r, consume, internals, reply);
}, function () { return { ok: false, occupato: true }; }));

/**
 * Risultato senza biglietto: il telefono nuovo che non l'ha avuto per la rete ("senzaBiglietto": id suo) o un'app vecchia (revisione
 * 19: niente id, finche' la compatibilita' e' accesa). Uno al minuto, "giorno" al giorno, meta' premio; solo l'app vecchia puo' avere
 * il premio pieno, se il record della stanza mostra un altro account ancora seduto (o uscito, per la vittoria per abbandono, con i suoi
 * tetti), come faceva la revisione 19 ma col record e non col campo "allenamento" del telefono.
 */
function noTicket(args, data, c, now) {
    var cfg = partitaValue(c.p, "senzaBiglietto") || {}, legacy = !args.senzaBiglietto, local = legacy ? "" : String(args.senzaBiglietto).substring(0, 32);
    var nt = parseObject(data[NOTICKET]) || {}, ids = arr(nt.ids);
    if (local && ids.indexOf(local) >= 0) return balance({ ok: true, gia: true, stato: "senzaBiglietto", monete: 0, saldoMonete: -1, saldoGemme: -1 });
    if (legacy && cfg.attiva === false)
        return { ok: false, partitaNonValida: true, stato: "nonValida", errore: "app da aggiornare: partita senza biglietto del server" };
    if (nt.data !== c.today) { nt.data = c.today; nt.n = 0; }
    if (now - (+nt.ultimo || 0) < MIN_MATCH_MS)
        return { ok: false, attendi: Math.ceil((MIN_MATCH_MS - (now - (+nt.ultimo || 0))) / 1000), stato: "attendi", errore: "una partita senza biglietto al minuto" };
    var won = !!args.vinta, consume = {}, reply = { stato: "senzaBiglietto", allenamento: true };
    var r = { coins: Math.floor((won ? c.p.vittoria | 0 : c.p.sconfitta | 0) / 2), xp: matchXp(won, args.scope, args.accusi, true), won: won,
        scope: args.scope, stats: true, count: true, nv: legacy ? "appVecchia" : "senzaBiglietto" };
    var full = false;
    if (legacy) {
        var match = readMatch(String(args.stanza || "")), me = false, other = null, a;
        if (match) for (a in match.giocatori) if (match.giocatori.hasOwnProperty(a)) {
            if (match.giocatori[a] === currentPlayerId) me = true;
            else if (!match.uscite[a]) other = other || match.giocatori[a];
        }
        if (me && won && args.abbandono) {
            var left = match.giocatori[Number(args.attore) || 0];
            if (left && left !== currentPlayerId && match.uscite[Number(args.attore) || 0]) {
                full = forfeitAllowed(c, left);
                if (!full) { r.coins = 0; r.xp = 0; reply.limiteAbbandoni = true; }
            } else reply.abbandonoNonValido = true;
        } else if (me && other) full = true;
        if (full) { r.coins = won ? c.p.vittoria | 0 : c.p.sconfitta | 0; r.xp = matchXp(won, args.scope, args.accusi, false); reply.allenamento = false; }
    }
    if (c.played >= c.maxGames) return { ok: true, monete: 0, limitePartite: true, stato: "limite" };
    if ((nt.n | 0) >= (cfg.giorno != null ? cfg.giorno | 0 : 20)) return { ok: true, monete: 0, stato: "limiteSenzaBiglietto", limitePartite: true };
    nt.n = (nt.n | 0) + 1; nt.ultimo = now;
    if (local) nt.ids = ids.concat([local]).slice(-MAX_LOCAL_IDS);
    consume[NOTICKET] = JSON.stringify(nt); // con la consegna: un tentativo dopo uno script fermato non paga due volte lo stesso id
    monitor("partita_senza_biglietto", { tipo: legacy ? "appVecchia" : "rete", pieno: full, versione: String(args.versione || "") });
    return settle(c, r, consume, null, reply);
}

/**
 * "Segnala giocatore" dal profilo rapido, con il motivo: "emoticon" spegne le emoticon, "gioco" e "nome" sospendono il gioco online.
 * Per ogni motivo conta un account diverso per volta e servono segnalazioni da almeno "partite" partite diverse, cosi' un gruppo di
 * amici nella stessa partita non basta. Solo chi ha un account puo' segnalare, e solo chi sedeva nella stessa partita (record del
 * server, "stanza" = codice della stanza): niente segnalazioni da fuori o con partite inventate. Altrimenti non conta, senza dirlo.
 */
handlers.segnala = phoneOnly(function (args) {
    args = args || {};
    var id = String(args.id || ""), motivo = String(args.motivo || "gioco");
    if (!id || id === currentPlayerId) return { ok: false, errore: "giocatore non valido" };
    if (!REPORT_REASONS[motivo]) return { ok: false, errore: "motivo non valido" };
    var me = server.GetUserAccountInfo({ PlayFabId: currentPlayerId });
    if (!(me && me.UserInfo && me.UserInfo.Username)) return { ok: false, errore: "serve un account" };
    var match = readMatch(String(args.stanza || "")), seated = {};
    if (match) for (var g in match.giocatori) seated["p" + match.giocatori[g]] = true;
    if (!match || !seated["p" + currentPlayerId] || !seated["p" + id]) return { ok: true };
    var now = Date.now(), mod = moderation();
    var mute = motivo === "emoticon", kind = mute ? MUTE : SOSP;
    // ponytail: leggi-modifica-scrivi senza lock, due segnalazioni nello stesso istante possono perderne una (mai una sanzione doppia).
    var data = internal([REPORTS, kind], id);
    var current = parseObject(data[kind]) || {};
    var list = recent(parseList(data[REPORTS]), now, mod.giorni), others = [], same = [];
    for (var i = 0; i < list.length; i++) {
        var e = list[i], sameKind = (e.motivo === "emoticon") === mute;
        if (sameKind && e.da === currentPlayerId) continue; // la sua segnalazione precedente per lo stesso tipo viene sostituita
        (sameKind ? same : others).push(e);
    }
    same.push({ da: currentPlayerId, t: now, motivo: motivo, modo: String(args.modo || "").substring(0, 12),
        stanza: match.id });
    var save = {};
    save[REPORTS] = JSON.stringify(others.concat(same));
    if (same.length >= mod.segnalazioni && distinct(same, "stanza") >= mod.partite && !activeSuspension(current, now)) {
        merge(save, sanction(current, mute ? "emoticon" : "segnalazioni", now, mod, kind, id));
        save[REPORTS] = JSON.stringify(others);
        // Esito per chi aveva segnalato: si accodano, il telefono li mostra tutti insieme. Solo gli ultimi 5: ogni avviso costa due
        // chiamate e lo script ne ha 25 (le segnalazioni si accumulano mentre la sospensione e' in corso).
        for (var k = Math.max(0, same.length - 5); k < same.length; k++) {
            try {
                var theirs = parseList(internal(["EsitoSegnalazione"], same[k].da).EsitoSegnalazione);
                theirs.push({ data: italianDate(same[k].t), modo: same[k].modo });
                saveInternal({ EsitoSegnalazione: JSON.stringify(theirs.slice(-10)) }, same[k].da);
            } catch (ex) { log.error("segnala, esito: " + ex); } // account cancellato: gli altri avvisi e la sanzione vanno avanti
        }
    }
    saveInternal(save, id);
    return { ok: true };
});

// --- Amicizie (giro Android 08/10): richiesta, Accetta o Rifiuta, amicizia reciproca, Rimuovi. PlayFab ha un elenco di amici per
// giocatore e nessuna richiesta: qui ogni voce ha un tag, "inviata" (A ha chiesto a B), "ricevuta" (B vede la richiesta di A) o
// "amico" (su entrambi gli elenchi). Gli elenchi li cambia solo il server: AddFriend/RemoveFriend del telefono facevano amicizie a
// senso unico. Le voci senza tag sono quelle aggiunte cosi' prima: "amici" le sistema (amico se l'altro aveva gia' questo
// giocatore, altrimenti una richiesta inviata che arriva all'altro), senza doppioni. Accetta conta solo se l'altro ha davvero la
// richiesta inviata nel suo elenco: un telefono modificato puo' mettere tag nel proprio elenco, non in quello degli altri.
var F_SENT = "inviata", F_RECEIVED = "ricevuta", F_FRIEND = "amico", F_LEGACY_MAX = 4;
var F_PROFILE = { ShowDisplayName: true, ShowAvatarUrl: true, ShowLastLogin: true, ShowStatistics: true };

function friendList(id, profile) {
    var r = server.GetFriendsList(profile ? { PlayFabId: id, ProfileConstraints: F_PROFILE } : { PlayFabId: id });
    return (r && r.Friends) || [];
}

function friendOf(list, id) {
    for (var i = 0; i < list.length; i++) if (list[i].FriendPlayFabId === id) return list[i];
    return null;
}

function friendTag(f) {
    var t = (f && f.Tags) || [];
    return t.indexOf(F_FRIEND) >= 0 ? F_FRIEND : t.indexOf(F_RECEIVED) >= 0 ? F_RECEIVED : t.indexOf(F_SENT) >= 0 ? F_SENT : null;
}

/** Voce di other nell'elenco di owner, col tag (creata se manca). */
function setFriend(owner, other, tag, exists) {
    if (!exists) {
        try { server.AddFriend({ PlayFabId: owner, FriendPlayFabId: other }); }
        catch (ex) { if (!/UsersAlreadyFriends/.test(apiError(ex))) throw ex; }
    }
    server.SetFriendTags({ PlayFabId: owner, FriendPlayFabId: other, Tags: [tag] });
}

function unfriend(owner, other) {
    try { server.RemoveFriend({ PlayFabId: owner, FriendPlayFabId: other }); } catch (ex) { log.error("RemoveFriend: " + apiError(ex)); }
}

/** owner ha bloccato other (BlockList del telefono, dati privati "Bloccati"): le sue richieste non arrivano. */
function blocks(owner, other) {
    var d = server.GetUserData({ PlayFabId: owner, Keys: ["Bloccati"] });
    var raw = d && d.Data && d.Data.Bloccati ? d.Data.Bloccati.Value : "";
    return ("|" + raw + "|").indexOf("|" + other + "|") >= 0;
}

function makeFriends(other, mineExists, theirsExists) {
    setFriend(currentPlayerId, other, F_FRIEND, mineExists);
    setFriend(other, currentPlayerId, F_FRIEND, theirsExists);
}

/** Voce a senso unico di prima della 2.65: il tag giusto, o null se l'altro account non c'e' piu' (voce tolta). */
function adoptLegacy(other) {
    var theirs;
    try { theirs = friendOf(friendList(other, false), currentPlayerId); } catch (ex) { unfriend(currentPlayerId, other); return null; }
    if (theirs) { makeFriends(other, true, true); return F_FRIEND; }
    setFriend(currentPlayerId, other, F_SENT, true);
    if (!blocks(other, currentPlayerId)) setFriend(other, currentPlayerId, F_RECEIVED, false);
    return F_SENT;
}

/** Amici, richieste ricevute e inviate, con nome, avatar (AvatarUrl "avatar:<id>"), statistiche e ultimo accesso. */
handlers.amici = phoneOnly(function (args) {
    if (isGuest()) return { ok: false, ospite: true };
    var list = friendList(currentPlayerId, true), out = [], fixed = 0;
    for (var i = 0; i < list.length; i++) {
        var f = list[i], id = f.FriendPlayFabId, tag = friendTag(f);
        if (!tag) {
            if (fixed >= F_LEGACY_MAX) tag = F_SENT; // le altre al prossimo giro; intanto "in attesa"
            else { fixed++; tag = adoptLegacy(id); if (!tag) continue; }
        }
        var p = f.Profile || {}, st = {};
        for (var k = 0; p.Statistics && k < p.Statistics.length; k++) st[p.Statistics[k].Name] = p.Statistics[k].Value;
        out.push({ id: id, nome: f.TitleDisplayName || p.DisplayName || f.Username || id, stato: tag, avatar: p.AvatarUrl || "",
            xp: st.XP | 0, partite: st.TotalGames | 0, vittorie: st.Wins | 0, scope: st.TotalScope | 0, ultimo: p.LastLogin || "" });
    }
    return { ok: true, amici: out };
});

/** A chiede a B ({id}). Se B l'aveva gia' chiesto ad A (o siete gia' amici): amici subito. */
handlers.richiestaAmico = phoneOnly(function (args) {
    if (isGuest()) return { ok: false, ospite: true };
    var id = String((args && args.id) || "");
    if (!id || id === currentPlayerId) return { ok: false, errore: "giocatore non valido" };
    var other = server.GetUserAccountInfo({ PlayFabId: id });
    if (!(other && other.UserInfo && other.UserInfo.Username)) return { ok: false, errore: "ospite" };
    var mine = friendOf(friendList(currentPlayerId, false), id), theirs = friendOf(friendList(id, false), currentPlayerId);
    if (theirs && friendTag(theirs) !== F_RECEIVED) { makeFriends(id, !!mine, true); return { ok: true, stato: F_FRIEND }; }
    setFriend(currentPlayerId, id, F_SENT, !!mine);
    if (!blocks(id, currentPlayerId)) setFriend(id, currentPlayerId, F_RECEIVED, !!theirs); // bloccato: non arriva, senza dirlo
    return { ok: true, stato: F_SENT };
});

/** B accetta la richiesta di A ({id}): amici su entrambi gli elenchi. */
handlers.accettaAmico = phoneOnly(function (args) {
    if (isGuest()) return { ok: false, ospite: true };
    var id = String((args && args.id) || "");
    var mine = friendOf(friendList(currentPlayerId, false), id), theirs = id ? friendOf(friendList(id, false), currentPlayerId) : null;
    var asked = friendTag(theirs);
    if (!mine || !(asked === F_SENT || asked === F_FRIEND)) return { ok: false, errore: "nessuna richiesta" };
    makeFriends(id, true, true);
    return { ok: true, stato: F_FRIEND };
});

/** Rifiuta, annulla la richiesta o rimuovi dagli amici ({id}): via da entrambi gli elenchi. */
handlers.rimuoviAmico = phoneOnly(function (args) {
    var id = String((args && args.id) || "");
    if (!id || id === currentPlayerId) return { ok: false, errore: "giocatore non valido" };
    unfriend(currentPlayerId, id);
    unfriend(id, currentPlayerId);
    return { ok: true };
});

/** Partita online con altre persone lasciata a meta' (il telefono la manda solo per gli account veri, mai contro i bot). */
handlers.abbandono = phoneOnly(function (args) {
    var now = Date.now(), mod = moderation();
    var data = internal(["Abbandoni", SOSP]);
    var sanctions = parseObject(data[SOSP]) || {};
    var list = recent(parseList(data.Abbandoni), now, mod.giorni);
    list.push({ t: now, modo: String((args && args.modo) || "").substring(0, 12) });
    var save = { Abbandoni: JSON.stringify(list) };
    if (list.length >= mod.abbandoni && !activeSuspension(sanctions, now)) {
        merge(save, sanction(sanctions, "abbandoni", now, mod, SOSP));
        sanctions = parseObject(save[SOSP]);
        save.Abbandoni = "[]";
    }
    saveInternal(save);
    return { ok: true, sospensione: activeSuspension(sanctions, now) };
});

/**
 * Stato per il telefono: sospensione ed emoticon spente in corso, esiti delle segnalazioni fatte (dati una volta sola).
 * Con {"stato":true} (prima della RIVINCITA) gli esiti restano per la Home.
 */
/**
 * Una sola sessione per account. Secondo giro 08/10 (scelta dell'utente): vince la sessione GIA' attiva, un nuovo accesso non butta
 * fuori nessuno. Ogni login vero ha un id nuovo; la sessione e' un affitto ({id, dev, t}) che il telefono rinnova a ogni chiamata
 * (battito ogni 30 s, anche al tavolo). Un nuovo accesso (nuova) entra solo se l'affitto e' libero, suo, scaduto (LEASE_MS senza battiti:
 * app chiusa, crash, rete persa: mai un blocco eterno) o dello stesso dispositivo (riavvio dopo un crash); altrimenti "occupato" coi
 * secondi che mancano. Un telefono che trova l'affitto di un altro (preso dopo che il suo era scaduto) e' "altrove". Esci lo libera
 * (fine). Le versioni vecchie non mandano "sessione": niente controllo. Il formato vecchio (solo l'id) vale come scaduto.
 */
var LEASE_MS = 90 * 1000, SESSION = "Sessione";

function session(args, raw, now) {
    var out = { altrove: false, occupato: false, secondi: 0 };
    var mine = args && typeof args.sessione === "string" && args.sessione !== "" ? args.sessione : null;
    if (!mine) return out;
    var dev = typeof args.dispositivo === "string" ? args.dispositivo : "";
    var cur = parseObject(raw) || (raw ? { id: String(raw), t: 0 } : null);
    var other = cur && cur.id !== mine, left = other ? LEASE_MS - (now - (+cur.t || 0)) : 0;
    if (args.fine) {
        if (cur && !other) server.UpdateUserInternalData({ PlayFabId: currentPlayerId, KeysToRemove: [SESSION] });
        return out;
    }
    if (other && (args.nuova ? left > 0 && !(dev && cur.dev === dev) : true)) {
        if (args.nuova) { out.occupato = true; out.secondi = Math.ceil(left / 1000); }
        else out.altrove = true;
        return out;
    }
    var save = {};
    save[SESSION] = JSON.stringify({ id: mine, dev: dev, t: now });
    saveInternal(save);
    return out;
}

/** Battito e ingresso della sessione (session): al login (nuova), ogni 30 s ovunque, a Esci (fine). */
handlers.sessione = phoneOnly(function (args) {
    var s = session(args, internal([SESSION])[SESSION], Date.now());
    return { ok: true, altrove: s.altrove, occupato: s.occupato, secondi: s.secondi };
});

handlers.moderazione = phoneOnly(function (args) {
    var now = Date.now(), data = internal([SOSP, MUTE, "EsitoSegnalazione", SESSION]);
    var s = session(args, data[SESSION], now);
    var esiti = [];
    if (!(args && args.stato)) {
        var one = parseObject(data.EsitoSegnalazione); // formato della 2.54: un solo esito
        esiti = one ? [one] : parseList(data.EsitoSegnalazione);
        if (data.EsitoSegnalazione) server.UpdateUserInternalData({ PlayFabId: currentPlayerId, KeysToRemove: ["EsitoSegnalazione"] });
    }
    // ora: l'orologio del server, per le sanzioni degli ospiti tenute sul dispositivo (l'ora del telefono si puo' spostare).
    return { ok: true, sospensione: activeSuspension(parseObject(data[SOSP]), now),
        silenzio: activeSuspension(parseObject(data[MUTE]), now), esiti: esiti, ora: now, altrove: s.altrove, occupato: s.occupato,
        secondi: s.secondi };
});

/**
 * Webhook di Photon (pannello Photon, Webhooks: PathCreate "RoomCreated<HOOK>", PathJoin "RoomJoined<HOOK>", PathLeave "RoomLeft<HOOK>",
 * PathClose "RoomClosed"; PathBeforeJoin vuoto; i nomi esatti li stampa carica.js).
 * Ogni funzione di questo script si puo' chiamare anche da un telefono (ExecuteCloudScript): i nomi dei webhook hanno un segreto
 * (HOOK) che sta solo qui e nel pannello Photon, cosi' un telefono non li trova e non si costruisce partite o posti. Il segreto vale
 * quanto quello dell'URL di Photon: se si sospetta che sia uscito, nuovo segreto (carica.js --nuovo) e nuova chiave Photon.
 * RoomClosed non si puo' rinominare (Photon): da un telefono non fa niente.
 * Nessun webhook rifiuta (ResultCode sempre 0): l'errore che Photon manda al telefono (32752) puo' contenere l'URL del webhook, con
 * la chiave di Photon e il segreto. Qualsiasi errore qui lascia passare. Nel pannello Photon HasErrorInfo resta spento.
 * Partita: a ogni stanza creata il server genera un id nuovo e tiene il record in uno Shared Group "partita<TAG>_<codice>" (id, codice,
 * ora di creazione, app, versione e regione di Photon, chi siede: "g<numero Photon>" = PlayFabId). Serve alle monete della vittoria per
 * abbandono (l'avversario lo dice il record) e alle segnalazioni (solo tra chi sedeva nella stessa partita). Il codice di una stanza e'
 * unico solo dentro la stessa app (AppId), versione e regione di Photon: un webhook di un'altra app, versione o regione con lo stesso
 * codice non tocca il
 * record (niente posti, niente cancellazione), e un record piu' giovane di MATCH_MS non si sostituisce.
 * Chi e' sospeso lo ferma il telefono (stato fresco prima di ogni partita online); con un'app modificata entra lo stesso e siede come
 * gli altri (si puo' segnalare, e chi vince per il suo abbandono e' pagato), ma premioPartita non gli da' niente per le partite online.
 * Photon esegue ogni webhook come il giocatore UserId (currentPlayerId = UserId), tranne RoomClosed che non ha giocatore. Se un
 * telefono scoprisse un nome agirebbe solo su se stesso (fromOther), e un posto gia' preso non cambia.
 * Gruppo e indice hanno nel nome un pezzo del segreto (TAG): le revisioni vecchie dello script, che un telefono puo' ancora far girare
 * (RevisionSelection), scrivono altri nomi ("stanza_" fino alla 2.61, un altro TAG dopo un cambio di segreto), che qui non contano.
 * Record orfani (RoomClosed non arrivato): chi crea una stanza la segna in "PartiteCreate<TAG>"; passate MATCH_MS si cancella, se il
 * record e' ancora quello (stesso id), alla sua prossima stanza creata o al prossimo avvio dell'app (pruneRooms).
 */
var MATCH_MS = 2 * 60 * 60 * 1000; // la partita piu' lunga: prima un record non si sostituisce ne' si pulisce
// Un record con biglietti (prove di presenza e dichiarazioni della Fase A) resta dopo la chiusura della stanza ("Chiusa") per il riesame
// delle partite incomplete (RIESAME_MS) e un giorno in piu'; lo toglie chi aveva creato la stanza (pruneRooms) o una stanza nuova con
// lo stesso codice. ponytail: se chi l'ha creata non torna piu' il record resta (pochi byte); serve una pulizia del titolo se cresce.
var KEEP_MS = 8 * 24 * 60 * 60 * 1000;

function hasTickets(match) { for (var a in match.biglietti) if (match.biglietti.hasOwnProperty(a)) return true; return false; }
var OK = { ResultCode: 0, Message: "ok" };
var HOOK = "__HOOK__"; // carica.js lo sostituisce con il segreto di Server/CloudScript/segreto.txt
// Con un segreto nuovo cambiano anche i nomi dei dati delle partite: le revisioni col segreto vecchio scrivono dati che qui non contano.
var TAG = HOOK === "__HOOK__" ? "" : HOOK.substring(1, 5);
var ROOMS = "PartiteCreate" + TAG;

function matchGroup(room) { return "partita" + TAG + "_" + room; }

/** Chiamata da un telefono per conto di un altro giocatore (da Photon currentPlayerId e' vuoto o e' proprio UserId). */
function fromOther(id) { return typeof currentPlayerId === "string" && currentPlayerId !== "" && currentPlayerId !== id; }

/** App (AppId), versione e regione di Photon del webhook: il codice di una stanza e' unico solo dentro queste. Con l'id di partita
 * generato dal server fanno l'identita' completa di una partita. */
function photonApp(args) {
    return String(args && args.AppId || "") + "/" + String(args && args.AppVersion || "") + "/" + String(args && args.Region || "");
}

/** Record della partita in corso con quel codice di stanza, o null. */
function readMatch(room) {
    try {
        var r = server.GetSharedGroupData({ SharedGroupId: matchGroup(room), GetMembers: true });
        if (r && r.Members && r.Members.length) return null; // gruppo creato da un telefono (Client API): solo il server crea i record
        var data = r && r.Data || {}, match = parseObject(data.Partita && data.Partita.Value);
        if (!match) return null;
        // g<numero> = chi siede (PathJoin), b<numero> = ha chiesto il biglietto di questa partita (inizioPartita), u<numero> = uscito per
        // sempre (PathLeave), d<numero> = risultato dichiarato (premioPartita). Solo il server li scrive.
        match.giocatori = {}; match.biglietti = {}; match.uscite = {}; match.dichiarazioni = {};
        for (var k in data) {
            var m = /^([gbud])(\d+)$/.exec(k);
            if (!data[k] || !m) continue;
            var v = data[k].Value;
            if (m[1] === "g") match.giocatori[m[2]] = v;
            else if (m[1] === "b" || m[1] === "u") match[m[1] === "b" ? "biglietti" : "uscite"][m[2]] = Number(v) || 1;
            else { var d = parseObject(v); if (d) match.dichiarazioni[m[2]] = d; }
        }
        if (data.Chiusa) match.chiusa = Number(data.Chiusa.Value) || 1;
        return match;
    } catch (ex) {
        return null; // stanza senza record (creata prima di questo script, o webhook di creazione fallito)
    }
}

/** Record della stanza di questo webhook, o null se manca o e' di un'altra versione o regione di Photon con lo stesso codice. */
function readMatchFor(args) {
    var match = readMatch(String(args && args.GameId || ""));
    return match && match.app === photonApp(args) ? match : null;
}

/** Stanza appena creata: id di partita nuovo, il record dell'eventuale partita vecchia con lo stesso codice sparisce.
 * Un record piu' giovane di MATCH_MS non si sostituisce (null = stanza senza record): e' una stanza con lo stesso codice in un'altra
 * versione o regione di Photon, un RoomCreated finto, o un codice riusato con RoomClosed perso (raro). */
function newMatch(room, id, now, app) {
    var group = matchGroup(room);
    try {
        server.CreateSharedGroup({ SharedGroupId: group });
    } catch (ex) { // codice gia' usato da una partita precedente: si riparte da un record vuoto
        var old = readMatch(room);
        if (old && !old.chiusa && now - (Number(old.creata) || 0) < MATCH_MS) return null; // una stanza chiusa si puo' riusare
        server.DeleteSharedGroup({ SharedGroupId: group });
        server.CreateSharedGroup({ SharedGroupId: group });
    }
    var match = { id: now.toString(36) + Math.floor(Math.random() * 2176782336).toString(36), codice: room, creata: now, app: app };
    server.UpdateSharedGroupData({ SharedGroupId: group, Data: { Partita: JSON.stringify(match), g1: id } }); // chi crea e' il numero 1
    return match;
}

/** Stanze create da questo giocatore piu' vecchie di MATCH_MS: record cancellato se e' ancora di quella partita (stesso id). Mai prima
 * (la partita puo' essere in corso). Al massimo `limit` per chiamata; quelle non fatte (o fallite) restano per la prossima. */
function pruneRooms(id, raw, now, add, limit) {
    var list = parseList(raw), keep = [], done = 0;
    if (add) list.push(add);
    for (var i = 0; i < list.length; i++) {
        var e = list[i];
        if (now - (Number(e.t) || 0) < MATCH_MS || done >= limit) { keep.push(e); continue; }
        done++;
        try {
            var m = readMatch(String(e.c));
            // Con biglietti: prove dei premi e dichiarazioni, si tiene per il riesame (KEEP_MS).
            if (m && m.id === e.p && hasTickets(m) && now - (Number(e.t) || 0) < KEEP_MS) { keep.push(e); continue; }
            if (m && m.id === e.p) server.DeleteSharedGroup({ SharedGroupId: matchGroup(String(e.c)) });
        } catch (ex) { keep.push(e); log.error("pruneRooms: " + ex); }
    }
    // ponytail: oltre 100 le piu' vecchie non si seguono piu' (solo spam): il loro record lo toglie ancora RoomClosed.
    if (done || add) { var d = {}; d[ROOMS] = JSON.stringify(keep.slice(-100)); saveInternal(d, id); }
}

/** PathCreate "RoomCreated<HOOK>": partita nuova, chi crea siede al numero 1. */
handlers["RoomCreated" + HOOK] = function (args) {
    try {
        var id = String(args && args.UserId || ""), room = String(args && args.GameId || "");
        if (!id || !room || fromOther(id)) return OK;
        var now = Date.now(), match = newMatch(room, id, now, photonApp(args));
        try { pruneRooms(id, internal([ROOMS], id)[ROOMS], now, match ? { c: room, p: match.id, t: now } : null, 2); } catch (ex) { log.error("pruneRooms: " + ex); }
    } catch (ex) {
        log.error("RoomCreated: " + ex);
    }
    return OK;
};

/** PathJoin "RoomJoined<HOOK>": chi siede al numero Photon ActorNr. Un posto preso non cambia (Photon non riusa un ActorNr, il rientro
 * tiene lo stesso); quanti al tavolo lo decide Photon (MaxPlayers), anche dopo chi e' uscito prima della partita. */
handlers["RoomJoined" + HOOK] = function (args) {
    try {
        var id = String(args && args.UserId || ""), actor = Number(args && args.ActorNr) || 0;
        if (!id || actor <= 0 || fromOther(id)) return OK;
        var match = readMatchFor(args);
        if (match && !match.giocatori[actor]) {
            var seat = {}; seat["g" + actor] = id;
            server.UpdateSharedGroupData({ SharedGroupId: matchGroup(String(args.GameId)), Data: seat });
        }
    } catch (ex) {
        log.error("RoomJoined: " + ex);
    }
    return OK;
};

/** PathLeave "RoomLeft<HOOK>": uscita per sempre (IsInactive false: LeaveRequest, tempo di rientro finito, o PlayerTtl 0 prima
 * dell'inizio). Una disconnessione con rientro possibile (IsInactive true) non conta. Prima uscita sola, mai cambiata. */
handlers["RoomLeft" + HOOK] = function (args) {
    try {
        var id = String(args && args.UserId || ""), actor = Number(args && args.ActorNr) || 0;
        if (!id || actor <= 0 || fromOther(id) || args.IsInactive === true || args.IsInactive === "true") return OK;
        var match = readMatchFor(args);
        if (match && match.giocatori[actor] === id && !match.uscite[actor]) {
            var left = {}; left["u" + actor] = String(Date.now());
            server.UpdateSharedGroupData({ SharedGroupId: matchGroup(String(args.GameId)), Data: left });
        }
    } catch (ex) {
        log.error("RoomLeft: " + ex);
    }
    return OK;
};

/** PathClose "RoomClosed" (nome fisso per Photon): stanza tolta da Photon. Il record senza biglietti si cancella; con biglietti resta
 * segnato "Chiusa" per il riesame (KEEP_MS, pruneRooms). */
handlers.RoomClosed = function (args) {
    try {
        if (typeof currentPlayerId === "string" && currentPlayerId !== "") return OK; // da Photon arriva senza giocatore: questo e' un telefono
        var match = readMatchFor(args);
        // Un record di pochi secondi e' di una stanza nuova con lo stesso codice: la chiusura in ritardo della vecchia non lo tocca.
        // Soglia bassa: una stanza lasciata prima della partita si chiude subito (EmptyRoomTtl 0).
        if (!match || Date.now() - (Number(match.creata) || 0) < 5 * 1000) return OK;
        if (hasTickets(match)) server.UpdateSharedGroupData({ SharedGroupId: matchGroup(String(args.GameId)), Data: { Chiusa: String(Date.now()) } });
        else server.DeleteSharedGroup({ SharedGroupId: matchGroup(String(args.GameId)) });
    } catch (ex) {
        log.error("RoomClosed: " + ex);
    }
    return OK;
};

// Per le prove con Node (Server/CloudScript/test.js): in PlayFab "module" non esiste.
if (typeof module !== "undefined") module.exports = { italianDate: italianDate, secondsToItalianMidnight: secondsToItalianMidnight, previousDate: previousDate,
    levelOf: levelOf, matchXp: matchXp };
