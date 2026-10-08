// Prepara il file da caricare su PlayFab: node Server/CloudScript/carica.js
// Scrive 51.carica.js (= 51.js con il segreto dei nomi dei webhook al posto di __HOOK__) e stampa i percorsi per il pannello Photon.
// Il segreto sta in segreto.txt (fuori da git come 51.carica.js: il repository e' pubblico). Un segreto nuovo solo con --nuovo
// (anche se segreto.txt c'e' gia', per cambiarlo se e' uscito): cambia i nomi da mettere nel pannello Photon e i nomi dei dati delle
// partite in corso. Niente copia del vecchio: si ritrova nei nomi del pannello Photon.
var fs = require("fs"), path = require("path"), crypto = require("crypto");
var dir = __dirname, file = path.join(dir, "segreto.txt"), nuovo = process.argv[2] === "--nuovo";
if (nuovo || !fs.existsSync(file)) {
    if (!nuovo)
        throw new Error("segreto.txt mancante: rimettilo copiando il segreto dai nomi nel pannello Photon (la parte dopo 'RoomCreated_'), " +
            "oppure usa --nuovo e poi aggiorna il pannello Photon subito dopo il Deploy");
    fs.writeFileSync(file, crypto.randomBytes(12).toString("hex") + "\n");
    console.log("NUOVO segreto: dopo Upload e Deploy di 51.carica.js aggiorna subito i nomi nel pannello Photon");
}
var secret = fs.readFileSync(file, "utf8").trim();
if (!/^[0-9a-f]{16,}$/i.test(secret)) throw new Error("segreto.txt non valido: solo cifre esadecimali, almeno 16");
var hook = "_" + secret;
var src = fs.readFileSync(path.join(dir, "51.js"), "utf8");
if (src.indexOf('var HOOK = "__HOOK__";') < 0) throw new Error("51.js: segnaposto __HOOK__ non trovato");
fs.writeFileSync(path.join(dir, "51.carica.js"), src.replace('var HOOK = "__HOOK__";', 'var HOOK = "' + hook + '";'));
console.log("Da caricare su PlayFab: Server/CloudScript/51.carica.js\nPannello Photon, Webhooks:");
console.log("  PathCreate RoomCreated" + hook + "\n  PathJoin RoomJoined" + hook + "\n  PathLeave RoomLeft" + hook + "\n  PathClose RoomClosed\n  PathBeforeJoin: vuoto");
