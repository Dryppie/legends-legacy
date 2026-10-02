/*
 * Test helpers for Grimoire's announcer (`LgAnnouncer` in grimoire-announcer.ts): what screen readers are told.
 *
 * The announcer speaks through the CDK's LiveAnnouncer: one live region on the body, each line written 100 ms after the
 * region is cleared. Polite lines go out at most every 1.5 s and the same text is not repeated within 5 s. Each test
 * gets its own announcer (TestBed makes a new root injector per test), but a line a test outside fakeAsync left
 * waiting can still be written while the next test starts. So:
 *   - in the describe, `beforeEach(lgAnnouncerIdle)`, so the live region is quiet when the test starts;
 *   - in the fakeAsync test, start with `lgQuietAnnouncer()`, so the clock is clear of every earlier line;
 *   - after the action, `tick(100)` before reading what was said;
 *   - end with `flush()`, so the queue's timer has run out.
 */
import { tick } from '@angular/core/testing';

/** Long enough that nothing an earlier test announced still counts as "just said". */
export const LG_ANNOUNCER_QUIET = 10 * 60 * 1000;

/** The CDK's live region: LiveAnnouncer writes every line into it. */
const LIVE_REGION = 'cdk-live-announcer-element';

/** Whether a mutation record touches the announcer's live region. */
function inAnnouncer(r: MutationRecord): boolean {
  const el = (
    r.target.nodeType === Node.TEXT_NODE ? r.target.parentElement : r.target
  ) as Element | null;
  return !!el?.classList?.contains(LIVE_REGION);
}

/** The text a mutation record writes into the live region, or ''. */
function writtenText(r: MutationRecord): string {
  // The text as written by this change, not as the region reads now.
  return r.type === 'characterData'
    ? (r.target as Text).data
    : Array.from(r.addedNodes)
        .map((n) => n.textContent ?? '')
        .join('');
}

/**
 * The lines a batch of mutation records writes (`polite: …`), in order. The one live region changes its politeness
 * line by line, so each line takes the politeness the region had when it was written, not the one it has now.
 */
function announcerLines(records: MutationRecord[]): string[] {
  const region = (r: MutationRecord) =>
    (r.target.nodeType === Node.TEXT_NODE
      ? r.target.parentElement
      : r.target) as Element;
  // The politeness after each change of it: the next change's old value, or the live attribute after the last one.
  const after = new Map<MutationRecord, string | null>();
  const first = new Map<Element, string | null>();
  for (let i = records.length - 1; i >= 0; i--) {
    const r = records[i];
    if (r.type !== 'attributes' || !inAnnouncer(r)) continue;
    const el = region(r);
    after.set(r, first.has(el) ? first.get(el)! : el.getAttribute('aria-live'));
    first.set(el, r.oldValue);
  }
  const now = new Map<Element, string | null>();
  const lines: string[] = [];
  for (const r of records) {
    if (!inAnnouncer(r)) continue;
    const el = region(r);
    if (!now.has(el))
      now.set(
        el,
        first.has(el) ? first.get(el)! : el.getAttribute('aria-live'),
      );
    if (r.type === 'attributes') {
      now.set(el, after.get(r) ?? null);
      continue;
    }
    const text = writtenText(r);
    if (text) lines.push(`${now.get(el)}: ${text}`);
  }
  return lines;
}

// When the announcer last wrote a line in real time. A line a test outside fakeAsync left waiting is written into the
// live region after that test ends, where the next test would read it as its own.
let lastRealLine = -Infinity;
// Any change to the region counts, the clearing too: a line is written 100 ms after the region is cleared, so a test
// that ends in between would otherwise leave a busy region unnoticed.
const realLines = new MutationObserver((records) => {
  if (records.some((r) => inAnnouncer(r))) lastRealLine = performance.now();
});
realLines.observe(document.body, {
  subtree: true,
  childList: true,
  characterData: true,
});

/**
 * Waits, in real time, until the announcer's queue has run out: 1.6 s after the last line written outside fakeAsync,
 * or at once when there was none. Use it before a fakeAsync test that reads announcements:
 * `beforeEach(lgAnnouncerIdle)`.
 */
export async function lgAnnouncerIdle(): Promise<void> {
  const pause = (ms: number) =>
    new Promise<void>((resolve) => setTimeout(resolve, ms));
  const left = () => 1600 - (performance.now() - lastRealLine);
  await pause(0); // lets the observer hear any line not yet reported
  while (left() > 0) await pause(left());
}

// The furthest fake time a test using these helpers reached.
let quietUntil = 0;

/**
 * Moves the fakeAsync clock past every line an earlier test announced. A plain `tick(LG_ANNOUNCER_QUIET)` is not
 * enough: fakeAsync restarts `Date.now()` at the real time in every test, so two tests that both tick it speak at
 * nearly the same fake time, and the second one's line is dropped as a repeat when the text is the same.
 */
export function lgQuietAnnouncer(): void {
  const target =
    Math.max(Date.now() + LG_ANNOUNCER_QUIET, quietUntil) + LG_ANNOUNCER_QUIET;
  tick(target - Date.now());
  quietUntil = target;
}

export interface LgAnnouncementWatch {
  /** Every line written, in order: `polite: Locked. Unlocks at level 20`. */
  readonly said: string[];
  stop(): void;
}

/** Records every line the announcer writes from now on. Call `stop()` at the end of the test. */
export function lgWatchAnnouncements(): LgAnnouncementWatch {
  const said: string[] = [];
  const read = (records: MutationRecord[]) =>
    said.push(...announcerLines(records));
  const observer = new MutationObserver(read);
  observer.observe(document.body, {
    subtree: true,
    childList: true,
    characterData: true,
    attributes: true,
    attributeFilter: ['aria-live'],
    attributeOldValue: true,
  });
  return {
    // A fakeAsync test never yields to the browser, so the observer's callback would not run before the test reads
    // `said`: take the pending records now.
    get said() {
      read(observer.takeRecords());
      return said;
    },
    stop: () => {
      read(observer.takeRecords());
      observer.disconnect();
      quietUntil = Math.max(quietUntil, Date.now());
      // This test's lines were written in fake time and its timers flushed: they keep no real queue running.
      realLines.takeRecords();
    },
  };
}

/** What the live region holds right now, under its politeness. */
export function lgAnnouncerText(): { polite: string; assertive: string } {
  const read = (p: string) =>
    document.querySelector(`.${LIVE_REGION}[aria-live="${p}"]`)?.textContent ??
    '';
  return { polite: read('polite'), assertive: read('assertive') };
}
