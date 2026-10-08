// Ambiente QA dei premi (terzo giro Android 08/10): stati ripetibili su un account di prova, senza creare account nuovi ne' aspettare
// il giorno dopo. Gira solo sul PC, con la chiave segreta di PlayFab in Server/QA/qa.json (fuori da git):
//   {"titleId": "XXXXX", "secretKey": "...", "accountProva": ["qa_1"]}
// Titolo di SVILUPPO (consigliato): tutto. Titolo dell'app (TitleId di PlayFabSharedSettings): si legge, e "prepara"/"riscattato" solo
// sugli account elencati in accountProva; mai guasti finti ne' "deploy". Nell'app non c'e' niente di tutto questo: nessun pulsante,
// nessun reset, nessun accredito.
//
//   node Server/QA/qa.js diagnosi                  valute CO/GE, revisione CloudScript pubblicata, Economia (anche sul titolo dell'app)
//   node Server/QA/qa.js stato <utente>            saldo, premio del giorno, tutorial, posta, consegne (attesa / incerte), guasto
//   node Server/QA/qa.js deploy                    carica e pubblica 51.js sul titolo di sviluppo, coi guasti finti accesi (QA = true)
//   node Server/QA/qa.js prepara <utente> [1-7]    premio del giorno N da riscattare (7 = forziere viola), tutorial +200 da riscattare,
//                                                  due messaggi con premio (monete+gemme, forziere verde), nessun guasto
//   node Server/QA/qa.js riscattato <utente>       premio di oggi gia' riscattato
//   node Server/QA/qa.js guasto <utente> <tipo>    rifiuto | incerto | timeout | lento | nessuno (vedi LEGGIMI.md)
//   node Server/QA/qa.js riconcilia <utente> <Cons_...|Tutorial> [CO=si|no] [GE=si|no] [--reale]
//                                                  consegna incerta (dopo PlayStream) o ferma: arrivata = chiusa, non arrivata =
//                                                  in attesa, la paga il server una volta (LEGGIMI.md, sezione 5)
//   node Server/QA/qa.js esito <utente> <partita> vinta|persa|nulla [--reale]
//                                                  partita contestata, incompleta chiusa o da riconciliare: decisione a mano


var fs = require("fs"), path = require("path");
var cs = require("../CloudScript/51.js");

var cfgFile = path.join(__dirname, "qa.json");
if (!fs.existsSync(cfgFile)) fail("manca Server/QA/qa.json: {\"titleId\": \"...\", \"secretKey\": \"...\"} (vedi LEGGIMI.md)");
var cfg = JSON.parse(fs.readFileSync(cfgFile, "utf8"));
var settings = fs.readFileSync(path.join(__dirname, "../../Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset"), "utf8");
var appTitle = (/TitleId:\s*(\S+)/.exec(settings) || [])[1];
var production = String(cfg.titleId).toUpperCase() === String(appTitle).toUpperCase();
var FAULTS = ["rifiuto", "incerto", "timeout", "lento"];
var RECONCILED = "Riconciliazioni"; // dati interni del giocatore: consegne gia' riconciliate (riconcilia)
var KINDS = { premio: "premio giornaliero", tutorial: "tutorial", m: "Posta", g: "partita", v: "registro del terzo giro", rec: "recupero" };

/** Una consegna leggibile: chiave, cosa era, quando (ora italiana), importi, stato e cosa fare. */
function describe(k, e) {
    var id = String(e.id || ""), kind = id.indexOf("premio") === 0 ? "premio giornaliero " + id.substring(6)
        : KINDS[id] || (id.indexOf("rec") === 0 ? KINDS.rec : KINDS[id.charAt(0)]) || "?"; // m, g, v + ora in base 36
    var when = e.t ? new Date(e.t).toLocaleString("it-IT", { timeZone: "Europe/Rome" }) : "?";
    var todo = { attesa: "arriva da sola alla prossima apertura dell'app",
        inCorso: "accredito in corso, o script fermato: la prossima chiamata la segna incerta",
        incerto: "controlla PlayStream (" + (e.dubbio || "?") + ") e poi riconcilia",
        fermo: "rifiutata " + (e.tentativi | 0) + " volte, di sicuro non arrivata: togli la causa e poi riconcilia" }[e.stato] || "?";
    return k + "  " + kind + ", " + when + ": " + (e.CO | 0) + " CO, " + (e.GE | 0) + " GE  " + String(e.stato).toUpperCase() +
        (e.errore ? "  (" + e.errore + ")" : "") + "\n      -> " + todo;
}

function fail(msg) { console.error("qa: " + msg); process.exit(1); }

