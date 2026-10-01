/* React side of the parity harness: each case renders the reference component with the given props. */
window.CASES = function (h, L, D) {
  var noop = function () {};
  var B = function (t) { return h(L.Button, null, t); };
  return [
    ['icon-20', function () { return h(L.Icon, { name: 'overview' }); }],
    ['icon-title-16', function () { return h(L.Icon, { name: 'inventory', size: 16, title: 'Inventory' }); }],
    ['icon-unknown', function () { return h(L.Icon, { name: 'nope' }); }],
    ['heading-screen', function () { return h(L.Heading, { level: 'screen', sub: 'Ember' }, 'Wolf'); }],
    ['heading-section', function () { return h(L.Heading, { level: 'section' }, 'Stats'); }],
    ['key', function () { return h(L.Key, null, 'Esc'); }],
    ['keyhints', function () { return h(L.KeyHints, { hints: D.keyhints }); }],
    ['button-primary', function () { return h(L.Button, null, 'Level up'); }],
    ['button-solid', function () { return h(L.Button, { variant: 'solid', size: 'sm', hotkey: 'E', icon: 'inventory' }, 'Open'); }],
    ['button-locked', function () { return h(L.Button, { state: 'locked', reason: 'Unlocks at level 20' }, 'Ascend'); }],
    ['button-insufficient', function () { return h(L.Button, { state: 'insufficient', shortfall: D.shortfall }, 'Buy'); }],
    ['button-cooldown', function () { return h(L.Button, { state: 'cooldown', remaining: 252 }, 'Use'); }],
    ['button-pending', function () { return h(L.Button, { state: 'pending', pendingLabel: 'Claiming…' }, 'Claim'); }],
    ['button-pendinglabel', function () { return h(L.Button, { pendingLabel: 'Claiming…' }, 'Claim'); }],
    ['tag-tone', function () { return h(L.Tag, { tone: 'new' }, 'New quest'); }],
    ['tag-expiring', function () { return h(L.Tag, { state: 'expiring', value: '2h' }); }],
    ['tag-success', function () { return h(L.Tag, { tone: 'success' }, 'Saved'); }],
    ['tag-harmful', function () { return h(L.Tag, { tone: 'harmful' }, 'Bleeding'); }],
    ['tag-state-label', function () { return h(L.Tag, { state: 'assigned' }, 'In Preset 2'); }],
    ['delta-up', function () { return h(L.Delta, { direction: 'up', value: '12%', polarity: 'better' }); }],
    ['delta-none', function () { return h(L.Delta, { direction: 'none', value: '0', polarity: 'neutral' }); }],
    ['sigil-static', function () { return h(L.Sigil, { value: 24, label: 'Strength' }); }],
    ['sigil-left-ready', function () { return h(L.Sigil, { value: 18, label: 'Dex', labelPosition: 'left', size: 'sm', state: 'ready' }); }],
    ['sigil-selected', function () { return h(L.Sigil, { value: 9, label: 'Int', state: 'selected', onClick: noop }); }],
    ['sigil-locked', function () { return h(L.Sigil, { value: 9, label: 'Int', state: 'locked', reason: 'Unlocks at level 30', onClick: noop }); }],
    ['constellation', function () { var c = D.constellation; return h(L.Constellation, { items: c.items, rings: c.rings, nodes: c.nodes, selectedId: 'str', onSelect: noop }); }],
    ['meter', function () { return h(L.Meter, { value: 3120, max: 4150, label: 'Health', tone: 'hp' }); }],
    ['meter-bar', function () { return h(L.Meter, { value: 40, max: 120, unit: 'HP', size: 'bar', live: true, tone: 'sp', label: 'Stamina' }); }],
    ['meter-novalue', function () { return h(L.Meter, { value: 5, max: 10, showValue: false, ariaLabel: 'XP', tone: 'xp' }); }],
    ['track', function () { return h(L.Track, { steps: 5, current: 2, startLabel: 'First Hunt', endLabel: 'Journey complete' }); }],
    ['track-labels', function () { return h(L.Track, { steps: 4, current: 1, labels: D.stages, tone: 'arcana', label: 'Journey' }); }],
    ['levelplate', function () { return h(L.LevelPlate, { level: 17, xp: 8420, xpMax: 12000, xpUnit: 'Combat XP', aside: D.aside }); }],
    ['levelplate-plain', function () { return h(L.LevelPlate, { level: 3 }); }],
    ['stattile', function () { return h(L.StatTile, { label: 'Power', value: 1284 }); }],
    ['stattile-delta', function () { return h(L.StatTile, { label: 'Cooldown', value: 12, suffix: 's', delta: -2, deltaPolarity: 'better' }); }],
    ['stattile-delta0', function () { return h(L.StatTile, { label: 'Armor', value: 240, delta: 0 }); }],
    ['statfigure', function () { return h(L.StatFigure, { label: 'Combat Rating', value: 1284, caption: 'Permanent', title: 'All sources', size: 'sm' }); }],
    ['rule-band', function () { return h(L.SectionRule, { label: 'Status', aside: h('span', null, 'Live') }); }],
    ['rule-ornament', function () { return h(L.SectionRule, { variant: 'ornament', label: 'Lore' }); }],
    ['rule-hairline', function () { return h(L.SectionRule, { variant: 'hairline', align: 'end' }); }],
    ['panel', function () { return h(L.Panel, { title: 'Season standings', aside: h('span', null, 'All'), flush: true, density: 'compact' }, h('p', null, 'Body')); }],
    ['panel-notitle', function () { return h(L.Panel, null, h('p', null, 'Body')); }],
    ['emblem', function () { return h(L.Emblem, { points: 7, size: 120 }); }],
    ['folio', function () { return h(L.Folio, { eyebrow: 'Essence', title: 'Wolf', titleSub: 'Ember', lore: 'Fast and hungry.', effects: D.effects, rarity: 'Epic', cornerSrc: 'c.svg', actions: B('Attune') }); }],
    ['folio-min', function () { return h(L.Folio, { title: 'Details', align: 'start' }); }],
    ['entrylist', function () { return h(L.EntryList, { items: D.entries, activeId: 'wolf', density: 'compact', label: 'Creatures', onSelect: noop }); }],
    ['itemslot-basic', function () { return h(L.ItemSlot, { name: 'Ember Fang', rarity: 'Epic', icon: 'inventory', quantity: 3, meta: 'Sword · Lv 18' }); }],
    ['itemslot-empty', function () { return h(L.ItemSlot, { slotLabel: 'Off-hand' }); }],
    ['itemslot-button', function () { return h(L.ItemSlot, { name: 'Ember Fang', rarity: 'Rare', image: 'x.webp', selected: true, onClick: noop }); }],
    ['itemslot-locked-caption', function () { return h(L.ItemSlot, { name: 'Tower Key', state: 'locked', reason: 'Clear floor 10', onClick: noop }); }],
    ['itemslot-insufficient', function () { return h(L.ItemSlot, { icon: 'essences', caption: false, state: 'insufficient', shortfall: D.shortfall, onClick: noop }); }],
    ['itemslot-locked-static', function () { return h(L.ItemSlot, { icon: 'essences', caption: false, state: 'locked', reason: 'Clear floor 10' }); }],
    ['itemslot-equipped', function () { return h(L.ItemSlot, { name: 'Ember Fang', state: 'equipped', favourite: true }); }],
    ['itemslot-undiscovered', function () { return h(L.ItemSlot, { state: 'undiscovered', rarity: 'Epic', icon: 'inventory' }); }],
    ['list', function () {
      return h(L.List, { label: 'Inventory', rhythm: 'zebra' },
        h(L.ListRow, { title: 'Ember Fang', rarity: 'Epic', icon: 'inventory', quantity: 1, value: '9,400', onClick: noop, selected: true }),
        h(L.ListRow, { title: 'Iron Sabre', quantity: 2, value: 85, meta: 'Sword', trailing: B('Sell') }),
        h(L.ListRow, { title: 'Gone', muted: true }));
    }],
    ['listrow-alone', function () { return h(L.ListRow, { title: 'Solo', value: 12 }); }],
    ['currency', function () { return h(L.CurrencyPill, { name: 'Cinders', amount: 12480, iconSrc: 'Coins.svg' }); }],
    ['currency-short', function () { return h(L.CurrencyPill, { name: 'Cinders', amount: 12480, short: true }); }],
    ['currency-click', function () { return h(L.CurrencyPill, { name: 'Soulstones', amount: 36, onClick: noop }); }],
    ['navrail', function () { return h(L.NavRail, { sections: D.nav, activeId: 'overview', header: h('span', null, 'LL'), footer: h('span', null, 'v1') }); }],
    ['navrail-compact', function () { return h(L.NavRail, { sections: D.nav, activeId: 'inventory', compact: true }); }],
    ['tabstrip', function () { return h(L.TabStrip, { tabs: D.tabs, activeId: 'all', label: 'Filters', level: 'secondary', density: 'compact' }); }],
    ['stage', function () { return h(L.Stage, { image: 'bg.webp', focus: 'top', label: 'Tavern' }, h('h2', null, 'The Tavern')); }],
    ['stage-noart', function () { return h(L.Stage, { label: 'Empty' }); }],
    ['topbar', function () { return h(L.TopBar, { title: 'Aldric', eyebrow: 'Lv. 17', onMenu: noop, center: h('span', null, 'Track') }, h('span', null, 'Pills')); }],
    ['page', function () { return h(L.Page, { label: 'Overview', maxWidth: '60rem', role: 'region' }, h('p', null, 'Content')); }],
    ['pageheader', function () { return h(L.PageHeader, { icon: 'leaderboard', eyebrow: 'City', title: 'Leaderboard', summary: 'Top Legends' }, B('Refresh')); }],
    ['search', function () { return h(L.SearchField, { value: 'Mar', placeholder: 'Search character by name…', suggestions: [], onChange: noop }); }],
    ['banner', function () { return h(L.Banner, { image: 'bg.webp', label: 'Season', cornerSrc: 'c.svg', aside: h('span', null, 'Aside'), footer: h('span', null, 'Foot') }, h('h2', null, 'Season of Ash')); }],
    ['journey', function () { return h(L.JourneyCard, { phase: 'Shenic Journey', stages: D.stages, title: 'Develop your build', summary: 'Fight on.', objective: 'Choose a quest.', nextUnlock: 'A third slot', actions: B('Open map'), id: 'j1' }); }],
    ['journey-noid', function () { return h(L.JourneyCard, { phase: 'Tower', title: 'Climb' }); }],
    ['ledger', function () { return h(L.Ledger, { title: 'Offense', rows: D.ledger, density: 'compact' }); }],
    ['loadout-attuned', function () { return h(L.LoadoutSlot, { index: 0, name: 'Ember Wolf Essence', rarity: 'Rare', active: { name: 'Cinder Bite', cooldown: '12s' }, passive: { name: 'Pack Instinct' }, onClick: noop }); }],
    ['loadout-open', function () { return h(L.LoadoutSlot, { index: 1, hint: 'Choose an Essence' }); }],
    ['loadout-locked', function () { return h(L.LoadoutSlot, { index: 2, state: 'locked', reason: 'Unlocks at level 20', onClick: noop }); }],
    ['presence-online', function () { return h(L.Presence, { online: true }); }],
    ['presence-offline', function () { return h(L.Presence, { online: false, lastSeen: '3 h ago' }); }],
    ['presence-compact', function () { return h(L.Presence, { online: false, compact: true }); }],
    ['itemlink', function () { return h(L.ItemLink, { rarity: 'Epic', meta: 'Sword' }, 'Ember Fang'); }],
    ['itemlink-button', function () { return h(L.ItemLink, { onClick: noop }, 'Plain'); }],
    ['chronicle', function () { return h(L.Chronicle, { channels: D.channels, activeChannel: 'all', messages: D.messages, onToggle: noop, composer: { value: '', onChange: noop, onSend: noop, channel: 'general' } }); }],
    ['chronicle-collapsed', function () { return h(L.Chronicle, { channels: D.channels, messages: D.messages, open: false, onToggle: noop }); }],
    ['chronicle-trade', function () { return h(L.Chronicle, { channels: D.channels, activeChannel: 'trade', messages: D.messages, announce: 'mentions' }); }],
    ['gameshell', function () {
      return h(L.GameShell, {
        height: 600,
        rail: h(L.NavRail, { sections: D.nav, activeId: 'overview' }),
        top: h(L.TopBar, { title: 'Aldric' }),
        folio: h(L.Folio, { title: 'Wolf' }),
        hints: h(L.KeyHints, { hints: D.keyhints }),
        chronicle: h(L.Chronicle, { channels: D.channels, messages: D.messages, onToggle: noop })
      }, h(L.Page, { label: 'Overview' }, h('p', null, 'Content')));
    }],
    ['gameshell-floating', function () {
      return h(L.GameShell, { chatLayout: 'floating', chronicle: h(L.Chronicle, { channels: D.channels, messages: D.messages, onToggle: noop }) }, h('p', null, 'Stage'));
    }]
  ];
};

