/* =====================================================================
   MOLTEN METAL — vanilla WebGL2 (no React, no ogl, no build step)
   ---------------------------------------------------------------------
   Faithful port of the React/ogl MoltenMetal component to a plain
   script so it can drop into this static site + the WebView2 embedded
   runtime:

     OceanMolten.mount(el, { ...options })     // returns an instance
     OceanMolten.auto(document)                // mounts [data-molten]

   Declarative usage:
     <div data-molten data-preset="hero"></div>
     <div data-molten='{"speed":0.5,"glow":2}'></div>

   Default palette is black + purple (deep violet -> violet -> white-hot
   core). No pink, nothing neon-bright: the shader's alpha is the
   intensity, so it composites as purple LIGHT over the black page.
   ===================================================================== */
(function (global) {
  'use strict';

  var VERTEX = '#version 300 es\nin vec2 position;\nvoid main() {\n  gl_Position = vec4(position, 0.0, 1.0);\n}\n';

  var FRAGMENT = [
    '#version 300 es',
    'precision highp float;',
    'uniform vec2 iResolution;',
    'uniform float iTime;',
    'uniform float uSpeed;',
    'uniform float uScale;',
    'uniform float uDetail;',
    'uniform float uGlow;',
    'uniform float uCoreSize;',
    'uniform float uSwirl;',
    'uniform float uFold;',
    'uniform float uBlackPoint;',
    'uniform float uBrightness;',
    'uniform float uColorMode;',
    'uniform float uGrain;',
    'uniform float uGrainIntensity;',
    'uniform float uOpacity;',
    'uniform vec2 uMouse;',
    'uniform float uMouseStrength;',
    'uniform bool uEnableMouse;',
    'uniform vec3 uColor1;',
    'uniform vec3 uColor2;',
    'uniform vec3 uColor3;',
    'uniform vec3 uBackgroundColor;',
    'uniform bool uLightMode;',
    'out vec4 fragColor;',
    '',
    'float hash(vec2 p) {',
    '  return fract(sin(dot(p, vec2(12.9898, 78.233))) * 43758.5453);',
    '}',
    '',
    'void main() {',
    '  float time = iTime * uSpeed;',
    '  vec2 p = uScale * ((gl_FragCoord.xy - 0.5 * iResolution.xy) / iResolution.y) - 0.5;',
    '',
    '  vec2 drift = vec2(0.0);',
    '  if (uEnableMouse) {',
    '    drift = (uMouse - 0.5) * uMouseStrength * 2.0;',
    '  }',
    '  p += drift;',
    '',
    '  vec2 i = p;',
    '  float c = 0.0;',
    '  float r = length(p + vec2(sin(time), sin(time * 0.3 + 5.0)) * 0.5);',
    '  float d = length(p);',
    '  float rot = d + time + p.x * uSwirl;',
    '',
    '  float cosRot = cos(rot);',
    '  mat2 warp = mat2(cos(rot - sin(time / 5.0)), sin(rot), -sin(cosRot - time), cosRot) * uFold;',
    '  float glowCore = uGlow * uCoreSize;',
    '',
    '  for (float n = 0.0; n < 8.0; n++) {',
    '    if (n >= uDetail) break;',
    '    p *= warp;',
    '    float t = r - time / (n + 3.0);',
    '    i -= p + vec2(cos(t - i.x - r) + sin(t + i.y), sin(t - i.y) + cos(t + i.x) + r);',
    '    c += glowCore / length(vec2(sin(i.x + t), cos(i.y + t)));',
    '  }',
    '',
    '  c /= 6.0;',
    '',
    '  float intensity = max(c - uBlackPoint, 0.0) * uBrightness;',
    '  float g = clamp(intensity, 0.0, 1.0);',
    '',
    '  float mid = 0.5;',
    '  if (uColorMode > 1.5) {',
    '    mid = 0.65;',
    '  } else if (uColorMode > 0.5) {',
    '    mid = 0.35;',
    '  }',
    '',
    '  vec3 col = mix(uColor1, uColor2, smoothstep(0.0, mid, g));',
    '  col = mix(col, uColor3, smoothstep(mid, 1.0, g));',
    '',
    '  float a = g;',
    '  if (uGrain > 0.5) {',
    '    float gr = hash(gl_FragCoord.xy + iTime);',
    '    a += (gr - 0.5) * uGrainIntensity;',
    '  }',
    '  a = clamp(a, 0.0, 1.0) * uOpacity;',
    '  if (uLightMode) {',
    '    float signal = 1.0 - exp(-max(c, 0.0) * 6.5);',
    '    float body = smoothstep(0.075, 0.68, signal);',
    '    float ridge = smoothstep(0.42, 0.92, signal);',
    '',
    '    vec3 lightCol = mix(uColor1, uColor2, smoothstep(0.08, 0.52, signal));',
    '    lightCol = mix(lightCol, uColor3, smoothstep(0.52, 0.96, signal));',
    '    lightCol = mix(lightCol, lightCol * 0.72, ridge * 0.24);',
    '',
    '    float coverage = body * mix(0.2, 0.86, signal) * uOpacity;',
    '    if (uGrain > 0.5) {',
    '      float gr = hash(gl_FragCoord.xy + iTime);',
    '      coverage += (gr - 0.5) * uGrainIntensity * body * 0.16;',
    '    }',
    '    fragColor = vec4(mix(uBackgroundColor, lightCol, clamp(coverage, 0.0, 0.92)), 1.0);',
    '  } else {',
    '    fragColor = vec4(col * a, a);',
    '  }',
    '}',
    ''
  ].join('\n');

  /* ---------------- palettes (black + purple only) ---------------- */
  var PALETTES = {
    molten: { color1: '#4c1d95', color2: '#a855f7', color3: '#ede9fe', background: '#000000' },
    violet: { color1: '#3b0764', color2: '#9333ea', color3: '#e9d5ff', background: '#000000' },
    /* `ember` and `frost` used to be orange and blue. The brief is one accent
       family on black, so both are now deep purple variants — the names stay
       so existing preset references keep working. */
    ember: { color1: '#3b0764', color2: '#7c3aed', color3: '#ddd6fe', background: '#000000' },
    frost: { color1: '#2e1065', color2: '#6d28d9', color3: '#c4b5fd', background: '#000000' }
  };

  var PRESETS = {
    page: { speed: 0.2, scale: 2.4, detail: 3, glow: 1.15, coreSize: 0.1, swirl: 0.7, fold: -0.18, blackPoint: 0.07, brightness: 1.15, grain: true, grainIntensity: 0.045, mouseInteraction: true, mouseStrength: 0.18, opacity: 0.85, palette: 'molten' },
    hero: { speed: 0.3, scale: 3.2, detail: 3, glow: 1.5, coreSize: 0.1, swirl: 1, fold: -0.2, blackPoint: 0.06, brightness: 1.25, grain: true, grainIntensity: 0.05, mouseInteraction: true, mouseStrength: 0.3, opacity: 1, palette: 'molten' },
    card: { speed: 0.32, scale: 3.6, detail: 3, glow: 1.5, coreSize: 0.1, swirl: 1, fold: -0.2, blackPoint: 0.05, brightness: 1.3, grain: true, grainIntensity: 0.05, mouseInteraction: true, mouseStrength: 0.3, opacity: 1, palette: 'molten' },
    scanner: { speed: 0.55, scale: 3.4, detail: 4, glow: 1.9, coreSize: 0.11, swirl: 1.3, fold: -0.22, blackPoint: 0.045, brightness: 1.35, grain: true, grainIntensity: 0.06, mouseInteraction: true, mouseStrength: 0.4, opacity: 1, palette: 'violet' },
    accent: { speed: 0.4, scale: 5, detail: 3, glow: 2.1, coreSize: 0.09, swirl: 1.6, fold: -0.24, blackPoint: 0.03, brightness: 1.45, grain: false, grainIntensity: 0.04, mouseInteraction: false, mouseStrength: 0.2, opacity: 0.9, palette: 'molten' }
  };

  var DEFAULTS = {
    color1: null,
    color2: null,
    color3: null,
    backgroundColor: '#000000',
    palette: 'molten',
    speed: 0.35,
    scale: 4,
    detail: 3,
    glow: 1.6,
    coreSize: 0.1,
    swirl: 1,
    fold: -0.2,
    blackPoint: 0.05,
    brightness: 1.3,
    colorMode: 'molten',
    grain: true,
    grainIntensity: 0.05,
    mouseInteraction: true,
    mouseStrength: 0.3,
    opacity: 1,
    lightMode: false,
    blur: 0
  };

  function hexToRgb(hex) {
    var result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(String(hex || ''));
    if (!result) return [1, 1, 1];
    return [
      parseInt(result[1], 16) / 255,
      parseInt(result[2], 16) / 255,
      parseInt(result[3], 16) / 255
    ];
  }

  function colorModeToFloat(mode) {
    return mode === 'ember' ? 1 : mode === 'frost' ? 2 : 0;
  }

  function merge(target) {
    for (var i = 1; i < arguments.length; i++) {
      var src = arguments[i];
      if (!src) continue;
      for (var k in src) {
        if (Object.prototype.hasOwnProperty.call(src, k) && src[k] !== undefined && src[k] !== null) {
          target[k] = src[k];
        }
      }
    }
    return target;
  }

  function resolveOptions(raw) {
    raw = raw || {};
    var preset = (raw.preset && PRESETS[raw.preset]) ? PRESETS[raw.preset] : null;
    var o = merge({}, DEFAULTS, preset, raw);
    var pal = PALETTES[o.palette] || PALETTES.molten;
    if (!raw.color1) o.color1 = pal.color1;
    if (!raw.color2) o.color2 = pal.color2;
    if (!raw.color3) o.color3 = pal.color3;
    if (!raw.backgroundColor) o.backgroundColor = pal.background;
    return o;
  }

  function readDataset(el) {
    var out = {};
    var raw = el.getAttribute('data-molten');
    if (raw) {
      if (raw.charAt(0) === '{') {
        try {
          out = JSON.parse(raw);
        } catch (e) { /* ignore malformed json */ }
      } else {
        out.preset = raw;
      }
    }
    var map = {
      'data-preset': 'preset',
      'data-palette': 'palette',
      'data-speed': 'speed',
      'data-scale': 'scale',
      'data-detail': 'detail',
      'data-glow': 'glow',
      'data-core': 'coreSize',
      'data-swirl': 'swirl',
      'data-fold': 'fold',
      'data-blackpoint': 'blackPoint',
      'data-brightness': 'brightness',
      'data-colormode': 'colorMode',
      'data-grain': 'grain',
      'data-grainintensity': 'grainIntensity',
      'data-mousestrength': 'mouseStrength',
      'data-mouse': 'mouseInteraction',
      'data-opacity': 'opacity',
      'data-color1': 'color1',
      'data-color2': 'color2',
      'data-color3': 'color3',
      'data-blur': 'blur'
    };
    for (var attr in map) {
      if (!el.hasAttribute(attr)) continue;
      var val = el.getAttribute(attr);
      var key = map[attr];
      if (val === 'true' || val === 'false') out[key] = val === 'true';
      else if (val !== '' && !isNaN(parseFloat(val)) && /^[-\d.]+$/.test(val)) out[key] = parseFloat(val);
      else out[key] = val;
    }
    return out;
  }

  var instances = new WeakMap();

  function compile(gl, type, src) {
    var shader = gl.createShader(type);
    gl.shaderSource(shader, src);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
      if (global.console && console.warn) console.warn('[molten] shader error', gl.getShaderInfoLog(shader));
      gl.deleteShader(shader);
      return null;
    }
    return shader;
  }

  function Instance(container, rawOptions) {
    this.container = container;
    this.options = resolveOptions(rawOptions);
    this.disposed = false;
    this.raf = 0;
    this.visible = true;
    this.pageVisible = !document.hidden;
    this.targetMouse = [0.5, 0.5];
    this.currentMouse = [0.5, 0.5];

    var canvas = document.createElement('canvas');
    canvas.className = 'molten-metal-canvas';
    canvas.setAttribute('aria-hidden', 'true');
    container.appendChild(canvas);
    this.canvas = canvas;

    var attrs = {
      alpha: true,
      premultipliedAlpha: true,
      antialias: false,
      depth: false,
      stencil: false,
      powerPreference: 'low-power',
      preserveDrawingBuffer: false
    };
    var gl = canvas.getContext('webgl2', attrs);
    if (!gl) {
      // graceful: leave the container as a flat black surface
      container.classList.add('molten-metal-unsupported');
      this.gl = null;
      return;
    }
    this.gl = gl;

    var vs = compile(gl, gl.VERTEX_SHADER, VERTEX);
    var fs = compile(gl, gl.FRAGMENT_SHADER, FRAGMENT);
    if (!vs || !fs) {
      container.classList.add('molten-metal-unsupported');
      this.gl = null;
      return;
    }
    var program = gl.createProgram();
    gl.attachShader(program, vs);
    gl.attachShader(program, fs);
    gl.linkProgram(program);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) {
      if (global.console && console.warn) console.warn('[molten] link error', gl.getProgramInfoLog(program));
      container.classList.add('molten-metal-unsupported');
      this.gl = null;
      return;
    }
    gl.deleteShader(vs);
    gl.deleteShader(fs);
    this.program = program;

    // fullscreen triangle
    var buffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 3, -1, -1, 3]), gl.STATIC_DRAW);
    var loc = gl.getAttribLocation(program, 'position');
    gl.enableVertexAttribArray(loc);
    gl.vertexAttribPointer(loc, 2, gl.FLOAT, false, 0, 0);

    var names = [
      'iResolution', 'iTime', 'uSpeed', 'uScale', 'uDetail', 'uGlow', 'uCoreSize', 'uSwirl',
      'uFold', 'uBlackPoint', 'uBrightness', 'uColorMode', 'uGrain', 'uGrainIntensity',
      'uOpacity', 'uMouse', 'uMouseStrength', 'uEnableMouse', 'uColor1', 'uColor2',
      'uColor3', 'uBackgroundColor', 'uLightMode'
    ];
    this.u = {};
    for (var i = 0; i < names.length; i++) this.u[names[i]] = gl.getUniformLocation(program, names[i]);

    gl.clearColor(0, 0, 0, 0);
    this.apply(true);
    this.bind();
  }

  Instance.prototype.apply = function (force) {
    var gl = this.gl;
    if (!gl) return;
    var o = this.options;
    var u = this.u;
    gl.useProgram(this.program);
    gl.uniform1f(u.iTime, 0);
    gl.uniform1f(u.uSpeed, o.speed);
    gl.uniform1f(u.uScale, o.scale);
    gl.uniform1f(u.uDetail, o.detail);
    gl.uniform1f(u.uGlow, o.glow);
    gl.uniform1f(u.uCoreSize, Math.max(o.coreSize, 0.001));
    gl.uniform1f(u.uSwirl, o.swirl);
    gl.uniform1f(u.uFold, o.fold);
    gl.uniform1f(u.uBlackPoint, o.blackPoint);
    gl.uniform1f(u.uBrightness, o.brightness);
    gl.uniform1f(u.uColorMode, colorModeToFloat(o.colorMode));
    gl.uniform1f(u.uGrain, o.grain ? 1 : 0);
    gl.uniform1f(u.uGrainIntensity, o.grainIntensity);
    gl.uniform1f(u.uOpacity, o.opacity);
    gl.uniform1f(u.uMouseStrength, o.mouseStrength);
    gl.uniform1i(u.uEnableMouse, o.mouseInteraction ? 1 : 0);
    gl.uniform1i(u.uLightMode, o.lightMode ? 1 : 0);
    gl.uniform2fv(u.uMouse, new Float32Array(this.currentMouse));
    gl.uniform3fv(u.uColor1, new Float32Array(hexToRgb(o.color1)));
    gl.uniform3fv(u.uColor2, new Float32Array(hexToRgb(o.color2)));
    gl.uniform3fv(u.uColor3, new Float32Array(hexToRgb(o.color3)));
    gl.uniform3fv(u.uBackgroundColor, new Float32Array(hexToRgb(o.backgroundColor)));
    if (this.container && typeof o.blur === 'number' && o.blur > 0) {
      this.canvas.style.filter = 'blur(' + o.blur + 'px)';
    }
  };

  Instance.prototype.update = function (next) {
    if (this.disposed) return this;
    this.options = merge(this.options, resolveOptions(merge({}, this.options, next || {})));
    this.apply();
    return this;
  };

  Instance.prototype.size = function () {
    var gl = this.gl;
    if (!gl) return;
    var rect = this.container.getBoundingClientRect();
    var dpr = Math.min(global.devicePixelRatio || 1, this.options.maxDpr || 1.5);
    var w = Math.max(1, Math.floor((rect.width || 1) * dpr));
    var h = Math.max(1, Math.floor((rect.height || 1) * dpr));
    if (this.canvas.width !== w || this.canvas.height !== h) {
      this.canvas.width = w;
      this.canvas.height = h;
    }
    this.canvas.style.width = '100%';
    this.canvas.style.height = '100%';
    gl.viewport(0, 0, w, h);
    gl.useProgram(this.program);
    gl.uniform2fv(this.u.iResolution, new Float32Array([w, h]));
    this.draw(0);
  };

  Instance.prototype.draw = function (t) {
    var gl = this.gl;
    if (!gl) return;
    gl.useProgram(this.program);
    gl.uniform1f(this.u.iTime, t);
    gl.drawArrays(gl.TRIANGLES, 0, 3);
  };

  Instance.prototype.loop = function (t) {
    if (this.disposed || !this.gl) return;
    if (!this.start) this.start = t;
    var time = (t - this.start) * 0.001;
    var ease = 0.05;
    this.currentMouse[0] += ease * (this.targetMouse[0] - this.currentMouse[0]);
    this.currentMouse[1] += ease * (this.targetMouse[1] - this.currentMouse[1]);
    var gl = this.gl;
    gl.useProgram(this.program);
    gl.uniform2fv(this.u.uMouse, new Float32Array(this.currentMouse));
    this.draw(time);
    this.raf = global.requestAnimationFrame(this.loop.bind(this));
  };

  Instance.prototype.start_ = function () {
    if (this.raf || this.disposed || !this.gl) return;
    this.start = 0;
    this.raf = global.requestAnimationFrame(this.loop.bind(this));
  };

  Instance.prototype.stop = function () {
    if (this.raf) global.cancelAnimationFrame(this.raf);
    this.raf = 0;
  };

  Instance.prototype.bind = function () {
    var self = this;
    var container = this.container;

    this.onResize = function () { self.size(); };
    this.size();
    if (global.ResizeObserver) {
      this.ro = new ResizeObserver(this.onResize);
      this.ro.observe(container);
    } else {
      global.addEventListener('resize', this.onResize, { passive: true });
    }

    this.onMove = function (e) {
      var rect = container.getBoundingClientRect();
      if (!rect.width || !rect.height) return;
      self.targetMouse[0] = (e.clientX - rect.left) / rect.width;
      self.targetMouse[1] = 1 - (e.clientY - rect.top) / rect.height;
    };
    this.onLeave = function () {
      self.targetMouse[0] = 0.5;
      self.targetMouse[1] = 0.5;
    };
    container.addEventListener('mousemove', this.onMove, { passive: true });
    container.addEventListener('mouseleave', this.onLeave, { passive: true });

    this.onVisibility = function () {
      self.pageVisible = !document.hidden;
      self.pageVisible && self.visible ? self.start_() : self.stop();
    };
    document.addEventListener('visibilitychange', this.onVisibility);

    if (global.IntersectionObserver) {
      this.io = new IntersectionObserver(function (entries) {
        self.visible = !!(entries[0] && entries[0].isIntersecting);
        self.visible && self.pageVisible ? self.start_() : self.stop();
      }, { threshold: 0 });
      this.io.observe(container);
    }

    if (!this.options.pauseOffscreen && !global.IntersectionObserver) this.start_();
  };

  Instance.prototype.destroy = function () {
    if (this.disposed) return;
    this.disposed = true;
    this.stop();
    if (this.ro && this.ro.disconnect) this.ro.disconnect();
    if (this.io && this.io.disconnect) this.io.disconnect();
    if (this.container) {
      this.container.removeEventListener('mousemove', this.onMove);
      this.container.removeEventListener('mouseleave', this.onLeave);
    }
    document.removeEventListener('visibilitychange', this.onVisibility);
    global.removeEventListener('resize', this.onResize);
    if (this.gl) {
      var lose = this.gl.getExtension('WEBGL_lose_context');
      if (lose) lose.loseContext();
    }
    if (this.canvas && this.canvas.parentNode === this.container) {
      this.container.removeChild(this.canvas);
    }
    instances.delete(this.container);
  };

  function mount(container, options) {
    if (!container) return null;
    if (instances.has(container)) return instances.get(container);
    var inst = new Instance(container, options);
    instances.set(container, inst);
    return inst;
  }

  function auto(root) {
    var scope = root || document;
    var nodes = scope.querySelectorAll('[data-molten]');
    var out = [];
    for (var i = 0; i < nodes.length; i++) out.push(mount(nodes[i], readDataset(nodes[i])));
    return out;
  }

  global.OceanMolten = {
    mount: mount,
    auto: auto,
    readDataset: readDataset,
    presets: PRESETS,
    palettes: PALETTES,
    defaults: DEFAULTS
  };
  if (typeof module !== 'undefined' && module.exports) module.exports = global.OceanMolten;
})(typeof window !== 'undefined' ? window : this);
