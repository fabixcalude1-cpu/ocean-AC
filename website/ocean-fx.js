/* =====================================================================
   OCEAN FX — molten backdrops, the purple cursor canvas and the
   scanning-stage UI. Loaded by main.js; no dependencies.
   ---------------------------------------------------------------------
     OceanFX.molten()                 mount every [data-molten] + hero
     OceanFX.cursor()                 canvas ribbon + particles + burst
     OceanFX.scanStage(el, opts)      molten scanner panel (live state)
     window.ocScanStage(el, opts)     convenience alias
   ===================================================================== */
(function (global) {
  'use strict';

  // Honour the OS "reduce motion" switch, but let the site opt back in:
  //   ?motion=1 in the URL, or localStorage['oc-motion'] === 'on'
  var forced = false;
  try {
    forced = /[?&]motion=1/.test(global.location.search) ||
      global.localStorage.getItem('oc-motion') === 'on';
  } catch (e0) {}
  var reduced = false;
  try {
    reduced = !forced && global.matchMedia && global.matchMedia('(prefers-reduced-motion: reduce)').matches;
  } catch (e) {}
  var fine = true;
  try {
    fine = !global.matchMedia || global.matchMedia('(pointer: fine)').matches;
  } catch (e) {}

  var MOLTEN_SRC = '/js/molten-metal.js';
  var moltenLoading = null;

  function loadMolten(cb) {
    if (global.OceanMolten) { cb(global.OceanMolten); return; }
    if (!moltenLoading) {
      moltenLoading = new Promise(function (resolve, reject) {
        var s = document.createElement('script');
        s.src = MOLTEN_SRC;
        s.async = true;
        s.onload = function () { resolve(global.OceanMolten || null); };
        s.onerror = function () { reject(new Error('molten-metal.js failed to load')); };
        document.head.appendChild(s);
      }).catch(function () { return null; });
    }
    moltenLoading.then(function (m) { if (cb) cb(m); });
  }

  function el(tag, cls, html) {
    var n = document.createElement(tag || 'div');
    if (cls) n.className = cls;
    if (html != null) n.innerHTML = html;
    return n;
  }

  /* =================================================================
     01 — MOLTEN BACKDROPS
     ================================================================= */

  function hostOf(node) {
    var host = node.parentNode;
    while (host && host !== document.body &&
      (!host.classList || (!host.classList.contains('oc-molten-host') && host.getAttribute('data-molten-host') === null))) {
      if (host.tagName === 'SECTION' || host.tagName === 'MAIN' || host.tagName === 'HEADER' || host.classList.contains('hero') || host.className && /hero/i.test(host.className)) break;
      host = host.parentNode;
    }
    return host || node.parentNode;
  }

  function mountNode(node, molten) {
    if (!node || node.getAttribute('data-molten-ready') === '1') return null;
    node.setAttribute('data-molten-ready', '1');
    var opts = molten.readDataset(node);
    var host = hostOf(node);
    if (host && host.classList) host.classList.add('oc-molten-host');
    var inst = molten.mount(node, opts);
    return inst;
  }

  // find the page's hero-ish block and give it a molten light behind it
  function autoHero(molten) {
    if (reduced) return null;
    if (document.querySelector('.oc-molten')) return null;
    var candidates = document.querySelectorAll('header h1, [class*="hero"] h1, main h1, h1');
    var h1 = candidates[0];
    if (!h1) return null;
    var block = h1.parentNode;
    var depth = 0;
    while (block && block !== document.body && depth < 4) {
      var r = block.getBoundingClientRect ? block.getBoundingClientRect() : null;
      if (r && r.width > 240 && r.height > 90) break;
      block = block.parentNode;
      depth++;
    }
    if (!block || block === document.body) return null;
    block.classList.add('oc-molten-host');
    var layer = el('div', 'oc-molten oc-molten-hero oc-molten-vignette oc-molten-scrim');
    layer.setAttribute('data-preset', 'hero');
    layer.setAttribute('aria-hidden', 'true');
    block.insertBefore(layer, block.firstChild);
    return molten.mount(layer, { preset: 'hero' });
  }

  function moltenInit() {
    loadMolten(function (molten) {
      if (!molten) return;
      var nodes = document.querySelectorAll('[data-molten]');
      for (var i = 0; i < nodes.length; i++) mountNode(nodes[i], molten);
      if (!nodes.length) autoHero(molten);
      else autoHero(molten);
    });
  }

  /* =================================================================
     02 — CURSOR: purple ribbon + particles + shockwave (canvas)
     ================================================================= */

  function cursorInit() {
    if (!fine || reduced) return;
    if (document.querySelector('.oc-cursor-canvas')) return;

    var canvas = el('canvas', 'oc-cursor-canvas');
    canvas.setAttribute('aria-hidden', 'true');
    document.body.appendChild(canvas);
    var ctx = canvas.getContext('2d');
    if (!ctx) return;

    var dpr = Math.min(global.devicePixelRatio || 1, 2);
    var w = 0, h = 0;
    function size() {
      w = global.innerWidth;
      h = global.innerHeight;
      canvas.width = Math.floor(w * dpr);
      canvas.height = Math.floor(h * dpr);
      canvas.style.width = w + 'px';
      canvas.style.height = h + 'px';
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    }
    size();
    global.addEventListener('resize', size, { passive: true });

    var mx = w / 2, my = h / 2;
    var pmx = mx, pmy = my;
    var speed = 0;
    var trail = [];        // ribbon history
    var particles = [];
    var rings = [];
    var MAX_PARTS = 320;
    var lastEmit = 0;
    var hoverKey = null;

    var TYPES = ['ember', 'spark', 'orb', 'shard', 'rune'];

    function emit(x, y, type, opts) {
      if (particles.length > MAX_PARTS) particles.shift();
      opts = opts || {};
      var p = {
        x: x, y: y,
        vx: opts.vx != null ? opts.vx : (Math.random() - 0.5) * 1.4,
        vy: opts.vy != null ? opts.vy : (Math.random() - 0.5) * 1.4,
        life: 0,
        max: opts.max || (0.5 + Math.random() * 0.9),
        size: opts.size || (1.2 + Math.random() * 2.4),
        type: type,
        spin: Math.random() * Math.PI * 2,
        spinV: (Math.random() - 0.5) * 0.16,
        hue: 266 + Math.random() * 12,
        l: 58 + Math.random() * 16
      };
      if (type === 'ember') { p.vy = -0.25 - Math.random() * 0.6; p.vx *= 0.5; p.max = 0.9 + Math.random() * 1.2; }
      if (type === 'spark') { p.vx *= 3.4; p.vy *= 3.4; p.max = 0.24 + Math.random() * 0.26; p.size = 0.9 + Math.random() * 1.3; }
      if (type === 'orb') { p.max = 1.1 + Math.random() * 0.8; p.size = 5 + Math.random() * 9; }
      if (type === 'shard') { p.max = 0.7 + Math.random() * 0.5; p.size = 5 + Math.random() * 9; }
      particles.push(p);
      return p;
    }

    function burst(x, y) {
      rings.push({ x: x, y: y, r: 4, max: 90 + Math.random() * 40, life: 0, maxLife: 0.62, w: 1.6 });
      rings.push({ x: x, y: y, r: 2, max: 150, life: 0, maxLife: 0.9, w: 0.9 });
      for (var i = 0; i < 18; i++) {
        var a = (i / 18) * Math.PI * 2 + Math.random() * 0.3;
        var d = 2.4 + Math.random() * 3.4;
        emit(x, y, i % 3 === 0 ? 'shard' : 'spark', { vx: Math.cos(a) * d, vy: Math.sin(a) * d });
      }
      for (var j = 0; j < 8; j++) emit(x, y, 'ember');
    }

    function onMove(e) {
      pmx = mx; pmy = my;
      mx = e.clientX; my = e.clientY;
      speed = Math.min(60, Math.hypot(mx - pmx, my - pmy));

      var t = e.target;
      var interactive = t && t.closest && t.closest('a,button,input,textarea,select,[role="button"],.oc-hover,label');
      var key = interactive ? (interactive.tagName + ':' + (interactive.className || '').toString().slice(0, 40)) : null;
      if (key !== hoverKey) {
        hoverKey = key;
        if (key) for (var i = 0; i < 7; i++) emit(mx, my, 'rune');
      }
    }

    function onDown(e) { burst(e.clientX, e.clientY); }

    document.addEventListener('mousemove', onMove, { passive: true });
    document.addEventListener('mousedown', onDown, { passive: true });

    var last = performance.now();
    function frame(now) {
      var dt = Math.min(0.05, (now - last) / 1000);
      last = now;

      ctx.clearRect(0, 0, w, h);

      // ---- ribbon (the always-on purple streak) ----
      trail.push({ x: mx, y: my, t: now });
      while (trail.length && now - trail[0].t > 420) trail.shift();

      if (trail.length > 2) {
        ctx.globalCompositeOperation = 'lighter';
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';
        for (var pass = 0; pass < 2; pass++) {
          var wide = pass === 0;
          ctx.beginPath();
          ctx.moveTo(trail[0].x, trail[0].y);
          for (var k = 1; k < trail.length - 1; k++) {
            var a = trail[k], b = trail[k + 1];
            var mid = { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
            ctx.quadraticCurveTo(a.x, a.y, mid.x, mid.y);
          }
          ctx.strokeStyle = wide ? 'rgba(100,50,200,0.14)' : 'rgba(148,80,225,0.45)';
          ctx.lineWidth = wide ? 14 : 2.2;
          ctx.shadowBlur = wide ? 22 : 10;
          ctx.shadowColor = 'rgba(130,70,220,0.75)';
          ctx.stroke();
        }
        ctx.shadowBlur = 0;
      }

      // ---- emit along the motion ----
      if (now - lastEmit > 16) {
        lastEmit = now;
        var n = speed > 26 ? 3 : speed > 10 ? 2 : 1;
        for (var e2 = 0; e2 < n; e2++) {
          var type = speed > 22 ? (Math.random() < 0.5 ? 'spark' : 'ember') : (Math.random() < 0.6 ? 'ember' : 'orb');
          emit(mx + (Math.random() - 0.5) * 10, my + (Math.random() - 0.5) * 10, type);
        }
      }

      // ---- particles ----
      ctx.globalCompositeOperation = 'lighter';
      for (var i = particles.length - 1; i >= 0; i--) {
        var p = particles[i];
        p.life += dt;
        if (p.life >= p.max) { particles.splice(i, 1); continue; }
        var t = p.life / p.max;
        var fade = 1 - t;
        p.x += p.vx;
        p.y += p.vy;
        p.vx *= 0.965;
        p.vy *= 0.965;
        if (p.type === 'ember') p.vy -= 0.012;
        p.spin += p.spinV;

        ctx.beginPath();
        if (p.type === 'shard') {
          var len = p.size * (1 + t);
          ctx.save();
          ctx.translate(p.x, p.y);
          ctx.rotate(p.spin + t * 2);
          ctx.strokeStyle = 'hsla(' + p.hue + ',' + '82%,' + p.l + '%,' + (0.75 * fade) + ')';
          ctx.lineWidth = 1.2;
          ctx.moveTo(-len, 0);
          ctx.lineTo(len, 0);
          ctx.stroke();
          ctx.restore();
        } else if (p.type === 'orb') {
          var rad = p.size * (0.4 + t * 1.5);
          var grd = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, rad);
          grd.addColorStop(0, 'hsla(' + p.hue + ',90%,' + p.l + '%,' + (0.35 * fade) + ')');
          grd.addColorStop(1, 'hsla(' + p.hue + ',90%,60%,0)');
          ctx.fillStyle = grd;
          ctx.fillRect(p.x - rad, p.y - rad, rad * 2, rad * 2);   /* square orb */
        } else if (p.type === 'rune') {
          ctx.save();
          ctx.translate(p.x, p.y);
          ctx.rotate(p.spin + t * 5);
          ctx.strokeStyle = 'hsla(' + p.hue + ',92%,' + p.l + '%,' + (0.85 * fade) + ')';
          ctx.lineWidth = 0.9;
          var s = p.size * 1.7;
          ctx.moveTo(0, -s); ctx.lineTo(s, 0); ctx.lineTo(0, s); ctx.lineTo(-s, 0); ctx.closePath();
          ctx.stroke();
          ctx.restore();
        } else if (p.type === 'spark') {
          ctx.strokeStyle = 'hsla(' + p.hue + ',95%,' + p.l + '%,' + (0.8 * fade) + ')';
          ctx.lineWidth = p.size * fade;
          ctx.moveTo(p.x, p.y);
          ctx.lineTo(p.x - p.vx * 4.2, p.y - p.vy * 4.2);
          ctx.stroke();
        } else {
          var flick = 0.6 + 0.4 * Math.sin(p.life * 22 + p.spin);
          ctx.fillStyle = 'hsla(' + p.hue + ',92%,' + p.l + '%,' + (0.7 * fade * flick) + ')';
          var ps = p.size * fade;
          ctx.fillRect(p.x - ps, p.y - ps, ps * 2, ps * 2);        /* square spark */
        }
      }

      // ---- shockwaves ----
      for (var r = rings.length - 1; r >= 0; r--) {
        var ring = rings[r];
        ring.life += dt;
        var rt = ring.life / ring.maxLife;
        if (rt >= 1) { rings.splice(r, 1); continue; }
        var rr = ring.r + (ring.max - ring.r) * (1 - Math.pow(1 - rt, 2.2));
        ctx.beginPath();
        ctx.strokeStyle = 'hsla(268,90%,65%,' + (0.5 * (1 - rt)) + ')';
        ctx.lineWidth = ring.w * (1 - rt) + 0.3;
        ctx.strokeRect(ring.x - rr, ring.y - rr, rr * 2, rr * 2);   /* square shockwave */
        ctx.stroke();
      }

      // ---- pointer core light ----
      var core = ctx.createRadialGradient(mx, my, 0, mx, my, 42);
      core.addColorStop(0, 'hsla(270,95%,82%,0.28)');
      core.addColorStop(0.45, 'hsla(268,90%,62%,0.11)');
      core.addColorStop(1, 'hsla(266,85%,55%,0)');
      ctx.fillStyle = core;
      ctx.fillRect(mx - 46, my - 46, 92, 92);                       /* square core light */

      ctx.globalCompositeOperation = 'source-over';
      requestAnimationFrame(frame);
    }
    requestAnimationFrame(frame);

    document.addEventListener('visibilitychange', function () {
      if (document.hidden) { particles.length = 0; rings.length = 0; trail.length = 0; }
    });
  }

  /* =================================================================
     03 — SCANNING STAGE (molten + sweep + progress + live stream)
     ================================================================= */

  var STREAM_POOL = [
    'mapping module list', 'walking driver objects', 'hashing loaded images', 'reading handle table',
    'checking kernel notify callbacks', 'enumerating thread start addresses', 'verifying PE headers',
    'inspecting stack frames', 'sweeping memory regions', 'resolving import thunks',
    'comparing code signatures', 'profiling module timings', 'checking hidden pages',
    'tracing APC queue', 'validating debug flags', 'scanning for manual maps'
  ];

  var FINDING_POOL = [
    ['clean', 'no anomaly in module list'],
    ['clean', 'import table intact'],
    ['info', 'large page region mapped'],
    ['clean', 'stack frames consistent'],
    ['warn', 'thread start outside module'],
    ['clean', 'driver signature valid'],
    ['info', 'notify callback registered by driver'],
    ['clean', 'no manual mapped image found']
  ];

  function scanStage(node, opts) {
    if (!node) return null;
    opts = opts || {};
    if (node.__ocScanStage) {
      if (opts.state) node.__ocScanStage.setState(opts.state, opts);
      return node.__ocScanStage;
    }

    node.classList.add('oc-molten-host', 'oc-scan');
    if (!node.querySelector('.oc-molten')) {
      var layer = el('div', 'oc-molten oc-molten-scan oc-molten-scrim');
      layer.setAttribute('preset', 'scanner');
      layer.setAttribute('data-preset', 'scanner');
      layer.setAttribute('aria-hidden', 'true');
      node.insertBefore(layer, node.firstChild);
      loadMolten(function (molten) { if (molten) molten.mount(layer, { preset: 'scanner' }); });
    }

    var sweep = el('div', 'oc-scan-sweep');
    sweep.setAttribute('aria-hidden', 'true');
    node.appendChild(sweep);

    var hud = el('div', 'oc-scan-hud');
    hud.innerHTML =
      '<div class="oc-scan-top">' +
      '  <span class="oc-scan-state"><i class="oc-scan-dot"></i><b data-scan-label>SCANNING</b></span>' +
      '  <span class="oc-scan-src" data-scan-src>SYNTH</span>' +
      '  <span class="oc-scan-pct" data-scan-pct>0%</span>' +
      '</div>' +
      '<div class="oc-scan-bar"><i data-scan-fill></i></div>' +
      '<div class="oc-scan-meta"><span data-scan-stage>warming up</span>' +
      '  <span data-scan-host></span></div>' +
      '<div class="oc-scan-stream" data-scan-stream aria-live="polite"></div>';
    node.appendChild(hud);

    var label = hud.querySelector('[data-scan-label]');
    var srcEl = hud.querySelector('[data-scan-src]');
    var pctEl = hud.querySelector('[data-scan-pct]');
    var fill = hud.querySelector('[data-scan-fill]');
    var stream = hud.querySelector('[data-scan-stream]');
    var stageEl = hud.querySelector('[data-scan-stage]');
    var hostEl = hud.querySelector('[data-scan-host]');
    var timer = null;
    var feedTimer = null;
    var progress = 0;
    var target = opts.progress != null ? opts.progress : 0;
    var liveSeen = 0;
    var isLive = false;
    var wantLive = opts.live !== false;
    var code = (opts.code || '').toUpperCase();

    function esc(s) {
      return String(s == null ? '' : s)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function line(kind, text) {
      var row = el('div', 'oc-scan-line oc-scan-' + kind);
      row.innerHTML = '<span class="oc-scan-tag">' + esc(kind).toUpperCase() + '</span><span>' + esc(text) + '</span>';
      stream.appendChild(row);
      while (stream.children.length > 7) stream.removeChild(stream.firstChild);
    }

    function pushLines(list) {
      if (!list || !list.length) return;
      for (var i = 0; i < list.length; i++) {
        var l = list[i] || {};
        line(l.kind || 'info', l.text || '');
      }
      liveSeen += list.length;
    }

    function paint(shown) {
      shown = Math.max(0, Math.min(100, Math.round(shown)));
      pctEl.textContent = shown + '%';
      fill.style.width = shown + '%';
    }

    /* ---- synthetic fallback: keeps the stage alive with no scanner -------- */
    function tick() {
      if (isLive) return;
      target = Math.min(99, target + 0.5 + Math.random() * 3.4);
      progress += (target - progress) * 0.28;
      paint(progress);
      if (stageEl) stageEl.textContent = STREAM_POOL[Math.floor(Math.random() * STREAM_POOL.length)];
      if (Math.random() < 0.55) {
        if (Math.random() < 0.78) line('step', STREAM_POOL[Math.floor(Math.random() * STREAM_POOL.length)]);
        else { var f = FINDING_POOL[Math.floor(Math.random() * FINDING_POOL.length)]; line(f[0], f[1]); }
      }
    }

    /* ---- real feed: /api/scanner/live, POSTed by the desktop scanner ------ */
    function applyLive(rec) {
      if (!rec) return;
      isLive = true;
      if (stageEl && rec.stage) stageEl.textContent = rec.stage;
      if (hostEl) hostEl.textContent = [rec.player, rec.pcName].filter(Boolean).join(' · ');

      var list = rec.lines || [];
      if (list.length > liveSeen) pushLines(list.slice(liveSeen));
      else if (liveSeen > list.length) liveSeen = list.length;

      var done = rec.state === 'complete' || rec.state === 'error';
      var pct = done ? (rec.state === 'complete' ? 100 : rec.progress) : rec.progress;
      progress = pct;
      paint(pct);

      // A real run has been observed for this code: never fall back to the
      // synthetic stream again, or the panel would start inventing progress
      // for a scan that actually stopped reporting.
      node.classList.toggle('oc-scan-stale', rec.state === 'stale');
      node.classList.toggle('oc-scan-live', true);

      if (rec.state === 'complete') {
        if (timer) { clearInterval(timer); timer = null; }
        node.classList.remove('oc-scan-active');
        node.classList.add('oc-scan-done');
        if (label) label.textContent = 'COMPLETE';
      } else if (rec.state === 'error') {
        if (timer) { clearInterval(timer); timer = null; }
        node.classList.remove('oc-scan-active');
        if (label) label.textContent = 'ERROR';
      } else if (rec.state === 'stale') {
        if (timer) { clearInterval(timer); timer = null; }
        if (srcEl) { srcEl.textContent = 'STALE'; srcEl.classList.remove('is-live'); }
        if (label) label.textContent = 'WAITING';
      } else {
        node.classList.add('oc-scan-active');
        node.classList.remove('oc-scan-done');
        if (srcEl) { srcEl.textContent = 'LIVE'; srcEl.classList.add('is-live'); }
        if (label) label.textContent = 'SCANNING';
      }
    }

    function pollFeed() {
      if (!wantLive || !global.fetch) return;
      global.fetch('/api/scanner/live', { credentials: 'same-origin' })
        .then(function (r) { return r.ok ? r.json() : null; })
        .catch(function () { return null; })
        .then(function (d) {
          if (!d || !d.scans || !d.scans.length) return;
          var pick = null;
          for (var i = 0; i < d.scans.length; i++) {
            var c = (d.scans[i].code || '').toUpperCase();
            if (!code || c === code) { pick = d.scans[i]; break; }
          }
          if (!pick) pick = d.scans[0];
          applyLive(pick);
        });
    }

    function start() {
      node.classList.add('oc-scan-active');
      node.classList.remove('oc-scan-done');
      label.textContent = 'SCANNING';
      if (!timer) timer = setInterval(tick, 620);
      tick();
      if (wantLive) {
        pollFeed();
        if (!feedTimer) feedTimer = setInterval(pollFeed, 2000);
      }
    }

    function stop(state) {
      if (timer) { clearInterval(timer); timer = null; }
      node.classList.toggle('oc-scan-active', state === 'scanning');
      node.classList.toggle('oc-scan-done', state === 'complete');
      if (state === 'complete') {
        progress = 100;
        paint(100);
        if (label) label.textContent = 'COMPLETE';
        if (!isLive) line('done', 'scan finished — report ready');
      } else if (state === 'idle') {
        if (label) label.textContent = 'IDLE';
        if (feedTimer) { clearInterval(feedTimer); feedTimer = null; }
      }
    }

    var api = {
      node: node,
      setState: function (state, o) {
        if (state === 'scanning') start();
        else stop(state);
        if (o && o.progress != null) target = o.progress;
        return api;
      },
      // Push findings from any other source (replay, replay of history, ...)
      feed: function (list) { pushLines(Array.isArray(list) ? list : [list]); return api; },
      setStage: function (text) { if (stageEl) stageEl.textContent = text; return api; },
      isLive: function () { return isLive; },
      isScanning: function () { return !!timer; },
      destroy: function () {
        stop('idle');
        if (feedTimer) { clearInterval(feedTimer); feedTimer = null; }
        hud.remove();
        sweep.remove();
        delete node.__ocScanStage;
      }
    };

    node.__ocScanStage = api;
    api.setState(opts.state || 'scanning');
    return api;
  }

  function autoScanStages() {
    var nodes = document.querySelectorAll('[data-scan-stage]');
    for (var i = 0; i < nodes.length; i++) {
      var st = nodes[i].getAttribute('data-scan-stage') || 'scanning';
      scanStage(nodes[i], {
        state: st === 'auto' ? 'scanning' : st,
        code: nodes[i].getAttribute('data-scan-code') || ''
      });
    }
  }

  function init() {
    moltenInit();
    cursorInit();
    autoScanStages();
  }

  global.OceanFX = {
    init: init,
    molten: moltenInit,
    cursor: cursorInit,
    scanStage: scanStage,
    loadMolten: loadMolten
  };
  global.ocScanStage = scanStage;
})(typeof window !== 'undefined' ? window : this);
