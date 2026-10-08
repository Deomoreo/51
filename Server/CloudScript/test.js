// Prova del CloudScript con un finto server PlayFab: node Server/CloudScript/test.js
var assert = require("assert");
var fs = require("fs");
var path = require("path");
var vm = require("vm");

function load(nowMs, titleData, hook) {
    // Dati di P1 in store.readOnly/internal; degli altri giocatori in store.others[id]. I PlayFabId che iniziano con "G" sono ospiti.
    var store = { readOnly: {}, internal: {}, stats: {}, wallet: { CO: 0, GE: 0 }, others: {}, groups: {}, friends: {} };
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
            GetTitleInternalData: function () { return { Data: store.titleInternal || {} }; },
            GetUserReadOnlyData: function (r) { return { Data: wrap(of(r.PlayFabId, "readOnly"), r.Keys) }; },
            UpdateUserReadOnlyData: function (r) { update(of(r.PlayFabId, "readOnly"), r); },
            GetUserInternalData: function (r) { return { Data: wrap(of(r.PlayFabId, "internal"), r.Keys) }; },
            UpdateUserInternalData: function (r) { update(of(r.PlayFabId, "internal"), r); },
            GetUserAccountInfo: function (r) { return { UserInfo: r.PlayFabId.charAt(0) === "G" ? {} : { Username: "u" + r.PlayFabId } }; },
            AddUserVirtualCurrency: function (r) {
                if (store.abort) throw new Error("script fermato"); // prima dell'accredito, senza risposta
                if (store.refuse && store.refuse[r.VirtualCurrency]) throw { apiErrorInfo: { apiError: { error: "InvalidVirtualCurrency", errorCode: 1050, errorMessage: "no" } } };
                store.wallet[r.VirtualCurrency] += r.Amount;
                // errore senza risposta di PlayFab (rete, tempo scaduto) dopo che l'accredito e' passato: il caso "incerto"
                if (store.lost) throw new Error("timeout");
                return { Balance: store.wallet[r.VirtualCurrency] };
            },
            GetUserInventory: function (r) { return { VirtualCurrency: { CO: store.wallet.CO, GE: store.wallet.GE } }; },
            GetPlayerStatistics: function (r) {
                var st = of(r.PlayFabId, "stats");
                return { Statistics: r.StatisticNames.filter(function (n) { return st[n] !== undefined; }).map(function (n) { return { StatisticName: n, Value: st[n] }; }) };
            },
            // Amici come PlayFab: un elenco per giocatore (store.friends[id][altro] = tag), AddFriend rifiuta i doppioni.
            GetFriendsList: function (r) {
                var mine = store.friends[r.PlayFabId] || {};
                return { Friends: Object.keys(mine).map(function (id) {
                    return { FriendPlayFabId: id, Tags: mine[id], TitleDisplayName: "n" + id,
                        Profile: r.ProfileConstraints ? { AvatarUrl: (store.avatars || {})[id], Statistics: [{ Name: "XP", Value: 120 }] } : undefined };
                }) };
            },
            AddFriend: function (r) {
                var mine = store.friends[r.PlayFabId] = store.friends[r.PlayFabId] || {};
                if (mine[r.FriendPlayFabId]) throw { apiErrorInfo: { apiError: { error: "UsersAlreadyFriends", errorCode: 1183, errorMessage: "gia'" } } };
                mine[r.FriendPlayFabId] = [];
            },
            SetFriendTags: function (r) {
                var mine = store.friends[r.PlayFabId] || {};
                if (!mine[r.FriendPlayFabId]) throw new Error("non amici");
                mine[r.FriendPlayFabId] = r.Tags;
            },
            RemoveFriend: function (r) { var mine = store.friends[r.PlayFabId] || {}; delete mine[r.FriendPlayFabId]; },
            GetUserData: function (r) { return { Data: wrap((store.userData || {})[r.PlayFabId] || {}, r.Keys) }; },
            UpdatePlayerStatistics: function (r) { var st = of(r.PlayFabId, "stats"); r.Statistics.forEach(function (x) { st[x.StatisticName] = x.Value; }); },
            // Shared Group come PlayFab: errore se si crea un gruppo che c'e' o si legge/cancella uno che non c'e'.
            CreateSharedGroup: function (r) { if (store.groups[r.SharedGroupId]) throw new Error("in uso"); store.groups[r.SharedGroupId] = {}; },
            DeleteSharedGroup: function (r) { if (!store.groups[r.SharedGroupId]) throw new Error("non trovato"); delete store.groups[r.SharedGroupId]; },
            GetSharedGroupData: function (r) { var g = store.groups[r.SharedGroupId]; if (!g) throw new Error("non trovato"); return { Data: wrap(g, Object.keys(g)), Members: (store.groupMembers || {})[r.SharedGroupId] || [] }; },
            UpdateSharedGroupData: function (r) { var g = store.groups[r.SharedGroupId]; if (!g) throw new Error("non trovato"); Object.assign(g, r.Data || {}); },
            WritePlayerEvent: function (r) { (store.events = store.events || []).push({ id: r.PlayFabId, nome: r.EventName, dati: r.Body }); }
        }
    };
    ctx.of = of;
    var src = fs.readFileSync(path.join(__dirname, "51.js"), "utf8");
    if (hook) src = src.replace('var HOOK = "__HOOK__";', 'var HOOK = "' + hook + '";'); // come carica.js
    vm.runInNewContext(src, ctx);
    // I webhook hanno nomi col segreto (qui il segnaposto): coi nomi di Photon di default un telefono non trova niente.
    ["RoomCreated", "RoomJoined", "RoomLeft"].forEach(function (n) {
        assert.strictEqual(ctx.handlers[n], undefined, n + " senza segreto");
        ctx.handlers[n] = ctx.handlers[n + (hook || "__HOOK__")];
    });
    ctx.store = store;
    ctx.now = nowMs;
    ctx.setNow = function (ms) { ctx.now = ms; ctx.Date = fixedDate(ms); vm.runInNewContext("0", ctx); };
    return ctx;
}

function wrap(obj, keys) {
    var out = {};
    (keys || Object.keys(obj)).forEach(function (k) { if (obj[k] !== undefined) out[k] = { Value: obj[k] }; }); // senza Keys: tutte, come PlayFab
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

// Una partita premiabile come la fanno i telefoni (#141, #143, Fase A): biglietto del server a inizio partita (inizioPartita) per
// ognuno, almeno un minuto di gioco, poi premioPartita col biglietto e la dichiarazione. Online = stanza col record dei webhook in
// cui l'avversario (args.altro, "QX") ha chiesto anche lui il biglietto e dichiara lo stesso risultato; con args.abbandono esce per
// sempre (PathLeave) senza dichiarare; allenamento = senza stanza. args.stanza: una stanza preparata dalla prova (nessun avversario
// automatico). Usa la "c" e il giocatore correnti.
var roomSeq = 0;
function actingAs(id, fn) { var me = c.currentPlayerId; c.currentPlayerId = id; try { return fn(); } finally { c.currentPlayerId = me; } }
function onlineRoom(other) {
    var me = c.currentPlayerId, room = "Q" + (++roomSeq);
    actingAs("", function () { // webhook di Photon
        c.handlers.RoomCreated({ UserId: me, Type: "Create", GameId: room });
        c.handlers.RoomJoined({ UserId: other || "QX", GameId: room, ActorNr: 2 });
    });
    return room;
}
// Dichiarazione coerente di un 1v1: posto 0 = chi ha creato la stanza (numero Photon 1), posto 1 = l'avversario.
function decl(won, posto) {
    var first = won === (posto === 0);
    return { vinta: won, punti: first ? [51, 30] : [30, 51], smazzate: 4, vincitore: first ? 0 : 1, posto: posto, squadre: false, giocatori: 2, versione: "2.64" };
}
function ticket(args) {
    args = args || {};
    if (args.allenamento) return c.handlers.inizioPartita({ locale: "L" + (++roomSeq) });
    var room = args.stanza || onlineRoom(args.altro), tk = c.handlers.inizioPartita({ stanza: room });
    if (!args.stanza) tk.altro = actingAs(args.altro || "QX", function () { return c.handlers.inizioPartita({ stanza: room }); });
    tk.stanza = room;
    return tk;
}
function claim(args) {
    args = args || {};
    var tk = ticket(args), other = args.altro || "QX";
    c.setNow(c.now + 61 * 1000);
    if (tk.altro && !args.uscita) {
        if (args.abbandono) actingAs("", function () { c.handlers.RoomLeft({ UserId: other, GameId: tk.stanza, ActorNr: 2 }); });
        else actingAs(other, function () { c.handlers.premioPartita(Object.assign({ partita: tk.altro.partita }, decl(!args.vinta, 1))); });
    }
    return c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(!!args.vinta, 0), args));
}

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
c.currentPlayerId = "G1"; // ospite (niente Username): niente Posta per tutti
assert.strictEqual(c.handlers.inizio().postaNuova, 0);
c.currentPlayerId = "P1";
// Un messaggio senza data (scritto a mano) si riscatta e resta, come lo mostra il telefono.
c.store.readOnly.Posta = JSON.stringify([{ id: "mano", allegati: [{ tipo: "monete", quantita: 5 }] }]);
assert.strictEqual(c.handlers.riscattaPosta({ id: "mano" }).ok, true);

