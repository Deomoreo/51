// Prova del CloudScript con un finto server PlayFab: node Server/CloudScript/test.js
var assert = require("assert");
var fs = require("fs");
var path = require("path");
var vm = require("vm");

function load(nowMs, titleData, hook) {
    // Dati di P1 in store.readOnly/internal; degli altri giocatori in store.others[id]. I PlayFabId che iniziano con "G" sono ospiti.
    var store = { readOnly: {}, internal: {}, stats: {}, wallet: { CO: 0, GE: 0 }, others: {}, groups: {} };
    function of(id, kind) {
        if (id === "P1") return store[kind];
        var o = store.others[id] = store.others[id] || { readOnly: {}, internal: {}, stats: {} };
        return o[kind];
    }
    function update(target, r) {
        Object.assign(target, r.Data || {});
        (r.KeysToRemove || []).forEach(function (k) { delete target[k]; });
    }
    var ctx = {
        handlers: {}, currentPlayerId: "P1", log: { error: function (e) { if (process.env.LOGERR) console.log(e); } }, JSON: JSON, Math: Math, Date: fixedDate(nowMs), String: String,
        Array: Array, isNaN: isNaN,
        server: {
            GetTitleData: function () { return { Data: titleData || {} }; },
            GetUserReadOnlyData: function (r) { return { Data: wrap(of(r.PlayFabId, "readOnly"), r.Keys) }; },
            UpdateUserReadOnlyData: function (r) { update(of(r.PlayFabId, "readOnly"), r); },
            GetUserInternalData: function (r) { return { Data: wrap(of(r.PlayFabId, "internal"), r.Keys) }; },
            UpdateUserInternalData: function (r) { update(of(r.PlayFabId, "internal"), r); },
            GetUserAccountInfo: function (r) { return { UserInfo: r.PlayFabId.charAt(0) === "G" ? {} : { Username: "u" + r.PlayFabId } }; },
            AddUserVirtualCurrency: function (r) { store.wallet[r.VirtualCurrency] += r.Amount; },
            GetPlayerStatistics: function (r) {
                var st = of(r.PlayFabId, "stats");
                return { Statistics: r.StatisticNames.filter(function (n) { return st[n] !== undefined; }).map(function (n) { return { StatisticName: n, Value: st[n] }; }) };
            },
            UpdatePlayerStatistics: function (r) { var st = of(r.PlayFabId, "stats"); r.Statistics.forEach(function (x) { st[x.StatisticName] = x.Value; }); },
            // Shared Group come PlayFab: errore se si crea un gruppo che c'e' o si legge/cancella uno che non c'e'.
            CreateSharedGroup: function (r) { if (store.groups[r.SharedGroupId]) throw new Error("in uso"); store.groups[r.SharedGroupId] = {}; },
            DeleteSharedGroup: function (r) { if (!store.groups[r.SharedGroupId]) throw new Error("non trovato"); delete store.groups[r.SharedGroupId]; },
            GetSharedGroupData: function (r) { var g = store.groups[r.SharedGroupId]; if (!g) throw new Error("non trovato"); return { Data: wrap(g, Object.keys(g)), Members: (store.groupMembers || {})[r.SharedGroupId] || [] }; },
            UpdateSharedGroupData: function (r) { var g = store.groups[r.SharedGroupId]; if (!g) throw new Error("non trovato"); Object.assign(g, r.Data || {}); }
        }
    };
    ctx.of = of;
    var src = fs.readFileSync(path.join(__dirname, "51.js"), "utf8");
    if (hook) src = src.replace('var HOOK = "__HOOK__";', 'var HOOK = "' + hook + '";'); // come carica.js
    vm.runInNewContext(src, ctx);
    // I webhook hanno nomi col segreto (qui il segnaposto): coi nomi di Photon di default un telefono non trova niente.
    ["RoomCreated", "RoomJoined"].forEach(function (n) {
        assert.strictEqual(ctx.handlers[n], undefined, n + " senza segreto");
        ctx.handlers[n] = ctx.handlers[n + (hook || "__HOOK__")];
    });
    ctx.store = store;
    ctx.setNow = function (ms) { ctx.Date = fixedDate(ms); vm.runInNewContext("0", ctx); };
    return ctx;
}

function wrap(obj, keys) {
    var out = {};
    keys.forEach(function (k) { if (obj[k] !== undefined) out[k] = { Value: obj[k] }; });
    return out;
}

