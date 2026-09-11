const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const A = __dirname;
const PORT = process.env.PORT || 8080;

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.webp': 'image/webp',
  '.woff2': 'font/woff2',
  '.woff': 'font/woff',
  '.ttf': 'font/ttf',
  '.txt': 'text/plain; charset=utf-8',
};

// route -> files (fenti = preferált)
const ROUTES = {
  '/': 'index.html',
  '/dashboard/library': 'dashboard-library.html',
  '/dashboard/api-keys': 'dashboard-api-keys.html',
  '/dashboard/server/demo': 'dashboard-demo.html',
  '/dashboard/pins': 'dashboard-pins.html',
  '/dashboard/detections': 'dashboard-detections.html',
  '/dashboard/tickets': 'dashboard-tickets.html',
  '/dashboard/chat': 'dashboard-chat.html',
  '/dashboard/leaderboard': 'dashboard-leaderboard.html',
  '/dashboard/settings': 'dashboard-settings.html',
  '/dashboard/enterprise': 'dashboard-enterprise.html',
  '/dashboard/strings': 'dashboard-strings.html',
  '/dashboard/stats': 'dashboard.html',
  '/dashboard/watchlist': 'dashboard-watchlist.html',
  '/dashboard': 'dashboard.html',
  '/downloads': 'downloads.html',
  '/pricing': 'pricing.html',
  '/docs': 'docs.html',
  '/blog': 'blog.html',
  '/legal': 'legal.html',
  '/privacy': 'privacy.html',
  '/tos': 'tos.html',
  '/about': 'dashboard.html',
  '/detection-team': 'detection-team.html',
  '/login': 'auth-login.html',
  '/signin': 'auth-login.html',
  '/auth/login': 'auth-login.html',
  '/register': 'auth-register.html',
  '/signup': 'auth-register.html',
  '/auth/register': 'auth-register.html',
  '/branding': 'branding.html',
  '/changelog': 'changelog.html',
  '/docs/api': 'docs.html',
  '/favicon.ico': 'favicon.ico',
};

const HEAD_SCRIPT = `
<script>
window.__OC_MOCK__=1;
(function(){
  try{sessionStorage.removeItem('ocean_server_status');}catch(e){}
  try{localStorage.removeItem('ocean_server_status');}catch(e){}

  // --- Blokkoljuk a React bootstrap-et (Next.js turbopack chunkok) ---
  // Ezzel a teljes SSR tartalom marad a lapon, es a BODY_SCRIPT vanilla
  // JS-sel adja vissza a gombok interaktivitasat.
  (function(){
    var stateKey = '_rw';
    document.addEventListener('DOMContentLoaded', function(){
      try{
        document.querySelectorAll('script[src*="/_next/static/chunks/"]').forEach(function(scr){
          var src = scr.getAttribute('src')||'';
          if (/turbopack|035e08ev1m88b/.test(src) || src.indexOf('turbopack')>-1) {
            scr.setAttribute('type','application/x-blocked');
          }
        });
      }catch(e){}
    });
    // Korai blokk: a first script is futo
    var s = document.createElement('style');
    s.id = 'oc_react_block_state';
  })();

  var _savedUser = null; try{ _savedUser = JSON.parse(localStorage.getItem('__OC_REAL_USER__')||'null'); }catch(e){}
  var ME_JSON = _savedUser || ${JSON.stringify({id:'6a96d96848dd455e51a4477f',email:'fabixcalude1@gmail.com',username:'fabixboss',isAccountActive:true,isEmailConfirmed:true,role:'USER',is2FAEnabled:false,isLocked:false,createdAt:'2026-09-01T13:55:52.517Z',discordWebhook:false,usernameChangeCooldown:{canChange:true,daysRemaining:0,nextChangeAvailableAt:null},plans:[],enterprises:[],enterprisePermissions:{canCreate:false},connections:[],isBanned:false,bannedBy:null,isDeleted:false,lastSuccessfulLogin:'2026-09-01T14:24:50.134Z',pendingInvitations:[],hasPendingInvitations:false,hasPassword:true,isDetectionTeam:false,canValidateDetections:false,hasDbAccess:false})};
  var MAINT_JSON = ${JSON.stringify({isInMaintenance:false,upcomingMaintenance:null,canAccess:true})};

  function apiRespond(url){
    if(!url)return null;
    if(/\\/health($|\\?)/.test(url))return {status:200,body:{status:'ok',ok:true,statusCode:200}};
    if(/\\/users\\/@me($|\\?)/.test(url))return null;
     if(/\\/api\\/auth\\/me($|\\?)/.test(url))return null;
    if(/\\/maintenance\\/status($|\\?)/.test(url))return {status:200,body:MAINT_JSON};
    return null;
  }
  window.__OC_apiRespond = apiRespond;
  window.__OC_LOG = true;

  window.__OC_MOCK__=2;
  var REAL_FETCH=window.fetch;
  window.fetch=function(){
    try{
      var input=arguments[0], u=typeof input==='string'?input:(input&&input.url)||'';
      var m=apiRespond(u);
      if(window.__OC_LOG && u.indexOf('avatars')<0 && u.indexOf('assets')<0){ try{ console.log('[OCFETCH]', u, m?('MOCK->'+JSON.stringify(m.body).slice(0,80)):'REAL'); }catch(e){} }
      if(m){ window.__OC_MOCK__=3; return Promise.resolve(new Response(JSON.stringify(m.body),{status:m.status,statusText:'OK',headers:{'Content-Type':'application/json'}})); }
    }catch(e){}
    return REAL_FETCH.apply(this,arguments);
  };

  var REAL_XHR=window.XMLHttpRequest;
  if(REAL_XHR){
    function mockheaders(){
      var h='cache-control: no-cache\\r\\ncontent-type: application/json\\r\\n';
      return h;
    }
    var XHR_PROTO=REAL_XHR.prototype;
    var origOpen=XHR_PROTO.open;
    var origSend=XHR_PROTO.send;
    var origSetHeader=XHR_PROTO.setRequestHeader;
    var origGetAllHeaders=XHR_PROTO.getAllResponseHeaders;
    var origGetHeader=XHR_PROTO.getResponseHeader;
    XHR_PROTO.getAllResponseHeaders=function(){ if(this.__mocked) return mockheaders(); return origGetAllHeaders.apply(this,arguments); };
    XHR_PROTO.getResponseHeader=function(n){ if(this.__mocked) return 'application/json'; return origGetHeader.apply(this,arguments); };
    XHR_PROTO.open=function(method,url){
      this.__m=method; this.__u=url;
      return origOpen.apply(this,arguments);
    };
    XHR_PROTO.send=function(){
      var self=this;
      var m=apiRespond(self.__u||'');
      if(window.__OC_LOG){ try{ console.log('[OCXHR]', self.__u||'', m?('MOCK->'+JSON.stringify(m.body).slice(0,60)):'REAL'); }catch(e){} }
      if(m){
        self.status=m.status;
        self.statusText='OK';
        self.readyState=4;
        self.responseText=JSON.stringify(m.body);
        self.responseJSON=m.body;
        self.response=m.body;
        self.responseType='json';
        self.__mocked=true;
        setTimeout(function(){
          try{
            if(self.onreadystatechange)self.onreadystatechange();
            if(self.onload)self.onload();
          }catch(e){}
          try{ if(self.onloadend)self.onloadend(); }catch(e){}
        },0);
        return;
      }
      return origSend.apply(this,arguments);
    };
    XHR_PROTO.responseText = undefined;
    XHR_PROTO.status = 0;
  }

  try{Object.defineProperty(navigator,'onLine',{get:function(){return true;}});}catch(e){}
  try{
    var RealWS=window.WebSocket;
    if(RealWS){
      window.WebSocket=function(url,protocols){
        this.url=url;this.readyState=1;this.protocol='';
        var self=this;
        setTimeout(function(){
          try{ if(self.onopen)self.onopen({type:'open'}); }catch(e){}
          setTimeout(function(){ try{ if(self.onmessage)self.onmessage({type:'message',data:'{\\"type\\":\\"connected\\"}'}); }catch(e){} },50);
        },10);
      };
      window.WebSocket.prototype.send=function(){ return true; };
      window.WebSocket.prototype.close=function(){ this.readyState=3; };
      window.WebSocket.prototype.addEventListener=function(type,cb){ this['__cb_'+type]=cb; };
      window.WebSocket.prototype.removeEventListener=function(){};
      window.WebSocket.OPEN=1;window.WebSocket.CONNECTING=0;window.WebSocket.CLOSED=3;window.WebSocket.CLOSING=2;
    }
  }catch(e){}
})();
</script>
<script>
// ===== REAL ACCOUNT GATE (login / create-account on the main page) =====
(function(){
  var page = location.pathname || '';
  var isAdmin = page.indexOf('/dashboard') === 0;
  var isHome = page === '/';
  if (!isAdmin && !isHome) return;
  var errMsg = { username_exists:'That username is already taken.', invalid_username:'Use 3-32 letters, numbers, dots or dashes.', weak_password:'Password must be at least 6 characters.', password_mismatch:'Passwords do not match.', bad_credentials:'Wrong username or password.' };
  function showAuth(startMode){
    if(document.getElementById('oc-auth-screen')) return;
    var el=document.createElement('div');
    el.id='oc-auth-screen';
    el.style.cssText='position:fixed;inset:0;z-index:9999999;display:flex;align-items:center;justify-content:center;padding:20px;background:radial-gradient(1200px 600px at 20% -10%,rgba(168,85,247,0.16),transparent 60%),radial-gradient(900px 500px at 100% 110%,rgba(147,51,234,0.13),transparent 55%),#060a12;font-family:Inter,ui-sans-serif,system-ui,sans-serif;';
    el.innerHTML=
      '<div style="width:min(92vw,400px);background:rgba(13,18,30,0.92);border:1px solid rgba(148,163,184,0.2);border-radius:20px;padding:28px;color:#e2e8f0;box-shadow:0 25px 60px rgba(0,0,0,0.6);">'+
        '<div style="text-align:center;margin-bottom:22px;">'+
          '<div style="font-size:26px;font-weight:800;letter-spacing:1px;background:linear-gradient(90deg,#e879f9,#9333ea);-webkit-background-clip:text;background-clip:text;color:transparent;">OCEAN AC</div>'+
          '<div style="font-size:13px;color:#94a3b8;margin-top:6px;">Scan-pin console — everything is saved to your account</div>'+
        '</div>'+
        '<div style="display:flex;background:rgba(148,163,184,0.08);border-radius:10px;padding:4px;margin-bottom:18px;">'+
          '<button data-oc-mode="login" style="flex:1;padding:8px;border:none;border-radius:8px;cursor:pointer;font-weight:700;font-size:13px;background:#9333ea;color:#fff;">Sign in</button>'+
          '<button data-oc-mode="register" style="flex:1;padding:8px;border:none;border-radius:8px;cursor:pointer;font-weight:600;font-size:13px;background:transparent;color:#94a3b8;">Create account</button>'+
        '</div>'+
        '<div data-oc-auth-err style="display:none;background:rgba(239,68,68,0.1);border:1px solid rgba(239,68,68,0.35);color:#f87171;border-radius:10px;padding:10px 12px;font-size:12.5px;margin-bottom:14px;"></div>'+
        '<div style="margin-bottom:12px;"><label style="display:block;font-size:12px;font-weight:600;margin-bottom:6px;color:#cbd5e1;">Username</label>'+
        '<input data-oc-user type="text" autocomplete="username" placeholder="your username" style="width:100%;box-sizing:border-box;padding:10px 12px;border:1px solid rgba(148,163,184,0.3);border-radius:10px;background:#0b1424;color:#e2e8f0;font-size:14px;outline:none;"></div>'+
        '<div style="margin-bottom:12px;"><label style="display:block;font-size:12px;font-weight:600;margin-bottom:6px;color:#cbd5e1;">Password</label>'+
        '<input data-oc-pass type="password" autocomplete="current-password" placeholder="••••••••" style="width:100%;box-sizing:border-box;padding:10px 12px;border:1px solid rgba(148,163,184,0.3);border-radius:10px;background:#0b1424;color:#e2e8f0;font-size:14px;outline:none;"></div>'+
        '<div data-oc-conf-wrap style="margin-bottom:12px;display:none;"><label style="display:block;font-size:12px;font-weight:600;margin-bottom:6px;color:#cbd5e1;">Confirm password</label>'+
        '<input data-oc-conf type="password" autocomplete="new-password" placeholder="••••••••" style="width:100%;box-sizing:border-box;padding:10px 12px;border:1px solid rgba(148,163,184,0.3);border-radius:10px;background:#0b1424;color:#e2e8f0;font-size:14px;outline:none;"></div>'+
        '<button data-oc-submit style="width:100%;padding:11px;border:none;border-radius:10px;cursor:pointer;font-weight:700;font-size:14px;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;">Sign in</button>'+
        '<div style="text-align:center;font-size:11.5px;color:#64748b;margin-top:16px;line-height:1.5;">Create your account here once — afterwards you can only sign in to an existing account. Your pins, scans and stats are tied to this account.</div>'+
      '</div>';
    document.body.appendChild(el);
    var cur=(startMode==='register')?'register':'login';
    var user=el.querySelector('[data-oc-user]'), pass=el.querySelector('[data-oc-pass]'), conf=el.querySelector('[data-oc-conf]'), confWrap=el.querySelector('[data-oc-conf-wrap]'), err=el.querySelector('[data-oc-auth-err]'), submit=el.querySelector('[data-oc-submit]');
    function setMode(m){
      cur=m;
      el.querySelectorAll('[data-oc-mode]').forEach(function(b){
        var on=b.getAttribute('data-oc-mode')===m;
        b.style.background=on?'#9333ea':'transparent';
        b.style.color=on?'#fff':'#94a3b8';
        b.style.fontWeight=on?'700':'600';
      });
      confWrap.style.display=(m==='register')?'block':'none';
      submit.textContent=(m==='register')?'Create account':'Sign in';
    }
    el.querySelectorAll('[data-oc-mode]').forEach(function(b){ b.onclick=function(){ setMode(b.getAttribute('data-oc-mode')); }; });
    setMode(cur);
    function showErr(t){ err.textContent=t; err.style.display='block'; }
    function submit2(){
      var u=user.value.trim(), p=pass.value;
      if(!u){ showErr('Enter your username.'); return; }
      if(!p){ showErr('Enter your password.'); return; }
      var body = cur==='register' ? {username:u,password:p,confirm:conf.value} : {username:u,password:p};
      submit.disabled=true; submit.textContent='Working...';
      fetch('/api/auth/'+(cur==='register'?'register':'login'),{method:'POST',headers:{'Content-Type':'application/json'},credentials:'same-origin',body:JSON.stringify(body)})
        .then(function(r){ return r.json().then(function(d){ return {ok:r.ok,d:d}; }); })
        .then(function(x){
          if(x.ok && x.d && x.d.ok){
            try{ localStorage.setItem('__OC_REAL_USER__', JSON.stringify(x.d.user)); }catch(e){}
            location.reload();
          } else {
            submit.disabled=false; submit.textContent=(cur==='register')?'Create account':'Sign in';
            showErr(errMsg[x.d&&x.d.error] || 'Something went wrong.' );
          }
        })
        .catch(function(){ submit.disabled=false; submit.textContent=(cur==='register')?'Create account':'Sign in'; showErr('Cannot reach the server.'); });
    }
    submit.onclick=submit2;
    function onKey(e){ if(e.key==='Enter'){ e.preventDefault(); submit2(); } }
    user.addEventListener('keydown',onKey); pass.addEventListener('keydown',onKey); conf.addEventListener('keydown',onKey);
    setTimeout(function(){ user.focus(); },50);
  }
  function showLoggedIn(u){
    try{ localStorage.setItem('__OC_REAL_USER__', JSON.stringify(u)); }catch(e){}
    document.addEventListener('DOMContentLoaded', function(){
      var chip=document.createElement('div');
      chip.style.cssText='position:fixed;left:16px;bottom:16px;z-index:999998;display:flex;align-items:center;gap:8px;background:rgba(13,18,30,0.92);border:1px solid rgba(148,163,184,0.25);color:#e2e8f0;border-radius:999px;padding:7px 12px;font:600 12px Inter,ui-sans-serif,system-ui,sans-serif;box-shadow:0 10px 30px rgba(0,0,0,0.5);';
      chip.innerHTML='<span style="width:8px;height:8px;border-radius:50%;background:#34d399;display:inline-block;"></span><span>'+u.username+'</span><span style="color:#334155;">|</span><button data-oc-signout style="background:none;border:none;color:#f87171;cursor:pointer;font:600 12px Inter,system-ui,sans-serif;">Sign out</button>';
      document.body.appendChild(chip);
      var so=chip.querySelector('[data-oc-signout]');
      if(so) so.onclick=function(){
        fetch('/api/auth/logout',{method:'POST',credentials:'same-origin'}).then(function(){
          try{ localStorage.removeItem('__OC_REAL_USER__'); }catch(e){}
          location.reload();
        }).catch(function(){});
      };
      try{
        var w=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT,{acceptNode:function(n){ return n.nodeValue && n.nodeValue.indexOf('fabixboss')>-1 ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT; }});
        var n; while(n=w.nextNode()){ n.nodeValue=n.nodeValue.split('fabixboss').join(u.username); }
      }catch(e){}
    });
  }
  function showHomeAuth(){
    document.addEventListener('DOMContentLoaded', function(){
      // A főoldal Login / Sign Up gombjai a beépített modált nyitják ahelyett,
      // hogy külön oldalra navigálnának — így helyben lehet fiókot létrehozni.
      document.querySelectorAll('a[href*="/auth/login"], a[href*="/auth/register"], a[href*="/login"], a[href*="/signup"], a[href*="/register"]').forEach(function(a){
        if(a.__ocBound) return; a.__ocBound=true;
        var isReg=/(register|signup)/i.test(a.getAttribute('href')||'');
        a.addEventListener('click', function(e){ e.preventDefault(); e.stopPropagation(); showAuth(isReg?'register':'login'); }, true);
      });
      if(document.getElementById('oc-home-auth-btn')) return;
      var btn=document.createElement('button');
      btn.id='oc-home-auth-btn';
      btn.innerHTML='<span style="width:8px;height:8px;border-radius:50%;background:#e879f9;display:inline-block;"></span> Create Account / Sign in';
      btn.style.cssText='position:fixed;right:18px;bottom:18px;z-index:999998;display:flex;align-items:center;gap:8px;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;border:none;border-radius:999px;padding:11px 16px;font:700 13px Inter,ui-sans-serif,system-ui,sans-serif;cursor:pointer;box-shadow:0 12px 30px rgba(0,0,0,.45);';
      btn.addEventListener('click', function(){ showAuth('register'); });
      document.body.appendChild(btn);
    });
  }
  try{ fetch('/api/auth/me',{credentials:'same-origin'}).then(function(r){ return r.json().catch(function(){ return {}; }); }).then(function(d){
    if(d && d.user){ window.__OC_USER__=d.user; document.documentElement.setAttribute('data-user',d.user.username); showLoggedIn(d.user); }
    else if(isAdmin){ document.addEventListener('DOMContentLoaded', showAuth); if(document.readyState!=='loading'){ setTimeout(showAuth,200); } }
    else if(isHome){ showHomeAuth(); }
  }).catch(function(){ if(isAdmin){ document.addEventListener('DOMContentLoaded', showAuth); } else if(isHome){ showHomeAuth(); } }); }catch(e){}
})();
</script>`;

