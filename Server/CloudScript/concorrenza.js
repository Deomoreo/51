// Prova di concorrenza vera delle consegne: node Server/CloudScript/concorrenza.js [giri] [seme]
// Ogni chiamata del telefono gira in un thread suo (worker_threads) con 51.js; le API di PlayFab sono un finto server nel thread
// principale che esegue una richiesta alla volta (come PlayFab: ogni chiamata e' atomica) e sceglie a caso quale, tra quelle di tutti i
// thread fermi in attesa: cosi' si provano gli intrecci veri (Home e fine partita insieme, due tocchi, due telefoni). Guasti a caso:
// rifiuto certo di PlayFab, accredito fatto con risposta persa, nessuna risposta, script morto prima o dopo una chiamata.
// Ogni premio ha importi unici, quindi ogni accredito si riconosce. Si controlla: nessun premio consumato due volte, nessun accredito
// doppio, e alla fine (giro senza guasti) ogni premio e' arrivato una volta oppure e' segnato "incerto" per la riconciliazione.
var wt = require("worker_threads"), vm = require("vm"), fs = require("fs"), path = require("path"), assert = require("assert");
var SRC = path.join(__dirname, "51.js");

function fixedDate(ms) {
    function D(a, b, c, d, e, f) { return arguments.length ? new (Function.prototype.bind.apply(Date, [null].concat([].slice.call(arguments))))() : new Date(ms); }
    D.now = function () { return ms; }; D.UTC = Date.UTC; D.parse = Date.parse;
    return D;
}

// --- Thread di una chiamata: 51.js con "server" che chiede al thread principale e aspetta (le API di CloudScript sono sincrone).
if (!wt.isMainThread) {
    var d = wt.workerData, ctl = new Int32Array(d.sab, 0, 2), buf = new Uint8Array(d.sab, 8);
    var server = new Proxy({}, { get: function (_, fn) {
        return function (req) {
            Atomics.store(ctl, 0, 0);
            wt.parentPort.postMessage({ api: fn, req: req });
            Atomics.wait(ctl, 0, 0);
            var res = JSON.parse(Buffer.from(buf.slice(0, Atomics.load(ctl, 1))).toString());
            if (res.apiError) throw { apiErrorInfo: { apiError: res.apiError } };
            if (res.error) throw new Error(res.error);
            return res.data;
        };
    } });
    var ctx = { handlers: {}, currentPlayerId: "P1", server: server, log: { error: function () {} }, Date: fixedDate(d.now) };
    vm.runInNewContext(fs.readFileSync(SRC, "utf8"), ctx);
    var out;
    try { out = { result: ctx.handlers[d.fn](d.args) }; } catch (ex) { out = { thrown: String(ex && ex.message || JSON.stringify(ex)) }; }
    wt.parentPort.postMessage({ done: out });
    return;
}

// --- Finto PlayFab (un giocatore, P1).
function rng(seed) { return function () { seed = (seed * 1103515245 + 12345) & 0x7fffffff; return seed / 0x7fffffff; }; }

function Store(rnd, faults) {
    this.ro = {}; this.internal = {}; this.stats = {}; this.wallet = { CO: 0, GE: 0 }; this.groups = {};
    this.rnd = rnd; this.faults = faults; this.economies = 0;
    this.obligations = {};  // chiave Cons_ -> {CO, GE} della prima scrittura
    this.applied = [];      // accrediti fatti: {c, n, ack}
    this.writes = { Premi: 0, Tutorial: 0 };
}

function wrap(obj, keys) {
    var out = {};
    (keys || Object.keys(obj)).forEach(function (k) { if (obj[k] !== undefined) out[k] = { Value: obj[k] }; });
    return out;
}

