import AxeBuilder from '@axe-core/playwright';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { expect, openInApp, readStories, settle, test } from './fixtures';

/**
 * Every story of the /grimoire showcase, compared with its baseline (one PNG per story, named after its address:
 * primitives/button/primary → primitives-button-primary.png), and every entry page checked with axe.
 */

test('every story matches its snapshot', async ({ showcase: page }) => {
  const stories = (await readStories(page)).filter((s) => s.snapshot);
  expect(stories.length, 'the showcase lists its stories').toBeGreaterThan(0);

  for (const story of stories) {
    await test.step(story.id, async () => {
      await openInApp(page, story.url);
      const frame = page.locator(
        `[data-story-frame][data-story="${story.id}"]`,
      );
      await frame.waitFor();
      await settle(page);
      await expect
        .soft(frame)
        .toHaveScreenshot(`${story.id.replace(/\//g, '-')}.png`);
    });
  }
});

interface KnownViolation {
  /** axe rule id, e.g. color-contrast */
  rule: string;
  /** The entry it was found on: primitives/button */
  entry: string;
  /** Why it is accepted for now, and which plan step fixes it. */
  reason: string;
}

const KNOWN_PATH = join(__dirname, 'axe-known.json');
const known: KnownViolation[] = JSON.parse(
  readFileSync(KNOWN_PATH, 'utf8'),
).violations;

test('no serious or critical accessibility violations beyond the known list', async ({
  showcase: page,
}) => {
  const entries = [
    ...new Set(
      (await readStories(page)).map((s) =>
        s.id.split('/').slice(0, 2).join('/'),
      ),
    ),
  ];
  const found: {
    entry: string;
    rule: string;
    impact: string;
    help: string;
    targets: string[];
  }[] = [];

  for (const entry of entries) {
    await test.step(entry, async () => {
      await openInApp(page, `/grimoire/${entry}`);
      await page.locator('[data-story-frame]').first().waitFor();
      await settle(page);
      const result = await new AxeBuilder({ page })
        .include('[data-story-frame]')
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
        .analyze();
      for (const v of result.violations) {
        if (v.impact !== 'serious' && v.impact !== 'critical') continue;
        found.push({
          entry,
          rule: v.id,
          impact: v.impact,
          help: v.help,
          targets: v.nodes.map((n) => n.target.join(' ')).slice(0, 5),
        });
      }
    });
  }

  // Kept beside the run's output folder (which each run empties) and attached to the HTML report.
  const report = join(__dirname, '../../test-results/grimoire-axe-report.json');
  mkdirSync(dirname(report), { recursive: true });
  writeFileSync(report, JSON.stringify(found, null, 2));
  await test.info().attach('axe-report.json', {
    body: JSON.stringify(found, null, 2),
    contentType: 'application/json',
  });

  const isKnown = (f: { entry: string; rule: string }) =>
    known.some((k) => k.entry === f.entry && k.rule === f.rule);
  const fresh = found.filter((f) => !isKnown(f));
  const fixed = known.filter(
    (k) => !found.some((f) => f.entry === k.entry && f.rule === k.rule),
  );
  if (fixed.length) {
    console.log(
      `axe: ${fixed.length} known violation(s) no longer occur; remove them from axe-known.json:`,
    );
    for (const k of fixed) console.log(`  ${k.entry}: ${k.rule}`);
  }
  expect(
    fresh,
    `new serious or critical violations (full list in ${report})`,
  ).toEqual([]);
});
