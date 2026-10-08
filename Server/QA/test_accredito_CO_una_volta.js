// 51 — Test singolo di accredito CO (+1) per UN account QA autorizzato.
// Copiare questo file in Server/QA. Richiede Node.js 18+ e qa.json nella stessa cartella.
// Non esegue retry e impedisce una seconda esecuzione sullo stesso PC.
'use strict';
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const readline = require('node:readline/promises');

const configurationFile = path.join(__dirname, 'qa.json');
if (!fs.existsSync(configurationFile)) {
  console.error('ERRORE: manca Server/QA/qa.json nella stessa cartella dello script.');
  process.exit(1);
}
let cfg;
try { cfg = JSON.parse(fs.readFileSync(configurationFile, 'utf8')); }
catch { console.error('ERRORE: qa.json non e un JSON valido.'); process.exit(1); }
const title = String(cfg.titleId || '').toUpperCase();
const players = cfg.accountProva;
if (title !== '10A53D' || !cfg.secretKey || !Array.isArray(players) || players.length !== 1 || !/^[0-9A-F]{16}$/.test(String(players[0]).toUpperCase())) {
  console.error('ERRORE: attesi titleId 10A53D, secretKey e UN SOLO PlayFab ID valido in accountProva. Nessuna modifica eseguita.');
  process.exit(1);
}
const userId = String(players[0]).toUpperCase();
const endpoint = `https://${title}.playfabapi.com`;
const lockHash = crypto.createHash('sha256').update(`${title}/${userId}/CO-test-one`).digest('hex').slice(0, 18);
const lockFile = path.join(os.tmpdir(), `51-test-accredito-CO-${lockHash}.lock`);
async function api(route, body) {
  const r = await fetch(endpoint + route, {
    method: 'POST',
    headers: {'Content-Type': 'application/json', 'X-SecretKey': cfg.secretKey},
    body: JSON.stringify(body),
    signal: AbortSignal.timeout(15000),
  });
  const j = await r.json();
  if (!r.ok || j.code !== 200) {
    const err = new Error(`${route}: ${j.error || 'API_ERROR'} (${j.errorCode || r.status}) ${j.errorMessage || ''}`);
    err.name = 'PlayFabError';
    throw err;
  }
  return j.data;
}
async function getBalance() {
  const inventory = await api('/Server/GetUserInventory', {PlayFabId: userId});
  return (inventory.VirtualCurrency || {}).CO ?? 0;
}
(async () => {
  console.log('TEST PLAYFAB +1 CO (una sola volta, account QA)');
  console.log('Titolo:', title, '| Account:', `${userId.slice(0, 4)}...${userId.slice(-4)}`);
  const before = await getBalance();
  console.log('Saldo CO prima:', before);
  if (before !== 0) {
    console.log('STOP: il saldo iniziale non e 0. Nessun accredito eseguito.');
    return;
  }
  if (fs.existsSync(lockFile)) {
    console.log('STOP: risulta gia tentato questo test sul PC. Non ripeterlo per evitare doppi accrediti.');
    return;
  }
  const rl = readline.createInterface({input: process.stdin, output: process.stdout});
  const answer = await rl.question('Per accreditare UNA SOLA moneta scrivi esattamente SI: ');
  rl.close();
  if (answer.trim() !== 'SI') { console.log('Annullato. Nessuna modifica.'); return; }
  // Marker creato PRIMA della chiamata, per evitare ritentativi accidentali in esiti incerti.
  fs.writeFileSync(lockFile, `Tentativo +1 CO; titolo ${title}; account ${userId}; data ${new Date().toISOString()}\n`, {flag:'wx'});
  let granted;
  try {
    granted = await api('/Server/AddUserVirtualCurrency', {PlayFabId: userId, VirtualCurrency:'CO', Amount:1});
    console.log('API OK: BalanceChange =', granted.BalanceChange, ', Balance =', granted.Balance);
  } catch (e) {
    console.log('ERRORE/ESITO INCERTO:', e.message);
    console.log('NON ripetere il comando. Controlla prima il saldo con qa.js stato.');
  }
  try {
    const after = await getBalance();
    console.log('Saldo CO dopo:', after);
    console.log(after === 1 && granted && granted.BalanceChange === 1 ? 'TEST SUPERATO (+1 CO)' : 'TEST DA VALUTARE: nessun altro tentativo automatico');
  } catch (e) {
    console.log('Lettura saldo finale non riuscita:', e.message);
    console.log('NON ripetere il comando; usa qa.js stato per verificare.');
  }
})().catch(e => { console.error('ERRORE:', e.message); process.exitCode = 1; });
