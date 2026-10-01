import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, computed, signal, viewChild } from '@angular/core';
import { LG_FORMAT, LG_STATES, lgTopState, LgStateName, LgLayerHandle, lgOpenLayer, lgTopLayer, LG_GRIMOIRE, LgEntry, LgLedgerRow, LgNavSection, LgChronicleMessage, LgFolioEffect, LgLevelPlateStat, LgConstellationItem } from '../../src/app/shared/components/grimoire';
import data from './data.json';

interface ParityWindow {
  React: { createElement: unknown };
  ReactDOM: { flushSync(fn: () => void): void; createRoot(el: Element): { render(node: unknown): void } };
  LL: unknown;
  CASES(h: unknown, ll: unknown, d: unknown): [string, () => unknown][];
  ICASES(h: unknown, ll: unknown, d: unknown): [string, () => unknown][];
  lgParityNormalize(root: Element): Record<string, { tree: string; text: string }>;
  __parity?: unknown;
  __parityReady?: boolean;
}

@Component({
  selector: 'parity-root',
  imports: [...LG_GRIMOIRE],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="report">
      <h1>Grimoire parity: React reference and lg-* port</h1>
      @if (results(); as r) {
        <p [class]="r.failed.length ? 'bad' : 'good'">
          {{ r.total - r.failed.length }} of {{ r.total }} cases render the same DOM in React and Angular.
        </p>
        @for (f of r.failed; track f.name) {
          <details open>
            <summary>{{ f.name }}</summary>
            <pre>{{ f.diff }}</pre>
          </details>
        }
      } @else {
        <p>Comparing…</p>
      }
    </header>
    <section class="side">
      <h2>Angular</h2>
      <div id="ng">
        @if (only !== 'react') {

    <div class="case" data-case="icon-20"><lg-icon name="overview" /></div>
    <div class="case" data-case="icon-title-16"><lg-icon name="inventory" [size]="16" title="Inventory" /></div>
    <div class="case" data-case="icon-unknown"><lg-icon [name]="$any('nope')" /></div>
    <div class="case" data-case="heading-screen"><h1 lgHeading="screen" sub="Ember">Wolf</h1></div>
    <div class="case" data-case="heading-section"><h3 lgHeading="section">Stats</h3></div>
    <div class="case" data-case="key"><lg-key>Esc</lg-key></div>
    <div class="case" data-case="keyhints"><lg-key-hints [hints]="d.keyhints" /></div>
    <div class="case" data-case="button-primary"><button lgButton>Level up</button></div>
    <div class="case" data-case="button-solid"><button lgButton="solid" size="sm" hotkey="E" icon="inventory">Open</button></div>
    <div class="case" data-case="button-locked"><button lgButton state="locked" reason="Unlocks at level 20">Ascend</button></div>
    <div class="case" data-case="button-insufficient"><button lgButton state="insufficient" [shortfall]="d.shortfall">Buy</button></div>
    <div class="case" data-case="button-cooldown"><button lgButton state="cooldown" [remaining]="252">Use</button></div>
    <div class="case" data-case="button-pending"><button lgButton state="pending" pendingLabel="Claiming…">Claim</button></div>
    <div class="case" data-case="button-pendinglabel"><button lgButton pendingLabel="Claiming…">Claim</button></div>
    <div class="case" data-case="tag-tone"><lg-tag tone="new">New quest</lg-tag></div>
    <div class="case" data-case="tag-expiring"><lg-tag state="expiring" value="2h" /></div>
    <div class="case" data-case="tag-success"><lg-tag tone="success">Saved</lg-tag></div>
    <div class="case" data-case="tag-harmful"><lg-tag tone="harmful">Bleeding</lg-tag></div>
    <div class="case" data-case="tag-state-label"><lg-tag state="assigned" label="In Preset 2" /></div>
    <div class="case" data-case="delta-up"><lg-delta direction="up" value="12%" polarity="better" /></div>
    <div class="case" data-case="delta-none"><lg-delta direction="none" value="0" polarity="neutral" /></div>
    <div class="case" data-case="sigil-static"><lg-sigil [value]="24" label="Strength" /></div>
    <div class="case" data-case="sigil-left-ready"><lg-sigil [value]="18" label="Dex" labelPosition="left" size="sm" state="ready" /></div>
    <div class="case" data-case="sigil-selected"><lg-sigil [value]="9" label="Int" state="selected" interactive /></div>
    <div class="case" data-case="sigil-locked"><lg-sigil [value]="9" label="Int" state="locked" reason="Unlocks at level 30" interactive /></div>
    <div class="case" data-case="constellation">
      <lg-constellation [items]="items" [rings]="d.constellation.rings" [nodes]="d.constellation.nodes" selectedId="str" />
    </div>
    <div class="case" data-case="meter"><lg-meter [value]="3120" [max]="4150" label="Health" tone="hp" /></div>
    <div class="case" data-case="meter-bar"><lg-meter [value]="40" [max]="120" unit="HP" size="bar" live tone="sp" label="Stamina" /></div>
    <div class="case" data-case="meter-novalue"><lg-meter [value]="5" [max]="10" [showValue]="false" ariaLabel="XP" tone="xp" /></div>
    <div class="case" data-case="track"><lg-track [steps]="5" [current]="2" startLabel="First Hunt" endLabel="Journey complete" /></div>
    <div class="case" data-case="track-labels"><lg-track [steps]="4" [current]="1" [labels]="d.stages" tone="arcana" label="Journey" /></div>
    <div class="case" data-case="levelplate"><lg-level-plate [level]="17" [xp]="8420" [xpMax]="12000" xpUnit="Combat XP" [aside]="aside" /></div>
    <div class="case" data-case="levelplate-plain"><lg-level-plate [level]="3" /></div>
    <div class="case" data-case="stattile"><lg-stat-tile label="Power" [value]="1284" /></div>
    <div class="case" data-case="stattile-delta"><lg-stat-tile label="Cooldown" [value]="12" suffix="s" [delta]="-2" deltaPolarity="better" /></div>
    <div class="case" data-case="stattile-delta0"><lg-stat-tile label="Armor" [value]="240" [delta]="0" /></div>
    <div class="case" data-case="statfigure"><lg-stat-figure label="Combat Rating" [value]="1284" caption="Permanent" title="All sources" size="sm" /></div>
    <div class="case" data-case="rule-band"><lg-section-rule label="Status"><span lgSlot="aside">Live</span></lg-section-rule></div>
    <div class="case" data-case="rule-ornament"><lg-section-rule variant="ornament" label="Lore" /></div>
    <div class="case" data-case="rule-hairline"><lg-section-rule variant="hairline" align="end" /></div>
    <div class="case" data-case="panel">
      <lg-panel title="Season standings" flush density="compact"><span lgSlot="aside">All</span><p>Body</p></lg-panel>
    </div>
    <div class="case" data-case="panel-notitle"><lg-panel><p>Body</p></lg-panel></div>
    <div class="case" data-case="emblem"><lg-emblem [points]="7" [size]="120" /></div>
    <div class="case" data-case="folio">
      <lg-folio eyebrow="Essence" title="Wolf" titleSub="Ember" lore="Fast and hungry." [effects]="effects" rarity="Epic" cornerSrc="c.svg">
        <button lgButton lgSlot="actions">Attune</button>
      </lg-folio>
    </div>
    <div class="case" data-case="folio-min"><lg-folio title="Details" align="start" /></div>
    <div class="case" data-case="entrylist"><lg-entry-list [items]="entries" activeId="wolf" density="compact" label="Creatures" /></div>
    <div class="case" data-case="itemslot-basic"><lg-item-slot name="Ember Fang" rarity="Epic" icon="inventory" [quantity]="3" meta="Sword · Lv 18" /></div>
    <div class="case" data-case="itemslot-empty"><lg-item-slot slotLabel="Off-hand" /></div>
    <div class="case" data-case="itemslot-button"><lg-item-slot name="Ember Fang" rarity="Rare" image="x.webp" selected interactive /></div>
    <div class="case" data-case="itemslot-locked-caption"><lg-item-slot name="Tower Key" state="locked" reason="Clear floor 10" interactive /></div>
    <div class="case" data-case="itemslot-insufficient">
      <lg-item-slot icon="essences" [caption]="false" state="insufficient" [shortfall]="d.shortfall" interactive />
    </div>
    <div class="case" data-case="itemslot-locked-static"><lg-item-slot icon="essences" [caption]="false" state="locked" reason="Clear floor 10" /></div>
    <div class="case" data-case="itemslot-equipped"><lg-item-slot name="Ember Fang" state="equipped" favourite /></div>
    <div class="case" data-case="itemslot-undiscovered"><lg-item-slot state="undiscovered" rarity="Epic" icon="inventory" /></div>
    <div class="case" data-case="list">
      <lg-list label="Inventory" rhythm="zebra">
        <li lgListRow title="Ember Fang" rarity="Epic" icon="inventory" [quantity]="1" value="9,400" interactive [selected]="true"></li>
        <li lgListRow title="Iron Sabre" [quantity]="2" [value]="85" meta="Sword"><button lgButton lgSlot="trailing">Sell</button></li>
        <li lgListRow title="Gone" muted></li>
      </lg-list>
    </div>
    <div class="case" data-case="listrow-alone"><li lgListRow title="Solo" [value]="12"></li></div>
    <div class="case" data-case="currency"><lg-currency-pill name="Cinders" [amount]="12480" iconSrc="Coins.svg" /></div>
    <div class="case" data-case="currency-short"><lg-currency-pill name="Cinders" [amount]="12480" short /></div>
    <div class="case" data-case="currency-click"><lg-currency-pill name="Soulstones" [amount]="36" interactive /></div>
    <div class="case" data-case="navrail">
      <lg-nav-rail [sections]="nav" activeId="overview"><span lgSlot="header">LL</span><span lgSlot="footer">v1</span></lg-nav-rail>
    </div>
    <div class="case" data-case="navrail-compact"><lg-nav-rail [sections]="nav" activeId="inventory" compact /></div>
    <div class="case" data-case="tabstrip"><lg-tab-strip [tabs]="d.tabs" activeId="all" label="Filters" level="secondary" density="compact" /></div>
    <div class="case" data-case="stage"><lg-stage image="bg.webp" focus="top" label="Tavern"><h2>The Tavern</h2></lg-stage></div>
    <div class="case" data-case="stage-noart"><lg-stage label="Empty" /></div>
    <div class="case" data-case="topbar">
      <lg-top-bar title="Aldric" eyebrow="Lv. 17" showMenu><span lgSlot="center">Track</span><span>Pills</span></lg-top-bar>
    </div>
    <div class="case" data-case="page"><lg-page label="Overview" maxWidth="60rem" role="region"><p>Content</p></lg-page></div>
    <div class="case" data-case="pageheader">
      <lg-page-header icon="leaderboard" eyebrow="City" title="Leaderboard" summary="Top Legends"><button lgButton lgSlot="actions">Refresh</button></lg-page-header>
    </div>
    <div class="case" data-case="search"><lg-search-field value="Mar" placeholder="Search character by name…" [suggestions]="[]" /></div>
    <div class="case" data-case="banner">
      <lg-banner image="bg.webp" label="Season" cornerSrc="c.svg"><span lgSlot="aside">Aside</span><span lgSlot="footer">Foot</span><h2>Season of Ash</h2></lg-banner>
    </div>
    <div class="case" data-case="journey">
      <lg-journey-card phase="Shenic Journey" [stages]="d.stages" title="Develop your build" summary="Fight on." objective="Choose a quest." nextUnlock="A third slot" id="j1">
        <button lgButton lgSlot="actions">Open map</button>
      </lg-journey-card>
    </div>
    <div class="case" data-case="journey-noid"><lg-journey-card phase="Tower" title="Climb" /></div>
    <div class="case" data-case="ledger"><lg-ledger title="Offense" [rows]="ledger" density="compact" /></div>
    <div class="case" data-case="loadout-attuned">
      <lg-loadout-slot [index]="0" name="Ember Wolf Essence" rarity="Rare" [active]="{ name: 'Cinder Bite', cooldown: '12s' }" [passive]="{ name: 'Pack Instinct' }" interactive />
    </div>
    <div class="case" data-case="loadout-open"><lg-loadout-slot [index]="1" hint="Choose an Essence" /></div>
    <div class="case" data-case="loadout-locked"><lg-loadout-slot [index]="2" state="locked" reason="Unlocks at level 20" interactive /></div>
    <div class="case" data-case="presence-online"><lg-presence [online]="true" /></div>
    <div class="case" data-case="presence-offline"><lg-presence [online]="false" lastSeen="3 h ago" /></div>
    <div class="case" data-case="presence-compact"><lg-presence [online]="false" compact /></div>
    <div class="case" data-case="itemlink"><lg-item-link rarity="Epic" meta="Sword">Ember Fang</lg-item-link></div>
    <div class="case" data-case="itemlink-button"><lg-item-link interactive>Plain</lg-item-link></div>
    <div class="case" data-case="chronicle">
      <lg-chronicle [channels]="d.channels" activeChannel="all" [messages]="messages" composerChannel="general" />
    </div>
    <div class="case" data-case="chronicle-collapsed">
      <lg-chronicle [channels]="d.channels" [messages]="messages" [open]="false" [composer]="false" />
    </div>
    <div class="case" data-case="chronicle-trade">
      <lg-chronicle [channels]="d.channels" activeChannel="trade" [messages]="messages" announce="mentions" [toggleable]="false" [composer]="false" />
    </div>
    <div class="case" data-case="gameshell">
      <lg-game-shell [height]="600">
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="overview" />
        <lg-top-bar lgSlot="top" title="Aldric" />
        <lg-folio lgSlot="folio" title="Wolf" />
        <lg-key-hints lgSlot="hints" [hints]="d.keyhints" />
        <lg-chronicle lgSlot="chronicle" [channels]="d.channels" [messages]="messages" [composer]="false" />
        <lg-page label="Overview"><p>Content</p></lg-page>
      </lg-game-shell>
    </div>
    <div class="case" data-case="gameshell-floating">
      <lg-game-shell chatLayout="floating">
        <lg-chronicle lgSlot="chronicle" [channels]="d.channels" [messages]="messages" [composer]="false" />
        <p>Stage</p>
      </lg-game-shell>
    </div>
    <div class="case" data-case="i-button-locked"><button lgButton state="locked" reason="Unlocks at level 20" (click)="count('locked')">Ascend</button></div>
    <div class="case" data-case="i-button-ok"><button lgButton (click)="count('ok')">Go</button></div>
    <div class="case" data-case="i-button-pending"><button lgButton state="pending" (click)="count('pending')">Claim</button></div>
    <div class="case" data-case="i-entrylist"><lg-entry-list [items]="entries" [(activeId)]="entry" /></div>
    <div class="case" data-case="i-tabstrip"><lg-tab-strip [tabs]="d.tabs" [(activeId)]="tab" /></div>
    <div class="case" data-case="i-constellation">
      <lg-constellation [items]="items" [rings]="d.constellation.rings" [nodes]="d.constellation.nodes" [selectedId]="star()" (select)="star.set($event)" />
    </div>
    <div class="case" data-case="i-list">
      <lg-list label="Inventory">
        <li lgListRow title="A" [value]="1" interactive [selected]="row() === 'a'" (activate)="row.set('a')"><button lgButton lgSlot="trailing" (click)="count('sellA')">Sell</button></li>
        <li lgListRow title="B" [value]="2" interactive [selected]="row() === 'b'" (activate)="row.set('b')"><button lgButton lgSlot="trailing" (click)="count('sellB')">Sell</button></li>
        <li lgListRow title="C" [value]="3" interactive [selected]="row() === 'c'" (activate)="row.set('c')"></li>
      </lg-list>
    </div>
    <div class="case" data-case="i-ledger"><lg-ledger title="Offense" [rows]="ledger" /></div>
    <div class="case" data-case="i-search">
      <lg-search-field [(value)]="query" [suggestions]="suggestions()" searched (pick)="count('pick')" (submitted)="count('submit')" />
    </div>
    <div class="case" data-case="i-currency"><lg-currency-pill name="Cinders" [amount]="12480" short /></div>
    <div class="case" data-case="i-navrail"><lg-nav-rail [sections]="nav" activeId="overview" (navigate)="count('nav')" /></div>
    <div class="case" data-case="i-chronicle"><lg-chronicle [channels]="d.channels" [messages]="messages" [(activeChannel)]="channel" [(open)]="open" [composer]="false" /></div>
    <div class="case" data-case="i-itemslot-locked"><lg-item-slot name="Tower Key" state="locked" reason="Clear floor 10" interactive (activate)="count('slot')" /></div>
    <div class="case" data-case="i-itemslot-nocaption">
      <lg-item-slot icon="essences" [caption]="false" state="cooldown" [remaining]="3725" interactive (activate)="count('slot2')" />
    </div>
    <div class="case" data-case="i-sigil-locked"><lg-sigil [value]="9" label="Int" state="locked" reason="Unlocks at level 30" interactive (activate)="count('sigil')" /></div>
    <div class="case" data-case="i-loadout-locked"><lg-loadout-slot [index]="2" state="locked" reason="Unlocks at level 20" interactive (activate)="count('loadout')" /></div>
    <div class="case" data-case="i-shell-narrow">
      <div style="width: 800px">
        <lg-game-shell [height]="500">
          <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="overview" />
          <lg-top-bar lgSlot="top" title="Aldric" showMenu />
          <p>Stage</p>
        </lg-game-shell>
      </div>
    </div>
    <div class="case" data-case="i-shell-floating">
      <div style="width: 1200px">
        <lg-game-shell [height]="500" chatLayout="floating">
          <lg-chronicle lgSlot="chronicle" [channels]="d.channels" [messages]="messages" [composer]="false" />
          <p>Stage</p>
        </lg-game-shell>
      </div>
    </div>
    <div class="case" data-case="i-chronicle-live">
      <div style="height: 260px; display: flex; flex-direction: column">
        <button class="add" (click)="addLine()">add</button>
        <lg-chronicle [channels]="d.channels" [messages]="lines()" activeChannel="all" [toggleable]="false" [composer]="false" />
      </div>
    </div>
    <div class="case" data-case="i-livelist">
      <div>
        <button class="mut" (click)="liveItems.set([{ id: 'd', v: 9 }, { id: 'a', v: 10 }, { id: 'c', v: 30 }])">mutate</button>
        <button class="rel" (click)="live.release()">release</button>
        <span class="pending">{{ live.pending() }}</span>
        <ul class="ll" [lgLiveList]="liveItems()" [key]="'id'" [keep]="'b'" #live="lgLiveList">
          @for (r of live.rows(); track r.key) {
            <li [class]="r.gone ? 'gone' : 'here'">{{ r.key }}:{{ r.item.v }}</li>
          }
        </ul>
      </div>
    </div>
    <div class="case" data-case="i-layers">
      <div>
        <button #opener class="open" (click)="openModal(opener)">open</button>
        @if (modal()) {
          <div class="dlg">
            <button class="pop" (click)="openPopover()">pop</button>
            @if (popover()) {
              <span class="popover">popover</span>
            }
          </div>
        }
      </div>
    </div>
    <div class="case" data-case="i-live">
      <div>
        <button class="add" (click)="price.set(price() + 1)">add</button>
        <lg-list><li lgListRow title="Price" [value]="price()" live></li></lg-list>
      </div>
    </div>
        }
      </div>
    </section>
    <section class="side">
      <h2>React reference</h2>
      <div id="react" #react></div>
    </section>
  `,
})
export class ParityComponent {
  /** ?only=react or ?only=angular renders one side, for the behaviour checks. */
  protected readonly only = new URLSearchParams(location.search).get('only');
  protected readonly results = signal<{ total: number; failed: { name: string; diff: string }[] } | null>(null);
  private readonly reactRoot = viewChild.required<ElementRef<HTMLElement>>('react');

  constructor() {
    // The behaviour checks read the top layer from both sides: LL.layers.top() and this.
    (window as unknown as { __ngTopLayer: typeof lgTopLayer }).__ngTopLayer = lgTopLayer;
    afterNextRender(() => setTimeout(() => this.compare(), 300));
  }

  /** LL.format, LL.states and LL.topState against their Angular mirrors. */
  private compareHelpers(w: ParityWindow): { total: number; failed: { name: string; diff: string }[] } {
    const LL = w.LL as {
      format: Record<string, (...a: unknown[]) => unknown> & { none: string };
      states: Record<string, unknown>;
      topState(s: string[]): string | null;
    };
    const ng = LG_FORMAT as unknown as Record<string, (...a: unknown[]) => unknown> & { none: string };
    const nums: unknown[] = [0, 1, -1, 12, 999, 1000, 9999, 10000, 12480, 99999, 100000, 1234567, 12345678, 1e9, 2.5e10, -12480, 0.5, -0.004, 3.14159, NaN, Infinity, null, undefined, '', '-12', '84 HP/5s', 'abc'];
    const calls: [string, unknown[]][] = [];
    for (const n of nums) {
      calls.push(['number', [n]], ['number', [n, 2]], ['short', [n]], ['times', [n, 1]], ['percent', [n, 1]], ['unit', [n, 'HP/5s']], ['unit', [n, 's']]);
      calls.push(['fraction', [n, 4150]], ['duration', [n]], ['spokenDuration', [n]]);
    }
    for (const t of ['184.6 threat/s', '24.8%', '12–18', '7\u00A0/\u00A020', '−12', '+3', '×1.5', '—', '1,284', 'Reaper', 12.5, -3])
      calls.push(['parts', [t]]);
    calls.push(['range', [12, 18]], ['range', [1.5, 2.25, 1]], ['duration', [59]], ['duration', [3600]], ['duration', [90061]], ['duration', [3725]]);
    const failed: { name: string; diff: string }[] = [];
    for (const [fn, args] of calls) {
      const r = JSON.stringify(LL.format[fn](...args));
      const a = JSON.stringify(ng[fn](...args));
      if (r !== a) failed.push({ name: `format.${fn}(${args.map((x) => JSON.stringify(x)).join(', ')})`, diff: `React:   ${r}\nAngular: ${a}` });
    }
    if (LL.format.none !== ng.none) failed.push({ name: 'format.none', diff: '' });
    const rs = JSON.stringify(LL.states);
    const as = JSON.stringify(LG_STATES);
    if (rs !== as) failed.push({ name: 'states', diff: `React:   ${rs}\nAngular: ${as}` });
    const lists: string[][] = [[], ['new', 'locked'], ['equipped', 'claimable', 'new'], ['opened', 'claimed'], ['favourite', 'selected'], ['expired', 'in-progress', 'attuned']];
    for (const l of lists) {
      const r = LL.topState(l);
      const a = lgTopState(l as LgStateName[]);
      if (r !== a) failed.push({ name: `topState(${l.join(',')})`, diff: `React: ${r}\nAngular: ${a}` });
    }
    return { total: calls.length + 2 + lists.length, failed };
  }

  private compare(): void {
    const w = window as unknown as ParityWindow;
    const root = this.reactRoot().nativeElement;
    if (this.only !== 'angular') {
      const cases = [...w.CASES(w.React.createElement, w.LL, data), ...w.ICASES(w.React.createElement, w.LL, data)];
      for (const [name, render] of cases) {
        const div = document.createElement('div');
        div.className = 'case';
        div.setAttribute('data-case', name);
        root.appendChild(div);
        w.ReactDOM.flushSync(() => w.ReactDOM.createRoot(div).render(render()));
      }
    }
    if (this.only) {
      w.__parityReady = true;
      return;
    }
    const R = w.lgParityNormalize(root);
    const A = w.lgParityNormalize(document.getElementById('ng')!);
    const failed: { name: string; diff: string }[] = [];
    for (const name of Object.keys(R)) {
      const r = R[name];
      const a = A[name];
      if (!a) {
        failed.push({ name, diff: 'No Angular case.' });
        continue;
      }
      if (r.tree === a.tree && r.text === a.text) continue;
      const rl = r.tree.split('\n');
      const al = a.tree.split('\n');
      const lines: string[] = [];
      if (r.text !== a.text) lines.push('text React:   ' + r.text, 'text Angular: ' + a.text);
      for (let i = 0; i < Math.max(rl.length, al.length) && lines.length < 40; i++) {
        if (rl[i] !== al[i]) lines.push('React   ' + i + ': ' + (rl[i] ?? '—'), 'Angular ' + i + ': ' + (al[i] ?? '—'));
      }
      failed.push({ name, diff: lines.join('\n') });
    }
    for (const name of Object.keys(A)) if (!R[name]) failed.push({ name, diff: 'No React case.' });
    const helpers = this.compareHelpers(w);
    failed.push(...helpers.failed);
    const result = { total: Object.keys(R).length + helpers.total, failed };
    this.results.set(result);
    w.__parity = result;
  }

  protected readonly price = signal(100);
  protected readonly liveItems = signal([{ id: 'a', v: 1 }, { id: 'b', v: 2 }, { id: 'c', v: 3 }]);
  protected readonly modal = signal(false);
  protected readonly popover = signal(false);
  // Like the React case: each layer is registered while its flag is set, and closing one returns focus to its opener.
  private readonly layers: Partial<Record<'modal' | 'popover', LgLayerHandle>> = {};
  protected openModal(opener: HTMLElement): void {
    this.modal.set(true);
    this.layers.modal = lgOpenLayer({ kind: 'modal', opener, onClose: () => this.closeLayer('modal') });
  }
  protected openPopover(): void {
    this.popover.set(true);
    this.layers.popover = lgOpenLayer({ kind: 'popover', onClose: () => this.closeLayer('popover') });
  }
  private closeLayer(kind: 'modal' | 'popover'): void {
    (kind === 'modal' ? this.modal : this.popover).set(false);
    this.layers[kind]?.close();
    delete this.layers[kind];
  }
  protected readonly lines = signal<LgChronicleMessage[]>(
    Array.from({ length: 30 }, (_, i) => ({ id: 'n' + (i + 1), channel: 'general', author: 'Bot', text: 'Line ' + (i + 1) })),
  );
  protected addLine(): void {
    const n = this.lines().length + 1;
    this.lines.set([...this.lines(), { id: 'n' + n, channel: 'general', author: 'Bot', text: 'Line ' + n }]);
  }
  protected readonly entry = signal('wolf');
  protected readonly tab = signal('all');
  protected readonly star = signal('str');
  protected readonly row = signal('b');
  protected readonly query = signal('');
  protected readonly channel = signal('all');
  protected readonly open = signal(true);
  private readonly names = ['Maren', 'Marek', 'Mara', 'Kaelen'];
  protected readonly suggestions = computed(() => {
    const v = this.query();
    return v ? this.names.filter((n) => n.toLowerCase().startsWith(v.toLowerCase())) : [];
  });
  protected count(k: string): void {
    const w = window as unknown as { __count?: Record<string, number> };
    w.__count = w.__count || {};
    w.__count[k] = (w.__count[k] || 0) + 1;
  }
  protected readonly d = data;
  protected readonly items = data.constellation.items as LgConstellationItem[];
  protected readonly entries = data.entries as LgEntry[];
  protected readonly ledger = data.ledger as LgLedgerRow[];
  protected readonly nav = data.nav as unknown as LgNavSection[];
  protected readonly messages = data.messages as LgChronicleMessage[];
  protected readonly effects = data.effects as (string | LgFolioEffect)[];
  protected readonly aside = data.aside as LgLevelPlateStat[];
}