// Monete di fine partita: 40/20, meta' coi bot, tetto 400 al giorno, si riparte il giorno dopo.
c = load(t0);
assert.strictEqual(claim({ vinta: true }).monete, 40);
assert.strictEqual(claim({ vinta: false }).monete, 20);
assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 20);
for (var i = 0; i < 20; i++) claim({ vinta: true });
assert.strictEqual(c.store.wallet.CO, 400);
assert.strictEqual(claim({ vinta: true }).tetto, true);
c.setNow(t0 + DAY);
assert.strictEqual(claim({ vinta: true }).monete, 40);

// Statistiche e XP li scrive il server (come PlayerXp): 40/20 + 2 per scopa + 5 per accuso (bonus max 20), meta' coi bot.
assert.deepStrictEqual([99, 100, 219, 220, 1619, 1620].map(m.levelOf), [1, 2, 2, 3, 9, 10]);
assert.strictEqual(m.matchXp(true, 3, 1, false), 51);
assert.strictEqual(m.matchXp(false, 50, 50, true), 20);
c = load(t0);
r = claim({ vinta: true, scope: 3, accusi: 1 });
assert.strictEqual(r.xp, 51);
assert.deepStrictEqual(c.store.stats, { TotalGames: 1, Wins: 1, XP: 51, Level: 1, TotalScope: 3, XPSettimana: 51, XPConcordato: 51 });
assert.strictEqual(r.esperienza, 51); assert.strictEqual(r.partite, 1); assert.strictEqual(r.vittorie, 1);
claim({ vinta: false, scope: 9999, allenamento: true });
assert.strictEqual(c.store.stats.TotalScope, 33, "scope inventate: al massimo 30 a partita");
assert.strictEqual(c.store.stats.XP, 51 + 20);
r = claim({ vinta: true, uscita: true, scope: 5 });
assert.strictEqual(r.monete, 0); assert.strictEqual(r.xp, 0);
assert.deepStrictEqual(c.store.stats, { TotalGames: 3, Wins: 1, XP: 71, Level: 1, TotalScope: 33, XPSettimana: 71, XPConcordato: 51 }, "uscita: persa, niente XP; allenamento fuori da XPConcordato (D6)");
delete c.store.stats.XPSettimana; // azzeramento settimanale di PlayFab: la statistica torna vuota
claim({ vinta: false });
assert.strictEqual(c.store.stats.XPSettimana, 20, "la settimana riparte da 0");
assert.strictEqual(c.store.stats.XP, 91);
c.store.stats.XP = 1600;
assert.strictEqual(claim({ vinta: true }).livello, 10);
for (i = 0; i < 60; i++) r = claim({ vinta: true });
assert.strictEqual(r.limitePartite, true, "al massimo 60 risultati al giorno");
assert.strictEqual(c.store.stats.TotalGames, 60);
c.currentPlayerId = "G1";
assert.strictEqual(claim({ vinta: true }).ospite, true);
assert.strictEqual(c.of("G1", "stats").TotalGames, undefined, "ospite: niente");
// Solo il telefono, per se stesso: dall'URL dei webhook (UserId) o senza giocatore non si fa niente.
c.currentPlayerId = "P1";
assert.strictEqual(claim({ vinta: true, UserId: "P1" }).ok, false);
assert.strictEqual(c.handlers.abbandono({ UserId: "P7" }).ok, false);
c.currentPlayerId = undefined;
assert.strictEqual(c.handlers.riscattaPremio({}).ok, false);
c.currentPlayerId = "P1";

// Consegna sicura (09/10). Accredito rifiutato da PlayFab (valuta che non c'e': il caso C0/CO del tablet): il premio resta consumato,
// la consegna resta in attesa e arriva una volta sola appena PlayFab accetta.
function cons(c) {
    return Object.keys(c.store.readOnly).filter(function (k) { return k.indexOf("Cons_") === 0; }).map(function (k) { return JSON.parse(c.store.readOnly[k]); });
}
c = load(t0);
c.store.refuse = { CO: true };
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, false); assert.strictEqual(r.inConsegna, true); assert.strictEqual(r.riscattato, true);
assert.ok(/InvalidVirtualCurrency/.test(r.errore));
assert.strictEqual(c.handlers.statoPremi().riscattato, true, "non torna riscattabile");
for (i = 0; i < 5; i++) assert.strictEqual(c.handlers.riscattaPremio().gia, true, "spam: niente di nuovo");
assert.strictEqual(c.store.wallet.CO, 0);
assert.strictEqual(cons(c).length, 1, "una consegna sola");
assert.strictEqual(cons(c)[0].stato, "attesa");
c.store.refuse = null;
r = c.handlers.inizio();
assert.strictEqual(r.recuperoMonete, 50); assert.strictEqual(r.saldoMonete, 50); assert.strictEqual(c.store.wallet.CO, 50);
assert.strictEqual(cons(c).length, 0, "registro vuoto");
assert.strictEqual(c.handlers.inizio().recuperoMonete, 0, "recuperato una volta sola");
assert.strictEqual(c.store.wallet.CO, 50);
assert.strictEqual(Object.keys(c.store.groups).length, 0, "lucchetti restituiti");
// Tutorial e Posta rifiutati: consumati con la consegna in attesa; il tentativo dopo paga una volta.
c.store.refuse = { CO: true };
r = c.handlers.premioTutorial();
assert.strictEqual(r.ok, false); assert.strictEqual(r.inConsegna, true);
assert.strictEqual(c.handlers.premioTutorial().gia, true, "il tutorial non si riscatta due volte");
c.store.readOnly.Posta = JSON.stringify([{ id: "y", titolo: "Y", data: "2026-09-30", allegati: [{ tipo: "monete", quantita: 20 }] }]);
r = c.handlers.riscattaPosta({ id: "y" });
assert.strictEqual(r.ok, false); assert.strictEqual(r.inConsegna, true); assert.strictEqual(JSON.stringify(r.riscattati), "[\"y\"]");
assert.strictEqual(c.handlers.riscattaPosta({ id: "y" }).gia, true);
c.store.refuse = null;
// Il "gia'" ritenta il registro, una consegna vecchia per chiamata (MAX_RETRY), prima la meno rifiutata: la Posta (il tutorial e'
// stato ritentato e rifiutato anche dal "gia'" della Posta sopra).
r = c.handlers.riscattaPosta({ id: "y" });
assert.strictEqual(r.gia, true); assert.strictEqual(r.recuperoMonete, 20); assert.strictEqual(c.store.wallet.CO, 70);
r = c.handlers.premioTutorial();
assert.strictEqual(r.gia, true); assert.strictEqual(r.recuperoMonete, 200); assert.strictEqual(c.store.wallet.CO, 270);
assert.strictEqual(c.handlers.premioTutorial().gia, true); assert.strictEqual(c.store.wallet.CO, 270, "pagati una volta");
// Partita: le monete restano in consegna e arrivano dopo, il risultato conta comunque.
c.store.refuse = { CO: true };
r = claim({ vinta: true });
assert.strictEqual(r.ok, true); assert.strictEqual(r.monete, 0); assert.strictEqual(r.inConsegna, true); assert.strictEqual(r.statistiche, true);
c.store.refuse = null;
assert.strictEqual(c.handlers.inizio().recuperoMonete, 40); assert.strictEqual(c.store.wallet.CO, 310);