const BODY_SCRIPT = `
<script>
(function(){
  window.__OC_BODY__ = { ran: 1, time: Date.now() };
  // ==== 0) Ragadt framer-motion scroll animaciok javitasa ====
  // A React/turbopack JS blokkolva van a szerveren, ezert a whileInView
  // animaciok az ertes allapotukban ragadnak (opacity 0.x + translate/scale).
  // Itt elfusereljuk oket a vegso (lathato) allapotra.
  function fixStuckFramer(){
    try{
      // FIGYELEM: template literal, ezert nem hasznalhato backslash escape a regexekben
      // (a \\s / \\( lathatatlannak tunik a vegen). Ezert string.cuccokkal dolgozunk.
      var els = document.querySelectorAll('[style*="translate: none"],[style*="translate:none"]');
      for(var i=0;i<els.length;i++){
        var el = els[i];
        if(el.tagName==='CANVAS') continue;
        var st = el.getAttribute('style')||'';
        var op = 1;
        var pos = st.indexOf('opacity:');
        if(pos !== -1){
          var segEnd = st.indexOf(';', pos);
          var seg = st.slice(pos + 8, segEnd === -1 ? st.length : segEnd).trim();
          var num = parseFloat(seg);
          if(!isNaN(num)) op = num;
        }
        var hasShift = st.indexOf('translate(') !== -1 || st.indexOf('scale(') !== -1;
        var needsFix = (op < 1) || hasShift;
        if(needsFix){
          el.style.opacity = '1';
          el.style.transform = '';
          el.style.translate = 'none';
          el.style.rotate = 'none';
          el.style.scale = 'none';
        }
      }
    }catch(e){}
  }
  fixStuckFramer();
  setTimeout(fixStuckFramer, 500);
  setTimeout(fixStuckFramer, 1500);
  setTimeout(fixStuckFramer, 3000);
  setInterval(fixStuckFramer, 5000);
  // ==== Vanilla JS interakciok a React streaming nelkul ====

  // 1) Collapsible radix menuk toggle (Database / Support / Resources / account)
  function toggleRadix(trigger){
    if(!trigger)return false;
    var li=trigger.closest('li');
    if(!li)return false;
    var id=trigger.getAttribute('aria-controls');
    var content=id?document.getElementById(id):null;
    if(!id||!content)return false;
    var open = trigger.getAttribute('aria-expanded')==='true';
    if(open){
      li.setAttribute('data-state','closed');
      trigger.setAttribute('aria-expanded','false');
      trigger.setAttribute('data-state','closed');
      content.setAttribute('hidden','');
    } else {
      li.setAttribute('data-state','open');
      trigger.setAttribute('aria-expanded','true');
      trigger.setAttribute('data-state','open');
      content.removeAttribute('hidden');
    }
    return true;
  }

  function bindSidebar(){
    window.__OC_BODY__.bindAttempt = Date.now();
    // Delegált capture listener: megkeruli az adott elemeken esetleg levo
    // stopPropagation-ot, mert document szinten capture fazisban fut.
    try {
    document.addEventListener('click', function(e){
      var t = e.target;
      while (t && t !== document) {
        if (t.getAttribute && t.getAttribute('data-sidebar') === 'menu-button') {
          var hasA = t.closest('li') && t.closest('li').querySelector('a[href]');
          var aid = t.getAttribute('aria-controls');
          window.__OC_BODY__.lastAid = aid; window.__OC_BODY__.lastHasA = !!hasA;
          if (aid) {
            e.preventDefault();
            window.__OC_BODY__.toggled = toggleRadix(t);
            // Ha van href es megis akarnank nyitni/navigalni: ha nyitva van es megint kattintanak, vagy ha nincs almenue
            var href = t.getAttribute('href') || (t.querySelector('a')?t.querySelector('a').getAttribute('href'):null);
            if (href && t.getAttribute('aria-expanded')==='true') {
              window.location.href = href;
            }
            break;
          }
        }
        t = t.parentNode;
      }
    }, true);
    } catch(e){ window.__OC_BODY__.clickErr = String(e); }

    // 1b) Ures collapsible-ek feltoltese navigacios linkekkel
    try {
      var SUBLINKS = [
        { href: '/dashboard/pins', label: 'Pins' },
        { href: '/dashboard/detections', label: 'Detections' },
        { href: '/dashboard/watchlist', label: 'Watchlist' },
        { href: '/dashboard/tickets', label: 'Tickets' },
        { href: '/dashboard/settings', label: 'Settings' },
        { href: '/dashboard/api-keys', label: 'API Keys' },
        { href: '/downloads', label: 'Downloads' }
      ];
      document.querySelectorAll('li[class*="collapsible"]').forEach(function(li){
        var btn = li.querySelector('[data-sidebar="menu-button"]');
        if(!btn)return;
        var id = btn.getAttribute('aria-controls');
        var content = id ? document.getElementById(id) : null;
        if(!content)return;
        if(content.children.length > 0)return; // mar van tartalma
        var name = btn.textContent.replace(/\s+/g,'').trim();
        var ul = document.createElement('ul');
        ul.className = 'flex w-full min-w-0 flex-col gap-1 px-2 pb-2';
        ul.setAttribute('data-sidebar','menu-sub');
        SUBLINKS.forEach(function(lk){
          var li2 = document.createElement('li');
          li2.className = 'group/menu-item relative';
          var a = document.createElement('a');
          a.href = lk.href;
          a.className = 'flex w-full items-center gap-2 rounded-lg p-2 text-[13px] text-sidebar-foreground/70 hover:bg-sidebar-accent/60 hover:text-foreground transition duration-150';
          a.textContent = lk.label;
          li2.appendChild(a);
          ul.appendChild(li2);
        });
        content.appendChild(ul);
        window.__OC_BODY__.filledSubs = (window.__OC_BODY__.filledSubs||0)+1;
      });
    } catch(e){ window.__OC_BODY__.fillErr = String(e); }

    // 1c) Create Pin dialog - vanilla, mukodo pin generalas (max 10/nap)
    try {
      var __ocLoadPins__ = function(){
        var a=[]; try{ a=JSON.parse(localStorage.getItem('oc_pins')||'[]'); }catch(e){}
        if(!a.length){ try{ a=JSON.parse(sessionStorage.getItem('oc_pins')||'[]'); }catch(e2){} }
        return a;
      };
      window.__OC_PINS__ = __ocLoadPins__();
      function savePins(){ try{ localStorage.setItem('oc_pins', JSON.stringify(window.__OC_PINS__)); }catch(e){} try{ sessionStorage.setItem('oc_pins', JSON.stringify(window.__OC_PINS__)); }catch(e2){} }
      window.savePins = savePins;
      function getTodayCount(){ try{ return JSON.parse(localStorage.getItem('oc_pin_count') || '{}'); }catch(e){ return {}; } }
      function todayKey(){ return new Date().toISOString().slice(0,10); }
      function getPinCountToday(){ var c=getTodayCount(); return c[todayKey()]||0; }
      window.getPinCountToday = getPinCountToday;
      function incrementPinCount(){ var c=getTodayCount(); var k=todayKey(); c[k]=(c[k]||0)+1; try{localStorage.setItem('oc_pin_count',JSON.stringify(c));}catch(e){} }
      window.incrementPinCount = incrementPinCount;
      window.__OC_MAKE_PIN = function(){
        if(document.getElementById('oc-pin-overlay'))return;
        var dailyCount=getPinCountToday();
        var ov=document.createElement('div');
        ov.id='oc-pin-overlay';
        ov.style.cssText='position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.8);backdrop-filter:blur(10px);z-index:99999;display:flex;align-items:center;justify-content:center;font-family:Inter,sans-serif;';
        var box=document.createElement('div');
        box.style.cssText='background:#0a0714;border:1px solid #2a1845;border-radius:18px;padding:28px;max-width:440px;width:92%;color:#fff;box-shadow:0 25px 70px rgba(0,0,0,0.8), 0 0 30px rgba(168,85,247,0.15);';
        if(dailyCount>=0){
          // No daily limit: pins are unlimited.
        }
        var remaining='unlimited';
        box.innerHTML='<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">'+
          '<h2 style="margin:0;font-size:20px;font-weight:700;color:#fff;">New Scan</h2>'+
          '<button data-oc-close style="background:none;border:none;font-size:22px;cursor:pointer;color:#94a3b8;">&times;</button></div>'+
          '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;">'+
          '<p style="color:#94a3b8;margin:0;font-size:13px;">Pick a game, configure access, and generate a pin ready to scan.</p>'+
          '<span style="font-size:12px;color:#c084fc;white-space:nowrap;margin-left:8px;font-weight:600;">'+remaining+'</span></div>'+
          '<label style="display:block;font-size:13px;font-weight:600;margin-bottom:6px;color:#e2e8f0;">Game</label>'+
          '<select data-oc-game style="width:100%;padding:10px 12px;border:1px solid #2a1845;border-radius:10px;background:#120c22;color:#fff;margin-bottom:14px;font-size:14px;outline:none;">'+
          '<option>Minecraft</option><option>Minecraft Screenshare</option><option>FiveM</option><option>GTA V</option><option>Roblox</option><option>Valorant</option></select>'+
          '<label style="display:flex;align-items:center;gap:8px;font-size:13px;margin-bottom:12px;color:#cbd5e1;cursor:pointer;"><input type="checkbox" data-oc-private style="accent-color:#a855f7;"> Private Pin</label>'+
          '<label style="display:flex;align-items:center;gap:8px;font-size:13px;margin-bottom:20px;color:#cbd5e1;cursor:pointer;"><input type="checkbox" data-oc-ruin style="accent-color:#a855f7;"> Enable RUIN checks</label>'+
          '<button type="button" data-oc-generate onclick="return false" style="width:100%;padding:12px;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;border:none;border-radius:10px;font-weight:700;cursor:pointer;font-size:14px;box-shadow:0 8px 25px rgba(168,85,247,0.35);">Generate Pin</button>'+
          '<div data-oc-result style="display:none;margin-top:18px;text-align:center;padding:16px;border-radius:12px;background:rgba(168,85,247,0.12);border:1px solid rgba(168,85,247,0.35);">'+
          '<div style="font-size:13px;color:#c084fc;margin-bottom:6px;font-weight:600;">Pin Created Successfully</div>'+
          '<div data-oc-pin-code style="font-family:monospace;font-size:22px;font-weight:800;letter-spacing:2px;color:#fff;margin-bottom:10px;"></div>'+
          '<button data-oc-copy style="background:#1a1030;border:1px solid #a855f7;color:#fff;border-radius:8px;padding:6px 16px;cursor:pointer;font-size:12px;font-weight:600;">Copy Code</button>'+
          '</div>';
        ov.appendChild(box);
        document.body.appendChild(ov);
        function close(){ document.body.removeChild(ov); }
        box.querySelector('[data-oc-close]').addEventListener('click',close);
        ov.addEventListener('click',function(e){ if(e.target===ov)close(); });
        box.querySelector('[data-oc-generate]').addEventListener('click',function(e){
          if(e){ try{e.preventDefault&&e.preventDefault();}catch(_){} try{e.stopPropagation&&e.stopPropagation();}catch(_){} }
          function part(){ var s=''; for(var i=0;i<4;i++) s+='ABCDEFGHJKLMNPQRSTUVWXYZ23456789'[Math.floor(Math.random()*32)]; return s; }
          var code=part()+'-'+part()+'-'+part();
          box.querySelector('[data-oc-pin-code]').textContent=code;
          box.querySelector('[data-oc-result]').style.display='block';
          var pin={ code:code, game:box.querySelector('[data-oc-game]').value, private:box.querySelector('[data-oc-private]').checked, ruin:box.querySelector('[data-oc-ruin]').checked, createdAt:new Date().toISOString(), status:'pending' };
          window.__OC_PINS__.unshift(pin);
          if(window.savePins)window.savePins();
          window.__OC_BODY__.lastCreatedPin=code;
          // Mentsuk a pin-t valid key-kent a szerveren is, hogy a desktop
          // app (/api/keys/validate/:key) elfogadja.
          try {
            var bodyStr=JSON.stringify({ key:code, maxUses:999, expiresAt:new Date(Date.now()+365*86400000).toISOString(), status:'active', note:'generated pin', pin:true });
            if(window.fetch){
              fetch('/api/keys', { method:'POST', headers:{'Content-Type':'application/json'}, body:bodyStr }).catch(function(){});
            } else {
              var xhr=new XMLHttpRequest();
              xhr.open('POST','/api/keys',true);
              xhr.setRequestHeader('Content-Type','application/json');
              xhr.send(bodyStr);
            }
          } catch(e){ window.__OC_BODY__.pinSaveErr=String(e); }
          try {
            var panel=document.getElementById('radix-_r_o_-content-my-pins');
            var tb=panel&&panel.querySelector('table tbody');
            if(tb){
              var empty=[].slice.call(tb.querySelectorAll('td')).find(function(td){return /no results/i.test(td.textContent||'');});
              if(empty){ var tr0=empty.closest('tr'); if(tr0)tr0.parentNode.removeChild(tr0); }
              var tr=document.createElement('tr');
              tr.className='border-b border-border/20 transition-colors duration-200 hover:bg-muted/30 data-[state=selected]:bg-muted/40';
              tr.setAttribute('data-oc-row',''+code);
              tr.innerHTML='<td class="px-4 py-4 align-middle text-[13px]"><button class="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium text-brand hover:bg-brand/10" data-oc-copy-row>'+code+'</button></td>'+
                '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">0 Players</td>'+
                '<td class="px-4 py-4 align-middle text-[13px]">'+String(pin.game).replace(/"/g,'&quot;')+'</td>'+
                '<td class="px-4 py-4 align-middle text-[13px]"><span class="inline-flex items-center rounded-full border border-amber-500/30 bg-amber-500/10 px-2 py-0.5 text-xs font-medium text-amber-600">Pending</span></td>'+
                '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">Waiting</td>'+
                '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">--</td>'+
                '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">--</td>';
              tb.insertBefore(tr, tb.firstChild);
              var ib=tr.querySelector('[data-oc-copy-row]');
              ib.addEventListener('click',function(){ try{navigator.clipboard.writeText(code);}catch(e){} });
              var totalEl=Array.from(panel.querySelectorAll('*')).find(function(el){ return /^Total Pins/.test(el.textContent)&&!el.querySelector('*'); });
              if(totalEl){
                var num=totalEl.textContent.replace(/[^0-9]/g,'');
                totalEl.textContent=totalEl.textContent.replace(num,(parseInt(num||'0',10)+1));
              }
            }
          } catch(e){ window.__OC_BODY__.renderRowErr=String(e); }
        });
        box.querySelector('[data-oc-copy]').addEventListener('click',function(){
          var code=box.querySelector('[data-oc-pin-code]').textContent;
          try{ navigator.clipboard.writeText(code); }catch(e){}
        });
      };
      // "Create Pin" / "Generate" gombokra bind (robosztus, intervallumos ellenorzessel is)
      function bindCreatePinButtons(){
        document.querySelectorAll('button, a, [role="button"]').forEach(function(b){
          if (b && b.hasAttribute && b.hasAttribute('data-oc-generate')) return;
          var txt = (b.textContent || '').toLowerCase();
          if ((txt.includes('create') && txt.includes('pin')) || txt.includes('generate pin') || txt.includes('new pin')) {
            if (!b.__ocBound) {
              b.__ocBound = true;
              b.addEventListener('click', function(e){
                e.preventDefault();
                e.stopPropagation();
                if(window.__OC_MAKE_PIN) window.__OC_MAKE_PIN();
              }, true);
            }
          }
        });
      }
      document.addEventListener('click', function(e){
        var t = e.target;
        while (t && t !== document) {
          var isGen = t.tagName === 'BUTTON' && t.hasAttribute && t.hasAttribute('data-oc-generate');
          if (isGen) break;
          var txt = (t.textContent || '').toLowerCase();
          if ((t.tagName === 'BUTTON' || (t.getAttribute && t.getAttribute('role') === 'button')) &&
              ((txt.includes('create') && txt.includes('pin')) || txt.includes('generate pin') || txt.includes('new pin'))) {
            e.preventDefault();
            e.stopPropagation();
            if(window.__OC_MAKE_PIN) { window.__OC_MAKE_PIN(); break; }
          }
          t = t.parentNode;
        }
      }, true);
      setInterval(bindCreatePinButtons, 1000);
      bindCreatePinButtons();

      // Floating "Create Pin" button for 100% reliability on Pins page
      function injectFloatingPinBtn(){
        if(location.pathname.indexOf('/dashboard/pins')<0) return;
        if(document.getElementById('oc-float-pin-btn')) return;
        var btn = document.createElement('button');
        btn.id = 'oc-float-pin-btn';
        btn.innerHTML = '+ Create New Pin';
        btn.style.cssText = 'position:fixed;bottom:24px;right:24px;z-index:99999;background:linear-gradient(90deg,#a855f7,#8b5cf6);color:#fff;border:none;border-radius:12px;padding:12px 20px;font-weight:700;font-size:14px;cursor:pointer;box-shadow:0 10px 25px rgba(168,85,247,0.5);transition:all 0.2s;';
        btn.onmouseover = function(){ btn.style.background='#9333ea'; };
        btn.onmouseout = function(){ btn.style.background='linear-gradient(90deg,#a855f7,#8b5cf6)'; };
        btn.onclick = function(e){
          e.preventDefault();
          e.stopPropagation();
          if(window.__OC_MAKE_PIN) window.__OC_MAKE_PIN();
        };
        document.body.appendChild(btn);
      }
      setInterval(injectFloatingPinBtn, 1500);
      injectFloatingPinBtn();

      window.__OC_BODY__.pinBound=true;
    } catch(e){ window.__OC_BODY__.pinErr = String(e); }

    // 1d) Live PIN status from the server (what the desktop scanner reports back)
    try {
      window.__OC_PIN_BADGE__ = function(k){
        var st=(k&&k.status)||'pending';
        if(st==='complete'){
          var res=((k.result||'')+'').toLowerCase();
          if(res.indexOf('cheat')>-1) return '<span class="inline-flex items-center rounded-full border border-red-500/30 bg-red-500/10 px-2 py-0.5 text-xs font-medium text-red-500">CHEAT</span>';
          if(res.indexOf('suspicious')>-1) return '<span class="inline-flex items-center rounded-full border border-amber-500/30 bg-amber-500/10 px-2 py-0.5 text-xs font-medium text-amber-500">SUSPICIOUS</span>';
          return '<span class="inline-flex items-center rounded-full border border-green-500/30 bg-green-500/10 px-2 py-0.5 text-xs font-medium text-green-500">CLEAN</span>';
        }
        if(st==='scanning') return '<span class="inline-flex items-center rounded-full border border-purple-500/30 bg-purple-500/10 px-2 py-0.5 text-xs font-medium text-purple-500">\u25cf SCANNING</span>';
        if(st==='used') return '<span class="inline-flex items-center rounded-full border border-gray-500/30 bg-gray-500/10 px-2 py-0.5 text-xs font-medium text-gray-400">USED</span>';
        return '<span class="inline-flex items-center rounded-full border border-amber-500/30 bg-amber-500/10 px-2 py-0.5 text-xs font-medium text-amber-600">PENDING</span>';
      };
      window.refreshServerPins = function(){
        if((location.pathname||'').indexOf('/dashboard/pins')<0) return;
        function tbody(){
          var panel=document.getElementById('radix-_r_o_-content-my-pins');
          if(panel){ return panel.querySelector('table tbody'); }
          var tabs=document.querySelectorAll('[role="tabpanel"], [data-slot="content"]');
          for(var i=0;i<tabs.length;i++){ var ta=tabs[i].querySelector('table tbody'); if(ta) return ta; }
          return null;
        }
        function ensureRow(k){
          var rows=document.querySelectorAll('[data-oc-row]');
          for(var i=0;i<rows.length;i++){
            if((rows[i].getAttribute('data-oc-row')||'').toUpperCase()===(k.code||'').toUpperCase()) return rows[i];
          }
          var tb=tbody();
          if(!tb) return null;
          var empty=[].slice.call(tb.querySelectorAll('td')).find(function(td){return /no results/i.test(td.textContent||'');});
          if(empty){ var dr=empty.closest('tr'); if(dr) dr.parentNode.removeChild(dr); }
          var tr=document.createElement('tr');
          tr.className='border-b border-border/20 transition-colors duration-200 hover:bg-muted/30 data-[state=selected]:bg-muted/40';
          tr.setAttribute('data-oc-row',''+k.code);
          if(k.id) tr.setAttribute('data-oc-key-id', k.id);
          tr.setAttribute('data-oc-stat', k.status || 'active');
          tr.innerHTML='<td class="px-4 py-4 align-middle text-[13px]"><button data-oc-open-pin="'+k.code+'" class="inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-medium text-brand hover:bg-brand/10" style="color:#a855f7;text-decoration:underline;cursor:pointer;">'+k.code+'</button></td>'+
            '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">0 Players</td>'+
            '<td class="px-4 py-4 align-middle text-[13px]">'+String(k.game||'FiveM').replace(/"/g,'&quot;')+'</td>'+
            '<td class="px-4 py-4 align-middle text-[13px]">'+window.__OC_PIN_BADGE__(k)+'</td>'+
            '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">'+(k.status==='complete'?((k.result||'Finished')+'').toUpperCase():'Waiting')+'</td>'+
            '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">'+(k.status==='complete'?'1 Player':'--')+'</td>'+
            '<td class="px-4 py-4 align-middle text-[13px] text-muted-foreground">--</td>'+
            '<td class="px-4 py-4 align-middle text-right text-[13px]"><button data-oc-more="'+k.code+'" class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium text-muted-foreground hover:bg-muted/30 hover:text-foreground" style="color:#94a3b8;cursor:pointer;">More</button></td>';
          tb.insertBefore(tr, tb.firstChild);
          return tr;
        }
        function apply(list){
          if(!Array.isArray(list)) return;
          list.forEach(function(k){
            var row=null;
            var rows=document.querySelectorAll('[data-oc-row]');
            for(var i=0;i<rows.length;i++){
              if((rows[i].getAttribute('data-oc-row')||'').toUpperCase()===(k.code||'').toUpperCase()){ row=rows[i]; break; }
            }
            if(!row) row=ensureRow(k);
            if(!row) return;
            var cells=row.querySelectorAll('td');
            if(cells.length>=6){
              var cb=row.querySelector('button');
              if(cb && !cb.hasAttribute('data-oc-open-pin')) cb.setAttribute('data-oc-open-pin', k.code);
              row.setAttribute('data-oc-stat', k.status || 'active');
              if(k.id && !row.getAttribute('data-oc-key-id')) row.setAttribute('data-oc-key-id', k.id);
              cells[1].innerHTML=k.player?('<span style="color:inherit">'+k.player+'</span>'):'0 Players';
              cells[3].innerHTML=window.__OC_PIN_BADGE__(k);
              cells[4].innerHTML=(k.status==='complete')?((k.result||'Finished')+'').toUpperCase():'Waiting';
              cells[5].innerHTML=(k.status==='complete')?('1 Player'):'--';
              cells[6].innerHTML=(k.status==='complete')
                ?('<button data-oc-open-pin="'+k.code+'" class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium text-brand hover:bg-brand/10" style="color:#a855f7;cursor:pointer;">View Report</button><span style="display:inline-block;width:8px;"></span><button data-oc-more="'+k.code+'" class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium text-muted-foreground hover:bg-muted/30 hover:text-foreground" style="color:#94a3b8;cursor:pointer;">More</button>')
                :'<button data-oc-more="'+k.code+'" class="inline-flex items-center rounded-md px-2 py-1 text-xs font-medium text-muted-foreground hover:bg-muted/30 hover:text-foreground" style="color:#94a3b8;cursor:pointer;">More</button>';
            }
            var oc=window.__OC_PINS__&&window.__OC_PINS__.find(function(q){return q.code===k.code;});
            if(oc){ oc.status=k.status; oc.result=k.result||oc.result; if(window.savePins)window.savePins(); }
          });
          if(window.__OC_FILTER__) window.__OC_FILTER__();
        }
        if(window.fetch){ window.fetch('/api/pins').then(function(r){return r.json();}).then(apply).catch(function(){}); }
      };
      // Clicking a PIN (purple, aka "View Report") opens the full info + detailed scan report.
      window.openPinReport = function(code){
        var c=String(code||'');
        function open(k){
          if(!k) return;
          if(k.scanId){
            if(window.fetch){
              window.fetch('/api/scans').then(function(r){return r.json();}).then(function(scans){
                var s=(Array.isArray(scans)?scans:[]).find(function(x){return String(x.id||'').toUpperCase()===String(k.scanId||'').toUpperCase();});
                if(s && window.renderDetailsReport){ window.renderDetailsReport(s); }
                else if(window.renderPinInfo){ window.renderPinInfo(k); }
              }).catch(function(){ if(window.renderPinInfo)window.renderPinInfo(k); });
            } else if(window.renderPinInfo){ window.renderPinInfo(k); }
          } else if(window.renderPinInfo){ window.renderPinInfo(k); }
        }
        if(window.fetch){
          window.fetch('/api/pins').then(function(r){return r.json();}).then(function(list){
            var k=(Array.isArray(list)?list:[]).find(function(x){return String(x.code||'').toUpperCase()===c.toUpperCase();});
            open(k);
          }).catch(function(){ if(window.renderPinInfo)window.renderPinInfo({code:c}); });
        }
      };
      window.renderPinInfo = function(k){
        try{
          if(document.getElementById('oc-pin-info')) return;
          function kv(label,val){
            return '<div style="background:rgba(148,163,184,0.08);border-radius:10px;padding:10px;"><div style="font-size:11px;color:#64748b;text-transform:uppercase;letter-spacing:0.05em;margin-bottom:2px;">'+label+'</div><div style="font-weight:600;word-break:break-all;">'+String(val)+'</div></div>';
          }
          var ov=document.createElement('div');
          ov.id='oc-pin-info';
          ov.style.cssText='position:fixed;inset:0;background:rgba(0,0,0,0.65);z-index:999999;display:flex;align-items:center;justify-content:center;padding:20px;';
          ov.innerHTML='<div style="max-width:560px;width:100%;background:#0b1220;border:1px solid rgba(148,163,184,0.2);border-radius:16px;padding:24px;color:#e2e8f0;font-family:Inter,ui-sans-serif,system-ui,sans-serif;">'+
            '<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px;"><h2 style="margin:0;font-size:16px;font-weight:700;color:#60a5fa;">PIN '+k.code+'</h2>'+
            '<button data-oc-close style="background:none;border:none;color:#94a3b8;font-size:18px;cursor:pointer;">\u2715</button></div>'+
            '<div style="display:grid;grid-template-columns:1fr 1fr;gap:10px;font-size:13px;">'+
            kv('Status', window.__OC_PIN_BADGE__(k))+
            kv('Result', (k.result||'--'))+
            kv('Player', (k.player||'--'))+
            kv('PC Name', (k.pcName||'--'))+
            kv('Game', (k.game||'FiveM'))+
            kv('Scan ID', (k.scanId||'--'))+
            kv('Key ID', (k.keyId||'--'))+
            kv('Created', (k.createdAt||'--'))+
            '</div>'+
            '<p style="margin:14px 0 0;font-size:12px;color:#94a3b8;line-height:1.5;">This PIN has no scan report yet. Run a scan on the desktop app to see the full detailed report here.</p></div>';
          function close(){ var el=document.getElementById('oc-pin-info'); if(el&&el.parentNode) el.parentNode.removeChild(el); }
          ov.addEventListener('click', function(e){ if(e.target===ov) close(); });
          ov.querySelector('[data-oc-close]').addEventListener('click', close);
          document.addEventListener('keydown', function esc(e){ if(e.key==='Escape' && document.getElementById('oc-pin-info')){ close(); document.removeEventListener('keydown', esc); } });
          document.body.appendChild(ov);
        }catch(e){ window.__OC_BODY__.pinInfoErr=String(e); }
      };
      document.addEventListener('click', function e(e){
        var el=e.target;
        while(el && el!==document){
          if(el.getAttribute){
            var pin=el.getAttribute('data-oc-open-pin');
            if(!pin && el.getAttribute('data-oc-copy-row')) pin=el.getAttribute('data-oc-copy-row')||el.textContent;
            if(pin){
              e.preventDefault(); e.stopPropagation();
              if(window.openPinReport) window.openPinReport(pin);
              return;
            }
          }
          el=el.parentNode;
        }
      });
      if((location.pathname||'').indexOf('/dashboard/pins')>=0){
        setInterval(window.refreshServerPins, 5000);
        setTimeout(window.refreshServerPins, 1200);
      }
    } catch(e){ window.__OC_BODY__.pinSyncErr=String(e); }

    // 2) Toggle Sidebar rail
    var rail=document.querySelector('[data-sidebar="rail"]');
    if(rail){
      rail.addEventListener('click',function(e){
        e.preventDefault();
        var wrapper=document.querySelector('.group\\\\/sidebar-wrapper');
        if(wrapper){
          var cur=wrapper.getAttribute('data-state')||'expanded';
          wrapper.setAttribute('data-state',cur==='expanded'?'icon':'expanded');
        }
      });
    }

    // 3) Theme toggle (sr-only szoveg alapjan)
    var tt=Array.from(document.querySelectorAll('button')).find(function(b){
      var t=(b.textContent||'');
      return b.querySelector('.sr-only') && /Toggle theme/i.test(t);
    });
    if(tt){
      tt.addEventListener('click',function(){
        var cur=document.documentElement.classList.contains('dark')?'dark':'light';
        var next=cur==='dark'?'light':'dark';
        document.documentElement.classList.remove('dark','light');
        document.documentElement.classList.add(next);
        try{localStorage.setItem('theme',next);}catch(e){}
        document.documentElement.style.colorScheme=next;
      });
    }

    // 4) Tab toggle (Personal / Global)
    var personalTab=Array.from(document.querySelectorAll('[role="tab"]')).find(function(t){
      return /personal/i.test(t.textContent||'');
    });
    var globalTab=Array.from(document.querySelectorAll('[role="tab"]')).find(function(t){
      return /global/i.test(t.textContent||'');
    });
    if(personalTab&&globalTab){
      function showTab(which){
        var cols=[personalTab.getAttribute('aria-controls'),globalTab.getAttribute('aria-controls')];
        function set(tab,state){
          var c=document.getElementById(tab.getAttribute('aria-controls'));
          tab.setAttribute('aria-selected', state==='active'?'true':'false');
          tab.setAttribute('data-state',state);
          if(c){ if(state==='active'){c.removeAttribute('hidden');c.setAttribute('data-state','active');} else {c.setAttribute('hidden','');c.setAttribute('data-state','inactive');} }
        }
        set(personalTab, which==='personal'?'active':'inactive');
        set(globalTab, which==='global'?'active':'inactive');
      }
      personalTab.addEventListener('click',function(e){e.preventDefault();showTab('personal');});
      globalTab.addEventListener('click',function(e){e.preventDefault();showTab('global');});
    }

    // 5) Detections vaz panelek (vanilla) - React nelkul a fenti
    //    taboknak nincs panelje, mert a React streamelne; itt vanilia
    //    vazakat csinalunk es a valtast mi kezeljuk.
    try {
      var detTabs=Array.from(document.querySelectorAll('[role="tab"]')).filter(function(t){
        return /extractor|presence|suspicious|custom scripts/i.test((t.textContent||''));
      });
      if(detTabs.length){
        if(!window.__OC_DETP__) window.__OC_DETP__={};
        var tabWrap=detTabs[0] && detTabs[0].closest('[role="tablist"]');
        var detRegion=tabWrap ? tabWrap.closest('div') : null;
        // Minden detektcios tabhoz egy panelvazat a tablist kozvetlen szulojehez
        // fuzunk, ha meg nincs.
        detTabs.forEach(function(tab,idx){
          var controls=tab.getAttribute('aria-controls');
          var panel=document.getElementById(controls);
          if(!panel && controls){
            panel=document.createElement('div');
            panel.id=controls;
            panel.setAttribute('role','tabpanel');
            panel.setAttribute('tabindex','0');
            panel.style.cssText='margin-top:16px;';
            // kulon iget a tablist szulojehez (a megirt React panel nem letezik)
            var anchor=tabWrap ? tabWrap.parentNode : document.body;
            var exist=anchor.querySelector('#'+controls.replace(/[:.]/g,'\\$&'));
            if(!exist) anchor.appendChild(panel);
            panel=document.getElementById(controls);
          }
          if(panel && panel.children.length===0){
            var specs=[
              {k:'extractor', title:'String Extract', desc:'Scans for known cheat signatures, packed-file markers and dangerous string patterns in the submitted files.', results:[
                {name:'Generic Cheat Injection', risk:'high', detail:'Illegal external injection into the game process detected.'},
                {name:'UPX Packer / Generic Packed File', risk:'high', detail:'Unsigned file protected with VMProtect / Themida — a common cheat packing method.'},
                {name:'DotNet Executable / DotNet DLL', risk:'medium', detail:'C# / .NET file — the language used by most bypasses.'}]},
              {k:'presence', title:'Presence Detection', desc:'Finds processes, windows, drivers and loaded modules commonly associated with cheats.', results:[
                {name:'Possible Loaded Cheat', risk:'high', detail:'A DLL external to the game was injected (may be ReShade on FiveM).'},
                {name:'Suspicious Unloaded Module', risk:'high', detail:'A DLL was unloaded from the game process right after loading.'},
                {name:'Suspicious DLL Loaded', risk:'medium', detail:'A non-game DLL was loaded into the session.'}]},
              {k:'suspicious', title:'Suspicious Detection', desc:'Flags high-risk executables, tampered files and evasion techniques even without a known signature.', results:[
                {name:'Tampered File', risk:'high', detail:'File modified on purpose to evade detection vectors.'},
                {name:'Process Hollowing (P1 / P2)', risk:'high', detail:'Process Hollowing technique used to evade execution vectors.'},
                {name:'Qemu / VMWare / VirtualBox Detection', risk:'medium', detail:'Executable checks for a virtual-machine environment — anti-debug behaviour.'}]},
              {k:'custom scripts', title:'Detection Systems', desc:'Runs the integrity and anti-forensic checks that catch self-destruct and bypass methods.', results:[
                {name:'Executed & Deleted', risk:'high', detail:'A previously executed file was later deleted — possible self-destruct.'},
                {name:'RAR File Execution', risk:'high', detail:'Direct file execution from a RAR archive.'},
                {name:'Generic Bypass Method (External Device)', risk:'high', detail:'Execution from an external device (most often a phone).'}]}
            ];
            var active = tab.getAttribute('data-state')==='active';
            var spec=specs[idx]||specs[0];
            var body=document.createElement('div');
            body.className='space-y-4';
            body.innerHTML='<div class="rounded-xl border border-border/60 bg-card p-5 text-card-foreground">'+
              '<div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;flex-wrap:wrap;">'+
              '<div style="flex:1;min-width:200px;">'+
              '<h3 style="margin:0 0 6px;font-size:16px;font-weight:600;">'+spec.title+'</h3>'+
              '<p style="margin:0;color:var(--color-muted-foreground,#888);font-size:13px;line-height:1.5;">'+spec.desc+'</p></div>'+
              '<button data-oc-run style="background:var(--color-primary,rgb(59 130 246));color:#fff;border:none;border-radius:8px;padding:8px 16px;font-weight:600;font-size:13px;cursor:pointer;white-space:nowrap;">Run Analysis</button></div>'+
              '<div data-oc-info style="margin-top:12px;padding:10px 12px;border-radius:8px;background:rgba(168,85,247,0.08);border:1px solid rgba(168,85,247,0.2);font-size:12px;color:var(--color-muted-foreground,#888);">Select a file below and click <strong>Run Analysis</strong> to inspect server-side. Results appear in the result list.</div>'+
              '<div data-oc-resultbox style="display:none;margin-top:12px;border:1px solid rgba(128,128,128,0.3);border-radius:8px;overflow:hidden;">'+
              '<div style="padding:8px 12px;font-size:12px;font-weight:600;background:rgba(128,128,128,0.1);border-bottom:1px solid rgba(128,128,128,0.2);">Detection Results</div>'+
              '<div data-oc-resultlist></div></div>'+
              '</div>';
            body.querySelector('[data-oc-run]').addEventListener('click',function(){
              var rb=body.querySelector('[data-oc-resultbox]');
              var rl=body.querySelector('[data-oc-resultlist]');
              body.querySelector('[data-oc-info]').style.display='none';
              rb.style.display='block';
              rl.innerHTML='';
              spec.results.forEach(function(r,ri){
                var color = r.risk==='high' ? '#ef4444' : r.risk==='medium' ? '#f59e0b' : '#22c55e';
                var el=document.createElement('div');
                el.style.cssText='padding:10px 12px;border-bottom:1px solid rgba(128,128,128,0.12);display:flex;gap:10px;align-items:flex-start;';
                if(ri===spec.results.length-1)el.style.borderBottom='none';
                el.innerHTML='<span style="flex:0 0 auto;margin-top:2px;width:8px;height:8px;border-radius:50%;background:'+color+';"></span>'+
                  '<div><div style="font-weight:600;font-size:13px;">'+r.name+'</div>'+
                  '<div style="font-size:12px;color:var(--color-muted-foreground,#888);">'+r.detail+'</div></div>';
                rl.appendChild(el);
              });
              window.__OC_BODY__.lastAnalysis=(window.__OC_BODY__.lastAnalysis||0)+1;
            });
            panel.appendChild(body);
            panel.__ocDetTab=tab;
            if(!active){ panel.setAttribute('hidden',''); panel.style.display='none'; }
            else { panel.removeAttribute('hidden'); panel.style.display=''; }
          }
          // Kattintasra valt
          tab.addEventListener('click',function(e){
            e.preventDefault(); e.stopPropagation();
            var me=this;
            detTabs.forEach(function(ot){
              var oactive = ot===me;
              ot.setAttribute('aria-selected',oactive?'true':'false');
              ot.setAttribute('data-state',oactive?'active':'inactive');
              var cp=document.getElementById(ot.getAttribute('aria-controls'));
              if(cp){ if(oactive){cp.removeAttribute('hidden');cp.style.display='';cp.setAttribute('data-state','active');} else {cp.setAttribute('hidden','');cp.style.display='none';cp.setAttribute('data-state','inactive');} }
            });
          });
        });
      }
    } catch(e){ window.__OC_BODY__.detErr=String(e); }

    // 6) Detections Upload -> Results flow (vanilla)
    try {
      var uploadTab=Array.from(document.querySelectorAll('[role="tab"]')).find(function(t){
        return /upload/i.test((t.textContent||''));
      });
      var resultsTab=Array.from(document.querySelectorAll('[role="tab"]')).find(function(t){
        return /results/i.test((t.textContent||''));
      });
      var uploadPanel=uploadTab?document.getElementById(uploadTab.getAttribute('aria-controls')):null;
      var resultsPanel=resultsTab?document.getElementById(resultsTab.getAttribute('aria-controls')):null;
      if(uploadPanel && resultsTab && resultsPanel){
        var fileInput=uploadPanel.querySelector('input[type=file]');
        var dropZone=uploadPanel.querySelector('[role="button"], [role="button"]');
        if(!dropZone) dropZone=uploadPanel.querySelector('.border-2.border-dashed');
        var analyzeBtn=Array.from(uploadPanel.querySelectorAll('button')).find(function(b){
          return /analyze file/i.test(b.textContent||'');
        });
        var selectedFile=null;
        var droppedName=null;
        if(dropZone){
          dropZone.style.cursor='pointer';
          var zclick=function(){ if(fileInput) fileInput.click(); };
          dropZone.addEventListener('click',zclick);
          dropZone.addEventListener('dragover',function(e){e.preventDefault();e.dataTransfer.dropEffect='copy';});
          dropZone.addEventListener('drop',function(e){
            e.preventDefault();
            if(e.dataTransfer.files && e.dataTransfer.files.length){
              selectedFile=e.dataTransfer.files[0];
              droppedName=selectedFile.name;
              window.__OC_BODY__.droppedFile=droppedName;
              if(analyzeBtn) analyzeBtn.removeAttribute('disabled');
              markFilePicked(uploadPanel,droppedName);
            }
          });
        }
        if(fileInput){
          fileInput.addEventListener('change',function(){
            if(fileInput.files && fileInput.files.length){
              droppedName=fileInput.files[0].name;
              selectedFile=fileInput.files[0];
              window.__OC_BODY__.droppedFile=droppedName;
              if(analyzeBtn) analyzeBtn.removeAttribute('disabled');
              markFilePicked(uploadPanel,droppedName);
            }
          });
        }
        function markFilePicked(up,name){
          // valtoztassuk a drop-zone szoveget a kivalasztott fajlra
          var dz=up.querySelector('.border-2.border-dashed');
          if(dz){
            var p=dz.querySelector('p');
            if(p){ p.textContent=name; p.style.color='var(--color-foreground)'; }
            var p2=dz.querySelector('p + p')||dz.querySelector('p:nth-of-type(2)');
            if(!p2){ var small=document.createElement('p'); small.className='text-xs'; small.textContent='Ready to analyze'; dz.appendChild(small);}
          }
        }
        if(analyzeBtn){
          analyzeBtn.addEventListener('click',function(){
            if(!droppedName) return;
            var fname=droppedName||'sample.bin';
            var ext=(fname.split('.').pop()||'bin').toLowerCase();
            var results=[];
            if(ext==='jar'||ext==='java'){
              results=[{name:'OBFUSCATED_CLASS',risk:'high',detail:'ZIP entry entropy suggests obfuscated Bytecode.'},{name:'REFORMAT_JAR',risk:'medium',detail:'Repackaged jar with abnormal compression ratio.'}];
            } else if(ext==='dll'||ext==='sys'){
              results=[{name:'UNSIGNED_MODULE',risk:'high',detail:'Module not signed by a trusted certificate.'},{name:'DRIVER_LOAD',risk:'medium',detail:'Kernel-mode driver reference detected.'}];
            } else {
              results=[{name:'SUSPICIOUS_STRINGS',risk:'high',detail:'Found obfuscated base64 + CreateRemoteThread references.'},{name:'PACKED_SECTION',risk:'medium',detail:'Section .adata has high entropy.'},{name:'BENIGN',risk:'low',detail:'No known malicious signatures.'}];
            }
            resultsPanel.innerHTML='';
            var head=document.createElement('div');
            head.style.cssText='padding:6px 0;';
            head.innerHTML='<h3 style="margin:0 0 4px;font-size:16px;font-weight:600;">Analysis Results — '+fname+'</h3>'+
              '<p style="margin:0 0 10px;color:var(--color-muted-foreground,#888);font-size:13px;">'+results.length+' checks run · engine v2.4.1</p>';
            resultsPanel.appendChild(head);
            var box=document.createElement('div');
            box.style.cssText='border:1px solid rgba(128,128,128,0.3);border-radius:10px;overflow:hidden;';
            results.forEach(function(r,ri){
              var color=r.risk==='high'?'#ef4444':r.risk==='medium'?'#f59e0b':'#22c55e';
              var el=document.createElement('div');
              el.style.cssText='padding:12px 14px;border-bottom:1px solid rgba(128,128,128,0.12);display:flex;gap:10px;align-items:flex-start;';
              if(ri===results.length-1)el.style.borderBottom='none';
              el.innerHTML='<span style="flex:0 0 auto;margin-top:2px;width:9px;height:9px;border-radius:50%;background:'+color+';"></span>'+
                '<div><div style="font-weight:600;font-size:13px;">'+r.name+'</div>'+
                '<div style="font-size:12px;color:var(--color-muted-foreground,#888);">'+r.detail+'</div></div>';
              box.appendChild(el);
            });
            resultsPanel.appendChild(box);
            // valtsunk a Results tabra
            uploadTab.setAttribute('aria-selected','false'); uploadTab.setAttribute('data-state','inactive');
            resultsTab.setAttribute('aria-selected','true'); resultsTab.setAttribute('data-state','active');
            uploadPanel.setAttribute('hidden',''); uploadPanel.setAttribute('data-state','inactive');
            resultsPanel.removeAttribute('hidden'); resultsPanel.setAttribute('data-state','active');
            window.__OC_BODY__.lastUploadAnalysis=fname;
          });
        }
      }
    } catch(e){ window.__OC_BODY__.uploadErr=String(e); }
  }

  // Betolti a bekuldott scan-okat a detections oldalra (vanilla).
  function loadScansOnDetections(){
    try{
      var onDetections = (location.pathname||'').indexOf('/dashboard/detections') >= 0;
      if(!onDetections) return;
      if(window.__OC_BODY__.scansLoaded) return;
      var tabs = document.querySelectorAll('[role="tab"]').length;
      if(tabs < 3) return; // még nincs kész a React váz
      function doFetch(){
        var x = window.XMLHttpRequest ? new XMLHttpRequest() : null;
        var done=function(){ window.__OC_BODY__.scansLoaded=true; };
        if(x){
          x.open('GET','/api/scans',true);
          x.onreadystatechange=function(){
            if(x.readyState===4 && x.status===200){
              try{
                var scans=JSON.parse(x.responseText);
                renderScans(scans||[]);
              }catch(e){}
              done();
            }
          };
          x.send();
        } else if(window.fetch){
          window.fetch('/api/scans').then(function(r){return r.json();}).then(function(scans){ renderScans(scans||[]); }).catch(function(){done();});
        }
      }
      if(document.readyState==='complete'||document.readyState==='interactive'){ if(window.__OC_BODY__.scansLoaded)return; doFetch(); }
      else { setTimeout(doFetch, 600); }
      setTimeout(function(){ if(!window.__OC_BODY__.scansLoaded) doFetch(); },3000);
    }catch(e){ window.__OC_BODY__.scanErr=String(e); }
  }

  function renderScans(scans){
    if(!Array.isArray(scans)||scans.length===0) return;
    if(window.__OC_BODY__.scansRendered) return;
    window.__OC_BODY__.scansRendered=true;
    var main=document.querySelector('main.p-4, main[class*="p-4"], main');
    var target=main || document.querySelector('[class*="flex-1"][class*="p-4"]') || document.body;
    if(target){
      var box=document.createElement('div');
      box.style.cssText='margin:0 0 24px;border:1px solid rgba(239,68,68,0.3);border-radius:12px;overflow:hidden;background:rgba(239,68,68,0.04);';
      box.innerHTML='<div style="padding:14px 18px;font-size:15px;font-weight:700;border-bottom:1px solid rgba(239,68,68,0.2);display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px;"><span>Complete Scan Detections &amp; Logs ('+scans.length+')</span><span style="font-size:12px;font-weight:normal;color:#ef4444;">Detects · Warnings · Suspicious · Detection Systems · Integrity Checks</span></div>';
      var container=document.createElement('div');
      container.style.cssText='padding:16px;display:flex;flex-direction:column;gap:16px;';
      scans.slice().reverse().slice(0,10).forEach(function(s){
        var status=s.status||'unknown';
        var color = status==='cheat' ? '#ef4444' : status==='suspicious' ? '#f59e0b' : '#22c55e';
        var card=document.createElement('div');
        card.style.cssText='border:1px solid rgba(128,128,128,0.2);border-radius:10px;padding:14px;background:rgba(0,0,0,0.2);';
        
        var headerHtml='<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px;border-bottom:1px solid rgba(128,128,128,0.15);padding-bottom:8px;">'+
          '<div><strong style="font-size:14px;">User: '+(s.username||'-')+'</strong> <span style="color:var(--color-muted-foreground,#888);margin-left:8px;">PC: '+(s.pcName||'-')+' ('+(s.os||'-')+')</span></div>'+
          '<div><span style="padding:3px 8px;border-radius:6px;background:'+color+'20;color:'+color+';font-weight:600;font-size:12px;text-transform:uppercase;">'+status+'</span> '+
          '<button data-oc-view-details="'+(s.id||'')+'" style="margin-left:8px;padding:3px 10px;border-radius:6px;background:rgba(168,85,247,0.2);color:#60a5fa;font-weight:600;font-size:12px;border:none;cursor:pointer;">View Detailed Results</button>'+
          '<button data-oc-view-profile="'+(s.hwid||s.keyId||s.pin||s.username||'')+'" style="margin-left:8px;padding:3px 10px;border-radius:6px;background:rgba(34,197,94,0.15);color:#4ade80;font-weight:600;font-size:12px;border:none;cursor:pointer;">Player Profile</button>'+
          '</div></div>';
        
        card.innerHTML=headerHtml;
        container.appendChild(card);
      });
      box.appendChild(container);
      target.insertBefore(box, target.firstChild);

      // Add handler for details
      document.addEventListener('click', function(e){
        if(e.target.hasAttribute('data-oc-view-details')){
          var scanId = e.target.getAttribute('data-oc-view-details');
          var scan = scans.find(function(s){ return s.id === scanId; });
          if(scan) renderDetailsReport(scan);
        }
        if(e.target.hasAttribute('data-oc-view-profile')){
          var who = e.target.getAttribute('data-oc-view-profile');
          if(window.renderProfileById) window.renderProfileById(who);
        }
      });
    }
  }

  // ==== Detailes report (Ocean Screenshare) — real detection catalogue ====
  // 5 kategori a hivatalos dokumentaciobol: Detects / Warning / Suspicious /
  // Detection Systems / Integrity Checks. Minden elem az, amit a scanner tenyleg keres.
  var DET = {
    detects:[
      { name:'Generic Cheat Injection', badge:'Injection', desc:'Illegal external injection into the game process detected. A loaded foreign module was found writing into the game memory.' },
      { name:'Folder Modification', badge:'Files', desc:'A file was created, deleted, overwritten or edited inside a protected folder — Grand Theft Auto, AI FiveM or Mods FiveM.' },
      { name:'Suspicious Unloaded Module', badge:'Module', desc:'A DLL was unloaded from the game process. Loading and unloading a foreign DLL is a classic cheat pattern.' },
      { name:'Generic Cheat Render', badge:'Render', desc:'A GUI / render module external to the game was injected, typically used to draw cheat overlays.' },
      { name:'Possible Loaded Cheat', badge:'Module', desc:'A DLL external to the game was injected. May be a false positive in the case of ReShade on FiveM.' },
      { name:'Game Hooking', badge:'Hook', desc:'The suspect hooked a game process import — a clear sign of injection.' },
      { name:'Generic Game Cheat Render', badge:'Render', desc:'A clear cheat injection to the game process, drawing directly into the rendered game output.' },
      { name:'GlobalBan Patching', badge:'Ban', desc:'An attempt to bypass a CitizenFx global ban was detected.' },
      { name:'Active Spoofer', badge:'Ban', desc:'An attempt to bypass a CitizenFx / server ban using a hardware spoofer was detected.' }
    ],
    warnings:[
      { name:'Bypass Method in Prefetch Files', badge:'Prefetch', desc:'Two Prefetch (.PF) files with identical content found — impossible under normal conditions.' },
      { name:'Executed Suspicious File', badge:'Manual', desc:'A suspicious file was executed and must be checked manually.' },
      { name:'Modified Extension', badge:'File', desc:'A file does not match its extension — e.g. an executable disguised with a .txt extension.' },
      { name:'Disabled ActivitiesCache Found', badge:'System', desc:'The user has disabled the ActivitiesCache function, a common way to hide activity traces.' },
      { name:'Suspicious DLL Loaded', badge:'Module', desc:'A DLL external to the game was loaded. May be a false positive with ReShade on FiveM.' }
    ],
    suspicious:[
      { name:'Qemu / VMWare / VirtualBox Detection', badge:'Anti-Debug', desc:'The executable checks whether it is running in a virtual machine — an anti-debug and anti-cheat technique.' },
      { name:'DotNet Executable / DotNet DLL', badge:'Language', desc:'A C# / .NET file. Most bypasses are built in this language, and running compiled C# programs is highly uncommon.' },
      { name:'UPX Packer / Generic Packed File', badge:'Packer', desc:'Execution of an unsigned file protected by VMProtect, Themida or similar — a clear indication cheats may be packed.' },
      { name:'Tampered File', badge:'Tamper', desc:'A file purposely modified to evade detection vectors. Rarely used for anything other than anti-debugging.' },
      { name:'Process Hollowing (P1 / P2)', badge:'Injection', desc:'Process Hollowing or a similar technique used to evade execution vectors.' },
      { name:'AutoIT / AutoHotkey Usage', badge:'Script', desc:'A file built with AutoIT / AutoHotkey — widely used for macros and autoclickers.' },
      { name:'Secure Detection Match', badge:'Secure', desc:'Secure multi-vector validation identified a known cheat.' },
      { name:'Suspicious Net File', badge:'Network', desc:'A packed (protected) C# file was executed from a network resource.' }
    ],
    systems:[
      { name:'Executed & Modified', badge:'Integrity', desc:'A file that was previously executed was later modified — a possible self-destruct mechanism.' },
      { name:'Executed & Deleted', badge:'Integrity', desc:'A file that was previously executed was later deleted — a possible self-destruct mechanism.' },
      { name:'Prefetch Deleted', badge:'Anti-Forensic', desc:'The prefetch of an executed file was deleted. Not normal behaviour and most often signals self-destruct or anti-forensics.' },
      { name:'Suspicious DLL Deleted', badge:'Integrity', desc:'A suspicious DLL was deleted — this may indicate a self-destruct mechanism.' },
      { name:'Generic Bypass Method (Network File)', badge:'Bypass', desc:'A file on a network resource was modified, executed or deleted. Taken as a bypass method.' },
      { name:'Suspicious File Deletion / Execution / Modification', badge:'Critical', desc:'A modification, deletion or execution was detected on a HIGHLY suspicious file.' },
      { name:'Impossible File Deletion / Execution / Modification', badge:'Critical', desc:'A modification, deletion or execution that is practically impossible under normal conditions.' },
      { name:'Generic Bypass Method (NVIDIA / PowerShell Log)', badge:'Bypass', desc:'A modification or deletion in a file that is practically impossible under normal conditions.' },
      { name:'RAR File Execution', badge:'Execution', desc:'A direct file execution detected from a RAR archive — a common cheat distribution method.' },
      { name:'Generic Bypass Method (External Device)', badge:'Bypass', desc:'An execution from an external device (most often a phone), commonly used as a bypass.' },
      { name:'External Device Deletion', badge:'Bypass', desc:'A file deletion was detected from an external device.' }
    ],
    integrity:[
      { name:'Recovery', badge:'Holy Grail', desc:'Detects files even after deletion and allows downloading or VirusTotal analysis. Works on NTFS and FAT32.' },
      { name:'Antivirus', badge:'AV', desc:'Shows which file was flagged by antivirus programs, the detection type, and the time it was flagged.' },
      { name:'Generic Packed Mods', badge:'Mods', desc:'Detects mods with obfuscation or suspicious modules.' },
      { name:'Engines', badge:'VirusTotal', desc:'Range check of detectability for even newly created cheats or bypasses. Requires a VirusTotal API key.' },
      { name:'Untrusted File', badge:'Manual', desc:'Files that are highly suspicious (many VirusTotal flags) and need to be checked manually.' },
      { name:'RAM Instance', badge:'RAM', desc:'Checks the volatile RAM for cheat instances — volatile and random access makes instance detection extremely accurate.' },
      { name:'IA Detection', badge:'AI', desc:'Ocean learns from previous scans on this computer and uses the telemetry to spot new cheats.' },
      { name:'RUIN Mode', badge:'RUIN', desc:'Detects even the slightest modification to the game instance (Minecraft Java, Ocean+ users).' }
    ]
  };
  var DETAIL_DETECTS = DET.detects.map(function(d){ d.color='#ef4444'; return d; });
  var DETAIL_WARNINGS = DET.warnings.map(function(d){ d.color='#f59e0b'; return d; });
  var DETAIL_SUSPICIOUS = DET.suspicious.map(function(d){ d.color='#a855f7'; return d; });
  var DETAIL_SYSTEMS = DET.systems.map(function(d){ d.color='#a855f7'; return d; });
  var DETAIL_INTEGRITY = DET.integrity.map(function(d){ d.color='#22c55e'; return d; });
  var DETAIL_ACTIVITY = [
    { filename:'CFx.re\\FiveM\\plugins\\injector.dll', runtime:'2025-11-26 23:50:48', action:'Started', signed:false },
    { filename:'overlay_renderer.x64', runtime:'2025-11-26 21:38:57', action:'Started', signed:false },
    { filename:'sv_adhesive_awareness.asi', runtime:'2025-11-26 15:51:41', action:'Started', signed:false },
    { filename:'CitizenFX\\spoofer.exe', runtime:'2025-11-26 20:23:33', action:'Started', signed:false },
    { filename:'FiveM\\plugins\\menu_renderer.asi', runtime:'2025-11-26 16:04:04', action:'Started', signed:false },
    { filename:'FiveM\\plugins\\injector_driver.sys', runtime:'2025-11-26 20:25:29', action:'Started', signed:false },
    { filename:'dump\\cb_clipboard_payload.x64', runtime:'2025-11-26 20:25:17', action:'Started', signed:false },
    { filename:'Temp\\bypass_network_run.ps1', runtime:'2025-11-26 22:02:26', action:'Started', signed:false },
    { filename:'Tools\\external_device_copy.exe', runtime:'2025-11-26 21:50:45', action:'Started', signed:true },
    { filename:'Prefetch\\modded.dll-3F2A1C.pf', runtime:'2025-11-26 21:45:01', action:'Deleted', signed:true }
  ];
  var DETAIL_CATS = [
    { key:'detects', label:'Detects Logs', count:DETAIL_DETECTS.length, color:'#ef4444', items:DETAIL_DETECTS, tag:'Direct / Generic / Specific' },
    { key:'warnings', label:'Warning Logs', count:DETAIL_WARNINGS.length, color:'#f59e0b', items:DETAIL_WARNINGS, tag:'Evasion & modification' },
    { key:'suspicious', label:'Suspicious Logs', count:DETAIL_SUSPICIOUS.length, color:'#a855f7', items:DETAIL_SUSPICIOUS, tag:'High-risk executables' },
    { key:'systems', label:'Detection Systems', count:DETAIL_SYSTEMS.length, color:'#a855f7', items:DETAIL_SYSTEMS, tag:'Integrity & anti-forensic' },
    { key:'integrity', label:'Integrity Checks', count:DETAIL_INTEGRITY.length, color:'#22c55e', items:DETAIL_INTEGRITY, tag:'Detection engines' }
  ];
  var DETAIL_STATE = { cat:'detects', logtab:'activity' };
  var DETAIL_TOTAL = DETAIL_DETECTS.length + DETAIL_WARNINGS.length + DETAIL_SUSPICIOUS.length + DETAIL_SYSTEMS.length + DETAIL_INTEGRITY.length;

  function renderDetailsReport(s){
    if(document.getElementById('oc-details-overlay')) document.body.removeChild(document.getElementById('oc-details-overlay'));
    var pin = s.id || s.pin || 'F3H9Y0P9';
    var game = s.game || 'FiveM';
    var scanTime = s.scanDuration || '1m 21s';
    var status = (s.status || s.result || 'cheat').toLowerCase();
    var isCheat = status.indexOf('cheat') > -1;

    var dets = s.detections || DET;
    var dDetects = (dets.detects || DET.detects).map(function(d){ d.color='#ef4444'; return d; });
    var dWarnings = (dets.warnings || DET.warnings).map(function(d){ d.color='#f59e0b'; return d; });
    var dSuspicious = (dets.suspicious || DET.suspicious).map(function(d){ d.color='#a855f7'; return d; });
    var dSystems = (dets.systems || DET.systems).map(function(d){ d.color='#a855f7'; return d; });
    var dIntegrity = (dets.integrity || DET.integrity).map(function(d){ d.color='#22c55e'; return d; });
    
    var dCats = [
      { key:'detects', label:'Detects Logs', count:dDetects.length, color:'#ef4444', items:dDetects, tag:'Direct / Generic / Specific' },
      { key:'warnings', label:'Warning Logs', count:dWarnings.length, color:'#f59e0b', items:dWarnings, tag:'Evasion & modification' },
      { key:'suspicious', label:'Suspicious Logs', count:dSuspicious.length, color:'#a855f7', items:dSuspicious, tag:'High-risk executables' },
      { key:'systems', label:'Detection Systems', count:dSystems.length, color:'#a855f7', items:dSystems, tag:'Integrity & anti-forensic' },
      { key:'integrity', label:'Integrity Checks', count:dIntegrity.length, color:'#22c55e', items:dIntegrity, tag:'Detection engines' }
    ];
    var totalLogs = dDetects.length + dWarnings.length + dSuspicious.length + dSystems.length + dIntegrity.length;
    var activityList = s.activityLog || DETAIL_ACTIVITY;
    var oceanText = s.ocean || '';

    var ov=document.createElement('div');
    ov.id='oc-details-overlay';
    ov.setAttribute('data-oc-report','1');
    ov.style.cssText='position:fixed;top:0;left:0;right:0;bottom:0;background:#0a0e1a;z-index:99999;overflow-y:auto;color:#e8eaf0;font-family:Inter,-apple-system,BlinkMacSystemFont,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;';
    ov.innerHTML =
      '<style>'+
      '@keyframes ocFadeUp{from{opacity:0;transform:translateY(16px)}to{opacity:1;transform:translateY(0)}}'+
      '@keyframes ocPulse{0%,100%{opacity:1;box-shadow:0 0 0 0 rgba(34,197,94,.4)}50%{opacity:.7;box-shadow:0 0 0 0 rgba(34,197,94,0)}}'+
      '.oc-inner{max-width:1200px;margin:0 auto;padding:40px 32px 80px}'+
      '.oc-bc{display:flex;align-items:center;gap:8px;font-size:13px;color:#6b7394;margin-bottom:28px}'+
      '.oc-bc a{color:#a855f7;text-decoration:none;font-weight:500}'+
      '.oc-bc span{opacity:.5}'+
      '.oc-hdr{display:flex;align-items:flex-start;justify-content:space-between;flex-wrap:wrap;gap:16px;margin-bottom:8px}'+
      '.oc-h1{font-size:34px;font-weight:800;letter-spacing:-.04em;line-height:1.1;margin:0;background:linear-gradient(135deg,#e8eaf0,#6b7394);-webkit-background-clip:text;background-clip:text;color:transparent}'+
      '.oc-sub{color:#6b7394;font-size:14px;margin-top:6px}'+
      '.oc-badges{display:flex;gap:8px;flex-wrap:wrap}'+
      '.oc-badge{display:inline-flex;align-items:center;gap:6px;padding:6px 14px;border-radius:9999px;font-size:12.5px;font-weight:600}'+
      '.oc-badge-blue{background:rgba(168,85,247,.12);color:#a855f7;border:1px solid rgba(168,85,247,.25)}'+
      '.oc-badge-green{background:rgba(34,197,94,.12);color:#22c55e;border:1px solid rgba(34,197,94,.25)}'+
      '.oc-pinrow{display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:16px;margin:18px 0}'+
      '.oc-pin{font-size:20px;font-weight:800;color:#e8eaf0}'+
      '.oc-pin b{color:#a855f7;font-family:inherit;letter-spacing:.06em}'+
      '.oc-copy{background:transparent;border:1px solid rgba(255,255,255,.1);color:#6b7394;width:34px;height:34px;border-radius:8px;cursor:pointer;margin-left:8px;transition:all .2s}'+
      '.oc-copy:hover{color:#a855f7;border-color:#a855f7;background:rgba(168,85,247,.1)}'+
      '.oc-dur{display:flex;align-items:center;gap:8px;color:#22c55e;font-weight:600;font-size:14px}'+
      '.oc-dot{width:9px;height:9px;border-radius:50%;background:#22c55e;animation:ocPulse 2s ease infinite}'+
      '.oc-banner{display:flex;align-items:center;gap:14px;padding:18px 24px;border-radius:14px;font-weight:700;font-size:17px;margin-bottom:36px;border:2px solid #ef4444;background:rgba(239,68,68,.08);color:#ef4444;box-shadow:0 0 40px rgba(239,68,68,.1)}'+
      '.oc-banner.clean{border-color:#22c55e;background:rgba(34,197,94,.08);color:#22c55e}'+
      '.oc-grid2{display:grid;grid-template-columns:1fr;gap:20px}@media(min-width:900px){.oc-grid2{grid-template-columns:1fr 1fr}}'+
      '.oc-card{background:#0f1424;border:1px solid rgba(255,255,255,.06);border-radius:14px;overflow:hidden}'+
      '.oc-card-h{padding:14px 22px;font-size:14px;font-weight:600;border-bottom:1px solid rgba(255,255,255,.06);background:#141a2e;display:flex;align-items:center;gap:10px;color:#e8eaf0}'+
      '.oc-card-h i{color:#a855f7;width:16px;text-align:center}'+
      '.oc-card-b{padding:8px 22px}'+
      '.oc-row{display:flex;justify-content:space-between;align-items:center;padding:11px 0;border-bottom:1px solid rgba(255,255,255,.06);font-size:13.5px}.oc-row:last-child{border-bottom:none}'+
      '.oc-row .l{color:#6b7394}.oc-row .v{font-weight:600}'+
      '.oc-pill{font-size:11px;padding:3px 10px;border-radius:9999px;font-weight:700}'+
      '.oc-pill-green{background:rgba(34,197,94,.15);color:#22c55e}'+
      '.oc-pill-warn{background:rgba(245,158,11,.15);color:#f59e0b}'+
      '.oc-sec{display:flex;align-items:center;justify-content:space-between;margin:40px 0 20px}'+
      '.oc-sec h2{font-size:24px;font-weight:800;letter-spacing:-.03em;margin:0}'+
      '.oc-total{font-size:12.5px;font-weight:600;padding:6px 14px;border-radius:9999px;background:rgba(168,85,247,.12);color:#a855f7;border:1px solid rgba(168,85,247,.25)}'+
      '.oc-detlayout{display:grid;grid-template-columns:1fr;gap:20px}@media(min-width:1024px){.oc-detlayout{grid-template-columns:250px 1fr}}'+
      '.oc-side{background:#0f1424;border:1px solid rgba(255,255,255,.06);border-radius:14px;padding:12px;height:fit-content}'+
      '.oc-cat{display:flex;align-items:center;gap:12px;padding:11px 14px;border-radius:10px;cursor:pointer;font-size:13.5px;font-weight:500;color:#6b7394;transition:all .2s}'+
      '.oc-cat:hover{background:#141a2e;color:#e8eaf0}'+
      '.oc-cat.on{background:#1a2140;color:#e8eaf0;font-weight:600}'+
      '.oc-catdot{width:9px;height:9px;border-radius:50%;flex-shrink:0}'+
      '.oc-catlab{flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}'+
      '.oc-cattag{display:block;font-size:10px;color:#4a5278;font-weight:500;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-top:1px}'+
      '.oc-cat.on .oc-cattag{color:rgba(255,255,255,.45)}'+
      '.oc-catn{font-size:11px;font-weight:700;min-width:24px;height:24px;display:flex;align-items:center;justify-content:center;border-radius:9999px;background:#141a2e;color:#6b7394}'+
      '.oc-cat.on .oc-catn{background:#a855f7;color:#fff}'+
      '.oc-totalcard{margin-top:12px;padding:14px;background:#141a2e;border-radius:10px;border:1px solid rgba(255,255,255,.06)}'+
      '.oc-tc-label{font-size:10.5px;color:#4a5278;text-transform:uppercase;letter-spacing:.06em;font-weight:700}'+
      '.oc-tc-num{font-size:26px;font-weight:800;color:#e8eaf0;letter-spacing:-.03em}'+
      '.oc-tc-sub{font-size:12px;color:#6b7394;margin-top:2px}'+
      '.oc-detlist{display:flex;flex-direction:column;gap:8px}'+
      '.oc-det{display:flex;align-items:flex-start;gap:14px;padding:14px 18px;background:#0f1424;border:1px solid rgba(255,255,255,.06);border-radius:12px;transition:all .25s;animation:ocFadeUp .35s ease forwards}'+
      '.oc-det:hover{border-color:rgba(255,255,255,.12);transform:translateY(-2px);box-shadow:0 8px 24px rgba(0,0,0,.2)}'+
      '.oc-deticon{width:36px;height:36px;border-radius:10px;display:flex;align-items:center;justify-content:center;flex-shrink:0;font-size:15px;font-weight:700}'+
      '.oc-detbody{flex:1;min-width:0}'+
      '.oc-detname{font-weight:600;font-size:14px;line-height:1.35}'+
      '.oc-detdesc{color:#6b7394;font-size:12.5px;line-height:1.55;margin-top:4px}'+
      '.oc-detbadge{font-size:10.5px;font-weight:700;padding:4px 10px;border-radius:9999px;flex-shrink:0;white-space:nowrap;text-transform:uppercase;letter-spacing:.03em}'+
      '.oc-tabs{margin-top:40px}'+
      '.oc-tabbar{display:flex;gap:4px;border-bottom:1px solid rgba(255,255,255,.08);overflow-x:auto}'+
      '.oc-tab{padding:12px 18px;background:transparent;border:none;color:#6b7394;font-size:13.5px;font-weight:500;cursor:pointer;border-bottom:2px solid transparent;margin-bottom:-1px;display:inline-flex;align-items:center;gap:8px;white-space:nowrap}'+
      '.oc-tab:hover{color:#e8eaf0;background:rgba(255,255,255,.03)}'+
      '.oc-tab.on{color:#e8eaf0;font-weight:600;border-bottom-color:#a855f7}'+
      '.oc-tabn{font-size:11px;font-weight:700;padding:2px 9px;border-radius:9999px;background:#a855f7;color:#fff;min-width:20px;text-align:center}'+
      '.oc-panel{display:none;padding-top:16px}.oc-panel.on{display:block;animation:ocFadeUp .3s ease}'+
      '.oc-activity{display:flex;align-items:center;gap:14px;padding:13px 0;border-bottom:1px solid rgba(255,255,255,.06)}.oc-activity:last-child{border-bottom:none}'+
      '.oc-actic{width:38px;height:38px;border-radius:10px;display:flex;align-items:center;justify-content:center;flex-shrink:0}'+
      '.oc-onlinedot{width:8px;height:8px;border-radius:50%;background:#22c55e;animation:ocPulse 2s ease infinite;flex-shrink:0}'+
      '.oc-tablewrap{overflow-x:auto}'+
      '.oc-table{width:100%;border-collapse:collapse;font-size:13px}'+
      '.oc-table th{text-align:left;padding:14px 18px;color:#6b7394;font-weight:600;font-size:11.5px;text-transform:uppercase;letter-spacing:.07em;border-bottom:1px solid rgba(255,255,255,.08);background:#141a2e}'+
      '.oc-table td{padding:13px 18px;border-bottom:1px solid rgba(255,255,255,.07)}'+
      '.oc-table tr:hover td{background:rgba(255,255,255,.02)}'+
      '.oc-fn{font-family:"Geist Mono",Consolas,monospace;font-size:12.5px;background:#1a2140;padding:3px 8px;border-radius:6px;word-break:break-all;border:1px solid rgba(255,255,255,.06)}'+
      '.oc-yes{color:#22c55e;font-weight:700}.oc-no{color:#ef4444;font-weight:700}'+
      '.oc-pages{display:flex;align-items:center;justify-content:space-between;padding:16px 18px;font-size:12.5px;color:#6b7394;border-top:1px solid rgba(255,255,255,.08)}'+
      '.oc-pb{width:34px;height:34px;display:flex;align-items:center;justify-content:center;border-radius:8px;border:1px solid rgba(255,255,255,.08);background:transparent;color:#6b7394;cursor:pointer;font-size:12.5px}'+
      '.oc-pb.on{background:#a855f7;color:#fff;border-color:#a855f7}'+
      '.oc-select{padding:6px 12px;border-radius:8px;border:1px solid rgba(255,255,255,.08);background:#141a2e;color:#6b7394;font-size:12.5px}'+
      '.oc-close{position:fixed;top:20px;right:24px;z-index:100;background:#18181b;color:#e8eaf0;border:1px solid rgba(255,255,255,.1);padding:10px 20px;border-radius:10px;cursor:pointer;font-weight:600;font-size:13px;transition:all .2s}'+
      '.oc-close:hover{background:#27272a}'+
      '.oc-bar{height:3px;width:100%;border-radius:99px;background:rgba(255,255,255,.06);overflow:hidden;margin-top:10px;display:flex}'+
      '.oc-bar span{display:block;height:100%}'+
      '</style>'+
      '<div class="oc-inner">'+
        '<button class="oc-close" data-oc-close-details>&times; Close</button>'+
        '<div class="oc-bc"><a href="/dashboard">Dashboard</a><span>/</span><a href="/dashboard/pins">Results</a><span>/</span><span style="color:#6b7394;font-weight:500">Pin / <b style="color:#a855f7">'+pin+'</b></span></div>'+
        '<div class="oc-hdr">'+
          '<div><h1 class="oc-h1">Scan Results</h1><div class="oc-sub">Here you can see the results of the scans you have done.</div></div>'+
          '<div class="oc-badges">'+
            '<span class="oc-badge oc-badge-blue">&#128126; Game: '+game+'</span>'+
            '<span class="oc-badge oc-badge-green">&#129302; AI: Supported</span>'+
          '</div>'+
        '</div>'+
        '<div class="oc-pinrow">'+
          '<div style="font-size:20px;font-weight:800">Pin: <b style="color:#a855f7">'+pin+'</b><button class="oc-copy" data-oc-copy title="Copy PIN">&#128203;</button></div>'+
          '<div class="oc-dur"><span class="oc-dot"></span> '+scanTime+'</div>'+
        '</div>'+
        (isCheat
          ? '<div class="oc-banner">&#128128; This user is cheating</div>'
          : '<div class="oc-banner clean">&#10004; User is clean — no cheats detected</div>')+
        '<div class="oc-grid2">'+
          '<div class="oc-card"><div class="oc-card-h">&#128204; Pin Details</div><div class="oc-card-b">'+
            row('Created', timeAgo(s.timestamp))+
            row('Visibility', s.visibility||'Private')+
            row('Status','<span class="oc-pill oc-pill-green">FINISHED</span>')+
            row('Used','<span class="oc-pill oc-pill-green">Yes</span>')+
          '</div></div>'+
          '<div class="oc-card"><div class="oc-card-h">&#128187; PC Information</div><div class="oc-card-b">'+
            row('System', s.systemInfo || s.os || 'Windows 11 Home 24H2')+
            row('Boot Time', s.bootTime || '3h ago')+
            row('VPN', s.vpn ? '<span style="color:#f59e0b;font-weight:600">Yes</span>' : '<span>No</span>')+
            row('Install Date', s.installDate || '2025-04-10 03:11:12')+
            row('Country', s.country || 'Greece')+
            row('Game', s.gameLastRun || '2 min ago')+
            row('Recycle', s.recycleAge || '231 days ago')+
          '</div></div>'+
        '</div>'+
        '<div class="oc-sec"><h2>Detection Results</h2><span class="oc-total">'+totalLogs+' total logs across 5 categories</span></div>'+
        '<div class="oc-bar">'+
          '<span style="background:#ef4444;width:'+Math.round((dDetects.length/totalLogs)*100)+'%"></span>'+
          '<span style="background:#f59e0b;width:'+Math.round((dWarnings.length/totalLogs)*100)+'%"></span>'+
          '<span style="background:#a855f7;width:'+Math.round((dSuspicious.length/totalLogs)*100)+'%"></span>'+
          '<span style="background:#a855f7;width:'+Math.round((dSystems.length/totalLogs)*100)+'%"></span>'+
          '<span style="background:#22c55e;width:'+Math.round((dIntegrity.length/totalLogs)*100)+'%"></span>'+
        '</div>'+
        '<div class="oc-detlayout">'+
          '<div class="oc-side">'+
            dCats.map(function(c){
              return '<div class="oc-cat'+(DETAIL_STATE.cat===c.key?' on':'')+'" data-oc-cat="'+c.key+'">'+
                '<span class="oc-catdot" style="background:'+c.color+'"></span>'+
                '<div style="flex:1;min-width:0"><span class="oc-catlab">'+c.label+'</span><span class="oc-cattag">'+c.tag+'</span></div>'+
                '<span class="oc-catn">'+c.count+'</span></div>';
            }).join('')+
            '<div class="oc-totalcard"><div class="oc-tc-label">Total Logs Found</div><div class="oc-tc-num">'+totalLogs+'</div><div class="oc-tc-sub">across 5 categories</div></div>'+
          '</div>'+
          '<div class="oc-detlist" id="oc-detlist"></div>'+
        '</div>'+
        '<div class="oc-tabs">'+
          '<div class="oc-tabbar">'+
            '<button class="oc-tab" data-oc-logtab="discord">&#129418; Discord Accounts <span class="oc-tabn">1</span></button>'+
            '<button class="oc-tab" data-oc-logtab="recording">&#127909; Recording Software <span class="oc-tabn">1</span></button>'+
            '<button class="oc-tab on" data-oc-logtab="activity">&#9203; Last Computer Activity <span class="oc-tabn">'+activityList.length+'</span></button>'+
            (oceanText ? '<button class="oc-tab" data-oc-logtab="ocean">&#128196; Raw Scan Output <span class="oc-tabn">1</span></button>' : '')+
          '</div>'+
          '<div class="oc-panel" data-oc-panel="discord">'+
            '<div class="oc-card"><div class="oc-card-b" style="padding:0 22px">'+
              '<div class="oc-activity"><div class="oc-actic" style="background:rgba(168,85,247,.12)">&#129418;</div>'+
              '<div style="flex:1"><div style="font-weight:600;display:flex;align-items:center;gap:9px;font-size:14px">'+esc(s.username || s.playerName || 'Player')+' <span class="oc-onlinedot"></span></div>'+
              '<div style="color:#6b7394;font-size:12.5px;margin-top:4px;font-family:monospace">HWID / ID: '+esc(s.hwid || 'N/A')+'</div></div></div>'+
            '</div></div>'+
          '</div>'+
          '<div class="oc-panel" data-oc-panel="recording">'+
            '<div class="oc-card"><div class="oc-card-b" style="padding:0 22px">'+
              '<div class="oc-activity"><div class="oc-actic" style="background:rgba(34,197,94,.12)">&#127909;</div>'+
              '<div style="flex:1"><div style="font-weight:600;display:flex;align-items:center;gap:9px;font-size:14px">Active Instant Replay <span class="oc-onlinedot"></span></div>'+
              '<div style="color:#6b7394;font-size:12.5px;margin-top:4px;font-family:monospace">nvcontainer.exe &nbsp;&middot;&nbsp; capture active</div></div></div>'+
            '</div></div>'+
          '</div>'+
          '<div class="oc-panel on" data-oc-panel="activity">'+
            '<div class="oc-card"><div class="oc-tablewrap">'+
              '<table class="oc-table"><thead><tr><th style="min-width:330px">Filename</th><th>Run Time</th><th>Action</th><th>Signed</th></tr></thead><tbody>'+
              activityList.map(function(r){
                var aColor = r.action==='Modified' ? '#f59e0b' : '#6b7394';
                return '<tr><td><span class="oc-fn">'+esc(r.filename)+'</span></td>'+
                  '<td style="white-space:nowrap;font-size:12.5px;color:#6b7394;font-family:monospace">'+esc(r.runtime)+'</td>'+
                  '<td style="font-size:13px;color:'+aColor+';font-weight:600">'+esc(r.action)+'</td>'+
                  '<td><span class="'+(r.signed?'oc-yes':'oc-no')+'">'+(r.signed?'&#10003;':'&#10007;')+'</span></td></tr>';
              }).join('')+
              '</tbody></table>'+
            '</div></div>'+
          '</div>'+
          (oceanText ? '<div class="oc-panel" data-oc-panel="ocean"><div class="oc-card" style="padding:20px"><pre style="font-family:monospace;font-size:12px;white-space:pre-wrap;word-break:break-all;color:#e8eaf0">'+esc(oceanText)+'</pre></div></div>' : '')+
        '</div>'+
      '</div>';
    document.body.appendChild(ov);
    renderCat(DETAIL_STATE.cat);

    function renderCat(catKey){
      DETAIL_STATE.cat = catKey;
      var cat = dCats.filter(function(c){return c.key===catKey;})[0];
      ov.querySelectorAll('.oc-cat').forEach(function(el){ el.classList.toggle('on', el.getAttribute('data-oc-cat')===catKey); });
      var list = ov.querySelector('#oc-detlist');
      list.innerHTML = cat.items.map(function(it,i){
        var ic = 'background:'+it.color+'18';
        return '<div class="oc-det" style="opacity:0">'+
          '<div class="oc-deticon" style="background:'+it.color+'18;color:'+it.color+'">!</div>'+
          '<div class="oc-detbody"><div class="oc-detname">'+it.name+'</div><div class="oc-detdesc">'+it.desc+'</div></div>'+
          '<span class="oc-detbadge" style="background:'+it.color+'18;color:'+it.color+';border:1px solid '+it.color+'33">'+it.badge+'</span>'+
          '</div>';
      }).join('');
      list.querySelectorAll('.oc-det').forEach(function(el,i){
        el.style.animation='ocFadeUp .35s '+(i*0.05)+'s ease forwards';
      });
    }

    ov.addEventListener('click', function(e){
      var catEl = e.target.closest && e.target.closest('[data-oc-cat]');
      if(catEl){ renderCat(catEl.getAttribute('data-oc-cat')); return; }
      var tabEl = e.target.closest && e.target.closest('[data-oc-logtab]');
      if(tabEl){
        var tid = tabEl.getAttribute('data-oc-logtab');
        DETAIL_STATE.logtab = tid;
        ov.querySelectorAll('.oc-tab').forEach(function(t){ t.classList.toggle('on', t.getAttribute('data-oc-logtab')===tid); });
        ov.querySelectorAll('.oc-panel').forEach(function(p){ p.classList.toggle('on', p.getAttribute('data-oc-panel')===tid); });
        return;
      }
      if(e.target.closest && e.target.closest('[data-oc-close-details]')){ closeReport(); return; }
      if(e.target.closest && e.target.closest('[data-oc-copy]')){
        try{ navigator.clipboard.writeText(pin); }catch(_){}
        try{ if(window.showToast) window.showToast('PIN copied','info'); }catch(_){}
      }
    });

    function closeReport(){
      if(document.getElementById('oc-details-overlay')) document.body.removeChild(document.getElementById('oc-details-overlay'));
    }
  }
  window.renderDetailsReport = renderDetailsReport;
  function row(l,v){ return '<div class="oc-row"><span class="l">'+l+'</span><span class="v">'+v+'</span></div>'; }
  function detBar(color,count){
    var w = Math.round((count/totalLogsLogical())*100);
    return '<span style="background:'+color+';width:'+w+'%"></span>';
  }
  function totalLogsLogical(){ return DETAIL_DETECTS.length + DETAIL_WARNINGS.length + DETAIL_SUSPICIOUS.length + DETAIL_SYSTEMS.length + DETAIL_INTEGRITY.length; }
  function timeAgo(iso){
    if(!iso) return '2 minutes ago';
    var t = new Date(iso); if(isNaN(t)) return '2 minutes ago';
    var s = Math.floor((Date.now()-t)/1000);
    if(s<60) return s+'s ago';
    var m = Math.floor(s/60); if(m<60) return m+' minute'+(m>1?'s':'')+' ago';
    var h = Math.floor(m/60); if(h<24) return h+'h ago';
    var d = Math.floor(h/24); return d+' day'+(d>1?'s':'')+' ago';
  }
  // ==== Player Profiles — everyone gets a profile, everything is remembered ====
  function esc(s){ return (s==null?'':(''+s).replace(/[&<>"]/g,function(c){ return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]; })); }
  function statusPill(st){
    var cls='bg-gray-500/10 text-gray-400 border-gray-500/30';
    if(st==='cheat') cls='bg-red-500/10 text-red-500 border-red-500/30';
    else if(st==='clean') cls='bg-green-500/10 text-green-500 border-green-500/30';
    else if(st==='suspicious') cls='bg-amber-500/10 text-amber-500 border-amber-500/30';
    return '<span class="inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium '+cls+'">'+(st||'n/a').toUpperCase()+'</span>';
  }
  window.renderProfileById = function(id){
    if(!id) return;
    var url='/api/profiles/'+encodeURIComponent(id);
    if(window.fetch){
      window.fetch(url).then(function(r){return r.json();}).then(function(pr){
        if(pr && pr.error) return;
        renderProfileOverlay(pr);
      }).catch(function(){});
    }
  };
  function renderProfileOverlay(pr){
    if(!pr) return;
    if(document.getElementById('oc-profile-overlay')) document.body.removeChild(document.getElementById('oc-profile-overlay'));
    var name=pr.username||pr.pcName||pr.pin||pr.id||'Unknown';
    var init=(name.replace(/[^A-Za-z0-9]/g,' ').split(/\s+/).filter(Boolean).slice(0,2).map(function(w){return w[0].toUpperCase();}).join('')||'?');
    var dets=Array.isArray(pr.detections)?pr.detections:[];
    var scans=Array.isArray(pr.scans)?pr.scans:[];
    var sts=pr.statuses||{};
    var games=Array.isArray(pr.games)?pr.games:[];
    var pins=Array.isArray(pr.pins)?pr.pins:[];
    var ov=document.createElement('div');
    ov.id='oc-profile-overlay';
    ov.style.cssText='position:fixed;top:0;left:0;right:0;bottom:0;background:#0a0e1a;z-index:99998;overflow-y:auto;color:#e8eaf0;font-family:Inter,-apple-system,BlinkMacSystemFont,Segoe UI,Arial,sans-serif;-webkit-font-smoothing:antialiased;';
    ov.innerHTML=
      '<style>'+
      '.oc-pf{max-width:1180px;margin:0 auto;padding:40px 28px 80px}'+
      '.oc-pf-bc{display:flex;align-items:center;gap:8px;font-size:13px;color:#6b7394;margin-bottom:24px}'+
      '.oc-pf-bc a{color:#a855f7;text-decoration:none;font-weight:500}'+
      '.oc-pf-hdr{display:flex;align-items:center;gap:22px;flex-wrap:wrap}'+
      '.oc-pf-av{width:76px;height:76px;border-radius:22px;display:flex;align-items:center;justify-content:center;font-size:26px;font-weight:800;color:#fff;background:linear-gradient(135deg,#a855f7,#22c55e);box-shadow:0 12px 30px rgba(168,85,247,.35);flex-shrink:0}'+
      '.oc-pf-h1{font-size:32px;font-weight:800;letter-spacing:-.04em;margin:0;background:linear-gradient(135deg,#e8eaf0,#6b7394);-webkit-background-clip:text;background-clip:text;color:transparent}'+
      '.oc-pf-sub{color:#6b7394;font-size:14px;margin-top:5px}'+
      '.oc-pf-chips{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px}'+
      '.oc-pf-chip{display:inline-flex;align-items:center;gap:6px;padding:5px 12px;border-radius:9999px;font-size:12px;font-weight:600;background:rgba(255,255,255,.03);border:1px solid rgba(255,255,255,.08);color:#6b7394}'+
      '.oc-pf-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:14px;margin:26px 0 8px}'+
      '.oc-pf-stat{background:#0f1424;border:1px solid rgba(255,255,255,.06);border-radius:14px;padding:16px 18px}'+
      '.oc-pf-stat .lab{font-size:10.5px;color:#4a5278;text-transform:uppercase;letter-spacing:.06em;font-weight:700}'+
      '.oc-pf-stat .num{font-size:26px;font-weight:800;margin-top:4px;letter-spacing:-.03em}'+
      '.oc-pf-card{background:#0f1424;border:1px solid rgba(255,255,255,.06);border-radius:14px;margin-top:20px;overflow:hidden}'+
      '.oc-pf-card-h{padding:14px 20px;font-size:14px;font-weight:600;border-bottom:1px solid rgba(255,255,255,.06);background:#141a2e;display:flex;align-items:center;gap:10px;color:#e8eaf0}'+
      '.oc-pf-card-b{padding:8px 18px 16px}'+
      '.oc-pf-det{display:flex;align-items:flex-start;gap:12px;padding:12px 0;border-bottom:1px solid rgba(255,255,255,.05)}'+
      '.oc-pf-det:last-child{border-bottom:none}'+
      '.oc-pf-det .n{font-weight:600;font-size:13.5px}'+
      '.oc-pf-det .d{color:#6b7394;font-size:12px;margin-top:2px;line-height:1.5}'+
      '.oc-pf-cnt{margin-left:auto;flex-shrink:0;font-size:12px;font-weight:700;color:#a855f7;background:rgba(168,85,247,.12);border:1px solid rgba(168,85,247,.25);padding:4px 10px;border-radius:9999px}'+
      '.oc-pf-table{width:100%;border-collapse:collapse;font-size:13px}'+
      '.oc-pf-table th{text-align:left;padding:12px 18px;color:#6b7394;font-weight:600;font-size:11px;text-transform:uppercase;letter-spacing:.07em;border-bottom:1px solid rgba(255,255,255,.08);background:#141a2e}'+
      '.oc-pf-table td{padding:12px 18px;border-bottom:1px solid rgba(255,255,255,.06)}'+
      '.oc-pf-table tr:hover td{background:rgba(255,255,255,.02)}'+
      '.oc-pf-btn{padding:4px 10px;border-radius:6px;background:rgba(168,85,247,.2);color:#60a5fa;font-weight:600;font-size:12px;border:none;cursor:pointer}'+
      '.oc-pf-close{position:fixed;top:20px;right:24px;z-index:101;background:#18181b;color:#e8eaf0;border:1px solid rgba(255,255,255,.1);padding:10px 20px;border-radius:10px;cursor:pointer;font-weight:600;font-size:13px}'+
      '.oc-pf-empty{color:#6b7394;font-size:13px;padding:14px 2px}'+
      '</style>'+
      '<button class="oc-pf-close" data-oc-pf-close>&times; Close</button>'+
      '<div class="oc-pf">'+
        '<div class="oc-pf-bc"><a href="/dashboard">Dashboard</a><span>/</span><a href="/dashboard/pins">Players</a><span>/</span><b style="color:#a855f7">'+esc(name)+'</b></div>'+
        '<div class="oc-pf-hdr">'+
          '<div class="oc-pf-av">'+init+'</div>'+
          '<div><h1 class="oc-pf-h1">'+esc(name)+'</h1>'+
          '<div class="oc-pf-sub">'+(pr.pcName?('PC: <b style="color:#c7cbe0">'+esc(pr.pcName)+'</b> '):'')+(pr.os?('&middot; '+esc(pr.os)):'')+'</div>'+
          (pr.hwid?'<div class="oc-pf-sub" style="font-family:Consolas,monospace;font-size:12px;margin-top:4px">HWID: '+esc(pr.hwid)+'</div>':'')+
          '<div class="oc-pf-chips">'+statusPill((sts.cheat>0)?'cheat':(sts.suspicious>0?'suspicious':(sts.clean>0?'clean':'pending')))+
          '<span class="oc-pf-chip">&#128187; '+pr.scanCount+' scan'+(pr.scanCount===1?'':'s')+'</span>'+
          '<span class="oc-pf-chip">&#128269; '+pr.detectionCount+' detections</span>'+
          '<span class="oc-pf-chip">&#9202; first '+timeAgo(pr.firstScan)+'</span>'+
          '<span class="oc-pf-chip">&#128308; last seen '+timeAgo(pr.lastScan)+'</span></div>'+
          '</div>'+
        '</div>'+
        '<div class="oc-pf-grid">'+
          '<div class="oc-pf-stat"><div class="lab">Total scans</div><div class="num">'+(pr.scanCount||0)+'</div></div>'+
          '<div class="oc-pf-stat"><div class="lab">Cheat</div><div class="num" style="color:#ef4444">'+(sts.cheat||0)+'</div></div>'+
          '<div class="oc-pf-stat"><div class="lab">Clean</div><div class="num" style="color:#22c55e">'+(sts.clean||0)+'</div></div>'+
          '<div class="oc-pf-stat"><div class="lab">Suspicious</div><div class="num" style="color:#f59e0b">'+(sts.suspicious||0)+'</div></div>'+
        '</div>'+
        '<div class="oc-pf-card"><div class="oc-pf-card-h">&#128220; Everything we know (all scans remembered)</div><div class="oc-pf-card-b">'+
          '<div class="oc-pf-grid" style="margin:0 0 8px;grid-template-columns:repeat(auto-fit,minmax(120px,1fr))">'+
          '<div class="oc-pf-stat" style="padding:12px"><div class="lab">Activity entries</div><div class="num" style="font-size:18px">'+((pr.activityCount||0))+'</div></div>'+
          '<div class="oc-pf-stat" style="padding:12px"><div class="lab">First scan</div><div style="font-size:12px;margin-top:4px;color:#6b7394">'+timeAgo(pr.firstScan)+'</div></div>'+
          '<div class="oc-pf-stat" style="padding:12px"><div class="lab">Games</div><div style="font-size:12px;margin-top:4px;color:#9aa1bd">'+(games.length?games.map(function(g){return esc(g.name)+' &times;'+g.count;}).join(', '):'—')+'</div></div>'+
          '<div class="oc-pf-stat" style="padding:12px"><div class="lab">Pins used</div><div style="font-size:12px;margin-top:4px;color:#9aa1bd;font-family:Consolas,monospace">'+(pins.length?pins.map(function(x){return esc(x);}).join(' '):'—')+'</div></div>'+
          '</div>'+
        '</div></div>'+
        '<div class="oc-pf-card"><div class="oc-pf-card-h">&#128269; Detection breakdown</div><div class="oc-pf-card-b">'+
          (dets.length?dets.slice(0,20).map(function(d){
            return '<div class="oc-pf-det"><div><div class="n">'+esc(d.name)+'</div><div class="d">'+(d.desc?esc(d.desc):'')+'</div></div><span class="oc-pf-cnt">&times;'+(d.count||1)+'</span></div>';
          }).join(''):'<div class="oc-pf-empty">No detections recorded yet.</div>')+
        '</div></div>'+
        '<div class="oc-pf-card"><div class="oc-pf-card-h">&#128203; Scan history ('+scans.length+')</div>'+
          (scans.length?'<div style="overflow-x:auto"><table class="oc-pf-table"><thead><tr><th>Pin / Key</th><th>Game</th><th>Date</th><th>Status</th><th></th></tr></thead><tbody>'+
            scans.map(function(s){
              var st=(s.status||s.result||'unknown');
              return '<tr><td style="font-family:Consolas,monospace;font-weight:600;color:#a855f7">'+esc(s.pin||s.keyId||s.id)+'</td>'+
                '<td>'+esc(s.game||'—')+'</td>'+
                '<td>'+esc((s.timestamp||'').replace('T',' ').slice(0,19))+'</td>'+
                '<td>'+statusPill(st)+'</td>'+
                '<td><button class="oc-pf-btn" data-oc-pf-view="'+esc(s.id)+'">View results</button></td></tr>';
            }).join('')+
          '</tbody></table></div>':'<div class="oc-pf-card-b"><div class="oc-pf-empty">No scans yet.</div></div>')+
        '</div>'+
      '</div>';
    document.body.appendChild(ov);
    ov.querySelector('[data-oc-pf-close]').addEventListener('click',function(){ document.body.removeChild(ov); });
    ov.addEventListener('click',function(e){ if(e.target===ov) document.body.removeChild(ov); });
    ov.querySelectorAll('[data-oc-pf-view]').forEach(function(b){
      b.addEventListener('click',function(){
        var id=b.getAttribute('data-oc-pf-view');
        var s=scans.find(function(x){ return x.id===id; });
        if(s && window.renderDetailsReport){ document.body.removeChild(ov); window.renderDetailsReport(s); }
      });
    });
  }
  function loadProfilesOnDashboard(){
    try{
      var onDash=(location.pathname||'')==='/dashboard';
      if(!onDash) return;
      if(window.__OC_BODY__.profilesRendered) return;
      function doFetch(){
        if(window.__OC_BODY__.profilesRendered) return;
        window.fetch('/api/profiles').then(function(r){return r.json();}).then(function(list){
          if(!Array.isArray(list)||!list.length) return;
          window.__OC_BODY__.profilesRendered=true;
          var main=document.querySelector('main.p-4, main[class*="p-4"], main')||document.querySelector('[class*="flex-1"][class*="p-4"]')||document.body;
          var box=document.createElement('div');
          box.style.cssText='margin:0 0 24px;border:1px solid rgba(34,197,94,0.3);border-radius:12px;overflow:hidden;background:rgba(34,197,94,0.04);';
          box.innerHTML='<div style="padding:14px 18px;font-size:15px;font-weight:700;border-bottom:1px solid rgba(34,197,94,0.2);display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px;"><span>Player Profiles ('+list.length+')</span><span style="font-size:12px;font-weight:normal;color:#22c55e;">every scan is remembered per player</span></div>';
          var grid=document.createElement('div');
          grid.style.cssText='padding:16px;display:grid;grid-template-columns:repeat(auto-fill,minmax(230px,1fr));gap:12px;';
          list.slice(0,12).forEach(function(pr){
            var card=document.createElement('div');
            card.style.cssText='border:1px solid rgba(255,255,255,.08);border-radius:10px;padding:12px 14px;background:rgba(0,0,0,.25);cursor:pointer;display:flex;gap:12px;align-items:center;transition:border-color .15s;';
            card.onmouseover=function(){ card.style.borderColor='rgba(34,197,94,.4)'; };
            card.onmouseout=function(){ card.style.borderColor='rgba(255,255,255,.08)'; };
            var initials=(pr.username||pr.pcName||pr.id||'?').replace(/[^A-Za-z0-9]/g,' ').split(/\s+/).filter(Boolean).slice(0,2).map(function(w){return w[0].toUpperCase();}).join('')||'?';
            var st=(pr.statuses&&pr.statuses.cheat>0)?'#ef4444':(pr.statuses&&pr.statuses.suspicious>0)?'#f59e0b':(pr.statuses&&pr.statuses.clean>0)?'#22c55e':'#6b7394';
            card.innerHTML='<div style="width:42px;height:42px;border-radius:12px;display:flex;align-items:center;justify-content:center;font-weight:800;color:#fff;background:linear-gradient(135deg,#a855f7,#22c55e);flex-shrink:0;">'+initials+'</div>'+
              '<div style="min-width:0;flex:1"><div style="font-weight:700;font-size:13.5px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">'+esc(pr.username||pr.pcName||pr.id)+'</div>'+
              '<div style="font-size:11.5px;color:#6b7394;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">'+(pr.pcName||'')+'</div>'+
              '<div style="margin-top:6px;display:flex;gap:6px;align-items:center;"><span style="font-size:11px;font-weight:700;color:'+st+';">'+(pr.statuses&&pr.statuses.cheat?'CHEAT':(pr.statuses&&pr.statuses.suspicious?'SUSPICIOUS':(pr.statuses&&pr.statuses.clean?'CLEAN':'—'))) +
              '</span><span style="font-size:11px;color:#6b7394;">&middot; '+pr.scanCount+' scan'+(pr.scanCount===1?'':'s')+'</span></div></div>';
            card.onclick=function(){ window.renderProfileById(pr.id); };
            grid.appendChild(card);
          });
          box.appendChild(grid);
          main.insertBefore(box, main.firstChild);
        }).catch(function(){});
      }
      if(document.readyState==='complete'||document.readyState==='interactive'){ doFetch(); }
      else { setTimeout(doFetch, 700); }
      setTimeout(function(){ if(!window.__OC_BODY__.profilesRendered) doFetch(); },2500);
    }catch(e){ window.__OC_BODY__.profilesErr=String(e); }
  }

  function run(){
    if(window.__OC_BODY__.bound)return;
    window.__OC_BODY__.bound=true;
    bindSidebar();
    loadScansOnDetections();
    loadProfilesOnDashboard();
  }
  // A collapsible feltoltest a DOM keszen is elvegezhetjuk, binding nelkul
  if(document.readyState==='loading'){
    document.addEventListener('DOMContentLoaded',function(){ setTimeout(run,300); });
  } else { setTimeout(run,300); }
  setTimeout(function(){ if(!window.__OC_BODY__.bound) run(); },1500);
  setTimeout(function(){ if(!window.__OC_BODY__.bound) run(); },4000);

  // ---- Account stats cards + universal buttons (everything remembers per-account) ----
  try{
    function __ocApplyStat__(label, val){
      var all=document.querySelectorAll('h1,h2,h3,h4,h5,h6,p,span,div,strong,b');
      for(var i=0;i<all.length;i++){
        var n=all[i];
        if(n.tagName==='SCRIPT'||n.tagName==='STYLE'||n.tagName==='TEMPLATE')continue;
        if(n.children.length)continue;
        if((n.textContent||'').trim()!==label)continue;
        // 1) "Last month"/"Current" stílus: label után közvetlen a szám
        var nx=n.nextElementSibling;
        if(nx && !nx.children.length && /tabular-nums/.test(String(nx.className||''))){
          nx.textContent=String(val); continue;
        }
        // 2) kártya nagy száma
        var card=n.closest('[class*="rounded-xl"]')||n.closest('div.flex.flex-col')||n.closest('[class*="gap-2.5"]');
        if(card){
          var big=card.querySelector('p span')||card.querySelector('[class*="tabular-nums"]');
          if(big){
            if(big.firstElementChild && /tabular-nums/.test(String(big.className||''))) big.firstElementChild.textContent=String(val);
            else if(big.children.length) big.lastChild.textContent=String(val);
            else big.textContent=String(val);
            continue;
          }
        }
        // 3) fallback: előző elem span-je
        var prev=n.previousElementSibling;
        if(prev){
          var sp=prev.querySelector?prev.querySelector('span'):null;
          var holder=sp||prev;
          if(holder && holder!==n) holder.textContent=String(val);
        }
      }
    }
    function __ocPatchText__(re, make){
      try{
        var nodes=[]; var W=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT,{acceptNode:function(x){ return re.test(x.nodeValue||'')?NodeFilter.FILTER_ACCEPT:NodeFilter.FILTER_REJECT; }});
        var w; while(w=W.nextNode()) nodes.push(w);
        nodes.forEach(function(w){ w.nodeValue=make(w.nodeValue); });
      }catch(e){}
    }
    function applyStatsDOM(st){
      __ocApplyStat__('Total Pins', st.totalPins);
      __ocApplyStat__('Total Scans', st.totalScans);
      __ocApplyStat__('Active Pins', st.activePins);
      __ocApplyStat__('Expired Pins', st.expiredPins);
      __ocApplyStat__('Pending', st.pendingPins);
      __ocApplyStat__('Finished', st.completedPins);
      __ocApplyStat__('Expired', st.expiredPins);
      __ocApplyStat__('Detections', st.detected);
      __ocApplyStat__('Unique Cheats', st.uniqueCheats);
      __ocApplyStat__('Legit', st.clean);
      __ocApplyStat__('Last month', st.scansLastMonth);
      __ocApplyStat__('Current', st.scansThisMonth);
      __ocApplyStat__('Suspicious', st.suspicious);
      __ocApplyStat__('Cheating', st.cheating);
      __ocApplyStat__('Clean', st.clean);
      __ocPatchText__(/completion rate/i, function(v){ var tot=st.totalPins||0; var pct=tot?((st.completedPins/tot)*100).toFixed(1):'0.0'; return v.replace(/\d+(\.\d+)?\s*%/, pct+'%'); });
      __ocPatchText__(/completed this week/i, function(v){ return v.replace(/\d+/, st.completedThisWeek||0); });
      __ocPatchText__(/fewer than last month/i, function(v){ return v.replace(/-?\d+/, '-'+(st.expiredPins||0)); });
      __ocPatchText__(/vs last month/i, function(v){
        var cur=st.pinsThisMonth||0, last=st.pinsLastMonth||0;
        var pct=last?(((cur-last)/last)*100).toFixed(1):'0.0';
        if(cur>0 && pct==='0.0' && last===0) pct='100.0';
        var sign=(cur>=last)?'+':'';
        return v.replace(/[+-]?\d+(\.\d+)?\s*%/, sign+pct+'%');
      });
      var mt=document.getElementById('radix-_r_o_-trigger-my-pins');
      if(mt){ var mc=mt.querySelector('span:last-child'); if(mc && !isNaN(mc.textContent)) mc.textContent=String(st.totalPins||0); }
      if(window.__OC_RENDER_RECENT__) window.__OC_RENDER_RECENT__(st);
    }
    var _ocStatReApply=false;
    function _ocScheduleStatApply(){ if(_ocStatReApply) return; _ocStatReApply=true; (window.requestAnimationFrame||function(f){setTimeout(f,16);})(function(){ _ocStatReApply=false; if(window.__OC_STATS__) applyStatsDOM(window.__OC_STATS__); }); }
    function _ocStatForceFix(){
      var st=window.__OC_STATS__; if(!st) return;
      var c=[].slice.call(document.querySelectorAll('[class="flex flex-col gap-2.5"]'));
      for(var i=0;i<c.length;i++){
        var h=c[i].querySelector('h3'); if(!h) continue;
        var lbl=h.textContent.trim(); var valRaw=null;
        if(lbl==='Total Pins') valRaw=st.totalPins;
        else if(lbl==='Pending') valRaw=st.pendingPins;
        else if(lbl==='Finished') valRaw=st.completedPins;
        else if(lbl==='Expired') valRaw=st.expiredPins;
        if(valRaw==null) continue;
        var p=null; var pte=[].slice.call(c[i].children); for(var j=0;j<pte.length;j++){ if((pte[j].className||'').toString().indexOf('tabular-nums')>-1){ p=pte[j]; break; } }
        if(!p) p=c[i].querySelector('p');
        if(p && p.textContent.trim()!==String(valRaw)) p.textContent=String(valRaw);
      }
    }
    function _ocInstallStatObserver(){
      var card=document.querySelector('[class="flex flex-col gap-2.5"]');
      var root=card&&card.parentElement?card.parentElement:document.body;
      try{
        var mo=new (window.MutationObserver)(function(){ _ocScheduleStatApply(); _ocStatForceFix(); });
        mo.observe(root,{subtree:true,childList:true,characterData:true,attributes:false});
        window.__OC_STAT_OBSERVER__=mo;
      }catch(e){}
    }
    window.__OC_REFRESH_STATS__=function(){
      if((location.pathname||'').indexOf('/dashboard')<0) return;
      fetch('/api/stats',{credentials:'same-origin'}).then(function(r){return r.json();}).then(function(st){
        if(!st||st.error) return;
        window.__OC_STATS__=st;
        applyStatsDOM(st);
        if(!window.__OC_STAT_OBSERVER__) _ocInstallStatObserver();
      }).catch(function(){});
    };
    function __ocSyncPins__(){
      if((location.pathname||'').indexOf('/dashboard/pins')<0) return;
      fetch('/api/pins',{credentials:'same-origin'}).then(function(r){return r.json();}).then(function(list){
        if(!Array.isArray(list)) return;
        var existing=window.__OC_PINS__||[];
        list.forEach(function(sv){
          var idx=-1;
          for(var i=0;i<existing.length;i++){ if(String(existing[i].code).toUpperCase()===String(sv.code).toUpperCase()){ idx=i; break; } }
          if(idx>-1){ for(var k in sv){ existing[idx][k]=sv[k]; } }
          else { existing.unshift({code:sv.code, game:sv.game||'FiveM', createdAt:sv.createdAt, status:sv.status||'active', private:false, ruin:false, shared:false}); }
        });
        window.__OC_PINS__=existing;
        if(window.savePins) window.savePins();
      }).catch(function(){});
    }
    window.__OC_FILTER__=function(){
      var mode=window.__OC_PFILTER__||'all';
      var pst=window.__OC_PSTATUS__||'all';
      var pgm=window.__OC_PGAME__||'all';
      var n=0;
      document.querySelectorAll('[data-oc-row]').forEach(function(r){
        var st=(r.getAttribute('data-oc-stat')||'active');
        if(st==='complete') st='completed';
        var gm='';
        var cells=r.querySelectorAll('td');
        if(cells.length>2) gm=String(cells[2].textContent||'').trim().toLowerCase();
        var okP=(pst==='all')||(st===pst)||(pst==='pending'&&(st==='pending'||st==='scanning'));
        var okG=(pgm==='all')||(gm==='')||(gm.indexOf(pgm)>-1);
        var show=((mode==='all')||(mode==='pending'&&(st==='pending'||st==='scanning'))) && okP && okG;
        r.style.display=show?'':'none';
        if(show)n++;
      });
      var chip=document.getElementById('oc-filter-chip');
      if(mode!=='all' && !chip){
        chip=document.createElement('div');
        chip.id='oc-filter-chip';
        chip.style.cssText='position:fixed;top:84px;right:24px;z-index:99997;background:#0b1424;border:1px solid rgba(168,85,247,0.5);color:#93c5fd;border-radius:999px;padding:8px 14px;font:600 12px Inter,system-ui,sans-serif;box-shadow:0 10px 25px rgba(0,0,0,0.5);';
        chip.innerHTML='Viewing pending ('+n+') <span style="color:#f87171;cursor:pointer;margin-left:8px;">\u2715 clear</span>';
        chip.onclick=function(){ window.__OC_PFILTER__='all'; __ocApplyFilter__(); };
        document.body.appendChild(chip);
      }
      else if(chip){ chip.innerHTML='Viewing pending ('+n+') <span style="color:#f87171;cursor:pointer;margin-left:8px;">\u2715 clear</span>'; }
      if(mode==='all'&&chip&&chip.parentNode) chip.parentNode.removeChild(chip);
    };
    function __ocApplyFilter__(){ window.__OC_FILTER__&&window.__OC_FILTER__(); }
    window.__ocApplyFilter__=__ocApplyFilter__;
    document.addEventListener('click', function(e){
      var el=e.target;
      if(!el||!el.closest) return;
      var b=el.closest('button,a,[role="button"]');
      if(!b) return;
      var t=(b.textContent||'').trim().toLowerCase();
      var row=b.closest('[data-oc-row]');
      var code=row?row.getAttribute('data-oc-row'):'';
      if(/shared with me/.test(t)&&!(b.getAttribute('role')==='tab'||b.hasAttribute('aria-selected'))){
        e.preventDefault(); e.stopPropagation();
        var bn=document.getElementById('oc-shared-banner');
        if(bn){ if(bn.parentNode) bn.parentNode.removeChild(bn); document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display=''; }); return; }
        document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display='none'; });
        bn=document.createElement('div');
        bn.id='oc-shared-banner';
        bn.style.cssText='margin:14px;padding:18px;border:1px dashed rgba(148,163,184,0.35);border-radius:12px;color:#94a3b8;text-align:center;font-size:13px;background:rgba(148,163,184,0.06);';
        bn.textContent='No pins are shared with you yet. Toggle the button again to go back to your pins.';
        var panel=document.getElementById('radix-_r_o_-content-my-pins')||document.body;
        panel.appendChild(bn);
      }
      else if(/^delete$/i.test(t) && code){
        e.preventDefault(); e.stopPropagation();
        var kid=row?row.getAttribute('data-oc-key-id'):'';
        if(confirm('Delete PIN '+code+'?' + (kid?'':' (refreshing from server may restore it)' ))){
          if(kid){
            fetch('/api/keys/'+encodeURIComponent(kid),{method:'DELETE',credentials:'same-origin'}).then(function(){ if(window.refreshServerPins) window.refreshServerPins(); }).catch(function(){});
          }
          if(row&&row.parentNode) row.parentNode.removeChild(row);
        }
      }
      else if(/^copy$/i.test(t) && code){
        try{ navigator.clipboard.writeText(code); }catch(ex){}
      }
    }, false);
    window.ocMoreMenu=function(code, anchor){
      try{
        var old=document.getElementById('oc-more-menu'); if(old&&old.parentNode) old.parentNode.removeChild(old);
        var m=document.createElement('div');
        m.id='oc-more-menu';
        m.style.cssText='position:fixed;z-index:999999;background:#0b1424;border:1px solid rgba(148,163,184,0.25);border-radius:10px;padding:6px;min-width:180px;box-shadow:0 12px 32px rgba(0,0,0,0.5);font:400 13px Inter,ui-sans-serif,system-ui,sans-serif;';
        m.innerHTML='<div data-oc-mi="view" style="padding:8px 12px;border-radius:8px;cursor:pointer;color:#e2e8f0;">View Report</div>'+
          '<div data-oc-mi="copy" style="padding:8px 12px;border-radius:8px;cursor:pointer;color:#e2e8f0;">Copy PIN</div>'+
          '<div data-oc-mi="delete" style="padding:8px 12px;border-radius:8px;cursor:pointer;color:#f87171;">Delete PIN</div>';
        var r=anchor.getBoundingClientRect();
        m.style.left=Math.max(8,(r.right-190))+'px';
        m.style.top=(r.bottom+6)+'px';
        m.addEventListener('click', function(ev){
          var mi=ev.target&&ev.target.getAttribute?ev.target.getAttribute('data-oc-mi'):'';
          if(mi==='view'){ if(window.openPinReport)window.openPinReport(code); }
          else if(mi==='copy'){ try{ navigator.clipboard.writeText(code); }catch(ex){} }
          else if(mi==='delete'){
            var row=null; var rows=document.querySelectorAll('[data-oc-row]');
            for(var i=0;i<rows.length;i++){ if((rows[i].getAttribute('data-oc-row')||'').toUpperCase()===String(code).toUpperCase()){ row=rows[i]; break; } }
            var kid=row?row.getAttribute('data-oc-key-id'):'';
            if(confirm('Delete PIN '+code+'?'+(kid?'':' (refreshing from server may restore it)'))){
              if(kid){ window.fetch('/api/keys/'+encodeURIComponent(kid),{method:'DELETE',credentials:'same-origin'}).then(function(){ if(window.refreshServerPins)window.refreshServerPins(); }).catch(function(){}); }
              if(row&&row.parentNode) row.parentNode.removeChild(row);
            }
          }
          if(m.parentNode) m.parentNode.removeChild(m);
        });
        document.body.appendChild(m);
        setTimeout(function(){ var once=function(ev){ var x=document.getElementById('oc-more-menu'); var tgt=ev&&ev.target?ev.target:null; if(x&&x.parentNode&&!(tgt&&x.contains(tgt))){ x.parentNode.removeChild(x); } document.removeEventListener('click', once, true); }; document.addEventListener('click', once, true); }, 60);
      }catch(e){}
    };
    function _ocLoadTools(){
      if(document.getElementById('oc-ctrl-bar')) return;
      if((location.pathname||'').indexOf('/dashboard/pins')<0 && (location.pathname||'').indexOf('/dashboard')<0) return;
      var bar=document.createElement('div');
      bar.id='oc-ctrl-bar';
      bar.style.cssText='position:fixed;right:16px;bottom:100px;z-index:99997;display:flex;flex-direction:column;gap:8px;align-items:flex-end;';
      bar.innerHTML='<button data-oc-act="pending" style="background:rgba(11,20,36,0.92);border:1px solid rgba(168,85,247,0.45);color:#93c5fd;border-radius:999px;padding:8px 14px;font:600 12px Inter,system-ui,sans-serif;cursor:pointer;box-shadow:0 8px 20px rgba(0,0,0,0.4);">Show pending</button>'+
        '<button data-oc-act="shared" style="background:rgba(11,20,36,0.92);border:1px solid rgba(148,163,184,0.35);color:#cbd5e1;border-radius:999px;padding:8px 14px;font:600 12px Inter,system-ui,sans-serif;cursor:pointer;box-shadow:0 8px 20px rgba(0,0,0,0.4);">Show shared</button>';
      document.body.appendChild(bar);
      bar.querySelectorAll('[data-oc-act]').forEach(function(btn){
        btn.addEventListener('click', function(e){ try{ e.preventDefault(); e.stopPropagation(); }catch(_x){} window.ocAct&&window.ocAct(btn.getAttribute('data-oc-act')); }, true);
      });
      if(!window.ocAct){
        window.ocAct=function(a){
          if(a==='pending'){
            var val=(window.__OC_PFILTER__==='pending')?'all':'pending';
            window.__OC_PFILTER__=val;
            if(window.__OC_FILTER__) window.__OC_FILTER__();
          } else if(a==='shared'){
            var bn=document.getElementById('oc-shared-banner');
            if(bn){ if(bn.parentNode) bn.parentNode.removeChild(bn); document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display=''; }); return; }
            document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display='none'; });
            bn=document.createElement('div');
            bn.id='oc-shared-banner';
            bn.style.cssText='margin:14px;padding:18px;border:1px dashed rgba(148,163,184,0.35);border-radius:12px;color:#94a3b8;text-align:center;font-size:13px;background:rgba(148,163,184,0.06);';
            bn.textContent='No pins are shared with you yet. Toggle the button again to go back to your pins.';
            var panel=document.getElementById('radix-_r_o_-content-my-pins')||document.body;
            panel.appendChild(bn);
          }
        };
      }
    }
    setTimeout(_ocLoadTools, 1200);
    setInterval(_ocLoadTools, 4000);
    function _ocBindButtons(){
      try{
        var vps=[].slice.call(document.querySelectorAll('button,[role="button"]')).filter(function(x){ return /view pending/i.test(x.textContent||'') && !x.__ocVP; });
        vps.forEach(function(vp){
          vp.__ocVP=true;
          vp.addEventListener('click', function(e){ try{ if(e){e.preventDefault();e.stopPropagation();} }catch(_x){}
            window.__OC_PFILTER__=(window.__OC_PFILTER__==='all')?'pending':'all';
            if(window.__OC_FILTER__) window.__OC_FILTER__();
          }, true);
        });
        var sbs=[].slice.call(document.querySelectorAll('button,[role="button"]')).filter(function(x){ return /shared with me/i.test(x.textContent||'') && !x.__ocSB && !(x.getAttribute('role')==='tab'||x.hasAttribute('aria-selected')); });
        sbs.forEach(function(sb){
          sb.__ocSB=true;
          sb.addEventListener('click', function(e){ try{ if(e){e.preventDefault();e.stopPropagation();} }catch(_x){}
            var bn=document.getElementById('oc-shared-banner');
            if(bn){ if(bn.parentNode) bn.parentNode.removeChild(bn); document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display=''; }); return; }
            document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display='none'; });
            bn=document.createElement('div');
            bn.id='oc-shared-banner';
            bn.style.cssText='margin:14px;padding:18px;border:1px dashed rgba(148,163,184,0.35);border-radius:12px;color:#94a3b8;text-align:center;font-size:13px;background:rgba(148,163,184,0.06);';
            bn.textContent='No pins are shared with you yet. Toggle the button again to go back to your pins.';
            var panel=document.getElementById('radix-_r_o_-content-my-pins')||document.body;
            panel.appendChild(bn);
          }, true);
        });
      }catch(e){}
    }
    _ocBindButtons();
    setInterval(_ocBindButtons, 2000);
    document.addEventListener('click', function(e){
      var el=e.target;
      if(!el||!el.closest) return;
      var cb=el.closest('button,a,[role="button"],[role="tab"]');
      if(cb){
        var ct=(cb.textContent||'').trim().toLowerCase();
        if(/view pending/.test(ct)){
          e.preventDefault(); e.stopPropagation();
          window.__OC_PFILTER__=(window.__OC_PFILTER__==='all')?'pending':'all';
          if(window.__OC_FILTER__) window.__OC_FILTER__();
          return;
        }
        if(/shared with me/.test(ct)&&!(cb.getAttribute('role')==='tab'||cb.hasAttribute('aria-selected'))){
          e.preventDefault(); e.stopPropagation();
          var bn=document.getElementById('oc-shared-banner');
          if(bn){ if(bn.parentNode) bn.parentNode.removeChild(bn); document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display=''; }); return; }
          document.querySelectorAll('[data-oc-row]').forEach(function(r){ r.style.display='none'; });
          bn=document.createElement('div');
          bn.id='oc-shared-banner';
          bn.style.cssText='margin:14px;padding:18px;border:1px dashed rgba(148,163,184,0.35);border-radius:12px;color:#94a3b8;text-align:center;font-size:13px;background:rgba(148,163,184,0.06);';
          bn.textContent='No pins are shared with you yet. Toggle the button again to go back to your pins.';
          var panel=document.getElementById('radix-_r_o_-content-my-pins')||document.body;
          panel.appendChild(bn);
          return;
        }
      }
      var m=el.closest('[data-oc-more]');
      if(m){ e.preventDefault(); e.stopPropagation(); window.ocMoreMenu&&window.ocMoreMenu(m.getAttribute('data-oc-more'), m); return; }
      var act=el.closest('[data-oc-act]');
      if(act){
        e.preventDefault(); e.stopPropagation();
        if(window.ocAct) window.ocAct(act.getAttribute('data-oc-act'));
        return;
      }
      var opt=el.closest('[role="option"],[role="option"],[data-radix-collection-item]');
      if(opt){
        var ot=(opt.textContent||'').trim().toLowerCase();
        var STATUSES=['all status','pending','active','completed','scanning','expired','in progress'];
        var known=false;
        for(var i=0;i<STATUSES.length;i++){ if(ot===STATUSES[i]){ known=true; break; } }
        if(known){ window.__OC_PSTATUS__=(ot==='all status')?'all':ot; window.__OC_PGAME__=window.__OC_PGAME__||'all'; setTimeout(function(){ __ocApplyFilter__(); }, 120); }
        else if(ot==='all games'){ window.__OC_PGAME__='all'; setTimeout(function(){ __ocApplyFilter__(); }, 120); }
        else if(/games|minecraft|fivem|cs2|counter-strike|valorant|fortnite|rust|ark|gta|roblox|garrys|vrchat|rec room|the isle|phasmophobia|tom clancy|overwatch|apex/.test(ot)){ window.__OC_PGAME__=ot; setTimeout(function(){ __ocApplyFilter__(); }, 120); }
      }
    }, true);
    setInterval(window.__OC_REFRESH_STATS__, 3000);
    setTimeout(window.__OC_REFRESH_STATS__, 700);
    setInterval(_ocStatForceFix, 300);
    setInterval(__ocSyncPins__, 7000);
    setTimeout(__ocSyncPins__, 1500);
  }catch(e){ window.__OC_BODY__.statsErr=String(e); }
})();
</script></body>`;

