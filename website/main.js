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
    function buildAmbientLayers() {
        var noise = make('div', 'oc-noise');
        noise.setAttribute('aria-hidden', 'true');
        document.body.appendChild(noise);

        var scan = make('div', 'oc-scanline');
        scan.setAttribute('aria-hidden', 'true');
        document.body.appendChild(scan);

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
                b.style.borderColor = i % 2 ? 'rgba(34,211,238,0.8)' : 'rgba(216,180,254,0.8)';
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
                ctx.beginPath();
                ctx.arc(d.x, d.y, d.r, 0, Math.PI * 2);
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
        var color = type === 'success' ? '#34d399' : type === 'error' ? '#f87171' : '#a855f7';
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
        return '<svg width="130" height="34" viewBox="0 0 206 50" fill="none" xmlns="http://www.w3.org/2000/svg" style="height:2rem;width:auto"><rect x="2" y="6" width="38" height="38" rx="9" fill="url(#oc-logo-g)"/><defs><linearGradient id="oc-logo-g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#7c3aed"/><stop offset="1" stop-color="#e879f9"/></linearGradient></defs><path d="M24 14v15a7 7 0 0 0 14 0" stroke="#fff" stroke-width="5" stroke-linecap="round"/><text x="50" y="33" font-family="Arial, sans-serif" font-weight="700" font-size="24" fill="#f7f8fa">OCEAN</text></svg>';
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
    function detect() {
        reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        finePointer = window.matchMedia('(pointer: fine)').matches && !window.matchMedia('(any-pointer: coarse)').matches;
    }

    /* =================================================================
       17 â€” 1:1 DASHBOARD (valós adatokkal)
       ================================================================= */
    function init1to1Dashboard() {
        var path = location.pathname || '';
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

        function statusBadge(status) {
            var s = (status || '').toLowerCase();
            if (s === 'complete' || s === 'clean' || s.indexOf('clean') > -1) return '<span style="background:rgba(34,197,94,.12);color:#22c55e;border:1px solid rgba(34,197,94,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">CLEAN</span>';
            if (s.indexOf('cheat') > -1) return '<span style="background:rgba(239,68,68,.12);color:#ef4444;border:1px solid rgba(239,68,68,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">CHEAT</span>';
            if (s.indexOf('suspicious') > -1) return '<span style="background:rgba(245,158,11,.12);color:#f59e0b;border:1px solid rgba(245,158,11,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">SUSPICIOUS</span>';
            if (s === 'scanning') return '<span style="background:rgba(168,85,247,.12);color:#a855f7;border:1px solid rgba(168,85,247,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">SCANNING</span>';
            if (s === 'pending') return '<span style="background:rgba(245,158,11,.12);color:#f59e0b;border:1px solid rgba(245,158,11,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">PENDING</span>';
            if (s === 'active') return '<span style="background:rgba(34,197,94,.12);color:#22c55e;border:1px solid rgba(34,197,94,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">ACTIVE</span>';
            if (s === 'used') return '<span style="background:rgba(148,163,184,.12);color:#94a3b8;border:1px solid rgba(148,163,184,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">USED</span>';
            return '<span style="background:rgba(148,163,184,.12);color:#94a3b8;border:1px solid rgba(148,163,184,.25);padding:3px 10px;border-radius:9999px;font-size:11px;font-weight:700;">' + (status || 'UNKNOWN').toUpperCase() + '</span>';
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
                    '<td style="padding:12px 16px;font-size:13px;color:#e2e8f0;font-family:monospace;font-weight:600;">' + code + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;color:#94a3b8;">' + game + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;">' + statusBadge(st) + '</td>' +
                    '<td style="padding:12px 16px;font-size:13px;color:#94a3b8;">' + created + '</td>' +
                    '</tr>';
            }).join('');

            var scansRows = recentScans.map(function(s) {
                var player = s.username || s.playerName || '--';
                var pc = s.pcName || '--';
                var game = s.game || 'FiveM';
                var st = s.status || '--';
                var ts = timeAgo(s.timestamp);
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:10px 14px;font-size:13px;color:#e2e8f0;">' + player + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;color:#94a3b8;">' + pc + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;color:#94a3b8;">' + game + '</td>' +
                    '<td style="padding:10px 14px;font-size:13px;">' + statusBadge(st) + '</td>' +
                    '<td style="padding:10px 14px;font-size:12px;color:#64748b;">' + ts + '</td>' +
                    '</tr>';
            }).join('');

            return '' +
            '<div class="grid grid-cols-12 gap-5">' +

            '<div class="col-span-8 space-y-5">' +

            '<div style="padding:20px;border-radius:18px;background:#0a0714;border:1px solid #1b122b;">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">' +
                    '<div style="font-weight:600;color:#fff;font-size:14px;">Scan Activity</div>' +
                    '<div style="font-size:12px;color:#94a3b8;">Last 6 months</div>' +
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
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#38bdf8;">' + (stats.totalScans || 0) + '</div>' +
                    '<div style="font-size:11px;color:#a855f7;margin-top:4px;">' + (stats.scansThisMonth || 0) + ' this month</div>' +
                '</div>' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Detections</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#ef4444;">' + (stats.detected || 0) + '</div>' +
                    '<div style="font-size:11px;color:#ef4444;margin-top:4px;">' + (stats.cheating || 0) + ' cheats found</div>' +
                '</div>' +
                '<div class="oc-stat"><div style="font-size:11px;color:#808098;margin-bottom:6px;">Profiles</div>' +
                    '<div class="oc-stat-val" style="font-size:24px;font-weight:800;color:#22c55e;">' + (stats.profiles || 0) + '</div>' +
                    '<div style="font-size:11px;color:#22c55e;margin-top:4px;">' + (stats.clean || 0) + ' clean</div>' +
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
                        '<div style="font-size:13px;color:#e2e8f0;font-weight:600;">Ocean v2.4 Released</div>' +
                        '<div style="font-size:11px;color:#94a3b8;margin-top:4px;">New detection engine with improved accuracy</div>' +
                        '<div style="font-size:10px;color:#64748b;margin-top:6px;">Sep 6, 2026</div>' +
                    '</div>' +
                    '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;">' +
                        '<div style="font-size:13px;color:#e2e8f0;font-weight:600;">FiveM Update Support</div>' +
                        '<div style="font-size:11px;color:#94a3b8;margin-top:4px;">Full compatibility with latest FiveM build</div>' +
                        '<div style="font-size:10px;color:#64748b;margin-top:6px;">Sep 4, 2026</div>' +
                    '</div>' +
                    '<div style="padding:14px 16px;border-radius:12px;background:#120c22;border:1px solid #1f1436;">' +
                        '<div style="font-size:13px;color:#e2e8f0;font-weight:600;">Custom Scripts Feature</div>' +
                        '<div style="font-size:11px;color:#94a3b8;margin-top:4px;">Write your own Lua detection scripts in Ocean Lab</div>' +
                        '<div style="font-size:10px;color:#64748b;margin-top:6px;">Sep 2, 2026</div>' +
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
                 { label: 'Active', v: stats.activePins || 0, c: '#22c55e' },
                 { label: 'Scanning', v: stats.scanningPins || 0, c: '#a855f7' },
                 { label: 'Complete', v: stats.completedPins || 0, c: '#38bdf8' },
                 { label: 'Pending', v: stats.pendingPins || 0, c: '#f59e0b' },
                 { label: 'Expired', v: stats.expiredPins || 0, c: '#94a3b8' }
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
                         '<button data-oc-open-pin="' + code + '" style="padding:8px 20px;border-radius:10px;background:linear-gradient(135deg,rgba(168,85,247,0.18),rgba(232,121,249,0.12));border:1px solid rgba(168,85,247,0.45);color:#e9d5ff;font-size:12px;font-weight:700;cursor:pointer;transition:all .25s;letter-spacing:0.04em;text-transform:uppercase;box-shadow:0 0 12px rgba(168,85,247,0.15),inset 0 0 20px rgba(168,85,247,0.05);position:relative;overflow:hidden;">' +
                             '<span style="position:relative;z-index:1;">View</span>' +
                             '<span style="position:absolute;inset:0;border-radius:inherit;background:linear-gradient(90deg,transparent,rgba(232,121,249,0.18),transparent);background-size:200% 100%;animation:oc-btn-shift 2s ease-in-out infinite;opacity:0;transition:opacity .25s;"></span>' +
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
                     '<p style="font-size:13px;color:#94a3b8;margin:0 0 14px 0;">Generate a fresh scan pin, share it with a player, and watch the live result.</p>' +
                     '<button onclick="if(window.__OC_MAKE_PIN)window.__OC_MAKE_PIN()" class="oc-btn oc-btn-primary">+ Create New Pin</button>') +
                 '</div>' +
                 '</div>';
         }

        /* ---- Custom Strings page (reuses the parsed ocean/log text) ---- */
        function renderStringsPage(user, stats, pins, scans) {
            var seen = {};
            var strings = [];
            (scans || []).forEach(function(s) {
                var txt = s.ocean || s.customStrings || '';
                if (s.strings && Array.isArray(s.strings)) {
                    s.strings.forEach(function(str) {
                        var key = String(str || '').trim(); if (!key) return;
                        if (!seen[key]) { seen[key] = { text: key, game: s.game || 'FiveM', count: 0 }; strings.push(seen[key]); }
                        seen[key].count++;
                    });
                }
                var re = /\*\*([^*]+)\*\*/g, m;
                while ((m = re.exec(txt))) {
                    var key = m[1].trim(); if (!key) continue;
                    if (!seen[key]) { seen[key] = { text: key, game: s.game || 'FiveM', count: 0 }; strings.push(seen[key]); }
                    seen[key].count++;
                }
            });
            var rows = strings.map(function(x) {
                return '<tr style="border-bottom:1px solid #1b122b;">' +
                    '<td style="padding:12px 14px;font-family:monospace;font-size:13px;color:#e9d5ff;">' + x.text + '</td>' +
                    '<td style="padding:12px 14px;font-size:13px;color:#94a3b8;">' + x.game + '</td>' +
                    '<td style="padding:12px 14px;font-size:13px;color:#a855f7;">' + x.count + '</td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Custom Strings', '<p style="font-size:13px;color:#94a3b8;margin:0 0 12px 0;">Detection strings collected from your scans.</p>' +
                    '<table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                    '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">String</th>' +
                    '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Game</th>' +
                    '<th style="padding:10px 14px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Count</th>' +
                    '</tr></thead><tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="3">No strings yet.</td></tr>') + '</tbody></table>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '</div>';
        }

        /* ---- Configs / Enterprise page ---- */
        function renderConfigsPage(user, stats, pins, scans) {
            var cfg = [
                { name: 'Default Config', desc: 'Balanced detection defaults', game: 'FiveM', status: 'active', count: stats.totalScans || 0 },
                { name: 'Strict Mode', desc: 'Aggressive anti-cheat rules', game: 'FiveM', status: 'active', count: stats.detected || 0 },
                { name: 'Screenshare Lite', desc: 'Quick shared-session profile', game: 'All', status: 'active', count: stats.completedPins || 0 }
            ];
            var cards = cfg.map(function(c) {
                return '<div style="padding:18px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;display:flex;flex-direction:column;gap:8px;">' +
                    '<div style="display:flex;justify-content:space-between;align-items:center;"><span style="font-weight:600;color:#fff;font-size:14px;">' + c.name + '</span>' + statusBadge(c.status) + '</div>' +
                    '<div style="font-size:12px;color:#94a3b8;">' + c.desc + '</div>' +
                    '<div style="font-size:11px;color:#a855f7;">' + c.game + ' · ' + c.count + ' linked</div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Configs / Enterprise', 'Tune how Ocean behaves for your community.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12"><div style="display:grid;grid-template-columns:repeat(3,1fr);gap:14px;">' + cards + '</div></div></div>';
        }

        /* ---- Custom Gui page ---- */
        function renderCustomGuiPage(user, stats, pins, scans) {
            var guis = [
                { name: 'My Custom Scan UI', desc: 'Personalized dashboard theme', status: 'active', updated: '2d ago' },
                { name: 'Branded Overlay', desc: 'Community logo watermark', status: 'active', updated: '5d ago' }
            ];
            var rows = guis.map(function(g) {
                return '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;display:flex;justify-content:space-between;align-items:center;">' +
                    '<div><div style="font-weight:600;color:#fff;font-size:14px;">' + g.name + '</div><div style="font-size:12px;color:#94a3b8;">' + g.desc + '</div></div>' +
                    '<div style="display:flex;align-items:center;gap:10px;">' + statusBadge(g.status) + '<span style="font-size:11px;color:#64748b;">' + g.updated + '</span></div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Custom GUI', 'Build your own scan interface. Your saved themes appear here.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12" style="display:flex;flex-direction:column;gap:10px;">' + rows + '</div></div>';
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
                    '<div><div style="font-weight:600;color:#fff;font-size:14px;">' + p.name + '</div><div style="font-size:12px;color:#94a3b8;">' + p.desc + '</div></div>' +
                    '<div style="font-size:12px;color:#a855f7;font-weight:600;">' + p.uses + ' uses</div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Publics GUI', 'Shared public interfaces everyone can use.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12" style="display:flex;flex-direction:column;gap:10px;">' + rows + '</div></div>';
        }

        /* ---- Detections page with real scan logs ---- */
        function renderDetectionsPage(user, stats, scans) {
            var RE = { red: '#ef4444' };
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
                    '<td style="padding:10px 12px;color:#64748b;">' + fmtDate(l.ts) + '</td>' +
                    '<td style="padding:10px 12px;color:#e9d5ff;">' + l.player + '</td>' +
                    '<td style="padding:10px 12px;color:#94a3b8;">' + l.pc + '</td>' +
                    '<td style="padding:10px 12px;color:#94a3b8;">' + l.game + '</td>' +
                    '<td style="padding:10px 12px;">' + statusBadge(l.status) + '</td>' +
                    '<td style="padding:10px 12px;color:' + (l.detections ? RE.red : '#808098') + ';">' + l.detections + '</td></tr>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12"><div style="display:grid;grid-template-columns:repeat(4,1fr);gap:14px;">' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#ef4444;">' + (stats.detected || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Total Detections</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#a855f7;">' + (stats.uniqueCheats || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Unique Cheats</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#f59e0b;">' + (stats.suspicious || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Suspicious</div></div>' +
                    '<div style="padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;"><div style="font-size:24px;font-weight:800;color:#22c55e;">' + (stats.clean || 0) + '</div><div style="font-size:11px;color:#808098;margin-top:4px;">Clean Scans</div></div>' +
                '</div></div>' +
                '<div class="col-span-12">' + card('Scan Logs', '<table style="width:100%;border-collapse:collapse;"><thead><tr style="border-bottom:1px solid #1b122b;">' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Date</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Player</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">PC</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Game</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Status</th>' +
                    '<th style="padding:10px 12px;text-align:left;font-size:11px;color:#808098;text-transform:uppercase;">Dets</th>' +
                    '</tr></thead><tbody>' + (rows || '<tr><td style="padding:20px;color:#808098;" colspan="6">No scans yet.</td></tr>') + '</tbody></table>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
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
                    '<div style="display:flex;justify-content:space-between;font-size:12px;color:#94a3b8;">' +
                        '<span>' + p.scanCount + ' scans</span><span style="color:#a855f7;">' + (p.dets.length ? p.dets.length + ' detections' : 'clean') + '</span>' +
                    '</div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5">' +
                '<div class="col-span-12">' + card('Player Profiles', '<p style="font-size:13px;color:#94a3b8;margin:0 0 12px 0;">Every scan is remembered per player. Click a profile to view full details.</p>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
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
                    '<td style="padding:12px 14px;color:#94a3b8;font-size:13px;">' + p.pcName + '</td>' +
                    '<td style="padding:12px 14px;color:#94a3b8;font-size:13px;">' + p.scanCount + '</td>' +
                    '<td style="padding:12px 14px;color:#ef4444;font-size:13px;">' + p.dets.join(', ').slice(0, 40) + '</td>' +
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
                    '<td style="padding:12px 14px;color:#94a3b8;font-size:13px;">' + p.pcName + '</td>' +
                    '<td style="padding:12px 14px;color:#94a3b8;font-size:13px;">' + p.scanCount + ' scans</td>' +
                    '<td style="padding:12px 14px;color:#ef4444;font-size:13px;">' + p.dets.length + '</td></tr>';
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
                    '<div><div style="font-size:13px;color:#e2e8f0;font-weight:600;">' + t.subject + '</div><div style="font-size:11px;color:#64748b;">' + t.id + ' · ' + t.date + ' · ' + t.msgs + ' messages</div></div>' +
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
                        '<div style="font-size:12px;color:#a855f7;font-weight:600;">' + m.user + ' · <span style="color:#64748b;font-weight:400;">' + m.time + '</span></div>' +
                        '<div style="font-size:13px;color:#e2e8f0;margin-top:4px;">' + m.text + '</div></div></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Community Chat', '<div style="background:#07050d;border-radius:12px;padding:16px;min-height:300px;">' + list + '</div><div style="display:flex;gap:8px;margin-top:10px;"><input id="oc-chat-input" placeholder="Type a message..." style="flex:1;padding:12px;border-radius:10px;background:#120c22;border:1px solid #1f1436;color:#e2e8f0;font-size:13px;outline:none;" /><button data-oc-chat-send style="padding:12px 20px;border-radius:10px;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;border:none;font-weight:700;font-size:13px;cursor:pointer;">Send</button></div>', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div></div>';
        }

        /* ---- Settings ---- */
        function renderSettingsPage(user, stats, scans, pins) {
            var fields = [
                { k: 'Username', v: user.username || '--' },
                { k: 'Member since', v: fmtDate(user.createdAt) },
                { k: 'Total scans', v: stats.totalScans || 0 },
                { k: 'Total pins', v: stats.totalPins || 0 },
                { k: 'Preferred game', v: 'FiveM' },
                { k: 'Notifications', v: 'Enabled' }
            ];
            var rows = fields.map(function(f) {
                return '<div style="display:flex;justify-content:space-between;padding:12px 0;border-bottom:1px solid #1b122b;"><span style="font-size:13px;color:#94a3b8;">' + f.k + '</span><span style="font-size:13px;color:#fff;font-weight:600;">' + f.v + '</span></div>';
            }).join('');
            return '<div class="grid grid-cols-12 gap-5"><div class="col-span-12">' + card('Settings', 'Your account and preferences.', '<a href="/dashboard" style="font-size:12px;color:#a855f7;text-decoration:none;font-weight:600;">Back</a>') + '</div>' +
                '<div class="col-span-12">' + rows + '</div></div>';
        }

        /* ---- API Keys ---- */
        function renderApiKeysPage(user, stats, pins, scans) {
            var codes = (pins || []).map(function(k) { return k.code || k.key || k.pin; }).filter(Boolean);
            var rows = codes.map(function(c) {
                return '<tr style="border-bottom:1px solid #1b122b;"><td style="padding:12px 14px;font-family:monospace;font-size:13px;color:#e9d5ff;">' + c + '</td>' +
                    '<td style="padding:12px 14px;font-size:13px;color:#94a3b8;">pin</td>' +
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
        'body{background:#050508 !important;overflow:hidden !important;}' +
        'body::before{content:"";position:fixed;inset:0;z-index:0;pointer-events:none;' +
            'background-image:linear-gradient(rgba(168,85,247,.04) 1px,transparent 1px),linear-gradient(90deg,rgba(168,85,247,.04) 1px,transparent 1px);' +
            'background-size:48px 48px;}' +
        '#oc-dash-glow{position:fixed;width:320px;height:320px;border-radius:50%;pointer-events:none;z-index:1;' +
            'background:radial-gradient(circle,rgba(168,85,247,.15) 0%,rgba(168,85,247,.05) 40%,transparent 70%);' +
            'filter:blur(40px);transform:translate(-50%,-50%);transition:left .08s ease-out,top .08s ease-out;opacity:0.8;}' +
        'body:hover #oc-dash-glow{opacity:1;}' +
        /* nav items */
        '.oc-nav-item{display:flex;align-items:center;gap:10px;padding:10px 12px;border-radius:10px;font-size:13px;font-weight:500;color:#94a3b8;cursor:pointer;transition:all .2s ease;position:relative;overflow:hidden;text-decoration:none;}' +
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
        '.oc-btn-danger{background:rgba(239,68,68,.08);color:#ef4444;border-color:rgba(239,68,68,.2);}' +
        '.oc-btn-danger:hover{background:rgba(239,68,68,.15);box-shadow:0 0 16px rgba(239,68,68,.2);}' +
        /* cards */
        '.oc-card{padding:20px;border-radius:16px;background:#0a0714;border:1px solid #1b122b;transition:all .3s ease;position:relative;overflow:hidden;}' +
        '.oc-card::before{content:"";position:absolute;inset:0;background:linear-gradient(135deg,rgba(168,85,247,.03),transparent);opacity:0;transition:opacity .3s;pointer-events:none;}' +
        '.oc-card:hover{border-color:rgba(168,85,247,.2);transform:translateY(-2px);box-shadow:0 8px 32px rgba(0,0,0,.3),0 0 20px rgba(168,85,247,.06);}' +
        '.oc-card:hover::before{opacity:1;}' +
        /* stat cards */
        '.oc-stat{padding:18px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;transition:all .3s ease;cursor:default;}' +
        '.oc-stat:hover{border-color:rgba(168,85,247,.25);transform:translateY(-3px);box-shadow:0 8px 24px rgba(0,0,0,.3);}' +
        '.oc-stat:hover .oc-stat-val{filter:drop-shadow(0 0 10px currentColor);}' +
        '.oc-stat-val{transition:filter .3s;}' +
        /* table rows */
        '.oc-row{border-bottom:1px solid #1b122b;transition:background .2s;}' +
        '.oc-row:hover{background:rgba(168,85,247,.04);}' +
        /* header buttons */
        '.oc-hdr-btn{padding:10px 14px;border-radius:12px;background:#0f0a1c;border:1px solid #231738;color:#fff;cursor:pointer;display:flex;align-items:center;gap:6px;transition:all .25s ease;font-size:12px;font-weight:600;}' +
        '.oc-hdr-btn:hover{border-color:#a855f7;box-shadow:0 0 16px rgba(168,85,247,.2);transform:translateY(-1px);}' +
        '.oc-hdr-btn:hover svg{filter:drop-shadow(0 0 4px rgba(168,85,247,.5));}' +
        '.oc-hdr-btn svg{transition:filter .2s;}' +
        /* profile cards */
        '.oc-prof{padding:16px;border-radius:14px;background:#0a0714;border:1px solid #1b122b;cursor:pointer;transition:all .3s ease;}' +
        '.oc-prof:hover{border-color:rgba(168,85,247,.3);transform:translateY(-3px);box-shadow:0 8px 28px rgba(0,0,0,.3),0 0 16px rgba(168,85,247,.08);}' +
        '.oc-prof:hover .oc-prof-avatar{box-shadow:0 0 16px rgba(168,85,247,.4);transform:scale(1.08);}' +
        '.oc-prof-avatar{transition:all .3s;}' +
        /* toast notification */
        '.oc-toast{display:flex;align-items:center;gap:10px;padding:12px 18px;border-radius:14px;background:#0e0a1a;border:1px solid #2a1745;font-size:12px;font-weight:600;color:#e2e8f0;box-shadow:0 12px 32px rgba(0,0,0,.5);opacity:0;transform:translateX(120%);transition:all .4s cubic-bezier(.22,1,.36,1);}' +
        '.oc-toast.oc-show{opacity:1;transform:translateX(0);}' +
        '.oc-toast.oc-hide{opacity:0;transform:translateX(120%);}' +
        /* logo pulse */
        '.oc-logo-box{transition:all .3s;}' +
        '.oc-logo-box:hover{box-shadow:0 0 20px rgba(168,85,247,.3);transform:scale(1.05);}' +
        /* signout */
        '.oc-signout{transition:all .2s;}' +
        '.oc-signout:hover{color:#ef4444 !important;transform:scale(1.1);}' +
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
        '#oc-dash-content [class*="text-green"],[style*="color:#22c55e"]{transition:filter .2s;}' +
        '</style>' +

        '<div id="oc-dash-glow"></div>' +
        '<div style="display:flex;min-height:100vh;background:#050508;color:#fff;font-family:Inter,ui-sans-serif,system-ui,sans-serif;width:100%;position:relative;z-index:2;">' +

        '<aside style="width:248px;flex-shrink:0;border-right:1px solid #1a1325;background:#07050d;padding:16px;display:flex;flex-direction:column;justify-content:space-between;min-height:100vh;position:sticky;top:0;">' +
            '<div>' +
                '<div style="display:flex;align-items:center;gap:12px;padding:8px;margin-bottom:16px;">' +
                    '<div class="oc-logo-box" style="width:42px;height:42px;border-radius:12px;background:#130f1e;border:1px solid #2a1b40;display:flex;align-items:center;justify-content:center;font-weight:800;color:#c084fc;font-size:15px;">(*&gt;</div>' +
                    '<div><div style="font-weight:700;color:#fff;font-size:16px;line-height:1.2;">Ocean</div><div style="font-size:12px;color:#808098;">anticheat.ac</div></div>' +
                '</div>' +
                '<nav style="display:flex;flex-direction:column;gap:2px;">' + navHtml + '</nav>' +
            '</div>' +
            '<div style="display:flex;align-items:center;justify-content:space-between;padding:10px 12px;border-radius:14px;background:#0e0918;border:1px solid #1e152e;">' +
                '<div style="display:flex;align-items:center;gap:10px;">' +
                    '<div class="oc-prof-avatar" style="width:36px;height:36px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:700;font-size:14px;color:#fff;">' + (user.username || 'U').charAt(0).toUpperCase() + '</div>' +
                    '<div><div style="font-weight:600;color:#fff;font-size:13px;">' + (user.username || 'User') + '</div><div style="font-size:11px;color:#808098;">Customer</div></div>' +
                '</div>' +
                '<button class="oc-signout" data-oc-signout style="padding:6px;background:none;border:none;color:#808098;cursor:pointer;border-radius:6px;">' + iconTrash() + '</button>' +
            '</div>' +
        '</aside>' +

        '<main style="flex:1;padding:32px;overflow-y:auto;min-height:100vh;">' +
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
            var color = type === 'error' ? '#ef4444' : type === 'warn' ? '#f59e0b' : '#22c55e';
            var el = document.createElement('div');
            el.className = 'oc-toast';
            el.style.borderColor = color + '66';
            el.style.color = color;
            el.innerHTML = '<span style="width:8px;height:8px;border-radius:50%;background:' + color + ';display:inline-block;"></span>' + msg;
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
                return '<div style="display:flex;justify-content:space-between;padding:8px 0;border-bottom:1px solid #1b122b;"><span style="font-size:12px;color:#e2e8f0;">' + d.name + '</span><span style="font-size:12px;color:#ef4444;">' + d.count + 'Ă—</span></div>';
            }).join('');
            var ov = document.createElement('div');
            ov.id = 'oc-profile-overlay';
            ov.style.cssText = 'position:fixed;inset:0;background:rgba(5,5,8,.6);z-index:2147483600;display:flex;align-items:center;justify-content:center;backdrop-filter:blur(6px);';
            ov.innerHTML = '<div style="width:min(680px,92vw);max-height:86vh;overflow:auto;background:#0a0714;border:1px solid #2a1745;border-radius:18px;padding:24px;box-shadow:0 30px 80px rgba(0,0,0,.6);">' +
                '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">' +
                    '<div style="display:flex;align-items:center;gap:14px;">' +
                        '<div style="width:52px;height:52px;border-radius:50%;background:linear-gradient(135deg,#a855f7,#7c3aed);display:flex;align-items:center;justify-content:center;font-weight:800;color:#fff;font-size:18px;">' + (found.username.charAt(0).toUpperCase()) + '</div>' +
                        '<div><div style="font-size:18px;font-weight:800;color:#fff;">' + found.username + '</div><div style="font-size:12px;color:#94a3b8;">' + found.pcName + '</div></div>' +
                    '</div>' +
                    '<button data-oc-close-profile style="background:none;border:none;color:#94a3b8;font-size:22px;cursor:pointer;">&times;</button>' +
                '</div>' +
                '<div style="display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-bottom:16px;">' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#fff;">' + found.scanCount + '</div><div style="font-size:11px;color:#808098;">Scans</div></div>' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#ef4444;">' + dets.length + '</div><div style="font-size:11px;color:#808098;">Detections</div></div>' +
                    '<div style="padding:14px;border-radius:12px;background:#120c22;border:1px solid #1f1436;text-align:center;"><div style="font-size:22px;font-weight:800;color:#a855f7;">' + found.games.length + '</div><div style="font-size:11px;color:#808098;">Games</div></div>' +
                '</div>' +
                '<div style="font-size:12px;color:#94a3b8;margin-bottom:6px;">Detected cheats</div>' +
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
            fetch('/api/pins', { credentials: 'same-origin' }).then(function(r) { return r.ok ? r.json() : []; }).catch(function() { return []; })
        ]).then(function(results) {
            var me = results[0];
            var st = results[1];
            var sc = Array.isArray(results[2]) ? results[2] : [];
            var pi = Array.isArray(results[3]) ? results[3] : [];
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
            }

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
        initParticles();
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