function fixedDate(ms) {
    function D(a, b, c, d, e, f) { return arguments.length ? new (Function.prototype.bind.apply(Date, [null].concat([].slice.call(arguments))))() : new Date(ms); }
    D.now = function () { return ms; };
    D.UTC = Date.UTC;
    D.parse = Date.parse;
    return D;
}

var H = 3600000, DAY = 24 * H;
var t0 = Date.UTC(2026, 9, 1, 10, 0, 0); // 1 ottobre 2026, 12:00 in Italia

// Data italiana e mezzanotte (ora legale fino al 25/10/2026, poi solare).
var m = require("./51.js");
assert.strictEqual(m.italianDate(Date.UTC(2026, 9, 1, 21, 59)), "2026-10-01");
assert.strictEqual(m.italianDate(Date.UTC(2026, 9, 1, 22, 0)), "2026-10-02");
assert.strictEqual(m.italianDate(Date.UTC(2026, 11, 1, 22, 59)), "2026-12-01");
assert.strictEqual(m.italianDate(Date.UTC(2026, 11, 1, 23, 0)), "2026-12-02");
assert.strictEqual(m.secondsToItalianMidnight(Date.UTC(2026, 9, 1, 21, 0, 0)), 3600);
assert.strictEqual(m.previousDate("2026-03-01"), "2026-02-28");

// Premi giornalieri: serie, doppio riscatto, giorno saltato, forziere viola al giorno 7.
var c = load(t0);
assert.deepStrictEqual(pick(c.handlers.statoPremi()), { giorno: 1, riscattato: false });
var r = c.handlers.riscattaPremio();
assert.ok(r.ok); assert.strictEqual(r.monete, 50); assert.strictEqual(c.store.wallet.CO, 50);
assert.strictEqual(c.handlers.riscattaPremio().ok, false, "due volte lo stesso giorno");
for (var d = 2; d <= 6; d++) { c.setNow(t0 + (d - 1) * DAY); assert.strictEqual(c.handlers.riscattaPremio().giorno, d); }
c.setNow(t0 + 6 * DAY);
r = c.handlers.riscattaPremio();
assert.strictEqual(r.giorno, 7);
assert.strictEqual(r.forzieri.length, 1); assert.strictEqual(r.forzieri[0].colore, "viola");
assert.ok(r.monete >= 400 && r.monete <= 800 && r.gemme >= 35 && r.gemme <= 50, JSON.stringify(r));
c.setNow(t0 + 7 * DAY);
assert.strictEqual(c.handlers.statoPremi().giorno, 1, "dopo il 7 si riparte");
c.setNow(t0 + 9 * DAY);
assert.strictEqual(c.handlers.statoPremi().giorno, 1, "giorno saltato");

// Posta: riscatto singolo, doppio, scaduto; Raccogli tutto; senza id vale il titolo.
c = load(t0);
c.store.readOnly.Posta = JSON.stringify([
    { id: "a", titolo: "A", data: "2026-09-30", allegati: [{ tipo: "monete", quantita: 200 }, { tipo: "gemme", quantita: 10 }] },
    { titolo: "B", data: "2026-09-29", allegati: [{ tipo: "forziere", colore: "verde" }] },
    { id: "c", titolo: "C", data: "2026-09-01", scade: "2026-09-20", allegati: [{ tipo: "monete", quantita: 999 }] },
    { id: "d", titolo: "D", data: "2026-09-30" }
]);
r = c.handlers.riscattaPosta({ id: "a" });
assert.ok(r.ok); assert.strictEqual(c.store.wallet.CO, 200); assert.strictEqual(c.store.wallet.GE, 10);
assert.strictEqual(c.handlers.riscattaPosta({ id: "a" }).ok, false);
assert.strictEqual(c.handlers.riscattaPosta({ id: "c" }).ok, false, "scaduto");
r = c.handlers.riscattaTuttaPosta();
assert.strictEqual(JSON.stringify(r.riscattati), "[\"B\"]");
assert.ok(c.store.wallet.CO >= 350 && c.store.wallet.CO <= 500);
assert.strictEqual(c.handlers.riscattaTuttaPosta().ok, false);

// Posta per tutti: arriva una volta sola, anche dopo che il messaggio e' stato cancellato.
var globale = { PostaGlobale: JSON.stringify([{ id: "benvenuto", tipo: "team", titolo: "Benvenuto", allegati: [{ tipo: "monete", quantita: 200 }] }]) };
c = load(t0, globale);
assert.strictEqual(c.handlers.inizio().postaNuova, 1);
assert.strictEqual(c.handlers.inizio().postaNuova, 0);
assert.strictEqual(JSON.parse(c.store.readOnly.Posta)[0].data, "2026-10-01");
c.store.readOnly.Posta = "[]";
assert.strictEqual(c.handlers.inizio().postaNuova, 0);

