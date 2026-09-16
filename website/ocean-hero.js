/* =====================================================================
   OCEAN — 3D SCANNER PREVIEW + POINTER LIGHT + MOTION SWITCH
   ---------------------------------------------------------------------
   · home page: the oversized dashboard screenshot is replaced by a
     mouse-driven 3D preview of the desktop scanner (the same window the
     .exe shows), complete with its own 3D Ocean.ico logo
   · every page: per-box spotlight (--ob-mx/--ob-my), a pointer light and
     scroll reveal — all rate-limited and reduced-motion aware
   · every page: a MOTION switch that turns the ambient animation OFF as
     well as on. Hover feedback stays, because that answers the pointer
     instead of running on its own.
   ===================================================================== */
(function () {
  'use strict';

  var reduced = false;
  try {
    reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  } catch (e) {}
  /* Same escape hatch main.js uses. Without this the OS preference was the
     end of the story: `?motion=1` switched main.js over to full motion while
     this file still stamped `ob-no-motion` on <html>, whose `animation: none
     !important` won everywhere — so the site reported motion ON and animated
     nothing. One source of truth, read the same way in both files. */
  var forced = false;
  try {
    forced = /[?&]motion=1/.test(window.location.search) ||
      window.localStorage.getItem('oc-motion') === 'on';
  } catch (e1) {}

  /* ------------------------------------------------------------------
     00 — MOTION
     ------------------------------------------------------------------
     The switch is GONE from the page (it stays in the desktop app, which
     has its own MOTION control). The site now only honours the operating
     system: reduce-motion means no ambient animation. Everything that
     answers the pointer — the hero tilt, the cursor light, the per-box
     spotlight — keeps working either way, because it is not animation.
     ------------------------------------------------------------------ */
  var motionOn = !reduced || forced;

  function applyMotionClass() {
    var root = document.documentElement;
    if (motionOn) root.classList.remove('ob-no-motion');
    else root.classList.add('ob-no-motion');
  }

  /* ------------------------------------------------------------------
     01 — POINTER TRACKING (single shared listener, ~60fps)
     ------------------------------------------------------------------ */
  var pointer = { x: window.innerWidth / 2, y: window.innerHeight / 2 };
  var lastTick = 0;

  function onMove(e) {
    var now = Date.now();
    if (now - lastTick < 16) return;
    lastTick = now;
    pointer.x = e.clientX;
    pointer.y = e.clientY;

    var root = document.documentElement;
    root.style.setProperty('--mx', e.clientX + 'px');
    root.style.setProperty('--my', e.clientY + 'px');

    var light = document.querySelector('.oc-cursor-light');
    if (light) {
      light.style.transform =
        'translate3d(' + e.clientX + 'px,' + e.clientY + 'px,0)';
      if (!light.classList.contains('on')) light.classList.add('on');
    }

    paint3d();
    paintSpot(e);
  }

  function ensureLight() {
    if (document.querySelector('.oc-cursor-light')) return;
    var light = document.createElement('div');
    light.className = 'oc-cursor-light';
    light.setAttribute('aria-hidden', 'true');
    document.body.appendChild(light);
  }

  /* ------------------------------------------------------------------
     02 — PER-BOX SPOTLIGHT
     ------------------------------------------------------------------ */
  var spotHosts = [];

  var SPOT_SELECTOR =
    '.oc-card, .oc-stat, .oc-prof, .dash-card, .stat-card, .card, .panel, .oc-th-opt,' +
    '.ob-detect, .ob-tag, .key-item, [data-ob-spot], [class*="card"], [class*="Card"]';

  /* Elements the dashboard styles itself (they are safe to make relative). */
  var SELF_POSITIONED = /oc-card|oc-stat|oc-prof|dash-card|stat-card|ob-detect|ob-tag/;

  function collectSpots() {
    spotHosts = [];
    var nodes = document.querySelectorAll(SPOT_SELECTOR);
    for (var i = 0; i < nodes.length; i++) {
      var n = nodes[i];
      if (n.classList.contains('ob-spot')) {
        spotHosts.push(n);
        continue;
      }
      /* Making a static box relative can re-parent its absolutely positioned
         children, so only tag boxes that are already positioned - or that the
         dashboard owns and styles itself. */
      var ok = true;
      try {
        if (
          getComputedStyle(n).position === 'static' &&
          !SELF_POSITIONED.test(n.className || '')
        ) {
          ok = false;
        }
      } catch (e) {}
      if (!ok) continue;

      n.classList.add('ob-spot');
      spotHosts.push(n);
      n.addEventListener('mouseenter', spotEnter, { passive: true });
    }
  }

  function spotEnter(e) {
    moveSpot(e.currentTarget, e.clientX, e.clientY);
  }

  /* Only the box under the pointer needs updating, so we resolve it from the
     event target instead of measuring every host (avoids layout thrash). The
     ancestor chain is walked manually because closest() is not universal. */
  function spotUnder(target) {
    var n = target;
    while (n && n.nodeType === 1) {
      if (n.classList && n.classList.contains('ob-spot')) return n;
      n = n.parentNode;
    }
    return null;
  }

  function paintSpot(e) {
    if (!spotHosts.length) return;
    var n = spotUnder(e.target);
    if (n) moveSpot(n, e.clientX, e.clientY);
  }

  function moveSpot(n, x, y) {
    var r = n.getBoundingClientRect();
    if (!r.width) return;
    n.style.setProperty('--ob-mx', x - r.left + 'px');
    n.style.setProperty('--ob-my', y - r.top + 'px');
  }

  /* ------------------------------------------------------------------
     03 — 3D SCANNER PREVIEW (home page only)
     ------------------------------------------------------------------
     A miniature of the desktop app window: chrome, the 3D Ocean.ico logo,
     the license field, the scan bar and the module list. It tilts toward
     the pointer, and the logo inside spins on its own axis.
     ------------------------------------------------------------------ */
  var hero = null;
  var heroTarget = { rx: -10, ry: 14 };
  var heroCurrent = { rx: -10, ry: 14 };
  var heroRunning = false;

  /* The preview window is laid out at a fixed 640x400 and scaled to the host,
     so it always keeps the proportions of the real scanner window. */
  var APP_W = 640;

  function fitHero() {
    if (!hero || !hero.host) return;
    var w = hero.host.clientWidth;
    if (!w) return;
    var scale = Math.max(0.3, Math.min(1, w / APP_W));
    hero.host.style.setProperty('--ob-app-scale', scale.toFixed(3));
  }

  /* The extrusion behind the mark: eight copies of the same artwork, pushed
     back in Z and stepped from deep violet at the back to a brighter violet at
     the front, so the stack reads as one solid slab. Generated here instead of
     hand-written in the markup: keeping N spans and N colour stops in sync by
     hand is how the previous three-layer stack ended up looking like three
     separate ghosts. */
  var LOGO_LAYERS = 8;   // 8 layers x 6px of Z is a slab you can see the side of
  var LOGO_SIDES = (function () {
    var out = '';
    for (var i = LOGO_LAYERS; i >= 1; i--) {
      var f = 1 - (i - 1) / LOGO_LAYERS;          // 0 = back, 1 = front
      var c = [Math.round(26 + 58 * f), Math.round(9 + 29 * f), Math.round(60 + 90 * f)];
      out += '<span class="ob-logo-side" style="transform:translateZ(-' + (i * 6) +
             'px);background:rgb(' + c[0] + ',' + c[1] + ',' + c[2] + ')"></span>';
    }
    return out;
  })();

  function buildHero() {
    var shot = document.querySelector(
      'img[src*="/home/dashboard.png"], img[srcset*="/home/dashboard.png"]'
    );
    if (!shot) return;
    if (document.querySelector('.ob-hero-3d')) return;

    /* the screenshot wrapper is `<div><div style="opacity:1"><img…></div></div>` */
    var holder = shot.parentElement;
    var stage = holder && holder.parentElement ? holder.parentElement : holder;
    if (!stage) return;

    if (holder && holder.style) {
      holder.style.opacity = '0';
      holder.style.pointerEvents = 'none';
    }
    shot.style.display = 'none';

    var host = document.createElement('div');
    host.className = 'ob-hero-3d';
    host.setAttribute('aria-hidden', 'true');
    host.innerHTML =
      '<div class="ob-scene">' +
        '<div class="ob-object">' +
          '<div class="ob-ring"></div>' +
          '<div class="ob-app">' +
            '<div class="ob-app-bar">' +
              '<span class="ob-app-mk"></span>' +
              '<span class="ob-app-name">Ocean AC</span>' +
              '<span class="ob-app-win"><i></i><i></i></span>' +
            '</div>' +
            /* The window shows the brand mark and nothing else: the module
               readout (PREFETCH / AMCACHE / SHIMCACHE …), the license field,
               the scan bar and the floating chips were removed, and so was the
               purple tile — three violet copies of the mark behind one white
               copy read as a single extruded logo. */
            '<div class="ob-app-body">' +
              '<div class="ob-logo3d">' +
                '<span class="ob-logo-glow"></span>' +
                '<div class="ob-logo-card">' +
                  LOGO_SIDES +
                  '<span class="ob-logo-sheen" aria-hidden="true"></span>' +
                  '<img class="ob-logo-face" src="/ocean_logo.svg" alt="" loading="lazy">' +
                '</div>' +
                '<span class="ob-logo-floor" aria-hidden="true"></span>' +
              '</div>' +
            '</div>' +
          '</div>' +
        '</div>' +
      '</div>';

    stage.appendChild(host);

    var obj = host.querySelector('.ob-object');
    hero = {
      host: host,
      obj: obj,
      sheen: host.querySelector('.ob-logo-sheen')
    };

    host.addEventListener('mousemove', function (e) {
      var r = host.getBoundingClientRect();
      var nx = (e.clientX - r.left) / r.width - 0.5;
      var ny = (e.clientY - r.top) / r.height - 0.5;
      heroTarget.ry = 14 + nx * 26;
      heroTarget.rx = -10 - ny * 20;
      if (motionOn) startHeroLoop(); else applyHero(1);
    }, { passive: true });
    host.addEventListener('mouseleave', function () {
      heroTarget.rx = -10;
      heroTarget.ry = 14;
      if (motionOn) startHeroLoop(); else applyHero(1);
    });

    /* pointer-events: the host is decorative but must feel the pointer */
    host.style.pointerEvents = 'auto';
    host.style.zIndex = '1';
    stage.style.position = stage.style.position || 'relative';

    fitHero();
    applyHero(1);
  }

  /* Eases the current rotation toward the target and writes it out. Called
     both from the rAF loop and straight from the pointer handler, because
     rAF stops firing in a throttled / non-composited webview and the object
     would then never follow the mouse at all. */
  function applyHero(step) {
    if (!hero || !hero.obj) return;
    heroCurrent.rx += (heroTarget.rx - heroCurrent.rx) * step;
    heroCurrent.ry += (heroTarget.ry - heroCurrent.ry) * step;
    hero.obj.style.setProperty('--ob-rx', heroCurrent.rx.toFixed(2) + 'deg');
    hero.obj.style.setProperty('--ob-ry', heroCurrent.ry.toFixed(2) + 'deg');
    /* the specular band rides the same yaw as the mark, so the light slides
       across the face exactly while the object turns — driven from here
       rather than from an animation, which would keep running when idle */
    if (hero.sheen) {
      hero.sheen.style.setProperty('--ob-sheen', (50 + heroCurrent.ry * -1.6).toFixed(1) + '%');
    }
  }

  function paint3d() {
    if (!hero) return;
    /* window-relative parallax so the object reacts even outside the host */
    var nx = pointer.x / window.innerWidth - 0.5;
    var ny = pointer.y / window.innerHeight - 0.5;
    heroTarget.ry = 14 + nx * 24;
    heroTarget.rx = -10 - ny * 18;

    /* Ease while the pointer keeps moving; with reduce-motion on, snap so no
       animation runs at all. Either way the pose eases to the cursor instead
       of rotating on its own. */
    if (motionOn) startHeroLoop();
    else applyHero(1);
  }

  /* The pose eases toward the pointer instead of spinning on its own, so the
     object is still while the mouse is still and there is nothing to run in
     the background. The loop therefore stops by itself once the pose has
     settled: no idle rAF, which is what kept the page busy before. */
  function tickHero() {
    if (!hero) { heroRunning = false; return; }

    var dx = heroTarget.rx - heroCurrent.rx;
    var dy = heroTarget.ry - heroCurrent.ry;
    if (Math.abs(dx) < 0.02 && Math.abs(dy) < 0.02) {
      heroCurrent.rx = heroTarget.rx;
      heroCurrent.ry = heroTarget.ry;
      applyHero(1);
      heroRunning = false;
      return;
    }

    applyHero(0.16);
    window.requestAnimationFrame(tickHero);
  }

  function startHeroLoop() {
    if (heroRunning || !hero) return;
    heroRunning = true;
    window.requestAnimationFrame(tickHero);
  }

  function stopHeroLoop() {
    heroRunning = false;
  }

  /* ------------------------------------------------------------------
     04 — SCROLL REVEAL
     ------------------------------------------------------------------ */
  function initReveal() {
    var targets = document.querySelectorAll(
      'section, .oc-card, .oc-stat, .ob-detect, .dash-card, .stat-card'
    );
    var list = [];
    for (var i = 0; i < targets.length; i++) {
      var t = targets[i];
      if (t.closest('.ob-hero-3d')) continue;
      if (t.hasAttribute('data-ob-reveal')) continue;
      t.setAttribute('data-ob-reveal', '');
      list.push(t);
    }
    if (!motionOn) {
      for (var j = 0; j < list.length; j++) list[j].classList.add('ob-in');
      return;
    }
    if (!('IntersectionObserver' in window)) {
      for (var k = 0; k < list.length; k++) list[k].classList.add('ob-in');
      return;
    }
    var io = new IntersectionObserver(function (entries) {
      entries.forEach(function (en) {
        if (en.isIntersecting) {
          en.target.classList.add('ob-in');
          io.unobserve(en.target);
        }
      });
    }, { rootMargin: '0px 0px -8% 0px', threshold: 0.06 });
    list.forEach(function (el, idx) {
      el.style.transitionDelay = Math.min(idx % 6, 5) * 55 + 'ms';
      io.observe(el);
    });
  }

  /* ------------------------------------------------------------------
     05 — BOOT
     ------------------------------------------------------------------ */
  function boot() {
    try { applyMotionClass(); } catch (e) {}
    try { ensureLight(); } catch (e) {}
    try { buildHero(); } catch (e) {}
    try { collectSpots(); } catch (e) {}
    try { initReveal(); } catch (e) {}

    window.addEventListener('mousemove', onMove, { passive: true });

    var fitTimer = null;
    window.addEventListener('resize', function () {
      if (fitTimer) clearTimeout(fitTimer);
      fitTimer = setTimeout(fitHero, 120);
    }, { passive: true });

    /* the dashboard renderer replaces <body> later — re-scan for boxes */
    var rescans = 0;
    var iv = setInterval(function () {
      collectSpots();
      if (++rescans > 40) clearInterval(iv); /* ~12s of settling */
    }, 300);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  /* expose for the embedded runtime / dashboard renderer */
  window.OceanHero = {
    refresh: function () {
      collectSpots();
      buildHero();
      fitHero();
      initReveal();
    },
    motionEnabled: function () { return motionOn; }
  };
})();
