/* =====================================================================
   OCEAN V2 â€” EFFECT ENGINE / SHARED SITE JS
   ---------------------------------------------------------------------
   Minden, ami nagyon modern: egyedi kurzor, mágneses gombok, tilt- Ă©s
   spotlight-kártyák, scroll-reveal/stagger, rĂ©szecskĂ©s vizorháttĂ©r,
   scroll progressz, typing, számlálók, glitch, marquee, live óra,
   back-to-top, toast, tĂ©ma, mobil menü, FAQ, sidebar auto-nav.
   A rĂ©gi API-k (go, showToast, toggleTheme, splitWords, openMobile,
   closeMobile, scrollToSection, applyTheme) kompatibilitásban maradnak.
   ===================================================================== */
(function () {
    'use strict';

    var reducedMotion = false;
    var finePointer = false;

    /* =================================================================
       00 â€” UTIL
       ================================================================= */
    function onReady(fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            fn();
        }
    }

    function clamp(v, min, max) {
        return Math.max(min, Math.min(max, v));
    }

    function lerp(a, b, t) {
        return a + (b - a) * t;
    }

    function listen(target, ev, fn, opts) {
        (target || document).addEventListener(ev, fn, opts || { passive: true });
    }

    function make(el, cls, html) {
        var n = document.createElement(el || 'div');
        if (cls) n.className = cls;
        if (html) n.innerHTML = html;
        return n;
    }

    /* =================================================================
       01 â€” SĂ–TÉT TÉMA KÉNYSZER (színpaletta + color-scheme)
       ================================================================= */
    function forceDarkScheme() {
        document.documentElement.classList.add('dark');
        document.documentElement.style.colorScheme = 'dark';
        try {
            document.documentElement.style.setProperty('--background', '262 50% 4%');
            document.documentElement.style.setProperty('--foreground', '290 60% 97%');
            document.documentElement.style.setProperty('--card', '262 38% 6%');
            document.documentElement.style.setProperty('--border', '264 28% 16%');
            document.documentElement.style.setProperty('--brand', '265 92% 68%');
            document.documentElement.style.setProperty('--muted-foreground', '258 15% 64%');
        } catch (e) {}
    }
    forceDarkScheme();

    /* =================================================================
       02 â€” SZINTEZLŐ: AMBIENS RÉTEGEK (grain, scanline, progressz)
       ================================================================= */
    /* ÉLŐ FX: molten metal háttér, canvas-kurzor, scanner stég — külön
       fájlban, hogy a WebView2 embedded runtime is ugyanezt kapja. */
    function initFx() {
        if (window.OceanFX) { window.OceanFX.init(); return; }
        var s = document.createElement('script');
        s.src = '/js/ocean-fx.js';
        s.async = true;
        s.onload = function () { if (window.OceanFX) window.OceanFX.init(); };
        s.onerror = function () {};
        document.head.appendChild(s);
    }

    function buildAmbientLayers() {
        var noise = make('div', 'oc-noise');
        noise.setAttribute('aria-hidden', 'true');
        document.body.appendChild(noise);

        var prog = make('div', 'oc-progress');
        prog.setAttribute('aria-hidden', 'true');
        document.body.appendChild(prog);

        var topBtn = document.createElement('button');
        topBtn.className = 'oc-top';
        topBtn.type = 'button';
        topBtn.setAttribute('aria-label', 'Back to top');
        topBtn.innerHTML = '↑';
        document.body.appendChild(topBtn);
        topBtn.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });

        listen(window, 'scroll', function () {
            var s = window.scrollY;
            var h = document.documentElement.scrollHeight - window.innerHeight;
            var pct = h > 0 ? (s / h) * 100 : 0;
            prog.style.width = pct.toFixed(2) + '%';
            document.body.classList.toggle('oc-scrolled', s > 24);
            topBtn.classList.toggle('oc-show', s > 640);
        });
    }

    /* =================================================================
       03 â€” EGYEDI KURZOR + NYOMVONAL + BURST (csak fine pointer)
       ================================================================= */
    function initCursor() {
        if (!finePointer || reducedMotion) return;

        document.body.classList.add('oc-cursor-on');

        var cursor = make('div', 'oc-cursor');
        var follow = make('div', 'oc-cursor-follow');
        var ring = make('div', 'oc-cursor-ring');
        var aura = make('div', 'oc-cursor-aura');
        [cursor, follow, ring, aura].forEach(function (el) {
            el.setAttribute('aria-hidden', 'true');
            document.body.appendChild(el);
        });

        var mx = -100, my = -100;
        var fx = -100, fy = -100;
        var rx = -100, ry = -100;
        var ax = -100, ay = -100;
        var hoverEl = false;

        listen(document, 'mousemove', function (e) {
            mx = e.clientX;
            my = e.clientY;
            cursor.style.transform = 'translate3d(' + mx + 'px,' + my + 'px,0)';
            var target = e.target;
            hoverEl = !!(target && target.closest && target.closest('a,button,input,textarea,select,[role="button"],[onclick],.oc-hover,label'));
            document.body.classList.toggle('oc-cursor-hover', hoverEl);

            if (e.target && e.target.closest && e.target.closest('[data-tilt],.oc-tilt,button,a,[role="button"],[class*="btn"]')) {
                var r = e.target.closest('[data-tilt],.oc-tilt');
                if (r && !r.classList.contains('oc-tilt')) r = r.closest('.oc-tilt');
                var rc = (r || e.currentTarget || {}).getBoundingClientRect ? (r || { getBoundingClientRect: function () { return { left: 0, top: 0, width: 0, height: 0 }; } }).getBoundingClientRect() : { left: 0, top: 0, width: 0, height: 0 };
                var px = rc.width ? ((e.clientX - rc.left) / rc.width) * 100 : 50;
                var py = rc.height ? ((e.clientY - rc.top) / rc.height) * 100 : 50;
                if (r) r.style.setProperty('--oc-x', px.toFixed(1) + '%');
                if (r) r.style.setProperty('--oc-y', py.toFixed(1) + '%');
            }
        });

        var trailPool = [];
        var lastSpawn = 0;
        function spawnTrail() {
            var now = Date.now();
            if (now - lastSpawn < 26 || trailPool.length > 56) return;
            lastSpawn = now;
            var t = make('span', 'oc-cursor-trail');
            t.style.left = mx + 'px';
            t.style.top = my + 'px';
            t.style.opacity = String(0.35 + Math.random() * 0.5);
            var size = 2 + Math.random() * 5;
            t.style.width = size + 'px';
            t.style.height = size + 'px';
            t.style.marginLeft = (-size / 2) + 'px';
            t.style.marginTop = (-size / 2) + 'px';
            t.style.transition = 'opacity 0.5s ease-out, transform 0.5s ease-out';
            document.body.appendChild(t);
            trailPool.push(t);
            (function (node) {
                setTimeout(function () {
                    node.style.opacity = '0';
                    setTimeout(function () {
                        node.remove();
                        var i = trailPool.indexOf(node);
                        if (i > -1) trailPool.splice(i, 1);
                    }, 520);
                }, Math.random() * 120);
            })(t);
        }

        function burst(x, y) {
            for (var i = 0; i < 9; i++) {
                var b = make('span', 'oc-click-burst');
                b.style.left = x + 'px';
                b.style.top = y + 'px';
                b.style.animationDelay = (i * 0.02) + 's';
                b.style.borderColor = i % 2 ? 'rgba(192,132,252,0.9)' : 'rgba(216,180,254,0.9)';
                var ang = (i / 9) * Math.PI * 2;
                var dist = 26 + Math.random() * 30;
                (function (node, dx, dy) {
                    setTimeout(function () {
                        node.style.transition = 'transform 0.55s cubic-bezier(0.22,1,0.36,1), opacity 0.55s';
                        node.style.transform = 'translate(' + dx + 'px,' + dy + 'px) scale(0.5)';
                        node.style.opacity = '0';
                        node.addEventListener('transitionend', function () { node.remove(); }, { once: true });
                    }, 10);
                })(b, Math.cos(ang) * dist, Math.sin(ang) * dist);
                document.body.appendChild(b);
            }
        }

        listen(document, 'mousedown', function (e) { burst(e.clientX, e.clientY); });

        function frame() {
            fx = lerp(fx, mx, 0.16);
            fy = lerp(fy, my, 0.16);
            rx = lerp(rx, mx, 0.10);
            ry = lerp(ry, my, 0.10);
            ax = lerp(ax, mx, 0.07);
            ay = lerp(ay, my, 0.07);
            follow.style.transform = 'translate3d(' + fx + 'px,' + fy + 'px,0)';
            ring.style.transform = 'translate3d(' + rx + 'px,' + ry + 'px,0)';
            aura.style.transform = 'translate3d(' + ax + 'px,' + ay + 'px,0)';
            spawnTrail();
            if (!reducedMotion) requestAnimationFrame(frame);
        }
        frame();
    }

    /* =================================================================
       04 â€” MĂGNESES GOMBOK
       ================================================================= */
    function initMagnetic() {
        if (!finePointer) return;
        var targets = Array.prototype.slice.call(document.querySelectorAll('.oc-magnetic, nav a[class*="btn"], nav button[class*="btn"]'));
        if (!targets.length) return;
        targets.forEach(function (el) {
            el.classList.add('oc-magnetic');
            var strength = parseFloat(el.getAttribute('data-magnetic') || '0.35');
            listen(el, 'mousemove', function (e) {
                var r = el.getBoundingClientRect();
                var dx = (e.clientX - (r.left + r.width / 2)) * strength;
                var dy = (e.clientY - (r.top + r.height / 2)) * strength;
                el.style.transform = 'translate3d(' + dx + 'px,' + dy + 'px,0)';
            });
            listen(el, 'mouseleave', function () {
                el.style.transform = 'translate3d(0,0,0)';
                el.style.transition = 'transform 0.45s cubic-bezier(0.34,1.56,0.64,1)';
                setTimeout(function () { if (el) el.style.transition = ''; }, 460);
            });
        });
    }

    /* =================================================================
       05 â€” TILT KĂRTYĂK + GLARE + SPOTLIGHT CLOUD
       ================================================================= */
    function initTilt() {
        if (!finePointer || reducedMotion) return;
        var cards = document.querySelectorAll('.oc-tilt, [data-tilt]');
        if (!cards.length) return;
        Array.prototype.forEach.call(cards, function (card) {
            card.classList.add('oc-tilt');
            if (!card.querySelector('.oc-glare')) {
                var glare = make('div', 'oc-glare');
                glare.setAttribute('aria-hidden', 'true');
                card.appendChild(glare);
            }
            var max = parseFloat(card.getAttribute('data-tilt') || '10');
            var active = false;
            listen(card, 'mousemove', function (e) {
                active = true;
                var r = card.getBoundingClientRect();
                var px = clamp((e.clientX - r.left) / r.width, 0, 1);
                var py = clamp((e.clientY - r.top) / r.height, 0, 1);
                var rx = (0.5 - py) * max * 2;
                var ry = (px - 0.5) * max * 2;
                card.style.transition = 'transform 0.08s linear';
                card.style.transform = 'perspective(900px) rotateX(' + rx.toFixed(2) + 'deg) rotateY(' + ry.toFixed(2) + 'deg) translateY(-3px) scale3d(1.01,1.01,1)';
                card.style.setProperty('--oc-x', (px * 100).toFixed(1) + '%');
                card.style.setProperty('--oc-y', (py * 100).toFixed(1) + '%');
            });
            listen(card, 'mouseleave', function () {
                active = false;
                card.style.transition = 'transform 0.6s cubic-bezier(0.22,1,0.36,1)';
                card.style.transform = 'perspective(900px) rotateX(0) rotateY(0) translateY(0) scale3d(1,1,1)';
            });
        });
    }

    /* =================================================================
       06 â€” SPOTLIGHT HOVER (card-felületeken --oc-x / --oc-y)
       ================================================================= */
    function initSpotlight() {
        var els = document.querySelectorAll('.dash-card, .stat-card, .feature-card, [data-spotlight], .blog-card, .key-item');
        if (!els.length) return;
        Array.prototype.forEach.call(els, function (el) {
            if (!el.hasAttribute('data-tilt') && !el.classList.contains('oc-tilt')) {
                listen(el, 'mousemove', function (e) {
                    var r = el.getBoundingClientRect();
                    var px = r.width ? ((e.clientX - r.left) / r.width) * 100 : 50;
                    var py = r.height ? ((e.clientY - r.top) / r.height) * 100 : 50;
                    el.style.setProperty('--oc-x', px.toFixed(1) + '%');
                    el.style.setProperty('--oc-y', py.toFixed(1) + '%');
                });
            }
        });
    }

    /* =================================================================
       07 â€” REVEAL / STAGGER (IntersectionObserver)
       ================================================================= */
    function initRevealEffects() {
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (en.isIntersecting) {
                    en.target.classList.add('oc-in');
                    if (en.target.classList.contains('reveal')) en.target.classList.add('visible');
                    obs.unobserve(en.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -6% 0px' });

        var all = document.querySelectorAll('.oc-reveal, .oc-reveal-left, .oc-reveal-right, .oc-reveal-zoom, .reveal, .oc-stagger');
        Array.prototype.forEach.call(all, function (el) { obs.observe(el); });
    }

    /* =================================================================
       07b - ENTRANCE: every block arrives smoothly, once
       -----------------------------------------------------------------
       The stylesheet has the animation (`.oc-enter`, section 18 of
       ocean-black.css); this only decides *which* block gets it and
       *when*: the outermost block that is looked at first, staggered by
       its position among its siblings, so a row of cards comes in as a
       wave instead of a single slab.

       Outermost-only is deliberate: a card animates as one object, not as
       forty rows animating inside a card that is animating too (that
       double-fade reads as sluggish). With JS off, or motion reduced,
       nothing here runs and the page is simply already there.
       ================================================================= */
    var ENTER_TARGETS = [
        'section', 'article', 'aside', 'header', 'footer', 'table',
        'h1', 'h2', 'h3', 'h4', 'p', 'blockquote', 'figure', 'img',
        'button', 'input', 'select', 'textarea', 'label',
        '[class*="card"]', '[class*="panel"]', '[class*="stat"]',
        '[class*="rounded-"]', '[class*="chip"]', '[class*="badge"]',
        '[class*="glass"]', '[class*="tile"]',
        '[class*="oc-"]', '[class*="ob-"]', '[class*="dash-"]'
    ].join(',');

    /* Nothing in this subtree may animate at all: the pointer layers, the
       decorative canvases, the live overlays (animating a fixed element
       also re-parents its containing block) and the hero stage, which
       drives its own 3D transforms. */
    var ENTER_SKIP_SUBTREE = [
        'script', 'style', 'link', 'meta', 'noscript', 'svg', 'canvas', 'video', 'iframe',
        '.oc-particles', '#oc-dash-glow', '#oc-float-pin-btn', '#navBarStick',
        '.oc-cursor', '.oc-cursor-follow', '.oc-cursor-ring', '.oc-cursor-aura',
        '.oc-cursor-trail', '.oc-click-burst', '.oc-mouse-glow', '.oc-cursor-light',
        '.oc-noise', '.oc-scanline', '.oc-progress', '.oc-top', '.oc-ctrl-bar',
        '.oc-ambient', '.ob-spot',
        '.oc-toast', '.oc-toast-container', '#oc-notif-stack',
        '.oc-overlay', '.modal-overlay', '.mobile-panel', '.mobile-panel-overlay',
        '.ob-hero-3d', '[data-no-enter]',
        '[style*="position:fixed"]', '[style*="position: fixed"]'
    ].join(',');

    /* Structural wrappers: the wrapper itself is not a block, its contents
       are. Animating these would fade the whole page in as one slab. */
    var ENTER_SKIP_SELF = [
        'html', 'body', 'main', 'nav', 'section', 'article', 'header', 'footer',
        'aside', 'form', '#oc-dash-content'
    ].join(',');

    /* Blocks that are worth a re-entry when the DOM is re-rendered later
       (dashboard route changes). Small things -- a log line, a status pill,
       a table cell -- are excluded there, so a 2s live feed that repaints
       its list does not flicker. The dashboard builds its panels as plain
       inline-styled divs, so the content container's own children count as
       blocks there. */
    var ENTER_RESCAN = [
        '[class*="card"]', '[class*="panel"]', '[class*="stat"]',
        '[class*="tile"]', 'table', 'section', 'h1', 'h2', 'h3',
        '#oc-dash-content > *', '.grid > *', '[class*="col-span"]'
    ].join(',');

    /* The live scanner card repaints itself on a 2s poll; nothing inside it
       may re-enter, or the dashboard would flicker continuously. */
    var ENTER_RESCAN_QUIET = '[class*="scan"], [id*="scan"], [class*="feed"], [class*="log-"]';

    function initEntrance() {
        if (reducedMotion || !document.body || !window.IntersectionObserver) return;
        if (document.documentElement.classList.contains('ob-no-motion')) return;

        function skippedSelf(el) {
            return !el || el.nodeType !== 1 || !el.matches || el.matches(ENTER_SKIP_SELF);
        }

        function skipped(el) {
            if (!el || el.nodeType !== 1 || !el.matches) return true;
            if (el.getAttribute('data-oc-enter')) return true;
            if (el.matches(ENTER_SKIP_SUBTREE)) return true;
            if (el.closest(ENTER_SKIP_SUBTREE)) return true;
            if (skippedSelf(el)) return true;
            return false;
        }

        /* Only genuinely hidden elements are dropped. A block that is 0x0
           right now (the dashboard builds its panels around its data) is
           still observed: the observer re-evaluates on layout, so it fires
           the moment the panel gets its size. Requiring a size here is what
           used to make a freshly rendered dashboard panel never arrive. */
        function shown(el) {
            var cs = window.getComputedStyle(el);
            return cs.display !== 'none' && cs.visibility !== 'hidden';
        }

        /* keep the outermost match of every chain, so nothing animates twice */
        function outermost(list) {
            var out = [];
            Array.prototype.forEach.call(list, function (el) {
                var p = el.parentElement;
                while (p && p !== document.body) {
                    if (!skipped(p) && p.matches(ENTER_TARGETS)) return;
                    p = p.parentElement;
                }
                out.push(el);
            });
            return out;
        }

        /* the wave: 55ms per visible sibling, capped so a long column never
           leaves something waiting for the user to scroll to it */
        function delayFor(el) {
            var parent = el.parentElement;
            if (!parent) return 0;
            var kids = parent.children, seen = 0;
            for (var i = 0; i < kids.length; i++) {
                if (kids[i] === el) break;
                if (kids[i].matches && !skipped(kids[i]) && kids[i].matches(ENTER_TARGETS)) seen++;
            }
            return Math.min(seen * 55, 440);
        }

        function variantFor(el) {
            if (el.matches('img, figure, [class*="logo"], [class*="avatar"]')) return 'oc-enter--zoom';
            if (el.matches('li, tr, .pill, .ob-tag, [class*="badge"], [class*="chip"]')) return 'oc-enter--fade';
            return 'oc-enter--up';
        }

        function arrive(el) {
            if (!el || el.getAttribute('data-oc-enter') !== 'queued') return;
            obs.unobserve(el);
            var d = delayFor(el);
            el.setAttribute('data-oc-enter', 'done');
            el.style.setProperty('--oc-enter-delay', d + 'ms');
            el.style.setProperty('--oc-enter-dur', (520 - Math.min(d, 440) * 0.3) + 'ms');
            el.classList.add('oc-enter', variantFor(el));

            /* Hand the element back once it has arrived. `fill: both` keeps the
               end frame (transform: none) for as long as the class is on it,
               which would silently win over the hover transforms the rest of
               the site relies on. One exception matters: the site's own
               reveal layer (`.oc-revo`, opacity 0 until it is marked in view)
               is promoted to visible instead of being dropped back to hidden
               when the class comes off. */
            function settle() {
                if (!el.classList.contains('oc-enter')) return;
                if (el.classList.contains('oc-revo')) {
                    el.classList.add('oc-in');
                    el.style.opacity = '1';
                    el.style.transform = '';
                }
                el.classList.remove('oc-enter', 'oc-enter--up', 'oc-enter--fade', 'oc-enter--zoom');
                el.style.removeProperty('--oc-enter-delay');
                el.style.removeProperty('--oc-enter-dur');
            }
            el.addEventListener('animationend', function (ev) {
                if (ev.target === el) settle();
            }, { once: true });
            /* ...and a watchdog, because `animationend` is delivered on a
               frame: if the tab is throttled or the compositor is not
               sampling, a block must not be left holding the start frame
               (opacity 0) with nothing scheduled to release it. */
            setTimeout(settle, d + 900);
        }

        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) { if (en.isIntersecting) arrive(en.target); });
        }, { threshold: 0.02, rootMargin: '0px 0px -6% 0px' });

        /* Safety net. IntersectionObserver delivery is tied to the frame
           lifecycle and it clips against scroll containers, and a block that
           is on screen must never sit at opacity 0 waiting for a callback
           that is not coming (a background tab, a headless render, a panel
           inside the dashboard's own scroll area). This is the same
           "is it in view" test the site's own reveal pass uses. */
        function sweep() {
            var vh = window.innerHeight || document.documentElement.clientHeight || 0;
            if (!vh) return;
            var q = document.querySelectorAll('[data-oc-enter="queued"]');
            for (var i = 0; i < q.length; i++) {
                var r = q[i].getBoundingClientRect();
                if (r.width && r.height && r.top < vh * 0.94 && r.bottom > 0) arrive(q[i]);
            }
        }
        var sweepQueued = false;
        function scheduleSweep() {
            if (sweepQueued) return;
            sweepQueued = true;
            (window.requestAnimationFrame || window.setTimeout)(function () { sweepQueued = false; sweep(); });
        }
        window.addEventListener('scroll', scheduleSweep, { passive: true });
        window.addEventListener('resize', scheduleSweep, { passive: true });
        setTimeout(sweep, 500);
        var sweepTimer = setInterval(sweep, 1400);

        function scan(rescan) {
            var all = outermost(document.querySelectorAll(ENTER_TARGETS));
            all.forEach(function (el) {
                if (skipped(el) || !shown(el)) return;
                if (rescan && !el.matches(ENTER_RESCAN)) return;
                if (rescan && el.closest(ENTER_RESCAN_QUIET)) return;
                el.setAttribute('data-oc-enter', 'queued');
                obs.observe(el);
            });
        }

        scan(false);

        /* React renders its pages after we boot, and the dashboard swaps
           its whole content on every route change: re-scan when the DOM
           grows so those blocks arrive too. The window is bounded -- after
           a minute the page is considered settled and the observer stops
           waking up on toast/notification repaints. */
        if (window.MutationObserver) {
            var reTimer = 0;
            var mo = new MutationObserver(function () {
                clearTimeout(reTimer);
                reTimer = setTimeout(function () { scan(true); }, 160);
            });
            mo.observe(document.body, { childList: true, subtree: true });
            setTimeout(function () {
                mo.disconnect();
                clearInterval(sweepTimer);
            }, 60000);
        }
    }

    /* =================================================================
       08 â€” RÉSZECSKE-VIZSGĂLĂ“ CANVAS (háttĂ©r)
       ================================================================= */
    function initParticles() {
        if (reducedMotion) return;
        var canvas = document.createElement('canvas');
        canvas.className = 'oc-particles';
        canvas.setAttribute('aria-hidden', 'true');
        canvas.style.cssText = 'position:fixed;inset:0;z-index:-1;pointer-events:none;opacity:0.75;';
        document.body.appendChild(canvas);
        var ctx = canvas.getContext('2d');
        var w = 0, h = 0, dpr = 1;
        var dots = [];
        var mouse = { x: -9999, y: -9999 };
        var mouseIn = false;
        var maxDots = 64;

        function resize() {
            dpr = Math.min(window.devicePixelRatio || 1, 2);
            w = window.innerWidth;
            h = window.innerHeight;
            canvas.width = w * dpr;
            canvas.height = h * dpr;
            canvas.style.width = w + 'px';
            canvas.style.height = h + 'px';
            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
            var target = Math.min(maxDots, Math.floor((w * h) / 26000));
            if (dots.length > target) dots.length = target;
            while (dots.length < target) dots.push(spawn(true));
            ctx.globalCompositeOperation = 'lighter';
        }

        function spawn(anywhere) {
            var colorful = Math.random() < 0.35;
            return {
                x: Math.random() * w,
                y: Math.random() * h,
                r: 0.8 + Math.random() * 2.2,
                vx: (Math.random() - 0.5) * 0.25,
                vy: (Math.random() - 0.5) * 0.25,
                hue: colorful ? (262 + Math.random() * 60) : (200 + Math.random() * 60),
                a: 0.18 + Math.random() * 0.4
            };
        }

        listen(window, 'resize', resize);
        listen(document, 'mousemove', function (e) {
            mouse.x = e.clientX;
            mouse.y = e.clientY;
            mouseIn = true;
        });
        listen(document, 'mouseleave', function () { mouseIn = false; });

        var start = Date.now();
        function step() {
            ctx.clearRect(0, 0, w, h);
            var i, d, j;
            for (i = 0; i < dots.length; i++) {
                d = dots[i];
                d.x += d.vx;
                d.y += d.vy;
                if (d.x < 0 || d.x > w) d.vx *= -1;
                if (d.y < 0 || d.y > h) d.vy *= -1;
                if (mouseIn) {
                    var dx = mouse.x - d.x, dy = mouse.y - d.y;
                    var dist = Math.sqrt(dx * dx + dy * dy);
                    if (dist < 130 && dist > 0.1) {
                        var f = (130 - dist) / 130;
                        d.x += dx / dist * f * 1.1;
                        d.y += dy / dist * f * 1.1;
                    }
                }
                var tw = 0.5 + 0.5 * Math.sin((Date.now() - start) / 900 + i);
                /* squares, not dots: the whole design is checkered, so the
                   background particles are little blocks like the grid */
                ctx.beginPath();
                ctx.rect(Math.round(d.x - d.r), Math.round(d.y - d.r), d.r * 2, d.r * 2);
                if (i % 4 === 0) {
                    ctx.fillStyle = 'hsla(' + d.hue + ', 92%, 70%, ' + (d.a * tw) + ')';
                    ctx.shadowColor = 'hsla(' + d.hue + ', 92%, 70%, 0.8)';
                    ctx.shadowBlur = 8;
                } else {
                    ctx.fillStyle = 'hsla(' + d.hue + ', 70%, 78%, ' + (d.a * 0.8) + ')';
                    ctx.shadowBlur = 0;
                }
                ctx.fill();
                ctx.shadowBlur = 0;
            }
            for (i = 0; i < dots.length; i++) {
                for (j = i + 1; j < dots.length; j++) {
                    var a = dots[i], b = dots[j];
                    var dx = a.x - b.x, dy = a.y - b.y;
                    var dist = dx * dx + dy * dy;
                    if (dist < 140 * 140) {
                        var op = (1 - Math.sqrt(dist) / 140) * 0.14;
                        ctx.strokeStyle = 'rgba(168, 85, 247, ' + op + ')';
                        ctx.lineWidth = 0.7;
                        ctx.beginPath();
                        ctx.moveTo(a.x, a.y);
                        ctx.lineTo(b.x, b.y);
                        ctx.stroke();
                    }
                }
            }
            if (!reducedMotion && dots.length) requestAnimationFrame(step);
        }

        resize();
        step();

        return {
            destroy: function () {
                canvas.remove();
                dots = [];
            }
        };
    }

    /* =================================================================
       09 â€” COUNTER [data-count] + [data-suffix] / [data-prefix]
       ================================================================= */
    function initCounters() {
        var els = document.querySelectorAll('[data-count]');
        if (!els.length) return;
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (!en.isIntersecting) return;
                var el = en.target;
                obs.unobserve(el);
                var target = parseFloat(el.getAttribute('data-count')) || 0;
                var dur = parseFloat(el.getAttribute('data-duration') || '1400');
                var dec = el.getAttribute('data-decimals') !== null ? parseInt(el.getAttribute('data-decimals'), 10) : (target % 1 !== 0 ? 2 : 0);
                var suffix = el.getAttribute('data-suffix') || '';
                var prefix = el.getAttribute('data-prefix') || '';
                var start = null;
                function tick(ts) {
                    if (start === null) start = ts;
                    var p = clamp((ts - start) / dur, 0, 1);
                    var eased = 1 - Math.pow(1 - p, 3);
                    var val = target * eased;
                    el.textContent = prefix + val.toFixed(dec).replace(/\B(?=(\d{3})+(?!\d))/g, ',') + suffix;
                    if (p < 1) requestAnimationFrame(tick);
                }
                requestAnimationFrame(tick);
            });
        }, { threshold: 0.4 });
        Array.prototype.forEach.call(els, function (el) { obs.observe(el); });
    }

    /* =================================================================
       10 â€” TYPING EFFECT [data-type="hello,world"]
       ================================================================= */
    function initTyping() {
        var els = document.querySelectorAll('[data-type]');
        if (!els.length) return;
        Array.prototype.forEach.call(els, function (el, idx) {
            var phrases = (el.getAttribute('data-type') || '').split(',');
            if (!phrases.length) return;
            el.classList.add('oc-type');
            var pi = 0, ci = 0, deleting = false;
            setTimeout(function () {
                (function type() {
                    var word = phrases[pi];
                    el.textContent = word.slice(0, ci);
                    var speed = deleting ? 22 : 55 + Math.random() * 30;
                    if (!deleting && ci === word.length) {
                        speed = 1600;
                        deleting = true;
                    } else if (deleting && ci === 0) {
                        deleting = false;
                        pi = (pi + 1) % phrases.length;
                        speed = 320;
                    }
                    ci += deleting ? -1 : 1;
                    setTimeout(type, speed);
                })();
            }, idx * 500);
        });
    }

    /* =================================================================
       11 â€” GLITCH: [data-glitch] duplikálja a szöveget pseudóknak
       ================================================================= */
    function initGlitch() {
        Array.prototype.forEach.call(document.querySelectorAll('[data-glitch]'), function (el) {
            el.setAttribute('data-text', el.textContent);
        });
    }

    /* =================================================================
       12 â€” MARQUEE: tartalom duplikálása a zökkenŐ‘mentes loopĂ©rt
       ================================================================= */
    function initMarquee() {
        Array.prototype.forEach.call(document.querySelectorAll('.oc-marquee'), function (mq) {
            var track = mq.querySelector('.oc-marquee-track');
            if (!track || track.getAttribute('data-cloned')) return;
            var clone = track.cloneNode(true);
            track.parentNode.appendChild(clone);
            var all = mq.querySelectorAll('.oc-marquee-track');
            var total = 0;
            Array.prototype.forEach.call(all, function (t) { total += t.scrollWidth; });
            track.setAttribute('data-cloned', '1');
        });
    }

    /* =================================================================
       13 â€” ÉLŐ ÓRA [data-clock] + dátum [data-today]
       ================================================================= */
    function initClock() {
        var clocks = document.querySelectorAll('[data-clock]');
        var dates = document.querySelectorAll('[data-today]');
        if (!clocks.length && !dates.length) return;
        var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        function render() {
            var d = new Date();
            var hh = ('0' + d.getHours()).slice(-2);
            var mm = ('0' + d.getMinutes()).slice(-2);
            var ss = ('0' + d.getSeconds()).slice(-2);
            var now = hh + ':' + mm + ':' + ss;
            var date = d.getDate() + ' ' + MONTHS[d.getMonth()] + ' ' + d.getFullYear();
            clocks.forEach(function (el) { el.textContent = now; });
            dates.forEach(function (el) { el.textContent = date; });
        }
        render();
        setInterval(render, 1000);
    }

    /* =================================================================
       14 â€” AKTĂŤV NAV LÁNCOLÁS (scroll observer szekciókra)
       ================================================================= */
    function initActiveNav() {
        var links = Array.prototype.slice.call(document.querySelectorAll('.docs-sidebar a, [data-nav] a, nav a[href^="#"]'));
        if (!links.length) return;
        var map = {};
        links.forEach(function (a) {
            var id = (a.getAttribute('href') || '').replace('#', '');
            if (id) map['#' + id] = a;
        });
        var ids = Object.keys(map);
        if (!ids.length) return;
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (en) {
                if (!en.isIntersecting) return;
                ids.forEach(function (id) {
                    var el = document.querySelector(id);
                    if (!el) return;
                    var r = el.getBoundingClientRect();
                    map[id].classList.toggle('active', r.top <= window.innerHeight * 0.3 && r.bottom >= 0);
                });
            });
        }, { threshold: 0, rootMargin: '-20% 0px -60% 0px' });
        ids.forEach(function (id) {
            var el = document.querySelector(id);
            if (el) obs.observe(el);
        });
    }

    /* =================================================================
       15 â€” TOAST (rĂ©gi + új stílus)
       ================================================================= */
    window.showToast = function (msg, type) {
        var container = document.getElementById('toast-container');
        if (!container) {
            container = make('div', 'toast-container');
            container.id = 'toast-container';
            container.style.cssText = 'position:fixed;bottom:18px;right:18px;z-index:2147483647;display:flex;flex-direction:column;gap:10px;';
            document.body.appendChild(container);
        }
        var toast = make('div', 'oc-toast');
        var icon = type === 'success' ? 'âś”' : type === 'error' ? 'âś•' : 'â—';
        var color = type === 'success' ? '#86d6aa' : type === 'error' ? '#e39aa2' : '#a855f7';
        toast.innerHTML = '<span style="color:' + color + ';text-shadow:0 0 10px ' + color + ';">' + icon + '</span><span>' + msg + '</span>';
        container.appendChild(toast);
        setTimeout(function () {
            toast.classList.add('oc-leave');
            setTimeout(function () { toast.remove(); }, 420);
        }, 3800);
    };

    /* =================================================================
       16 â€” TÉMA (rĂ©gi kompatibilitás, de most fixen sötĂ©t)
       ================================================================= */
    var sunIcon = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M4.93 4.93l1.41 1.41m11.32 11.32 1.41 1.41M2 12h2m16 0h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41"/></svg>';
    var moonIcon = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>';

    window.applyTheme = function () {
        document.documentElement.classList.add('dark');
        forceDarkScheme();
        var btn = document.getElementById('theme-toggle');
        if (btn) btn.innerHTML = sunIcon;
    };
    window.toggleTheme = function () {
        document.documentElement.classList.add('dark');
        forceDarkScheme();
        var btn = document.getElementById('theme-toggle');
        if (btn) btn.innerHTML = sunIcon;
    };

    function renderThemeToggle() {
        var host = document.getElementById('theme-toggle-host');
        if (!host) return;
        var btn = document.getElementById('theme-toggle');
        if (!btn) {
            host.innerHTML = '<button class="icon-btn" id="theme-toggle" type="button" title="Theme" aria-label="Theme" style="width:2.5rem;height:2.5rem;border-radius:0.5rem;border:1px solid rgba(168,85,247,0.3);background:rgba(168,85,247,0.08);cursor:pointer;color:#e9d5ff"></button>';
            btn = document.getElementById('theme-toggle');
            btn.addEventListener('click', window.toggleTheme);
        }
        btn.innerHTML = sunIcon;
    }

    /* =================================================================
       17 â€” NAV / FOOTER (rĂ©gi hostokkal kompatibilis)
       ================================================================= */
    function logoMark() {
        return '<svg width="130" height="34" viewBox="0 0 206 50" fill="none" xmlns="http://www.w3.org/2000/svg" style="height:2rem;width:auto"><rect x="2" y="6" width="38" height="38" rx="9" fill="url(#oc-logo-g)"/><defs><linearGradient id="oc-logo-g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#7c3aed"/><stop offset="1" stop-color="#c084fc"/></linearGradient></defs><path d="M24 14v15a7 7 0 0 0 14 0" stroke="#fff" stroke-width="5" stroke-linecap="round"/><text x="50" y="33" font-family="Arial, sans-serif" font-weight="700" font-size="24" fill="#f7f8fa">OCEAN</text></svg>';
    }

    function renderNav() {
        var host = document.getElementById('nav-host');
        if (!host) return;
        host.innerHTML = `
            <nav class="navbar oc-glass" id="site-nav" style="position:fixed;top:0;left:0;right:0;z-index:1000;">
                <div class="container max-w-7xl nav-inner">
                    <a href="/" class="nav-logo" onclick="event.preventDefault();go('/')">
                        ${logoMark()}
                    </a>
                    <div class="nav-links-desktop">
                        <a href="/" class="nav-link" data-nav>Home</a>
                        <a href="/pricing" class="nav-link" data-nav>Pricing</a>
                        <a href="/docs" class="nav-link" data-nav>Docs</a>
                        <a href="/branding" class="nav-link" data-nav>Branding</a>
                        <a href="/downloads" class="nav-link" data-nav>Download</a>
                        <a href="https://discord.anticheat.ac" target="_blank" class="nav-link">Discord</a>
                    </div>
                    <div class="nav-actions">
                        <span id="theme-toggle-host"></span>
                        <button class="btn btn-outline text-sm" onclick="go('/login')" style="padding:0.5rem 1rem;">Login</button>
                        <button class="btn btn-brand text-sm" onclick="go('/register')" style="padding:0.5rem 1rem;">Sign Up</button>
                        <button class="nav-hamburger" id="hamburger" aria-label="Menu">
                            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 12h18M3 6h18M3 18h18"/></svg>
                        </button>
                    </div>
                </div>
            </nav>
            <div class="mobile-panel-overlay" id="mobile-overlay"></div>
            <div class="mobile-panel" id="mobile-panel">
                <div class="mobile-panel-header">
                    ${logoMark()}
                    <button class="icon-btn" onclick="closeMobile()" aria-label="Close"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 6 6 18M6 6l12 12"/></svg></button>
                </div>
                <div style="padding:0 1rem;">
                    <a class="mobile-link" href="/" onclick="closeMobile()">Home</a>
                    <a class="mobile-link" href="/pricing" onclick="closeMobile()">Pricing</a>
                    <a class="mobile-link" href="/docs" onclick="closeMobile()">Docs</a>
                    <a class="mobile-link" href="/branding" onclick="closeMobile()">Branding</a>
                    <a class="mobile-link" href="/downloads" onclick="closeMobile()">Download</a>
                    <a class="mobile-link" href="/dashboard" onclick="closeMobile()">Dashboard</a>
                </div>
                <div class="mobile-panel-footer">
                    <button class="btn btn-brand" style="width:100%" onclick="go('/register')">Sign Up</button>
                    <button class="btn btn-outline" style="width:100%" onclick="go('/login')">Login</button>
                </div>
            </div>
        `;
    }

    function renderFooter() {
        var host = document.getElementById('footer-host');
        if (!host) return;
        host.innerHTML = `
            <footer class="footer">
                <div class="container max-w-7xl">
                    <div class="footer-grid">
                        <div class="footer-col">
                            <div style="display:flex;align-items:center;gap:0.5rem;margin-bottom:1rem;">
                                ${logoMark()}
                            </div>
                            <p class="footer-tagline">Experience an unparalleled service designed with quality, safety, and speed in mind. The #1 screenshare tool for gaming communities.</p>
                        </div>
                        <div class="footer-col"><h4>Product</h4>
                            <a href="/downloads">Download</a>
                            <a href="/docs">Docs</a>
                            <a href="/pricing">Pricing</a>
                            <a href="/branding">Branding</a>
                            <a href="/blog">Blog</a>
                        </div>
                        <div class="footer-col"><h4>Legal</h4>
                            <a href="/tos">Terms of Service</a>
                            <a href="/privacy">Privacy Policy</a>
                            <a href="/legal">Legal</a>
                        </div>
                        <div class="footer-col"><h4>Community</h4>
                            <a href="https://discord.anticheat.ac" target="_blank">Discord</a>
                            <a href="https://youtube.com/@OceanScanner" target="_blank">YouTube</a>
                            <a href="#" onclick="showToast('Contact: contact@anticheat.ac','info');return false;">Contact</a>
                        </div>
                        <div class="footer-col"><h4>Support</h4>
                            <a href="#" onclick="showToast('Email us: contact@anticheat.ac','info');return false;">Contact Us</a>
                            <a href="/downloads">Troubleshooting</a>
                        </div>
                    </div>
                    <div class="footer-bottom">© Copyright 2026 Ocean Anticheat. All rights reserved.</div>
                </div>
            </footer>
        `;
    }

    /* =================================================================
       18 â€” MOBILE MENĂś, SCROLL NAV, FAQ, WORD BLUR, AUTO NAV (rĂ©gi)
       ================================================================= */
    function initScrollNav() {
        var nav = document.getElementById('site-nav');
        if (!nav) return;
        listen(window, 'scroll', function () {
            nav.classList.toggle('scrolled', window.scrollY > 10);
        });
    }

    function initHamburger() {
        var btn = document.getElementById('hamburger');
        var panel = document.getElementById('mobile-panel');
        var overlay = document.getElementById('mobile-overlay');
        if (!btn || !panel || !overlay) return;
        btn.addEventListener('click', openMobile);
        overlay.addEventListener('click', closeMobile);
    }
    window.openMobile = function () {
        var panel = document.getElementById('mobile-panel');
        var overlay = document.getElementById('mobile-overlay');
        if (panel) panel.classList.add('open');
        if (overlay) overlay.classList.add('open');
    };
    window.closeMobile = function () {
        var panel = document.getElementById('mobile-panel');
        var overlay = document.getElementById('mobile-overlay');
        if (panel) panel.classList.remove('open');
        if (overlay) overlay.classList.remove('open');
    };

    function initBlurWords() {
        var words = document.querySelectorAll('.word-blur');
        if (!words.length) return;
        var obs = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    e.target.classList.add('visible');
                    obs.unobserve(e.target);
                }
            });
        }, { threshold: 0.1 });
        words.forEach(function (w, i) { w.style.transitionDelay = (i * 0.03) + 's'; obs.observe(w); });
    }

    function initFAQ() {
        document.querySelectorAll('.faq-question').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var answer = btn.nextElementSibling;
                var chev = btn.querySelector('.faq-chevron');
                var open = answer && answer.classList.contains('open');
                document.querySelectorAll('.faq-answer').forEach(function (a) { a.classList.remove('open'); });
                document.querySelectorAll('.faq-chevron').forEach(function (c) { c.classList.remove('open'); });
                if (!open && answer) {
                    answer.classList.add('open');
                    if (chev) chev.classList.add('open');
                }
            });
        });
    }

    window.splitWords = function (selector) {
        var els = document.querySelectorAll(selector || '.split');
        els.forEach(function (el) {
            var words = el.textContent.split(' ');
            el.textContent = '';
            words.forEach(function (w) {
                var span = document.createElement('span');
                span.className = 'word-blur';
                span.textContent = w + ' ';
                el.appendChild(span);
            });
        });
    };

    function initAutoNav() {
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) {
                    var id = e.target.id;
                    document.querySelectorAll('.docs-sidebar a').forEach(function (a) {
                        a.classList.remove('active');
                        if (a.getAttribute('href') === '#' + id) a.classList.add('active');
                    });
                }
            });
        }, { threshold: 0.2 });
        document.querySelectorAll('section[id],div[id="getting-started"],div[id="api"],div[id="detections"],div[id="pins"],div[id="lookup"]')
            .forEach(function (el) { observer.observe(el); });
    }

    /* =================================================================
       19 â€” ROUTING (rĂ©gi kompatibilitás)
       ================================================================= */
    window.go = function (url) {
        var map = {
            '/pages/login.html': '/login',
            '/pages/register.html': '/register',
            '/pages/pricing.html': '/pricing',
            '/pages/docs.html': '/docs',
            '/pages/branding.html': '/branding',
            '/pages/downloads.html': '/downloads',
            '/pages/dashboard.html': '/dashboard',
            '/pages/blog.html': '/blog',
            '/pages/tos.html': '/tos',
            '/pages/privacy.html': '/privacy',
            '/pages/legal.html': '/legal',
            '/pages/changelog.html': '/changelog'
        };
        window.location.href = map[url] || url;
    };
    window.scrollToSection = function (id) {
        var el = document.querySelector(id);
        if (el) el.scrollIntoView({ behavior: 'smooth' });
    };

    /* =================================================================
       20 â€” RIPPLE A GOMBOKON
       ================================================================= */
    function initRipples() {
        document.addEventListener('click', function (e) {
            var t = e.target && e.target.closest ? e.target.closest('button,a[class*="btn"],[role="button"]') : null;
            if (!t) return;
            if (t.closest('.oc-magnetic')) return;
            var r = t.getBoundingClientRect();
            var rip = make('span', 'oc-ripple');
            var size = Math.max(r.width, r.height) * 0.6;
            rip.style.width = rip.style.height = size + 'px';
            rip.style.left = (e.clientX - r.left) + 'px';
            rip.style.top = (e.clientY - r.top) + 'px';
            rip.style.overflow = 'hidden';
            t.appendChild(rip);
            rip.addEventListener('animationend', function () { rip.remove(); }, { once: true });
        });
    }

    /* =================================================================
       21 â€” FONT AWESOME BETĂ–LTÉS (rĂ©gi kompatibilitás)
       ================================================================= */
    function loadIcons() {
        if (document.querySelector('link[href*="font-awesome"]')) return;
        var link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css';
        document.head.appendChild(link);
    }

    /* =================================================================
       22 â€” INDÍTĂS KAPCSOLĂ“K
       ================================================================= */
    /* prefers-reduced-motion is the OS switch for "no animation". We honour it,
       but a page that leans on motion should still offer a way back in:
       ?motion=1 in the URL, or the on-page toggle shown when the OS said no. */
    function motionForced() {
        try {
            if (/[?&]motion=1/.test(window.location.search)) return true;
            return window.localStorage.getItem('oc-motion') === 'on';
        } catch (e) { return false; }
    }

    function detect() {
        var systemReduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        var forced = motionForced();
        reducedMotion = systemReduced && !forced;
        finePointer = window.matchMedia('(pointer: fine)').matches && !window.matchMedia('(any-pointer: coarse)').matches;
        document.documentElement.setAttribute('data-oc-motion', reducedMotion ? 'reduced' : 'full');

        /* No motion switch on the page any more. The site just honours the
           operating system, and the MOTION control lives in the desktop app
           (it persists to OceanUiPrefs.json). The one-way "turn on full FX"
           button that used to appear here was the last motion UI on the web,
           so an older element is removed rather than left behind. */
        var oldToggle = document.querySelector('.oc-motion-toggle');
        if (oldToggle && oldToggle.parentNode) oldToggle.parentNode.removeChild(oldToggle);
    }

    /* =================================================================
       17 â€” 1:1 DASHBOARD (valós adatokkal)
       ================================================================= */
    function init1to1Dashboard() {
        var path = location.pathname || '';
        document.body.classList.add('oc-dash-body');
        if (window.__OC_EMBEDDED_DASH__) path = '/dashboard';
        if (path.indexOf('/dashboard') !== 0) return;

        var isHome = (path === '/dashboard' || path === '/dashboard/' || path === '/dashboard/stats');
        var isPins = path.indexOf('/dashboard/pins') === 0;
        var isStrings = path.indexOf('/dashboard/strings') === 0;
        var isEnterprise = path.indexOf('/dashboard/enterprise') === 0;
        var isSettings = path.indexOf('/dashboard/settings') === 0;
        var isDetections = path.indexOf('/dashboard/detections') === 0;
        var isWatchlist = path.indexOf('/dashboard/watchlist') === 0;
        var isTickets = path.indexOf('/dashboard/tickets') === 0;
        var isChat = path.indexOf('/dashboard/chat') === 0;
        var isLeaderboard = path.indexOf('/dashboard/leaderboard') === 0;
        var isApiKeys = path.indexOf('/dashboard/api-keys') === 0;
        var isCustomGui = path.indexOf('/dashboard/custom-gui') === 0;
        var isPublicsGui = path.indexOf('/dashboard/publics-gui') === 0;
        var isProfiles = path.indexOf('/dashboard/profiles') === 0;

        var route = isPins ? 'pins' : isStrings ? 'strings' : isEnterprise ? 'configs' :
            isCustomGui ? 'custom-gui' : isPublicsGui ? 'publics-gui' :
            isDetections ? 'detections' : isWatchlist ? 'watchlist' : isProfiles ? 'profiles' :
            isTickets ? 'tickets' : isChat ? 'chat' : isLeaderboard ? 'leaderboard' :
            isSettings ? 'settings' : isApiKeys ? 'api-keys' : 'home';

        var routeTitles = { home: 'Home', pins: 'Pins', strings: 'Custom Strings', configs: 'Configs',
            'custom-gui': 'Custom Gui', 'publics-gui': 'Publics Gui', detections: 'Detections',
            watchlist: 'Watchlist', profiles: 'Player Profiles', tickets: 'Support', chat: 'Chat',
            leaderboard: 'Leaderboard', settings: 'Settings', 'api-keys': 'API Keys' };

        function iconHome() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/></svg>'; }
        function iconPin() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>'; }
        function iconCode() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4"/></svg>'; }
        function iconCloud() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M3 15a4 4 0 004 4h9a5 5 0 001-9.999 5.002 5.002 0 00-9.78 2.096A4.001 4.001 0 003 15z"/></svg>'; }
        function iconPencil() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z"/></svg>'; }
        function iconShield() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>'; }
        function iconBell() { return '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9"/></svg>'; }
        function iconClock() { return '<svg class="w-4 h-4 text-[#a855f7]" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>'; }
        function iconTrash() { return '<svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>'; }
        function iconKey() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z"/></svg>'; }
        function iconChart() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/></svg>'; }
        function iconEye() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/></svg>'; }
        function iconTicket() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 5v2m0 4v2m0 4v2M5 5a2 2 0 00-2 2v3a2 2 0 110 4v3a2 2 0 002 2h14a2 2 0 002-2v-3a2 2 0 110-4V7a2 2 0 00-2-2H5z"/></svg>'; }
        function iconChat() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/></svg>'; }
        function iconTrophy() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12l2 2 4-4M7.835 4.697a3.42 3.42 0 001.946-.806 3.42 3.42 0 014.438 0 3.42 3.42 0 001.946.806 3.42 3.42 0 013.138 3.138 3.42 3.42 0 00.806 1.946 3.42 3.42 0 010 4.438 3.42 3.42 0 00-.806 1.946 3.42 3.42 0 01-3.138 3.138 3.42 3.42 0 00-1.946.806 3.42 3.42 0 01-4.438 0 3.42 3.42 0 00-1.946-.806 3.42 3.42 0 01-3.138-3.138 3.42 3.42 0 00-.806-1.946 3.42 3.42 0 010-4.438 3.42 3.42 0 00.806-1.946 3.42 3.42 0 013.138-3.138z"/></svg>'; }
        function iconSettings() { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.066 2.573c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.573 1.066c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.066-2.573c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/></svg>'; }

        var user = { username: 'User' };
        var stats = {};
        var scans = [];
        var pins = [];
        var cfg = {};

        var navItems = [
            { href: '/dashboard', label: 'Home', icon: iconHome(), active: isHome },
            { href: '/dashboard/pins', label: 'Pins', icon: iconPin(), active: isPins },
            { href: '/dashboard/strings', label: 'Custom Strings', icon: iconCode(), active: isStrings },
            { href: '/dashboard/enterprise', label: 'Configs', icon: iconCloud(), active: isEnterprise },
            'divider',
            { href: '/dashboard/custom-gui', label: 'Custom Gui', icon: iconPencil(), active: isCustomGui },
            { href: '/dashboard/publics-gui', label: 'Publics Gui', icon: iconCloud(), active: isPublicsGui },
            'divider',
            { href: '/dashboard/detections', label: 'Detections', icon: iconShield(), active: isDetections },
            { href: '/dashboard/watchlist', label: 'Watchlist', icon: iconEye(), active: isWatchlist },
            { href: '/dashboard/profiles', label: 'Profiles', icon: iconKey(), active: isProfiles },
            { href: '/dashboard/tickets', label: 'Support', icon: iconTicket(), active: isTickets },
            { href: '/dashboard/chat', label: 'Chat', icon: iconChat(), active: isChat },
            { href: '/dashboard/leaderboard', label: 'Leaderboard', icon: iconTrophy(), active: isLeaderboard },
            { href: '/dashboard/settings', label: 'Settings', icon: iconSettings(), active: isSettings },
            { href: '/dashboard/api-keys', label: 'API Keys', icon: iconKey(), active: isApiKeys }
        ];

        var navHtml = navItems.map(function(item) {
            if (item === 'divider') return '<div style="margin:8px 0;border-top:1px solid #1a1325;"></div>';
            var cls = 'oc-nav-item' + (item.active ? ' oc-active' : '');
            return '<a href="' + item.href + '" class="' + cls + '">' + item.icon + '<span>' + item.label + '</span></a>';
        }).join('');

        /* Badges: black outline chips lit by a single accent — no bright
           filled pills. SCANNING is the live one: molten + mini progress. */
        function statusBadge(status) {
            var s = (status || '').toLowerCase();
            function chip(label, color, bg) {
                return '<span style="background:' + (bg || 'rgba(10,6,18,.6)') + ';color:' + color + ';border:1px solid ' + color.replace(/rgb\(([^)]+)\)/, 'rgba($1,.4)') + ';padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;letter-spacing:.04em;">' + label + '</span>';
            }
            if (s === 'scanning') return '<span class="oc-scan-row"><i class="oc-scan-dot"></i>SCANNING' +
                '<span class="oc-scan-bar"><i style="width:64%"></i></span></span>';
            if (s === 'complete' || s === 'clean' || s.indexOf('clean') > -1) return chip('CLEAN', 'rgb(134, 214, 170)');
            if (s.indexOf('cheat') > -1) return chip('CHEAT', 'rgb(232, 122, 133)');
            if (s.indexOf('suspicious') > -1) return chip('SUSPICIOUS', 'rgb(224, 186, 122)');
            if (s === 'pending') return chip('PENDING', 'rgb(224, 186, 122)');
            if (s === 'active') return chip('ACTIVE', 'rgb(196, 132, 252)');
            if (s === 'used') return chip('USED', 'rgb(139, 139, 160)');
            return chip((status || 'UNKNOWN').toUpperCase(), 'rgb(139, 139, 160)');
        }

        /* Live scanner card: molten stége a dashboard tetején, amíg a
           desktop scanner dolgozik egy pinen. */
        function ocLiveScannerCard(content) {
            if (!content) return;
            var active = (pins || []).filter(function (p) {
                return ((p.status || '') + '').toLowerCase() === 'scanning';
            });
            var scanning = (stats && (stats.scanningPins || 0)) || 0;
            if (!active.length && !scanning) return;
            if (content.querySelector('.oc-scan-live')) return;

            var card = document.createElement('div');
            card.className = 'oc-scan oc-scan-live';
            card.setAttribute('data-scan-stage', 'scanning');
            card.style.margin = '0 0 18px';
            card.style.minHeight = '190px';

            var head = document.createElement('div');
            head.style.position = 'relative';
            head.style.zIndex = '3';
            head.style.padding = '18px 18px 0';
            head.innerHTML =
                '<span style="font-size:11px;letter-spacing:.18em;text-transform:uppercase;color:rgba(245,243,255,.45);display:block;margin-bottom:6px;">Live scanner</span>' +
                '<h3 style="margin:0 0 4px;font-size:16px;font-weight:600;color:#f5f3ff;">' +
                (active.length ? active.length + ' pin' + (active.length > 1 ? 's' : '') + ' in progress' : 'Scanner working') +
                '</h3>' +
                '<p style="margin:0;font-size:13px;color:rgba(245,243,255,.6);">' +
                (active[0] ? 'Pin ' + (active[0].code || active[0].pin || '') + ' — live detections stream below.' : 'Waiting for scan data...') +
                '</p>';
            card.appendChild(head);
            content.insertBefore(card, content.firstChild);

            // Feed the stage from the real desktop scanner (POST /api/scanner/live).
            // With no scanner running the stage falls back to its synthetic stream.
            var liveCode = (active[0] && (active[0].code || active[0].pin)) || '';
            card.setAttribute('data-scan-code', liveCode);
            if (window.ocScanStage) window.ocScanStage(card, { state: 'scanning', code: liveCode });
            else setTimeout(function () { if (window.ocScanStage) window.ocScanStage(card, { state: 'scanning', code: liveCode }); }, 800);
        }

        function timeAgo(ts) {
            if (!ts) return '--';
            var diff = Date.now() - new Date(ts).getTime();
            if (diff < 60000) return 'just now';
            if (diff < 3600000) return Math.floor(diff / 60000) + 'm ago';
            if (diff < 86400000) return Math.floor(diff / 3600000) + 'h ago';
            return Math.floor(diff / 86400000) + 'd ago';
        }

        function skeletonBars(n) {
            var h = '';
            for (var i = 0; i < (n || 4); i++) {
                h += '<div style="height:52px;background:#120c22;border:1px solid #1f1436;border-radius:12px;padding:14px 16px;display:flex;flex-direction:column;justify-content:center;gap:8px;">' +
                    '<div style="width:' + (50 + Math.random() * 40) + '%;height:8px;background:#1d1430;border-radius:4px;"></div>' +
                    '<div style="width:' + (30 + Math.random() * 30) + '%;height:6px;background:#16102a;border-radius:4px;"></div>' +
                    '</div>';
            }
            return h;
        }

        function miniSkeleton(n) {
            var h = '';
            for (var i = 0; i < (n || 3); i++) {
                h += '<div style="flex:1;padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;height:110px;display:flex;flex-direction:column;justify-content:space-between;">' +
                    '<div style="width:48px;height:8px;background:#1d1430;border-radius:4px;"></div>' +
                    '<div style="width:80px;height:10px;background:#181028;border-radius:4px;"></div></div>';
            }
            return h;
        }

        var chartMonthLabels = [];
        var now2 = new Date();
        for (var mi = 5; mi >= 0; mi--) {
            var d = new Date(now2.getFullYear(), now2.getMonth() - mi, 1);
            chartMonthLabels.push(d.toLocaleString('en', { month: 'short' }));
        }
        var monthLabelHtml = chartMonthLabels.map(function(l) { return '<span>' + l + '</span>'; }).join('');

        function chartSvg(scansData) {
            var counts = new Array(6).fill(0);
            if (scansData && scansData.length) {
                scansData.forEach(function(s) {
                    var t = s.timestamp ? new Date(s.timestamp).getTime() : 0;
                    for (var i = 0; i < 6; i++) {
                        var mStart = new Date(now2.getFullYear(), now2.getMonth() - (5 - i), 1).getTime();
                        var mEnd = new Date(now2.getFullYear(), now2.getMonth() - (5 - i) + 1, 1).getTime();
                        if (t >= mStart && t < mEnd) { counts[i]++; break; }
                    }
                });
            }
            var maxV = Math.max.apply(null, counts.concat([1]));
            var pts = counts.map(function(v, i) {
                var x = (i / 5) * 500;
                var y = 140 - (v / maxV) * 120;
                return x + ' ' + y;
            });
            var line = 'M ' + pts.join(' L ');
            var fill = line + ' L 500 150 L 0 150 Z';
            return '<svg viewBox="0 0 500 150" preserveAspectRatio="none" style="width:100%;height:100%;">' +
                '<defs><linearGradient id="pgDash" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stop-color="#9333ea" stop-opacity="0.4"/><stop offset="100%" stop-color="#9333ea" stop-opacity="0.02"/></linearGradient></defs>' +
                '<path d="' + fill + '" fill="url(#pgDash)"/>' +
                '<path d="' + line + '" fill="none" stroke="#a855f7" stroke-width="2.5" stroke-linecap="round"/>' +
                counts.map(function(v, i) {
                    var cx = (i / 5) * 500;
                    var cy = 140 - (v / maxV) * 120;
                    return '<circle cx="' + cx + '" cy="' + cy + '" r="4" fill="#c084fc" stroke="#0a0714" stroke-width="2"/>';
                }).join('') +
                '</svg>';
        }

        function renderHomeContent(user, stats, scans, pins) {
            var recentPins = (pins || []).slice(0, 8);
            var recentScans = (scans || []).slice(0, 5);

            var pinsTableRows = recentPins.map(function(k) {
                var code = k.code || k.key || '--';
                var game = k.game || '--';
                var st = k.status || 'active';
                var created = k.createdAt ? new Date(k.createdAt).toLocaleDateString() : '--';
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:12px 16px;font-size:13px;color:#e8e4f6;font-family:monospace;font-weight:600;">' + code + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;color:#a29cb8;">' + game + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;">' + statusBadge(st) + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;color:#a29cb8;">' + created + '</td>' +
                    '</tr>';
            }).join('');

            var scansRows = recentScans.map(function(s) {
                var player = s.username || s.playerName || '--';
                var pc = s.pcName || '--';
                var game = s.game || 'FiveM';
                var st = s.status || '--';
                var ts = timeAgo(s.timestamp);
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:10px 14px;font-size:13px;color:#e8e4f6;">' + player + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;color:#a29cb8;">' + pc + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;color:#a29cb8;">' + game + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;">' + statusBadge(st) + '</td>' +
                    '<td style="padding:10px 14px;font-size:12px;color:#7d7794;">' + ts + '</td>' +
                    '</tr>';
            }).join('');

            return '' +
            '<div class="grid grid-cols-12 gap-5">' +

            '<div class="col-span-8 space-y-5">' +

            '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">' +
                    '<div style="font-weight:600;color:#fff;font-size:14px;">Scan Activity</div>' +
                    '<div style="font-size:12px;color:#a29cb8;">Last 6 months</div>' +
                '</div>' +
                '<div style="position:relative;height:180px;">' + chartSvg(scans) + '</div>' +
                '<div style="display:flex;justify-content:space-between;font-size:12px;color:#808098;margin-top:8px;padding:0 4px;">' + monthLabelHtml + '</div>' +
            '</div>' +

            '<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;">' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Total Pins</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#c084fc;">' + (stats.totalPins || 0) + '</div>' +
                    '<div style="font-size:11px;color:#a855f7;margin-top:4px;">' + (stats.pinsThisMonth || 0) + ' this month</div>' +
                '</div>' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Total Scans</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#8fb6e8;">' + (stats.totalScans || 0) + '</div>' +
                    '<div style="font-size:11px;color:#a855f7;margin-top:4px;">' + (stats.scansThisMonth || 0) + ' this month</div>' +
                '</div>' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Detections</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#e0848f;">' + (stats.detected || 0) + '</div>' +
                    '<div style="font-size:11px;color:#e0848f;margin-top:4px;">' + (stats.cheating || 0) + ' cheats found</div>' +
                '</div>' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Profiles</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#7dc9a0;">' + (stats.profiles || 0) + '</div>' +
                    '<div style="font-size:11px;color:#7dc9a0;margin-top:4px;">' + (stats.clean || 0) + ' clean</div>' +
                '</div>' +
            '</div>' +

            (recentPins.length ? '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px;">' +
                    '<div style="font-weight:600;color:#fff;font-size:14px;">Recent Pins</div>' +
                    '<a href="/dashboard/pins" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">View all</a>' +
                '</div>' +
                '<div style="overflow-x:auto;">' +
                '<table style="width:100%;border-collapse:collapse;">' +
                    '<thead><tr style="border-bottom:1px solid #1b122b;">' +
                        '<th style="padding:10px 16px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Code</th>' +
                        '<th style="padding:10px 16px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Game</th>' +
                        '<th style="padding:10px 16px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Status</th>' +
                        '<th style="padding:10px 16px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;letter-spacing:.05em;">Created</th>' +
                    '</tr></thead>' +
                    '<tbody>' + pinsTableRows + '</tbody>' +
                '</table></div></div>' : '') +

            (recentScans.length ? '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px;">' +
                    '<div style="font-weight:600;color:#fff;font-size:14px;">Recent Scans</div>' +
                '</div>' +
                '<div style="overflow-x:auto;">' +
                '<table style="width:100%;border-collapse:collapse;">' +
                    '<thead><tr style="border-bottom:1px solid #1b122b;">' +
                        '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;">Player</th>' +
                        '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;">PC Name</th>' +
                        '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;">Game</th>' +
                        '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;">Status</th>' +
                        '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;font-weight:600;text-transform:uppercase;">When</th>' +
                    '</tr></thead>' +
                    '<tbody>' + scansRows + '</tbody>' +
                '</table></div></div>' : '') +

            '</div>' +

            '<div class="col-span-4 space-y-5">' +

            '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="font-weight:600;color:#fff;font-size:14px;margin-bottom:4px;">Announcements</div>' +
                '<div style="font-size:12px;color:#808098;margin-bottom:14px;">Latest from the Ocean team</div>' +
                '<div style="display:flex;flex-direction:column;gap:10px;">' +
                    '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;">' +
                        '<div style="font-size:13px;color:#e8e4f6;font-weight:600;">Ocean v2.4 Released</div>' +
                        '<div style="font-size:11px;color:#a29cb8;margin-top:4px;">New detection engine with improved accuracy</div>' +
                        '<div style="font-size:10px;color:#7d7794;margin-top:6px;">Sep 6, 2026</div>' +
                    '</div>' +
                    '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;">' +
                        '<div style="font-size:13px;color:#e8e4f6;font-weight:600;">FiveM Update Support</div>' +
                        '<div style="font-size:11px;color:#a29cb8;margin-top:4px;">Full compatibility with latest FiveM build</div>' +
                        '<div style="font-size:10px;color:#7d7794;margin-top:6px;">Sep 4, 2026</div>' +
                    '</div>' +
                    '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;">' +
                        '<div style="font-size:13px;color:#e8e4f6;font-weight:600;">Custom Scripts Feature</div>' +
                        '<div style="font-size:11px;color:#a29cb8;margin-top:4px;">Write your own Lua detection scripts in Ocean Lab</div>' +
                        '<div style="font-size:10px;color:#7d7794;margin-top:6px;">Sep 2, 2026</div>' +
                    '</div>' +
                '</div>' +
            '</div>' +

            '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="font-weight:600;color:#fff;font-size:14px;margin-bottom:4px;">License</div>' +
                '<div style="font-size:12px;color:#808098;margin-bottom:12px;">Your current plan</div>' +
                '<div style="padding:14px;border-radius:12px;background:rgba(168,85,247,.08);border:1px solid rgba(168,85,247,.25);display:flex;align-items:center;gap:12px;">' +
                    '<div style="width:40px;height:40px;border-radius:10px;background:#a855f7;display:flex;align-items:center;justify-content:center;">' +
                        '<svg class="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>' +
                    '</div>' +
                    '<div>' +
                        '<div style="font-size:14px;font-weight:700;color:#fff;">Active License</div>' +
                        '<div style="font-size:11px;color:#a855f7;">Full access to all features</div>' +
                    '</div>' +
                '</div>' +
            '</div>' +

            '<div class="oc-card">' +
                '<div style="font-weight:600;color:#fff;font-size:14px;margin-bottom:4px;">Quick Actions</div>' +
                '<div style="font-size:12px;color:#808098;margin-bottom:12px;">Common tasks</div>' +
                '<div style="display:flex;flex-direction:column;gap:8px;">' +
                    '<button onclick="if(window.__OC_MAKE_PIN)window.__OC_MAKE_PIN()" class="oc-btn oc-btn-primary" style="width:100%;">+ Create New Pin</button>' +
                    '<a href="/dashboard/pins" class="oc-btn oc-btn-ghost" style="display:block;width:100%;text-decoration:none;text-align:center;">View All Pins</a>' +
                    '<a href="/dashboard/strings" class="oc-btn oc-btn-ghost" style="display:block;width:100%;text-decoration:none;text-align:center;">Custom Strings</a>' +
                '</div>' +
            '</div>' +

            '</div>' +
            '</div>';
        }

        function fmtDate(ts) {
            if (!ts) return '--';
            try { return new Date(ts).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }); } catch (e) { return '--'; }
        }

        function card(head, body, right) {
            return '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px;">' +
                    '<div style="font-weight:600;color:#fff;font-size:14px;">' + head + '</div>' +
                    (right || '<div style="font-size:12px;color:#808098;"></div>') +
                '</div>' + body + '</div>';
        }

        function iconMini(d) { return '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="' + d + '"/></svg>'; }

         /* ---- PINS page ---- */
         function renderPinsPage(user, stats, pins, scans) {
             var breakdown = [
                 { label: 'Active', v: stats.activePins || 0, c: '#7dc9a0' },
                 { label: 'Scanning', v: stats.scanningPins || 0, c: '#a855f7' },
                 { label: 'Complete', v: stats.completedPins || 0, c: '#8fb6e8' },
                 { label: 'Pending', v: stats.pendingPins || 0, c: '#d8b46a' },
                 { label: 'Expired', v: stats.expiredPins || 0, c: '#a29cb8' }
             ];
             var rows = (pins || []).map(function(k) {
                 var code = k.code || k.key || k.pin || '--';
                 var game = k.game || 'FiveM';
                 var st = k.status || 'active';
                 var used = k.usedCount || 0;
                 var created = fmtDate(k.createdAt);
                 var scanId = k.scanId || '';
                 var hasScan = scanId !== '';
                 return '<tr style="border-bottom:1px solid rgba(168,85,247,0.12);">' +
                     '<td style="padding:14px 16px;font-family:monospace;font-size:14px;color:#e9d5ff;font-weight:600;letter-spacing:0.02em;">' + code + '</td>' +
                     '<td style="padding:14px 16px;font-size:13px;color:#a78bfa;">' + game + '</td>' +
                     '<td style="padding:14px 16px;font-size:13px;">' + statusBadge(st) + '</td>' +
                     '<td style="padding:14px 16px;font-size:13px;color:#c4b5fd;">' + used + '</td>' +
                     '<td style="padding:14px 16px;font-size:12px;color:#7c7c9c;">' + created + '</td>' +
                     '<td style="padding:14px 16px;text-align:right;">' +
                         '<button data-oc-open-pin="' + code + '" style="padding:8px 20px;border-radius:10px;background:linear-gradient(135deg,rgba(168,85,247,0.18),rgba(192,132,252,0.12));border:1px solid rgba(168,85,247,0.45);color:#e9d5ff;font-size:12px;font-weight:700;cursor:pointer;transition:all .25s;letter-spacing:0.04em;text-transform:uppercase;box-shadow:0 0 12px rgba(168,85,247,0.15),inset 0 0 20px rgba(168,85,247,0.05);position:relative;overflow:hidden;">' +
                             '<span style="position:relative;z-index:1;">View</span>' +
                             '<span style="position:absolute;inset:0;border-radius:inherit;background:linear-gradient(90deg,transparent,rgba(192,132,252,0.18),transparent);background-size:200% 100%;animation:oc-btn-shift 2s ease-in-out infinite;opacity:0;transition:opacity .25s;"></span>' +
                         '</button>' +
                     '</td></tr>';
             }).join('');

             var bd = breakdown.map(function(b) {
                 return '<div style="padding:18px 14px;border-radius:14px;background:rgba(16,10,30,0.75);border:1px solid rgba(168,85,247,0.18);text-align:center;backdrop-filter:blur(8px);box-shadow:0 4px 24px rgba(0,0,0,0.3);transition:transform .2s,border-color .2s;" onmouseover="this.style.transform=\'translateY(-3px)\';this.style.borderColor=\'rgba(168,85,247,0.45)\'" onmouseout="this.style.transform=\'\';this.style.borderColor=\'rgba(168,85,247,0.18)\'">' +
                     '<div style="font-size:28px;font-weight:800;color:' + b.c + ';text-shadow:0 0 18px ' + b.c + '55;">' + b.v + '</div>' +
                     '<div style="font-size:11px;color:#808098;margin-top:6px;text-transform:uppercase;letter-spacing:0.06em;">' + b.label + '</div></div>';
             }).join('');

             var completed = (pins || []).filter(function(k) { return k.status === 'completed'; }).length;
             var active = (pins || []).filter(function(k) { return k.status === 'active'; }).length;

             return '<div class="grid grid-cols-12 gap-5">' +
                 '<div class="col-span-12">' +
                     '<div style="display:grid;grid-template-columns:repeat(5,1fr);gap:14px;">' + bd + '</div>' +
                 '</div>' +
                 '<div class="col-span-12">' + card('All Pins',
                     '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:14px;">' +
                         '<span style="font-size:12px;color:#808098;">' + (pins||[]).length + ' pin(s) — ' + active + ' active · ' + completed + ' completed</span>' +
                     '</div>' +
                     '<div style="overflow-x:auto;"><table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid rgba(168,85,247,0.18);">' +
                         '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Code</th>' +
                         '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Game</th>' +
                         '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Status</th>' +
                         '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Uses</th>' +
                         '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Created</th>' +
                      '<th style="padding:10px 14px;text-align:right;font-size:11px;color:#808098;text-transform:uppercase;letter-spacing:0.06em;">Action</th>' +
                      '</tr></thead><tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;font-size:13px;" colspan="6">No pins created yet.</td></tr>') + '</tbody></table></div>',
                      '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') +
                  '</div>' +
                 '<div class="col-span-12">' + card('Create New Pin',
                     '<p style="font-size:13px;color:#a29cb8;margin:0 0 14px 0;">Generate a fresh scan pin, share it with a player, and watch the live result.</p>' +
                     '<button onclick="if(window.__OC_MAKE_PIN)window.__OC_MAKE_PIN()" class="oc-btn oc-btn-primary">+ Create New Pin</button>') +
                 '</div>' +
                 '</div>';
         }

        /* ================================================================
           DETECTION MODULES + CUSTOM STRINGS
           ----------------------------------------------------------------
           OB_MODULES drives both the Configs switches and the Detections
           catalog. Every key is persisted in the service config (cfg) so
           the desktop scanner pulls it from /api/scanner/config and reports
           back which modules actually ran inside the scan report.
           ================================================================ */
        var OB_MODULES = [
            { key: 'modulePrefetch', label: 'Prefetch', desc: 'Parses .pf files for last-run time and run count, and flags typed or swapped (duplicate-hash) prefetch entries.' },
            { key: 'moduleAmcache', label: 'Amcache', desc: 'Amcache.hve binaries, loaded drivers and their SHA-1 - matched against known signatures and your custom hashes.' },
            { key: 'moduleShimcache', label: 'ShimCache', desc: 'AppCompatCache entries survive deletion, so a moved or removed executable still shows up here.' },
            { key: 'moduleBam', label: 'BAM / DAM', desc: 'Background Activity Moderator per-user execution log. Flags a stopped, paused or cleaned BAM and targeted key deletions.' },
            { key: 'moduleEvtx', label: 'Event logs (EVTX)', desc: 'System, Security and PowerShell channels: log clears, read-only logs, renamed logs and service control events.' },
            { key: 'moduleUsn', label: 'USN Journal', desc: 'Detects a shrunk, deleted or missing journal - the classic way to hide removed cheat files.' },
            { key: 'modulePca', label: 'PCAsvc / PCA', desc: 'Program Compatibility Assistant records for apps that crashed or were blocked - common with one-shot loaders.' },
            { key: 'moduleUsb', label: 'USB / PnP + DMA', desc: 'Device arrival and removal history, DMA Guard state and vendor IDs - surfaces DMA cards and bypass hardware.' },
            { key: 'moduleIntegrity', label: 'File integrity', desc: 'Unsigned executions, PE files run under renamed extensions, elevated unsigned binaries, .bat/.cmd and Python loaders.' },
            { key: 'moduleInjection', label: 'Injection traces', desc: 'Injected DLLs, PE injection out of instance, registry-resident payloads and NTFS alternate data streams.' },
            { key: 'moduleNetwork', label: 'Network traces', desc: 'DNS client cache lookups, hosts file tampering, proxy or VPN presence and browser process memory keywords.' },
            { key: 'moduleCleaners', label: 'Trace cleaners', desc: 'Detects cleaner usage: wiped MRU and shellbags, resized journals, cleared logs and removed crash dumps.' }
        ];

        var OB_STRING_CATS = [
            { id: 'cheat', label: 'Cheat / menu' },
            { id: 'executable', label: 'Executable / loader' },
            { id: 'injector', label: 'Injector / mapper' },
            { id: 'cleaner', label: 'Trace cleaner' },
            { id: 'dma', label: 'DMA / hardware' },
            { id: 'network', label: 'Network / VPN' },
            { id: 'path', label: 'File path / folder' },
            { id: 'other', label: 'Other' }
        ];

        var OB_CATALOG = [
            { cat: 'Execution artifacts', tag: 'warning', name: 'Prefetch', desc: 'Parses .pf files for last-run time and run count, and flags typed or duplicate-hash prefetch used to wipe an entry.' },
            { cat: 'Execution artifacts', tag: 'warning', name: 'Amcache', desc: 'Reads Amcache.hve for executed binaries and loaded drivers with their SHA-1, including your own custom hashes.' },
            { cat: 'Execution artifacts', tag: 'warning', name: 'ShimCache', desc: 'AppCompatCache entries survive deletion, so a moved or removed executable still appears here.' },
            { cat: 'Execution artifacts', tag: 'warning', name: 'BAM / DAM', desc: 'Per-user execution log. Flags a stopped, paused or cleaned BAM plus targeted registry key deletions.' },
            { cat: 'Execution artifacts', tag: 'info', name: 'UserAssist', desc: 'Per-user GUI launch counts, used as a second opinion on what was actually opened.' },
            { cat: 'Execution artifacts', tag: 'info', name: 'PCAsvc / PCA', desc: 'Program Compatibility Assistant records for apps that crashed or were blocked - common with one-shot loaders.' },
            { cat: 'Execution artifacts', tag: 'info', name: 'SRUM / activities', desc: 'System Resource Usage Monitor keeps longer-lived per-app network and CPU history; cleansing it gets flagged.' },
            { cat: 'Event logs', tag: 'warning', name: 'Cleared event log', desc: 'A channel was cleared. Mass clears, or System and Security clears, escalate to a detection.' },
            { cat: 'Event logs', tag: 'warning', name: 'Read-only or renamed log', desc: 'An .evtx file flipped to read-only or renamed off its expected name silently freezes logging without a clear event.' },
            { cat: 'Event logs', tag: 'info', name: 'Service control events', desc: 'Service install, stop and restart events show drivers or logging services touched mid-session.' },
            { cat: 'Event logs', tag: 'warning', name: 'PowerShell history', desc: 'Flags a shrunk PowerShell log, encoded payloads and non-standard profiles that re-arm a bypass at launch.' },
            { cat: 'Bypass and cleaners', tag: 'warning', name: 'USN Journal tampering', desc: 'A shrunken or missing journal overwrites delete history within minutes - the classic way to hide removed cheat files.' },
            { cat: 'Bypass and cleaners', tag: 'warning', name: 'MRU / shellbag wipe', desc: 'Wiped recently-opened lists, OpenSavePidlMRU and shellbags aimed at hiding which folders were visited.' },
            { cat: 'Bypass and cleaners', tag: 'info', name: 'Crash dump removal', desc: 'A missing CrashDump folder suggests traces of a cheat that crashed were deleted.' },
            { cat: 'Bypass and cleaners', tag: 'warning', name: 'Recycle bin / partition churn', desc: 'Emptied recycle bin plus created-then-deleted partitions or virtual disks used to stage and discard cheat data.' },
            { cat: 'Bypass and cleaners', tag: 'info', name: 'System time change', desc: 'A manually shifted clock makes cheat activity look like it fell outside the session window.' },
            { cat: 'Bypass and cleaners', tag: 'warning', name: 'FAT / letterless volumes', desc: 'FAT has no USN journal and letterless volumes stay out of Explorer - both are used as no-record landing zones.' },
            { cat: 'Hardware and DMA', tag: 'warning', name: 'USB and PnP history', desc: 'Devices connected and removed before or after logon; unknown vendor IDs surface DMA cards and bypass hardware.' },
            { cat: 'Hardware and DMA', tag: 'info', name: 'Kernel DMA Protection', desc: 'DMA Guard state plus a firmware fingerprint check that flags a DMA card presenting itself as something else.' },
            { cat: 'Hardware and DMA', tag: 'detection', name: 'Serial and HWID reuse', desc: 'Machine serial, disk serial and HWID are cross-matched across every scan to catch spoofers and shared machines.' },
            { cat: 'Hardware and DMA', tag: 'warning', name: 'HDMI fuser / capture', desc: 'A display connection inconsistent with a normal monitor - a second PC merged into the video path.' },
            { cat: 'Injection and memory', tag: 'warning', name: 'Injected DLL traces', desc: 'A DLL previously injected into a process, including records that survive after the cheat file is gone.' },
            { cat: 'Injection and memory', tag: 'detection', name: 'PE injection out of instance', desc: 'Cheat code injected into Notepad, OSK, Calculator or PowerShell - strong evidence of prior cheating.' },
            { cat: 'Injection and memory', tag: 'warning', name: 'Alternate data streams', desc: 'A cheat payload hidden in an NTFS alternate data stream attached to an innocent looking file or folder.' },
            { cat: 'Injection and memory', tag: 'warning', name: 'Registry-resident payload', desc: 'Strings and YARA matches found inside the registry for payloads that never touch disk.' },
            { cat: 'Injection and memory', tag: 'warning', name: 'Disk and file integrity', desc: 'Unsigned executions, PE files run under renamed extensions, elevated unsigned binaries and self-deleting scripts.' },
            { cat: 'Network', tag: 'warning', name: 'DNS cache lookup', desc: 'A cheat-related host present in the DNS client cache, even when browser history was cleared.' },
            { cat: 'Network', tag: 'warning', name: 'Browser memory keyword', desc: 'Cheat and bypass keywords found in live Chrome, Edge, Brave, Firefox or Opera process memory.' },
            { cat: 'Network', tag: 'warning', name: 'Hosts file and proxy', desc: 'Hosts file tampering, proxy or VPN presence and streamproof registry edits that hide overlays from recording.' },
            { cat: 'Server supplied', tag: 'detection', name: 'Custom string match', desc: 'Any keyword you add on the Custom Strings page, searched across disk, process memory, registry and prefetch.' },
            { cat: 'Server supplied', tag: 'detection', name: 'Custom Amcache hash', desc: 'A hash you supply is checked against Amcache: found means it ran, not found means it ran and was removed.' },
            { cat: 'Server supplied', tag: 'warning', name: 'YARA rule match', desc: 'Generic and community rules surface unknown loaders and custom builds before they get a public name.' }
        ];

        function obEsc(s) {
            return String(s === undefined || s === null ? '' : s)
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function obCatalogCard() {
            var groups = {}, order = [];
            OB_CATALOG.forEach(function(d) {
                if (!groups[d.cat]) { groups[d.cat] = []; order.push(d.cat); }
                groups[d.cat].push(d);
            });
            var body = order.map(function(cat) {
                var items = groups[cat].map(function(d) {
                    var tag = d.tag === 'warning' ? 'ob-tag ob-tag-warn' : d.tag === 'info' ? 'ob-tag ob-tag-info' : 'ob-tag';
                    return '<div class="ob-detect" data-ob-spot><div style="display:flex;align-items:center;gap:8px;margin-bottom:5px;">' +
                        '<span class="ob-detect-name">' + obEsc(d.name) + '</span>' +
                        '<span class="' + tag + '" style="margin-left:auto;">' + d.tag + '</span></div>' +
                        '<div class="ob-detect-desc">' + obEsc(d.desc) + '</div></div>';
                }).join('');
                return '<div class="ob-cat"><div class="ob-cat-head"><span class="ob-cat-title">' + cat + '</span>' +
                    '<span class="ob-cat-count">' + groups[cat].length + ' checks</span></div>' +
                    '<div class="ob-detect-grid">' + items + '</div></div>';
            }).join('');
            return card('Detection Catalog',
                '<p style="font-size:12.5px;color:#a29cb8;margin:0 0 16px 0;">The artifact surface Ocean reads on a suspect machine - the same ground the commercial FiveM checkers work from (Prefetch, Amcache, ShimCache, BAM, EVTX, USN Journal, PCAsvc and USB/DMA history). Turn any module on or off on the Configs page and the desktop scan follows it.</p>' + body);
        }

        /* ---- Custom Strings: real editor, synced to the desktop scanner ---- */
        function ocStringList() {
            var raw = cfg.customStrings;
            var out = [];
            if (Array.isArray(raw)) {
                raw.forEach(function(x) {
                    if (typeof x === 'string') { var t = String(x).trim(); if (t) out.push({ term: t, desc: '', category: 'cheat' }); }
                    else if (x && (x.term || x.name)) out.push({ term: String(x.term || x.name), desc: String(x.desc || ''), category: String(x.category || 'cheat') });
                });
            } else if (raw && typeof raw === 'object') {
                Object.keys(raw).forEach(function(k) {
                    var v = raw[k];
                    if (v && typeof v === 'object') out.push({ term: k, desc: String(v.desc || ''), category: String(v.category || 'cheat') });
                    else out.push({ term: k, desc: String(v || ''), category: 'cheat' });
                });
            }
            return out;
        }
        function ocStringSync() {
            cfg.customStrings = ocStringList();
            ocSaveCfg();
            document.querySelectorAll('.ob-string-badge').forEach(function(el) {
                el.textContent = cfg.customStrings.length + ' synced';
            });
        }
        function ocStringSeed() {
            var seeds = ['nvevade', 'fivem_cheat.dll', 'injector.exe', 'mapper.exe', 'prefetch_cleaner', 'bam_cleaner', 'silent_aim', 'triggerbot', 'aimbot', 'esp.dll', 'shadow.exe', 'guardian.exe'];
            var list = ocStringList();
            var have = {};
            list.forEach(function(s) { have[s.term.toLowerCase()] = 1; });
            var added = 0;
            seeds.forEach(function(t) {
                if (have[t.toLowerCase()]) return;
                list.push({ term: t, desc: 'Seeded detection string', category: 'cheat' });
                added++;
            });
            if (added) { cfg.customStrings = list; ocSaveCfg(); }
            return added;
        }

        function renderStringsPage(user, stats, pins, scans) {
            var list = ocStringList();
            var collected = {}, collectedOrder = [];
            (scans || []).forEach(function(s) {
                var txt = s.ocean || '';
                var re = /\*\*([^*]+)\*\*/g, m;
                while ((m = re.exec(txt))) {
                    var k = m[1].trim();
                    if (!k) continue;
                    if (collected[k] === undefined) { collected[k] = 0; collectedOrder.push(k); }
                    collected[k]++;
                }
            });
            var catOptions = OB_STRING_CATS.map(function(c) { return '<option value="' + c.id + '">' + c.label + '</option>'; }).join('');
            var rows = list.map(function(s) {
                var cat = (OB_STRING_CATS.filter(function(c) { return c.id === s.category; })[0] || { label: 'Other' }).label;
                return '<div class="ob-string-row" data-ob-spot>' +
                    '<span class="ob-string-term">' + obEsc(s.term) + '</span>' +
                    '<span class="ob-string-meta">' + cat + (s.desc ? ' - ' + obEsc(s.desc) : '') + '</span>' +
                    '<button class="ob-icon-btn" data-oc-str-del="' + obEsc(s.term) + '" onclick="window.__OC_STR_DEL(this.getAttribute(\'data-oc-str-del\'))" title="Remove">' + iconMini('M6 18L18 6M6 6l12 12') + '</button>' +
                '</div>';
            }).join('');
            var collectedRows = collectedOrder.slice(0, 40).map(function(k) {
                return '<div class="ob-string-row" data-ob-spot><span class="ob-string-term">' + obEsc(k) + '</span>' +
                    '<span class="ob-string-meta">' + collected[k] + ' hit(s) in scans</span></div>';
            }).join('');
            var headerRight = '<div style="display:flex;align-items:center;gap:10px;">' +
                '<span class="ob-string-badge" style="font-size:12px;color:#7d7794;">' + list.length + ' synced</span>' +
                '<button onclick="window.__OC_STR_SEED()">Seed defaults</button>' +
                '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a></div>';
            var editor = '<div class="ob-toolbar">' +
                    '<input id="ob-str-term" class="ob-input" placeholder="string or file name, e.g. nvevade.exe" autocomplete="off">' +
                    '<input id="ob-str-desc" style="width:210px;" placeholder="note (optional)" autocomplete="off">' +
                    '<select id="ob-str-cat" style="width:180px;">' + catOptions + '</select>' +
                    '<button onclick="window.__OC_STR_ADD()">Add string</button>' +
                '</div>' +
                (rows || '<div class="ob-empty">No custom strings yet. Add one above, or seed a starter set.</div>');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Custom Strings',
                    '<p style="font-size:12.5px;color:#a29cb8;margin:0 0 14px 0;">Server-side detection keywords. The desktop scanner pulls this list with the rest of the config on every pin, then searches disk, process memory, registry and prefetch for each one - exactly like the custom detections on the commercial FiveM checkers.</p>' + editor, headerRight) +
                '</div>' +
                '<div class="col-span-7">' + card('Your detection strings',
                    '<p style="font-size:12.5px;color:#a29cb8;margin:0 0 8px 0;">Matched case-insensitively. File names, folder names and in-memory keywords all work.</p>' +
                    (rows || '<div class="ob-empty">Nothing configured.</div>')) + '</div>' +
                '<div class="col-span-5">' + card('Collected from scans',
                    '<p style="font-size:12.5px;color:#a29cb8;margin:0 0 8px 0;">Strings your scans have already surfaced on real machines.</p>' +
                    (collectedRows || '<div class="ob-empty">No scan strings recorded yet.</div>')) + '</div>' +
                '</div>';
        }

        /* ---- Editable settings helpers (persist to /api/config → desktop scanner) ---- */
        function ocBool(key) { var v = cfg[key]; return v === true || v === 'true' || v === 1 || v === 'on'; }
        function ocSwitchCss(b) {
            var style = 'background:' + (b ? 'linear-gradient(135deg,#a855f7,#7c3aed)' : '#1a1325') + ';';
            return style;
        }
        function ocToggle(key, label, desc, iconSvg) {
            var on = ocBool(key);
            return '<div class="oc-set"><div class="oc-fl-out">' +
                '<div style="display:flex;align-items:center;gap:8px;">' + (iconSvg || '') + '<span style="font-size:13px;color:#fff;font-weight:600;">' + label + '</span></div>' +
                '<div style="font-size:12px;color:#a29cb8;margin-top:3px;line-height:1.45;">' + (desc || '') + '</div></div>' +
                '<button type="button" onclick="window.__OC_TOGGLE(event)" data-key="' + key + '" class="oc-switch' + (on ? ' on' : '') + '" ' + (on ? 'style="background:linear-gradient(135deg,#a855f7,#7c3aed);"' : 'style="background:#1a1325;"') + '></button></div>';
        }
        function ocSlider(key, label, desc, iconSvg) {
            var min = 1, max = 10;
            var v = Math.min(max, Math.max(min, Number(cfg[key]) || 5));
            return '<div class="oc-set"><div class="oc-fl-out">' +
                '<div style="display:flex;align-items:center;gap:8px;">' + (iconSvg || '') + '<span style="font-size:13px;color:#fff;font-weight:600;">' + label + '</span>' +
                '<span class="oc-set-val" style="font-size:12px;color:#c084fc;font-weight:700;font-family:monospace;">/' + max + '</span></div>' +
                '<div style="font-size:12px;color:#a29cb8;margin-top:3px;line-height:1.45;">' + (desc || '') + '</div></div>' +
                '<span class="oc-set-val" style="font-size:15px;color:#c084fc;font-weight:800;min-width:22px;text-align:center;">' + v + '</span>' +
                '<input type="range" data-key="' + key + '" min="' + min + '" max="' + max + '" value="' + v + '" oninput="window.__OC_CFG_SET(this)" style="width:120px;accent-color:#a855f7;cursor:pointer;flex-shrink:0;"></div>';
        }
        function ocSaveCfg() {
            var pill = document.getElementById('oc-cfg-status');
            if (pill) pill.innerHTML = '<span style="color:#d8b46a;">saving...</span>';
            fetch('/api/config', { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify({ config: cfg }) })
                .then(function(r) { return r.json(); })
                .then(function(j) {
                    if (j && j.ok) {
                        cfg = j.config || cfg; window.__OC_CFG = cfg;
                        if (pill) pill.innerHTML = '<span style="color:#7dc9a0;">saved · synced to scanner</span>';
                        else ocNotify('Config saved', 'success');
                    } else if (pill) { pill.innerHTML = '<span style="color:#e0848f;">save failed</span>'; }
                    else { ocNotify('Could not save config', 'error'); }
                })
                .catch(function() { if (pill) pill.innerHTML = '<span style="color:#e0848f;">save failed</span>'; else ocNotify('Could not save config', 'error'); });
        }
        function ocGuiTheme() {
            var t = (cfg.uiTheme || 'neon'); var a = /^#[0-9a-fA-F]{6}$/.test(cfg.accentColor || '') ? cfg.accentColor : '#a855f7';
            return { t: t, a: a };
        }
        function ocGuiRender() {
            var pre = document.getElementById('oc-gui-preview');
            if (!pre) return;
            var g = ocGuiTheme();
            var bg = g.t === 'classic' ? '#0c0c12' : g.t === 'minimal' ? '#000' : '#020208';
            var bd = g.t === 'minimal' ? '1px solid #1d1d26' : '1px solid ' + g.a + '44';
            var font = g.t === 'minimal' ? '"Segoe UI",sans-serif' : 'inherit';
            var wm = ocBool('watermark');
            var sl = cfg.overlayScanLines === false ? false : true;
            pre.innerHTML =
                '<div style="background:' + bg + ';border:' + bd + ';border-radius:16px;padding:16px;font-family:' + font + ';position:relative;overflow:hidden;box-shadow:0 0 26px ' + g.a + '22;">' +
                (sl ? '<div style="position:absolute;inset:0;pointer-events:none;background:repeating-linear-gradient(0deg,' + g.a + '0d 0px,' + g.a + '0d 1px,transparent 1px,transparent 5px);"></div>' : '') +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;"><span style="font-size:12px;color:' + g.a + ';font-weight:800;letter-spacing:.14em;">OCEAN SCAN</span><span style="font-size:10px;color:#7d7794;">LIVE</span></div>' +
                '<div style="display:flex;align-items:center;gap:12px;">' +
                '<div style="width:34px;height:34px;border-radius:50%;background:linear-gradient(135deg,' + g.a + ',' + g.a + 'cc);box-shadow:0 0 14px ' + g.a + '66;"></div>' +
                '<div><div style="font-size:13px;color:#fff;font-weight:700;">Player #001</div><div style="font-size:11px;color:#a29cb8;">v4.2.0 · FiveM</div></div></div>' +
                '<div style="height:8px;border-radius:4px;background:' + g.a + '22;margin-top:14px;overflow:hidden;"><div style="width:64%;height:100%;border-radius:4px;background:linear-gradient(90deg,' + g.a + ',transparent);animation:ocPreFill 1.6s ease-in-out infinite alternate;"></div></div>' +
                '<div style="margin-top:12px;display:flex;gap:6px;">' +
                '<span style="font-size:9px;padding:3px 8px;border-radius:9999px;background:' + g.a + '1f;color:' + g.a + ';border:1px solid ' + g.a + '44;">CLEAN 87%</span>' +
                '<span style="font-size:9px;padding:3px 8px;border-radius:9999px;background:#ffffff0d;color:#a29cb8;border:1px solid #ffffff1a;">STRINGS 12</span></div>' +
                (wm ? '<div style="position:absolute;bottom:10px;right:14px;font-size:10px;color:' + g.a + 'aa;letter-spacing:.14em;">OCEAN · your.guild</div>' : '') +
                '</div>';
        }
        window.__OC_TOGGLE = function(ev) {
            var b = ev.currentTarget; var k = b.getAttribute('data-key');
            var nv = !ocBool(k); cfg[k] = nv;
            if (nv) { b.classList.add('on'); b.style.background = 'linear-gradient(135deg,#a855f7,#7c3aed)'; }
            else { b.classList.remove('on'); b.style.background = '#1a1325'; }
            ocSaveCfg();
        };
        window.__OC_CFG_SET = function(el) {
            var k = el.getAttribute('data-key');
            cfg[k] = parseInt(el.value, 10) || 5;
            var sibs = el.parentNode ? el.parentNode.querySelectorAll('.oc-set-val') : [];
            if (sibs.length) sibs[sibs.length - 1].textContent = cfg[k];
            ocSaveCfg();
        };
        window.__OC_ACCENT = function(el) {
            cfg.accentColor = el.value; cfg.uiTheme = ocGuiTheme().t || 'neon';
            el.style.border = '1px solid ' + el.value;
            ocSaveCfg(); ocGuiRender(); window.__OC_UI_RECLASS();
        };
        window.__OC_THEME = function(el) {
            cfg.uiTheme = el.getAttribute('data-val') || 'neon';
            ocSaveCfg(); ocGuiRender(); window.__OC_UI_RECLASS();
        };
        window.__OC_UI_RECLASS = function() {
            document.querySelectorAll('.oc-th-opt').forEach(function(b) {
                if ((b.getAttribute('data-val') || '') === (cfg.uiTheme || 'neon')) b.classList.add('on');
                else b.classList.remove('on');
            });
        };
        window.__OC_SCAN = function(el) { cfg.overlayScanLines = el.checked; ocSaveCfg(); ocGuiRender(); };
        window.__OC_WM = function(el) { cfg.watermark = el.checked; ocSaveCfg(); ocGuiRender(); };
        window.__OC_SYNC = function() { ocSaveCfg(); };

        /* ---- Custom strings handlers (used by the Custom Strings page) ---- */
        function ocRerenderStrings() {
            if (route !== 'strings') return;
            var host = document.getElementById('oc-dash-content');
            if (!host) return;
            host.innerHTML = renderPageContent(route, user, stats, scans, pins);
        }
        window.__OC_STR_ADD = function() {
            var termEl = document.getElementById('ob-str-term');
            var descEl = document.getElementById('ob-str-desc');
            var catEl = document.getElementById('ob-str-cat');
            var term = ((termEl && termEl.value) || '').trim();
            if (!term) { ocNotify('Enter a string to detect', 'warn'); return; }
            var list = ocStringList();
            if (list.some(function(s) { return s.term.toLowerCase() === term.toLowerCase(); })) {
                ocNotify('That string is already in the list', 'warn');
                return;
            }
            list.push({ term: term, desc: ((descEl && descEl.value) || '').trim(), category: (catEl && catEl.value) || 'cheat' });
            cfg.customStrings = list;
            if (termEl) termEl.value = '';
            if (descEl) descEl.value = '';
            ocSaveCfg();
            ocRerenderStrings();
            ocNotify('Added ' + term + ' - synced to the scanner', 'success');
        };
        window.__OC_STR_DEL = function(term) {
            var want = String(term || '').toLowerCase();
            cfg.customStrings = ocStringList().filter(function(s) { return s.term.toLowerCase() !== want; });
            ocSaveCfg();
            ocRerenderStrings();
            ocNotify('Removed ' + term, 'success');
        };
        window.__OC_STR_SEED = function() {
            var n = ocStringSeed();
            ocRerenderStrings();
            ocNotify(n ? n + ' default strings added' : 'Defaults already present', n ? 'success' : 'warn');
        };
        window.__OC_CATALOG = OB_CATALOG;
        window.__OC_MODULES = OB_MODULES;

        /* ---- Configs / Enterprise page ---- */
        function renderConfigsPage(user, stats, pins, scans) {
            var sh = '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>';
            var sliders = '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M4 6h16M4 12h16M4 18h12M7 3v6m5 3v6m5-3v6"/></svg>';
            var paint = '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M4 3h16a1 1 0 011 1v10a1 1 0 01-1 1H9l-4 5v-5H4a1 1 0 01-1-1V4a1 1 0 011-1z"/></svg>';
            var robot = '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 7h10a2 2 0 012 2v6a2 2 0 01-2 2H7a2 2 0 01-2-2V9a2 2 0 012-2z"/></svg>';
            var conn = '<svg class="oc-ic" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.8" d="M13 10V3L4 14h7v7l9-11h-7z"/></svg>';
            var a = /^#[0-9a-fA-F]{6}$/.test(cfg.accentColor || '') ? cfg.accentColor : '#a855f7';
            var modules = OB_MODULES.map(function(m) { return ocToggle(m.key, m.label, m.desc, sh); }).join('');
            var rules = ocToggle('detect', 'Deep Detection', 'Core engine scans processes, modules and FiveM combat the main categories.', sh)
                + ocToggle('strictMode', 'Strict Mode', 'Aggressive ruleset — flags any suspicious module or string, even borderline ones.', sh)
                + ocToggle('warnDuringScan', 'Live warnings mid-scan', 'Surface real-time warnings while a scan is still running.', conn)
                + ocToggle('discordCheck', 'Discord account check', 'Collect linked Discord accounts from the scanned PC for identity matching.', conn)
                + ocToggle('autoUpgradeStrings', 'Auto-upgrade string DB', 'Push newly discovered strings into the shared detection database.', robot);
            var engine = ocSlider('scanBits', 'Scan depth', 'How deep the scanner digs: 1 = quick pass · 10 = maximum detail.', sliders)
                + ocToggle('screenshareAutoStart', 'Screenshare auto-start', 'Automatically launch the screenshare session when a scan starts.', conn)
                + ocToggle('captureRecordings', 'Capture recordings', 'Record the screen during scans for evidence review.', paint);
            var app = ocToggle('notifications', 'Notifications', 'Dashboard + desktop alerts when scans finish.', sliders)
                + ocToggle('overlayScanLines', 'Scanline overlay', 'Animated scan texture over results and the scanner UI.', paint)
                + ocToggle('watermark', 'Watermark', 'Stamp your community watermark on scan results.', paint);
            var headerRight = '<div style="display:flex;align-items:center;gap:10px;">' +
                '<span id="oc-cfg-status" style="font-size:12px;color:#7d7794;">' + (cfg.__savedAt ? 'last saved ' + fmtDate(cfg.__savedAt) : 'not saved to cloud yet') + '</span>' +
                '<button onclick="window.__OC_SYNC()" style="padding:9px 16px;border-radius:10px;background:linear-gradient(135deg,#a855f7,#7c3aed);color:#fff;border:none;font-weight:700;font-size:12px;cursor:pointer;transition:all .2s ease;">Save &amp; Sync Scanner</button>' +
                '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a></div>';
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Configs / Enterprise', 'Tune exactly how Ocean scans — every setting is streamed to the desktop scanner on the next scan.', headerRight) + '</div>' +
                '<div class="col-span-7">' +
                    '<div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);box-shadow:0 0 0 1px rgba(138,92,246,.06);">' +
                        '<div style="padding:16px 0 4px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;display:flex;align-items:center;gap:8px;">' + sh + 'DETECTION RULES</span><div style="font-size:12px;color:#7d7794;margin-top:2px;">What the scanner looks for.</div></div>' +
                        rules +
                    '</div>' +
                    '<div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);margin-top:14px;">' +
                        '<div style="padding:16px 0 4px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;display:flex;align-items:center;gap:8px;">' + sliders + 'SCANNER ENGINE</span><div style="font-size:12px;color:#7d7794;margin-top:2px;">Depth and behaviour of the scan itself.</div></div>' +
                        engine +
                    '</div>' +
                    '<div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);margin-top:14px;" id="oc-modules-card">' +
                        '<div style="padding:16px 0 4px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;display:flex;align-items:center;gap:8px;">' + robot + 'DETECTION MODULES</span>' +
                        '<div style="font-size:12px;color:#7d7794;margin-top:2px;">Each switch is streamed to the scanner on the next pin. What is off is not read at all.</div></div>' +
                        '<div style="font-size:11.5px;color:#7d7794;padding:0 0 6px 0;">' + OB_MODULES.filter(function(m) { return ocBool(m.key); }).length + ' of ' + OB_MODULES.length + ' modules enabled</div>' +
                        modules +
                    '</div>' +
                '</div>' +
                '<div class="col-span-5">' +
                    '<div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);">' +
                        '<div style="padding:16px 0 4px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;display:flex;align-items:center;gap:8px;">' + paint + 'APPEARANCE</span><div style="font-size:12px;color:#7d7794;margin-top:2px;">How scan results look.</div></div>' +
                        app +
                        '<div class="oc-set"><div class="oc-fl-out"><span style="font-size:13px;color:#fff;font-weight:600;">Accent color</span><div style="font-size:12px;color:#a29cb8;margin-top:3px;">Brand hue for overlays + scanner UI.</div></div>' +
                            '<input type="color" value="' + a + '" onchange="window.__OC_ACCENT(this)" style="border:1px solid ' + a + ';width:34px;height:34px;border-radius:9px;background:#000;cursor:pointer;padding:0;flex-shrink:0;"></div>' +
                    '</div>' +
                    '<div style="padding:16px 18px;border-radius:16px;background:linear-gradient(160deg,' + a + '14,#020208 55%);border:1px solid ' + a + '33;margin-top:14px;position:relative;overflow:hidden;">' +
                        '<div style="font-size:13px;color:#fff;font-weight:700;display:flex;align-items:center;gap:8px;">' + conn + 'Scanner sync</div>' +
                        '<div style="font-size:12px;color:#a29cb8;margin-top:4px;line-height:1.55;">When a pin is scanned, the desktop app pulls <b style="color:#e9d5ff;">' + Object.keys(cfg).filter(function(k) { return cfg[k] === true || cfg[k] === false; }).length + ' settings</b> from the cloud, applies them and reports them back into this scan report.</div>' +
                        '<div style="margin-top:10px;font-size:11px;color:' + a + ';cursor:pointer;" onclick="window.__OC_SYNC()">● ' + (cfg.__savedAt ? 'last synced ' + fmtDate(cfg.__savedAt) : 'save to activate') + '</div>' +
                    '</div>' +
                '</div></div>';
        }

        /* ---- Custom Gui page ---- */
        function renderCustomGuiPage(user, stats, pins, scans) {
            var a = /^#[0-9a-fA-F]{6}$/.test(cfg.accentColor || '') ? cfg.accentColor : '#a855f7';
            var thBtn = function(x, label) {
                return '<button data-val="' + x + '" class="oc-th-opt' + ((cfg.uiTheme || 'neon') === x ? ' on' : '') + '" onclick="window.__OC_THEME(this)" style="background:' + ((cfg.uiTheme || 'neon') === x ? 'linear-gradient(135deg,#a855f7,#7c3aed);color:#fff;border-color:transparent;' : '') + ';">' + label + '</button>';
            };
            var headerRight = '<div style="display:flex;align-items:center;gap:12px;">' +
                '<span id="oc-cfg-status" style="font-size:12px;color:#7d7794;">' + (cfg.__savedAt ? 'last saved ' + fmtDate(cfg.__savedAt) : 'not saved to cloud yet') + '</span>' +
                '<button onclick="window.__OC_SYNC()" style="padding:9px 16px;border-radius:10px;background:linear-gradient(135deg,#a855f7,#7c3aed);color:#fff;border:none;font-weight:700;font-size:12px;cursor:pointer;">Save &amp; Sync</button>' +
                '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a></div>';
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Custom GUI', 'Design your own scan interface. Every change is saved to the cloud and pulled by the desktop scanner.', headerRight) + '</div>' +
                '<div class="col-span-5">' +
                    '<div style="padding:4px 18px 8px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);">' +
                        '<div style="padding:16px 0 12px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;">THEME PRESET</span></div>' +
                        '<div style="display:flex;gap:8px;">' + thBtn('neon', 'Neon') + thBtn('classic', 'Classic') + thBtn('minimal', 'Minimal') + '</div>' +
                        '<div class="oc-set"><div class="oc-fl-out"><span style="font-size:13px;color:#fff;font-weight:600;">Accent color</span><div style="font-size:12px;color:#a29cb8;margin-top:3px;">Used for borders, bars and glow.</div></div>' +
                            '<input type="color" value="' + a + '" onchange="window.__OC_ACCENT(this)" style="border:1px solid rgba(168,85,247,.5);width:34px;height:34px;border-radius:9px;background:#000;cursor:pointer;padding:0;flex-shrink:0;"></div>' +
                        '<label class="oc-set" style="display:flex;align-items:center;gap:10px;cursor:pointer;"><div class="oc-fl-out"><span style="font-size:13px;color:#fff;font-weight:600;">Scanline overlay</span><div style="font-size:12px;color:#a29cb8;margin-top:3px;">Animated scan texture.</div></div><input type="checkbox" ' + (cfg.overlayScanLines === false ? '' : 'checked') + ' onchange="window.__OC_SCAN(this)" style="accent-color:#a855f7;width:17px;height:17px;"></label>' +
                        '<label class="oc-set" style="display:flex;align-items:center;gap:10px;cursor:pointer;"><div class="oc-fl-out"><span style="font-size:13px;color:#fff;font-weight:600;">Watermark</span><div style="font-size:12px;color:#a29cb8;margin-top:3px;">Community watermark on results.</div></div><input type="checkbox" ' + (cfg.watermark === true ? 'checked' : '') + ' onchange="window.__OC_WM(this)" style="accent-color:#a855f7;width:17px;height:17px;"></label>' +
                    '</div>' +
                '</div>' +
                '<div class="col-span-7">' +
                    '<div style="padding:16px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);">' +
                        '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;">LIVE PREVIEW</span><span style="font-size:11px;color:#7d7794;">desktop scanner</span></div>' +
                        '<div id="oc-gui-preview"></div>' +
                    '</div>' +
                '</div></div>';
        }

        /* ---- Publics Gui page ---- */
        function renderPublicsGuiPage(user, stats, pins, scans) {
            var pub = [
                { name: 'Ocean Classic', desc: 'The default public interface', uses: stats.completedPins || 0 },
                { name: 'Ocean Minimal', desc: 'Clean, low-noise layout', uses: stats.totalScans || 0 },
                { name: 'Ocean Neon', desc: 'High-contrast purple theme', uses: stats.clean || 0 }
            ];
            var rows = pub.map(function(p) {
                return '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;display:flex;justify-content:space-between;align-items:center;">' +
                    '<div><div style="font-weight:600;color:#fff;font-size:14px;">' + p.name + '</div><div style="font-size:12px;color:#a29cb8;">' + p.desc + '</div></div>' +
                    '<div style="font-size:12px;color:#a855f7;font-weight:600;">' + p.uses + ' uses</div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Publics GUI', 'Shared public interfaces everyone can use.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12" style="display:flex;flex-direction:column;gap:10px;">' + rows + '</div></div>';
        }

        /* ---- Detections page with real scan logs ---- */
        function renderDetectionsPage(user, stats, scans) {
            var RE = { red: '#e0848f' };
            var logs = [];
            (scans || []).forEach(function(s) {
                if (!s.timestamp) return;
                var status = (s.status || s.result || 'unknown') + '';
                var detections = 0;
                if (s.detections && typeof s.detections === 'object') Object.keys(s.detections).forEach(function(cat) { var a = s.detections[cat]; if (Array.isArray(a)) detections += a.length; });
                logs.push({
                    ts: s.timestamp,
                    player: s.username || s.playerName || '--',
                    pc: s.pcName || '--',
                    game: s.game || 'FiveM',
                    status: status,
                    detections: detections,
                    hwid: s.hwid || '--'
                });
            });
            logs.sort(function(a, b) { return a.ts < b.ts ? 1 : -1; });
            var rows = logs.slice(0, 50).map(function(l) {
                return '<tr style="border-bottom:1px solid #1b122b;font-family:monospace;font-size:12px;">' +
                    '<td style="padding:10px 12px;color:#7d7794;">' + fmtDate(l.ts) + '</td>' +
                    '<td style="padding:10px 12px;color:#e9d5ff;">' + l.player + '</td>' +
                    '<td style="padding:10px 12px;color:#a29cb8;">' + l.pc + '</td>' +
                    '<td style="padding:10px 12px;color:#a29cb8;">' + l.game + '</td>' +
                    '<td style="padding:10px 12px;">' + statusBadge(l.status) + '</td>' +
                    '<td style="padding:10px 12px;color:' + (l.detections ? RE.red : '#808098') + ';">' + l.detections + '</td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12"><div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;">' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#e0848f;">' + (stats.detected || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Total Detections</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#a855f7;">' + (stats.uniqueCheats || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Unique Cheats</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#d8b46a;">' + (stats.suspicious || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Suspicious</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#7dc9a0;">' + (stats.clean || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Clean Scans</div></div>' +
                '</div></div>' +
                '<div class="col-span-12">' + card('Scan Logs', '<table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Date</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Player</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">PC</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Game</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Status</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Dets</th>' +
                    '</tr></thead><tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="6">No scans yet.</td></tr>') + '</tbody></table>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12">' + obCatalogCard() + '</div>' +
                '</div>';
        }

        /* ---- Profiles grid + view ---- */
        function profilesFrom(scans) {
            var buckets = {};
            (scans || []).forEach(function(s) {
                var hwid = (s.hwid && ('' + s.hwid).trim()) || (s.pin && ('' + s.pin).trim().toUpperCase()) || ((((s.username || '') + '|' + (s.pcName || ''))));
                if (!buckets[hwid]) buckets[hwid] = { id: hwid, username: '', pcName: '', hwid: s.hwid, pin: s.pin, scans: [], games: {}, dets: {} };
                var b = buckets[hwid];
                b.scans.push(s);
                if (!b.username && (s.username || s.playerName)) b.username = s.username || s.playerName;
                if (!b.pcName && s.pcName) b.pcName = s.pcName;
                if (s.game) b.games[s.game] = (b.games[s.game] || 0) + 1;
                if (s.detections && typeof s.detections === 'object') Object.keys(s.detections).forEach(function(cat) { var a = s.detections[cat]; if (Array.isArray(a)) a.forEach(function(it) { var nm = it && (it.name || it.Name); if (nm) b.dets[nm] = (b.dets[nm] || 0) + 1; }); });
            });
            return Object.keys(buckets).map(function(id) { var b = buckets[id]; return {
                id: id,
                username: b.username || b.pcName || b.pin || 'Unknown',
                pcName: b.pcName || '--',
                scanCount: b.scans.length,
                games: Object.keys(b.games),
                dets: Object.keys(b.dets),
                hwid: b.hwid || '--',
                pin: b.pin || '--'
            }; }).sort(function(a, z) { return z.scanCount - a.scanCount; });
        }

        function renderProfilesPage(user, stats, scans, pins) {
            var profiles = profilesFrom(scans);
            var cards = profiles.map(function(p) {
                var initials = (p.username.replace(/[^A-Za-z0-9]/g, ' ').split(/\s+/).filter(Boolean).slice(0, 2).map(function(w) { return w[0].toUpperCase(); }).join('') || '?');
                return '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;cursor:pointer;" data-oc-profile="' + encodeURIComponent(p.id) + '">' +
                    '<div style="display:flex;align-items:center;gap:12px;margin-bottom:10px;">' +
                        '<div style="width:40px;height:40px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:700;color:#fff;font-size:14px;">' + initials + '</div>' +
                        '<div><div style="font-weight:600;color:#fff;font-size:14px;">' + p.username + '</div><div style="font-size:11px;color:#808098;">' + (p.pcName) + '</div></div>' +
                    '</div>' +
                    '<div style="display:flex;justify-content:space-between;font-size:12px;color:#a29cb8;">' +
                        '<span>' + p.scanCount + ' scans</span><span style="color:#a855f7;">' + (p.dets.length ? p.dets.length + ' detections' : 'clean') + '</span>' +
                    '</div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Player Profiles', '<p style="font-size:13px;color:#a29cb8;margin:0 0 12px 0;">Every scan is remembered per player. Click a profile to view full details.</p>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12"><div style="display:grid;grid-template-columns:repeat(3,1fr);gap:14px;" id="oc-profiles-grid">' + (cards || '<div style="color:#808098;font-size:13px;">No profiles yet.</div>') + '</div></div>' +
                '<div id="oc-profile-detail"></div>' +
                '</div>';
        }

        /* ---- Watchlist ---- */
        function renderWatchlistPage(user, stats, scans, pins) {
            var profiles = profilesFrom(scans).filter(function(p) { return p.dets.length > 0; });
            var rows = profiles.map(function(p) {
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:12px 14px;color:#e9d5ff;font-size:13px;">' + p.username + '</td>' +
                    '<td style="padding:12px 14px;color:#a29cb8;font-size:13px;">' + p.pcName + '</td>' +
                    '<td style="padding:12px 14px;color:#a29cb8;font-size:13px;">' + p.scanCount + '</td>' +
                    '<td style="padding:12px 14px;color:#e0848f;font-size:13px;">' + p.dets.join(', ').slice(0, 40) + '</td>' +
                    '<td style="padding:12px 14px;text-align:right;"><button data-oc-profile="' + encodeURIComponent(p.id) + '" style="padding:6px 12px;border-radius:8px;background:rgba(168,85,247,.12);border:1px solid rgba(168,85,247,.25);color:#c084fc;font-size:12px;cursor:pointer;">Watch</button></td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Watchlist', 'Players with detections are listed here so you can keep an eye on them.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12"><table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Player</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">PC</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Scans</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Detected</th>' +
                '<th style="padding:10px 14px;text-align:right;font-size:11px;color:#808098;text-transform:uppercase;"></th></tr></thead>' +
                '<tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="5">No flagged players.</td></tr>') + '</tbody></table></div></div>';
        }

        /* ---- Leaderboard ---- */
        function renderLeaderboardPage(user, stats, scans, pins) {
            var profiles = profilesFrom(scans).sort(function(a, z) { return z.scanCount - a.scanCount; });
            var rows = profiles.slice(0, 10).map(function(p, i) {
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:12px 14px;color:#a855f7;font-size:13px;font-weight:700;">#' + (i + 1) + '</td>' +
                    '<td style="padding:12px 14px;color:#e9d5ff;font-size:13px;">' + p.username + '</td>' +
                    '<td style="padding:12px 14px;color:#a29cb8;font-size:13px;">' + p.pcName + '</td>' +
                    '<td style="padding:12px 14px;color:#a29cb8;font-size:13px;">' + p.scanCount + ' scans</td>' +
                    '<td style="padding:12px 14px;color:#e0848f;font-size:13px;">' + p.dets.length + '</td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Leaderboard', 'Most scanned players in your server.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12"><table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;">#</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;">Player</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;">PC</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;">Scans</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;">Dets</th></tr></thead>' +
                '<tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="5">No data yet.</td></tr>') + '</tbody></table></div></div>';
        }

        /* ---- Tickets ---- */
        function renderTicketsPage(user, stats, scans, pins) {
            var rows = [
                { id: 'TKT-1141', subject: 'Help with my new server', status: 'open', date: '2d ago', msgs: 3 },
                { id: 'TKT-1132', subject: 'Detection false positive', status: 'open', date: '4d ago', msgs: 6 },
                { id: 'TKT-1120', subject: 'Billing question', status: 'closed', date: '1w ago', msgs: 2 }
            ];
            var list = rows.map(function(t) {
                return '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;display:flex;justify-content:space-between;align-items:center;margin-bottom:8px;">' +
                    '<div><div style="font-size:13px;color:#e8e4f6;font-weight:600;">' + t.subject + '</div><div style="font-size:11px;color:#7d7794;">' + t.id + ' · ' + t.date + ' · ' + t.msgs + ' messages</div></div>' +
                    statusBadge(t.status) + '</div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Support Tickets', 'Your conversations with Ocean support.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12" style="display:flex;flex-direction:column;">' + list + '</div></div>';
        }

        /* ---- Chat ---- */
        function renderChatPage(user, stats, scans, pins) {
            var msgs = [
                { user: 'OceanBot', text: 'Welcome to the Ocean community chat!', time: 'now', me: false },
                { user: user.username || 'You', text: 'Hi there!', time: '1m', me: true },
                { user: 'fabix', text: 'Does anyone have a good FiveM config?', time: '3m', me: false }
            ];
            var list = msgs.map(function(m) {
                return '<div style="display:flex;gap:10px;margin-bottom:12px;' + (m.me ? 'flex-direction:row-reverse;' : '') + '">' +
                    '<div style="width:34px;height:34px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:700;color:#fff;font-size:12px;">' + m.user.charAt(0).toUpperCase() + '</div>' +
                    '<div style="max-width:70%;padding:10px 14px;border-radius:14px;background:' + (m.me ? 'rgba(168,85,247,.18)' : '#120c22') + ';border:1px solid #1f1436;">' +
                        '<div style="font-size:12px;color:#a855f7;font-weight:600;">' + m.user + ' · <span style="color:#7d7794;font-weight:400;">' + m.time + '</span></div>' +
                        '<div style="font-size:13px;color:#e8e4f6;margin-top:4px;">' + m.text + '</div></div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Community Chat', '<div style="background:#07050d;border-radius:12px;padding:16px;min-height:300px;">' + list + '</div><div style="display:flex;gap:8px;margin-top:10px;"><input id="oc-chat-input" placeholder="Type a message..." style="flex:1;padding:12px;border-radius:10px;background:#120c22;border:1px solid #1f1436;color:#e8e4f6;font-size:13px;outline:none;" /><button data-oc-chat-send style="padding:12px 20px;border-radius:10px;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;border:none;font-weight:700;font-size:13px;cursor:pointer;">Send</button></div>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div></div>';
        }

        /* ---- Settings ---- */
        function renderSettingsPage(user, stats, scans, pins) {
            var fields = [
                { k: 'Username', v: user.username || '--' },
                { k: 'Member since', v: fmtDate(user.createdAt) },
                { k: 'Total scans', v: stats.totalScans || 0 },
                { k: 'Total pins', v: stats.totalPins || 0 },
                { k: 'Preferred game', v: 'FiveM' },
                { k: 'Cloud config', v: (cfg.__savedAt ? 'active · synced ' + timeAgo(cfg.__savedAt) : 'not configured yet') }
            ];
            var rows = fields.map(function(f) {
                return '<div class="oc-row" style="display:flex;justify-content:space-between;align-items:center;padding:13px 0;border-bottom:1px solid rgba(138,105,235,.1);"><span style="font-size:13px;color:#a29cb8;">' + f.k + '</span><span style="font-size:13px;color:#fff;font-weight:600;">' + f.v + '</span></div>';
            }).join('');
            var headerRight = '<div style="display:flex;align-items:center;gap:10px;"><span id="oc-cfg-status" style="font-size:12px;color:#7d7794;">' + (cfg.__savedAt ? 'config synced' : 'sync started on the Configs page') + '</span><a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a></div>';
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Settings', 'Your account and preferences. Everything here is synced with the desktop scanner.', headerRight) + '</div>' +
                '<div class="col-span-7"><div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);">' +
                    '<div style="padding:14px 0 2px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;">ACCOUNT</span></div>' + rows +
                '</div></div>' +
                '<div class="col-span-5"><div style="padding:4px 18px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);">' +
                    '<div style="padding:14px 0 2px 0;"><span style="font-size:13px;color:#c084fc;font-weight:700;letter-spacing:.08em;">PREFERENCES</span><div style="font-size:12px;color:#7d7794;margin-top:2px;">Applies to your scans.</div></div>' +
                    ocToggle('notifications', 'Notifications', 'Alerts when scans complete.') +
                    ocToggle('autoUpgradeStrings', 'Auto-upgrade strings', 'Share new strings into the detection DB.') +
                    ocToggle('discordCheck', 'Discord check', 'Collect Discord accounts during scans.') +
                '</div></div></div>';
        }

        /* ---- API Keys ---- */
        function renderApiKeysPage(user, stats, pins, scans) {
            var codes = (pins || []).map(function(k) { return k.code || k.key || k.pin; }).filter(Boolean);
            var rows = codes.map(function(c) {
                return '<tr style="border-bottom:1px solid #1b122b;"><td style="padding:12px 14px;font-family:monospace;font-size:13px;color:#e9d5ff;">' + c + '</td>' +
                    '<td style="padding:12px 14px;font-size:13px;color:#a29cb8;">pin</td>' +
                    '<td style="padding:12px 14px;font-size:13px;">' + statusBadge('active') + '</td>' +
                    '<td style="padding:12px 14px;text-align:right;"><button style="padding:6px 12px;border-radius:8px;background:rgba(168,85,247,.12);border:1px solid rgba(168,85,247,.25);color:#c084fc;font-size:12px;cursor:pointer;" onclick="navigator.clipboard&&navigator.clipboard.writeText(\'' + c + '\')">Copy</button></td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('API Keys', 'Your active scan pins act as API keys for the server scanner.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12"><table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Key</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Type</th>' +
                '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Status</th>' +
                '<th style="padding:10px 14px;text-align:right;font-size:11px;color:#808098;text-transform:uppercase;"></th></tr></thead>' +
                '<tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="4">No keys yet.</td></tr>') + '</tbody></table></div></div>';
        }

        function renderPageContent(route, user, stats, scans, pins) {
            if (route === 'pins') return renderPinsPage(user, stats, pins, scans);
            if (route === 'strings') return renderStringsPage(user, stats, pins, scans);
            if (route === 'configs') return renderConfigsPage(user, stats, pins, scans);
            if (route === 'custom-gui') return renderCustomGuiPage(user, stats, pins, scans);
            if (route === 'publics-gui') return renderPublicsGuiPage(user, stats, pins, scans);
            if (route === 'detections') return renderDetectionsPage(user, stats, scans);
            if (route === 'watchlist') return renderWatchlistPage(user, stats, scans, pins);
            if (route === 'profiles') return renderProfilesPage(user, stats, scans, pins);
            if (route === 'leaderboard') return renderLeaderboardPage(user, stats, scans, pins);
            if (route === 'tickets') return renderTicketsPage(user, stats, scans, pins);
            if (route === 'chat') return renderChatPage(user, stats, scans, pins);
            if (route === 'settings') return renderSettingsPage(user, stats, scans, pins);
            if (route === 'api-keys') return renderApiKeysPage(user, stats, pins, scans);
            return renderHomeContent(user, stats, scans, pins);
        }

        var html = '<style>' +
        '/* ===== DASHBOARD EFFECTS ===== */' +
        'body{background:#000 !important;overflow:hidden !important;}' +
        'body::before{content:"";position:fixed;inset:0;z-index:0;pointer-events:none;background-color:#000;' +
            'background-image:linear-gradient(rgba(168,85,247,.05) 1px,transparent 1px),linear-gradient(90deg,rgba(168,85,247,.05) 1px,transparent 1px);' +
            'background-size:48px 48px;}' +
        '#oc-dash-glow{position:fixed;width:520px;height:520px;border-radius:50%;pointer-events:none;z-index:2147482000;' +
            'background:radial-gradient(circle,rgba(168,85,247,.16) 0%,rgba(147,51,234,.07) 38%,transparent 68%);' +
            'filter:blur(34px);transform:translate(-50%,-50%);transition:left .08s ease-out,top .08s ease-out;opacity:0.85;}' +
        'body:hover #oc-dash-glow{opacity:1;}' +
        /* pure black surfaces: every dashboard card collapses to near-black with a cool purple edge + screen glow */
        '#oc-dash-content [style*="background:#0a0714"],#oc-dash-content [style*="background:#0e0a1a"],' +
        '#oc-dash-content [style*="background:#0f0a1c"],#oc-dash-content [style*="background:#120c22"],' +
        '#oc-dash-content [style*="background:#0e0918"],#oc-dash-content [style*="background:#0b0614"],' +
        '#oc-dash-content [style*="background:#0a080f"]' +
        '{background:#020208 !important;border-color:rgba(138,105,235,.26) !important;}' +
        '#oc-dash-content [style*="background:#0a0714"]:hover,#oc-dash-content [style*="background:#0e0a1a"]:hover,' +
        '#oc-dash-content [style*="background:#0f0a1c"]:hover,#oc-dash-content [style*="background:#0e0918"]:hover' +
        '{border-color:rgba(168,85,247,.4) !important;transform:translateY(-2px);' +
            'box-shadow:0 10px 30px rgba(0,0,0,.6),0 0 22px rgba(147,51,234,.14);}' +
        /* nav items */
        '.oc-nav-item{display:flex;align-items:center;gap:10px;padding:10px 12px;border-radius:10px;font-size:13px;font-weight:500;color:#a29cb8;cursor:pointer;transition:all .2s ease;position:relative;overflow:hidden;text-decoration:none;}' +
        '.oc-nav-item::before{content:"";position:absolute;left:0;top:0;width:3px;height:100%;background:#a855f7;border-radius:0 2px 2px 0;transform:scaleY(0);transition:transform .2s ease;}' +
        '.oc-nav-item:hover{color:#fff;background:rgba(168,85,247,.08);transform:translateX(4px);}' +
        '.oc-nav-item:hover::before{transform:scaleY(1);}' +
        '.oc-nav-item:hover svg{filter:drop-shadow(0 0 6px rgba(168,85,247,.5));transform:scale(1.15);}' +
        '.oc-nav-item svg{transition:all .2s ease;width:16px;height:16px;flex-shrink:0;}' +
        '.oc-nav-item.oc-active{color:#fff;background:rgba(168,85,247,.12);border-left:2px solid #a855f7;font-weight:600;}' +
        '.oc-nav-item.oc-active::before{transform:scaleY(1);}' +
        '.oc-nav-item.oc-active svg{filter:drop-shadow(0 0 6px rgba(168,85,247,.6));}' +
        '.oc-ic{width:16px;height:16px;flex-shrink:0;transition:all .2s ease;}' +
        '.oc-nav-item .oc-ic,.oc-hdr-btn .oc-ic{width:16px;height:16px;}' +
        '.oc-nav-item:hover .oc-ic{filter:drop-shadow(0 0 6px rgba(168,85,247,.5));transform:scale(1.15);}' +
        '.oc-nav-item.oc-active .oc-ic{filter:drop-shadow(0 0 6px rgba(168,85,247,.6));}' +
        /* buttons */
        '.oc-btn{padding:10px 20px;border-radius:10px;font-weight:600;font-size:13px;cursor:pointer;transition:all .25s ease;border:1px solid transparent;position:relative;overflow:hidden;}' +
        '.oc-btn::after{content:"";position:absolute;inset:0;background:linear-gradient(135deg,rgba(255,255,255,.08),transparent);opacity:0;transition:opacity .25s;}' +
        '.oc-btn:hover::after{opacity:1;}' +
        '.oc-btn:active{transform:scale(.97);}' +
        '.oc-btn-primary{background:linear-gradient(135deg,#a855f7,#7c3aed);color:#fff;border-color:rgba(168,85,247,.3);}' +
        '.oc-btn-primary:hover{box-shadow:0 0 20px rgba(168,85,247,.4),0 0 40px rgba(168,85,247,.15);transform:translateY(-1px);}' +
        '.oc-btn-ghost{background:rgba(168,85,247,.06);color:#c084fc;border-color:rgba(168,85,247,.2);}' +
        '.oc-btn-ghost:hover{background:rgba(168,85,247,.14);border-color:rgba(168,85,247,.4);box-shadow:0 0 16px rgba(168,85,247,.2);}' +
        '.oc-btn-danger{background:rgba(224,132,143,.08);color:#e0848f;border-color:rgba(224,132,143,.2);}' +
        '.oc-btn-danger:hover{background:rgba(224,132,143,.15);box-shadow:0 0 16px rgba(224,132,143,.2);}' +
        /* cards */
        '.oc-card{padding:20px;border-radius:16px;background:#020208;border:1px solid rgba(138,105,235,.26);transition:all .3s ease;position:relative;overflow:hidden;}' +
        '.oc-card::before{content:"";position:absolute;inset:0;background:linear-gradient(135deg,rgba(168,85,247,.05),transparent);opacity:0;transition:opacity .3s;pointer-events:none;}' +
        '.oc-card:hover{border-color:rgba(168,85,247,.4);transform:translateY(-2px);box-shadow:0 8px 32px rgba(0,0,0,.6),0 0 24px rgba(147,51,234,.14);}' +
        '.oc-card:hover::before{opacity:1;}' +
        /* stat cards */
        '.oc-stat{padding:18px;border-radius:14px;background:#020208;border:1px solid rgba(138,105,235,.26);transition:all .3s ease;cursor:default;position:relative;overflow:hidden;}' +
        '.oc-stat:hover{border-color:rgba(168,85,247,.4);transform:translateY(-3px);box-shadow:0 8px 24px rgba(0,0,0,.6),0 0 20px rgba(147,51,234,.16);}' +
        '.oc-stat:hover .oc-stat-val{filter:drop-shadow(0 0 10px currentColor);}' +
        '.oc-stat-val{transition:filter .3s;}' +
        /* proper tables: zebra + hover glow + header underline */
        '#oc-dash-content table{background:transparent;}' +
        '#oc-dash-content thead th{letter-spacing:.08em;}' +
        '#oc-dash-content tbody tr:nth-child(even){background:rgba(168,85,247,.025);}' +
        '#oc-dash-content tbody tr:hover{background:rgba(168,85,247,.07) !important;box-shadow:inset 0 0 26px rgba(147,51,234,.08);}' +
        /* table rows */
        '.oc-row{border-bottom:1px solid #1b122b;transition:background .2s;}' +
        '.oc-row:hover{background:rgba(168,85,247,.04);}' +
        /* header buttons */
        '.oc-hdr-btn{padding:10px 14px;border-radius:12px;background:#0f0a1c;border:1px solid #231738;color:#fff;cursor:pointer;display:flex;align-items:center;gap:6px;transition:all .25s ease;font-size:12px;font-weight:600;}' +
        '.oc-hdr-btn:hover{border-color:#a855f7;box-shadow:0 0 16px rgba(168,85,247,.2);transform:translateY(-1px);}' +
        '.oc-hdr-btn:hover svg{filter:drop-shadow(0 0 4px rgba(168,85,247,.5));}' +
        '.oc-hdr-btn svg{transition:filter .2s;}' +
        /* profile cards */
        '.oc-prof{padding:16px;border-radius:14px;background:#020208;border:1px solid rgba(138,105,235,.26);cursor:pointer;transition:all .3s ease;}' +
        '.oc-prof:hover{border-color:rgba(168,85,247,.4);transform:translateY(-3px);box-shadow:0 8px 28px rgba(0,0,0,.6),0 0 18px rgba(147,51,234,.14);}' +
        '.oc-prof:hover .oc-prof-avatar{box-shadow:0 0 16px rgba(168,85,247,.4);transform:scale(1.08);}' +
        '.oc-prof-avatar{transition:all .3s;}' +
        /* editable settings: neon switches + theme chips + color picker + preview */
        '.oc-set{display:flex;align-items:center;gap:14px;padding:13px 0;border-bottom:1px solid rgba(138,105,235,.1);}' +
        '.oc-set:last-child{border-bottom:none;}' +
        '.oc-fl-out{flex:1;min-width:0;}' +
        '.oc-switch{position:relative;width:46px;height:26px;border-radius:9999px;background:#1a1325;border:1px solid rgba(168,85,247,.35);cursor:pointer;transition:all .25s ease;flex-shrink:0;outline:none;}' +
        '.oc-switch::after{content:"";position:absolute;top:3px;left:3px;width:18px;height:18px;border-radius:3px;background:#7d7794;transition:all .25s ease;box-shadow:0 0 8px rgba(0,0,0,.5);}' +
        '.oc-switch:hover{border-color:#a855f7;}' +
        '.oc-switch.on{background:linear-gradient(135deg,#a855f7,#7c3aed) !important;border-color:rgba(168,85,247,.6);box-shadow:0 0 14px rgba(168,85,247,.5);}' +
        '.oc-switch.on::after{left:23px;background:#fff;}' +
        '.oc-th-opt{padding:9px 16px;border-radius:10px;border:1px solid rgba(138,105,235,.2);background:#020208;color:#c084fc;font-size:12px;font-weight:700;cursor:pointer;transition:all .2s ease;}' +
        '.oc-th-opt:hover{border-color:#a855f7;box-shadow:0 0 12px rgba(168,85,247,.25);}' +
        '.oc-th-opt.on{background:linear-gradient(135deg,#a855f7,#7c3aed);color:#fff;border-color:transparent;box-shadow:0 0 16px rgba(168,85,247,.4);}' +
        '.oc-qpick{width:32px;height:32px;border-radius:9px;background:#000;border:1px solid rgba(168,85,247,.5);cursor:pointer;padding:0;flex-shrink:0;}' +
        '.oc-qpick:hover{box-shadow:0 0 12px rgba(168,85,247,.5);}' +
        '@keyframes ocPreFill{from{transform:translateX(-30%);}to{transform:translateX(30%);}}' +
        /* page entrance choreography */
        '#oc-dash-content .grid > div{animation:ocRise .45s cubic-bezier(.22,.8,.36,1) both;}' +
        '@keyframes ocRise{from{opacity:0;transform:translateY(10px);filter:blur(3px);}to{opacity:1;transform:translateY(0);filter:blur(0);}}' +
        '#oc-dash-content .grid > div:nth-child(1){animation-delay:.02s;}' +
        '#oc-dash-content .grid > div:nth-child(2){animation-delay:.07s;}' +
        '#oc-dash-content .grid > div:nth-child(3){animation-delay:.12s;}' +
        '#oc-dash-content .grid > div:nth-child(4){animation-delay:.17s;}' +
        '#oc-dash-content .grid > div:nth-child(5){animation-delay:.22s;}' +
        '#oc-dash-content .grid > div:nth-child(6){animation-delay:.27s;}' +
        '#oc-dash-content .grid > div:nth-child(7){animation-delay:.32s;}' +
        '#oc-dash-content .grid > div:nth-child(8){animation-delay:.37s;}' +
        /* toast notification */
        '.oc-toast{display:flex;align-items:center;gap:10px;padding:12px 18px;border-radius:14px;background:#0e0a1a;border:1px solid #2a1745;font-size:12px;font-weight:600;color:#e8e4f6;box-shadow:0 12px 32px rgba(0,0,0,.5);opacity:0;transform:translateX(120%);transition:all .4s cubic-bezier(.22,1,.36,1);}' +
        '.oc-toast.oc-show{opacity:1;transform:translateX(0);}' +
        '.oc-toast.oc-hide{opacity:0;transform:translateX(120%);}' +
        /* logo pulse */
        '.oc-logo-box{transition:all .3s;}' +
        '.oc-logo-box:hover{box-shadow:0 0 20px rgba(168,85,247,.3);transform:scale(1.05);}' +
        /* signout */
        '.oc-signout{transition:all .2s;}' +
        '.oc-signout:hover{color:#e0848f !important;transform:scale(1.1);}' +
        /* overlay */
        '.oc-overlay{position:fixed;inset:0;background:rgba(5,5,8,.65);z-index:2147483600;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(8px);animation:ocFadeIn .25s ease;}' +
        '@keyframes ocFadeIn{from{opacity:0}to{opacity:1}}' +
        '@keyframes ocSlideUp{from{opacity:0;transform:translateY(20px)}to{opacity:1;transform:translateY(0)}}' +
        '.oc-overlay>div{animation:ocSlideUp .3s ease;}' +
        /* scrollbar */
        '#oc-dash-content::-webkit-scrollbar{width:6px;}' +
        '#oc-dash-content::-webkit-scrollbar-track{background:transparent;}' +
        '#oc-dash-content::-webkit-scrollbar-thumb{background:#2a1745;border-radius:3px;}' +
        '#oc-dash-content::-webkit-scrollbar-thumb:hover{background:#3b1f5c;}' +
        /* ===== UNIVERSAL HOVER: every button/card/row in dashboard ===== */
        '#oc-dash-content button{cursor:pointer;}' +
        '#oc-dash-content button:not(.oc-hdr-btn):not(.oc-btn){transition:all .2s ease;}' +
        '#oc-dash-content button:not(.oc-hdr-btn):not(.oc-btn):hover{border-color:#a855f7 !important;box-shadow:0 0 14px rgba(168,85,247,.25);' +
            'background:rgba(168,85,247,.08) !important;transform:translateY(-1px);}' +
        '#oc-dash-content button:not(.oc-hdr-btn):not(.oc-btn):active{transform:scale(.97);}' +
        '#oc-dash-content [class*="rounded-"][class*="b-"]:not(button):not(a):not(svg):hover,' +
        '#oc-dash-content [class*="rounded-"][class*="background:#0a"]:hover,' +
        '#oc-dash-content [style*="background:#0a0"],#oc-dash-content [style*="background:#0e0"],#oc-dash-content [style*="background:#10"]' +
        '{transition:all .3s ease;}' +
        /* cards edging with purple on hover */
        '#oc-dash-content [style*="background:#0a0714"],#oc-dash-content [style*="background:#0e0a1a"],' +
        '#oc-dash-content [style*="background:#0f0a1c"],#oc-dash-content [style*="background:#1b122b"]' +
        '{transition:all .3s ease;}' +
        '#oc-dash-content [style*="background:#0a0714"]:hover,#oc-dash-content [style*="background:#0e0a1a"]:hover,' +
        '#oc-dash-content [style*="background:#0f0a1c"]:hover' +
        '{border-color:rgba(168,85,247,.25) !important;transform:translateY(-2px);box-shadow:0 8px 24px rgba(0,0,0,.3),0 0 12px rgba(168,85,247,.06);}' +
        /* table rows hover */
        '#oc-dash-content tr{transition:background .2s;}' +
        '#oc-dash-content tbody tr:hover{background:rgba(168,85,247,.06);}' +
        '#oc-dash-content tbody tr:hover td{color:#e9d5ff !important;}' +
        /* links */
        '#oc-dash-content a{transition:color .2s;}' +
        '#oc-dash-content a:hover{color:#c084fc !important;}' +
        /* inputs */
        '#oc-dash-content input,#oc-dash-content select,#oc-dash-content textarea{transition:all .2s ease;}' +
        '#oc-dash-content input:focus,#oc-dash-content select:focus,#oc-dash-content textarea:focus{border-color:#a855f7 !important;box-shadow:0 0 0 3px rgba(168,85,247,.15);outline:none;}' +
        /* status badges glow */
        '#oc-dash-content [class*="text-green"],[style*="color:#7dc9a0"]{transition:filter .2s;}' +
        '</style>' +

        '<div id="oc-dash-glow"></div>' +
        '<div style="display:flex;height:100vh;overflow:hidden;background:#000;color:#fff;font-family:Inter,ui-sans-serif,system-ui,sans-serif;width:100%;position:relative;z-index:2;">' +

        '<aside style="width:248px;flex-shrink:0;border-right:1px solid #17102a;background:#010103;padding:16px;display:flex;flex-direction:column;justify-content:space-between;min-height:100vh;position:sticky;top:0;">' +
            '<div>' +
                '<div style="display:flex;align-items:center;gap:12px;padding:8px;margin-bottom:16px;">' +
                    '<div class="oc-logo-box" style="width:42px;height:42px;border-radius:12px;background:#0b0714;border:1px solid #2a1b40;display:flex;align-items:center;justify-content:center;font-weight:800;color:#c084fc;font-size:15px;">(*&gt;</div>' +
                    '<div><div style="font-weight:700;color:#fff;font-size:16px;line-height:1.2;">Ocean</div><div style="font-size:12px;color:#808098;">anticheat.ac</div></div>' +
                '</div>' +
                '<nav style="display:flex;flex-direction:column;gap:2px;">' + navHtml + '</nav>' +
            '</div>' +
            '<div style="display:flex;align-items:center;justify-content:space-between;padding:10px 12px;border-radius:14px;background:#03030a;border:1px solid #1e152e;">' +
                '<div style="display:flex;align-items:center;gap:10px;">' +
                    '<div class="oc-prof-avatar" style="width:36px;height:36px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:700;font-size:14px;color:#fff;">' + (user.username || 'U').charAt(0).toUpperCase() + '</div>' +
                    '<div><div style="font-weight:600;color:#fff;font-size:13px;">' + (user.username || 'User') + '</div><div style="font-size:11px;color:#808098;">Customer</div></div>' +
                '</div>' +
                '<button class="oc-signout" data-oc-signout style="padding:6px;background:none;border:none;color:#808098;cursor:pointer;border-radius:6px;">' + iconTrash() + '</button>' +
            '</div>' +
        '</aside>' +

        '<main style="flex:1;min-width:0;padding:32px;box-sizing:border-box;height:100vh;overflow-y:auto;overflow-x:hidden;">' +
            '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:24px;">' +
                '<div>' +
                    '<div style="font-size:12px;color:#808098;margin-bottom:4px;">dashboard &gt; ' + (route === 'home' ? 'home' : routeTitles[route]) + '</div>' +
                    '<h1 style="font-size:28px;font-weight:800;color:#fff;margin:0;letter-spacing:-.03em;">' + (route === 'home'
                        ? 'Welcome Back, <span style="color:#c084fc;">' + (user.username || 'User') + '</span>'
                        : (routeTitles[route] || 'Home')) + '</h1>' +
                '</div>' +
                '<div style="display:flex;align-items:center;gap:12px;">' +
                    '<button id="oc-notif-btn" class="oc-hdr-btn">' + iconBell() + '<span style="font-size:12px;font-weight:600;">Notifications</span></button>' +
                    '<button class="oc-hdr-btn">' + iconClock() + '<span>Date Filter</span></button>' +
                '</div>' +
            '</div>' +
            '<div id="oc-dash-content">' + miniSkeleton(4) + '</div>' +
        '</main>' +

        '</div>' +

        '<div id="oc-notif-stack" style="position:fixed;bottom:24px;right:24px;z-index:50;display:flex;flex-direction:column;gap:8px;"></div>';

        document.body.innerHTML = html;
        document.body.style.background = '#050508';
        document.body.style.margin = '0';
        document.body.style.padding = '0';
        document.body.style.overflow = 'hidden';
        document.body.classList.remove('oc-cursor-on');
        document.querySelectorAll('#oc-ctrl-bar, .oc-cursor-trail, .oc-cursor, .oc-cursor-follow, .oc-cursor-ring, .oc-cursor-aura, .oc-noise, .oc-scanline, .oc-progress, .oc-top, .oc-mouse-glow, [data-sidebar]').forEach(function(el) { if (el.parentNode) el.parentNode.removeChild(el); });

        var glow = document.getElementById('oc-dash-glow');
        if (glow) {
            listen(document, 'mousemove', function(e) {
                glow.style.left = e.clientX + 'px';
                glow.style.top = e.clientY + 'px';
            });
            var lipos = { x: window.innerWidth / 2, y: 200 };
            glow.style.left = lipos.x + 'px';
            glow.style.top = lipos.y + 'px';
        }

        var so = document.querySelector('[data-oc-signout]');
        if (so) {
            so.onclick = function() {
                fetch('/api/auth/logout', { method: 'POST', credentials: 'same-origin' }).then(function() {
                    try { localStorage.removeItem('__OC_REAL_USER__'); } catch(e) {}
                    location.reload();
                }).catch(function() {});
            };
        }

        function ocNotify(msg, type) {
            var stack = document.getElementById('oc-notif-stack');
            if (!stack) { stack = make('div', ''); stack.id = 'oc-notif-stack'; stack.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:50;display:flex;flex-direction:column;gap:8px;'; document.body.appendChild(stack); }
            var color = type === 'error' ? '#e0848f' : type === 'warn' ? '#d8b46a' : '#7dc9a0';
            var el = document.createElement('div');
            el.className = 'oc-toast';
            el.style.borderColor = color + '66';
            el.style.color = color;
            el.innerHTML = '<span style="width:8px;height:8px;border-radius:2px;background:' + color + ';display:inline-block;"></span>' + msg;
            stack.appendChild(el);
            requestAnimationFrame(function() { el.classList.add('oc-show'); });
            setTimeout(function() {
                el.classList.remove('oc-show');
                el.classList.add('oc-hide');
                setTimeout(function() { if (el.parentNode) el.parentNode.removeChild(el); }, 400);
            }, 4200);
        }

        function showProfileOverlay(pid) {
            if (!pid) return;
            var found = null;
            (profilesFrom(scans)).forEach(function(p) { if (p.id === decodeURIComponent(pid) || pid === p.id) found = p; });
            if (!found) { ocNotify('Profile not found', 'error'); return; }
            var dets = Object.keys(found.dets).map(function(k) { return { name: k, count: found.dets[k] }; });
            var detRows = dets.slice(0, 15).map(function(d) {
                return '<div style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid #1b122b;"><span style="font-size:12px;color:#e8e4f6;">' + d.name + '</span><span style="font-size:12px;color:#e0848f;">' + d.count + 'Ă—</span></div>';
            }).join('');
            var ov = document.createElement('div');
            ov.id = 'oc-profile-overlay';
            ov.style.cssText = 'position:fixed;inset:0;background:rgba(5,5,8,.6);z-index:2147483600;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(6px);';
            ov.innerHTML = '<div style="width:min(680px,92vw);max-height:86vh;overflow:auto;background:#0a0714;border:1px solid #2a1745;border-radius:18px;padding:24px;box-shadow:0 30px 80px rgba(0,0,0,.6);">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">' +
                    '<div style="display:flex;align-items:center;gap:14px;">' +
                        '<div style="width:52px;height:52px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:800;color:#fff;font-size:18px;">' + (found.username.charAt(0).toUpperCase()) + '</div>' +
                        '<div><div style="font-size:18px;font-weight:800;color:#fff;">' + found.username + '</div><div style="font-size:12px;color:#a29cb8;">' + found.pcName + '</div></div>' +
                    '</div>' +
                    '<button data-oc-close-profile style="background:none;border:none;color:#a29cb8;font-size:22px;cursor:pointer;">&times;</button>' +
                '</div>' +
                '<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-bottom:16px;">' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#fff;">' + found.scanCount + '</div><div style="font-size:11px;color:#808098;">Scans</div></div>' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#e0848f;">' + dets.length + '</div><div style="font-size:11px;color:#808098;">Detections</div></div>' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#a855f7;">' + found.games.length + '</div><div style="font-size:11px;color:#808098;">Games</div></div>' +
                '</div>' +
                '<div style="font-size:12px;color:#a29cb8;margin-bottom:6px;">Detected cheats</div>' +
                (detRows || '<div style="font-size:13px;color:#808098;padding:12px 0;">No detections recorded.</div>') +
                '<div style="margin-top:16px;font-family:monospace;font-size:11px;color:#4b5563;">HWID: ' + found.hwid + ' · PIN: ' + found.pin + '</div>' +
                '</div>';
            document.body.appendChild(ov);
            ov.querySelector('[data-oc-close-profile]').onclick = function() { document.body.removeChild(ov); };
            ov.addEventListener('click', function(e) { if (e.target === ov) document.body.removeChild(ov); });
        }

        window.__OC_MAKE_PIN = function(game) {
            try {
                game = game || 'FiveM';
                fetch('/api/pins', { method: 'POST', headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin', body: JSON.stringify([{ game: game, status: 'active', maxUses: 999 }]) }).then(function(r) { return r.json().catch(function() { return []; }); }).then(function(arr) {
                    var code = (Array.isArray(arr) && arr[0] && arr[0].code) ? arr[0].code : '';
                    if (code) {
                        ocNotify('Pin ' + code + ' created', 'success');
                        setTimeout(function() { try { location.reload(); } catch (e) {} }, 600);
                    } else {
                        ocNotify('Could not create pin', 'error');
                    }
                }).catch(function() { ocNotify('Could not create pin', 'error'); });
            } catch (e) { ocNotify('Could not create pin', 'error'); }
        };

        Promise.all([
            fetch('/api/auth/me', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : {}; }).catch(function() { return {}; }),
            fetch('/api/stats', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : {}; }).catch(function() { return {}; }),
            fetch('/api/scans', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : []; }).catch(function() { return []; }),
            fetch('/api/pins', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : []; }).catch(function() { return []; }),
            fetch('/api/config', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : {}; }).catch(function() { return {}; })
        ]).then(function(results) {
            var me = results[0];
            var st = results[1];
            var sc = Array.isArray(results[2]) ? results[2] : [];
            var pi = Array.isArray(results[3]) ? results[3] : [];
            cfg = (results[4] && results[4].config) || {};
            // Detection modules are ON by default (the scanner treats an absent
            // switch as enabled). The config only ever stores explicit choices.
            try {
                OB_MODULES.forEach(function(m) { if (cfg[m.key] === undefined) cfg[m.key] = true; });
            } catch (e) {}
            window.__OC_CFG = cfg;
            if (me && me.user) user = me.user;
            stats = st;
            scans = sc;
            pins = pi;

            var uname = user.username || 'User';
            var h1 = document.querySelector('h1');
            if (h1) {
                h1.innerHTML = route === 'home'
                    ? 'Welcome Back, <span style="color:#c084fc;">' + uname + '</span>'
                    : (routeTitles[route] || 'Home');
            }
            var avatarEl = document.querySelector('aside > div:last-child > div > div:first-child > div');
            if (avatarEl) avatarEl.textContent = uname.charAt(0).toUpperCase();
            var nameEl = document.querySelector('aside > div:last-child > div > div > div:first-child');
            if (nameEl) nameEl.textContent = uname;

            var content = document.getElementById('oc-dash-content');
            if (content) {
                content.innerHTML = renderPageContent(route, user, stats, scans, pins);
                try { ocLiveScannerCard(content); } catch (e) {}
            }
            ocGuiRender();

            document.querySelectorAll('[data-oc-profile]').forEach(function(el) {
                el.addEventListener('click', function() { showProfileOverlay(el.getAttribute('data-oc-profile')); });
            });

            document.querySelectorAll('[data-oc-open-pin]').forEach(function(el) {
                el.addEventListener('click', function() {
                    console.log('--- OC OPEN PIN CLICKED ---', el.getAttribute('data-oc-open-pin'));
                    var code = el.getAttribute('data-oc-open-pin');
                    if (window.openPinReport) {
                        window.openPinReport(code);
                    } else {
                        var k = (pins || []).find(function(x) { return (x.code || x.key || x.pin) === code; });
                        var s = k && k.scanId ? (scans || []).find(function(x) { return x.id === k.scanId; }) : (scans || []).find(function(x) { return x.pin === code; });
                        if (s && window.renderDetailsReport) {
                            window.renderDetailsReport(s);
                        } else if (k && window.renderPinInfo) {
                            window.renderPinInfo(k);
                        } else {
                            ocNotify('No scan report found for pin ' + code, 'warn');
                        }
                    }
                });
            });

            var notifBtn = document.getElementById('oc-notif-btn');
            if (notifBtn) {
                notifBtn.addEventListener('click', function() {
                    ocNotify('All systems operational â€” ' + (new Date().toLocaleTimeString()), 'success');
                });
            }

            var sendBtn = document.querySelector('[data-oc-chat-send]');
            if (sendBtn) {
                sendBtn.addEventListener('click', function() {
                    var inp = document.getElementById('oc-chat-input');
                    if (inp && inp.value.trim()) { ocNotify('Message sent', 'success'); inp.value = ''; }
                });
            }

            ocNotify('Welcome back, ' + uname, 'success');
        });
    }

    function boot() {
        detect();
        var isDash = (location.pathname || '').indexOf('/dashboard') === 0;
        if (window.__OC_EMBEDDED_DASH__) isDash = true;
        document.body.classList.add('ocean-v2');
        try { initFx(); } catch (e) {}
        try { initEntrance(); } catch (e) {}

        if (isDash) {
            init1to1Dashboard();
            return;
        }

        buildAmbientLayers();
        renderThemeToggle();
        renderNav();
        renderFooter();
        initScrollNav();
        initHamburger();
        initBlurWords();
        initFAQ();
        initAutoNav();
        initActiveNav();
        initRevealEffects();
        initCounters();
        initTyping();
        initGlitch();
        initMarquee();
        initClock();
        initRipples();
        initMagnetic();
        initTilt();
        initSpotlight();
        initCursor();
    }

    onReady(boot);

    /* =================================================================
       23 â€” KONZI GYŐ°JTEMÉNY: MINI EASTER EGG (ossze-vissza színváltás)
       ================================================================= */
    var konami = [];
    var konamiSeq = [38, 38, 40, 40, 37, 39, 37, 39, 66, 65];
    listen(document, 'keydown', function (e) {
        konami.push(e.keyCode);
        if (konami.length > konamiSeq.length) konami.shift();
        if (konami.join(',') === konamiSeq.join(',')) {
            konami = [];
            var body = document.body;
            var hue = 0;
            var iv = setInterval(function () {
                hue = (hue + 18) % 360;
                body.style.background = 'hsl(' + hue + ' 60% 8%)';
            }, 240);
            setTimeout(function () {
                clearInterval(iv);
                body.style.background = '';
                window.showToast('Gratulálunk, hiba nĂ©lkül kalibráltál! đźŽ‰', 'success');
            }, 4200);
        }
    });
})();