// Monete di fine partita: 40/20, meta' coi bot, tetto 400 al giorno, si riparte il giorno dopo.
c = load(t0);
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).monete, 40);
assert.strictEqual(c.handlers.premioPartita({ vinta: false }).monete, 20);
assert.strictEqual(c.handlers.premioPartita({ vinta: true, allenamento: true }).monete, 20);
for (var i = 0; i < 20; i++) c.handlers.premioPartita({ vinta: true });
assert.strictEqual(c.store.wallet.CO, 400);
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).tetto, true);
c.setNow(t0 + DAY);
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).monete, 40);

// Statistiche e XP li scrive il server (come PlayerXp): 40/20 + 2 per scopa + 5 per accuso (bonus max 20), meta' coi bot.
assert.deepStrictEqual([99, 100, 219, 220, 1619, 1620].map(m.levelOf), [1, 2, 2, 3, 9, 10]);
assert.strictEqual(m.matchXp(true, 3, 1, false), 51);
assert.strictEqual(m.matchXp(false, 50, 50, true), 20);
c = load(t0);
r = c.handlers.premioPartita({ vinta: true, scope: 3, accusi: 1 });
assert.strictEqual(r.xp, 51);
assert.deepStrictEqual(c.store.stats, { TotalGames: 1, Wins: 1, XP: 51, Level: 1, TotalScope: 3, XPSettimana: 51 });
assert.strictEqual(r.esperienza, 51); assert.strictEqual(r.partite, 1); assert.strictEqual(r.vittorie, 1);
c.handlers.premioPartita({ vinta: false, scope: 9999, allenamento: true });
assert.strictEqual(c.store.stats.TotalScope, 33, "scope inventate: al massimo 30 a partita");
assert.strictEqual(c.store.stats.XP, 51 + 20);
r = c.handlers.premioPartita({ vinta: true, uscita: true, scope: 5 });
assert.strictEqual(r.monete, 0); assert.strictEqual(r.xp, 0);
assert.deepStrictEqual(c.store.stats, { TotalGames: 3, Wins: 1, XP: 71, Level: 1, TotalScope: 33, XPSettimana: 71 }, "uscita: persa, niente XP");
delete c.store.stats.XPSettimana; // azzeramento settimanale di PlayFab: la statistica torna vuota
c.handlers.premioPartita({ vinta: false });
assert.strictEqual(c.store.stats.XPSettimana, 20, "la settimana riparte da 0");
assert.strictEqual(c.store.stats.XP, 91);
c.store.stats.XP = 1600;
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).livello, 10);
for (i = 0; i < 60; i++) r = c.handlers.premioPartita({ vinta: true });
assert.strictEqual(r.limitePartite, true, "al massimo 60 risultati al giorno");
assert.strictEqual(c.store.stats.TotalGames, 60);
c.currentPlayerId = "G1";
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).ospite, true);
assert.strictEqual(c.of("G1", "stats").TotalGames, undefined, "ospite: niente");
// Solo il telefono, per se stesso: dall'URL dei webhook (UserId) o senza giocatore non si fa niente.
c.currentPlayerId = "P1";
assert.strictEqual(c.handlers.premioPartita({ vinta: true, UserId: "P1" }).ok, false);
assert.strictEqual(c.handlers.abbandono({ UserId: "P7" }).ok, false);
c.currentPlayerId = undefined;
assert.strictEqual(c.handlers.riscattaPremio({}).ok, false);
c.currentPlayerId = "P1";