// Errore permanente della valuta: ogni chiamata ritenta, niente arriva; quando la valuta c'e', arriva una volta.
c = load(t0);
c.store.refuse = { CO: true };
c.handlers.riscattaPremio();
for (i = 0; i < 5; i++) c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 0); assert.strictEqual(cons(c)[0].tentativi, 6); assert.strictEqual(cons(c)[0].stato, "attesa");
c.store.refuse = null;
assert.strictEqual(c.handlers.inizio().recuperoMonete, 50);
c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 50, "una volta");
// Audit 09/10: una consegna rifiutata sempre non blocca le altre (prima: MAX_RETRY la riprendeva per prima a ogni chiamata, per
// sempre) e dopo MAX_TENTATIVI rifiuti si ferma ("fermo": non accreditata, si rimette in attesa a mano).
c = load(t0);
c.store.readOnly.Cons_a = JSON.stringify({ id: "a", CO: 0, GE: 5, stato: "attesa" });
c.store.readOnly.Cons_b = JSON.stringify({ id: "b", CO: 40, GE: 0, stato: "attesa" });
c.store.refuse = { GE: true };
for (i = 0; i < 15; i++) c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 40, "la consegna buona arriva");
assert.strictEqual(c.store.readOnly.Cons_b, undefined);
var fermo = JSON.parse(c.store.readOnly.Cons_a);
assert.strictEqual(fermo.stato, "fermo"); assert.strictEqual(fermo.tentativi, 10); assert.strictEqual(c.store.wallet.GE, 0);
c.store.refuse = null;
c.handlers.inizio();
assert.strictEqual(c.store.wallet.GE, 0, "ferma: non si ritenta da sola");

// Risposta persa dopo l'accredito: "incerto", mai ritentato da solo (il saldo non prova quale accredito e' arrivato).
c = load(t0);
c.store.lost = true;
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, false); assert.strictEqual(r.inConsegna, true); assert.strictEqual(r.incerto, true); assert.ok(/timeout/.test(r.errore));
c.store.lost = false;
r = c.handlers.riscattaPremio();
assert.strictEqual(r.gia, true); assert.strictEqual(r.recuperoMonete, 0);
c.handlers.inizio(); c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 50, "arrivato una volta, mai ripagato");
assert.strictEqual(cons(c)[0].stato, "incerto"); assert.strictEqual(cons(c)[0].dubbio, "CO");
// Nessuna risposta e niente accredito (script fermato durante la chiamata): incerto anche lui, non pagato da solo.
c.setNow(t0 + DAY);
c.store.abort = true;
r = c.handlers.riscattaPremio();
assert.strictEqual(r.incerto, true); assert.strictEqual(c.store.wallet.CO, 50);
c.store.abort = false;
c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 50); assert.strictEqual(cons(c).length, 2);
// Script morto tra la scrittura della consegna e l'esito (PlayFab lo ferma): resta "inCorso", la chiamata dopo la segna incerta.
c = load(t0);
c.store.readOnly.Cons_x = JSON.stringify({ id: "x", CO: 30, GE: 4, stato: "inCorso" });
c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 0); assert.strictEqual(cons(c)[0].stato, "incerto"); assert.strictEqual(cons(c)[0].dubbio, "CO,GE");

// Lucchetto: tenuto da un'altra chiamata (o lasciato da uno script morto) niente consumo ne' accredito; scade da solo.
c = load(t0); // t0 = inizio di un minuto: si prendono il minuto e quello prima
c.store.groups["consegne_P1_" + Math.floor(t0 / 60000)] = {};
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, false); assert.strictEqual(r.occupato, true);
assert.strictEqual(c.handlers.statoPremi().riscattato, false, "niente consumato");
r = c.handlers.inizio();
assert.strictEqual(r.ok, true); assert.strictEqual(r.occupato, true); assert.strictEqual(r.giorno, 1);
// Fine partita e biglietto sotto lo stesso lucchetto: occupato = niente consumato, il telefono tiene il risultato e ritenta.
assert.strictEqual(c.handlers.inizioPartita({}).occupato, true);
r = c.handlers.premioPartita({ vinta: true, partita: "p1" });
assert.strictEqual(r.ok, false); assert.strictEqual(r.occupato, true);
assert.strictEqual(cons(c).length, 0); assert.strictEqual(c.store.stats.TotalGames, undefined, "niente statistiche");
c.store.groups["consegne_P1_" + (Math.floor(t0 / 60000) - 1)] = {}; // anche il minuto prima
c.setNow(t0 + 10000); // 10 s dopo: guardia ancora attiva
assert.strictEqual(c.handlers.riscattaPremio().occupato, true);
c.setNow(t0 + 80000); // minuto dopo, oltre i 15 s di guardia: i lucchetti vecchi non contano piu'
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, true); assert.strictEqual(r.monete, 50);
assert.strictEqual(c.store.wallet.CO, 50);
assert.strictEqual(Object.keys(c.store.groups).length, 2, "restano solo i due lasciati");

// Tutorial segnato dagli script fino all'08/10 (dati interni): mai pagato da solo, consegna "incerto" da riconciliare.
c = load(t0);
c.store.internal.Tutorial = "2026-10-07";
r = c.handlers.premioTutorial();
assert.strictEqual(r.gia, true); assert.strictEqual(c.store.wallet.CO, 0);
assert.strictEqual(cons(c).length, 1); assert.strictEqual(cons(c)[0].stato, "incerto"); assert.strictEqual(cons(c)[0].CO, 200);
assert.strictEqual(c.handlers.premioTutorial().gia, true); assert.strictEqual(cons(c).length, 1, "deciso una volta");
c.handlers.inizio();
assert.strictEqual(c.store.wallet.CO, 0);
c = load(t0);
c.store.internal.Tutorial = "2026-10-07"; c.store.wallet.CO = 150; // il saldo non decide piu' niente
assert.strictEqual(c.handlers.premioTutorial().gia, true); assert.strictEqual(c.store.wallet.CO, 150);

// Registro della revisione del terzo giro (lista "Consegne"): un no certo torna in attesa e si paga, il resto e' incerto.
c = load(t0);
c.store.readOnly.Consegne = JSON.stringify([
    { id: "premio 2026-09-30", t: "2026-09-30T10:00:00Z", CO: 50, GE: 0, prima: { CO: 0, GE: 0 }, rifiutato: { CO: true } },
    { id: "partita 2026-09-30", t: "2026-09-30T11:00:00Z", CO: 40, GE: 0, prima: { CO: 0, GE: 0 }, rifiutato: {} }]);
r = c.handlers.inizio();
assert.strictEqual(c.store.readOnly.Consegne, undefined);
assert.strictEqual(cons(c).length, 2);
assert.strictEqual(c.handlers.inizio().recuperoMonete, 50); assert.strictEqual(c.store.wallet.CO, 50);
assert.strictEqual(cons(c).length, 1); assert.strictEqual(cons(c)[0].stato, "incerto"); assert.strictEqual(cons(c)[0].CO, 40);

// Ogni risposta porta il saldo vero; secondo tocco o nuovo tentativo dopo un riscatto: "gia'", niente di nuovo.
c = load(t0);
c.store.wallet.CO = 1000; c.store.wallet.GE = 7;
r = c.handlers.riscattaPremio();
assert.ok(r.ok); assert.strictEqual(r.monete, 50); assert.strictEqual(r.saldoMonete, 1050); assert.strictEqual(r.saldoGemme, 7, "anche la valuta non toccata");
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, false); assert.strictEqual(r.gia, true); assert.strictEqual(r.riscattato, true);
assert.strictEqual(r.saldoMonete, 1050, "il no porta il saldo vero");
assert.strictEqual(c.handlers.statoPremi().riscattato, true, "rientro in Home: resta riscattato");
assert.strictEqual(c.handlers.premioTutorial().saldoMonete, 1250);
r = c.handlers.premioTutorial();
assert.strictEqual(r.gia, true); assert.strictEqual(r.saldoMonete, 1250); assert.strictEqual(c.store.wallet.CO, 1250, "tutorial una volta");
c.store.readOnly.Posta = JSON.stringify([{ id: "g", titolo: "G", data: "2026-09-30", allegati: [{ tipo: "gemme", quantita: 10 }] }]);
r = c.handlers.riscattaPosta({ id: "g" });
assert.ok(r.ok); assert.strictEqual(r.saldoGemme, 17); assert.strictEqual(r.saldoMonete, 1250);
r = c.handlers.riscattaPosta({ id: "g" });
assert.strictEqual(r.ok, false); assert.strictEqual(r.gia, true); assert.strictEqual(JSON.stringify(r.riscattati), "[\"g\"]");
assert.strictEqual(r.saldoGemme, 17); assert.strictEqual(c.store.wallet.GE, 17, "posta: secondo tocco senza gemme");
assert.strictEqual(c.handlers.riscattaTuttaPosta().ok, false);
assert.strictEqual(c.store.wallet.GE, 17);

