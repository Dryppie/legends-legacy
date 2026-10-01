#!/usr/bin/env node
// Runs the Grimoire parity check in a headless browser: the static comparison (every case renders the same DOM in the
// React reference and the lg-* port) and the behaviour scenarios below (the same clicks and keys leave the same DOM,
// focus, reason tip, handler calls, top layer and announcements on both sides).
//
// From LL/src/Presentation/ll/design-system/parity:
//   npx ng build                     build the parity page into ../../dist/grimoire-parity
//   node check-parity.mjs            run the checks (needs the playwright package; see README.md)
//   node check-parity.mjs --static   only the static comparison
//
// Exit code 1 when anything differs.

import { createServer } from 'node:http';
import { existsSync, readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, extname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const dist = join(here, '..', '..', 'dist', 'grimoire-parity', 'browser');
if (!existsSync(join(dist, 'index.html'))) {
  console.error('Build the parity page first: npx ng build (in design-system/parity).');
  process.exit(1);
}
const require = createRequire(import.meta.url);
let chromium;
try {
  ({ chromium } = require(process.env.PLAYWRIGHT_MODULE || 'playwright'));
} catch {
  console.error('The playwright package is not installed. See design-system/parity/README.md.');
  process.exit(1);
}

const TYPES = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.woff2': 'font/woff2', '.svg': 'image/svg+xml' };
const server = createServer((req, res) => {
  const path = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  const file = join(dist, path === '/' ? 'index.html' : path);
  if (!file.startsWith(dist) || !existsSync(file)) {
    res.writeHead(404);
    res.end();
    return;
  }
  res.writeHead(200, { 'content-type': TYPES[extname(file)] || 'application/octet-stream' });
  res.end(readFileSync(file));
});
await new Promise((r) => server.listen(0, r));
const base = `http://localhost:${server.address().port}/`;

// Each step: [action, selector-in-case | key | ms]. 'snap' records the state.
const SCENARIOS = {
  'i-button-locked': [['hover', 'button'], ['snap'], ['click', 'button'], ['wait', 250], ['snap'], ['click', 'button'], ['snap']],
  'i-button-ok': [['click', 'button'], ['snap']],
  'i-button-pending': [['click', 'button'], ['snap']],
  'i-entrylist': [['click', 'li:nth-child(2)'], ['snap'], ['key', 'ArrowDown'], ['wait', 50], ['snap'], ['key', 'ArrowDown'], ['snap'], ['key', 'Home'], ['snap'], ['key', 'ArrowUp'], ['snap'], ['key', 'ArrowUp'], ['key', 'Enter'], ['wait', 250], ['snap']],
  'i-tabstrip': [['click', 'button:nth-child(2)'], ['snap'], ['key', 'ArrowRight'], ['snap'], ['key', 'ArrowRight'], ['snap']],
  'i-constellation': [['click', '.lg-constellation__node:nth-of-type(2) button'], ['snap'], ['key', 'ArrowRight'], ['snap'], ['key', 'Enter'], ['wait', 250], ['snap'], ['key', 'Home'], ['snap']],
  'i-list': [['click', 'li:nth-child(1) .lg-listrow__hit'], ['snap'], ['key', 'ArrowDown'], ['snap'], ['key', 'ArrowRight'], ['snap'], ['key', 'ArrowLeft'], ['snap'], ['key', 'End'], ['snap'], ['key', 'Enter'], ['snap']],
  'i-ledger': [['click', '.lg-ledger__row:nth-child(1)'], ['snap'], ['key', 'Escape'], ['snap'], ['key', 'ArrowDown'], ['snap'], ['key', 'Escape'], ['snap'], ['click', '.lg-ledger__row:nth-child(3)'], ['snap'], ['clickOutside'], ['snap']],
  'i-search': [['click', 'input'], ['type', 'Ma'], ['wait', 50], ['snap'], ['key', 'ArrowDown'], ['key', 'ArrowDown'], ['snap'], ['key', 'Enter'], ['wait', 50], ['snap'], ['type', 'x'], ['wait', 50], ['snap'], ['key', 'Escape'], ['snap'], ['key', 'Enter'], ['snap']],
  'i-currency': [['click', 'button'], ['snap'], ['click', 'button'], ['snap']],
  'i-navrail': [['click', 'li:nth-child(2) a'], ['snap'], ['click', 'li:nth-child(3) a'], ['wait', 250], ['snap'], ['key', 'Escape'], ['snap']],
  'i-chronicle': [['click', '[role=tab]:nth-child(2)'], ['snap'], ['key', 'ArrowRight'], ['snap'], ['click', '.lg-chronicle__toggle'], ['snap'], ['click', '.lg-chronicle__ticker'], ['snap']],
  'i-itemslot-locked': [['click', 'button'], ['wait', 250], ['snap'], ['hover', 'button'], ['snap']],
  'i-itemslot-nocaption': [['hover', 'button'], ['snap'], ['click', 'button'], ['wait', 250], ['snap']],
  'i-sigil-locked': [['click', 'button'], ['wait', 250], ['snap']],
  'i-loadout-locked': [['click', 'button'], ['wait', 250], ['snap']],
  'i-shell-narrow': [['click', '.lg-topbar__menu'], ['wait', 100], ['snap'], ['key', 'Escape'], ['wait', 100], ['snap']],
  'i-shell-floating': [['click', '.lg-chronicle__grip'], ['key', 'ArrowRight'], ['key', 'ArrowUp'], ['snap'], ['click', '.lg-chronicle__tall'], ['snap']],
  'i-chronicle-live': [['wait', 100], ['snap'], ['scrollTop', '.lg-chronicle__log'], ['wait', 50], ['click', 'button.add'], ['click', 'button.add'], ['wait', 100], ['snap'], ['click', '.lg-chronicle__jump'], ['wait', 100], ['snap']],
  'i-livelist': [['hover', 'ul.ll'], ['wait', 50], ['jsclick', 'button.mut'], ['wait', 50], ['snap'], ['mouseAway'], ['wait', 100], ['snap'], ['click', 'button.rel'], ['wait', 50], ['snap']],
  'i-layers': [['click', 'button.open'], ['snap'], ['click', 'button.pop'], ['snap'], ['key', 'Escape'], ['snap'], ['key', 'Escape'], ['wait', 50], ['snap']],
  'i-live': [['click', 'button.add'], ['wait', 50], ['snap'], ['wait', 1100], ['snap']],
};
// The announcer speaks at most every 1.5s and drops stale lines; a pause between scenarios lets it finish.
const GAP = 4600;

async function side(browser, which) {
  const page = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const errors = [];
  page.on('pageerror', (e) => errors.push(e.message));
  await page.goto(base + '?only=' + which);
  await page.waitForFunction(() => window.__parityReady, null, { timeout: 20000 });
  await page.evaluate(() => {
    window.__said = [];
    new MutationObserver((ms) =>
      ms.forEach((m) => {
        const el = m.target.nodeType === 3 ? m.target.parentElement : m.target;
        if (el?.classList?.contains('lg-announcer') && el.textContent) window.__said.push(el.getAttribute('aria-live') + ': ' + el.textContent);
      }),
    ).observe(document.body, { subtree: true, childList: true, characterData: true });
  });
  const root = which === 'react' ? '#react' : '#ng';
  const out = {};
  for (const [name, steps] of Object.entries(SCENARIOS)) {
    const sel = `${root} [data-case="${name}"]`;
    const snaps = [];
    await page.mouse.move(1, 1);
    await page.waitForTimeout(GAP);
    for (const [act, arg] of steps) {
      const loc = typeof arg === 'string' ? page.locator(sel + ' ' + arg).first() : null;
      if (act === 'hover') await loc.hover({ force: true });
      else if (act === 'click') await loc.click({ force: true });
      else if (act === 'key') await page.keyboard.press(arg);
      else if (act === 'type') await page.keyboard.type(arg);
      else if (act === 'wait') await page.waitForTimeout(arg);
      else if (act === 'clickOutside') await page.mouse.click(2, 2);
      else if (act === 'mouseAway') await page.mouse.move(1, 1);
      else if (act === 'jsclick') await loc.evaluate((el) => el.click());
      else if (act === 'scrollTop') await loc.evaluate((el) => { el.scrollTop = 0; el.dispatchEvent(new Event('scroll')); });
      else if (act === 'snap') {
        await page.waitForTimeout(60);
        snaps.push(
          await page.evaluate(([name, root]) => {
            const box = document.querySelector(root);
            const a = document.activeElement;
            const why = [...document.querySelectorAll('.lg-why')].map((w) => [...w.classList].sort().join(' ') + ' | ' + w.textContent).join(' / ');
            const log = document.querySelector(`${root} [data-case="${name}"] .lg-chronicle__log`);
            return {
              tree: window.lgParityNormalize(box)[name].tree,
              active: a ? a.tagName.toLowerCase() + '.' + [...a.classList].sort().join('.') + ' "' + (a.textContent || a.value || '').replace(/\s+/g, ' ').trim().slice(0, 30) + '"' : null,
              why: why || 'none',
              count: JSON.stringify(window.__count || {}),
              scroll: log ? (log.scrollHeight - log.scrollTop - log.clientHeight < 2 ? 'foot' : 'top:' + log.scrollTop) : '',
              layer: String(root === '#react' ? window.LL.layers.top() : window.__ngTopLayer()),
            };
          }, [name, root]),
        );
      }
    }
    out[name] = snaps;
  }
  await page.waitForTimeout(GAP);
  const said = await page.evaluate(() => window.__said);
  await page.close();
  return { out, errors, said };
}

const browser = await chromium.launch();
let failed = 0;
try {
  const page = await browser.newPage({ viewport: { width: 1400, height: 1000 } });
  await page.goto(base);
  const stat = await page.waitForFunction(() => window.__parity, null, { timeout: 30000 }).then((h) => h.jsonValue());
  console.log(`Static: ${stat.total - stat.failed.length} of ${stat.total} cases match.`);
  for (const f of stat.failed) console.log(`\n${f.name}\n${f.diff}`);
  failed += stat.failed.length;
  await page.close();

  if (!process.argv.includes('--static')) {
    const R = await side(browser, 'react');
    const A = await side(browser, 'angular');
    let same = 0;
    for (const name of Object.keys(SCENARIOS)) {
      const diffs = [];
      R.out[name].forEach((r, i) => {
        for (const f of ['tree', 'active', 'why', 'count', 'scroll', 'layer'])
          if (r[f] !== A.out[name][i][f]) diffs.push(`  step ${i} ${f}\n    React:   ${r[f]}\n    Angular: ${A.out[name][i][f]}`);
      });
      if (diffs.length) {
        failed++;
        console.log(`\n${name}\n${diffs.join('\n')}`);
      } else same++;
    }
    console.log(`Behaviour: ${same} of ${Object.keys(SCENARIOS).length} scenarios match.`);
    const saidSame = JSON.stringify(R.said) === JSON.stringify(A.said);
    if (!saidSame) {
      failed++;
      console.log('Announcements differ.\n  React:   ' + JSON.stringify(R.said) + '\n  Angular: ' + JSON.stringify(A.said));
    } else console.log(`Announcements: the same ${R.said.length} lines on both sides.`);
    if (R.errors.length || A.errors.length) {
      failed++;
      console.log('Page errors:', R.errors, A.errors);
    }
  }
} finally {
  await browser.close();
  server.close();
}
process.exit(failed ? 1 : 0);