// Vittoria per abbandono: monete al massimo 3 volte al giorno e una per avversario; oltre la vittoria resta senza monete.
// Ogni vittoria passa dal record della partita (stanza creata da P1, avversario al numero 2), come con i webhook veri.
c = load(t0);
var rooms = 0;
function seatIn(room, who, actor) { c.currentPlayerId = who; c.handlers.RoomJoined({ UserId: who, GameId: room, ActorNr: actor }); c.currentPlayerId = "P1"; }
function newRoom(who) {
    var room = "F" + (++rooms);
    c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: room });
    if (who) seatIn(room, who, 2);
    return room;
}
function forfeit(who, room) {
    return c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: room || newRoom(who), attore: 2 });
}
assert.strictEqual(forfeit("A").monete, 40);
var again = forfeit("A");
assert.strictEqual(again.monete, 0, "stesso avversario, stesso giorno");
assert.strictEqual(again.limiteAbbandoni, true);
assert.strictEqual(again.tetto, false);
assert.strictEqual(forfeit("C").monete, 40);
assert.strictEqual(forfeit("D").monete, 40);
assert.strictEqual(forfeit("B").limiteAbbandoni, true, "quarta vittoria per abbandono");
assert.strictEqual(c.handlers.premioPartita({ vinta: true }).monete, 40, "le partite finite restano premiate");
assert.strictEqual(c.handlers.premioPartita({ vinta: false, abbandono: true }).monete, 20, "abbandono vale solo per chi vince");
c.setNow(t0 + DAY);
// Controlli sul record: partita che esiste, chi chiede seduto li', al posto indicato un altro, una volta sola.
var paid = newRoom("E");
assert.strictEqual(forfeit("E", paid).monete, 40);
assert.strictEqual(forfeit("E", paid).abbandonoNonValido, true, "la stessa partita paga una volta sola");
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true }).abbandonoNonValido, true, "senza stanza");
var before = Object.assign({}, c.store.stats);
r = c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "NESSUNA", attore: 2, scope: 4 });
assert.strictEqual(r.abbandonoNonValido, true, "partita che non esiste");
assert.strictEqual(r.monete, 0); assert.strictEqual(r.xp, 0); assert.strictEqual(r.tetto, false); assert.strictEqual(r.limiteAbbandoni, false);
assert.strictEqual(c.store.stats.Wins, before.Wins + 1, "non valida: la vittoria resta");
assert.strictEqual(c.store.stats.XP, before.XP, "non valida: niente XP");
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: newRoom("F"), attore: 1 }).abbandonoNonValido, true, "il proprio posto");
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: newRoom(""), attore: 2 }).abbandonoNonValido, true, "posto vuoto");
var other = "F" + (++rooms);
c.currentPlayerId = "Q1"; c.handlers.RoomCreated({ UserId: "Q1", Type: "Create", GameId: other }); c.currentPlayerId = "P1";
seatIn(other, "G", 2);
assert.strictEqual(forfeit("G", other).abbandonoNonValido, true, "chi chiede non sedeva in quella partita");
c.setNow(t0 + 2 * DAY);
assert.strictEqual(forfeit("A").monete, 40, "il giorno dopo si riparte");