// QA: guasti finti solo sul titolo di sviluppo (QA = true, lo mette qa.js deploy) e per l'account che li ha nei dati interni.
c = load(t0);
c.store.internal.QAGuasto = "rifiuto";
assert.strictEqual(c.handlers.riscattaPremio().ok, true, "titolo dell'app (QA = false): il guasto non conta");
c.setNow(t0 + DAY);
vm.runInNewContext("QA = true", c);
r = c.handlers.riscattaPremio();
assert.strictEqual(r.inConsegna, true); assert.ok(/QARifiuto/.test(r.errore));
delete c.store.internal.QAGuasto;
assert.strictEqual(c.handlers.inizio().recuperoMonete, 100); assert.strictEqual(c.store.wallet.CO, 150);
c.store.internal.QAGuasto = "incerto";
c.setNow(t0 + 2 * DAY);
r = c.handlers.riscattaPremio();
assert.strictEqual(r.ok, false); assert.strictEqual(r.incerto, true);
delete c.store.internal.QAGuasto;
c.handlers.inizio();
assert.strictEqual(c.store.wallet.GE, 5, "incerto: arrivato una volta, non ripagato");

// Giro Android 08/10 (#10): amicizia reciproca. A chiede, B vede la richiesta, accetta: amici su entrambi gli elenchi.
c = load(t0);
function tags(owner, other) { var m = c.store.friends[owner] || {}; return m[other] ? m[other].join(",") : null; }
function asP(id) { c.currentPlayerId = id; vm.runInNewContext("0", c); }
asP("PA");
assert.strictEqual(c.handlers.richiestaAmico({ id: "PB" }).stato, "inviata");
assert.strictEqual(tags("PA", "PB"), "inviata"); assert.strictEqual(tags("PB", "PA"), "ricevuta", "la richiesta arriva a B");
assert.strictEqual(c.handlers.richiestaAmico({ id: "PB" }).stato, "inviata", "di nuovo: niente doppioni");
assert.strictEqual(Object.keys(c.store.friends.PB).length, 1);
assert.strictEqual(c.handlers.accettaAmico({ id: "PB" }).ok, false, "A non puo' accettare la propria richiesta");
asP("PB");
var listB = c.handlers.amici().amici;
assert.strictEqual(listB.length, 1); assert.strictEqual(listB[0].stato, "ricevuta"); assert.strictEqual(listB[0].id, "PA");
assert.strictEqual(c.handlers.accettaAmico({ id: "PA" }).stato, "amico");
assert.strictEqual(tags("PA", "PB"), "amico"); assert.strictEqual(tags("PB", "PA"), "amico");
// Telefono modificato: C si mette da solo "ricevuta" da A nel proprio elenco; accetta non passa (A non ha chiesto niente).
c.store.friends.PC = { PA: ["ricevuta"] };
asP("PC");
assert.strictEqual(c.handlers.accettaAmico({ id: "PA" }).ok, false);
assert.strictEqual(tags("PA", "PC"), null);
// Richieste incrociate: B chiede a C mentre C aveva chiesto a B -> amici subito.
c.store.friends.PC = {};
c.handlers.richiestaAmico({ id: "PB" });
asP("PB");
assert.strictEqual(c.handlers.richiestaAmico({ id: "PC" }).stato, "amico");
assert.strictEqual(tags("PC", "PB"), "amico");
// Rimuovi: via da entrambi gli elenchi. Ospiti: niente.
c.handlers.rimuoviAmico({ id: "PA" });
assert.strictEqual(tags("PA", "PB"), null); assert.strictEqual(tags("PB", "PA"), null);
assert.strictEqual(c.handlers.richiestaAmico({ id: "G9" }).errore, "ospite");
asP("G9"); assert.strictEqual(c.handlers.amici().ospite, true);
// Bloccato: la richiesta non arriva (senza dirlo).
c.store.userData = { PD: { Bloccati: "PX|PE" } };
asP("PE"); assert.strictEqual(c.handlers.richiestaAmico({ id: "PD" }).stato, "inviata");
assert.strictEqual(tags("PD", "PE"), null);
// Dati vecchi a senso unico: F aveva aggiunto G (G no) -> richiesta per G; H e F si erano aggiunti a vicenda -> amici. Avatar e livello.
c.store.friends.PF = { PG: [], PH: [] }; c.store.friends.PH = { PF: [] }; c.store.avatars = { PH: "avatar:av_3" };
asP("PF");
var listF = c.handlers.amici().amici, byId = {};
listF.forEach(function (f) { byId[f.id] = f; });
assert.strictEqual(byId.PG.stato, "inviata"); assert.strictEqual(tags("PG", "PF"), "ricevuta");
assert.strictEqual(byId.PH.stato, "amico"); assert.strictEqual(tags("PH", "PF"), "amico");
assert.strictEqual(byId.PH.avatar, "avatar:av_3"); assert.strictEqual(byId.PH.xp, 120);
assert.strictEqual(c.handlers.amici().amici.length, 2, "seconda lettura: niente doppioni");
c.currentPlayerId = "P1";

// B32: premio del tutorial una volta per account, mai agli ospiti.
c = load(t0);
var coins = c.store.wallet.CO;
assert.strictEqual(c.handlers.premioTutorial().monete, 200);
assert.strictEqual(c.store.wallet.CO, coins + 200);
assert.strictEqual(c.handlers.premioTutorial().gia, true, "una volta sola");
assert.strictEqual(c.store.wallet.CO, coins + 200);
c.currentPlayerId = "G1";
assert.strictEqual(c.handlers.premioTutorial().ospite, true);
// 26c: niente premio giornaliero ne' Posta agli ospiti, nemmeno un messaggio scritto a mano nei loro dati.
coins = c.store.wallet.CO;
assert.strictEqual(c.handlers.riscattaPremio().ospite, true);
c.of("G1", "readOnly").Posta = JSON.stringify([{ id: "mano", allegati: [{ tipo: "monete", quantita: 5 }] }]);
assert.strictEqual(c.handlers.riscattaPosta({ id: "mano" }).ospite, true);
assert.strictEqual(c.handlers.riscattaTuttaPosta().ospite, true);
assert.strictEqual(c.store.wallet.CO, coins, "ospite: saldo fermo");
c.currentPlayerId = "P1";

// Vittoria per abbandono (#143, Fase A): l'avversario aveva chiesto il biglietto (era presente all'inizio) ed e' uscito per sempre senza
// dichiarare: lo dice Photon (PathLeave), non il telefono. Monete al massimo 3 volte al giorno e una per avversario; oltre la vittoria
// resta senza monete ne' XP.
c = load(t0);
function forfeit(who) { return claim({ vinta: true, abbandono: true, altro: who }); }
r = forfeit("A");
assert.strictEqual(r.monete, 40); assert.strictEqual(r.stato, "abbandono"); assert.strictEqual(r.concordata, false, "abbandono: fuori da XPConcordato");
var again = forfeit("A");
assert.strictEqual(again.monete, 0, "stesso avversario, stesso giorno");
assert.strictEqual(again.limiteAbbandoni, true);
assert.strictEqual(again.tetto, false);
assert.strictEqual(forfeit("C").monete, 40);
assert.strictEqual(forfeit("D").monete, 40);
assert.strictEqual(forfeit("B").limiteAbbandoni, true, "quarta vittoria per abbandono");
assert.strictEqual(claim({ vinta: true }).monete, 40, "le partite finite restano premiate");
assert.strictEqual(claim({ vinta: false, abbandono: true, altro: "B2" }).monete, 20, "persa dopo l'uscita dell'altro: una sconfitta normale");
c.setNow(t0 + DAY);
// La stessa partita paga una volta sola.
var tk = ticket({ altro: "E" });
c.setNow(c.now + 61 * 1000);
actingAs("", function () { c.handlers.RoomLeft({ UserId: "E", GameId: tk.stanza, ActorNr: 2 }); });
var co = c.store.wallet.CO;
assert.strictEqual(c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))).monete, 40);
r = c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0)));
assert.strictEqual(r.gia, true, "la stessa partita paga una volta sola"); assert.strictEqual(r.monete, 40, "risponde quanto aveva dato");
assert.strictEqual(c.store.wallet.CO, co + 40);
// Il telefono dice "abbandono" ma l'avversario e' ancora seduto e non ha dichiarato: niente vincitore, si aspetta (in verifica).
tk = ticket({ altro: "H" });
c.setNow(c.now + 61 * 1000);
r = c.handlers.premioPartita(Object.assign({ partita: tk.partita, abbandono: true, attore: 2 }, decl(true, 0)));
assert.strictEqual(r.stato, "inVerifica"); assert.strictEqual(r.monete, 0);
// Ingresso fittizio (#143): un secondo account entra e esce prima dell'inizio senza biglietto: nessuna persona, meta' premio.
var fake = onlineRoom("FINTO");
actingAs("", function () { c.handlers.RoomLeft({ UserId: "FINTO", GameId: fake, ActorNr: 2 }); });
r = claim({ vinta: true, abbandono: true, stanza: fake, attore: 2 });
assert.strictEqual(r.monete, 20); assert.strictEqual(r.allenamento, true, "ingresso fittizio: come l'allenamento");
// Una disconnessione con rientro possibile (IsInactive) non e' un'uscita.
tk = ticket({ altro: "I" });
c.setNow(c.now + 61 * 1000);
actingAs("", function () { c.handlers.RoomLeft({ UserId: "I", GameId: tk.stanza, ActorNr: 2, IsInactive: true }); });
assert.strictEqual(c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))).stato, "inVerifica", "disconnesso: non e' uscito");
// Stanza di altri in cui non siede: allenamento.
var other = "ALTRUI";
actingAs("", function () { c.handlers.RoomCreated({ UserId: "Q1", Type: "Create", GameId: other }); c.handlers.RoomJoined({ UserId: "G", GameId: other, ActorNr: 2 }); });
assert.strictEqual(claim({ vinta: true, abbandono: true, stanza: other, attore: 2 }).allenamento, true, "chi chiede non sedeva in quella partita");
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