/* Interactive cases: stateful where the Angular component is two-way bound. */
window.ICASES = function (h, L, D) {
  var R = window.React;
  function count(k) { return function () { window.__count = window.__count || {}; window.__count[k] = (window.__count[k] || 0) + 1; }; }
  function Stateful(render, init) { return function () { var s = R.useState(init); return render(s[0], s[1]); }; }
  var names = ['Maren', 'Marek', 'Mara', 'Kaelen'];
  return [
    ['i-button-locked', function () { return h(L.Button, { state: 'locked', reason: 'Unlocks at level 20', onClick: count('locked') }, 'Ascend'); }],
    ['i-button-ok', function () { return h(L.Button, { onClick: count('ok') }, 'Go'); }],
    ['i-button-pending', function () { return h(L.Button, { state: 'pending', onClick: count('pending') }, 'Claim'); }],
    ['i-entrylist', function () { return h(Stateful(function (v, set) { return h(L.EntryList, { items: D.entries, activeId: v, onSelect: set }); }, 'wolf')); }],
    ['i-tabstrip', function () { return h(Stateful(function (v, set) { return h(L.TabStrip, { tabs: D.tabs, activeId: v, onChange: set }); }, 'all')); }],
    ['i-constellation', function () { var c = D.constellation; return h(Stateful(function (v, set) { return h(L.Constellation, { items: c.items, rings: c.rings, nodes: c.nodes, selectedId: v, onSelect: set }); }, 'str')); }],
    ['i-list', function () {
      return h(Stateful(function (v, set) {
        return h(L.List, { label: 'Inventory' },
          h(L.ListRow, { title: 'A', value: 1, selected: v === 'a', onClick: function () { set('a'); }, trailing: h(L.Button, { onClick: count('sellA') }, 'Sell') }),
          h(L.ListRow, { title: 'B', value: 2, selected: v === 'b', onClick: function () { set('b'); }, trailing: h(L.Button, { onClick: count('sellB') }, 'Sell') }),
          h(L.ListRow, { title: 'C', value: 3, selected: v === 'c', onClick: function () { set('c'); } }));
      }, 'b'));
    }],
    ['i-ledger', function () { return h(L.Ledger, { title: 'Offense', rows: D.ledger }); }],
    ['i-search', function () {
      return h(Stateful(function (v, set) {
        var sug = v ? names.filter(function (n) { return n.toLowerCase().indexOf(v.toLowerCase()) === 0; }) : [];
        return h(L.SearchField, { value: v, onChange: set, suggestions: sug, searched: true, onSelect: function (x) { set(x); count('pick')(); }, onSubmit: count('submit') });
      }, ''));
    }],
    ['i-currency', function () { return h(L.CurrencyPill, { name: 'Cinders', amount: 12480, short: true }); }],
    ['i-navrail', function () { return h(L.NavRail, { sections: D.nav, activeId: 'overview', onNavigate: count('nav') }); }],
    ['i-chronicle', function () {
      return h(Stateful(function (st, set) {
        return h(L.Chronicle, { channels: D.channels, messages: D.messages, activeChannel: st.ch, open: st.open,
          onChannelChange: function (c) { set({ ch: c, open: st.open }); }, onToggle: function () { set({ ch: st.ch, open: !st.open }); } });
      }, { ch: 'all', open: true }));
    }],
    ['i-itemslot-locked', function () { return h(L.ItemSlot, { name: 'Tower Key', state: 'locked', reason: 'Clear floor 10', onClick: count('slot') }); }],
    ['i-itemslot-nocaption', function () { return h(L.ItemSlot, { icon: 'essences', caption: false, state: 'cooldown', remaining: 3725, onClick: count('slot2') }); }],
    ['i-sigil-locked', function () { return h(L.Sigil, { value: 9, label: 'Int', state: 'locked', reason: 'Unlocks at level 30', onClick: count('sigil') }); }],
    ['i-loadout-locked', function () { return h(L.LoadoutSlot, { index: 2, state: 'locked', reason: 'Unlocks at level 20', onClick: count('loadout') }); }],
    ['i-shell-narrow', function () {
      return h('div', { style: { width: '800px' } }, h(L.GameShell, {
        height: 500,
        rail: h(L.NavRail, { sections: D.nav, activeId: 'overview' }),
        top: function (api) { return h(L.TopBar, { title: 'Aldric', onMenu: api.openRail }); }
      }, h('p', null, 'Stage')));
    }],
    ['i-shell-floating', function () {
      return h('div', { style: { width: '1200px' } }, h(L.GameShell, { height: 500, chatLayout: 'floating',
        chronicle: h(L.Chronicle, { channels: D.channels, messages: D.messages, onToggle: function () {} }) }, h('p', null, 'Stage')));
    }],
    ['i-chronicle-live', function () {
      return h(Stateful(function (msgs, set) {
        var more = function () { var n = msgs.length + 1; set(msgs.concat([{ id: 'n' + n, channel: 'general', author: 'Bot', text: 'Line ' + n }])); };
        return h('div', { style: { height: '260px', display: 'flex', flexDirection: 'column' } },
          h('button', { className: 'add', onClick: function () { more(); } }, 'add'),
          h(L.Chronicle, { channels: D.channels, messages: msgs, activeChannel: 'all' }));
      }, (function () { var m = []; for (var i = 1; i <= 30; i++) m.push({ id: 'n' + i, channel: 'general', author: 'Bot', text: 'Line ' + i }); return m; })()));
    }],
    ['i-livelist', function () {
      function LiveList() {
        var st = R.useState([{ id: 'a', v: 1 }, { id: 'b', v: 2 }, { id: 'c', v: 3 }]), items = st[0], set = st[1];
        var live = L.motion.useLiveList(items, { key: 'id', keep: 'b' });
        return h('div', null,
          h('button', { className: 'mut', onClick: function () { set([{ id: 'd', v: 9 }, { id: 'a', v: 10 }, { id: 'c', v: 30 }]); } }, 'mutate'),
          h('button', { className: 'rel', onClick: live.release }, 'release'),
          h('span', { className: 'pending' }, String(live.pending)),
          h('ul', { ref: live.ref, className: 'll' }, live.rows.map(function (r) {
            return h('li', { key: r.key, className: r.gone ? 'gone' : 'here' }, r.key + ':' + r.item.v);
          })));
      }
      return h(LiveList);
    }],
    ['i-layers', function () {
      function Layers() {
        var a = R.useState(false), b = R.useState(false), btn = R.useRef(null);
        L.layers.use(a[0], { kind: 'modal', onClose: function () { a[1](false); }, opener: btn });
        L.layers.use(b[0], { kind: 'popover', onClose: function () { b[1](false); } });
        return h('div', null,
          h('button', { ref: btn, className: 'open', onClick: function () { a[1](true); } }, 'open'),
          a[0] ? h('div', { className: 'dlg' }, h('button', { className: 'pop', onClick: function () { b[1](true); } }, 'pop'), b[0] ? h('span', { className: 'popover' }, 'popover') : null) : null);
      }
      return h(Layers);
    }],
    ['i-live', function () {
      return h(Stateful(function (v, set) {
        return h('div', null, h('button', { className: 'add', onClick: function () { set(v + 1); } }, 'add'),
          h(L.List, null, h(L.ListRow, { title: 'Price', value: v, live: true })));
      }, 100));
    }]
  ];
};