// Segnalazioni: un account diverso per volta, da almeno 3 partite diverse, solo con account; alla quinta sospensione di 24 ore
// ed esito a chi ha segnalato.
c = load(t0);
function as(id) { c.currentPlayerId = id; vm.runInNewContext("0", c); }
// La partita e' quella vera (record del server): X e chi segnala ci siedono.
function seatBoth(room, from) {
    var saved = c.currentPlayerId; c.currentPlayerId = "";
    if (!c.store.groups["partita_" + room]) c.handlers.RoomCreated({ UserId: "X", Type: "Create", GameId: room });
    var g = c.store.groups["partita_" + room], n = 1, taken = false;
    for (var k in g) if (k.charAt(0) === "g") { n++; if (g[k] === from) taken = true; }
    if (!taken) c.handlers.RoomJoined({ UserId: from, GameId: room, ActorNr: n });
    c.currentPlayerId = saved;
}
function report(from, room, motivo) { seatBoth(room, from); as(from); return c.handlers.segnala({ id: "X", modo: "1 vs 1", stanza: room, motivo: motivo }); }
as("G1");
assert.strictEqual(c.handlers.segnala({ id: "X" }).ok, false, "ospite");
as("P2");
assert.strictEqual(c.handlers.segnala({ id: "P2" }).ok, false, "se stesso");
assert.strictEqual(c.handlers.segnala({ id: "X", motivo: "boh" }).ok, false, "motivo sconosciuto");
[["P2", "A"], ["P2", "A"], ["P3", "A"], ["P4", "B"], ["P5", "B"]].forEach(function (x) { assert.ok(report(x[0], x[1]).ok); });
// Da fuori dalla partita, senza partita o contro chi non c'era: non conta (risposta uguale).
as("P8"); assert.ok(c.handlers.segnala({ id: "X", stanza: "A" }).ok);
as("P8"); c.handlers.segnala({ id: "X", stanza: "INVENTATA" });
as("P8"); c.handlers.segnala({ id: "X" });
as("P2"); c.handlers.segnala({ id: "P9", stanza: "A" });
assert.strictEqual(JSON.parse(c.of("X", "internal").SegnalazioniPartite).length, 4);
assert.strictEqual(c.of("P9", "internal").SegnalazioniPartite, undefined);
assert.strictEqual(c.of("X", "internal").Sospensione, undefined, "4 account diversi non bastano");
report("P6", "B");
assert.strictEqual(c.of("X", "internal").Sospensione, undefined, "5 account ma da 2 partite sole: non basta");
var getInternal = c.server.GetUserInternalData;
c.server.GetUserInternalData = function (r) {
    if (r.PlayFabId === "P4" && r.Keys[0] === "EsitoSegnalazione") throw new Error("account cancellato");
    return getInternal(r);
};
report("P7", "C", "nome");
c.server.GetUserInternalData = getInternal;
assert.strictEqual(c.of("P4", "internal").EsitoSegnalazione, undefined);
assert.ok(c.of("P5", "internal").EsitoSegnalazione, "un account cancellato non ferma gli altri esiti");
var sanz = JSON.parse(c.of("X", "internal").Sospensione);
assert.strictEqual(sanz.volte, 1); assert.strictEqual(sanz.motivo, "segnalazioni");
assert.strictEqual(Date.parse(sanz.fine) - t0, 24 * H);
assert.strictEqual(c.of("X", "internal").SegnalazioniPartite, "[]");
assert.strictEqual(JSON.parse(c.of("X", "internal").StoricoModerazione).length, 1, "storico");
as("P3");
r = c.handlers.moderazione();
assert.strictEqual(JSON.stringify(r.esiti), JSON.stringify([{ data: "2026-10-01", modo: "1 vs 1" }]));
assert.strictEqual(r.sospensione, null);
assert.strictEqual(c.handlers.moderazione().esiti.length, 0, "gli esiti si vedono una volta sola");
as("X");
r = c.handlers.moderazione();
assert.strictEqual(r.sospensione.motivo, "segnalazioni"); assert.strictEqual(r.sospensione.secondi, 24 * 3600);
// Segnalazioni vecchie di 7 giorni non contano.
c = load(t0);
["P2", "P3", "P4", "P5"].forEach(function (id, n) { report(id, "S" + n); });
c.setNow(t0 + 7 * DAY); report("P6", "S9");
assert.strictEqual(c.of("X", "internal").Sospensione, undefined);

// Emoticon offensive: contano a parte e spengono le emoticon (24 ore, 3 giorni, 7 giorni), non sospendono il gioco online.
c = load(t0);
["P2", "P3", "P4", "P5"].forEach(function (id, n) { report(id, "E" + n, "emoticon"); });
report("P6", "G", "gioco");
assert.strictEqual(c.of("X", "internal").SilenzioEmoticon, undefined, "i motivi non si sommano");
report("P7", "E9", "emoticon");
assert.strictEqual(c.of("X", "internal").Sospensione, undefined);
assert.strictEqual(Date.parse(JSON.parse(c.of("X", "internal").SilenzioEmoticon).fine) - t0, 24 * H);
assert.strictEqual(JSON.parse(c.of("X", "internal").SegnalazioniPartite).length, 1, "resta quella per gioco scorretto");
as("X");
assert.strictEqual(c.handlers.moderazione().silenzio.secondi, 24 * 3600);
// Esiti accodati: due sanzioni diverse segnalate dalla stessa persona si vedono entrambe.
c = load(t0);
["P2", "P3", "P4", "P5", "P6"].forEach(function (id, n) { report(id, "R" + n, "emoticon"); });
["P2", "P3", "P4", "P5", "P6"].forEach(function (id, n) { report(id, "R" + n, "gioco"); });
as("P2");
assert.strictEqual(c.handlers.moderazione().esiti.length, 2);