// Giro Android 08/10 (#12): un account segnala un ospite: la segnalazione va sull'account PlayFab della sua sessione, che il suo
// telefono legge con "moderazione" (DeviceModeration la tiene sul dispositivo).
c = load(t0);
c.currentPlayerId = "";
c.handlers.RoomCreated({ UserId: "G5", Type: "Create", GameId: "OS" });
c.handlers.RoomJoined({ UserId: "P2", GameId: "OS", ActorNr: 2 });
as("P2");
assert.ok(c.handlers.segnala({ id: "G5", modo: "1 vs 1", stanza: "OS", motivo: "gioco" }).ok);
assert.strictEqual(JSON.parse(c.of("G5", "internal").SegnalazioniPartite)[0].da, "P2", "segnalazione salvata sull'ospite");

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
// Una sola sessione (secondo giro 08/10): vince quella gia' attiva. A entra e batte; B col suo login viene fermato e A resta dentro;
// senza "sessione" (versioni vecchie) niente controllo.
r = c.handlers.sessione({ sessione: "S1", dispositivo: "A", nuova: true });
assert.strictEqual(r.occupato, false); assert.strictEqual(r.altrove, false);
c.setNow(t0 + 30000);
assert.strictEqual(c.handlers.moderazione({ sessione: "S1", stato: true }).altrove, false, "battito di A");
r = c.handlers.sessione({ sessione: "S2", dispositivo: "B", nuova: true });
assert.strictEqual(r.occupato, true, "B non butta fuori A"); assert.strictEqual(r.secondi, 90);
assert.strictEqual(c.handlers.moderazione({ sessione: "S2", dispositivo: "B", nuova: true }).occupato, true, "anche dalla moderazione");
c.setNow(t0 + 60000);
assert.strictEqual(c.handlers.sessione({ sessione: "S1", dispositivo: "A" }).altrove, false, "A resta dentro (anche al tavolo)");
// A sparisce (crash, rete persa): dopo 90 s senza battiti B entra; A, se torna, e' "altrove".
c.setNow(t0 + 60000 + 89000);
assert.strictEqual(c.handlers.sessione({ sessione: "S2", dispositivo: "B", nuova: true }).occupato, true, "89 s: ancora di A");
c.setNow(t0 + 60000 + 91000);
assert.strictEqual(c.handlers.sessione({ sessione: "S2", dispositivo: "B", nuova: true }).occupato, false, "affitto scaduto: niente blocco eterno");
assert.strictEqual(c.handlers.sessione({ sessione: "S1", dispositivo: "A" }).altrove, true, "A torna: esce lui");
// Lo stesso dispositivo dopo un crash rientra subito (login nuovo, affitto ancora vivo).
assert.strictEqual(c.handlers.sessione({ sessione: "S3", dispositivo: "B", nuova: true }).occupato, false, "riavvio sullo stesso telefono");
assert.strictEqual(c.handlers.sessione({ sessione: "S2", dispositivo: "B" }).altrove, true);
// Esci libera subito l'account; la fine mandata da un altro non tocca l'affitto.
c.handlers.sessione({ sessione: "S1", dispositivo: "A", fine: true });
assert.strictEqual(c.handlers.sessione({ sessione: "S4", dispositivo: "C", nuova: true }).occupato, true, "fine di un altro: niente");
c.handlers.sessione({ sessione: "S3", dispositivo: "B", fine: true });
assert.strictEqual(c.handlers.sessione({ sessione: "S4", dispositivo: "C", nuova: true }).occupato, false, "dopo Esci si entra subito");
// Formato vecchio (solo l'id): scaduto.
c.store.internal.Sessione = "VECCHIA";
assert.strictEqual(c.handlers.sessione({ sessione: "S5", dispositivo: "D", nuova: true }).occupato, false);
assert.strictEqual(c.handlers.moderazione().altrove, false);
assert.strictEqual(c.handlers.moderazione().occupato, false);
c.setNow(t0);

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
// Monete: l'abbandono di un sospeso (biglietto chiesto, poi uscito per sempre) paga chi vince; gruppi di una revisione vecchia
// ("stanza_") o fatti da un telefono (Client API, con membri) non sono prove: come l'allenamento.
c.currentPlayerId = "P1";
tk = c.handlers.inizioPartita({ stanza: "R1" });
actingAs("S1", function () { c.handlers.inizioPartita({ stanza: "R1" }); });
c.setNow(c.now + 61 * 1000);
actingAs("", function () { c.handlers.RoomLeft(hook({ UserId: "S1", GameId: "R1", ActorNr: 3 })); });
assert.strictEqual(c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))).monete, 40, "abbandono di un sospeso");
c.store.groups.stanza_R0 = { Partita: JSON.stringify({ id: "vecchio" }), g1: "P1", g2: "P7", b1: "1", b2: "1", u2: "1" };
c.store.groups.partita_FINTA = { Partita: JSON.stringify({ id: "x" }), g1: "P1", g2: "P7", b1: "1", b2: "1", u2: "1" };
c.store.groupMembers = { partita_FINTA: ["P1"] };
assert.strictEqual(claim({ vinta: true, abbandono: true, stanza: "R0", attore: 2 }).allenamento, true, "gruppo di una revisione vecchia");
assert.strictEqual(claim({ vinta: true, abbandono: true, stanza: "FINTA", attore: 2 }).allenamento, true, "gruppo fatto da un telefono");
// Sospeso: niente per le partite online; allenamento coi bot e uscite contano come per tutti.
c.currentPlayerId = "S1";
r = claim({ vinta: true });
assert.strictEqual(r.sospeso, true); assert.strictEqual(r.monete, 0);
assert.strictEqual(c.of("S1", "stats").TotalGames, undefined, "sospeso: niente statistiche online");
assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 20);
assert.strictEqual(c.of("S1", "stats").TotalGames, 1, "sospeso: l'allenamento conta");
claim({ uscita: true });
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
c.setNow(t0 + 3 * H);
// Vittoria per abbandono: l'avversario lo dice il record della partita, non il telefono.
c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: "R9" });
c.handlers.RoomJoined({ UserId: "P7", GameId: "R9", ActorNr: 2 });
c.currentPlayerId = "P1";
tk = c.handlers.inizioPartita({ stanza: "R9" });
actingAs("P7", function () { c.handlers.inizioPartita({ stanza: "R9" }); });
c.setNow(c.now + 61 * 1000);
actingAs("", function () { c.handlers.RoomLeft({ UserId: "P7", GameId: "R9", ActorNr: 2 }); });
c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0)));
c.setNow(t0);
assert.ok(JSON.parse(c.store.readOnly.PartiteOggi).abbandoni.indexOf("P7") >= 0, "id dal record anche se il telefono non lo sa");
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
assert.strictEqual(c.store.groups.partita_Z0, undefined, "record orfano senza biglietti tolto all'avvio");
assert.ok(c.store.groups.partita_R9, "record con biglietti: resta per il riesame");
assert.strictEqual(c.store.groups.partita_R10.g1, "Q2", "codice riusato: il record nuovo resta");
c.setNow(t0 + 9 * DAY);
for (i = 0; i < 2; i++) c.handlers.inizio({});
assert.strictEqual(c.store.groups.partita_R9, undefined, "dopo KEEP_MS lo toglie anche con biglietti");
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
c.store.groups.partita_T9 = { Partita: JSON.stringify({ id: "vecchio", creata: t0 }), g1: "P1", g2: "P7", b1: "1", b2: "1", u2: "1" };
c.currentPlayerId = "P1";
assert.strictEqual(claim({ vinta: true, abbandono: true, stanza: "T9", attore: 2 }).allenamento, true, "gruppo del segreto vecchio: nessuna prova");