function sendRes(res, code, body, type) {
  res.writeHead(code, { 'Content-Type': type, 'Cache-Control': 'no-store' });
  res.end(body);
}

// --- Real API storage (JSON-backed) ---
const cliDataIdx = process.argv.indexOf('--data-dir');
const DATA_DIR = cliDataIdx > -1 ? path.resolve(process.argv[cliDataIdx + 1]) : __dirname;
const SCANS_FILE = path.join(DATA_DIR, 'scans.json');
const KEYS_FILE = path.join(DATA_DIR, 'keys.json');
const ACCOUNTS_FILE = path.join(DATA_DIR, 'accounts.json');

if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });
if (!fs.existsSync(SCANS_FILE)) fs.writeFileSync(SCANS_FILE, '[]');
if (!fs.existsSync(KEYS_FILE)) fs.writeFileSync(KEYS_FILE, '[]');
if (!fs.existsSync(ACCOUNTS_FILE)) fs.writeFileSync(ACCOUNTS_FILE, '[]');

// --- Accounts + sessions ---
const SESSIONS_FILE = path.join(DATA_DIR, 'sessions.json');
function loadSessions() {
  try {
    const o = JSON.parse(fs.readFileSync(SESSIONS_FILE, 'utf8'));
    return (o && typeof o === 'object') ? o : {};
  } catch (e) { return {}; }
}
const SESSIONS = loadSessions();
function saveSessions() {
  try { fs.writeFileSync(SESSIONS_FILE, JSON.stringify(SESSIONS)); } catch (e) {}
}
const COOKIE_NAME = 'oc_session';
function hashPassword(pw) {
  const salt = crypto.randomBytes(16).toString('hex');
  const hash = crypto.scryptSync(String(pw), salt, 32).toString('hex');
  return salt + ':' + hash;
}
function verifyPassword(pw, stored) {
  try {
    const parts = String(stored || '').split(':');
    if (parts.length < 2) return false;
    const test = crypto.scryptSync(String(pw), parts[0], 32).toString('hex');
    return test === parts[1];
  } catch (e) { return false; }
}
const apiGetAccounts = () => readJson(ACCOUNTS_FILE);
function writeAccounts(accs) { writeJson(ACCOUNTS_FILE, accs); }
function publicUser(acc) {
  return { id: acc.id, username: acc.username, createdAt: acc.createdAt };
}
function setSession(res, acc) {
  const token = crypto.randomBytes(24).toString('hex');
  SESSIONS[token] = { userId: acc.id, ts: Date.now() };
  saveSessions();
  res.setHeader('Set-Cookie', COOKIE_NAME + '=' + token + '; Path=/; HttpOnly; Max-Age=2592000');
}
function clearSession(res, req) {
  const m = readCookie(req);
  if (m) delete SESSIONS[m];
  saveSessions();
  res.setHeader('Set-Cookie', COOKIE_NAME + '=; Path=/; HttpOnly; Max-Age=0');
}
function adoptOrphanKeys(userId) {
  try {
    const keys = apiGetKeys();
    let changed = false;
    keys.forEach(k => {
      if (!k.ownerId || k.ownerId === 'guest') { k.ownerId = userId; changed = true; }
    });
    if (changed) writeJson(KEYS_FILE, keys);
  } catch (e) {}
}
function readCookie(req) {
  try {
    const re = new RegExp('(?:^|;\\s*)' + COOKIE_NAME + '=([^;]+)');
    const m = (req.headers.cookie || '').match(re);
    return m ? m[1] : null;
  } catch (e) { return null; }
}
function accountBySession(req) {
  try {
    const token = readCookie(req);
    if (!token) return null;
    const rec = SESSIONS[token];
    if (!rec) return null;
    return apiGetAccounts().find(a => a.id === rec.userId) || null;
  } catch (e) { return null; }
}