// Ogni lettura dell'Economia da' importi nuovi: ogni premio (giornaliero, tutorial, partita) ha un importo che c'e' solo lui.
Store.prototype.economy = function () {
    if (this.eco) return this.eco;
    var k = ++this.economies * 10000;
    var day = [{ tipo: "monete", quantita: k + 1 }, { tipo: "gemme", quantita: k + 2 }];
    return JSON.stringify({ settimana: [day, day, day, day, day, day, day], tutorial: [{ tipo: "monete", quantita: k + 3 }],
        partita: { vittoria: k + 4, sconfitta: k + 4, tetto: 1e9, abbandoni: 3, partiteGiorno: 60 } });
};

Store.prototype.api = function (fn, r) {
    var s = this;
    switch (fn) {
        case "GetTitleData": return { Data: (r.Keys || []).indexOf("Economia") >= 0 ? { Economia: s.economy() } : {} };
        case "GetUserAccountInfo": return { UserInfo: { Username: "uP1", PlayFabId: "P1" } };
        case "GetUserReadOnlyData": return { Data: wrap(s.ro, r.Keys) };
        case "GetUserInternalData": return { Data: wrap(s.internal, r.Keys) };
        case "UpdateUserInternalData": Object.assign(s.internal, r.Data || {}); (r.KeysToRemove || []).forEach(function (k) { delete s.internal[k]; }); return {};
        case "UpdateUserReadOnlyData":
            assert.ok(Object.keys(r.Data || {}).length + (r.KeysToRemove || []).length <= 10, "al massimo 10 chiavi per chiamata");
            Object.keys(r.Data || {}).forEach(function (k) {
                if (k.indexOf("Cons_") === 0 && !s.obligations[k]) { var e = JSON.parse(r.Data[k]); s.obligations[k] = { CO: e.CO, GE: e.GE }; }
                if (k === "Premi" || k === "Tutorial") s.writes[k]++;
                s.ro[k] = r.Data[k];
            });
            (r.KeysToRemove || []).forEach(function (k) { delete s.ro[k]; });
            return {};
        case "GetUserInventory": return { VirtualCurrency: { CO: s.wallet.CO, GE: s.wallet.GE } };
        case "GetPlayerStatistics": return { Statistics: Object.keys(s.stats).map(function (n) { return { StatisticName: n, Value: s.stats[n] }; }) };
        case "UpdatePlayerStatistics": r.Statistics.forEach(function (x) { s.stats[x.StatisticName] = x.Value; }); return {};
        case "CreateSharedGroup":
            if (s.groups[r.SharedGroupId]) throw { apiError: { error: "InvalidSharedGroupId", errorCode: 1088, errorMessage: "in uso" } };
            s.groups[r.SharedGroupId] = {}; return { SharedGroupId: r.SharedGroupId };
        case "DeleteSharedGroup":
            if (!s.groups[r.SharedGroupId]) throw { apiError: { error: "InvalidSharedGroupId", errorCode: 1088, errorMessage: "non trovato" } };
            delete s.groups[r.SharedGroupId]; return {};
        case "AddUserVirtualCurrency":
            s.wallet[r.VirtualCurrency] += r.Amount;
            s.applied.push({ c: r.VirtualCurrency, n: r.Amount, ack: true });
            return { Balance: s.wallet[r.VirtualCurrency] };
        default: throw new Error("API non prevista: " + fn);
    }
};

/**
 * Una richiesta di un thread: esito {data} | {apiError} | {error} | "crash" (il thread muore: prima o dopo che PlayFab l'ha eseguita).
 * Guasti solo se faults: rifiuto certo dell'accredito, accredito fatto con risposta persa, accredito senza risposta, scrittura fatta
 * con risposta persa, script morto.
 */
