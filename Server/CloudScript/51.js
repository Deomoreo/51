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
//   Players -> Player Data -> Read Only: "Posta" (messaggi), "Premi" (serie dei premi giornalieri), "PostaGlobaleRicevuta".
//   Players -> Internal Data: "PartiteOggi" (monete date oggi dalle partite), "RisultatiOggi" (risultati contati oggi);
//     moderazione: "SegnalazioniPartite", "Abbandoni",
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
    partita: { vittoria: 40, sconfitta: 20, tetto: 400, abbandoni: 3, partiteGiorno: 60 }
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
            partita: e.partita || DEFAULTS.partita
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

/** Da' i premi e ritorna cosa e' arrivato davvero: [{tipo:"monete"|"gemme", quantita, forziere?}]. */
function grant(gifts, eco) {
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
    if (coins > 0) server.AddUserVirtualCurrency({ PlayFabId: currentPlayerId, VirtualCurrency: "CO", Amount: coins });
    if (gems > 0) server.AddUserVirtualCurrency({ PlayFabId: currentPlayerId, VirtualCurrency: "GE", Amount: gems });
    return { monete: coins, gemme: gems, forzieri: got };
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
    return now - mailDate(m) <= MAIL_DAYS * 86400000;
}

/** Copia nella Posta i messaggi di "PostaGlobale" non ancora ricevuti (Benvenuto, risarcimenti...). */
function deliverGlobalMail(now) {
    var globals = parseList(titleData(["PostaGlobale"]).PostaGlobale);
    if (globals.length === 0) return 0;
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
    for (var i = 0; i < mail.length; i++) if (now - mailDate(mail[i]) <= MAIL_DAYS * 86400000) out.push(mail[i]);
    return out;
}

