/**
 * OCEAN EMBED BUILD
 * ---------------------------------------------------------------------
 * Bundles the website assets (dashboard CSS + main.js renderer) and a
 * snapshot of the real API data into ocean_runtime.js so the desktop
 * client can render the full dashboard from a WebView2 / WebBrowser
 * control even when the localhost server is not running.
 *
 *   node embed_website.js
 *
 * Output: ocean_runtime.js (single file, no external dependency).
 *
 * Flat layout: all website files (style.css, ocean-v2.css, main.js, *.json)
 * live in the same folder as this script, so they are read directly from
 * __dirname.
 */
const fs = require("fs");
const path = require("path");

const website = __dirname;

function read(f) {
  return fs.readFileSync(path.join(website, f), "utf8");
}
function readJson(f) {
  try { return JSON.parse(fs.readFileSync(path.join(website, f), "utf8")); } catch (e) { return []; }
}

const cssStyle = read("style.css");
let cssV2 = read("ocean-v2.css");
const mainJS = read("main.js");

// ---- live data snapshot (mirrors the server's API shapes) ----
const keys = readJson("keys.json");
const scans = readJson("scans.json");
const accounts = readJson("accounts.json");

function pinFromKey(k) { return ((k && k.key) || "").trim(); }
function countDetections(s) {
  let n = 0;
  const d = s && s.detections;
  if (d && typeof d === "object") {
    Object.keys(d).forEach((cat) => {
      const arr = d[cat];
      if (Array.isArray(arr)) n += arr.length;
    });
  }
  return n;
}

const me = accounts[0] || { id: "acc-embedded-demo", username: "jeanclap", createdAt: "2026-01-01T00:00:00.000Z" };
const myKeys = keys.filter((k) => !k.ownerId || k.ownerId === me.id);
const myScans = scans.filter((s) => !s.ownerId || s.ownerId === me.id);

const pins = myKeys
  .filter((k) => k.pin === true || k.note === "generated pin")
  .map((k) => {
    const scan = myScans.find((s) =>
      (s.pin && ("" + s.pin).toUpperCase() === pinFromKey(k).toUpperCase()) ||
      (s.keyId && s.keyId === k.id)) || null;
    let status = k.status;
    if (k.status === "active" && scan) status = "complete";
    if (status === "complete" && k.result) status = "complete";
    return {
      code: pinFromKey(k),
      id: k.id,
      status: status,
      result: scan ? (scan.status || scan.result) : (k.result || null),
      createdAt: k.createdAt || (scan && scan.timestamp) || k.expiresAt,
      game: (scan && scan.game) || k.game || "",
      player: (scan && (scan.username || scan.playerName)) || k.player || "",
      pcName: (scan && scan.pcName) || "",
      scanId: scan ? scan.id : null,
      scannedAt: scan ? scan.timestamp : (k.scannedAt || null)
    };
  });

const now = new Date();
const monthStart = new Date(now.getFullYear(), now.getMonth(), 1).getTime();
let detected = 0, cheating = 0, suspicious = 0, clean = 0, scansThisMonth = 0;
const cheatSet = new Set();
myScans.forEach((s) => {
  const t = s.timestamp ? new Date(s.timestamp).getTime() : 0;
  if (t >= monthStart) scansThisMonth++;
  detected += countDetections(s);
  const low = ((s.status || s.result || "") + "").toLowerCase();
  if (low.indexOf("cheat") > -1) cheating++;
  else if (low.indexOf("suspicious") > -1) suspicious++;
  else if (low === "clean" || low.indexOf("legit") > -1) clean++;
  const d = s.detections;
  if (d && typeof d === "object") {
    Object.keys(d).forEach((cat) => {
      const arr = d[cat];
      if (!Array.isArray(arr)) return;
      arr.forEach((it) => { const nm = it && (it.name || it.Name); if (nm) cheatSet.add(String(nm)); });
    });
  }
});

let activePins = 0, scanningPins = 0, completedPins = 0, pinsThisMonth = 0;
const pinCodes = {};
myScans.forEach((s) => { if (s.pin) pinCodes[String(s.pin).toUpperCase()] = true; });
pins.forEach((p) => {
  if (p.status === "active") { activePins++; if (pinCodes[p.code.toUpperCase()]) completedPins++; }
  else if (p.status === "scanning") scanningPins++;
  else if (p.status === "complete") completedPins++;
  const t = p.createdAt ? new Date(p.createdAt).getTime() : 0;
  if (t >= monthStart) pinsThisMonth++;
});

const stats = {
  totalPins: pins.length,
  activePins, pendingPins: 0, scanningPins, completedPins, expiredPins: 0,
  pinsThisMonth, pinsLastMonth: 0,
  totalScans: myScans.length,
  scansThisMonth, scansLastMonth: 0,
  detected, cheating, suspicious, clean,
  completedThisWeek: myScans.length,
  uniqueDetectionCount: cheatSet.size,
  uniqueGames: 1,
  profiles: 2
};

const oceanData = {
  me: { user: { id: me.id, username: me.username, createdAt: me.createdAt || "2026-01-01T00:00:00.000Z" } },
  stats: stats,
  scans: myScans,
  pins: pins
};