Store.prototype.handle = function (fn, req) {
    var f = this.faults ? this.rnd() : 1;
    if (f < 0.02) return "crash";
    if (f < 0.04) { try { this.api(fn, req); } catch (ex) { } this.markUnacked(fn); return "crash"; }
    if (fn === "AddUserVirtualCurrency") {
        if (f < 0.14) return { apiError: { error: "InvalidVirtualCurrency", errorCode: 1050, errorMessage: "valuta che non c'e'" } };
        if (f < 0.20) { this.api(fn, req); this.markUnacked(fn); return { error: "timeout" }; }
        if (f < 0.23) return { error: "nessuna risposta" };
    }
    if (fn === "UpdateUserReadOnlyData" && f < 0.08) { this.api(fn, req); return { error: "timeout" }; }
    try { return { data: this.api(fn, req) }; } catch (ex) { return ex.apiError ? { apiError: ex.apiError } : { error: String(ex.message) }; }
};

Store.prototype.markUnacked = function (fn) { if (fn === "AddUserVirtualCurrency") this.applied[this.applied.length - 1].ack = false; };

/** Le chiamate insieme, ciascuna nel suo thread; ad ogni passo una richiesta a caso tra quelle dei thread fermi in attesa. */
function runTogether(store, calls) {
    return new Promise(function (resolve) {
        var live = calls.map(function (c) {
            var sab = new SharedArrayBuffer(8 + (1 << 20));
            var st = { sab: sab, ctl: new Int32Array(sab, 0, 2), pending: null, done: false };
            st.worker = new wt.Worker(__filename, { workerData: { sab: sab, now: c.now, fn: c.fn, args: c.args } });
            st.worker.on("message", function (m) { if (m.done) { st.done = true; st.result = m.done; } else st.pending = m; step(); });
            st.worker.on("error", function (e) { st.done = true; st.result = { thrown: String(e) }; step(); });
            return st;
        });
        var busy = false;
        function step() {
            if (busy) return;
            if (live.some(function (st) { return !st.done && !st.pending; })) return; // qualcuno sta ancora lavorando
            var waiting = live.filter(function (st) { return !st.done && st.pending; });
            if (!waiting.length) { resolve(live.map(function (st) { return st.result; })); return; }
            var st = waiting[Math.floor(store.rnd() * waiting.length)], m = st.pending;
            st.pending = null;
            var res = store.handle(m.api, m.req);
            if (res === "crash") {
                busy = true;
                st.worker.removeAllListeners("error");
                st.worker.terminate().then(function () { st.done = true; st.result = { crash: m.api }; busy = false; step(); });
                return;
            }
            var bytes = Buffer.from(JSON.stringify(res));
            new Uint8Array(st.sab, 8).set(bytes);
            Atomics.store(st.ctl, 1, bytes.length);
            Atomics.store(st.ctl, 0, 1);
            Atomics.notify(st.ctl, 0);
        }
    });
}

// --- Giri
var MAIL = [{ id: "m1", titolo: "M1", data: "2026-10-01", allegati: [{ tipo: "monete", quantita: 101 }, { tipo: "gemme", quantita: 7 }] },
    { id: "m2", titolo: "M2", data: "2026-10-01", allegati: [{ tipo: "monete", quantita: 202 }] }];
var CALLS = [{ fn: "inizio" }, { fn: "riscattaPremio" }, { fn: "riscattaPremio" }, { fn: "premioTutorial" }, { fn: "premioTutorial" },
    { fn: "riscattaPosta", args: { id: "m1" } }, { fn: "riscattaTuttaPosta" }, { fn: "premioPartita", args: { vinta: true, partita: "pT" } },
    { fn: "premioPartita", args: { vinta: true, partita: "pT" } }]; // la stessa partita due volte (tentativo ripetuto, riavvio)
// Ogni chiamata col suo orologio, a cavallo di un cambio di minuto (i lucchetti sono per minuto, con 15 s di guardia).
var T0 = Date.UTC(2026, 9, 1, 10, 0, 0) - 4000;