function claimMail(ids, now) {
    var eco = economy();
    var mail = parseList(readOnly(["Posta"]).Posta);
    var gifts = [], claimed = [];
    for (var i = 0; i < mail.length; i++) {
        var m = mail[i];
        var id = m && (m.id || m.titolo); // senza id il telefono usa il titolo (MailService)
        if ((ids === null || ids.indexOf(id) >= 0) && canClaim(m, now)) {
            gifts = gifts.concat(m.allegati);
            m.riscattato = true;
            claimed.push(id);
        }
    }
    if (claimed.length === 0) return { ok: false, errore: "niente da riscattare" };
    // Prima segna riscattato, poi da' i premi: un errore a meta' non permette di riscattare due volte.
    saveReadOnly({ Posta: JSON.stringify(mail) });
    var got = grant(gifts, eco);
    return { ok: true, riscattati: claimed, monete: got.monete, gemme: got.gemme, forzieri: got.forzieri };
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

/** All'avvio: consegna la Posta per tutti e ritorna lo stato dei premi giornalieri. */
handlers.inizio = phoneOnly(function (args) {
    var now = Date.now();
    var nuovi = deliverGlobalMail(now);
    try { pruneRooms(currentPlayerId, internal([ROOMS])[ROOMS], now, null, 5); } catch (ex) { log.error("pruneRooms: " + ex); }
    var s = dailyState(now);
    return { ok: true, postaNuova: nuovi, giorno: s.giorno, riscattato: s.riscattato, secondi: s.secondi };
});

handlers.statoPremi = phoneOnly(function (args) {
    var s = dailyState(Date.now());
    return { ok: true, giorno: s.giorno, riscattato: s.riscattato, secondi: s.secondi };
});

handlers.riscattaPremio = phoneOnly(function (args) {
    var now = Date.now();
    var s = dailyState(now);
    if (s.riscattato) return { ok: false, errore: "gia' riscattato", giorno: s.giorno, riscattato: true, secondi: s.secondi };
    var eco = economy();
    var gifts = eco.settimana[s.giorno - 1] || [];
    saveReadOnly({ Premi: JSON.stringify({ giorno: s.giorno, ultimo: italianDate(now) }) });
    var got = grant(gifts, eco);
    return { ok: true, giorno: s.giorno, riscattato: true, secondi: secondsToItalianMidnight(now),
        monete: got.monete, gemme: got.gemme, forzieri: got.forzieri };
});

handlers.riscattaPosta = phoneOnly(function (args) {
    var id = args && args.id;
    if (!id) return { ok: false, errore: "id mancante" };
    return claimMail([String(id)], Date.now());
});

handlers.riscattaTuttaPosta = phoneOnly(function (args) {
    return claimMail(null, Date.now());
});

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
var STATS = ["TotalGames", "Wins", "XP", "Level", "TotalScope", "XPSettimana"];
var MAX_SCOPE = 30; // scope contate per partita: oltre e' un numero inventato

/**
 * Fine partita (scelte dell'utente 01/10 e 02/10). Monete: 40 vittoria, 20 sconfitta, meta' coi bot, tetto giornaliero. XP e
 * statistiche del profilo le scrive solo il server: il telefono manda l'esito (vinta, allenamento, scope, accusi) e non puo' piu'
 * mettere numeri a piacere. Chi ha vinto lo dice ancora il telefono (provarlo richiede un server di gioco autorevole): per questo
 * contano al massimo "partiteGiorno" risultati al giorno.
 * Vittoria per abbandono ({"abbandono":true,"stanza","attore"}): la partita deve esistere nel record del server, chi chiede deve
 * sedere li', al posto indicato un altro (l'avversario), e paga una volta sola; le monete al massimo "abbandoni" volte al giorno e una
 * per avversario. Oltre, o se non e' valida (record mancante, posto sbagliato, gia' pagata), la vittoria resta ma senza monete ne' XP.
 * Uscita a meta' ({"uscita":true}): partita persa, senza XP ne' monete. Ospiti (account senza nome utente): niente. Sospesi: niente
 * per le partite online (chi e' sospeso lo ferma il telefono; con un'app modificata gioca online lo stesso, ma qui non conta);
 * allenamento coi bot e uscite contano come per tutti. Le revisioni vecchie dello script (un telefono le puo' far girare) pagano
 * senza guardare la sospensione, ma dentro lo stesso tetto giornaliero di monete ("PartiteOggi"), che resta il limite vero.
 */
handlers.premioPartita = phoneOnly(function (args) {
    args = args || {};
    var me = server.GetUserAccountInfo({ PlayFabId: currentPlayerId });
    if (!(me && me.UserInfo && me.UserInfo.Username)) return { ok: true, monete: 0, ospite: true };
    var eco = economy(), p = eco.partita, quit = !!args.uscita, won = !!args.vinta && !quit, training = !!args.allenamento;
    // Giorno del server (ora italiana dall'orologio di PlayFab), mai quello del telefono: tetti di monete e di risultati per account e giorno.
    var today = italianDate(Date.now());
    // Contatori in chiavi separate: le revisioni vecchie dello script riscrivono "PartiteOggi" senza il conto dei risultati.
    var data = internal(["PartiteOggi", "RisultatiOggi", SOSP]), s = parseObject(data.PartiteOggi), n = parseObject(data.RisultatiOggi);
    if (!training && !quit && activeSuspension(parseObject(data[SOSP]), Date.now())) return { ok: true, monete: 0, sospeso: true };
    var given = 0, forfeits = [], played = n && n.data === today ? n.partite | 0 : 0;
    if (s && s.data === today) { given = s.monete | 0; if (s.abbandoni instanceof Array) forfeits = s.abbandoni; }
    var maxGames = (p.partiteGiorno != null ? p.partiteGiorno : DEFAULTS.partita.partiteGiorno) | 0;
    if (played >= maxGames) return { ok: true, monete: 0, limitePartite: true };
    var amount = quit ? 0 : won ? p.vittoria | 0 : p.sconfitta | 0;
    if (training) amount = Math.floor(amount / 2);
    var limit = false, invalid = false;
    if (won && args.abbandono) {
        var room = String(args.stanza || ""), match = room ? readMatch(room) : null, seat = Number(args.attore) || 0;
        var recorded = match && match.giocatori[seat], mine = false;
        if (match) for (var g in match.giocatori) if (match.giocatori[g] === currentPlayerId) mine = true;
        var paidKey = "pagato_" + currentPlayerId, valid = match && mine && recorded && recorded !== currentPlayerId && !match[paidKey];
        if (!valid) invalid = true;
        else {
            // ponytail: due richieste nello stesso istante passano entrambe (PlayFab non ha scritture condizionate), come i tetti giornalieri.
            server.UpdateSharedGroupData({ SharedGroupId: matchGroup(room), Data: (function (d) { d[paidKey] = "1"; return d; })({}) });
            var who = String(recorded).substring(0, 32), max = (p.abbandoni != null ? p.abbandoni : DEFAULTS.partita.abbandoni) | 0;
            limit = forfeits.length >= max || forfeits.indexOf(who) >= 0;
            if (!limit) forfeits.push(who);
        }
        if (limit || invalid) amount = 0;
    }
    amount = Math.max(0, Math.min(amount, (p.tetto | 0) - given));
    saveInternal({ PartiteOggi: JSON.stringify({ data: today, monete: given + amount, abbandoni: forfeits }),
        RisultatiOggi: JSON.stringify({ data: today, partite: played + 1 }) });
    var xp = quit || limit || invalid ? 0 : matchXp(won, args.scope, args.accusi, training);
    var scope = quit ? 0 : Math.min(MAX_SCOPE, Math.max(0, args.scope | 0));
    var st = {}, got = server.GetPlayerStatistics({ PlayFabId: currentPlayerId, StatisticNames: STATS });
    var list = got && got.Statistics || [];
    for (var i = 0; i < list.length; i++) st[list[i].StatisticName] = list[i].Value | 0;
    st.TotalGames = (st.TotalGames | 0) + 1;
    st.Wins = (st.Wins | 0) + (won ? 1 : 0);
    st.XP = (st.XP | 0) + xp;
    st.TotalScope = (st.TotalScope | 0) + scope;
    st.Level = levelOf(st.XP);
    st.XPSettimana = (st.XPSettimana | 0) + xp;
    var upd = [];
    for (var k = 0; k < STATS.length; k++) upd.push({ StatisticName: STATS[k], Value: st[STATS[k]] });
    server.UpdatePlayerStatistics({ PlayFabId: currentPlayerId, Statistics: upd });
    if (amount > 0) server.AddUserVirtualCurrency({ PlayFabId: currentPlayerId, VirtualCurrency: "CO", Amount: amount });
    return { ok: true, monete: amount, tetto: !limit && !invalid && !quit && amount === 0 && given >= (p.tetto | 0), limiteAbbandoni: limit,
        abbandonoNonValido: invalid, xp: xp, statistiche: true, partite: st.TotalGames, vittorie: st.Wins, esperienza: st.XP, scopeTotali: st.TotalScope, livello: st.Level };
});

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
handlers.moderazione = phoneOnly(function (args) {
    var now = Date.now(), data = internal([SOSP, MUTE, "EsitoSegnalazione"]);
    var esiti = [];
    if (!(args && args.stato)) {
        var one = parseObject(data.EsitoSegnalazione); // formato della 2.54: un solo esito
        esiti = one ? [one] : parseList(data.EsitoSegnalazione);
        if (data.EsitoSegnalazione) server.UpdateUserInternalData({ PlayFabId: currentPlayerId, KeysToRemove: ["EsitoSegnalazione"] });
    }
    // ora: l'orologio del server, per le sanzioni degli ospiti tenute sul dispositivo (l'ora del telefono si puo' spostare).
    return { ok: true, sospensione: activeSuspension(parseObject(data[SOSP]), now),
        silenzio: activeSuspension(parseObject(data[MUTE]), now), esiti: esiti, ora: now };
});

/**
 * Webhook di Photon (pannello Photon, Webhooks: PathCreate "RoomCreated<HOOK>", PathJoin "RoomJoined<HOOK>", PathClose "RoomClosed";
 * PathBeforeJoin e PathLeave vuoti; i nomi esatti li stampa carica.js).
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
        match.giocatori = {};
        for (var k in data) {
            if (!data[k]) continue;
            if (k.charAt(0) === "g") match.giocatori[k.substring(1)] = data[k].Value;
            else if (k.indexOf("pagato_") === 0) match[k] = data[k].Value; // vittoria per abbandono gia' pagata a quel giocatore
        }
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
        if (old && now - (Number(old.creata) || 0) < MATCH_MS) return null;
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

/** PathClose "RoomClosed" (nome fisso per Photon): stanza tolta da Photon, il suo record non serve piu'. */
handlers.RoomClosed = function (args) {
    try {
        if (typeof currentPlayerId === "string" && currentPlayerId !== "") return OK; // da Photon arriva senza giocatore: questo e' un telefono
        var match = readMatchFor(args);
        // Un record di pochi secondi e' di una stanza nuova con lo stesso codice: la chiusura in ritardo della vecchia non lo tocca.
        // Soglia bassa: una stanza lasciata prima della partita si chiude subito (EmptyRoomTtl 0).
        if (match && Date.now() - (Number(match.creata) || 0) >= 5 * 1000) server.DeleteSharedGroup({ SharedGroupId: matchGroup(String(args.GameId)) });
    } catch (ex) {
        log.error("RoomClosed: " + ex);
    }
    return OK;
};

// Per le prove con Node (Server/CloudScript/test.js): in PlayFab "module" non esiste.
if (typeof module !== "undefined") module.exports = { italianDate: italianDate, secondsToItalianMidnight: secondsToItalianMidnight, previousDate: previousDate,
    levelOf: levelOf, matchXp: matchXp };