// ---- bundle ----
// NOTE: use JSON.stringify (not template literals) for any source payload.
// Backticks / ${ / backslashes inside the asset files are then escaped in a
// lossless, reversible way — no template-literal interpolation surprises.
function lit(x) { return JSON.stringify(x); }

let bundle = "/* =====================================================================\n";
bundle += "   OCEAN EMBEDDED RUNTIME  (AUTO-GENERATED by embed_website.js)\n";
bundle += "   Regenerate any time website files change:  node embed_website.js\n";
bundle += "   ===================================================================== */\n";
bundle += "(function () {\n";
bundle += "  'use strict';\n";
bundle += "  var oceanCSS = " + lit(cssStyle + "\n\n" + cssV2) + ";\n";
bundle += "  var oceanMainJS = " + lit(mainJS) + ";\n";
bundle += "  var oceanData = " + JSON.stringify(oceanData) + ";\n";
bundle += "  // Persist offline-created pins so a reload of the embedded page keeps them.\n";
bundle += "  try {\n";
bundle += "    var _savedPins = JSON.parse(localStorage.getItem('__oc_embedded_pins') || 'null');\n";
bundle += "    if (Array.isArray(_savedPins)) oceanData.pins = _savedPins;\n";
bundle += "  } catch (_e) {}\n";

bundle += `
  function oceanResponse(obj, status) {
    return Promise.resolve({
      ok: (status || 200) < 400,
      status: status || 200,
      json: function () { return Promise.resolve(obj); },
      text: function () { return Promise.resolve(JSON.stringify(obj)); }
    });
  }

  // Route a request that declared itself a relative /api/* URL.
  function oceanDispatch(url, opts) {
    opts = opts || {};
    var method = (opts.method || 'GET').toUpperCase();
    var u = String(url);
    var path = u.replace(/^https?:\\/\\/[^/]+/, '').split('?')[0];
    if (path === '/api/auth/me') return oceanResponse(oceanData.me);
    if (path === '/api/stats') return oceanResponse(oceanData.stats);
    if (path === '/api/scans' && method === 'GET') return oceanResponse(oceanData.scans);
    if (path === '/api/pins' && method === 'GET') return oceanResponse(oceanData.pins);
    if (path === '/api/profiles') return oceanResponse([]);
    if (method === 'POST' && path === '/api/logout') { location.reload(); return oceanResponse({ ok: true }); }
    // Offline "create pin": generate locally so the demo stays alive.
    if (method === 'POST' && path === '/api/pins') {
      var ch = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
      function part() { var s = ''; for (var i = 0; i < 4; i++) s += ch[Math.floor(Math.random() * ch.length)]; return s; }
      var code = part() + '-' + part() + '-' + part();
      var pin = {
        code: code, id: 'key-' + Date.now(),
        status: 'active', result: null,
        createdAt: new Date().toISOString(),
        game: 'FiveM', player: '', pcName: '', scanId: null, scannedAt: null
      };
      oceanData.pins.unshift(pin);
      oceanData.stats.totalPins = oceanData.pins.length;
      oceanData.stats.activePins = oceanData.pins.length;
      try { localStorage.setItem('__oc_embedded_pins', JSON.stringify(oceanData.pins)); } catch (e1) {}
      return oceanResponse([{ code: code, id: pin.id, status: 'active', createdAt: pin.createdAt, maxUses: 999 }]);
    }
    return oceanResponse({ error: 'not_found', path: path }, 404);
  }

  function oceanShimFetch() {
    if (window.__oceanShimInstalled__) return;
    window.__oceanShimInstalled__ = true;
    var realFetch = window.fetch ? window.fetch.bind(window) : null;
    window.fetch = function (url, opts) {
      var u = String(url);
      if (u.indexOf('/api/') === 0 && u.indexOf('\\/\\/') === -1) {
        // Always feed the dashboard from the embedded in-memory store.
        return oceanDispatch(url, opts);
      }
      if (realFetch) {
        try { return realFetch(url, opts); }
        catch (err) { return Promise.reject(err); }
      }
      return Promise.reject(new Error('fetch unavailable'));
    };
  }

  window.renderOceanPage = function (pageName, containerId) {
    // The dashboard renderer completely owns <body> (main.js init1to1Dashboard),
    // so the container is a no-op here — we only need CSS + JS + data.
    if (!document.getElementById('ocean-injected-css')) {
      var style = document.createElement('style');
      style.id = 'ocean-injected-css';
      style.textContent = oceanCSS;
      document.head.appendChild(style);
    }
    oceanShimFetch();
    // main.js picks the dashboard branch when the path starts with /dashboard.
    // In the embedded host the URL is file://… so force it explicitly.
    window.__OC_EMBEDDED_DASH__ = true;
    try {
      document.body.className = 'ocean-v2';
      var s = document.createElement('script');
      s.type = 'text/javascript';
      s.textContent = oceanMainJS;
      document.head.appendChild(s);
    } catch (err) {
      if (window.console && console.error) console.error('ocean embed boot failed', err);
    }
  };
})();
`;

fs.writeFileSync(path.join(__dirname, "ocean_runtime.js"), bundle);
console.log("ocean_runtime.js generated:", (bundle.length / 1024).toFixed(1) + " KB");