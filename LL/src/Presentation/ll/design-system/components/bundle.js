/* @ds-bundle: {"format":4,"namespace":"LL","components":[{"name":"GameShell"},{"name":"TopBar"},{"name":"NavRail"},{"name":"TabStrip"},{"name":"Stage"},{"name":"Page"},{"name":"PageHeader"},{"name":"Banner"},{"name":"Folio"},{"name":"Panel"},{"name":"StatFigure"},{"name":"LevelPlate"},{"name":"ProfileIdentity"},{"name":"Activity"},{"name":"Objective"},{"name":"Notice"},{"name":"Ledger"},{"name":"Meter"},{"name":"Track"},{"name":"StatTile"},{"name":"Delta"},{"name":"Sigil"},{"name":"Constellation"},{"name":"JourneyCard"},{"name":"LoadoutSlot"},{"name":"Chronicle"},{"name":"ItemLink"},{"name":"EntryList"},{"name":"ListRow"},{"name":"ItemSlot"},{"name":"Tag"},{"name":"Presence"},{"name":"CurrencyPill"},{"name":"Button"},{"name":"KeyHints"},{"name":"SearchField"},{"name":"Heading"},{"name":"SectionRule"},{"name":"Emblem"},{"name":"Icon"}]} */
(function () {
  'use strict';
  var R = window.React;
  var h = R.createElement;
  var F = R.Fragment;

  function cx() {
    var out = [];
    for (var i = 0; i < arguments.length; i++) if (arguments[i]) out.push(arguments[i]);
    return out.join(' ');
  }
  /* ---------- Numbers (Foundations · Numerals) ---------- */
  var MINUS = '\u2212', NDASH = '\u2013', TIMES = '\u00D7', NONE = '\u2014', NBSP = '\u00A0';
  function missing(n) { return n == null || n === '' || (typeof n === 'number' && !isFinite(n)); }
  // 12,480 · −12 (a true minus) · — for unknown or not applicable · a fixed number of decimals when given.
  function fmt(n, digits) {
    if (missing(n)) return NONE;
    if (typeof n !== 'number') return typeof n === 'string' ? n.replace(/^-(?=[\d.])/, MINUS) : n;
    var o = digits != null ? { minimumFractionDigits: digits, maximumFractionDigits: digits } : undefined;
    var s = Math.abs(n).toLocaleString('en-US', o);
    return n < 0 && /[1-9]/.test(s) ? MINUS + s : s;
  }
  function short(n) {
    if (typeof n !== 'number' || missing(n)) return fmt(n);
    var a = Math.abs(n), sg = n < 0 ? MINUS : '';
    if (a >= 1e9) return sg + (a / 1e9).toFixed(a >= 1e10 ? 0 : 1).replace(/\.0$/, '') + 'B';
    if (a >= 1e6) return sg + (a / 1e6).toFixed(a >= 1e7 ? 0 : 1).replace(/\.0$/, '') + 'M';
    if (a >= 1e4) return sg + (a / 1e3).toFixed(a >= 1e5 ? 0 : 1).replace(/\.0$/, '') + 'k';
    return fmt(n);
  }
  function range(a, b, digits) { return fmt(a, digits) + NDASH + fmt(b, digits); }            // 12–18
  function times(x, digits) { return missing(x) ? NONE : TIMES + fmt(x, digits); }            // ×1.5
  function fraction(v, max) { return fmt(v) + NBSP + '/' + NBSP + fmt(max); }               // 3,120 / 4,150
  function percent(x, digits) { return missing(x) ? NONE : fmt(x, digits) + '%'; }           // 24.8%
  // A unit follows its number: a symbol unit attaches (24.8%, 12s), a word unit takes a non-breaking space (84 HP/5s).
  function unit(x, u, digits) { return missing(x) ? NONE : fmt(x, digits) + (/^(%|ms|[smhd])$/.test(u) ? '' : NBSP) + u; }
  // Splits "184.6 threat/s" into sign-and-integer, fraction and unit, so a column can align on the decimal point.
  function numParts(v) {
    if (typeof v === 'number') v = fmt(v);
    if (typeof v !== 'string') return null;
    var t = v.trim();
    if (t === NONE) return { int: NONE, frac: '', unit: '', sp: false };
    // A range (18–24) or a fraction (7 / 20) stays whole in the integer column; only a unit after it is split off.
    var g = /^([+\u2212\-]?[\d,]+(?:\.\d+)?(?:\u2013[\d,]+(?:\.\d+)?|\u00A0\/\u00A0[\d,]+(?:\.\d+)?))([\s\u00A0]*)(.*)$/.exec(t);
    if (g) return { int: g[1].replace(/^-/, MINUS), frac: '', unit: g[3] || '', sp: !!g[2] && !!g[3] };
    var m = /^([+\u2212\-]?\u00D7?[\d,]+)(\.\d+)?([\s\u00A0]*)(.*)$/.exec(t);
    if (!m) return null;
    return { int: m[1].replace(/^-/, MINUS), frac: m[2] || '', unit: m[4] || '', sp: !!m[3] && !!m[4] };
  }
  // A value with its unit set smaller and muted right after it: 84 HP/5s, 12s, 24.8%.
  /* ---------- Layer stack (Foundations · Surfaces & Layering) ---------- */
  // One stack for everything Escape closes. Escape closes the topmost layer — the highest z, then the latest opened — and
  // focus returns to the element that opened it. A layer can trap Tab (a dialog, a confirmation, the tour). Toasts are not
  // layers: they never take focus or Escape. Only one modal at a time; a confirmation overlays the dialog that opened it.
  var LAYER_Z = { popover: 100, modal: 200, confirm: 250, 'popover-detached': 300, tour: 500, drag: 600 };
  var LAYERS = [], layerSeq = 0;
  // `scope` limits a layer to keys pressed inside an element — for an embedded widget or a catalogue preview that runs
  // several screens side by side. The game itself has one stack and no scope.
  function inScope(l, node) { var sc = resolveEls(l.scope)[0]; return !sc || !node || sc.contains(node); }
  function layerTop(node) {
    var t = null;
    LAYERS.forEach(function (l) { if (inScope(l, node) && (!t || l.z > t.z || (l.z === t.z && l.seq > t.seq))) t = l; });
    return t;
  }
  function resolveEls(x) {
    if (typeof x === 'function') x = x();
    if (!x) return [];
    return (Array.isArray(x) ? x : [x]).map(function (e) { return e && e.current !== undefined ? e.current : e; }).filter(Boolean);
  }
  var FOCUSABLE = 'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
  function focusablesIn(els) {
    var out = [];
    els.forEach(function (el) {
      if (el.matches && el.matches(FOCUSABLE)) out.push(el);
      Array.prototype.forEach.call(el.querySelectorAll(FOCUSABLE), function (f) { out.push(f); });
    });
    return out.filter(function (f) { return !f.closest('[inert]') && f.getClientRects().length > 0; });
  }
  function onLayerKey(ev) {
    var t = layerTop(ev.target);
    if (!t) return;
    if (ev.key === 'Escape' && !ev.defaultPrevented) { ev.preventDefault(); if (t.onClose) t.onClose(); return; }
    if (ev.key !== 'Tab' || !t.trap) return;
    var els = resolveEls(t.trap), f = focusablesIn(els);
    if (!f.length) { ev.preventDefault(); return; }
    // Tab moves through the trap's focusables in order and wraps, even when they sit apart in the page
    // (the tour's coach mark and the spotlighted item).
    var a = document.activeElement, i = f.indexOf(a);
    ev.preventDefault();
    if (i < 0) { (ev.shiftKey ? f[f.length - 1] : f[0]).focus(); return; }
    f[(i + (ev.shiftKey ? f.length - 1 : 1)) % f.length].focus();
  }
  function openLayer(o) {
    o = o || {};
    var kind = LAYER_Z[o.kind] ? o.kind : 'popover';
    var sc = resolveEls(o.scope)[0];
    if (kind === 'modal' && LAYERS.some(function (l) { return l.kind === 'modal' && resolveEls(l.scope)[0] === sc; }) && window.console) console.warn('LL.layers: one modal at a time. Change the open dialog\'s content, or close it first.');
    var e = { kind: kind, z: LAYER_Z[kind], seq: ++layerSeq, onClose: o.onClose, trap: o.trap, scope: o.scope,
      opener: resolveEls(o.opener)[0] || document.activeElement };
    LAYERS.push(e);
    if (LAYERS.length === 1) document.addEventListener('keydown', onLayerKey);
    return {
      close: function (restore) {
        var i = LAYERS.indexOf(e);
        if (i < 0) return;
        LAYERS.splice(i, 1);
        if (!LAYERS.length) document.removeEventListener('keydown', onLayerKey);
        var t = e.opener;
        if (restore !== false && t && t.isConnected && typeof t.focus === 'function') t.focus();
      },
      isTop: function () { return layerTop(resolveEls(e.scope)[0]) === e; }
    };
  }
  // React: register a layer while `open` is true. Closing (or unmounting) returns focus to the opener.
  function useLayer(open, o) {
    o = o || {};
    var cb = R.useRef(o.onClose); cb.current = o.onClose;
    var opt = R.useRef(o); opt.current = o;
    R.useEffect(function () {
      if (!open) return;
      var hnd = openLayer({ kind: opt.current.kind, trap: function () { return resolveEls(opt.current.trap); }, opener: opt.current.opener, scope: opt.current.scope,
        onClose: function () { if (cb.current) cb.current(); } });
      return function () { hnd.close(opt.current.restoreFocus !== false); };
    }, [open]);
  }

  /* ---------- Announcer: throttled live regions (Foundations · Accessibility · Live regions) ---------- */
  // announce(text, { key, assertive }). Polite messages go out one at a time, at most every 1.5s; a message with the
  // same key replaces the one still waiting (so ten loot drops become one line); the same text is not repeated
  // within 5s; at most three wait. assertive is for errors only and interrupts.
  var AN = null;
  function anRegion(pol) {
    var el = document.createElement('div');
    el.className = 'lg-sr lg-announcer';
    el.setAttribute('aria-live', pol); el.setAttribute('aria-atomic', 'true');
    document.body.appendChild(el);
    return el;
  }
  function anWrite(el, text) { el.textContent = ''; setTimeout(function () { el.textContent = text; }, 60); }
  function anFlush() {
    var q = AN.queue.shift();
    if (!q) { AN.timer = null; return; }
    anWrite(AN.polite, q.text); AN.last[q.key] = { text: q.text, at: Date.now() };
    AN.timer = setTimeout(anFlush, 1500);
  }
  function announce(text, o) {
    o = o || {};
    if (!text || typeof document === 'undefined') return;
    if (!AN) AN = { polite: anRegion('polite'), assertive: anRegion('assertive'), queue: [], last: {}, timer: null };
    var key = o.key || text, seen = AN.last[key];
    if (seen && seen.text === text && Date.now() - seen.at < 5000) return;
    if (o.assertive) { anWrite(AN.assertive, text); AN.last[key] = { text: text, at: Date.now() }; return; }
    var i = -1;
    for (var k = 0; k < AN.queue.length; k++) if (AN.queue[k].key === key) i = k;
    if (i >= 0) AN.queue[i].text = text; else AN.queue.push({ key: key, text: text });
    if (AN.queue.length > 3) AN.queue.shift();
    if (!AN.timer) anFlush();
  }
  // Focus helpers for roving tabindex.
  function moveKey(key, i, n) {
    if (key === 'ArrowDown' || key === 'ArrowRight') return Math.min(n - 1, i + 1);
    if (key === 'ArrowUp' || key === 'ArrowLeft') return Math.max(0, i - 1);
    if (key === 'Home') return 0;
    if (key === 'End') return n - 1;
    return null;
  }

  function numNode(v, cls) {
    var q = numParts(v);
    if (!q || !q.unit) return typeof v === 'number' || missing(v) ? fmt(v) : v;
    return h(F, null, q.int + q.frac, h('span', { className: cls || 'lg-unit' }, (q.sp ? NBSP : '') + q.unit));
  }
  function omit(o, keys) {
    var r = {};
    for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k) && keys.indexOf(k) < 0) r[k] = o[k];
    return r;
  }
  function useId(prefix) {
    var id = R.useId ? R.useId() : String(Math.random()).slice(2);
    return (prefix || 'lg') + id.replace(/[^A-Za-z0-9_-]/g, '');
  }
  // Two props objects as one: handlers both run, in order; anything else, the second wins.
  function chain(a, b) {
    var out = Object.assign({}, a);
    for (var k in b) {
      if (!Object.prototype.hasOwnProperty.call(b, k)) continue;
      var x = out[k], y = b[k];
      out[k] = typeof x === 'function' && typeof y === 'function' && /^on[A-Z]/.test(k) ? function (x, y) { return function (e) { x(e); y(e); }; }(x, y) : y;
    }
    return out;
  }

  /* ---------- Durations (Foundations · Numerals): two units at most ---------- */
  var DUR_UNITS = [['d', 86400, 'day'], ['h', 3600, 'hour'], ['m', 60, 'minute'], ['s', 1, 'second']];
  function durParts(sec) {
    var s = Math.max(0, Math.round(sec)), out = [];
    for (var i = 0; i < DUR_UNITS.length; i++) {
      var n = Math.floor(s / DUR_UNITS[i][1]);
      if (!out.length && n === 0 && i < DUR_UNITS.length - 1) continue;
      if (out.length && n === 0) break;
      out.push([n, DUR_UNITS[i]]); s -= n * DUR_UNITS[i][1];
      if (out.length === 2) break;
    }
    return out;
  }
  // 4m 12s · 2h 14m · 3d 4h · 12s
  function duration(sec) {
    if (missing(sec)) return NONE;
    return durParts(sec).map(function (q) { return q[0] + q[1][0]; }).join(' ');
  }
  // "4 minutes 12 seconds", for screen readers.
  function spokenDuration(sec) {
    if (missing(sec)) return 'unknown';
    return durParts(sec).map(function (q) { return q[0] + ' ' + q[1][2] + (q[0] === 1 ? '' : 's'); }).join(' ');
  }

  /* ---------- States (Standards · States) ----------
     Every state in the model: its family, its word, the Tag tone and glyph it takes, and what screen readers hear after
     a name. `blocks` marks the states that stop a press and must give a reason. Tag's `state` reads this, so a state's
     words are the same everywhere. */
  function st(family, word, tone, sr, extra) { return Object.assign({ family: family, word: word, tone: tone || null, sr: sr || null }, extra); }
  var STATES = {
    'default': st('interaction', null), hover: st('interaction', null), 'focus-visible': st('interaction', null),
    pressed: st('interaction', null, null, 'pressed'), selected: st('interaction', null, null, 'selected'),
    current: st('interaction', null, null, 'current'), dragging: st('interaction', null, null, 'grabbed'),
    disabled: st('interaction', null, null, 'unavailable'),
    available: st('availability', null), ready: st('availability', 'Ready', 'new', 'ready'),
    unavailable: st('availability', null, null, 'unavailable', { blocks: true }),
    locked: st('availability', 'Locked', 'locked', 'locked', { blocks: true }),
    restricted: st('availability', 'Restricted', null, 'unavailable', { blocks: true }),
    insufficient: st('availability', 'Short by', 'warning', 'unavailable', { blocks: true }),
    cooldown: st('availability', 'Ready in', null, 'unavailable', { blocks: true }),
    'new': st('lifecycle', 'New', 'new', 'new', { glyph: '+' }), unread: st('lifecycle', 'Unread', 'new', 'unread'),
    'in-progress': st('lifecycle', 'In progress', null, 'in progress'), completed: st('lifecycle', 'Completed', 'success', 'completed'),
    claimable: st('lifecycle', 'Claimable', 'new', 'claimable'), claimed: st('lifecycle', 'Claimed', null, 'claimed'),
    opened: st('lifecycle', 'Opened', null, 'opened'), expiring: st('lifecycle', 'Expires in', 'warning', 'expires in'),
    expired: st('lifecycle', 'Expired', null, 'expired'), failed: st('lifecycle', 'Failed', 'danger', 'failed', { glyph: '✕' }),
    owned: st('ownership', 'Owned', null, 'owned'), 'not-owned': st('ownership', 'Not owned', null, 'not owned'),
    equipped: st('ownership', 'Equipped', null, 'equipped'), attuned: st('ownership', 'Attuned', null, 'attuned'),
    assigned: st('ownership', 'Assigned', null, 'assigned'), captured: st('ownership', 'In snapshot', null, 'in snapshot'),
    listed: st('ownership', 'Listed', null, 'listed'), escrow: st('ownership', 'In escrow', null, 'in escrow'),
    borrowed: st('ownership', 'Borrowed', null, 'borrowed'), favourite: st('ownership', 'Favourite', null, 'favourite'),
    discovered: st('knowledge', null), undiscovered: st('knowledge', 'Undiscovered', null, 'undiscovered'),
    hidden: st('knowledge', null), unknown: st('knowledge', NONE, null, 'unknown'),
    loading: st('data', 'Loading…', null, 'loading'), refreshing: st('data', 'Updating…', null, null),
    pending: st('data', 'Saving…', null, 'busy'), stale: st('data', 'Updated', null, null),
    error: st('data', null, 'danger', null, { glyph: '✕' }), empty: st('data', null), offline: st('data', 'Reconnecting…', 'warning', null)
  };
  // The one Tag on a row, first to last (Standards · State combinations).
  var TAG_ORDER = ['failed', 'claimable', 'expiring', 'locked', 'listed', 'escrow', 'borrowed', 'equipped', 'attuned', 'assigned', 'captured', 'in-progress', 'new', 'completed', 'claimed', 'expired', 'opened'];
  function firstState(list) {
    var best = null, at = Infinity;
    (list || []).forEach(function (s) { var i = TAG_ORDER.indexOf(s); if (i >= 0 && i < at) { at = i; best = s; } });
    return best;
  }
  var stateWarned = {};
  function stateWarn(key, msg) { if (stateWarned[key] || typeof console === 'undefined') return; stateWarned[key] = true; console.warn('LL: ' + msg + ' (Standards · States).'); }
  // The attention mark (Standards · State combinations): the arcana-glow diamond for Ready, in a fixed place. `ready` is
  // true or the words ("Upgrade available"); the words are what screen readers hear and what the Folio says.
  function readyWords(ready) { return ready ? (typeof ready === 'string' ? ready : STATES.ready.word) : null; }
  function attention(cls) { return h('span', { className: cx('lg-attention', cls), 'aria-hidden': 'true' }); }

  /* ---------- The reason tip (Standards · States · The reason tip) ----------
     A blocked control stays focusable and says why. One tip serves the page, drawn on the body so no scrolling region
     clips it: it shows beside the control on hover and keyboard focus; a click or tap pins it, so touch can read it;
     pressing again, Escape, leaving or tabbing on closes it, and it can be hovered without closing (WCAG 1.4.13).
     The same words are the control's description, so screen readers hear them on focus, and a press announces them. */
  var WHY = null;
  function whyLayer() {
    if (WHY) return WHY;
    if (typeof document === 'undefined' || !document.body) return null;
    var el = document.createElement('div');
    // The control's description is what is read; the float is for the eye.
    el.className = 'lg-why'; el.setAttribute('aria-hidden', 'true');
    document.body.appendChild(el);
    WHY = { el: el, owner: null, pinned: false, timer: null, opts: null };
    el.addEventListener('mouseenter', function () { clearTimeout(WHY.timer); });
    el.addEventListener('mouseleave', function () { if (!WHY.pinned) whyLater(); });
    document.addEventListener('pointerdown', function (e) {
      if (WHY.owner && !WHY.owner.contains(e.target) && !el.contains(e.target)) whyHide();
    }, true);
    // Escape closes the tip first, before anything else hears it — a drawer or dialog closes on the next Escape.
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && WHY.owner) { whyHide(); e.stopPropagation(); e.preventDefault(); }
    }, true);
    window.addEventListener('scroll', function () { if (WHY.owner) whyPlace(); }, true);
    window.addEventListener('resize', function () { if (WHY.owner) whyPlace(); });
    return WHY;
  }
  function whyFill(o) {
    var w = WHY, el = w.el;
    el.textContent = '';
    // A title names the control when its own label isn't visible (the compact rail); it is for the eye only.
    if (o.title) { var t = document.createElement('span'); t.className = 'lg-why__title'; t.textContent = o.title; el.appendChild(t); }
    if (o.word) { var a = document.createElement('span'); a.className = 'lg-why__word'; a.textContent = o.word; el.appendChild(a); }
    String(o.reason || '').split('\n').forEach(function (line) {
      var b = document.createElement('span'); b.className = 'lg-why__reason'; b.textContent = line; el.appendChild(b);
    });
    w.opts = o;
  }
  function whyPlace() {
    var w = WHY, o = w.owner;
    if (!o || !o.isConnected) { whyHide(); return; }
    var r = o.getBoundingClientRect(), vw = window.innerWidth, vh = window.innerHeight;
    if (r.bottom < 0 || r.top > vh || (r.width === 0 && r.height === 0)) { whyHide(); return; }
    var tw = w.el.offsetWidth, th = w.el.offsetHeight, gap = 6, x, y, end = w.opts && w.opts.place === 'end';
    if (end && r.right + gap + tw <= vw - 8) { x = r.right + gap; y = r.top + r.height / 2 - th / 2; }
    else { x = r.left; y = r.bottom + gap; if (y + th > vh - 8 && r.top - gap - th >= 8) y = r.top - gap - th; end = false; }
    w.el.classList.toggle('lg-why--end', !!end);
    w.el.style.left = Math.round(Math.max(8, Math.min(x, vw - tw - 8))) + 'px';
    w.el.style.top = Math.round(Math.max(8, Math.min(y, vh - th - 8))) + 'px';
  }
  function whyShow(owner, o, pinned) {
    var w = whyLayer(); if (!w) return;
    clearTimeout(w.timer);
    if (w.owner !== owner || w.opts !== o) whyFill(o);
    w.owner = owner; w.pinned = !!pinned;
    w.el.classList.toggle('lg-why--warning', o.tone === 'warning');
    w.el.classList.toggle('is-detached', !!(owner.closest && owner.closest('[role="dialog"], [role="alertdialog"], [aria-modal="true"]')));
    if (owner.closest && owner.closest('[data-motion="reduced"]')) w.el.setAttribute('data-motion', 'reduced'); else w.el.removeAttribute('data-motion');
    whyPlace();
    if (w.owner) w.el.classList.add('is-shown');
  }
  function whyHide() {
    var w = WHY; if (!w) return;
    clearTimeout(w.timer); w.owner = null; w.pinned = false; w.el.classList.remove('is-shown');
  }
  function whyLater() {
    var w = WHY; if (!w) return;
    clearTimeout(w.timer); w.timer = setTimeout(function () { if (!w.pinned) whyHide(); }, 150);
  }
  // A press on a blocked control: pin its reason (or close it if pinned) and say it again.
  function whyPress(el, o, spoken) {
    var w = WHY;
    if (w && w.owner === el && w.pinned) { whyHide(); return; }
    if (!o.printed) whyShow(el, o, true);
    announce(spoken, { key: 'why' });
  }
  // LL.why(id, reason, { word, tone, place, spoken, printed }) → { props, desc }. Spread `props` on the focusable
  // element and render `desc` inside it: the description is aria-hidden, so it stays out of the name and is read only
  // as the description. `printed: true` means the reason is already on screen, in the element whose id is `id`: no
  // tip and no desc, but the press still announces it.
  function why(id, reason, o) {
    o = o || {};
    var opts = { word: o.word || null, reason: reason || '', tone: o.tone || null, place: o.place || null, printed: !!o.printed, title: o.title || null };
    var spoken = (opts.word ? opts.word + '. ' : '') + (o.spoken || opts.reason);
    var props = {
      'aria-disabled': 'true', 'aria-describedby': id,
      onClick: function (e) { e.preventDefault(); whyPress(e.currentTarget, opts, spoken); }
    };
    if (!opts.printed) Object.assign(props, {
      onMouseEnter: function (e) { if (!WHY || !WHY.pinned) whyShow(e.currentTarget, opts, false); },
      onMouseLeave: function (e) { if (WHY && WHY.owner === e.currentTarget && !WHY.pinned) whyLater(); },
      onFocus: function (e) { var t = e.currentTarget; if (t.matches && t.matches(':focus-visible')) whyShow(t, opts, false); },
      onBlur: function (e) { if (WHY && WHY.owner === e.currentTarget) whyHide(); },
      // A re-render while the tip is open (a cooldown ticking) refreshes its words.
      ref: function (el) { if (el && WHY && WHY.owner === el && WHY.opts && (WHY.opts.reason !== opts.reason || WHY.opts.word !== opts.word || WHY.opts.title !== opts.title)) { whyFill(opts); whyPlace(); } }
    });
    return {
      props: props, spoken: spoken,
      desc: opts.printed ? null : h('span', { id: id, className: 'lg-sr lg-why__desc', 'aria-hidden': 'true' }, spoken)
    };
  }
  // The reason a blocked part gives, built from its state: Locked's condition, a shortfall, the time left.
  function shortfallText(list) {
    var parts = (list || []).map(function (x) { return fmt(x.amount) + NBSP + x.name; });
    if (!parts.length) return '';
    return 'Short by ' + (parts.length > 1 ? parts.slice(0, -1).join(', ') + ' and ' + parts[parts.length - 1] : parts[0]);
  }
  function blockedReason(state, p, who) {
    var r = p.reason, spoken;
    if (state === 'insufficient' && p.shortfall) r = shortfallText(p.shortfall);
    if (state === 'cooldown' && typeof p.remaining === 'number') { r = 'Ready in ' + duration(p.remaining); spoken = 'Ready in ' + spokenDuration(p.remaining); }
    if (!r) stateWarn('reason:' + who + state, who + ' is ' + state + ' with no reason; ' + (state === 'locked' ? 'say how it unlocks' : 'say why'));
    return { reason: r || '', spoken: spoken, word: state === 'locked' ? 'Locked' : null, tone: state === 'insufficient' ? 'warning' : null };
  }

  /* ---------- Icon: the game's own sidebar icons, redrawn in currentColor ---------- */
  // @icons-start. Generated from design-system/icons.json by design-system/scripts/build-icons.mjs. Do not edit: change icons.json and run the script.
  var ICONS = {
    "achievements": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<circle cx=\"12\" cy=\"9\" r=\"4.5\"/><path d=\"M12 6.6l.9 1.7 1.9.3-1.4 1.3.3 1.9-1.7-.9-1.7.9.3-1.9-1.4-1.3 1.9-.3z\" stroke-width=\"1.2\"/><path d=\"M9 13l-2 7 5-2.6L17 20l-2-7\"/>"},
    "combat-styles": {"viewBox":"0 0 24 24","strokeWidth":1.75,"body":"<path d=\"M9.4 3.6a8.6 8.6 0 0 0 0 16.8\"/><path d=\"M14.6 3.6a8.6 8.6 0 0 1 0 16.8\"/><path d=\"M12 10.4v3.2\"/>"},
    "essences": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M12 3l5.5 4-2 9.5h-7L6.5 7z\"/><path d=\"M6.5 7L12 10.5 17.5 7\"/><path d=\"M12 10.5v6\"/><path d=\"M12 19.5v1.5\"/>"},
    "inventory": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M4 9h16v10a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 19z\"/><path d=\"M8 9V6.5A2.5 2.5 0 0 1 10.5 4h3A2.5 2.5 0 0 1 16 6.5V9\"/><path d=\"M4 13h16\"/><path d=\"M10.5 13v2h3v-2\"/>"},
    "overview": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M12 3l7 3v5c0 4.5-3 7.5-7 10-4-2.5-7-5.5-7-10V6z\"/><circle cx=\"12\" cy=\"10\" r=\"2.4\"/><path d=\"M8.5 16c.8-1.6 2-2.4 3.5-2.4s2.7.8 3.5 2.4\"/>"},
    "soulstones": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M12 3L5 9l7 12 7-12z\"/><path d=\"M5 9h14\"/><path d=\"M12 3L9.5 9 12 21l2.5-12z\"/>"},
    "cinder-bazaar": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M12 4v3\"/><path d=\"M4 8l2.5 5h-5L4 8zM20 8l2.5 5h-5L20 8z\"/><path d=\"M4 8h16\"/><path d=\"M1.5 13a2.5 2.5 0 0 0 5 0M17.5 13a2.5 2.5 0 0 0 5 0\"/><path d=\"M12 7v12\"/><path d=\"M8 19h8\"/>"},
    "colosseum": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M5 4l11 11\"/><path d=\"M19 4L8 15\"/><path d=\"M4.5 15.5l4 4M19.5 15.5l-4 4\"/><path d=\"M6 17l-2.5 2.5M18 17l2.5 2.5M7.5 20L5 22M16.5 20l2.5 2\"/>"},
    "guild": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M6 21V8l-2-2V4h4v2h2V4h4v2h2V4h4v2l-2 2v13\"/><path d=\"M10 21v-5a2 2 0 0 1 4 0v5\"/><path d=\"M4 21h16\"/>"},
    "leaderboard": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M4 8l4 3 4-6 4 6 4-3-1.5 9h-13z\"/><path d=\"M6 20h12\"/>"},
    "settings": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3M5.3 5.3l2.1 2.1M16.6 16.6l2.1 2.1M18.7 5.3l-2.1 2.1M7.4 16.6l-2.1 2.1\"/>"},
    "legacy-ascension": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M6 21h12\"/><path d=\"M8 21V9h8v12\"/><path d=\"M7 9V5h2v2h2V5h2v2h2V5h2v4\"/><path d=\"M10.5 21v-4a1.5 1.5 0 0 1 3 0v4\"/><path d=\"M12 2v3\"/>"},
    "prophecies": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M19 14.5A7.5 7.5 0 0 1 9.5 5 7.5 7.5 0 1 0 19 14.5z\"/><path d=\"M16.5 4.5l.5 1.5 1.5.5-1.5.5-.5 1.5-.5-1.5L14.5 6l1.5-.5z\" stroke-width=\"1.2\"/>"},
    "quest-journal": {"viewBox":"1 1 22 22","strokeWidth":1.6,"body":"<path d=\"M12 7.2v12\"/><path d=\"M12 7.2C10.2 5.6 7.6 4.9 4.5 5.2a1 1 0 0 0-.9 1v10.5a1 1 0 0 0 1.1 1c2.7-.2 5 .4 7.3 1.5\"/><path d=\"M12 7.2c1.8-1.6 4.4-2.3 7.5-2a1 1 0 0 1 .9 1v10.5a1 1 0 0 1-1.1 1c-2.7-.2-5 .4-7.3 1.5\"/><path d=\"M12 4.2 12.9 6l1.8.5-1.8.5-.9 1.8-.9-1.8-1.8-.5L11.1 6z\" fill=\"currentColor\" stroke=\"none\" opacity=\".85\"/>"},
    "world-map": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M9 4L4 6v14l5-2 6 2 5-2V4l-5 2z\"/><path d=\"M9 4v14M15 6v14\"/><circle cx=\"12\" cy=\"10\" r=\"1\" fill=\"currentColor\" stroke=\"none\"/>"},
    // The Nobility mark: a filled crown, drawn at 12px (icon-marker) beside a name and 16px beside a screen title (Registries · Nobility mark). Not a sidebar icon.
    "nobility": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M3.2 17.6 2.6 7.4l5.3 4.4L12 4l4.1 7.8 5.3-4.4-.6 10.2z\" fill=\"currentColor\" stroke=\"none\"/><path d=\"M3.6 20.2h16.8\" stroke-width=\"2.2\" stroke-linecap=\"butt\"/>","marker":true},
    // The Locked marker: a closed padlock, drawn at 12px, which the NavRail shows on a locked destination in place of the word (D-113). Not a sidebar icon.
    "lock": {"viewBox":"0 0 24 24","strokeWidth":1.6,"body":"<path d=\"M7.5 11V8.2a4.5 4.5 0 0 1 9 0V11\" fill=\"none\" stroke-width=\"2.4\" stroke-linecap=\"butt\"/><rect x=\"4.5\" y=\"11\" width=\"15\" height=\"10\" rx=\"1.6\" fill=\"currentColor\" stroke=\"none\"/>","marker":true}
  };
  // @icons-end
  // The size scale (Foundations · Iconography): line icons at 16, 20 or 24px; 12px only for the solid inline markers.
  var ICON_SIZES = [12, 16, 20, 24];
  // The solid inline markers ("marker": true in icons.json); every other icon is a line icon.
  var ICON_MARKERS = Object.keys(ICONS).filter(function (n) { return ICONS[n].marker; });
  var iconWarned = {};
  function iconWarn(key, msg) { if (iconWarned[key] || typeof console === 'undefined') return; iconWarned[key] = true; console.warn('LL.Icon: ' + msg + ' (Foundations · Iconography).'); }
  function Icon(p) {
    var d = ICONS[p.name];
    // An unknown name draws nothing: the label beside it is the fallback, so a missing icon never leaves a hole.
    if (!d) { if (p.name) iconWarn('name:' + p.name, '"' + p.name + '" is not in the set; its label stands alone'); return null; }
    var s = p.size || 20;
    if (ICON_SIZES.indexOf(s) < 0) iconWarn('size:' + s, s + 'px is off the scale; use 16, 20 or 24, or 12 for a marker');
    else if (s < 16 && ICON_MARKERS.indexOf(p.name) < 0) iconWarn('small:' + p.name, '"' + p.name + '" is a line icon; line icons stop at 16px, and 12px is for markers');
    return h('svg', {
      className: cx('lg-icon', p.className),
      // The size is set in rem, so icons scale with the reading-size setting (Foundations · Accessibility).
      width: s, height: s, viewBox: d.viewBox, style: { '--lg-icon-size': (s / 16) + 'rem' },
      fill: 'none', stroke: 'currentColor', strokeWidth: p.strokeWidth || d.strokeWidth,
      strokeLinecap: 'round', strokeLinejoin: 'round',
      role: p.title ? 'img' : undefined,
      'aria-label': p.title || undefined,
      'aria-hidden': p.title ? undefined : 'true',
      focusable: 'false',
      dangerouslySetInnerHTML: { __html: d.body }
    });
  }
  Icon.names = Object.keys(ICONS);

  /* ---------- geometry ---------- */
  function ngon(n, r, cx0, cy0, rot) {
    var pts = [];
    for (var i = 0; i < n; i++) {
      var a = (rot || -90) * Math.PI / 180 + i * 2 * Math.PI / n;
      pts.push([cx0 + r * Math.cos(a), cy0 + r * Math.sin(a)]);
    }
    return pts;
  }
  function ptsStr(pts) {
    return pts.map(function (p) { return p[0].toFixed(2) + ',' + p[1].toFixed(2); }).join(' ');
  }
  var HEX = ptsStr(ngon(6, 47, 50, 50, -90));
  var HEX_IN = ptsStr(ngon(6, 39, 50, 50, -90));

  /* ---------- Heading ---------- */
  function Heading(p) {
    var lvl = p.level || 'screen';
    var T = p.as || (lvl === 'screen' ? 'h1' : lvl === 'folio' ? 'h2' : lvl === 'subsection' ? 'h4' : 'h3');
    return h(T, { className: cx('lg-heading', 'lg-heading--' + lvl, p.className), id: p.id },
      p.sub ? h('span', { className: 'lg-heading__sub' }, p.sub, ' ') : null,
      h('span', { className: 'lg-heading__main' }, p.children));
  }

  /* ---------- Button + key caps ---------- */
  function Key(p) {
    return h('kbd', { className: cx('lg-key', p.className) }, p.children);
  }
  // A Button is available, plain disabled (`disabled`: rare, when the limit is plain beside it), blocked with a reason
  // (unavailable, locked, restricted, insufficient, cooldown) or pending (Standards · States). A blocked Button stays
  // focusable (`aria-disabled`), shows its reason tip on hover and focus, and answers a press with the reason.
  function Button(p) {
    var v = p.variant || 'primary';
    var state = p.state && p.state !== 'available' ? p.state : null;
    var blocked = !!(state && STATES[state] && STATES[state].blocks), pending = state === 'pending';
    var id = useId('lgb');
    var w = null;
    if (blocked) { var br = blockedReason(state, p, 'Button "' + (typeof p.children === 'string' ? p.children : '') + '"'); w = why(id + '-why', br.reason, br); }
    var rest = omit(p, ['variant', 'size', 'hotkey', 'className', 'children', 'icon', 'density', 'state', 'reason', 'shortfall', 'remaining', 'pendingLabel']);
    if (blocked || pending) { delete rest.onClick; delete rest.disabled; }
    var props = Object.assign({ type: 'button' }, rest, {
      className: cx('lg-btn', 'lg-btn--' + v, p.size === 'sm' && 'lg-btn--sm', p.size === 'md' && 'lg-btn--md', blocked && 'is-blocked', state && 'is-' + state, p.className),
      'data-density': p.density, 'aria-keyshortcuts': p.hotkey || undefined
    });
    if (pending) Object.assign(props, { 'aria-busy': 'true', 'aria-disabled': 'true' });
    if (w) props = chain(props, w.props);
    // With a pendingLabel the Button keeps room for both labels, so "Claim" becoming "Claiming…" shifts nothing.
    var stacked = pending || p.pendingLabel;
    var label = stacked
      ? h('span', { className: 'lg-btn__labels' },
          h('span', { className: cx('lg-btn__label', pending && 'is-off'), 'aria-hidden': pending ? 'true' : undefined }, p.children),
          h('span', { className: cx('lg-btn__label', !pending && 'is-off'), 'aria-hidden': pending ? undefined : 'true' }, p.pendingLabel || STATES.pending.word))
      : h('span', { className: 'lg-btn__label' }, p.children);
    return h('button', props,
      p.icon ? h(Icon, { name: p.icon, size: p.size === 'sm' ? 16 : 20 }) : null,
      label,
      p.hotkey ? h(Key, null, p.hotkey) : null,
      w ? w.desc : null);
  }
  function KeyHints(p) {
    return h('div', { className: cx('lg-keyhints', p.className), role: 'group', 'aria-label': p.label || 'Keyboard shortcuts' },
      (p.hints || []).map(function (k) {
        return h('span', { key: k.label + k.key, className: 'lg-keyhints__item' },
          h('span', null, k.label), h(Key, null, k.key));
      }));
  }

  /* ---------- Tag ---------- */
  // `state` takes the word, tone and glyph from LL.states, so "Locked", "Claimable" or "Expires in 2h" read the same
  // everywhere; children replace the word ("In Preset 2" for assigned), `value` follows it out of capitals.
  function Tag(p) {
    var s = p.state ? STATES[p.state] : null;
    if (p.state && !s) stateWarn('tag:' + p.state, '"' + p.state + '" is not a state');
    var tone = p.tone || (s && s.tone) || 'neutral';
    var word = p.children != null ? p.children : s ? s.word : null;
    var glyph = tone === 'success' ? '\u2713' : s && s.glyph ? s.glyph : null;
    var sr = tone === 'beneficial' ? ', beneficial' : tone === 'harmful' ? ', harmful' : null;
    return h('span', { className: cx('lg-tag', 'lg-tag--' + tone, p.state && 'lg-tag--is-' + p.state, p.className), 'aria-hidden': p['aria-hidden'] },
      glyph ? h('span', { className: 'lg-tag__glyph', 'aria-hidden': 'true' }, glyph) : null,
      word,
      p.value != null ? h('span', { className: 'lg-tag__value' }, fmt(p.value)) : null,
      sr ? h('span', { className: 'lg-sr' }, sr) : null);
  }

  /* ---------- Sigil (hex stat badge) ---------- */
  function Sigil(p) {
    var size = p.size || 'md';
    var pos = p.labelPosition || (p.label ? 'right' : 'none');
    var state = p.state || 'default';
    var interactive = typeof p.onClick === 'function';
    var id = useId('lgs');
    // A locked Sigil that would be a button stays one: focusable, aria-disabled, with its unlock condition as the
    // reason tip, and a press that shows the condition instead of selecting (Standards · States).
    var w = interactive && state === 'locked' ? why(id + '-why', blockedReason('locked', p, 'Sigil "' + (p.label || '') + '"').reason, { word: 'Locked' }) : null;
    var badge = h('span', { className: 'lg-sigil__badge', key: 'b' },
      h('svg', { className: 'lg-sigil__hex', viewBox: '0 0 100 100', 'aria-hidden': 'true', focusable: 'false' },
        h('polygon', { className: 'lg-sigil__outer', points: HEX }),
        h('polygon', { className: 'lg-sigil__inner', points: HEX_IN })),
      h('span', { className: 'lg-sigil__value' }, p.value));
    var label = p.label && pos !== 'none' ? h('span', { className: 'lg-sigil__label', key: 'l' }, p.label) : null;
    var kids = pos === 'left' || pos === 'top' ? [label, badge] : [badge, label];
    // The lock is in the description when there is one ("Locked. Unlocks at level 20."), so the name doesn't say it twice.
    var srText = (p.label ? p.label + ' ' : '') + p.value + (state === 'locked' && !w ? ', locked' : state === 'ready' ? ', can be raised' : '');
    var props = {
      type: interactive ? 'button' : undefined,
      className: cx('lg-sigil', 'lg-sigil--' + size, 'lg-sigil--label-' + pos, state !== 'default' && 'is-' + state, p.className),
      onClick: interactive && !w ? p.onClick : undefined, tabIndex: interactive ? p.tabIndex : undefined, 'data-index': p['data-index'],
      'aria-pressed': interactive && !w ? state === 'selected' : undefined,
      'aria-label': srText,
      title: p.title,
      style: p.style
    };
    if (w) props = chain(props, w.props);
    return h(interactive ? 'button' : 'div', props, kids, w ? w.desc : null);
  }

  /* ---------- Constellation ---------- */
  function Constellation(p) {
    var w = p.width || 1000, ht = p.height || 700;
    var items = p.items || [];
    // Roving tabindex: one Sigil is in the tab order; arrow keys, Home and End move between them.
    var foc = R.useState(null), sel = -1;
    items.forEach(function (it, i) { if (it.id === p.selectedId) sel = i; });
    var active = foc[0] != null ? foc[0] : Math.max(0, sel);
    return h('div', {
      className: cx('lg-constellation', p.className),
      style: { aspectRatio: w + ' / ' + ht },
      role: 'group', 'aria-label': p.label || 'Attributes',
      onFocus: function (e) { var i = e.target.getAttribute && e.target.getAttribute('data-index'); if (i != null) foc[1](+i); },
      onKeyDown: p.onSelect ? function (e) {
        var j = moveKey(e.key, active, items.length);
        if (j == null) return;
        e.preventDefault(); foc[1](j);
        var b = e.currentTarget.querySelector('[data-index="' + j + '"]'); if (b) b.focus();
      } : undefined
    },
      h('svg', { className: 'lg-constellation__rings', viewBox: '0 0 ' + w + ' ' + ht, preserveAspectRatio: 'xMidYMid meet', 'aria-hidden': 'true', focusable: 'false' },
        (p.rings || []).map(function (r, i) {
          return h('circle', { key: 'r' + i, cx: r.cx, cy: r.cy, r: r.r, className: r.strong ? 'is-strong' : undefined });
        }),
        // Nodes are ticks across their ring, like an astrolabe's graduations: linework, not a diamond,
        // since a diamond means a milestone, a ready mark or the current location (Foundations · Shape).
        (p.nodes || []).map(function (n, i) {
          var best = null;
          (p.rings || []).forEach(function (r) { var d = Math.abs(Math.hypot(n.x - r.cx, n.y - r.cy) - r.r); if (!best || d < best.d) best = { d: d, r: r }; });
          var ux = 0, uy = 1;
          if (best) { var dx = n.x - best.r.cx, dy = n.y - best.r.cy, len = Math.hypot(dx, dy) || 1; ux = dx / len; uy = dy / len; }
          return h('line', { key: 'n' + i, x1: n.x - ux * 6, y1: n.y - uy * 6, x2: n.x + ux * 6, y2: n.y + uy * 6, className: 'lg-constellation__tick' });
        })),
      items.map(function (it, idx) {
        var pos = it.labelPosition || 'right';
        var state = it.id === p.selectedId ? 'selected' : (it.state || 'default');
        return h('div', {
          key: it.id,
          className: cx('lg-constellation__node', 'lg-constellation__node--' + pos, 'lg-constellation__node--' + (it.size || 'md')),
          style: { left: (it.x / w * 100) + '%', top: (it.y / ht * 100) + '%' }
        }, h(Sigil, {
          label: it.label, value: it.value, labelPosition: pos, size: it.size || 'md', state: state, reason: it.reason,
          onClick: p.onSelect ? function () { p.onSelect(it.id); } : undefined,
          tabIndex: idx === active ? 0 : -1, 'data-index': idx
        }));
      }));
  }

  /* ---------- Meter ---------- */
  function Meter(p) {
    var max = p.max || 0;
    var v = Math.max(0, Math.min(p.value || 0, max || 0));
    var pct = max ? (v / max) * 100 : 0;
    var tone = p.tone || 'hp';
    var showValue = p.showValue !== false;
    // The value reserves the width of max / max, so a live value never moves the label or the bar.
    var reserve = fmt(max).length * 2 + 3 + (p.unit ? String(p.unit).length + 1 : 0);
    var head = (p.label || showValue) ? h('div', { className: 'lg-meter__head' },
      p.label ? h('span', { className: 'lg-meter__label' }, p.label) : null,
      showValue ? h('span', { className: 'lg-meter__value', style: { minWidth: reserve + 'ch' } }, fmt(p.value),
        h('span', { className: 'lg-meter__max' }, NBSP + '/' + NBSP + fmt(max)),
        p.unit ? h('span', { className: 'lg-unit' }, NBSP + p.unit) : null) : null) : null;
    // Motion: the fill scales to the value over duration-slow; `live` (combat playback) follows ticks over duration-fast.
    return h('div', { className: cx('lg-meter', 'lg-meter--' + tone, 'lg-meter--' + (p.size || 'thin'), p.live && 'is-live', p.className) },
      head,
      h('div', {
        className: 'lg-meter__track', role: 'meter',
        'aria-valuemin': 0, 'aria-valuemax': max, 'aria-valuenow': p.value,
        'aria-valuetext': fmt(p.value) + ' of ' + fmt(max) + (p.unit ? ' ' + p.unit : ''),
        'aria-label': p.ariaLabel || p.label || tone
      }, h('div', { className: 'lg-meter__fill', style: { '--lg-meter-p': String(pct / 100) } })));
  }

  /* ---------- Track (milestones) ---------- */
  function Track(p) {
    var n = Math.max(2, p.steps || 5);
    var cur = Math.max(0, Math.min(p.current || 0, n - 1));
    var pct = (cur / (n - 1)) * 100;
    var nodes = [];
    for (var i = 0; i < n; i++) {
      var st = i < cur ? 'is-done' : i === cur ? 'is-current' : 'is-todo';
      nodes.push(h('span', {
        key: i, className: cx('lg-track__node', st),
        style: { left: (i / (n - 1) * 100) + '%' },
        title: p.labels && p.labels[i] ? p.labels[i] : undefined
      }));
    }
    return h('div', { className: cx('lg-track', 'lg-track--' + (p.tone || 'gilt'), p.className) },
      p.startLabel ? h('span', { className: 'lg-track__end' }, p.startLabel) : null,
      h('div', {
        className: 'lg-track__rail', role: 'progressbar',
        'aria-valuemin': 1, 'aria-valuemax': n, 'aria-valuenow': cur + 1,
        'aria-valuetext': (p.labels && p.labels[cur]) || ('Step ' + (cur + 1) + ' of ' + n),
        'aria-label': p.label || 'Progress'
      },
        h('span', { className: 'lg-track__fill', style: { width: pct + '%' } }),
        nodes),
      p.endLabel ? h('span', { className: 'lg-track__end' }, p.endLabel) : null);
  }

  /* ---------- LevelPlate ---------- */
  function LevelPlate(p) {
    return h('div', { className: cx('lg-levelplate', p.className) },
      h('div', { className: 'lg-levelplate__row' },
        h('div', { className: 'lg-levelplate__level' },
          h('span', { className: 'lg-levelplate__kicker' }, p.kicker || 'Level'),
          h('span', { className: 'lg-levelplate__num' }, p.level)),
        p.aside && p.aside.length ? h('ul', { className: 'lg-levelplate__aside' },
          p.aside.map(function (a) {
            return h('li', { key: a.label },
              a.icon ? h(Icon, { name: a.icon, size: 16 }) : null,
              h('span', { className: 'lg-levelplate__aside-label' }, a.label),
              h('b', null, numNode(a.unit ? unit(a.value, a.unit) : a.value)));
          })) : null),
      p.xpMax != null ? h('div', { className: 'lg-levelplate__xp' },
        h(Meter, { tone: 'xp', value: p.xp || 0, max: p.xpMax, showValue: false, ariaLabel: p.xpUnit || 'Experience' }),
        h('span', { className: 'lg-levelplate__xptext' }, fraction(p.xp || 0, p.xpMax), h('span', { className: 'lg-unit' }, NBSP + (p.xpUnit || 'EXP')))) : null);
  }

  /* ---------- StatTile ---------- */
  /* ---------- Delta ---------- */
  // The glyph and sign say which way the number moved; the polarity (from the game, never from the sign) says
  // whether that helps the player. Screen readers hear the sign, the value and the judgement in words.
  function Delta(p) {
    var dir = p.direction || 'none';
    var pol = p.polarity || 'neutral';
    // Up and down take a triangle; unchanged is ±0, with no shape (Foundations · Shape: a hollow diamond is a milestone to come).
    var glyph = dir === 'up' ? '\u25B2' : dir === 'down' ? '\u25BC' : null;
    var sign = dir === 'up' ? '+' : dir === 'down' ? '\u2212' : '\u00B1';
    var word = pol === 'better' ? 'better' : pol === 'worse' ? 'worse' : dir === 'none' ? 'unchanged' : null;
    return h('span', { className: cx('lg-delta', 'lg-delta--' + pol, p.className) },
      glyph ? h('span', { className: 'lg-delta__glyph', 'aria-hidden': 'true' }, glyph) : null,
      h('span', { className: 'lg-delta__value' }, glyph ? sign : h('span', { 'aria-hidden': 'true' }, sign), p.value),
      word ? h('span', { className: 'lg-sr' }, ', ' + word) : null);
  }

  function StatTile(p) {
    var d = p.delta;
    var hasDelta = d != null;
    var value = h('span', { className: 'lg-stattile__value' }, typeof p.value === 'number' || missing(p.value) ? fmt(p.value) : p.value,
      p.suffix && !missing(p.value) ? h('small', { className: 'lg-unit' }, p.suffix) : null);
    // With a delta, the value and its change stack at the right so both fit a two-up tile.
    return h('div', { className: cx('lg-stattile', hasDelta && 'has-delta', p.className) },
      h('span', { className: 'lg-stattile__label' }, p.label),
      hasDelta ? h('span', { className: 'lg-stattile__figure' }, value, h(Delta, {
        className: 'lg-stattile__delta',
        direction: d > 0 ? 'up' : d < 0 ? 'down' : 'none',
        polarity: d === 0 ? 'neutral' : (p.deltaPolarity || 'neutral'),
        value: p.deltaText != null ? p.deltaText : fmt(Math.abs(d)) + (p.suffix || '')
      })) : value);
  }

  /* ---------- Motion (Foundations · Motion) ---------- */
  // Durations and easings mirror the motion tokens; motionMs reads the token from tokens.css when it is loaded.
  var MOTION = {
    duration: { instant: 0, fast: 140, base: 220, slow: 400, reveal: 600 },
    easing: { standard: 'cubic-bezier(0.4, 0, 0.2, 1)', enter: 'cubic-bezier(0, 0, 0.2, 1)', exit: 'cubic-bezier(0.4, 0, 1, 1)' }
  };
  // The only loops allowed: an indeterminate progress indicator (a progressbar with no value) and live combat playback.
  var LOOPS_ALLOWED = '[role="progressbar"]:not([aria-valuenow]), [data-motion="playback"]';
  var ANIMATED_OK = /^(transform|opacity|translate|scale|rotate|visibility|none)$/;
  function motionMs(name) {
    try {
      var v = getComputedStyle(document.documentElement).getPropertyValue('--duration-' + name).trim();
      var m = /^([\d.]+)(ms|s)$/.exec(v);
      if (m) return +m[1] * (m[2] === 's' ? 1000 : 1);
    } catch (e) { /* no tokens.css: use the mirror */ }
    return MOTION.duration[name];
  }
  // Reduced motion: the operating system's setting, or data-motion="reduced" on the element's ancestors or the root.
  function reducedMotion(el) {
    if (typeof window === 'undefined') return true;
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) return true;
    var t = el && el.nodeType === 1 ? el : document.documentElement;
    return !!(t && t.closest && t.closest('[data-motion="reduced"]'));
  }
  // cubic-bezier easing for counts that run in script (ease-standard), solved by bisection.
  function bezier(x1, y1, x2, y2) {
    function at(a, b, t) { return 3 * a * t * (1 - t) * (1 - t) + 3 * b * t * t * (1 - t) + t * t * t; }
    return function (x) {
      if (x <= 0) return 0; if (x >= 1) return 1;
      var lo = 0, hi = 1, t = x;
      for (var i = 0; i < 24; i++) { t = (lo + hi) / 2; if (at(x1, x2, t) < x) lo = t; else hi = t; }
      return at(y1, y2, t);
    };
  }
  var EASE_STANDARD = bezier(0.4, 0, 0.2, 1);
  function decimals(n) { var s = String(n), i = s.indexOf('.'); return i < 0 ? 0 : s.length - i - 1; }
  // useLive(value, { cause, mark, reduced, el }) — one live value, shown by the rules of Foundations · Motion:
  // a change the player caused (cause: 'player' — after combat, after a purchase) counts to the new value over
  // duration-slow; any other change shows at once and is marked (className 'lg-live is-changed') for duration-reveal,
  // then the mark fades. Never both. Under reduced motion nothing counts; the mark still shows, and goes at once.
  function useLive(value, o) {
    o = o || {};
    var tick = R.useState(0), markSt = R.useState(false);
    var r = R.useRef(null);
    if (!r.current) r.current = { prev: value, shown: value, from: value, counting: false, raf: 0, timer: 0 };
    var st = r.current, opt = R.useRef(o); opt.current = o;
    if (value !== st.prev) {
      var elx = o.el && (o.el.current !== undefined ? o.el.current : o.el);
      var red = o.reduced != null ? o.reduced : reducedMotion(elx);
      var numeric = typeof value === 'number' && typeof st.shown === 'number' && isFinite(value) && isFinite(st.shown);
      st.counting = o.cause === 'player' && !red && numeric;
      st.from = st.shown; st.cause = o.cause; st.prev = value;
      if (!st.counting) st.shown = value;
      st.pending = true;
    }
    R.useEffect(function () {
      if (!st.pending) return;
      st.pending = false;
      cancelAnimationFrame(st.raf);
      if (st.counting) {
        var a = st.from, b = value, d = motionMs('slow'), dec = Math.max(decimals(a), decimals(b)), t0 = null;
        var step = function (t) {
          if (t0 === null) t0 = t;
          var k = Math.min(1, (t - t0) / d);
          st.shown = k >= 1 ? b : +(a + (b - a) * EASE_STANDARD(k)).toFixed(dec);
          if (k >= 1) st.counting = false;
          tick[1](function (n) { return n + 1; });
          if (k < 1) st.raf = requestAnimationFrame(step);
        };
        st.raf = requestAnimationFrame(step);
      } else if (st.cause !== 'player' && opt.current.mark !== false) {
        markSt[1](true); clearTimeout(st.timer);
        st.timer = setTimeout(function () { markSt[1](false); }, motionMs('reveal'));
      }
    }, [value]);
    R.useEffect(function () { return function () { cancelAnimationFrame(st.raf); clearTimeout(st.timer); }; }, []);
    return { value: st.counting ? st.shown : value, changed: markSt[0], className: cx('lg-live', markSt[0] && 'is-changed') };
  }
  // useLiveList(items, { key, keep }) — a refreshed list that never moves what the player is about to click.
  // While the pointer is over the list or focus is inside it, rows keep their order: values update in place, a row whose
  // item has gone stays where it was ({ gone: true }), and new items wait, counted in `pending`, until the player leaves
  // the list or calls release(). Keys in `keep` (the selection) stay in place, gone, even after release.
  // Put `ref` on the list's element. Rows are keyed by item, never by index, so scroll position is kept.
  function useLiveList(items, o) {
    o = o || {};
    var keyOf = typeof o.key === 'function' ? o.key : function (x) { return x[o.key || 'id']; };
    var ref = R.useRef(null), heldSt = R.useState(false), tick = R.useState(0);
    var mem = R.useRef({ order: null, last: {} }), m = mem.current;
    R.useEffect(function () {
      var el = ref.current; if (!el) return;
      function check() { heldSt[1](!!(el.matches(':hover') || el.contains(document.activeElement))); }
      function enter() { heldSt[1](true); }
      function leave() { setTimeout(check, 0); }
      el.addEventListener('pointerenter', enter); el.addEventListener('pointerleave', leave);
      el.addEventListener('focusin', enter); el.addEventListener('focusout', leave);
      return function () {
        el.removeEventListener('pointerenter', enter); el.removeEventListener('pointerleave', leave);
        el.removeEventListener('focusin', enter); el.removeEventListener('focusout', leave);
      };
    }, []);
    var held = heldSt[0], byKey = {}, rows, pending = 0;
    (items || []).forEach(function (x) { byKey[keyOf(x)] = x; });
    if (held && m.order) {
      rows = m.order.map(function (k) { return byKey[k] ? { key: k, item: byKey[k], gone: false } : { key: k, item: m.last[k], gone: true }; });
      (items || []).forEach(function (x) { if (m.order.indexOf(keyOf(x)) < 0) pending++; });
    } else {
      rows = (items || []).map(function (x) { return { key: keyOf(x), item: x, gone: false }; });
      [].concat(o.keep == null ? [] : o.keep).forEach(function (k) {
        if (k == null || byKey[k] || !m.last[k] || !m.order) return;
        var i = m.order.indexOf(k);
        rows.splice(i < 0 ? rows.length : Math.min(i, rows.length), 0, { key: k, item: m.last[k], gone: true });
      });
    }
    var last = {};
    rows.forEach(function (row) { last[row.key] = row.item; });
    m.order = rows.map(function (row) { return row.key; }); m.last = last;
    return {
      ref: ref, rows: rows, held: held, pending: pending,
      release: function () { m.order = null; tick[1](function (n) { return n + 1; }); }
    };
  }
  // LL.motion.audit(root): what moves against the rules — a transition or animation on anything but transform and
  // opacity (visibility may step), a duration past duration-reveal, and loops other than the two allowed.
  function keyframeProps() {
    var out = {};
    Array.prototype.forEach.call(document.styleSheets, function (sh) {
      var rules; try { rules = sh.cssRules; } catch (e) { return; }
      (function walk(list) {
        Array.prototype.forEach.call(list || [], function (r) {
          if (r.type === 7) {
            var props = [];
            Array.prototype.forEach.call(r.cssRules, function (f) { for (var i = 0; i < f.style.length; i++) props.push(f.style[i]); });
            out[r.name] = props;
          } else if (r.cssRules) walk(r.cssRules);
        });
      })(rules);
    });
    return out;
  }
  function secs(v) { return String(v).split(',').map(function (x) { x = x.trim(); return /ms$/.test(x) ? parseFloat(x) : parseFloat(x) * 1000; }); }
  function auditMotion(root) {
    root = root || document.body;
    var kf = keyframeProps(), problems = [], counts = { transitions: 0, animations: 0, loops: 0 };
    var max = motionMs('reveal');
    function name(el) { return el.className && typeof el.className === 'string' ? '.' + el.className.trim().split(/\s+/)[0] : el.tagName.toLowerCase(); }
    Array.prototype.forEach.call(root.querySelectorAll('*'), function (el) {
      [null, '::before', '::after'].forEach(function (pe) {
        var s = getComputedStyle(el, pe);
        if (pe && (s.content === 'none' || s.content === 'normal')) return;
        // Durations of 1ms or less are transitions reduced motion has collapsed: they do not move.
        var durs = secs(s.transitionDuration);
        if (durs.some(function (d) { return d > 1; })) {
          counts.transitions++;
          s.transitionProperty.split(',').forEach(function (pr, i) {
            pr = pr.trim(); var d = durs[i % durs.length];
            if (d > 1 && !ANIMATED_OK.test(pr)) problems.push(name(el) + (pe || '') + ' transitions ' + pr + ': only transform and opacity animate');
            if (d > max) problems.push(name(el) + (pe || '') + ' transitions over ' + d + 'ms, past duration-reveal');
          });
        }
        if (s.animationName && s.animationName !== 'none' && secs(s.animationDuration).some(function (d) { return d > 1; })) {
          counts.animations++;
          s.animationName.split(',').forEach(function (an) {
            (kf[an.trim()] || []).forEach(function (pr) { if (!ANIMATED_OK.test(pr) && pr.indexOf('animation') !== 0) problems.push(name(el) + (pe || '') + ' animates ' + pr + ' (@keyframes ' + an.trim() + ')'); });
          });
          var loop = /infinite/.test(s.animationIterationCount);
          if (loop) { counts.loops++; if (!el.closest(LOOPS_ALLOWED)) problems.push(name(el) + (pe || '') + ' loops: only an indeterminate progress indicator and live combat playback may'); }
          else if (secs(s.animationDuration).some(function (d) { return d > max; })) problems.push(name(el) + (pe || '') + ' runs past duration-reveal');
        }
      });
    });
    problems = problems.filter(function (x, i, a) { return a.indexOf(x) === i; });
    return { counts: counts, problems: problems, ok: !problems.length };
  }

  /* ---------- Ornament budget (Foundations · Ornament) ---------- */
  // Per screen: one ornamented framed surface (a Folio or a Banner), two ornament rules, grain only where there is art,
  // no halo and no looping animation. LL.ornament.audit(root) counts what it can see and lists what is over.
  var ORNAMENT_BUDGET = { framed: 1, rules: 2, halos: 0, loops: 0 };
  var ZONES = [
    ['table', 'a table'], ['[role="menu"], [role="menubar"]', 'a menu'], ['[role="status"], .lg-toast', 'a toast'],
    ['input, select, textarea', 'an input'],
    ['ul, ol, [role="list"], [role="listbox"], [role="log"], .lg-list, .lg-entrylist', 'a list'],
    ['[role="dialog"]:not([data-commitment="major"]), [role="alertdialog"]:not([data-commitment="major"])', 'a dialog that is not a major commitment']
  ];
  function forbiddenZone(el) {
    if (!el || !el.closest) return null;
    for (var i = 0; i < ZONES.length; i++) if (el.parentElement && el.parentElement.closest(ZONES[i][0])) return ZONES[i][1];
    var panel = el.closest('.lg-panel');
    if (panel && (panel.matches('[data-density="compact"], .lg-panel--flush') || panel.querySelector('.lg-list, table, .lg-ledger'))) return 'a dense Panel';
    return null;
  }
  function checkFramed(el) {
    var shell = el && el.closest && el.closest('.lg-shell');
    if (!shell || !window.console) return;
    var framed = shell.querySelectorAll('.lg-folio, .lg-banner');
    if (framed.length > ORNAMENT_BUDGET.framed && framed[framed.length - 1] === el) console.warn('LL: a Folio and a Banner on one screen. One ornamented framed surface per screen (Foundations · Ornament).');
  }
  function shadowParts(v) {
    if (!v || v === 'none') return [];
    var out = [], depth = 0, cur = '';
    for (var i = 0; i < v.length; i++) { var ch = v[i]; if (ch === '(') depth++; if (ch === ')') depth--; if (ch === ',' && !depth) { out.push(cur.trim()); cur = ''; } else cur += ch; }
    if (cur.trim()) out.push(cur.trim());
    return out;
  }
  // A halo is a blurred shadow in a colour that is not near-black: the glow D-012 removed. Dark shadows, the focus ring and edges have no blur or no light.
  function isHalo(part) {
    var m = part.match(/rgba?\(([^)]+)\)/); if (!m) return false;
    var c = m[1].split(',').map(parseFloat), a = c.length > 3 ? c[3] : 1;
    var lens = part.replace(m[0], '').replace('inset', '').trim().split(/\s+/).map(parseFloat);
    var blur = lens.length > 2 ? lens[2] : 0;
    var lum = (0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]) / 255;
    return blur > 0 && a > 0.05 && lum > 0.16;
  }
  function auditOrnament(root) {
    root = root || document.body;
    var q = function (s) { return Array.prototype.slice.call(root.querySelectorAll(s)); };
    var counts = { framed: 0, rules: 0, corners: 0, grain: 0, textures: 0, vignettes: 0, halos: 0, loops: 0 };
    var problems = [];
    var framed = q('.lg-folio, .lg-banner, [data-ornament="frame"]');
    counts.framed = framed.length;
    if (framed.length > ORNAMENT_BUDGET.framed) problems.push(framed.length + ' ornamented framed surfaces; the budget is 1 (a Folio or a Banner, not both)');
    var rules = q('.lg-rule--ornament, [data-ornament="rule"]');
    counts.rules = rules.length;
    if (rules.length > ORNAMENT_BUDGET.rules) problems.push(rules.length + ' ornament rules; the budget is 2');
    var cornerHosts = [];
    q('.lg-folio__corner, [data-ornament="corner"]').forEach(function (c) { var host = c.closest('.lg-folio, .lg-banner, [data-ornament="frame"]') || c.parentElement; if (cornerHosts.indexOf(host) < 0) cornerHosts.push(host); });
    counts.corners = cornerHosts.length;
    cornerHosts.forEach(function (hst) { if (!hst.matches('.lg-folio, .lg-banner')) problems.push('Corner ornaments off the Folio and the Banner'); });
    q('.lg-rule--ornament, [data-ornament]').forEach(function (d) { var z = forbiddenZone(d); if (z) problems.push('Ornament in ' + z); });
    q('*').forEach(function (el) {
      var cs = getComputedStyle(el);
      [cs, getComputedStyle(el, '::before'), getComputedStyle(el, '::after')].forEach(function (s, k) {
        if (k && (s.content === 'none' || s.content === 'normal')) return;
        if (/feTurbulence/.test(s.backgroundImage)) {
          counts.grain++;
          var host = el.closest('.lg-stage, .lg-banner, [data-ornament="frame"]') || el;
          if (!host.querySelector('.lg-stage__art, .lg-banner__art')) problems.push('Film grain on a surface without art');
        }
        if (shadowParts(s.boxShadow).concat(shadowParts(s.textShadow)).some(isHalo)) counts.halos++;
        if (s.animationName && s.animationName !== 'none' && /infinite/.test(s.animationIterationCount) && !el.closest(LOOPS_ALLOWED)) counts.loops++;
      });
    });
    q('[data-ornament="texture"]').forEach(function () { counts.textures++; problems.push('A material texture (parchment, leather, metal)'); });
    counts.vignettes = q('.lg-stage__veil, .lg-banner__veil').length;
    if (counts.halos) problems.push(counts.halos + ' halo' + (counts.halos > 1 ? 's' : '') + ': glow is flat, never a blurred light');
    if (counts.loops) problems.push(counts.loops + ' looping animation' + (counts.loops > 1 ? 's' : ''));
    return { counts: counts, budget: ORNAMENT_BUDGET, problems: problems.filter(function (x, i, a) { return a.indexOf(x) === i; }), ok: !problems.length };
  }

  /* ---------- SectionRule ---------- */
  // The ornament appears at most once per surface (Foundations · Lines). A second one on the same surface warns once.
  var SURFACE_SEL = '.lg-folio, .lg-panel, .lg-banner, .lg-journey, .lg-page, [role="dialog"], .lg-level-1, .lg-level-2, .lg-level-3, .lg-level-1--float, .lg-level-2--float';
  function SectionRule(p) {
    var v = p.variant || 'band';
    var ref = R.useRef(null);
    R.useEffect(function () {
      var el = ref.current;
      if (v !== 'ornament' || !el || !el.closest || !window.console) return;
      var surf = el.closest(SURFACE_SEL) || el.ownerDocument.body;
      var mine = Array.prototype.filter.call(surf.querySelectorAll('.lg-rule--ornament'), function (o) { return (o.closest(SURFACE_SEL) || o.ownerDocument.body) === surf; });
      if (mine.length > 1 && mine.indexOf(el) > 0) console.warn('LL.SectionRule: one ornament per surface. Use a hairline or a band for the other groups (Foundations · Lines).');
      var zone = forbiddenZone(el);
      if (zone) console.warn('LL.SectionRule: no ornament in ' + zone + ' (Foundations · Ornament · Forbidden zones).');
      var shell = el.closest('.lg-shell');
      if (shell) {
        var all = shell.querySelectorAll('.lg-rule--ornament');
        if (all.length > ORNAMENT_BUDGET.rules && all[all.length - 1] === el) console.warn('LL.SectionRule: ' + all.length + ' ornament rules on one screen; the budget is ' + ORNAMENT_BUDGET.rules + ' (Foundations · Ornament).');
      }
    }, [v]);
    if (v === 'ornament') {
      return h('div', { ref: ref, className: cx('lg-rule', 'lg-rule--ornament', p.className), role: 'separator', 'aria-label': typeof p.label === 'string' ? p.label : undefined },
        h('span', { className: 'lg-rule__lattice', 'aria-hidden': 'true' }),
        p.label ? h('span', { className: 'lg-rule__label' }, p.label) : null,
        p.label ? h('span', { className: 'lg-rule__lattice', 'aria-hidden': 'true' }) : null);
    }
    return h('div', { className: cx('lg-rule', 'lg-rule--' + v, p.align === 'end' && 'lg-rule--end', p.className), role: 'separator', 'aria-label': p.label },
      p.label ? h('span', { className: 'lg-rule__label' }, p.label) : null,
      p.aside ? h('span', { className: 'lg-rule__aside' }, p.aside) : null);
  }

  /* ---------- Panel ---------- */
  function Panel(p) {
    return h('section', { className: cx('lg-panel', p.flush && 'lg-panel--flush', p.className), 'data-density': p.density, 'aria-label': typeof p.title === 'string' ? p.title : undefined },
      p.title ? h('header', { className: cx('lg-panel__head', p.titleAlign === 'end' && 'is-end') },
        h('span', { className: 'lg-panel__title' }, p.title), p.aside || null) : null,
      h('div', { className: 'lg-panel__body' }, p.children));
  }

  /* ---------- Emblem (engraved sacred geometry) ---------- */
  function gcd(a, b) { return b ? gcd(b, a % b) : a; }
  function Emblem(p) {
    var n = p.points || 6;
    var s = p.size || 160;
    var k = p.skip || (n >= 7 ? 3 : 2);
    var outer = ngon(n, 44, 50, 50, -90);
    var inner = ngon(n, 24, 50, 50, -90 + 180 / n);
    var shapes = [];
    var loops = gcd(n, k);
    for (var l = 0; l < loops; l++) {
      var pts = [];
      var idx = l;
      for (var j = 0; j < n / loops; j++) { pts.push(outer[idx]); idx = (idx + k) % n; }
      shapes.push(h('polygon', { key: 's' + l, points: ptsStr(pts), className: 'lg-emblem__star' }));
    }
    var spokes = outer.map(function (pt, i) {
      return h('line', { key: 'k' + i, x1: 50, y1: 50, x2: pt[0].toFixed(2), y2: pt[1].toFixed(2), className: 'lg-emblem__spoke' });
    });
    var dots = outer.map(function (pt, i) {
      return h('circle', { key: 'd' + i, cx: pt[0].toFixed(2), cy: pt[1].toFixed(2), r: 1.6, className: 'lg-emblem__dot' });
    });
    return h('svg', { className: cx('lg-emblem', p.className), width: s, height: s, viewBox: '0 0 100 100', 'aria-hidden': 'true', focusable: 'false' },
      h('circle', { cx: 50, cy: 50, r: 48, className: 'lg-emblem__ring' }),
      h('circle', { cx: 50, cy: 50, r: 44, className: 'lg-emblem__ring lg-emblem__ring--thin' }),
      spokes,
      h('polygon', { points: ptsStr(outer), className: 'lg-emblem__frame' }),
      shapes,
      h('polygon', { points: ptsStr(inner), className: 'lg-emblem__inner' }),
      h('circle', { cx: 50, cy: 50, r: 9, className: 'lg-emblem__core' }),
      h('circle', { cx: 50, cy: 50, r: 3, className: 'lg-emblem__heart' }),
      dots);
  }

  /* ---------- Folio (detail panel) ---------- */
  function Folio(p) {
    var fref = R.useRef(null);
    R.useEffect(function () { checkFramed(fref.current); }, []);
    var hasTop = p.emblem || p.eyebrow || p.title || p.rarity;
    var corner = p.cornerSrc ? { WebkitMaskImage: 'url(' + p.cornerSrc + ')', maskImage: 'url(' + p.cornerSrc + ')' } : null;
    var rar = p.rarity;
    return h('aside', { ref: fref, className: cx('lg-folio', p.align === 'start' && 'lg-folio--start', rar && 'lg-folio--item lg-folio--' + rar.toLowerCase(), p.className), 'aria-label': p.ariaLabel || (typeof p.title === 'string' ? p.title : 'Details') },
      h('span', { className: 'lg-folio__frame', 'aria-hidden': 'true' }),
      corner ? ['tl', 'tr', 'bl', 'br'].map(function (c) {
        return h('span', { key: c, className: 'lg-folio__corner lg-folio__corner--' + c, style: corner, 'aria-hidden': 'true' });
      }) : null,
      h('div', { className: 'lg-folio__scroll' },
        hasTop ? h('div', { className: 'lg-folio__top' },
          p.emblem ? h('div', { className: 'lg-folio__emblem' }, p.emblem) : null,
          p.eyebrow ? h('div', { className: 'lg-folio__eyebrow' }, p.eyebrow) : null,
          p.title ? h(Heading, { level: 'folio', sub: p.titleSub }, p.title) : null,
          rar ? h(Tag, { tone: rar.toLowerCase() }, rar) : null) : null,
        p.lore ? h('p', { className: 'lg-folio__lore' }, p.lore) : null,
        p.lore || p.effects ? h(SectionRule, { variant: 'ornament' }) : null,
        p.effects ? h('ul', { className: 'lg-folio__effects' }, p.effects.map(function (e, i) {
          if (typeof e === 'string') return h('li', { key: i }, e);
          return h('li', { key: i }, h('b', { className: 'lg-folio__fx' }, e.value), ' ', e.text);
        })) : null,
        p.children,
        p.actions ? h('div', { className: 'lg-folio__actions' }, p.actions) : null),
      p.footer ? h('div', { className: 'lg-folio__footer' }, p.footer) : null);
  }

  /* ---------- EntryList ---------- */
  // Selection follows focus. A locked entry stays in reach of the keyboard (Standards · States): Up and Down land on it
  // and its unlock condition opens beside it, but it never becomes selected; Enter or a click shows the condition.
  function EntryList(p) {
    var items = p.items || [];
    var base = useId('lge');
    var foc = R.useState(null);
    var ai = items.findIndex(function (x) { return x.id === p.activeId; });
    var tabAt = foc[0] != null && foc[0] < items.length ? foc[0] : Math.max(0, ai);
    function onKeyDown(e) {
      var row = e.target.closest && e.target.closest('[role=option]');
      var i = row ? +row.getAttribute('data-index') : Math.max(0, ai), n = items.length, j = null;
      if (e.key === 'ArrowDown') j = (i + 1) % n;
      else if (e.key === 'ArrowUp') j = (i - 1 + n) % n;
      else if (e.key === 'Home') j = 0;
      else if (e.key === 'End') j = n - 1;
      if (j != null && n) {
        e.preventDefault(); foc[1](j);
        var el = e.currentTarget.querySelector('[data-index="' + j + '"]'); if (el) el.focus();
        if (p.onSelect && !items[j].locked) p.onSelect(items[j].id);
        return;
      }
      if ((e.key === 'Enter' || e.key === ' ') && row) {
        e.preventDefault();
        if (items[i].locked) row.click(); else if (p.onSelect) p.onSelect(items[i].id);
      }
    }
    return h('ul', {
      className: cx('lg-entrylist', p.fade !== false && 'lg-entrylist--fade', p.className),
      role: 'listbox', 'aria-label': p.label || 'Entries', 'data-density': p.density,
      onKeyDown: onKeyDown
    }, items.map(function (it, i) {
      var active = it.id === p.activeId && !it.locked;
      var w = it.locked ? why(base + '-' + i, blockedReason('locked', it, 'EntryList entry "' + it.name + '"').reason, { word: 'Locked', place: 'end' }) : null;
      var props = {
        key: it.id, role: 'option', 'aria-selected': active, 'data-index': i,
        tabIndex: i === tabAt ? 0 : -1,
        className: cx('lg-entrylist__item', active && 'is-active', it.locked && 'is-locked'),
        onFocus: function () { foc[1](i); },
        onClick: it.locked || !p.onSelect ? undefined : function () { p.onSelect(it.id); }
      };
      if (w) props = chain(props, w.props);
      // One Tag per row: Locked comes before New (Standards · State combinations).
      return h('li', props,
        h('span', { className: 'lg-entrylist__name' }, it.name),
        it.locked ? h(Tag, { state: 'locked', 'aria-hidden': 'true' }) : it.tag ? h(Tag, { tone: it.tagTone || 'new' }, it.tag) : null,
        // The attention diamond at the row's end; a locked entry isn't waiting for the player, so it takes none.
        it.ready && !it.locked ? attention('lg-entrylist__attention') : null,
        it.ready && !it.locked ? h('span', { className: 'lg-sr' }, ', ' + readyWords(it.ready)) : null,
        w ? w.desc : null);
    }));
  }

  /* ---------- ItemSlot ---------- */
  var RARITY_CODES = { Common: 'C', Uncommon: 'UC', Rare: 'R', Epic: 'E', Unique: 'U', Legendary: 'L', Legacy: 'LG' };
  // States (Standards · States): a blocked slot (locked, unavailable, restricted, insufficient, cooldown) gives its
  // reason; `not-owned` fades the art; `undiscovered` withholds the name and art; any other state with a word
  // (equipped, listed, borrowed…) leads the meta line. Marks keep fixed corners (Standards · State combinations): the
  // rarity code top start; the attention diamond top end (`ready`, or Claimable); the ownership mark bottom start — the
  // in-use square for Equipped and Attuned, else the favourite ribbon (or its word until it is drawn); the quantity
  // bottom end. A blocked or undiscovered slot takes no attention mark.
  function ItemSlot(p) {
    var id = useId('lgi');
    var state = p.state && p.state !== 'available' && p.state !== 'default' ? p.state : null;
    var s = state ? STATES[state] : null;
    if (state && !s) stateWarn('slot:' + state, '"' + state + '" is not a state');
    var blocked = !!(s && s.blocks), unowned = state === 'not-owned', undiscovered = state === 'undiscovered';
    var interactive = typeof p.onClick === 'function';
    var r = undiscovered ? null : p.rarity;
    var label = undiscovered ? STATES.undiscovered.word : (p.name || p.slotLabel);
    var captioned = !!label && p.caption !== false;
    var br = blocked ? blockedReason(state, p, 'ItemSlot "' + (label || '') + '"') : null;
    // With a caption the reason is printed under the name and is the description; without one it is the reason tip.
    var w = blocked && interactive ? why(id + '-why', br.reason, Object.assign({}, br, { printed: captioned })) : null;
    var inUse = state === 'equipped' || state === 'attuned';
    var ribbon = p.favourite && !inUse && ICONS.favourite;
    var stateWord = s && !blocked && !undiscovered ? s.word : null;
    var attn = !blocked && !undiscovered && (!!p.ready || state === 'claimable');
    var readyWord = !blocked && !undiscovered ? readyWords(p.ready) : null;
    var meta = [stateWord, p.favourite && !ribbon ? STATES.favourite.word : null, p.meta].filter(Boolean);
    var hasArt = !undiscovered && (p.image || p.icon);
    var props = {
      type: interactive ? 'button' : undefined,
      className: cx('lg-slot', r && 'lg-slot--' + r.toLowerCase(), p.selected && !blocked && 'is-selected', !hasArt && 'is-empty', p.size && 'lg-slot--' + p.size,
        blocked && 'is-blocked', state && 'is-' + state, p.className),
      onClick: interactive && !blocked ? p.onClick : undefined,
      'aria-pressed': interactive && !blocked ? !!p.selected : undefined,
      'aria-label': interactive ? [label, r, p.quantity > 1 ? 'quantity ' + fmt(p.quantity) : null, stateWord ? stateWord.toLowerCase() : null, readyWord ? readyWord.toLowerCase() : null, p.favourite ? 'favourite' : null].filter(Boolean).join(', ') : undefined
    };
    if (w) props = chain(props, w.props);
    var reasonLine = blocked && captioned ? h('span', { id: id + '-why', className: cx('lg-slot__reason', br.tone === 'warning' && 'is-warning') },
      // The word is set in capitals for the eye; screen readers get it in sentence case, so it isn't spelled out.
      br.word ? h('span', { className: 'lg-slot__word', 'aria-hidden': 'true' }, br.word) : null,
      br.word ? h('span', { className: 'lg-sr lg-why__desc' }, br.word + '. ') : null,
      br.reason) : null;
    return h(interactive ? 'button' : 'div', props,
      h('span', { className: 'lg-slot__frame' },
        undiscovered ? null : p.image ? h('img', { src: p.image, alt: '', className: 'lg-slot__img' }) :
          p.icon ? h(Icon, { name: p.icon, size: 24 }) : null,
        r ? h('span', { className: 'lg-slot__code', title: r, 'aria-hidden': 'true' }, RARITY_CODES[r] || r) : null,
        r && !interactive ? h('span', { className: 'lg-sr' }, r) : null,
        attn ? attention('lg-slot__attention') : null,
        inUse ? h('span', { className: 'lg-slot__mark lg-slot__mark--inuse', 'aria-hidden': 'true' }) : null,
        ribbon ? h('span', { className: 'lg-slot__mark', 'aria-hidden': 'true' }, h(Icon, { name: 'favourite', size: 12 })) : null,
        ribbon && !interactive ? h('span', { className: 'lg-sr' }, STATES.favourite.word) : null,
        readyWord && !interactive ? h('span', { className: 'lg-sr' }, readyWord) : null,
        p.quantity > 1 ? h('span', { className: 'lg-slot__qty' }, '\u00D7' + fmt(p.quantity)) : null),
      captioned ? h('span', { className: 'lg-slot__caption' },
        h('span', { className: 'lg-slot__name' }, label),
        reasonLine,
        meta.length ? h('span', { className: 'lg-slot__meta' }, meta.join(' \u00B7 ')) : null) : null,
      blocked && !captioned && !interactive ? h('span', { className: 'lg-sr' }, (br.word ? br.word + '. ' : '') + br.reason) : null,
      w ? w.desc : null);
  }

  /* ---------- List and ListRow (inventory, rankings, members, orders) ---------- */
  // The rows share the List's columns through CSS subgrid: thumbnail, name, quantity, value, trailing action.
  // A column appears only if some row uses it, and every row then keeps its cell, so values line up.
  function List(p) {
    var kids = R.Children.toArray(p.children);
    var has = { thumb: false, qty: false, value: false, trail: false };
    kids.forEach(function (k) {
      var q = k && k.props || {};
      if (q.icon || q.image || q.thumb) has.thumb = true;
      if (q.quantity != null) has.qty = true;
      if (q.value !== undefined) has.value = true;
      if (q.trailing) has.trail = true;
    });
    var cols = (has.thumb ? 'auto ' : '') + 'minmax(0, 1fr)' + (has.qty ? ' auto' : '') + (has.value ? ' auto' : '') + (has.trail ? ' auto' : '');
    // Roving tabindex: the list is one tab stop — the selected row, else the first. Up, Down, Home and End move between
    // rows; Right moves into a row's trailing action and Left back to its name. Trailing actions leave the tab order.
    var foc = R.useState(null), sel = -1, ref = R.useRef(null);
    kids.forEach(function (k, i) { if (k && k.props && k.props.selected) sel = i; });
    var active = foc[0] != null ? foc[0] : Math.max(0, sel);
    R.useEffect(function () {
      var el = ref.current; if (!el) return;
      el.querySelectorAll('.lg-listrow__trail button, .lg-listrow__trail a, .lg-listrow__tags button').forEach(function (b) { b.setAttribute('tabindex', '-1'); });
    });
    function onKeyDown(e) {
      var row = e.target.closest && e.target.closest('.lg-listrow'); if (!row) return;
      var i = +row.getAttribute('data-index');
      if (e.key === 'ArrowRight') { var t = row.querySelector('.lg-listrow__trail button, .lg-listrow__trail a'); if (t) { e.preventDefault(); t.focus(); } return; }
      if (e.key === 'ArrowLeft') { var hb = row.querySelector('.lg-listrow__hit'); if (hb && e.target !== hb) { e.preventDefault(); hb.focus(); } return; }
      var j = moveKey(e.key, i, kids.length);
      if (j == null) return;
      var next = e.currentTarget.querySelector('.lg-listrow[data-index="' + j + '"] .lg-listrow__hit');
      if (!next) return;
      e.preventDefault(); foc[1](j); next.focus();
    }
    return h('ul', { ref: ref, className: cx('lg-list', p.rhythm && p.rhythm !== 'separators' && 'lg-list--' + p.rhythm, p.className), role: 'list', 'aria-label': p.label, 'data-density': p.density, style: { gridTemplateColumns: cols }, onKeyDown: onKeyDown,
      onFocus: function (e) { var row = e.target.closest && e.target.closest('.lg-listrow'); if (row) foc[1](+row.getAttribute('data-index')); } },
      kids.map(function (k, i) { return R.isValidElement(k) ? R.cloneElement(k, { _cols: has, _index: i, _tab: i === active }) : k; }));
  }
  function ListRow(p) {
    var c = p._cols || { thumb: !!(p.icon || p.image || p.thumb), qty: p.quantity != null, value: p.value !== undefined, trail: !!p.trailing };
    // `live`: a value that changes by itself (a refreshed price, a roster's status) takes the live-update mark.
    var lv = useLive(p.value, { mark: !!p.live });
    var r = p.rarity, rl = r ? String(r).toLowerCase() : null;
    var name = h('span', { className: 'lg-listrow__name' }, p.title);
    return h('li', { className: cx('lg-listrow', rl && 'lg-listrow--' + rl, p.selected && 'is-selected', p.muted && 'is-muted', p.className), 'data-density': p.density, 'data-index': p._index },
      c.thumb ? h('span', { className: 'lg-listrow__thumb', 'aria-hidden': 'true' },
        p.image ? h('img', { src: p.image, alt: '' }) : p.icon ? h(Icon, { name: p.icon, size: 20 }) : (p.thumb || null)) : null,
      h('span', { className: 'lg-listrow__main' },
        typeof p.onClick === 'function'
          ? h('button', { type: 'button', className: 'lg-listrow__hit', onClick: p.onClick, tabIndex: p._tab === false ? -1 : 0, 'aria-pressed': p.selected != null ? !!p.selected : undefined }, name)
          : name,
        r ? h('span', { className: 'lg-listrow__code', 'aria-hidden': 'true' }, RARITY_CODES[r] || r) : null,
        r ? h('span', { className: 'lg-sr' }, ', ' + r) : null,
        p.tags ? h('span', { className: 'lg-listrow__tags' }, p.tags) : null,
        p.meta ? h('span', { className: 'lg-listrow__meta' }, p.meta) : null),
      c.qty ? h('span', { className: 'lg-listrow__qty' }, p.quantity != null ? h(F, null, h('span', { className: 'lg-sr' }, 'Quantity '), TIMES + fmt(p.quantity)) : NONE) : null,
      c.value ? h('span', { className: cx('lg-listrow__value', p.live && lv.className) }, numNode(p.value === undefined ? null : p.value)) : null,
      c.trail ? h('span', { className: 'lg-listrow__trail' }, p.trailing || null) : null);
  }

  /* ---------- CurrencyPill ---------- */
  function CurrencyPill(p) {
    // An abbreviated pill is always a button: without onClick it toggles between 12.5k and 12,480 itself,
    // so the full figure is one click, tap or key away and never exists only in the hover title.
    var full = R.useState(false);
    var own = p.short && typeof p.onClick !== 'function';
    var interactive = typeof p.onClick === 'function' || own;
    var amt = p.short && !full[0] ? short(p.amount) : fmt(p.amount);
    // Live amounts reserve their width and never shrink while mounted, so the TopBar does not reflow as they tick.
    var widest = R.useRef(0);
    if (String(amt).length > widest.current) widest.current = String(amt).length;
    var reserve = Math.max(widest.current, p.reserve || 0);
    return h(interactive ? 'button' : 'span', {
      type: interactive ? 'button' : undefined,
      className: cx('lg-currency', p.className),
      onClick: own ? function () { full[1](!full[0]); } : interactive ? p.onClick : undefined,
      title: p.title || (fmt(p.amount) + ' ' + p.name)
    },
      p.iconSrc ? h('img', { src: p.iconSrc, alt: '', width: 18, height: 18, className: 'lg-currency__icon' }) : null,
      // An abbreviated amount is read in full: "12.5k" is announced as "12,480 Cinders".
      h('span', { className: 'lg-currency__amount', style: { minWidth: reserve + 'ch' }, 'aria-hidden': 'true' }, amt),
      h('span', { className: 'lg-currency__name', 'aria-hidden': 'true' }, p.name),
      h('span', { className: 'lg-sr' }, fmt(p.amount) + ' ' + p.name));
  }

  /* ---------- NavRail ---------- */
  // A locked item stays in the Tab order (Standards · States): it says "Locked", its unlock condition opens beside it on
  // hover and focus, and a click or Enter shows the condition instead of navigating.
  function NavRail(p) {
    var base = useId('lgr');
    return h('nav', { className: cx('lg-rail', p.compact && 'lg-rail--compact', p.className), 'aria-label': p.label || 'Game' },
      p.header ? h('div', { className: 'lg-rail__header' }, p.header) : null,
      h('div', { className: 'lg-rail__sections' },
        (p.sections || []).map(function (s, si) {
          return h('div', { key: s.label, className: 'lg-rail__section' },
            h('div', { className: 'lg-rail__group' }, s.label),
            h('ul', null, (s.items || []).map(function (it, ii) {
              var active = it.id === p.activeId && !it.locked;
              var w = it.locked ? why(base + '-' + si + '-' + ii, blockedReason('locked', it, 'NavRail item "' + it.title + '"').reason, { word: 'Locked', place: 'end', title: p.compact ? it.title : null }) : null;
              var props = {
                href: it.href || '#',
                className: cx('lg-rail__item', active && 'is-active', it.locked && 'is-locked'),
                'aria-current': active ? 'page' : undefined,
                title: p.compact && !it.locked ? it.title : undefined,
                onClick: it.locked ? undefined : function (e) { if (!it.href || it.href === '#') e.preventDefault(); if (p.onNavigate) p.onNavigate(it.id); }
              };
              if (w) props = chain(props, w.props);
              return h('li', { key: it.id },
                h('a', props,
                  it.icon ? h(Icon, { name: it.icon, size: 20 }) : null,
                  // With a description (D-104) the title and its line stack; compact shows neither.
                  it.description ? h('span', { className: 'lg-rail__text' }, h('span', { className: 'lg-rail__title' }, it.title), h('span', { className: 'lg-rail__desc' }, it.description))
                    : h('span', { className: 'lg-rail__title' }, it.title),
                  // The 12px lock marker once it is drawn (Foundations · Iconography); until then the word.
                  // One mark at the item's end (Standards · State combinations): Locked, else a count badge, else the
                  // attention diamond for something waiting with nothing to count.
                  it.locked ? h('span', { className: 'lg-rail__lock', 'aria-hidden': 'true' }, ICONS.lock ? h(Icon, { name: 'lock', size: 12 }) : STATES.locked.word)
                    : it.badge ? h('span', { className: 'lg-rail__badge', 'aria-label': it.badgeLabel || undefined }, it.badge)
                    : it.ready ? attention('lg-rail__attention') : null,
                  !it.locked && !it.badge && it.ready ? h('span', { className: 'lg-sr' }, ', ' + (it.badgeLabel || readyWords(it.ready))) : null,
                  w ? w.desc : null));
            })));
        })),
      p.footer ? h('div', { className: 'lg-rail__footer' }, p.footer) : null);
  }

  /* ---------- TabStrip ---------- */
  function TabStrip(p) {
    var tabs = p.tabs || [];
    var lvl = p.level || 'primary';
    return h('div', {
      className: cx('lg-tabs', 'lg-tabs--' + lvl, p.className), role: 'tablist', 'aria-label': p.label, 'data-density': p.density,
      onKeyDown: function (e) {
        if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
        e.preventDefault();
        var i = tabs.findIndex(function (t) { return t.id === p.activeId; });
        var j = (i + (e.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
        if (p.onChange) p.onChange(tabs[j].id);
        var el = e.currentTarget.querySelectorAll('[role=tab]')[j];
        if (el) el.focus();
      }
    }, tabs.map(function (t) {
      var active = t.id === p.activeId;
      return h('button', {
        key: t.id, type: 'button', role: 'tab', 'aria-selected': active, tabIndex: active ? 0 : -1,
        className: cx('lg-tabs__tab', active && 'is-active'),
        onClick: function () { if (p.onChange) p.onChange(t.id); }
      }, t.label, t.count != null ? h('span', { className: 'lg-tabs__count' }, t.count) : null);
    }));
  }

  /* ---------- Stage ---------- */
  function Stage(p) {
    return h('section', { className: cx('lg-stage', p.className), 'aria-label': p.label },
      // The veil — vignette, fades and film grain — belongs to the art: without an image there is none (Foundations · Ornament).
      p.image ? h('div', { className: 'lg-stage__art', 'aria-hidden': 'true', style: { backgroundImage: 'url(' + p.image + ')', backgroundPosition: p.focus || 'center' } }) : null,
      p.image ? h('div', { className: 'lg-stage__veil', 'aria-hidden': 'true' }) : null,
      h('div', { className: 'lg-stage__content' }, p.children));
  }

  /* ---------- TopBar ---------- */
  function TopBar(p) {
    return h('header', { className: cx('lg-topbar', p.className) },
      p.onMenu ? h('button', { type: 'button', className: 'lg-topbar__menu', 'aria-label': 'Open navigation', onClick: p.onMenu },
        h('svg', { viewBox: '0 0 24 24', width: 20, height: 20, fill: 'none', stroke: 'currentColor', strokeWidth: 1.6, strokeLinecap: 'round', 'aria-hidden': 'true' },
          h('path', { d: 'M4 7h16M4 12h16M4 17h10' }))) : null,
      h('div', { className: 'lg-topbar__title' },
        p.eyebrow ? h('span', { className: 'lg-topbar__eyebrow' }, p.eyebrow) : null,
        h('span', { className: 'lg-topbar__name' }, p.title)),
      p.center ? h('div', { className: 'lg-topbar__center' }, p.center) : null,
      h('div', { className: 'lg-topbar__end' }, p.children));
  }

  /* ---------- Page (scrolling screen without stage art) ---------- */
  // `flow`: a Page in a host frame that is not GameShell (the game's frame while screens migrate): in the normal flow,
  // filling its parent's height, with no room kept for a TopBar; the host gives the gutters.
  function Page(p) {
    return h('div', { className: cx('lg-page', p.flow && 'lg-page--flow', p.className), role: p.role, 'aria-label': p.label },
      h('div', { className: 'lg-page__inner', style: p.maxWidth ? { maxWidth: p.maxWidth } : undefined }, p.children));
  }

  /* ---------- PageHeader ---------- */
  function PageHeader(p) {
    return h('header', { className: cx('lg-pagehead', p.className) },
      p.icon ? h('span', { className: 'lg-pagehead__icon', 'aria-hidden': 'true' },
        // The current section is the current location, so its icon sits in a diamond (Foundations · Shape): a hexagon is a Sigil's value.
        h('svg', { viewBox: '0 0 100 100', className: 'lg-pagehead__mark' }, h('polygon', { points: '50,1.5 98.5,50 50,98.5 1.5,50' })),
        h(Icon, { name: p.icon, size: 24 })) : null,
      h('div', { className: 'lg-pagehead__text' },
        p.eyebrow ? h('span', { className: 'lg-pagehead__eyebrow' }, p.eyebrow) : null,
        h(Heading, { level: 'screen' }, p.title),
        p.summary ? h('p', { className: 'lg-pagehead__summary' }, p.summary) : null),
      p.children ? h('div', { className: 'lg-pagehead__actions' }, p.children) : null);
  }

  /* ---------- SearchField (combobox) ---------- */
  function SearchField(p) {
    var id = useId('lgs');
    var openSt = R.useState(false), activeSt = R.useState(-1);
    var list = p.suggestions || [];
    var showPanel = openSt[0] && (p.loading || list.length > 0 || (p.searched && (p.value || '').length > 0));
    function choose(v) { openSt[1](false); activeSt[1](-1); if (p.onSelect) p.onSelect(v); }
    // Screen readers hear how many suggestions there are, or that a search found nothing — once, through the announcer.
    var said = showPanel && !p.loading ? (list.length ? list.length + (list.length === 1 ? ' suggestion' : ' suggestions') : (p.searched ? (p.emptyText || 'No matching players') : '')) : '';
    R.useEffect(function () { if (said) announce(said, { key: 'search-' + id }); }, [said]);
    return h('div', { className: cx('lg-search', p.className) },
      h('div', { className: 'lg-search__field' },
        h('svg', { className: 'lg-search__glass', viewBox: '0 0 24 24', width: 16, height: 16, fill: 'none', stroke: 'currentColor', strokeWidth: 1.6, strokeLinecap: 'round', 'aria-hidden': 'true' },
          h('circle', { cx: 10.5, cy: 10.5, r: 6 }), h('path', { d: 'M15 15l5 5' })),
        h('input', {
          id: id, type: 'text', className: 'lg-input lg-search__input', autoComplete: 'off',
          role: 'combobox', 'aria-autocomplete': 'list', 'aria-controls': id + '-list', 'aria-expanded': !!showPanel,
          'aria-activedescendant': activeSt[0] >= 0 ? id + '-opt-' + activeSt[0] : undefined,
          'aria-label': p.label || p.placeholder, placeholder: p.placeholder, maxLength: p.maxLength || 80,
          value: p.value || '',
          onChange: function (e) { openSt[1](true); activeSt[1](-1); if (p.onChange) p.onChange(e.target.value); },
          onFocus: function () { openSt[1](true); },
          onBlur: function () { setTimeout(function () { openSt[1](false); }, 120); },
          onKeyDown: function (e) {
            if (e.key === 'ArrowDown' && list.length) { e.preventDefault(); openSt[1](true); activeSt[1]((activeSt[0] + 1) % list.length); }
            else if (e.key === 'ArrowUp' && list.length) { e.preventDefault(); activeSt[1]((activeSt[0] - 1 + list.length) % list.length); }
            else if (e.key === 'Enter') { e.preventDefault(); if (activeSt[0] >= 0 && list[activeSt[0]]) choose(list[activeSt[0]]); else if (p.onSubmit) { openSt[1](false); p.onSubmit(p.value); } }
            else if (e.key === 'Escape' && openSt[0]) { e.stopPropagation(); openSt[1](false); }
          }
        })),
      showPanel ? h('div', { id: id + '-list', className: 'lg-search__panel', role: 'listbox' },
        p.loading ? h('div', { className: 'lg-search__note' }, p.loadingText || 'Finding players…') : null,
        list.map(function (v, i) {
          return h('div', {
            key: v, id: id + '-opt-' + i, role: 'option', 'aria-selected': i === activeSt[0],
            className: cx('lg-search__option', i === activeSt[0] && 'is-active'),
            onMouseEnter: function () { activeSt[1](i); },
            onMouseDown: function (e) { e.preventDefault(); choose(v); }
          }, v);
        }),
        !p.loading && !list.length ? h('div', { className: 'lg-search__note' }, p.emptyText || 'No matching players') : null) : null);
  }

  /* ---------- Banner (art-backed band) ---------- */
  function Banner(p) {
    var bref = R.useRef(null);
    R.useEffect(function () { checkFramed(bref.current); }, []);
    var corner = p.cornerSrc ? { WebkitMaskImage: 'url(' + p.cornerSrc + ')', maskImage: 'url(' + p.cornerSrc + ')' } : null;
    return h('section', { ref: bref, className: cx('lg-banner', p.className), 'aria-label': p.label },
      p.image ? h('div', { className: 'lg-banner__art', 'aria-hidden': 'true', style: { backgroundImage: 'url(' + p.image + ')', backgroundPosition: p.focus || 'center' } }) : null,
      p.image ? h('div', { className: 'lg-banner__veil', 'aria-hidden': 'true' }) : null,
      h('span', { className: 'lg-banner__frame', 'aria-hidden': 'true' }),
      corner ? ['tl', 'tr', 'bl', 'br'].map(function (c) {
        return h('span', { key: c, className: 'lg-folio__corner lg-folio__corner--' + c, style: corner, 'aria-hidden': 'true' });
      }) : null,
      h('div', { className: 'lg-banner__body' },
        h('div', { className: 'lg-banner__main' }, p.children),
        p.aside ? h('div', { className: 'lg-banner__aside' }, p.aside) : null),
      p.footer ? h('div', { className: 'lg-banner__footer' }, p.footer) : null);
  }

  /* ---------- StatFigure ---------- */
  function StatFigure(p) {
    return h('div', { className: cx('lg-figure', p.size && 'lg-figure--' + p.size, p.className), title: p.title },
      h('span', { className: 'lg-figure__label' }, p.label),
      h('span', { className: 'lg-figure__value' }, typeof p.value === 'number' || missing(p.value) ? fmt(p.value) : p.value),
      p.caption ? h('span', { className: 'lg-figure__caption' }, p.caption) : null);
  }

  /* ---------- JourneyCard ---------- */
  function JourneyCard(p) {
    var stages = p.stages || [];
    var cur = Math.max(0, stages.indexOf(p.phase));
    return h('section', { className: cx('lg-journey', p.className), 'aria-labelledby': p.id ? p.id + '-title' : undefined },
      h('div', { className: 'lg-journey__main' },
        h('span', { className: 'lg-journey__phase' }, p.phase),
        h(Heading, { level: 'section', id: p.id ? p.id + '-title' : undefined }, p.title),
        p.summary ? h('p', { className: 'lg-journey__summary' }, p.summary) : null,
        p.objective ? h('div', { className: 'lg-journey__objective' },
          h('span', { className: 'lg-journey__label' }, p.objectiveLabel || 'Recommended now'),
          h('strong', null, p.objective)) : null,
        p.actions ? h('div', { className: 'lg-journey__actions' }, p.actions) : null),
      p.nextUnlock ? h('aside', { className: 'lg-journey__unlock' },
        h('svg', { className: 'lg-journey__key', viewBox: '0 0 24 24', width: 24, height: 24, fill: 'none', stroke: 'currentColor', strokeWidth: 1.6, strokeLinecap: 'round', strokeLinejoin: 'round', 'aria-hidden': 'true' },
          h('circle', { cx: 8, cy: 12, r: 4 }), h('path', { d: 'M12 12h9M18 12v3M21 12v2' })),
        h('span', { className: 'lg-journey__label' }, p.nextUnlockLabel || 'Next unlock'),
        h('strong', null, p.nextUnlock)) : null,
      stages.length > 1 ? h('div', { className: 'lg-journey__track' },
        h(Track, { steps: stages.length, current: cur, labels: stages, label: 'Journey', startLabel: stages[0], endLabel: stages[stages.length - 1] })) : null);
  }

  /* ---------- Ledger (label / value rows with explanations) ---------- */
  // Values align on the decimal point: the integer sits right in one column, the fraction and unit left in the
  // next (the rows share these columns through CSS subgrid). A value that is not a plain number sits at the right.
  function ledgerValue(v) {
    var q = numParts(missing(v) ? NONE : v);
    if (!q) return h('dd', { className: 'lg-ledger__value is-text' }, h('span', { className: 'lg-ledger__num' }, v));
    return h('dd', { className: 'lg-ledger__value' },
      h('span', { className: 'lg-ledger__num lg-ledger__int' }, q.int),
      h('span', { className: 'lg-ledger__num lg-ledger__tail' }, q.frac,
        q.unit ? h('span', { className: 'lg-unit' }, (q.sp ? NBSP : '') + q.unit) : null));
  }
  function Ledger(p) {
    var id = useId('lgl');
    var rows = p.rows || [];
    // Rows that explain themselves are one tab stop (roving tabindex); Up, Down, Home and End move between them.
    // A click or tap pins a row's explanation open; Escape closes it, and hides a hover explanation until the pointer leaves.
    var described = []; rows.forEach(function (r, i) { if (r.description) described.push(i); });
    var foc = R.useState(null), pin = R.useState(null), dis = R.useState(null), rootRef = R.useRef(null);
    var active = foc[0] != null ? foc[0] : described[0];
    R.useEffect(function () {
      if (pin[0] == null) return;
      function away(e) { if (rootRef.current && !rootRef.current.contains(e.target)) pin[1](null); }
      document.addEventListener('pointerdown', away);
      return function () { document.removeEventListener('pointerdown', away); };
    }, [pin[0]]);
    function onKeyDown(e) {
      if (e.key === 'Escape') {
        var i = +e.target.getAttribute('data-index');
        if (pin[0] === i) { pin[1](null); e.stopPropagation(); } else if (dis[0] !== i) { dis[1](i); e.stopPropagation(); }
        return;
      }
      var at = described.indexOf(active);
      var j = moveKey(e.key === 'ArrowLeft' || e.key === 'ArrowRight' ? '' : e.key, at, described.length);
      if (j == null || at < 0) return;
      e.preventDefault(); foc[1](described[j]); dis[1](null);
      var el = e.currentTarget.querySelector('[data-index="' + described[j] + '"]'); if (el) el.focus();
    }
    return h('section', { ref: rootRef, className: cx('lg-ledger', p.className), 'aria-labelledby': id + '-t', 'data-density': p.density },
      h('h3', { className: 'lg-ledger__title', id: id + '-t' }, p.title),
      h('dl', { className: 'lg-ledger__rows', onKeyDown: onKeyDown }, rows.map(function (r, i) {
        var tipId = r.description ? id + '-tip-' + i : undefined;
        return h('div', {
          key: r.id || r.label, className: cx('lg-ledger__row', r.muted && 'is-muted', pin[0] === i && 'is-pinned', dis[0] === i && 'is-dismissed'),
          tabIndex: r.description ? (i === active ? 0 : -1) : undefined, 'aria-describedby': tipId, 'data-index': i,
          onFocus: r.description ? function () { foc[1](i); } : undefined,
          onClick: r.description ? function () { pin[1](pin[0] === i ? null : i); dis[1](null); } : undefined,
          onMouseLeave: r.description ? function () { if (dis[0] === i) dis[1](null); } : undefined,
          onBlur: r.description ? function () { if (dis[0] === i) dis[1](null); } : undefined
        },
          h('dt', { className: 'lg-ledger__label' }, h('span', { className: 'lg-ledger__labeltext' }, r.label),
            h('span', { className: 'lg-ledger__leader', 'aria-hidden': 'true' })),
          ledgerValue(r.value),
          r.sub ? h('dd', { className: 'lg-ledger__sub' }, r.sub) : null,
          r.description ? h('div', { id: tipId, role: 'tooltip', className: 'lg-ledger__tip' },
            h('strong', null, r.label),
            h('p', null, r.description),
            r.tipMeta ? h('span', { className: 'lg-ledger__tipmeta' }, r.tipMeta) : null) : null);
      })));
  }

  /* ---------- LoadoutSlot ---------- */
  // Three states (Standards · States): attuned (Ownership · Attuned, a neutral Tag), open (Data · Empty: an empty frame
  // and "Empty") and locked (Availability · Locked: a dashed frame, a Locked Tag and the unlock condition printed as the
  // name). A locked slot that would be a button stays one, aria-disabled, its printed condition as the description.
  // `compact` (D-103) is one short row for a full loadout: the name leads, the slot number is for screen readers only,
  // and an attuned slot drops its Tag — being in the loadout says it. Open says "Empty"; locked keeps its Tag.
  function LoadoutSlot(p) {
    var id = useId('lgo');
    var state = p.state || (p.name ? 'attuned' : 'open');
    var locked = state === 'locked';
    var interactive = typeof p.onClick === 'function';
    var unlock = locked ? (p.reason || p.unlockLabel || '') : '';
    if (locked && !unlock) stateWarn('loadout:' + p.index, 'LoadoutSlot ' + (p.index != null ? p.index + 1 : '') + ' is locked with no reason; say how it unlocks');
    var w = locked && interactive ? why(id + '-why', unlock, { word: 'Locked', printed: true }) : null;
    var tag = state === 'attuned' ? h(Tag, { state: 'attuned' }) : locked ? h(Tag, { state: 'locked' }) : null;
    var name = h('div', { className: cx('lg-loadout__name', p.rarity && state === 'attuned' && 'lg-itemlink--' + String(p.rarity).toLowerCase()) },
      state === 'attuned' ? p.name : state === 'open' ? 'Empty' : (unlock || STATES.locked.word));
    var props = {
      type: interactive ? 'button' : undefined, onClick: interactive && !locked ? p.onClick : undefined,
      className: cx('lg-loadout', 'is-' + state, p.compact && 'lg-loadout--compact', p.className)
    };
    // The condition is printed inside the button, so it is already in the name ("Slot 3 Locked Unlocks at level 20").
    if (w) { props = chain(props, w.props); delete props['aria-describedby']; }
    return h(interactive ? 'button' : 'div', props,
      h(ItemSlot, { icon: state === 'attuned' ? (p.icon || 'essences') : undefined, image: state === 'attuned' ? p.image : undefined, rarity: state === 'attuned' ? p.rarity : undefined, caption: false, size: 'sm' }),
      h('div', { className: 'lg-loadout__body' },
        // The head: "Slot 3" at the start; the Tag, then the attention diamond, at the end (Standards · State combinations).
        // Compact: the name takes the slot label's place, and only a locked slot keeps its Tag.
        h('div', { className: 'lg-loadout__head' },
          h('span', { className: p.compact ? 'lg-sr' : 'lg-loadout__slot' }, 'Slot ' + (p.index != null ? p.index + 1 : '')),
          p.compact ? name : null,
          p.compact && !locked ? null : tag,
          p.ready && !locked ? attention('lg-loadout__attention') : null,
          p.ready && !locked ? h('span', { className: 'lg-sr' }, readyWords(p.ready)) : null),
        p.compact ? null : name,
        state === 'attuned' && (p.active || p.passive) ? h('div', { className: 'lg-loadout__abilities' },
          p.active ? h('span', null, h('b', null, 'Active'), ' ', p.active.name, p.active.cooldown ? ' \u00B7 ' + p.active.cooldown : '') : null,
          p.passive ? h('span', null, h('b', null, 'Passive'), ' ', p.passive.name) : null) : null,
        state === 'open' && p.hint ? h('div', { className: 'lg-loadout__abilities' }, p.hint) : null));
  }

  /* ---------- Presence ---------- */
  function Presence(p) {
    return h('span', { className: cx('lg-presence', p.online ? 'is-online' : 'is-offline', p.className), title: p.online ? 'Online' : (p.lastSeen ? 'Last seen ' + p.lastSeen : 'Last seen unknown') },
      h('span', { className: 'lg-presence__dot', 'aria-hidden': 'true' }),
      p.online ? 'Online' : (p.compact ? (p.lastSeen || 'Unknown') : 'Last seen ' + (p.lastSeen || 'unknown')));
  }

  /* ---------- ProfileIdentity ---------- */
  // Who a player is, at the head of a profile (D-099): an eyebrow, the name with the Nobility crown and, for another
  // player, Presence; then a short list of facts — Guild, Essences, Achievement Points, Nobility. It sits in a Banner's body.
  function ProfileIdentity(p) {
    var facts = (p.facts || []).filter(Boolean);
    return h('div', { className: cx('lg-identity', p.className) },
      p.eyebrow ? h('span', { className: 'lg-identity__eyebrow' }, p.eyebrow) : null,
      h('div', { className: 'lg-identity__name' },
        p.noble ? h('span', { className: 'lg-identity__noble', role: 'img', 'aria-label': 'Noble', title: 'Active Nobility' }, h(Icon, { name: 'nobility', size: 16 })) : null,
        h(Heading, { level: 'screen', as: p.as || 'h2', id: p.headingId }, p.name),
        p.presence ? h(Presence, p.presence) : null),
      facts.length ? h('dl', { className: 'lg-identity__facts' }, facts.map(function (f, i) {
        return h('div', { key: f.key || i }, h('dt', null, f.label), h('dd', null, f.value));
      })) : null);
  }

  /* ---------- ItemLink ---------- */
  function ItemLink(p) {
    var r = p.rarity;
    var interactive = typeof p.onClick === 'function';
    return h(interactive ? 'button' : 'span', {
      type: interactive ? 'button' : undefined,
      className: cx('lg-itemlink', r && 'lg-itemlink--' + String(r).toLowerCase(), p.className),
      onClick: interactive ? p.onClick : undefined,
      title: r ? r + (p.meta ? ' · ' + p.meta : '') : p.meta
    }, '[', p.children, ']', r ? h('span', { className: 'lg-sr' }, ', ' + r) : null);
  }

  /* ---------- Chronicle (chat + game log) ---------- */
  function chClass(id) { return 'lg-ch--' + String(id || 'general').toLowerCase(); }
  function ChronicleLine(p) {
    var m = p.m, kind = m.kind || 'chat';
    // A day break (D-112): the date between two days' lines, set as a rule; no time, tag or author.
    if (kind === 'day') return h('span', { className: 'lg-chronicle__text' }, m.text);
    var channelLabel = m.channelLabel || m.channel;
    var tag = p.showTag && channelLabel ? h('span', { className: 'lg-chronicle__tag' }, channelLabel) : null;
    if (kind === 'system' || kind === 'loot') {
      return h(F, null,
        m.time && !p.compact ? h('time', { className: 'lg-chronicle__time' }, m.time) : null,
        tag, h('span', { className: 'lg-chronicle__text' }, m.text));
    }
    var author = m.direction === 'to' ? 'To ' + m.author : m.direction === 'from' ? 'From ' + m.author : m.author;
    // With onAuthor the author is a button that opens the host's player actions (D-112) — not in the collapsed ticker,
    // which is a button itself. A Noble's crown sits before the name (D-066), at the 12px marker size.
    return h(F, null,
      m.time && !p.compact ? h('time', { className: 'lg-chronicle__time' }, m.time) : null,
      tag,
      author && m.noble ? h('span', { className: 'lg-chronicle__noble', role: 'img', 'aria-label': 'Noble', title: 'Active Nobility' }, h(Icon, { name: 'nobility', size: 12 })) : null,
      author ? (p.onAuthor && !p.compact
        ? h('button', { type: 'button', className: 'lg-chronicle__author', onClick: function (e) { p.onAuthor(m, e.currentTarget); } }, author)
        : h('span', { className: 'lg-chronicle__author' }, author)) : null,
      h('span', { className: 'lg-chronicle__text' }, m.text));
  }
  // The Chronicle is always Compact: its controls' icons are icon-sm (16px).
  var ICON_PROPS = { viewBox: '0 0 24 24', width: 16, height: 16, fill: 'none', stroke: 'currentColor', strokeWidth: 1.6, strokeLinecap: 'round', strokeLinejoin: 'round', 'aria-hidden': 'true' };
  function Chronicle(p) {
    var channels = p.channels || [];
    var open = p.open !== false;
    var active = p.activeChannel || (channels[0] && channels[0].id) || 'all';
    var all = p.messages || [];
    var msgs = active === 'all' ? all : all.filter(function (m) { return m.channel === active; });
    var last = all[all.length - 1];
    var unread = channels.reduce(function (a, c) { return a + (c.unread || 0); }, 0);
    // Live updates never move what the player is reading (Foundations · Motion). The log follows the newest line only
    // while the player is at its foot. Scrolled up, it keeps the line they are reading where it is — even when old lines
    // leave the top — and counts what arrived; the count jumps to the foot.
    var logRef = R.useRef(null), stick = R.useRef(true), anchor = R.useRef(null), newSt = R.useState(0);
    var seen = R.useRef({ last: null, active: active, open: open });
    var lastId = msgs.length ? msgs[msgs.length - 1].id : null;
    function readAnchor(el) {
      var lines = el.querySelectorAll('.lg-chronicle__line');
      for (var i = 0; i < lines.length; i++) if (lines[i].offsetTop + lines[i].offsetHeight > el.scrollTop) { anchor.current = { id: lines[i].getAttribute('data-id'), off: lines[i].offsetTop - el.scrollTop }; return; }
      anchor.current = null;
    }
    R.useLayoutEffect(function () {
      var el = logRef.current, sn = seen.current;
      var reset = sn.active !== active || (open && !sn.open);
      var prevLast = sn.last;
      seen.current = { last: lastId, active: active, open: open };
      if (!el) return;
      if (reset || stick.current) { stick.current = true; el.scrollTop = el.scrollHeight; if (newSt[0]) newSt[1](0); return; }
      if (prevLast === lastId) return;
      var a = anchor.current, line = a && el.querySelector('.lg-chronicle__line[data-id="' + String(a.id).replace(/"/g, '\\"') + '"]');
      if (line) el.scrollTop = line.offsetTop - a.off;
      var idx = -1;
      for (var i = msgs.length - 1; i >= 0; i--) if (msgs[i].id === prevLast) { idx = i; break; }
      var added = idx < 0 ? msgs.length : msgs.length - 1 - idx;
      if (added > 0) newSt[1](function (n) { return n + added; });
    }, [lastId, msgs.length, active, open]);
    function onLogScroll(e) {
      var el = e.currentTarget, atFoot = el.scrollHeight - el.scrollTop - el.clientHeight < 24;
      stick.current = atFoot;
      readAnchor(el);
      if (atFoot && newSt[0]) newSt[1](0);
    }
    function jump() {
      var el = logRef.current; stick.current = true; newSt[1](0);
      if (el) { el.scrollTop = el.scrollHeight; el.focus(); }
    }
    // Spatial: opened by the player, the body rises in over duration-base; closing is at once.
    var wasOpen = R.useRef(open), entSt = R.useState(false);
    R.useLayoutEffect(function () { if (open && wasOpen.current === false) entSt[1](true); wasOpen.current = open; }, [open]);
    // announce: 'all' (the default) makes the log a polite live region; 'mentions' announces only lines that mention
    // you and whispers to you, through the throttled announcer; 'off' announces nothing. Combat events never belong here.
    var mode = p.announce || 'all', seenRef = R.useRef(all.length);
    R.useEffect(function () {
      var from = seenRef.current; seenRef.current = all.length;
      if (mode !== 'mentions' || from >= all.length) return;
      all.slice(from).forEach(function (m) {
        if (!(m.mention || m.direction === 'from')) return;
        var who = m.direction === 'from' ? 'Whisper from ' + m.author : (m.author ? m.author + ' mentioned you' : 'You were mentioned');
        announce(typeof m.text === 'string' ? who + ': ' + m.text : who, { key: 'chronicle-' + (m.id != null ? m.id : who) });
      });
    }, [all.length, mode]);
    var comp = p.composer;
    var toggle = p.onToggle ? h('button', {
      type: 'button', className: 'lg-chronicle__toggle', onClick: p.onToggle,
      'aria-expanded': open, 'aria-label': open ? 'Collapse chat' : 'Expand chat'
    }, h('svg', ICON_PROPS, h('path', { d: open ? 'M6 9l6 6 6-6' : 'M6 15l6-6 6 6' }))) : null;
    var tallBtn = open && p.onTallToggle ? h('button', {
      type: 'button', className: 'lg-chronicle__toggle lg-chronicle__tall', onClick: p.onTallToggle,
      'aria-pressed': !!p.tall, 'aria-label': p.tall ? 'Make chat shorter' : 'Make chat taller'
    }, h('svg', ICON_PROPS, h('path', { d: p.tall ? 'M8 4l4 4 4-4M8 20l4-4 4 4' : 'M8 8l4-4 4 4M8 16l4 4 4-4' }))) : null;
    var grip = p.dragHandle ? h('button', Object.assign({
      type: 'button', className: 'lg-chronicle__toggle lg-chronicle__grip',
      'aria-label': 'Drag chat', title: 'Drag to move · arrow keys nudge'
    }, p.dragHandle), h('svg', { viewBox: '0 0 24 24', width: 16, height: 16, fill: 'currentColor', 'aria-hidden': 'true' },
      [[9, 6], [15, 6], [9, 12], [15, 12], [9, 18], [15, 18]].map(function (c, i) { return h('circle', { key: i, cx: c[0], cy: c[1], r: 1.4 }); }))) : null;
    return h('section', {
      className: cx('lg-chronicle', open ? 'is-open' : 'is-collapsed', p.floating && 'lg-chronicle--floating', p.tall && 'is-tall', entSt[0] && 'is-entering', p.className),
      'aria-label': p.label || 'Chronicle',
      onAnimationEnd: entSt[0] ? function (e) { if (e.target.classList && e.target.classList.contains('lg-chronicle__body')) entSt[1](false); } : undefined
    },
      h('header', { className: 'lg-chronicle__head' },
        open
          ? h('div', {
            className: 'lg-chronicle__channels', role: 'tablist', 'aria-label': 'Channels',
            onKeyDown: function (e) {
              if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
              e.preventDefault();
              var i = channels.findIndex(function (c) { return c.id === active; });
              var j = (i + (e.key === 'ArrowRight' ? 1 : -1) + channels.length) % channels.length;
              if (p.onChannelChange) p.onChannelChange(channels[j].id);
              var el = e.currentTarget.querySelectorAll('[role=tab]')[j];
              if (el) el.focus();
            }
          }, channels.map(function (c) {
            var on = c.id === active;
            return h('button', {
              key: c.id, type: 'button', role: 'tab', 'aria-selected': on, tabIndex: on ? 0 : -1,
              className: cx('lg-chronicle__channel', chClass(c.id), on && 'is-active'),
              onClick: function () { if (p.onChannelChange) p.onChannelChange(c.id); }
            }, c.label, c.unread ? h('span', { className: 'lg-chronicle__unread', 'aria-label': c.unread + ' unread' }, c.unread) : null);
          }))
          : h('button', { type: 'button', className: cx('lg-chronicle__ticker', last && chClass(last.channel), last && 'lg-chronicle__line--' + (last.kind || 'chat')), onClick: p.onToggle, 'aria-label': 'Open chat' + (unread ? ', ' + unread + ' unread' : '') },
            last ? h(ChronicleLine, { m: last, showTag: true, compact: true }) : h('span', { className: 'lg-chronicle__text' }, 'Chronicle'),
            h('span', { className: 'lg-chronicle__strip-label', 'aria-hidden': 'true' }, p.label || 'Chronicle'),
            unread ? h('span', { className: 'lg-chronicle__unread', 'aria-hidden': 'true' }, unread) : null),
        p.aside ? h('div', { className: 'lg-chronicle__aside' }, p.aside) : null,
        tallBtn, grip, toggle),
      open ? h('div', { className: 'lg-chronicle__body' },
        h('ol', { className: 'lg-chronicle__log', ref: logRef, onScroll: onLogScroll, 'aria-live': mode === 'all' ? 'polite' : 'off', 'aria-relevant': 'additions', tabIndex: 0, 'aria-label': 'Messages' },
          msgs.map(function (m) {
            return h('li', { key: m.id, 'data-id': m.id, className: cx('lg-chronicle__line', 'lg-chronicle__line--' + (m.kind || 'chat'), chClass(m.channel), m.mention && 'is-mention') },
              h(ChronicleLine, { m: m, showTag: active === 'all' || m.channel !== active, onAuthor: p.onAuthor }));
          })),
        newSt[0] ? h(Button, {
          size: 'sm', className: 'lg-chronicle__jump', onClick: jump,
          'aria-label': newSt[0] + ' new ' + (newSt[0] === 1 ? 'line' : 'lines') + ', jump to latest'
        }, newSt[0] + ' new ' + (newSt[0] === 1 ? 'line' : 'lines')) : null) : null,
      // A composer of the host's own (D-112) — a rich editor with @mention suggestions — in the composer's place.
      open && comp && R.isValidElement(comp) ? h('div', { className: 'lg-chronicle__composer lg-chronicle__composer--custom' }, comp) :
      open && comp ? h('form', {
        className: 'lg-chronicle__composer',
        onSubmit: function (e) { e.preventDefault(); stick.current = true; if (comp.onSend && comp.value && String(comp.value).trim()) comp.onSend(comp.value); }
      },
        h('span', { className: cx('lg-chronicle__prefix', chClass(comp.channel || active)) }, comp.channelLabel || comp.channel || active),
        h('input', {
          className: 'lg-chronicle__input', value: comp.value || '', maxLength: comp.maxLength,
          onChange: function (e) { if (comp.onChange) comp.onChange(e.target.value); },
          placeholder: comp.placeholder || 'Say something — /g guild, /w name whisper',
          'aria-label': 'Chat message'
        }),
        h('button', { type: 'submit', className: 'lg-chronicle__send', 'aria-label': 'Send' }, h(Key, null, '↵'))) : null);
  }

  /* ---------- GameShell ---------- */
  function clamp(v, lo, hi) { return Math.max(lo, Math.min(hi, v)); }
  function GameShell(p) {
    var st = R.useState(false), railOpen = st[0], setRailOpen = st[1];
    var tallSt = R.useState(false);
    var posSt = R.useState(null), pos = p.chroniclePosition !== undefined ? p.chroniclePosition : posSt[0];
    var shellRef = R.useRef(null), winRef = R.useRef(null), railRef = R.useRef(null), openerRef = R.useRef(null);
    var mainId = useId('lgmain');
    var openRail = function () { openerRef.current = document.activeElement; setRailOpen(true); };
    // The rail drawer (under 60rem) takes focus when it opens, makes the rest inert, closes on Escape and returns focus.
    R.useEffect(function () {
      if (!railOpen) return;
      var el = railRef.current, f = el && el.querySelector('button:not([disabled]), a[href], [tabindex="0"]');
      if (f) f.focus();
      function onKey(e) { if (e.key === 'Escape') { e.stopPropagation(); setRailOpen(false); } }
      document.addEventListener('keydown', onKey);
      return function () {
        document.removeEventListener('keydown', onKey);
        var o = openerRef.current; if (o && o.isConnected && o.focus) o.focus();
      };
    }, [railOpen]);
    var top = typeof p.top === 'function' ? p.top({ openRail: openRail }) : p.top;
    var layout = p.chatLayout === 'floating' ? 'floating' : 'docked';
    var chat = p.chronicle || null;
    var chatOpen = p.chronicleOpen != null ? p.chronicleOpen : !(chat && chat.props && chat.props.open === false);

    function setPos(next) {
      if (p.chroniclePosition === undefined) posSt[1](next);
      if (p.onChroniclePositionChange) p.onChroniclePositionChange(next);
    }
    function bounds() {
      var sh = shellRef.current, w = winRef.current;
      if (!sh || !w) return null;
      return { sh: sh.getBoundingClientRect(), w: w.getBoundingClientRect() };
    }
    function place(left, bottom, b) {
      var m = 8;
      setPos({
        left: Math.round(clamp(left, m, b.sh.width - b.w.width - m)),
        bottom: Math.round(clamp(bottom, m, b.sh.height - b.w.height - m))
      });
    }
    var dragHandle = layout === 'floating' ? {
      onPointerDown: function (e) {
        var b = bounds(); if (!b) return;
        e.preventDefault();
        var dx = e.clientX - b.w.left, dy = b.w.bottom - e.clientY;
        function move(ev) { place(ev.clientX - b.sh.left - dx, b.sh.bottom - ev.clientY - dy, bounds() || b); }
        function up() { window.removeEventListener('pointermove', move); window.removeEventListener('pointerup', up); }
        window.addEventListener('pointermove', move);
        window.addEventListener('pointerup', up);
      },
      onKeyDown: function (e) {
        var d = { ArrowLeft: [-16, 0], ArrowRight: [16, 0], ArrowUp: [0, 16], ArrowDown: [0, -16] }[e.key];
        var b = bounds(); if (!d || !b) return;
        e.preventDefault();
        place(b.w.left - b.sh.left + d[0], b.sh.bottom - b.w.bottom + d[1], b);
      }
    } : undefined;

    var chatEl = null;
    if (chat) {
      if (layout === 'floating') {
        var tall = chat.props.tall != null ? chat.props.tall : tallSt[0];
        chatEl = h('div', {
          ref: winRef,
          className: cx('lg-shell__chronicle', 'lg-shell__chronicle--floating', pos && 'is-placed'),
          style: pos ? { '--lg-float-left': pos.left + 'px', '--lg-float-bottom': pos.bottom + 'px' } : undefined
        }, R.cloneElement(chat, {
          floating: true, tall: tall, dragHandle: dragHandle,
          onTallToggle: chat.props.onTallToggle || function () { tallSt[1](!tallSt[0]); }
        }));
      } else {
        chatEl = h('div', { className: 'lg-shell__chronicle lg-shell__chronicle--docked' }, chat);
      }
    }

    return h('div', { className: cx('lg-shellhost', p.className), style: p.height ? { height: p.height } : undefined },
      h('div', {
        ref: shellRef,
        className: cx('lg-shell', p.folio && 'has-folio', chat && 'has-chat', chat && 'is-chat-' + layout,
          chat && !chatOpen && 'is-chat-collapsed', railOpen && 'is-rail-open', p.backdrop && 'has-backdrop'),
        // The frame's backdrop (D-107): one picture behind the rail, the stage and the docked Chronicle, at Level 0.
        style: p.backdrop ? { '--lg-shell-backdrop': 'url("' + p.backdrop + '")' } : undefined
      },
        h('a', { className: 'lg-skip', href: '#' + mainId }, 'Skip to content'),
        p.rail ? h('div', { ref: railRef, className: 'lg-shell__rail' }, p.rail) : null,
        h('div', { className: 'lg-shell__scrim', 'aria-hidden': 'true', onClick: function () { setRailOpen(false); } }),
        h('main', { className: 'lg-shell__main', id: mainId, tabIndex: -1, inert: railOpen ? '' : undefined },
          top ? h('div', { className: 'lg-shell__top' }, top) : null,
          h('div', { className: 'lg-shell__stage' },
            p.children,
            p.hints ? h('div', { className: 'lg-shell__hints' }, p.hints) : null)),
        p.folio ? h('div', { className: 'lg-shell__folio', inert: railOpen ? '' : undefined }, p.folio) : null,
        chatEl ? R.cloneElement(chatEl, { inert: railOpen ? '' : undefined }) : null));
  }

  /* ---------- Activity (the current action, D-109) ---------- */
  // What the character is doing now, at the head of the NavRail: the action, the time left and its progress, and a way
  // to it. A button when it opens the action (onOpen). Compact (the compact rail): the bar and the time; the action stays
  // its accessible name and tooltip.
  function Activity(p) {
    var pct = p.max ? (p.value || 0) / p.max : (p.progress || 0);
    pct = Math.max(0, Math.min(1, pct));
    var interactive = typeof p.onOpen === 'function';
    var props = {
      type: interactive ? 'button' : undefined, onClick: interactive ? p.onOpen : undefined,
      className: cx('lg-activity', p.compact && 'lg-activity--compact', p.className),
      title: p.compact ? p.label : undefined
    };
    // Compact (D-117): the game's own mark — a ring the progress rises in, the ✦ and the live dot — over one short word;
    // the action and the time left stay its accessible name.
    if (p.compact) return h(interactive ? 'button' : 'div', props,
      h('span', { className: 'lg-activity__orb', 'aria-hidden': 'true' },
        h('span', { className: 'lg-activity__well' },
          h('span', { className: 'lg-activity__rise', style: { '--lg-activity-p': String(pct) } })),
        h('span', { className: 'lg-activity__glyph' }, '\u2726'),
        h('span', { className: 'lg-activity__live' })),
      h('span', { className: 'lg-activity__word', 'aria-hidden': 'true' }, p.short || p.label || 'Idle'),
      h('span', { className: 'lg-activity__label' }, p.label || 'Idle'),
      p.remaining ? h('span', { className: 'lg-activity__time' }, p.remaining) : null);
    return h(interactive ? 'button' : 'div', props,
      h('span', { className: 'lg-activity__head' },
        h('span', { className: 'lg-activity__label' }, p.label || 'Idle'),
        p.remaining ? h('span', { className: 'lg-activity__time' }, p.remaining) : null),
      h('span', { className: 'lg-activity__bar', 'aria-hidden': 'true' },
        h('span', { className: 'lg-activity__fill', style: { '--lg-activity-p': String(pct) } })),
      interactive && p.openLabel !== '' ? h('span', { className: 'lg-activity__open' }, p.openLabel || 'Go to action') : null);
  }

  /* ---------- Objective (the pinned quest in the TopBar's centre, D-110) ---------- */
  // The one "now" thing in the TopBar when no run is in progress: the pinned quest's title and current objective with its
  // count. With children it is a disclosure: the button opens them — the full tracker — in a Level 2 popover beneath it,
  // which Escape, a click outside or the button close.
  function Objective(p) {
    var id = useId('lgobj');
    var st = R.useState(false), open = p.open != null ? p.open : st[0];
    var btnRef = R.useRef(null), panelRef = R.useRef(null), byPointer = R.useRef(false);
    var hasPanel = p.children != null && p.children !== false;
    function set(v) { if (p.open == null) st[1](v); if (p.onToggle) p.onToggle(v); }
    useLayer(open && hasPanel, { kind: 'popover', opener: btnRef, restoreFocus: !byPointer.current, onClose: function () { set(false); } });
    R.useEffect(function () {
      if (!open) return;
      byPointer.current = false;
      function down(e) {
        var b = btnRef.current, pn = panelRef.current;
        if ((b && b.contains(e.target)) || (pn && pn.contains(e.target))) return;
        byPointer.current = true; set(false);
      }
      document.addEventListener('pointerdown', down);
      return function () { document.removeEventListener('pointerdown', down); };
    }, [open]);
    var count = p.required ? h('span', { className: 'lg-objective__count' }, fmt(p.current || 0) + NBSP + '/' + NBSP + fmt(p.required)) : null;
    var summary = [
      p.kicker ? h('span', { key: 'k', className: 'lg-objective__kicker' }, p.kicker) : null,
      h('span', { key: 't', className: 'lg-objective__title' }, p.title),
      p.objective ? h('span', { key: 'o', className: 'lg-objective__line' }, h('span', { className: 'lg-objective__text' }, p.objective), count) : null
    ];
    return h('div', { className: cx('lg-objective', open && hasPanel && 'is-open', p.className) },
      hasPanel ? h('button', { ref: btnRef, type: 'button', className: 'lg-objective__summary', 'aria-expanded': open, 'aria-controls': id,
        onClick: function () { set(!open); } }, summary)
        : h('div', { className: 'lg-objective__summary' }, summary),
      open && hasPanel ? h('div', { ref: panelRef, id: id, className: 'lg-objective__panel', role: 'region', 'aria-label': p.title }, p.children) : null);
  }

  /* ---------- Notice (D-111) ---------- */
  // A persistent notice at the head of the stage or a region: restricted access, progress being caught up, an error
  // with a way out. The title says what happened in words; the tone's bar backs it, never alone (Foundations · Colour).
  function Notice(p) {
    var tone = p.tone || 'info';
    return h('div', { className: cx('lg-notice', 'lg-notice--' + tone, p.className), role: tone === 'danger' ? 'alert' : 'status',
      'aria-busy': p.busy ? 'true' : undefined },
      p.busy ? h('span', { className: 'lg-notice__busy', role: 'progressbar', 'aria-label': p.busyLabel || 'In progress' }) : null,
      h('div', { className: 'lg-notice__body' },
        p.title ? h('p', { className: 'lg-notice__title' }, p.title) : null,
        p.children ? h('div', { className: 'lg-notice__text' }, p.children) : null),
      p.action ? h('div', { className: 'lg-notice__action' }, p.action) : null);
  }

  var api = {
    GameShell: GameShell, TopBar: TopBar, NavRail: NavRail, TabStrip: TabStrip, Stage: Stage, Folio: Folio, Panel: Panel,
    Sigil: Sigil, Constellation: Constellation, Meter: Meter, Track: Track, LevelPlate: LevelPlate, StatTile: StatTile, Delta: Delta,
    EntryList: EntryList, ItemSlot: ItemSlot, Tag: Tag, CurrencyPill: CurrencyPill,
    Button: Button, KeyHints: KeyHints, Heading: Heading, SectionRule: SectionRule, Emblem: Emblem, Icon: Icon,
    Chronicle: Chronicle, ItemLink: ItemLink, Key: Key,
    Page: Page, PageHeader: PageHeader, SearchField: SearchField, Banner: Banner, StatFigure: StatFigure,
    JourneyCard: JourneyCard, Ledger: Ledger, List: List, ListRow: ListRow, LoadoutSlot: LoadoutSlot, Presence: Presence, ProfileIdentity: ProfileIdentity, Activity: Activity, Objective: Objective, Notice: Notice, format: { number: fmt, short: short, range: range, times: times, fraction: fraction, percent: percent, unit: unit, parts: numParts, none: NONE, duration: duration, spokenDuration: spokenDuration }, announce: announce,
    states: STATES, topState: firstState, why: why, ornament: { audit: auditOrnament, budget: ORNAMENT_BUDGET },
    motion: { duration: MOTION.duration, easing: MOTION.easing, ms: motionMs, reduced: reducedMotion, useLive: useLive, useLiveList: useLiveList, audit: auditMotion, loopsAllowed: LOOPS_ALLOWED }, layers: { open: openLayer, use: useLayer, top: function () { var t = layerTop(); return t ? t.kind : null; } }, RARITY_CODES: RARITY_CODES
  };
  window.LL = Object.assign(window.LL || {}, api);
})();