// #141, #143, #148-#150 e Fase A (decisioni D1-D7 dell'08/10): biglietti, presenza, risultati verificati, senza biglietto, app vecchie.
// Il portafoglio del finto PlayFab e' uno solo per tutti i giocatori: le prove guardano le differenze.
c = load(t0);
var MIN = 61 * 1000;
function tick(id, room, extra) { return actingAs(id, function () { return c.handlers.inizioPartita(Object.assign(room ? { stanza: room } : {}, extra || {})); }); }
function declare(id, tk, won, posto, extra) {
    return actingAs(id, function () { return c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(won, posto), extra || {})); });
}
function later(ms) { c.setNow(c.now + ms); }
function internalOf(id, k) { var v = c.of(id, "internal")[k]; return v ? JSON.parse(v) : null; }

// Partita vera fra due persone: il primo che dichiara aspetta (niente prima della conferma), poi entrambi pagati una volta sola.
var room = onlineRoom("P2");
var a1 = tick("P1", room), b1 = tick("P2", room);
assert.strictEqual(a1.online, false, "l'altro non ha ancora il biglietto"); assert.strictEqual(b1.online, true);
later(30 * 1000);
assert.strictEqual(tick("P1", room).partita, a1.partita, "rientro: stesso biglietto");
assert.strictEqual(declare("P1", a1, true, 0).stato, "troppoCorta", "partita di 30 s");
later(31 * 1000);
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "inVerifica", "il primo aspetta l'altro"); assert.strictEqual(r.monete, 0);
assert.strictEqual(c.store.stats.TotalGames, undefined, "niente statistiche prima della conferma");
r = declare("P2", b1, false, 1);
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.monete, 20); assert.strictEqual(r.concordata, true);
r = declare("P1", a1, false, 0, { scope: 9 }); // un tentativo dopo non cambia la dichiarazione gia' scritta
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.monete, 40); assert.strictEqual(r.xp, 40);
assert.deepStrictEqual([c.store.stats.TotalGames, c.store.stats.Wins, c.store.stats.XPConcordato], [1, 1, 40]);
for (i = 0; i < 3; i++) { r = declare("P1", a1, true, 0); assert.strictEqual(r.gia, true); assert.strictEqual(r.monete, 40); }
assert.strictEqual(c.store.wallet.CO, 60, "40 a P1 e 20 a P2, una volta sola");
assert.strictEqual(actingAs("P1", function () { return c.handlers.premioPartita({ uscita: true, partita: a1.partita }); }).gia, true,
    "un'uscita dopo non conta una persa in piu'");
// Partita inventata: id inventato o di un altro giocatore.
assert.strictEqual(declare("P1", { partita: "pinventato" }, true, 0).stato, "nonValida", "id inventato");
assert.strictEqual(declare("P1", b1, true, 0).stato, "nonValida", "biglietto di un altro");
// Rivincita nella stessa stanza: biglietto nuovo (quello vecchio ha il risultato); mai lo stesso per due partite.
later(MIN);
var a2 = tick("P1", room, { dopo: [a1.partita] });
assert.notStrictEqual(a2.partita, a1.partita, "rivincita: biglietto nuovo");
assert.strictEqual(tick("P1", room).partita, a2.partita, "e poi sempre quello");
// Biglietti in parallelo: al massimo uno nuovo al minuto.
assert.ok(tick("P1", onlineRoom("P2")).attendi > 0, "secondo biglietto nello stesso minuto");

// Risultati discordanti: nessun vincitore; solo la partecipazione (prove del server), niente XP ne' statistiche; registro e PlayStream;
// nessuna sanzione.
c = load(t0);
room = onlineRoom("P3");
a1 = tick("P1", room); b1 = tick("P3", room);
later(MIN);
assert.strictEqual(declare("P1", a1, true, 0).stato, "inVerifica");
r = declare("P3", b1, true, 1); // anche P3 dice di aver vinto
assert.strictEqual(r.stato, "contestata"); assert.strictEqual(r.monete, 10, "partecipazione"); assert.strictEqual(r.xp, 0);
assert.strictEqual(r.categoria, "sospetta");
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "contestata"); assert.strictEqual(r.monete, 10);
assert.strictEqual(declare("P1", a1, true, 0).monete, 0, "la partecipazione si paga una volta");
assert.strictEqual(c.store.wallet.CO, 20);
assert.strictEqual(c.store.stats.TotalGames, undefined, "contestata: nessun risultato in classifica");
assert.strictEqual(c.of("P3", "stats").TotalGames, undefined);
assert.strictEqual(internalOf("P1", "Incongruenze")[0].stato, "contestata");
assert.strictEqual(internalOf("P1", "Incongruenze")[0].dichiarazioni.length, 2);
assert.ok(c.store.events.some(function (e) { return e.nome === "partita_incongruenza" && e.id === "P3"; }), "evento PlayStream");
assert.strictEqual(c.store.internal.Sospensione, undefined, "nessuna sospensione automatica");
assert.strictEqual(JSON.parse(c.store.readOnly.Partite).riesame[0].stato, "contestata", "resta da decidere (amministratore)");
// Versioni diverse e punteggi diversi: categoria "bug".
later(MIN);
room = onlineRoom("P3");
a1 = tick("P1", room); b1 = tick("P3", room);
later(MIN);
declare("P1", a1, true, 0);
assert.strictEqual(declare("P3", b1, false, 1, { versione: "2.63", smazzate: 5 }).categoria, "bug");

// Partite fittizie per la partecipazione: due account dello stesso giocatore che si contestano apposta, 80 volte in un giorno. Rende
// meno di una sconfitta concordata (10 contro 20 a testa) e resta dentro i limiti del giorno (60 risultati, tetto D11 dei premi non
// verificabili: 100 monete); niente XP
// ne' statistiche. La partecipazione non e' quindi una strada migliore dell'accordo fra account, che resta il limite noto.
c = load(t0);
var farmed = { P1: 0, P2: 0 }, contested = 0, limited = 0;
for (i = 0; i < 80; i++) {
    room = onlineRoom("P2");
    a1 = tick("P1", room); b1 = tick("P2", room);
    later(MIN);
    declare("P1", a1, true, 0);
    r = declare("P2", b1, true, 1); farmed.P2 += r.monete | 0;
    if (r.stato === "contestata") contested++; else if (r.stato === "limite") limited++;
    farmed.P1 += declare("P1", a1, true, 0).monete | 0;
}
assert.strictEqual(contested, 60, "al massimo 60 risultati al giorno"); assert.strictEqual(limited, 20);
assert.deepStrictEqual(farmed, { P1: 100, P2: 100 }, "tetto D11 di 100 monete al giorno (raggiunto alla decima)");
assert.deepStrictEqual(JSON.parse(c.of("P2", "readOnly").PartiteOggi).nonVerificabili, { partecipazione: 100 });
assert.strictEqual(c.of("P1", "stats").XP, undefined); assert.strictEqual(c.of("P2", "stats").TotalGames, undefined);

// Un giocatore disconnesso: in verifica fino all'attesa (10 minuti), poi incompleta con la sola partecipazione; quando arriva la sua
// dichiarazione (il suo telefono ritenta) diventa confermata e si paga solo la differenza.
c = load(t0);
room = onlineRoom("P4");
a1 = tick("P1", room); b1 = tick("P4", room);
later(MIN);
assert.strictEqual(declare("P1", a1, true, 0).stato, "inVerifica");
later(9 * 60 * 1000);
assert.strictEqual(declare("P1", a1, true, 0).stato, "inVerifica", "entro l'attesa");
later(2 * 60 * 1000);
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "incompleta"); assert.strictEqual(r.monete, 10); assert.strictEqual(r.categoria, "rete");
assert.strictEqual(c.store.stats.TotalGames, undefined, "incompleta: nessun vincitore");
later(3 * H);
assert.strictEqual(declare("P4", b1, false, 1).stato, "confermata", "dichiarazione arrivata dopo");
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.monete, 30, "40 meno la partecipazione"); assert.strictEqual(r.xp, 40);
assert.strictEqual(c.store.stats.Wins, 1);
// Chi non torna mai: dopo il riesame (7 giorni) l'incompleta si chiude con la partecipazione, registrata.
later(MIN);
room = onlineRoom("P5");
a1 = tick("P1", room); tick("P5", room);
later(MIN);
declare("P1", a1, true, 0);
later(11 * 60 * 1000);
assert.strictEqual(declare("P1", a1, true, 0).stato, "incompleta");
later(8 * DAY);
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "incompleta"); assert.strictEqual(r.monete, 0);
assert.strictEqual(declare("P1", a1, true, 0).gia, true, "chiusa: definitiva");
assert.strictEqual(JSON.parse(c.store.readOnly.Partite).chiuse.slice(-1)[0].s, "incompleta");