// Abbandoni: al quinto in 7 giorni 24 ore, poi 3 giorni, 3 giorni, 7 giorni; durante la sospensione non si accumula altro;
// dopo 30 giorni puliti si riparte da 24 ore.
c = load(t0);
for (i = 0; i < 4; i++) assert.strictEqual(c.handlers.abbandono({ modo: "1 vs 1" }).sospensione, null);
r = c.handlers.abbandono({ modo: "1 vs 1" });
assert.strictEqual(r.sospensione.motivo, "abbandoni"); assert.strictEqual(r.sospensione.secondi, 24 * 3600);
assert.strictEqual(c.handlers.abbandono().sospensione.volte, 1, "nessuna seconda sospensione mentre e' in corso");
var expected = [72, 72, 168, 168], now = t0;
expected.forEach(function (hours, n) {
    now += 8 * DAY; c.setNow(now);
    assert.strictEqual(c.handlers.moderazione().sospensione, null, "finita");
    for (i = 0; i < 4; i++) c.handlers.abbandono();
    r = c.handlers.abbandono();
    assert.strictEqual(r.sospensione.volte, n + 2); assert.strictEqual(r.sospensione.secondi, hours * 3600);
});
now += 7 * DAY + 31 * DAY; c.setNow(now);
for (i = 0; i < 5; i++) r = c.handlers.abbandono();
assert.strictEqual(r.sospensione.volte, 1, "30 giorni puliti"); assert.strictEqual(r.sospensione.secondi, 24 * 3600);
assert.strictEqual(JSON.parse(c.store.internal.StoricoModerazione).length, 6, "lo storico invece resta");
// Sospensione scritta a mano in Game Manager, con un testo libero.
c = load(t0);
c.store.internal.Sospensione = JSON.stringify({ volte: 1, fine: "2026-10-02T10:00:00Z", motivo: "Linguaggio offensivo" });
assert.strictEqual(JSON.stringify(c.handlers.moderazione().sospensione), JSON.stringify({ secondi: 24 * 3600, motivo: "Linguaggio offensivo", volte: 1, fine: "2026-10-02T10:00:00Z" }));
// Esito nel formato della 2.54 (un oggetto solo).
c.store.internal.EsitoSegnalazione = JSON.stringify({ data: "2026-09-28", modo: "1 vs 1" });
assert.strictEqual(c.handlers.moderazione({ stato: true }).esiti.length, 0, "prima della rivincita gli esiti restano");
assert.strictEqual(c.handlers.moderazione().esiti[0].data, "2026-09-28");
assert.strictEqual(c.handlers.moderazione().ora, t0, "ora del server per gli ospiti");