async function round(seed, faults) {
    var rnd = rng(seed), store = new Store(rnd, faults);
    store.ro.Posta = JSON.stringify(MAIL);
    store.ro.Cons_vecchia = JSON.stringify({ id: "vecchia", CO: 9, GE: 0, stato: "attesa" }); // rimasta da prima (Home + fine partita)
    store.obligations.Cons_vecchia = { CO: 9, GE: 0 };
    store.ro.Partite = JSON.stringify({ aperte: [{ id: "pT", t: T0 - 120000, k: "a:loc", umani: 0 }], chiuse: [] });
    var n = 2 + Math.floor(rnd() * 3), calls = [];
    for (var i = 0; i < n; i++) {
        var c = CALLS[Math.floor(rnd() * CALLS.length)];
        calls.push({ fn: c.fn, args: c.args, now: T0 + Math.floor(rnd() * 8000) });
    }
    var results = await runTogether(store, calls);
    // Poi, senza guasti e con i lucchetti scaduti: Home dieci volte, una alla volta.
    store.faults = false;
    for (i = 0; i < 10; i++) await runTogether(store, [{ fn: "inizio", now: T0 + 140000 + i }]);
    // Il telefono ritenta la fine partita rimasta senza risposta (o occupata): paga solo se non era gia' pagata.
    await runTogether(store, [{ fn: "premioPartita", args: { vinta: true, partita: "pT" }, now: T0 + 150000 }]);
    check(store, faults, calls, results);
}

function check(store, faults, calls, results) {
    var where = JSON.stringify(calls.map(function (c) { return c.fn; })) + " -> " + JSON.stringify(results);
    // Consumi: premio di oggi e tutorial scritti una volta; ogni messaggio in una sola consegna (m1+m2 = 303).
    assert.ok(store.writes.Premi <= 1, "premio di oggi consumato due volte " + where);
    assert.ok(store.writes.Tutorial <= 1, "tutorial consumato due volte " + where);
    assert.ok(Object.keys(store.obligations).filter(function (k) { return k.indexOf("Cons_g") === 0; }).length <= 1, "partita pagata due volte " + where);
    assert.ok((store.stats.TotalGames | 0) <= 1, "partita contata due volte nelle statistiche " + where);
    var keys = Object.keys(store.obligations), byAmount = { CO: {}, GE: {} };
    var mails = 0;
    keys.forEach(function (k) {
        var o = store.obligations[k];
        if (o.CO === 101 || o.CO === 303) mails += 1; if (o.CO === 202 || o.CO === 303) mails += 2;
        ["CO", "GE"].forEach(function (c) { if (o[c] > 0) { assert.ok(!byAmount[c][o[c]], "due premi con lo stesso importo"); byAmount[c][o[c]] = k; } });
    });
    assert.ok(mails === 0 || mails === 1 || mails === 2 || mails === 3, "un messaggio riscattato due volte " + where);
    // Accrediti: ognuno appartiene a un premio, e ogni premio arriva al massimo una volta per valuta.
    var got = {};
    store.applied.forEach(function (a) {
        var k = byAmount[a.c][a.n];
        assert.ok(k, "accredito che non e' di nessun premio " + JSON.stringify(a) + " " + where);
        got[k + a.c] = (got[k + a.c] || 0) + 1;
        assert.strictEqual(got[k + a.c], 1, "ACCREDITO DOPPIO " + k + " " + a.c + " " + where);
    });
    // Alla fine niente in attesa ne' a meta': ogni premio e' arrivato, oppure e' "incerto" (da riconciliare), mai perso in silenzio.
    keys.forEach(function (k) {
        var e = store.ro[k] ? JSON.parse(store.ro[k]) : null;
        assert.ok(!e || e.stato === "incerto", "consegna rimasta " + (e && e.stato) + " " + k + " " + where);
        ["CO", "GE"].forEach(function (c) {
            if (!(store.obligations[k][c] > 0)) return;
            var applied = store.applied.filter(function (a) { return byAmount[a.c][a.n] === k && a.c === c; })[0];
            if (!applied) assert.ok(e && e[c] > 0, "premio perso senza segno " + k + " " + c + " " + where);
            else if (!applied.ack) assert.ok(e, "accredito senza risposta non segnato incerto " + k + " " + where);
            if (!faults) assert.ok(applied && applied.ack && !e, "senza guasti deve arrivare una volta " + k + " " + where);
        });
    });
    var total = { CO: 0, GE: 0 };
    store.applied.forEach(function (a) { total[a.c] += a.n; });
    assert.deepStrictEqual(total, store.wallet);
}