// Due account dello stesso giocatore. Entra e esce prima dell'inizio (senza biglietto): nessuna persona, meta' premio (prova sopra,
// "ingresso fittizio"). Chiede il biglietto e poi esce: abbandono, tetti degli abbandoni (prove sopra). Resta e dichiara la sconfitta:
// accordo fra account, confermata come una partita vera (limite noto: lo coprono tetti e registro, non una prova del server).
c = load(t0);
room = onlineRoom("ALT");
a1 = tick("P1", room); b1 = tick("ALT", room);
later(MIN);
declare("ALT", b1, false, 1);
assert.strictEqual(declare("P1", a1, true, 0).stato, "confermata", "accordo fra account: non distinguibile dal server");

// 2v2 con quattro persone (squadre: posti pari contro dispari) e 1v3 con due persone e due bot.
function decl4(won, posto, squadre) {
    return { vinta: won, punti: squadre ? [51, 30] : [51, 30, 20, 10], smazzate: 5, vincitore: 0, posto: posto, squadre: squadre, giocatori: 4 };
}
c = load(t0);
room = "Q4P";
actingAs("", function () {
    c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: room });
    ["T2", "T3", "T4"].forEach(function (u, k) { c.handlers.RoomJoined({ UserId: u, GameId: room, ActorNr: k + 2 }); });
});
var tks = ["P1", "T2", "T3", "T4"].map(function (u) { return tick(u, room); });
later(MIN);
["T2", "T3", "T4"].forEach(function (u, k) {
    var posto = k + 1;
    actingAs(u, function () { c.handlers.premioPartita(Object.assign({ partita: tks[k + 1].partita }, decl4(posto % 2 === 0, posto, true))); });
});
r = actingAs("P1", function () { return c.handlers.premioPartita(Object.assign({ partita: tks[0].partita }, decl4(true, 0, true))); });
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.concordata, true, "quattro persone, nessun bot");
actingAs("T3", function () { c.handlers.premioPartita(Object.assign({ partita: tks[2].partita }, decl4(true, 2, true))); }); // il suo tentativo dopo
assert.strictEqual(c.of("T3", "stats").Wins, 1, "il compagno di squadra ha vinto");
later(MIN);
room = onlineRoom("T2");
a1 = tick("P1", room); b1 = tick("T2", room);
later(MIN);
actingAs("T2", function () { c.handlers.premioPartita(Object.assign({ partita: b1.partita }, decl4(false, 1, false))); });
r = actingAs("P1", function () { return c.handlers.premioPartita(Object.assign({ partita: a1.partita }, decl4(true, 0, false))); });
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.concordata, false, "1v3 con due bot: fuori da XPConcordato (D6)");
assert.strictEqual(c.store.stats.XPConcordato, 40, "solo la partita senza bot");

// Gioco coi soli bot (partita rapida riempita dopo 30 s): meta', fuori da XPConcordato; il campo "allenamento" del telefono non conta.
c = load(t0);
room = "SOLO";
actingAs("", function () { c.handlers.RoomCreated({ UserId: "P1", Type: "Create", GameId: room }); });
a1 = tick("P1", room);
assert.strictEqual(a1.online, false);
later(MIN);
r = declare("P1", a1, true, 0, { allenamento: false });
assert.strictEqual(r.monete, 20); assert.strictEqual(r.xp, 20); assert.strictEqual(r.stato, "allenamento");
assert.strictEqual(c.store.stats.XPConcordato, 0);
// Sospeso: coi soli bot vale come allenamento; con altre persone niente (biglietto chiuso, non si incassa dopo la sospensione).
c.store.internal.Sospensione = JSON.stringify({ volte: 1, fine: new Date(c.now + 10 * DAY).toISOString(), motivo: "abbandoni" });
assert.strictEqual(claim({ vinta: true, stanza: room }).monete, 20, "sospeso, soli bot: come allenamento");
tk = ticket({});
later(MIN);
actingAs("QX", function () { c.handlers.premioPartita(Object.assign({ partita: tk.altro.partita }, decl(false, 1))); });
r = c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0)));
assert.strictEqual(r.sospeso, true);
delete c.store.internal.Sospensione;
assert.strictEqual(c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))).gia, true, "non si incassa dopo");

// Biglietti: niente scadenza a tempo (D1). Dieci aperti al massimo: l'undicesimo chiude il piu' vecchio come "scaduta" (partita mai
// finita); se il suo risultato arriva dopo resta registrato da riconciliare, mai pagato da solo ne' perso in silenzio.
c = load(t0);
var open = [];
for (i = 0; i < 11; i++) { open.push(tick("P1", null, { locale: "loc" + i })); later(MIN); }
var pt = JSON.parse(c.store.readOnly.Partite);
assert.strictEqual(pt.aperte.length, 10); assert.strictEqual(pt.chiuse[0].s, "scaduta");
r = c.handlers.premioPartita({ partita: open[0].partita, vinta: true });
assert.strictEqual(r.stato, "daRiconciliare"); assert.strictEqual(r.monete, 0);
assert.strictEqual(c.handlers.premioPartita({ partita: open[0].partita, vinta: true }).stato, "daRiconciliare", "e poi sempre");
assert.strictEqual(internalOf("P1", "Incongruenze")[0].cat, "limite");
later(30 * DAY);
r = c.handlers.premioPartita({ partita: open[1].partita, vinta: true });
assert.strictEqual(r.monete, 20, "un mese offline: pagata lo stesso");
// Allenamento dopo un riavvio: mai il biglietto della partita prima (id del telefono diverso, "dopo" con quelli in sospeso).
var x = tick("P1", null, { locale: "A" });
later(MIN);
assert.notStrictEqual(tick("P1", null, { locale: "B" }).partita, x.partita, "altra partita, altro biglietto");
later(MIN);
assert.notStrictEqual(tick("P1", null, { locale: "A", dopo: [x.partita] }).partita, x.partita, "in sospeso sul telefono: non si riusa");
// Archivio: le chiuse oltre 30 passano nei dati interni (ultime 150).
for (i = 0; i < 35; i++) { later(MIN); claim({ vinta: false, allenamento: true }); }
pt = JSON.parse(c.store.readOnly.Partite);
assert.strictEqual(pt.chiuse.length, 30); assert.ok(internalOf("P1", "ArchivioPartite").length >= 5);

// Biglietto non ricevuto (rete): meta' premio, una volta per id del telefono, uno al minuto, PlayStream.
c = load(t0);
r = c.handlers.premioPartita({ senzaBiglietto: "loc1", vinta: true, versione: "2.64" });
assert.strictEqual(r.monete, 20); assert.strictEqual(r.stato, "senzaBiglietto");
assert.strictEqual(c.handlers.premioPartita({ senzaBiglietto: "loc1", vinta: true }).gia, true, "stesso risultato ritentato");
assert.strictEqual(c.handlers.premioPartita({ senzaBiglietto: "loc2", vinta: true }).stato, "attendi", "uno al minuto");
assert.ok(c.store.events.some(function (e) { return e.nome === "partita_senza_biglietto" && e.dati.tipo === "rete"; }));
for (i = 0; i < 25; i++) { later(MIN); r = c.handlers.premioPartita({ senzaBiglietto: "n" + i, vinta: false }); }
assert.strictEqual(r.stato, "limiteSenzaBiglietto", "al massimo 20 al giorno");