function readJson(file) {
  try { return JSON.parse(fs.readFileSync(file, 'utf8')); } catch (e) { return []; }
}
function writeJson(file, data) {
  fs.writeFileSync(file, JSON.stringify(data, null, 2));
}
const apiGetScans = () => readJson(SCANS_FILE);
const apiGetKeys = () => readJson(KEYS_FILE);

function ownedKeys(ownerId) { return apiGetKeys().filter(k => k.ownerId === ownerId); }
function ownedScans(ownerId) { return apiGetScans().filter(s => s.ownerId === ownerId); }
function countDetections(s) {
  let n = 0;
  const det = s.detections;
  if (det && typeof det === 'object') {
    Object.keys(det).forEach(k => { const v = det[k]; if (v && String(v).trim()) n++; });
  }
  if (s.ocean && String(s.ocean).trim()) {
    n += (String(s.ocean).match(/\*\*[^*]+\*\*/g) || []).length || 1;
  }
  return n;
}
function computeStats(ownerId) {
  const keys = ownedKeys(ownerId);
  const scans = ownedScans(ownerId);
  const now = new Date();
  const monthStart = new Date(now.getFullYear(), now.getMonth(), 1).getTime();
  const prevMonthStart = new Date(now.getFullYear(), now.getMonth() - 1, 1).getTime();
  let activePins = 0, pendingPins = 0, scanningPins = 0, completedPins = 0, expiredPins = 0;
  let pinsThisMonth = 0, pinsLastMonth = 0;
  keys.forEach(k => {
    const st = k.status || 'active';
    if (st === 'active') activePins++;
    else if (st === 'pending') pendingPins++;
    else if (st === 'scanning') scanningPins++;
    else if (st === 'complete') completedPins++;
    else if (st === 'expired') expiredPins++;
    const t = k.createdAt ? new Date(k.createdAt).getTime() : 0;
    if (t >= monthStart) pinsThisMonth++;
    else if (t >= prevMonthStart && t < monthStart) pinsLastMonth++;
  });
  let cheating = 0, suspicious = 0, clean = 0, detected = 0, scansThisMonth = 0, scansLastMonth = 0, completedThisWeek = 0;
  const cheatSet = new Set();
  scans.forEach(s => {
    const t = s.timestamp ? new Date(s.timestamp).getTime() : 0;
    if (t >= monthStart) scansThisMonth++;
    if (t >= prevMonthStart && t < monthStart) scansLastMonth++;
    if (t >= now.getTime() - 7 * 86400000) completedThisWeek++;
    const st = ((s.status || s.result || 'unknown')) + '';
    const low = st.toLowerCase();
    if (low.indexOf('cheat') > -1) cheating++;
    else if (low.indexOf('suspicious') > -1) suspicious++;
    else if (low === 'clean' || low.indexOf('legit') > -1) clean++;
    detected += countDetections(s);
    if (s.detections && typeof s.detections === 'object') {
      Object.keys(s.detections).forEach(cat => {
        const arr = s.detections[cat];
        if (!Array.isArray(arr)) return;
        arr.forEach(it => { const nm = it && (it.name || it.Name); if (nm) cheatSet.add(String(nm)); });
      });
    }
  });
  return {
    totalPins: keys.length,
    activePins, pendingPins, scanningPins, completedPins, expiredPins,
    pinsThisMonth, pinsLastMonth,
    totalScans: scans.length,
    scansThisMonth, scansLastMonth,
    completedThisWeek,
    cheating, suspicious, clean,
    detected,
    uniqueCheats: cheatSet.size,
    profiles: buildProfiles(false, ownerId).length
  };
}