// D11: premi non verificabili (allenamento, senza biglietto, app vecchia) chiesti insieme, con guasti, poi ritentati tutti: le monete
// consegnate (anche incerte) non superano mai il tetto comune di 100 e i contatori del giorno sono esattamente quelle monete.
var NV_CALLS = [{ fn: "premioPartita", args: { vinta: true, partita: "pA" } }, { fn: "premioPartita", args: { vinta: true, partita: "pB" } },
    { fn: "premioPartita", args: { vinta: true, partita: "pC" } }, { fn: "premioPartita", args: { vinta: true, partita: "pD" } },
    { fn: "premioPartita", args: { vinta: true, senzaBiglietto: "x1" } }, { fn: "premioPartita", args: { vinta: true } }];

async function roundNv(seed, faults) {
    var rnd = rng(seed), store = new Store(rnd, faults), i;
    store.eco = JSON.stringify({ partita: { vittoria: 80, sconfitta: 80, tetto: 1e9, abbandoni: 3, partiteGiorno: 60, tettoNonVerificabili: 100 } });
    store.ro.Partite = JSON.stringify({ aperte: ["pA", "pB", "pC", "pD"].map(function (id) { return { id: id, t: T0 - 120000, k: "a:" + id, umani: 0 }; }), chiuse: [] });
    var n = 3 + Math.floor(rnd() * 4), calls = [];
    for (i = 0; i < n; i++) {
        var c = NV_CALLS[Math.floor(rnd() * NV_CALLS.length)];
        calls.push({ fn: c.fn, args: c.args, now: T0 + Math.floor(rnd() * 8000) });
    }
    var results = await runTogether(store, calls);
    store.faults = false;
    for (i = 0; i < NV_CALLS.length; i++) await runTogether(store, [{ fn: NV_CALLS[i].fn, args: NV_CALLS[i].args, now: T0 + 140000 + i * 61000 }]);
    var where = JSON.stringify(calls.map(function (c) { return c.args; })) + " -> " + JSON.stringify(results);
    var owedCO = Object.keys(store.obligations).reduce(function (sum, k) { return sum + store.obligations[k].CO; }, 0);
    var day = store.ro.PartiteOggi ? JSON.parse(store.ro.PartiteOggi) : { monete: 0, nonVerificabili: {} };
    var byType = Object.keys(day.nonVerificabili).reduce(function (sum, k) { return sum + day.nonVerificabili[k]; }, 0);
    assert.ok(owedCO <= 100, "TETTO D11 SUPERATO: " + owedCO + " " + where);
    assert.ok(store.wallet.CO <= 100, "accreditate oltre il tetto " + store.wallet.CO + " " + where);
    assert.strictEqual(day.monete, owedCO, "contatore del giorno diverso dalle monete consegnate " + where);
    assert.strictEqual(byType, owedCO, "contatori per tipo diversi dalle monete consegnate " + where);
    if (!faults) assert.strictEqual(owedCO, 100, "senza guasti il tetto si raggiunge " + where);
}

(async function () {
    var rounds = parseInt(process.argv[2] || "300", 10), seed0 = parseInt(process.argv[3] || "51", 10);
    for (var i = 0; i < rounds; i++) {
        var seed = seed0 + i;
        try { await round(seed, i % 3 !== 0); await roundNv(seed, i % 3 !== 0); }
        catch (ex) { console.error("seme " + seed + ": " + ex.message); process.exit(1); }
    }
    console.log("Concorrenza: " + rounds + " giri passati, consegne e tetto D11 (un terzo senza guasti)");
})();