// App vecchia (revisione 19, nessun biglietto): premiata finche' la compatibilita' e' accesa, con il record della stanza e non col
// campo "allenamento" del telefono; PlayStream per il controllo. Spenta: niente, "app da aggiornare". Tetto D11 alto: qui si provano
// i rami, il tetto comune ha la sua prova sotto.
c = load(t0, { Economia: JSON.stringify({ partita: { vittoria: 40, sconfitta: 20, tetto: 400, abbandoni: 3, partiteGiorno: 60, tettoNonVerificabili: 1000 } }) });
room = onlineRoom("P2");
r = c.handlers.premioPartita({ vinta: true, stanza: room, allenamento: false });
assert.strictEqual(r.monete, 40, "app vecchia, altra persona nella stanza");
later(MIN);
assert.strictEqual(c.handlers.premioPartita({ vinta: true, allenamento: false }).monete, 20, "senza stanza: meta' anche se dice online");
later(MIN);
actingAs("", function () { c.handlers.RoomLeft({ UserId: "P2", GameId: room, ActorNr: 2 }); });
assert.strictEqual(c.handlers.premioPartita({ vinta: true, stanza: room, abbandono: true, attore: 2 }).monete, 40, "abbandono vero");
later(MIN);
assert.strictEqual(c.handlers.premioPartita({ vinta: true, stanza: room }).monete, 20, "l'altro e' uscito: meta'");
assert.ok(c.store.events.some(function (e) { return e.dati.tipo === "appVecchia"; }));
var economia = { Economia: JSON.stringify({ partita: { vittoria: 40, sconfitta: 20, tetto: 400, abbandoni: 3, partiteGiorno: 60, senzaBiglietto: { attiva: false } } }) };
c = load(t0, economia);
r = c.handlers.premioPartita({ vinta: true });
assert.strictEqual(r.stato, "nonValida"); assert.strictEqual(c.store.wallet.CO, 0, "compatibilita' spenta");
assert.strictEqual(c.handlers.premioPartita({ senzaBiglietto: "x", vinta: true }).monete, 20, "il telefono nuovo senza biglietto resta premiato");

// D11: un tetto comune di 100 monete al giorno per allenamento, app vecchie, senza biglietto e partecipazione; statistiche per tipo;
// le partite confermate non lo toccano.
c = load(t0);
for (i = 0; i < 3; i++) assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 20);
later(MIN);
assert.strictEqual(c.handlers.premioPartita({ senzaBiglietto: "d11", vinta: true }).monete, 20);
later(MIN);
room = onlineRoom("P2");
r = c.handlers.premioPartita({ vinta: true, stanza: room });
assert.strictEqual(r.monete, 20, "app vecchia con un'altra persona: 40, ma restano 20 del tetto comune");
assert.strictEqual(r.tettoNonVerificabili, true); assert.strictEqual(r.tetto, false);
r = claim({ vinta: true, allenamento: true });
assert.strictEqual(r.monete, 0); assert.strictEqual(r.tetto, true, "il telefono dice 'tetto raggiunto'");
assert.strictEqual(r.xp > 0, true, "gli XP non cambiano");
var day = JSON.parse(c.store.readOnly.PartiteOggi);
assert.deepStrictEqual(day.nonVerificabili, { allenamento: 60, senzaBiglietto: 20, appVecchia: 20 }, "monete per tipo");
assert.strictEqual(day.monete, 100);
assert.strictEqual(claim({ vinta: true }).monete, 40, "confermata: premio pieno anche col tetto D11 pieno");
// Partecipazione tagliata dal tetto: quando la partita si conferma si paga il premio pieno (niente da scalare).
room = onlineRoom("P4");
a1 = tick("P1", room); b1 = tick("P4", room);
later(MIN); declare("P1", a1, true, 0); later(11 * MIN);
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "incompleta"); assert.strictEqual(r.monete, 0, "partecipazione oltre il tetto");
declare("P4", b1, false, 1);
r = declare("P1", a1, true, 0);
assert.strictEqual(r.stato, "confermata"); assert.strictEqual(r.monete, 40, "confermata: tutto il premio");
// Il giorno dopo si riparte; il tetto si regola dal Title Data.
c.setNow(c.now + DAY);
assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 20, "giorno nuovo");
c = load(t0, { Economia: JSON.stringify({ partita: { vittoria: 40, sconfitta: 20, tetto: 400, abbandoni: 3, partiteGiorno: 60, tettoNonVerificabili: 30 } }) });
assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 20);
assert.strictEqual(claim({ vinta: true, allenamento: true }).monete, 10, "Economia.partita.tettoNonVerificabili = 30");

// Record con biglietti: a stanza chiusa resta (riesame) segnato "Chiusa"; senza biglietti si cancella come prima.
c = load(t0);
room = onlineRoom("P2");
tick("P1", room);
later(10 * 1000);
actingAs("", function () { c.handlers.RoomClosed({ GameId: room }); });
assert.ok(c.store.groups["partita_" + room].Chiusa, "chiusa ma tenuta");
actingAs("", function () { c.handlers.RoomCreated({ UserId: "P9", Type: "Create", GameId: room }); });
assert.strictEqual(c.store.groups["partita_" + room].g1, "P9", "codice riusato dopo la chiusura: record nuovo");

// Audit 09/10: chiamate API per esecuzione nel caso peggiore (tetto del titolo: 25 per esecuzione, 10 s) (due lucchetti: t0 e' l'inizio di un minuto; una consegna vecchia da
// ritentare; monete e gemme). PlayFab ha un tetto per esecuzione (Title settings -> Limits): questi numeri vanno confrontati con quello,
// e la prova si rompe se una modifica li fa crescere senza accorgersene.
function apiCalls(setup, call) {
    c = load(t0);
    c.store.readOnly.Cons_vecchia = JSON.stringify({ id: "vecchia", CO: 10, GE: 1, stato: "attesa" });
    if (setup) setup();
    var n = 0;
    Object.keys(c.server).forEach(function (k) { var f = c.server[k]; c.server[k] = function (r) { n++; return f(r); }; });
    call();
    return n;
}
var mail2 = function () { c.store.readOnly.Posta = JSON.stringify([{ id: "z", allegati: [{ tipo: "monete", quantita: 100 }, { tipo: "gemme", quantita: 5 }] }]); };
var calls = {
    inizio: apiCalls(null, function () { c.handlers.inizio(); }),
    riscattaPremio: apiCalls(null, function () { c.handlers.riscattaPremio(); }),
    riscattaPosta: apiCalls(mail2, function () { c.handlers.riscattaPosta({ id: "z" }); }),
    riscattaTuttaPosta: apiCalls(mail2, function () { c.handlers.riscattaTuttaPosta(); }),
    premioTutorial: apiCalls(null, function () { c.handlers.premioTutorial(); }),
    inizioPartita: apiCalls(function () { paid = onlineRoom(); }, function () { c.handlers.inizioPartita({ stanza: paid }); }),
    // Fine partita confermata: l'altro ha gia' dichiarato (record, dichiarazione, consegna, statistiche).
    premioPartita: apiCalls(function () { tk = ticket({}); c.setNow(c.now + 61 * 1000);
        actingAs("QX", function () { c.handlers.premioPartita(Object.assign({ partita: tk.altro.partita }, decl(false, 1))); }); },
        function () { c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))); }),
    // Contestata: in piu' l'evento PlayStream.
    premioPartitaContestata: apiCalls(function () { tk = ticket({}); c.setNow(c.now + 61 * 1000);
        actingAs("QX", function () { c.handlers.premioPartita(Object.assign({ partita: tk.altro.partita }, decl(true, 1))); }); },
        function () { c.handlers.premioPartita(Object.assign({ partita: tk.partita }, decl(true, 0))); }),
    premioPartitaAbbandono: apiCalls(function () { tk = ticket({}); c.setNow(c.now + 61 * 1000);
        actingAs("", function () { c.handlers.RoomLeft({ UserId: "QX", GameId: tk.stanza, ActorNr: 2 }); }); },
        function () { c.handlers.premioPartita(Object.assign({ partita: tk.partita, abbandono: true, attore: 2 }, decl(true, 0))); }),
    premioPartitaAppVecchia: apiCalls(function () { paid = onlineRoom(); },
        function () { c.handlers.premioPartita({ vinta: true, stanza: paid }); })
};
var MAX_CALLS = { inizio: 12, riscattaPremio: 13, riscattaPosta: 14, riscattaTuttaPosta: 14, premioTutorial: 14, inizioPartita: 8, premioPartita: 18,
    premioPartitaContestata: 19, premioPartitaAbbandono: 18, premioPartitaAppVecchia: 18 }; // tetto del titolo 10A53D: 25
Object.keys(MAX_CALLS).forEach(function (k) { assert.ok(calls[k] <= MAX_CALLS[k], k + ": " + calls[k] + " chiamate API (max " + MAX_CALLS[k] + ")"); });
if (process.env.CHIAMATE) console.log(JSON.stringify(calls));

function pick(s) { return { giorno: s.giorno, riscattato: s.riscattato }; }
console.log("CloudScript: tutte le prove passate");