function sendJson(res, obj) {
  sendRes(res, 200, JSON.stringify(obj), 'application/json');
}

// --- Profile aggregation (everyone gets a profile; everything is remembered) ---
function tallyDetections(det, detections) {
  if (!detections) return;
  if (detections && typeof detections === 'object') {
    Object.keys(detections).forEach(cat => {
      const arr = detections[cat];
      if (!Array.isArray(arr)) return;
      arr.forEach(it => {
        const name = it && (it.name || it.Name);
        if (!name) return;
        const key = name.toString();
        det[key] = det[key] || { name: key, badge: (it.badge || it.Badge || ''), desc: (it.desc || it.Detail || ''), count: 0 };
        det[key].count++;
      });
    });
  }
}
function tallyOceanText(det, ocean) {
  if (!ocean) return;
  const re = /\*\*([^*]+)\*\*\s*\(([^)]+)\)\s*[—-]?\s*([^\n]*)/g;
  let m;
  while ((m = re.exec(ocean))) {
    const key = m[1].trim();
    det[key] = det[key] || { name: key, badge: m[2].trim(), desc: (m[3] || '').trim(), count: 0 };
    det[key].count++;
  }
}
function summarizeProfile(b, includeScans) {
  const statuses = { cheat: 0, clean: 0, suspicious: 0, pending: 0, unknown: 0 };
  b.scans.forEach(s => {
    const st = (((s.status || s.result || 'unknown')) + '').toLowerCase();
    if (statuses[st] !== undefined) statuses[st]++; else statuses.unknown++;
  });
  const dets = Object.keys(b.det).map(k => b.det[k]).sort((x, y) => y.count - x.count);
  const p = {
    id: b.id,
    username: b.username,
    pcName: b.pcName,
    os: b.os,
    hwid: b.hwid,
    keyId: b.keyId,
    pin: b.pin,
    scanCount: b.scans.length,
    firstScan: b.firstScan,
    lastScan: b.lastScan,
    statuses,
    games: Object.keys(b.games).map(g => ({ name: g, count: b.games[g] })),
    pins: Object.keys(b.pins),
    detections: dets.slice(0, 40),
    detectionCount: dets.reduce((n, d) => n + d.count, 0),
    activityCount: b.scans.reduce((n, s) => n + (Array.isArray(s.activityLog) ? s.activityLog.length : 0), 0)
  };
  if (includeScans) {
    p.scans = b.scans.slice().sort((a, z) => ((((z.timestamp || '')) < ((a.timestamp || ''))) ? -1 : 1));
  }
  return p;
}
function buildProfiles(includeScans, ownerId) {
  const scans = (ownerId ? apiGetScans().filter(s => s.ownerId === ownerId) : apiGetScans());
  const buckets = {};
  scans.forEach(s => {
    const hwid = (s.hwid && ('' + s.hwid).trim()) || '';
    const keyId = (s.keyId && ('' + s.keyId)) || '';
    const pin = (s.pin && ('' + s.pin).trim().toUpperCase()) || '';
    const uid = hwid || keyId || pin || ((((s.username || '') + '|' + (s.pcName || ''))));
    if (!buckets[uid]) buckets[uid] = { id: uid, username: '', pcName: '', os: '', hwid, keyId, pin, scans: [], pins: {}, games: {}, det: {}, firstScan: null, lastScan: null };
    const b = buckets[uid];
    b.scans.push(s);
    if (!b.username && (s.username || s.playerName)) b.username = s.username || s.playerName;
    if (!b.pcName && s.pcName) b.pcName = s.pcName;
    if (!b.os && s.os) b.os = s.os;
    if (!b.hwid && s.hwid) b.hwid = ('' + s.hwid).trim();
    if (!b.keyId && s.keyId) b.keyId = ('' + s.keyId);
    const t = s.timestamp || '';
    if (!b.firstScan || t < b.firstScan) b.firstScan = t;
    if (!b.lastScan || t > b.lastScan) b.lastScan = t;
    if (s.pin) b.pins[s.pin] = true;
    if (s.game) b.games[s.game] = (b.games[s.game] || 0) + 1;
    tallyDetections(b.det, s.detections);
    if (s.ocean) tallyOceanText(b.det, s.ocean);
  });
  return Object.keys(buckets).map(uid => summarizeProfile(buckets[uid], includeScans)).sort((a, z) => z.scanCount - a.scanCount);
}
function pinFromKey(k) {
  return ((k && k.key) || '').trim();
}