async function call(api, fn, body) {
    var res = await fetch("https://" + cfg.titleId + ".playfabapi.com/" + api + "/" + fn, {
        method: "POST", headers: { "Content-Type": "application/json", "X-SecretKey": cfg.secretKey }, body: JSON.stringify(body || {})
    });
    var json = await res.json();
    if (json.code !== 200) fail(api + "/" + fn + ": " + json.error + " " + json.errorCode + " " + json.errorMessage);
    return json.data;
}

function writable(name) {
    if (!production) return;
    if (name !== undefined && (cfg.accountProva || []).indexOf(name) >= 0) return;
    fail("il titolo " + cfg.titleId + " e' quello dell'app: " + (name === undefined ? "comando solo per il titolo di sviluppo"
        : "\"" + name + "\" non e' in accountProva di qa.json"));
}

async function player(name) {
    if (!name) fail("manca il nome utente");
    var info = await call("Admin", "GetUserAccountInfo", /^[0-9A-F]{16}$/.test(name) ? { PlayFabId: name } : { Username: name });
    return info.UserInfo.PlayFabId;
}

function value(data, key) { return data && data.Data && data.Data[key] ? data.Data[key].Value : undefined; }

var today = cs.italianDate(Date.now());

var commands = {
    diagnosi: async function () {
        console.log("Titolo " + cfg.titleId + (production ? " (quello dell'app, sola lettura)" : " (sviluppo)"));
        var vc = (await call("Admin", "ListVirtualCurrencyTypes")).VirtualCurrencies || [];
        var codes = vc.map(function (v) { return v.CurrencyCode; });
        console.log("Valute (Economy legacy): " + (codes.join(", ") || "nessuna"));
        ["CO", "GE"].forEach(function (c) {
            if (codes.indexOf(c) < 0) console.log("  MANCA " + c + ": AddUserVirtualCurrency la rifiuta, nessun premio puo' arrivare");
        });
        var versions = (await call("Admin", "GetCloudScriptVersions")).Versions || [];
        var live = versions.length ? versions[versions.length - 1] : null;
        if (!live) console.log("CloudScript: nessuna revisione");
        else {
            var rev = await call("Admin", "GetCloudScriptRevision", { Version: live.Version, Revision: live.PublishedRevision });
            var src = (rev.Files || []).map(function (f) { return f.FileContents; }).join("\n");
            console.log("CloudScript pubblicato: revisione " + live.PublishedRevision + " (ultima " + live.LatestRevision + ", " + rev.CreatedAt + ")");
            console.log("  consegna col lucchetto (09/10): " + (src.indexOf("function acquire(") >= 0 ? "si'"
                : src.indexOf("function deliver(") >= 0 ? "NO, terzo giro 08/10 (registro Consegne)" : "NO, revisione vecchia"));
            console.log("  sessione attiva (secondo giro 08/10): " + (src.indexOf("handlers.sessione") >= 0 ? "si'" : "no"));
        }
        var td = (await call("Server", "GetTitleData", { Keys: ["Economia"] })).Data || {};
        console.log("TitleData Economia: " + (td.Economia ? td.Economia : "assente (valori del CloudScript)"));
    },

    stato: async function (name) {
        var id = await player(name);
        var inv = await call("Server", "GetUserInventory", { PlayFabId: id });
        var ro = await call("Server", "GetUserReadOnlyData", { PlayFabId: id }); // tutte le chiavi: le consegne sono "Cons_<id>"
        var inn = await call("Server", "GetUserInternalData", { PlayFabId: id, Keys: ["Tutorial", "QAGuasto", RECONCILED, "Incongruenze"] });
        var premi = JSON.parse(value(ro, "Premi") || "null");
        var posta = JSON.parse(value(ro, "Posta") || "[]");
        console.log(name + " (" + id + ")");
        console.log("  saldo: " + JSON.stringify(inv.VirtualCurrency || {}));
        console.log("  premio di oggi (" + today + "): " + (premi && premi.ultimo === today ? "riscattato (giorno " + premi.giorno + ")" : "da riscattare") +
            "  dati: " + JSON.stringify(premi));
        console.log("  tutorial: " + (value(ro, "Tutorial") || value(inn, "Tutorial") ? "gia' dato" : "da riscattare"));
        console.log("  posta da riscattare: " + posta.filter(function (m) { return !m.riscattato && m.allegati && m.allegati.length; }).length + " di " + posta.length);
        var cons = Object.keys(ro.Data || {}).filter(function (k) { return k.indexOf("Cons_") === 0; });
        console.log("  consegne: " + (cons.length ? "" : "nessuna"));
        cons.forEach(function (k) { console.log("    " + describe(k, JSON.parse(value(ro, k) || "{}"))); });
        // Biglietti delle partite (#141, Fase A): aperte = iniziate e senza risultato; riesame = dichiarate e non pagate per intero;
        // chiuse = ultimi esiti. Incongruenze = registro interno (contestate, incomplete, da riconciliare): decide "esito".
        var partite = JSON.parse(value(ro, "Partite") || "{}"), when = function (t) { return new Date(t).toLocaleString("it-IT", { timeZone: "Europe/Rome" }); };
        (partite.aperte || []).forEach(function (p) {
            console.log("  partita aperta " + p.id + " (" + when(p.t) + ", " + (p.umani >= 2 ? "con altre persone" : "allenamento") +
                "): il telefono la chiude o la ritenta in Home");
        });
        (partite.riesame || []).forEach(function (p) {
            console.log("  partita in riesame " + p.id + " (" + when(p.t) + "): " + p.stato + ", " + (p.m | 0) + " CO di partecipazione" +
                (p.stato === "contestata" ? " -> esito " + name + " " + p.id + " vinta|persa|nulla" : ""));
        });
        (partite.chiuse || []).slice(-5).forEach(function (p) { console.log("  partita chiusa " + p.id + ": " + p.s + ", " + (p.m | 0) + " CO, " + (p.x | 0) + " XP"); });
        var odd = JSON.parse(value(inn, "Incongruenze") || "[]");
        if (odd.length) console.log("  incongruenze (ultime 5 di " + odd.length + "):");
        odd.slice(-5).forEach(function (e) {
            console.log("    " + e.partita + " " + when(e.t) + ": " + e.stato + " (" + (e.cat || "?") + ")  " + JSON.stringify(e.dichiarazioni || []));
        });
        if (value(ro, "Consegne")) console.log("  registro del terzo giro (lo converte la prossima chiamata dei premi): " + value(ro, "Consegne"));
        // Segno del tutorial degli script fino all'08/10: diventa una consegna incerta solo se il telefono richiama premioTutorial,
        // cosa che fa solo se il premio gli era rimasto in sospeso. Qui si vede comunque.
        if (value(inn, "Tutorial") && !value(ro, "Tutorial"))
            console.log("    Tutorial (segno vecchio del " + value(inn, "Tutorial") + ")  INCERTO: 200 CO forse mai arrivate -> riconcilia " + name + " Tutorial CO=si|no");
        var log = JSON.parse(value(inn, RECONCILED) || "[]");
        if (log.length) console.log("  riconciliate: " + log.map(function (x) { return x.chiave + " (" + x.data + ")"; }).join(", "));
        console.log("  guasto: " + (value(inn, "QAGuasto") || "nessuno"));
    },

    // Consegna incerta o ferma, dopo il controllo in PlayStream (LEGGIMI.md, sezione 5). Ogni riconciliazione va nell'elenco
    // "Riconciliazioni" (dati interni del giocatore); un'incerta gia' riconciliata si rifiuta. Sul titolo dell'app, per un giocatore
    // vero (non in accountProva), serve --reale.
    riconcilia: async function (name, key) {
        var flags = [].slice.call(arguments, 2), real = flags.indexOf("--reale") >= 0;
        if (!(real && production)) writable(name);
        if (!key) fail("riconcilia <utente> <Cons_...|Tutorial> [CO=si|no] [GE=si|no] [--reale]");
        var orig = key, said = {};
        flags.forEach(function (f) { var m = /^(CO|GE)=(si|no)$/i.exec(f); if (m) said[m[1].toUpperCase()] = m[2].toLowerCase() === "si"; });
        var id = await player(name);
        var inn = await call("Server", "GetUserInternalData", { PlayFabId: id, Keys: [RECONCILED, "Tutorial"] });
        var log = JSON.parse(value(inn, RECONCILED) || "[]");
        var ro = await call("Server", "GetUserReadOnlyData", { PlayFabId: id, Keys: [key, "Tutorial"] });
        var e, write = {}, remove = [];
        if (key === "Tutorial") {
            if (!value(inn, "Tutorial") || value(ro, "Tutorial")) fail("nessun segno vecchio del tutorial da riconciliare");
            // 200: il premio del tutorial degli script fino all'08/10 (DEFAULTS.tutorial)
            e = { id: "tutorial", t: value(inn, "Tutorial"), CO: 200, GE: 0, stato: "incerto", dubbio: "CO" };
            write.Tutorial = value(inn, "Tutorial"); // segno nuovo: il server non lo ritrasforma piu'
            key = "Cons_tutorial";
        } else {
            e = JSON.parse(value(ro, key) || "null");
            if (!e) fail(key + " non c'e' (gia' arrivata o tolta)");
        }
        // Una ferma si puo' rimettere in attesa piu' volte (di sicuro non e' mai arrivata), un'incerta una volta sola.
        if (e.stato !== "fermo" && log.some(function (x) { return x.chiave === orig; })) fail(orig + " e' gia' stata riconciliata (vedi stato)");
        var before = JSON.stringify(e), choice = {};
        if (e.stato === "incerto") {
            String(e.dubbio || "").split(",").filter(Boolean).forEach(function (c) {
                if (said[c] === undefined) fail(c + " senza esito certo: aggiungi " + c + "=si (arrivata, c'e' in PlayStream) o " + c + "=no (non c'e')");
                choice[c] = said[c] ? "arrivata" : "non arrivata";
                if (said[c]) e[c] = 0;
            });
        } else if (e.stato !== "fermo") fail(key + " e' \"" + e.stato + "\": la consegna la fa gia' il server, niente da fare");
        if (e.CO > 0 || e.GE > 0) {
            e = { id: e.id, t: e.t, CO: e.CO | 0, GE: e.GE | 0, stato: "attesa", nota: "riconciliata " + today };
            write[key] = JSON.stringify(e);
        } else remove.push(key);
        log.push({ chiave: orig, data: today, esito: choice, prima: before });
        var r = { PlayFabId: id, Data: write };
        if (remove.length) r.KeysToRemove = remove;
        // Prima l'elenco, poi la consegna: se la seconda scrittura fallisce si rifa' a mano, ma mai due volte da qui.
        await call("Server", "UpdateUserInternalData", { PlayFabId: id, Data: (function (d) { d[RECONCILED] = JSON.stringify(log.slice(-100)); return d; })({}) });
        await call("Server", "UpdateUserReadOnlyData", r);
        console.log(name + ": " + (remove.length ? key + " chiusa, niente da accreditare"
            : key + " in attesa: " + e.CO + " CO, " + e.GE + " GE arrivano alla prossima apertura dell'app (una volta sola)"));
    },

    // #142 Fase A (D3, D4): decisione a mano su una partita che il server non ha potuto verificare, dopo aver letto le dichiarazioni
    // (stato -> incongruenze). vinta/persa = monete della vittoria/sconfitta meno la partecipazione gia' data, come consegna in attesa
    // (la paga il server alla prossima apertura dell'app, una volta); nulla = solo registrata. Si decide solo cio' che il server non
    // pagera' mai da solo: contestate (le dichiarazioni non cambiano piu') e partite gia' chiuse; un'incompleta in riesame puo' ancora
    // confermarsi da sola. Il biglietto non si tocca (lo scrive solo il server, sotto il lucchetto). XP e statistiche no: le classifiche
    // restano solo sulle partite verificate. Una decisione per partita (elenco "Riconciliazioni").
    esito: async function (name, match, choice) {
        var real = [].slice.call(arguments, 3).indexOf("--reale") >= 0;
        if (!(real && production)) writable(name);
        if (!match || ["vinta", "persa", "nulla"].indexOf(choice) < 0) fail("esito <utente> <partita> vinta|persa|nulla [--reale]");
        var id = await player(name);
        var ro = await call("Server", "GetUserReadOnlyData", { PlayFabId: id, Keys: ["Partite"] });
        var inn = await call("Server", "GetUserInternalData", { PlayFabId: id, Keys: [RECONCILED, "ArchivioPartite"] });
        var t = JSON.parse(value(ro, "Partite") || "{}"), log = JSON.parse(value(inn, RECONCILED) || "[]");
        var find = function (list) { return (list || []).filter(function (x) { return x && x.id === match; })[0]; };
        var closed = find(t.chiuse) || find(JSON.parse(value(inn, "ArchivioPartite") || "[]")), open = find(t.riesame) || find(t.aperte);
        var e = closed || open, state = closed ? closed.s : open && open.stato;
        if (!e) fail(match + " non c'e' tra le partite di " + name);
        if (!(closed && ["incompleta", "contestata", "daRiconciliare", "scaduta"].indexOf(state) >= 0 || open && state === "contestata"))
            fail(match + " e' \"" + (state || "aperta") + "\": " + (state === "incompleta" || state === "inVerifica"
                ? "puo' ancora confermarsi da sola, si decide quando e' chiusa (7 giorni)" : "l'ha gia' decisa il server"));
        if (log.some(function (x) { return x.chiave === "partita:" + match; })) fail(match + " e' gia' stata decisa (vedi stato)");
        var eco = JSON.parse(((await call("Server", "GetTitleData", { Keys: ["Economia"] })).Data || {}).Economia || "{}").partita || {};
        var full = choice === "nulla" ? 0 : choice === "vinta" ? (eco.vittoria != null ? eco.vittoria : 40) : (eco.sconfitta != null ? eco.sconfitta : 20);
        var amount = Math.max(0, full - (e.m | 0)), write = {};
        if (amount > 0) write["Cons_e" + match] = JSON.stringify({ id: "e" + match, t: new Date().toISOString(), CO: amount, GE: 0, stato: "attesa",
            nota: "esito " + choice + " " + today });
        log.push({ chiave: "partita:" + match, data: today, esito: choice, prima: JSON.stringify(e) });
        // Prima l'elenco, poi la consegna (come riconcilia): mai due volte da qui.
        await call("Server", "UpdateUserInternalData", { PlayFabId: id, Data: (function (d) { d[RECONCILED] = JSON.stringify(log.slice(-100)); return d; })({}) });
        if (amount > 0) await call("Server", "UpdateUserReadOnlyData", { PlayFabId: id, Data: write });
        console.log(name + ": " + match + " (" + state + ") decisa " + choice + (amount > 0 ? ", " + amount + " CO arrivano alla prossima apertura dell'app"
            : ", niente da accreditare"));
    },

    deploy: async function () {
        writable();
        var src = fs.readFileSync(path.join(__dirname, "../CloudScript/51.js"), "utf8").replace("var QA = false;", "var QA = true;");
        var r = await call("Admin", "UpdateCloudScript", { Files: [{ Filename: "51.js", FileContents: src }], Publish: true });
        console.log("51.js pubblicato sul titolo di sviluppo: versione " + r.Version + ", revisione " + r.Revision);
    },

    prepara: async function (name, day) {
        writable(name);
        var id = await player(name), n = Math.max(1, Math.min(7, parseInt(day || "1", 10) || 1));
        // Giorno N oggi = giorno N-1 riscattato ieri (giorno 1 = nessuna serie).
        var premi = n > 1 ? JSON.stringify({ giorno: n - 1, ultimo: cs.previousDate(today) }) : null;
        var ro = await call("Server", "GetUserReadOnlyData", { PlayFabId: id, Keys: ["Posta"] });
        var posta = JSON.parse(value(ro, "Posta") || "[]").filter(function (m) { return String(m.id || "").indexOf("qa-") !== 0; });
        var t = Date.now();
        posta.push({ id: "qa-" + t + "-a", tipo: "team", titolo: "Prova QA: monete e gemme", testo: "Messaggio di prova.", data: today,
            allegati: [{ tipo: "monete", quantita: 100 }, { tipo: "gemme", quantita: 5 }] });
        posta.push({ id: "qa-" + t + "-b", tipo: "team", titolo: "Prova QA: forziere", testo: "Messaggio di prova.", data: today,
            allegati: [{ tipo: "forziere", colore: "verde" }] });
        var data = { Posta: JSON.stringify(posta) };
        if (premi) data.Premi = premi;
        await call("Server", "UpdateUserReadOnlyData", { PlayFabId: id, Data: data, KeysToRemove: premi ? ["Tutorial"] : ["Tutorial", "Premi"] });
        await call("Server", "UpdateUserInternalData", { PlayFabId: id, Data: {}, KeysToRemove: ["Tutorial", "QAGuasto"] });
        console.log(name + ": premio del giorno " + n + " da riscattare, tutorial +200 da riscattare, 2 messaggi con premio, nessun guasto");
    },

    riscattato: async function (name) {
        writable(name);
        var id = await player(name);
        await call("Server", "UpdateUserReadOnlyData", { PlayFabId: id, Data: { Premi: JSON.stringify({ giorno: 1, ultimo: today }) } });
        console.log(name + ": premio di oggi gia' riscattato");
    },

    guasto: async function (name, kind) {
        writable();
        if (kind !== "nessuno" && FAULTS.indexOf(kind) < 0) fail("guasto: " + FAULTS.join(" | ") + " | nessuno");
        var id = await player(name);
        await call("Server", "UpdateUserInternalData", kind === "nessuno"
            ? { PlayFabId: id, Data: {}, KeysToRemove: ["QAGuasto"] } : { PlayFabId: id, Data: { QAGuasto: kind } });
        console.log(name + ": guasto " + kind);
    }
};

var args = process.argv.slice(2), cmd = commands[args[0]];
if (!cmd) fail("comandi: " + Object.keys(commands).join(", ") + " (vedi l'inizio di questo file)");
cmd.apply(null, args.slice(1)).catch(function (e) { fail(String(e && e.stack || e)); });