// Webhook di Photon: mai un rifiuto (l'errore puo' mostrare l'URL). Record della partita: chi crea e' il numero 1, chi entra siede al
// suo numero Photon, un posto preso non cambia.
c = load(t0);
c.currentPlayerId = ""; // chiamate di Photon: nessun giocatore che chiama
var EU = { AppId: "app-51", AppVersion: "2.62", Region: "eu" };
function hook(a) { return Object.assign({}, EU, a); }
function matchId(c, room) { return JSON.parse(c.store.groups["partita_" + room].Partita).id; }
assert.strictEqual(c.handlers.RoomCreated(hook({ UserId: "P1", Type: "Create", GameId: "R1" })).ResultCode, 0);
var m1 = matchId(c, "R1");
assert.strictEqual(c.store.groups.partita_R1.g1, "P1", "chi crea e' il numero 1");
assert.strictEqual(c.handlers.RoomJoined(hook({ UserId: "P2", GameId: "R1", ActorNr: 2 })).ResultCode, 0);
assert.strictEqual(c.store.groups.partita_R1.g2, "P2", "roster della partita");
// Chi e' sospeso siede come gli altri: lo ferma il telefono; con un'app modificata si puo' segnalare e il suo abbandono paga chi vince.
c.of("S1", "internal").Sospensione = JSON.stringify({ volte: 1, fine: "2026-10-02T10:00:00Z", motivo: "abbandoni" });
assert.strictEqual(c.handlers.RoomJoined(hook({ UserId: "S1", GameId: "R1", ActorNr: 3 })).ResultCode, 0);
assert.strictEqual(c.store.groups.partita_R1.g3, "S1", "sospeso: siede");
assert.strictEqual(c.handlers.RoomCreated(hook({ UserId: "S1", Type: "Create", GameId: "RS" })).ResultCode, 0);
assert.strictEqual(c.store.groups.partita_RS.g1, "S1", "sospeso: la sua stanza ha il record");
// Rientro con lo stesso numero: il posto resta. Un posto preso non cambia. Quanti al tavolo lo decide Photon, anche dopo chi e'
// uscito prima della partita.
c.handlers.RoomJoined(hook({ UserId: "P2", GameId: "R1", ActorNr: 2 }));
c.handlers.RoomJoined(hook({ UserId: "P9", GameId: "R1", ActorNr: 2 }));
assert.strictEqual(c.store.groups.partita_R1.g2, "P2", "un posto preso non cambia");
["X4", "X5"].forEach(function (u, i) { c.handlers.RoomJoined(hook({ UserId: u, GameId: "R1", ActorNr: 4 + i })); });
assert.strictEqual(c.store.groups.partita_R1.g5, "X5", "il quinto entrato dopo un'uscita siede");
// Lo stesso codice in un'altra versione o regione di Photon e' un'altra stanza: non tocca il record.
c.handlers.RoomCreated({ UserId: "A0", Type: "Create", GameId: "R1", AppVersion: "x", Region: "eu" });
assert.strictEqual(matchId(c, "R1"), m1, "record giovane: non si rifa'");
c.handlers.RoomJoined({ UserId: "A1", GameId: "R1", ActorNr: 6, AppVersion: "x", Region: "eu" });
c.handlers.RoomJoined({ UserId: "A2", GameId: "R1", ActorNr: 7, AppVersion: "2.62", Region: "us" });
assert.strictEqual(c.store.groups.partita_R1.g6, undefined, "altra versione: non siede");
assert.strictEqual(c.store.groups.partita_R1.g7, undefined, "altra regione: non siede");
c.handlers.RoomJoined({ UserId: "A3", GameId: "R1", ActorNr: 9, AppId: "altra-app", AppVersion: "2.62", Region: "eu" });
assert.strictEqual(c.store.groups.partita_R1.g9, undefined, "altra app Photon: non siede");
c.setNow(t0 + 60 * 1000);
c.handlers.RoomClosed({ GameId: "R1", AppVersion: "x", Region: "eu" });
assert.ok(c.store.groups.partita_R1, "chiusa in un'altra versione: il record resta");
// Chiamate da un telefono (ExecuteCloudScript, se scoprisse i nomi): solo per se'; RoomClosed niente.
c.currentPlayerId = "P8";
c.handlers.RoomJoined(hook({ UserId: "P7", GameId: "R1", ActorNr: 8 }));
assert.strictEqual(c.store.groups.partita_R1.g8, undefined, "un telefono non fa sedere un altro");
c.handlers.RoomCreated(hook({ UserId: "P7", Type: "Create", GameId: "R5" }));
assert.strictEqual(c.store.groups.partita_R5, undefined, "ne' crea la stanza di un altro");
c.handlers.RoomClosed(hook({ GameId: "R1" }));
assert.ok(c.store.groups.partita_R1, "RoomClosed chiamato da un telefono: non cancella");
c.setNow(t0);
// Monete: l'abbandono di un sospeso paga chi vince; gruppi di una revisione vecchia ("stanza_") o fatti da un telefono (Client API,
// con membri) non contano.
c.currentPlayerId = "P1";
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "R1", attore: 3 }).monete, 40, "abbandono di un sospeso");
c.store.groups.stanza_R0 = { Partita: JSON.stringify({ id: "vecchio" }), g1: "P1", g2: "P7" };
c.store.groups.partita_FINTA = { Partita: JSON.stringify({ id: "x" }), g1: "P1", g2: "P7" };
c.store.groupMembers = { partita_FINTA: ["P1"] };
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "R0", attore: 2 }).abbandonoNonValido, true, "gruppo di una revisione vecchia");
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "FINTA", attore: 2 }).abbandonoNonValido, true, "gruppo fatto da un telefono");
// Sospeso: niente per le partite online; allenamento coi bot e uscite contano come per tutti.
c.currentPlayerId = "S1";
r = c.handlers.premioPartita({ vinta: true });
assert.strictEqual(r.sospeso, true); assert.strictEqual(r.monete, 0);
assert.strictEqual(c.of("S1", "stats").TotalGames, undefined, "sospeso: niente statistiche online");
assert.strictEqual(c.handlers.premioPartita({ vinta: true, allenamento: true }).monete, 20);
assert.strictEqual(c.of("S1", "stats").TotalGames, 1, "sospeso: l'allenamento conta");
c.handlers.premioPartita({ uscita: true });
assert.deepStrictEqual([c.of("S1", "stats").TotalGames, c.of("S1", "stats").Wins], [2, 1], "sospeso: l'uscita e' una persa");
c.currentPlayerId = "";
// Stesso codice, partita nuova dopo 2 ore (la vecchia e' finita con RoomClosed perso): id nuovo, roster vecchio sparito.
var T2 = t0 + 2 * 3600 * 1000 + 1000;
c.setNow(T2);
c.handlers.RoomCreated(hook({ UserId: "P3", Type: "Create", GameId: "R1" }));
assert.notStrictEqual(matchId(c, "R1"), m1, "id di partita nuovo");
assert.strictEqual(c.store.groups.partita_R1.g2, undefined, "roster vecchio sparito");
c.handlers.RoomClosed(hook({ GameId: "R1" }));
assert.ok(c.store.groups.partita_R1, "chiusura in ritardo della vecchia: il record nuovo resta");
c.setNow(T2 + 5 * 1000);
c.handlers.RoomClosed(hook({ GameId: "R1" }));
assert.strictEqual(c.store.groups.partita_R1, undefined, "stanza chiusa: record tolto");
assert.strictEqual(c.handlers.RoomClosed(hook({ GameId: "R1" })).ResultCode, 0, "chiusa due volte");
assert.strictEqual(c.handlers.RoomJoined(hook({ UserId: "P5", GameId: "SENZA", ActorNr: 2 })).ResultCode, 0, "stanza senza record");
c.setNow(t0);
// Vittoria per abbandono: l'avversario lo dice il record della partita, non il telefono.
c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: "R9" });
c.handlers.RoomJoined({ UserId: "P7", GameId: "R9", ActorNr: 2 });
c.currentPlayerId = "P1";
c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "R9", attore: 2 });
assert.ok(JSON.parse(c.store.internal.PartiteOggi).abbandoni.indexOf("P7") >= 0, "id dal record anche se il telefono non lo sa");
// Pulizia dei record orfani (RoomClosed mai arrivato): dopo 2 ore li toglie chi li ha creati, solo se il codice non e' stato riusato;
// mai prima, anche dopo tante stanze nuove (la partita puo' essere in corso).
c.currentPlayerId = "";
c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: "R10" });
for (i = 0; i < 25; i++) c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: "Z" + i });
assert.ok(c.store.groups.partita_R9 && c.store.groups.partita_R10, "stanze giovani: i record restano");
c.setNow(t0 + 2 * 3600 * 1000 + 60 * 1000);
c.handlers.RoomCreated({ UserId: "Q2", Type: "Create", GameId: "R10" });
c.currentPlayerId = "P1";
for (i = 0; i < 7; i++) c.handlers.inizio({});
assert.strictEqual(c.store.groups.partita_R9, undefined, "record orfano tolto all'avvio");
assert.strictEqual(c.store.groups.partita_R10.g1, "Q2", "codice riusato: il record nuovo resta");
assert.strictEqual(JSON.parse(c.of("P1", "internal").PartiteCreate).length, 0, "indice svuotato");
// Dati mancanti o PlayFab che non risponde: si passa.
c.setNow(t0);
c.currentPlayerId = "";
assert.strictEqual(c.handlers.RoomJoined({}).ResultCode, 0);
assert.strictEqual(c.handlers.RoomCreated({}).ResultCode, 0);
assert.strictEqual(c.handlers.RoomClosed({}).ResultCode, 0);
c.server.GetSharedGroupData = function () { throw new Error("PlayFab giu'"); };
assert.strictEqual(c.handlers.RoomJoined({ UserId: "P2", GameId: "R10", ActorNr: 3 }).ResultCode, 0, "un errore lascia passare");
c.server.CreateSharedGroup = function () { throw new Error("PlayFab giu'"); };
assert.strictEqual(c.handlers.RoomCreated({ UserId: "P2", GameId: "R11" }).ResultCode, 0, "un errore lascia passare");

// Con il segreto (carica.js) i nomi dei webhook e dei dati delle partite cambiano con lui: una revisione col segreto vecchio
// scrive dati che quella nuova non legge.
c = load(t0, null, "_abcdef0123456789abcdef01");
assert.strictEqual(c.handlers.RoomCreated__HOOK__, undefined);
c.currentPlayerId = "";
c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: "T1" });
assert.ok(c.store.groups.partitaabcd_T1, "gruppo col segreto");
assert.strictEqual(c.store.groups.partita_T1, undefined);
assert.ok(c.store.internal.PartiteCreateabcd);
c.store.groups.partita_T9 = { Partita: JSON.stringify({ id: "vecchio", creata: t0 }), g1: "P1", g2: "P7" };
c.currentPlayerId = "P1";
assert.strictEqual(c.handlers.premioPartita({ vinta: true, abbandono: true, stanza: "T9", attore: 2 }).abbandonoNonValido, true, "gruppo del segreto vecchio");

function pick(s) { return { giorno: s.giorno, riscattato: s.riscattato }; }
console.log("CloudScript: tutte le prove passate");