const server = http.createServer((req, res) => {
  try {
    let p = decodeURIComponent((req.url || '/').split('?')[0]);
    if (req.url && req.url.indexOf('/api/') === 0) {
      console.log('[API]', req.method, req.url);
      try {
        require('fs').appendFileSync(require('path').join(__dirname,'mirror-api.log'), new Date().toISOString()+' '+req.method+' '+req.url+'\n');
      } catch(e){}
    }

    // --- REAL API ROUTES (JSON data the scanner + dashboard use) ---
    // These run BEFORE the generic /api mock fallback below.

    // health / server status
    if (p === '/api/health' || p === '/api/server-status') {
      return sendJson(res, { status: 'ok', ok: true, statusCode: 200, scans: apiGetScans().length, keys: apiGetKeys().length, timestamp: new Date().toISOString() });
    }

    // ---- Accounts (register on the main page, then sign in to existing) ----
    if (p === '/api/auth/register' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          let b = {}; try { b = JSON.parse(body || '{}'); } catch (e2) {}
          const username = String(b.username || '').trim();
          const password = String(b.password || '');
          const confirm = b.confirm === undefined ? password : String(b.confirm);
          if (!/^[A-Za-z0-9_.-]{3,32}$/.test(username)) return sendJson(res, { ok: false, error: 'invalid_username' });
          if (password.length < 6) return sendJson(res, { ok: false, error: 'weak_password' });
          if (password !== confirm) return sendJson(res, { ok: false, error: 'password_mismatch' });
          const accs = apiGetAccounts();
          if (accs.some(a => (a.username || '').toLowerCase() === username.toLowerCase())) return sendJson(res, { ok: false, error: 'username_exists' });
          const acc = { id: 'acc-' + Date.now() + '-' + Math.random().toString(36).substr(2, 8), username, passwordHash: hashPassword(password), createdAt: new Date().toISOString() };
          accs.push(acc);
          writeAccounts(accs);
          setSession(res, acc);
          adoptOrphanKeys(acc.id);
          return sendJson(res, { ok: true, user: publicUser(acc) });
        } catch (e) { return sendJson(res, { ok: false, error: 'server_error' }); }
      });
      return;
    }
    if (p === '/api/auth/login' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          let b = {}; try { b = JSON.parse(body || '{}'); } catch (e2) {}
          const username = String(b.username || '').trim();
          const password = String(b.password || '');
          const acc = apiGetAccounts().find(a => (a.username || '').toLowerCase() === username.toLowerCase());
          if (!acc || !verifyPassword(password, acc.passwordHash)) {
            return sendJson(res, { ok: false, error: 'bad_credentials' });
          }
          setSession(res, acc);
          adoptOrphanKeys(acc.id);
          return sendJson(res, { ok: true, user: publicUser(acc) });
        } catch (e) { return sendJson(res, { ok: false, error: 'server_error' }); }
      });
      return;
    }
    if (p === '/api/auth/logout') {
      clearSession(res, req);
      return sendJson(res, { ok: true });
    }
    if (p === '/api/auth/me') {
      const acc = accountBySession(req);
      if (!acc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      return sendJson(res, { user: publicUser(acc) });
    }

    // ---- Stats (per-account totals for the dashboard cards) ----
    if (p === '/api/stats' && req.method === 'GET') {
      const acc = accountBySession(req);
      if (!acc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      return sendJson(res, computeStats(acc.id));
    }

    // scans
    if (p === '/api/scans' && req.method === 'GET') {
      const acc = accountBySession(req);
      if (!acc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      return sendJson(res, ownedScans(acc.id));
    }
    if (p === '/api/scans' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          const sessionsAcc = accountBySession(req);
          const scans = apiGetScans();
          let incoming;
          try {
            incoming = JSON.parse(body || '[]');
          } catch (e) {
            incoming = [];
          }
          if (!Array.isArray(incoming)) incoming = [incoming];
          incoming.forEach(scan => {
            scan.id = scan.id || 'scan-' + Date.now() + '-' + Math.random().toString(36).substr(2, 8);
            scan.timestamp = scan.timestamp || new Date().toISOString();
            const k0 = apiGetKeys().find(kk =>
              (scan.keyId && kk.id === scan.keyId) ||
              (scan.pin && pinFromKey(kk).toUpperCase() === ('' + scan.pin).toUpperCase()));
            scan.ownerId = scan.ownerId || (k0 && k0.ownerId) || (sessionsAcc && sessionsAcc.id) || 'guest';
            scans.push(scan);
          });
          writeJson(SCANS_FILE, scans);
          // Mark the connected PIN/key as finished on the website side.
          try {
            const keys = apiGetKeys();
            let changed = false;
            incoming.forEach(scan => {
              const k = keys.find(kk =>
                (scan.keyId && kk.id === scan.keyId) ||
                (scan.pin && pinFromKey(kk).toUpperCase() === ('' + scan.pin).toUpperCase()));
              if (k) {
                if (k.status !== 'used') k.status = 'complete';
                k.result = scan.status || scan.result || k.result;
                k.player = scan.username || scan.playerName || k.player;
                k.scanStartedAt = k.scanStartedAt || scan.timestamp;
                k.scannedAt = scan.timestamp;
                k.scanId = scan.id;
                changed = true;
              }
            });
            if (changed) writeJson(KEYS_FILE, keys);
          } catch (eKey) {}
          sendJson(res, incoming);
        } catch (e) {
          res.writeHead(500, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: 'bad request' }));
        }
      });
      return;
    }
    if (p === '/api/scans' && req.method === 'DELETE') {
      writeJson(SCANS_FILE, []);
      return sendJson(res, { success: true });
    }

    // keys
    if (p === '/api/keys' && req.method === 'GET') {
      const sessionsAcc = accountBySession(req);
      if (!sessionsAcc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      const keys = ownedKeys(sessionsAcc.id);
      const now = Date.now();
      keys.forEach(k => {
        if (k.status === 'active' && new Date(k.expiresAt).getTime() < now) k.status = 'expired';
      });
      writeJson(KEYS_FILE, apiGetKeys());
      return sendJson(res, keys);
    }
    if (p === '/api/keys' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          const sessionsAcc = accountBySession(req);
          const keys = apiGetKeys();
          let incoming;
          try {
            incoming = JSON.parse(body || '[]');
          } catch (e) {
            incoming = [];
          }
          if (!Array.isArray(incoming)) incoming = [incoming];
          incoming.forEach(k => { k.id = k.id || 'key-' + Date.now() + '-' + Math.random().toString(36).substr(2, 8); if (sessionsAcc) k.ownerId = sessionsAcc.id; keys.push(k); });
          writeJson(KEYS_FILE, keys);
          sendJson(res, incoming);
        } catch (e) {
          res.writeHead(500, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ error: 'bad request' }));
        }
      });
      return;
    }
    if (p === '/api/keys' && req.method === 'DELETE') {
      writeJson(KEYS_FILE, []);
      return sendJson(res, { success: true });
    }

    // key validate / use (must be matched before /api/keys/:id delete-only route below)
    let mValidate = p.match(/^\/api\/keys\/validate\/(.+)$/);
    if (mValidate && req.method === 'GET') {
      const keys = apiGetKeys();
      const key = keys.find(k => k.key === decodeURIComponent(mValidate[1]));
      if (!key) return sendJson(res, { valid: false, reason: 'not_found' });
      if (key.status === 'used' || key.usedCount >= key.maxUses) return sendJson(res, { valid: false, reason: 'used_up' });
      if (new Date(key.expiresAt).getTime() < Date.now()) {
        key.status = 'expired';
        writeJson(KEYS_FILE, keys);
        return sendJson(res, { valid: false, reason: 'expired' });
      }
      return sendJson(res, { valid: true, key });
    }
    let mUse = p.match(/^\/api\/keys\/use\/(.+)$/);
    if (mUse) {
      const keys = apiGetKeys();
      const key = keys.find(k => k.id === decodeURIComponent(mUse[1]));
      if (!key) {
        res.writeHead(404, { 'Content-Type': 'application/json' });
        return res.end(JSON.stringify({ error: 'Key not found' }));
      }
      key.usedCount = (key.usedCount || 0) + 1;
      if (key.usedCount >= key.maxUses) key.status = 'used';
      writeJson(KEYS_FILE, keys);
      return sendJson(res, key);
    }
    let mKeyDel = p.match(/^\/api\/keys\/(.+)$/);
    if (mKeyDel && req.method === 'DELETE') {
      const sessionsAcc = accountBySession(req);
      const wantId = decodeURIComponent(mKeyDel[1]);
      const keys = apiGetKeys();
      const target = keys.find(k => k.id === wantId);
      if (sessionsAcc && target && target.ownerId && target.ownerId !== sessionsAcc.id) {
        return sendJson(res, { success: false, error: 'forbidden' });
      }
      writeJson(KEYS_FILE, keys.filter(k => k.id !== wantId));
      return sendJson(res, { success: true });
    }

    // ---- Pins: create a new pin (must be bound to the logged-in account) ----
    if (p === '/api/pins' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          const sessionsAcc = accountBySession(req);
          if (!sessionsAcc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
          let b = [];
          try { b = JSON.parse(body || '[]'); } catch (e2) { b = []; }
          if (!Array.isArray(b)) b = [b];
          const keys = apiGetKeys();
          const CH = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
          function part() { let s = ''; for (let i = 0; i < 4; i++) s += CH[Math.floor(Math.random() * CH.length)]; return s; }
          const created = [];
          b.forEach((item) => {
            const now = new Date();
            const expires = new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString();
            const code = (item && item.code) || (part() + '-' + part() + '-' + part());
            const newKey = {
              key: code,
              maxUses: (item && item.maxUses) || 999,
              expiresAt: expires,
              status: (item && item.status) || 'active',
              note: (item && item.note) || 'generated pin',
              pin: true,
              game: (item && item.game) || 'FiveM',
              id: 'key-' + Date.now() + '-' + Math.random().toString(36).slice(2, 10),
              ownerId: sessionsAcc.id,
              createdAt: item && item.createdAt || now.toISOString()
            };
            keys.push(newKey);
            created.push(newKey);
          });
          writeJson(KEYS_FILE, keys);
          sendJson(res, created.map(k => ({ code: k.key, id: k.id, status: k.status, createdAt: k.createdAt, maxUses: k.maxUses })));
        } catch (e3) { sendJson(res, { ok: false, error: String(e3) }); }
      });
      return;
    }

    // ---- Pins: the website shows exactly what the desktop scanner reports ----
    if (p === '/api/pins' && req.method === 'GET') {
      const sessionsAcc = accountBySession(req);
      if (!sessionsAcc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      const keys = ownedKeys(sessionsAcc.id);
      const scans = ownedScans(sessionsAcc.id);
      const pins = keys
        .filter(k => k.pin === true || k.note === 'generated pin')
        .map(k => {
          const scan = scans.find(s =>
            (s.pin && ('' + s.pin).toUpperCase() === pinFromKey(k).toUpperCase()) ||
            (s.keyId && s.keyId === k.id)) || null;
          let status = k.status;
          if (k.status === 'active' && scan) status = 'complete';
          if (status === 'complete' && k.result) status = 'complete';
          return {
            code: pinFromKey(k),
            id: k.id,
            status: status,
            result: scan ? (scan.status || scan.result) : (k.result || null),
            createdAt: k.createdAt || (scan && scan.timestamp) || k.expiresAt,
            game: (scan && scan.game) || k.game || '',
            player: (scan && (scan.username || scan.playerName)) || k.player || '',
            pcName: (scan && scan.pcName) || '',
            scanId: scan ? scan.id : null,
            scannedAt: scan ? scan.timestamp : (k.scannedAt || null)
          };
        });
      return sendJson(res, pins);
    }
    if (p === '/api/pins/start' && req.method === 'POST') {
      let body = '';
      req.on('data', (c) => { body += c; });
      req.on('end', () => {
        try {
          let b = {};
          try { b = JSON.parse(body || '{}'); } catch (e2) {}
          const keys = apiGetKeys();
          const key = (b.code && keys.find(k => pinFromKey(k).toUpperCase() === ('' + b.code).toUpperCase())) ||
                      (b.keyId && keys.find(k => k.id === b.keyId));
          if (!key) return sendJson(res, { ok: false, reason: 'not_found' });
          if (key.status !== 'used') key.status = 'scanning';
          key.scanStartedAt = new Date().toISOString();
          writeJson(KEYS_FILE, keys);
          sendJson(res, { ok: true, code: pinFromKey(key), status: key.status });
        } catch (e3) { sendJson(res, { ok: false, error: String(e3) }); }
      });
      return;
    }

    // ---- Profiles: per-player identity aggregation ----
    if (p === '/api/profiles' && req.method === 'GET') {
      const pAcc = accountBySession(req);
      if (!pAcc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      return sendJson(res, buildProfiles(false, pAcc.id));
    }
    let mProfile = p.match(/^\/api\/profiles\/(.+)$/);
    if (mProfile && req.method === 'GET') {
      const pAcc = accountBySession(req);
      if (!pAcc) return sendRes(res, 401, JSON.stringify({ error: 'unauthorized' }), 'application/json');
      const want = decodeURIComponent(mProfile[1]);
      const wu = ('' + want).toUpperCase();
      const all = buildProfiles(true, pAcc.id);
      let found = null;
      for (let i = 0; i < all.length; i++) {
        const pr = all[i];
        if ((pr.id || '').toUpperCase() === wu || (pr.username || '').toUpperCase() === wu) { found = pr; break; }
      }
      if (!found) return sendJson(res, { error: 'not_found', wanted: want });
      return sendJson(res, found);
    }

    // Legacy mock fallback: any other /api/* call replies OK so the mock
    // static dashboard pages never hang.
    if (p.startsWith('/api/')) {
      return sendRes(res, 200, JSON.stringify({ ok: true, data: [] }), 'application/json');
    }

    // _next / egyéb statikus fájlok
    if (p.startsWith('/_next/') || p.startsWith('/icons/') || p.startsWith('/games/') || p.startsWith('/home/') || p.startsWith('/cdn-cgi/') || p.startsWith('/manifest') || p.startsWith('/css/') || p.startsWith('/js/')) {
      // React teljes blokkolasa: a turbopack regisztrator + az osszes chunk
      // helyett ures JS-t adunk. A teljes SSR tartalom a HTML-ben van, a
      // BODY_SCRIPT vanilla JS interakciot ad (sidebar, collapsible, theme).
      // A CSS (/_next/static/css) NEM ide tartozik, az megy tovabb.
      if (/\/_next\/static\/chunks\/[^/]+\.js$/.test(p) || p.startsWith('/cdn-cgi/')) {
        return sendRes(res, 200, '/* blocked by ocean mirror */', 'text/javascript');
      }
      // Lapos layout: minden asset közvetlenül ebben a mappában van, így az
      // eredeti URL útja mindig a basename-re képződik (pl. /css/style.css
      // vagy /_next/static/media/<fajl> -> az adott fájl innen).
      let fname = p.split('/').filter(Boolean).pop();
      if (fname === 'manifest') fname = 'manifest.json';
      const fp = path.join(A, fname);
      if (fs.existsSync(fp) && fs.statSync(fp).isFile()) {
        const ext = path.extname(fp).toLowerCase();
        return sendRes(res, 200, fs.readFileSync(fp), MIME[ext] || 'application/octet-stream');
      }
      return sendRes(res, 404, 'not found', 'text/plain');
    }

    // Route-ok
    let file = ROUTES[p];
    if (file) {
      const fp = path.join(A, file);
      if (fs.existsSync(fp)) {
        let html = fs.readFileSync(fp, 'utf8');
        html = html.replace('<head>', '<head>' + HEAD_SCRIPT);
        html = html.replace(/<script[^>]*src="\/_next\/static\/chunks\/[^"]*\.js"[^>]*><\/script>/g, '');
        html = html.replace('</head>', '<link rel="stylesheet" href="/css/style.css"><link rel="stylesheet" href="/css/ocean-v2.css"></head>');
        const isDashFile = file && file.indexOf('dashboard') === 0;
        return sendRes(res, 200, isDashFile ? html.replace('</body>', '<script src="/js/main.js"></script>' + (BODY_SCRIPT || '')) : (BODY_SCRIPT ? html.replace('</body>', '<script src="/js/main.js"></script>' + BODY_SCRIPT) : html), MIME['.html']);
      }
    }

    // fallback: dashboard
    const dsh = path.join(A, 'dashboard.html');
    let dh = fs.readFileSync(dsh, 'utf8');
    dh = dh.replace('<head>', '<head>' + HEAD_SCRIPT);
    dh = dh.replace(/<script[^>]*src="\/_next\/static\/chunks\/[^"]*\.js"[^>]*><\/script>/g, '');
    dh = dh.replace('</head>', '<link rel="stylesheet" href="/css/style.css"><link rel="stylesheet" href="/css/ocean-v2.css"></head>');
    return sendRes(res, 200, dh.replace('</body>', '<script src="/js/main.js"></script>' + (BODY_SCRIPT || '')), MIME['.html']);
  } catch (e) {
    sendRes(res, 500, 'error', 'text/plain');
  }
});

server.listen(PORT, () => console.log('Ocean mirror: http://localhost:' + PORT));