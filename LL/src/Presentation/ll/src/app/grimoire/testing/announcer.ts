/*
 * Test helpers for Grimoire's announcer (lgAnnounce in grimoire-a11y.ts): what screen readers are told.
 *
 * The announcer is one module-level queue. Polite lines go out at most every 1.5 s, the same text is not repeated
 * within 5 s, and each line is written 60 ms after its region is cleared. So:
 *   - in the describe, `beforeEach(lgAnnouncerIdle)`, so a line a test outside fakeAsync left in the queue is out;
 *   - in the fakeAsync test, start with `lgQuietAnnouncer()`, so a line an earlier test spoke is outside the 5 s window;
 *   - after the action, `tick(100)` before reading what was said;
 *   - end with `flush()`, so the queue's timer has run out and the next test starts with an idle announcer.
 */
import { tick } from '@angular/core/testing';

/** Long enough that nothing an earlier test announced still counts as "just said". */
export const LG_ANNOUNCER_QUIET = 10 * 60 * 1000;

/** Whether a mutation record touches one of the announcer's live regions. */
function inAnnouncer(r: MutationRecord): boolean {
  const el = (
    r.target.nodeType === Node.TEXT_NODE ? r.target.parentElement : r.target
  ) as Element | null;
  return !!el?.classList?.contains('lg-announcer');
}

/** The line a mutation record writes to the announcer (`polite: …`), or null. */
function announcerLine(r: MutationRecord): string | null {
  const el = (
    r.target.nodeType === Node.TEXT_NODE ? r.target.parentElement : r.target
  ) as Element | null;
  if (!el?.classList?.contains('lg-announcer')) return null;
  // The text as written by this change, not as the region reads now.
  const text =
    r.type === 'characterData'
      ? (r.target as Text).data
      : Array.from(r.addedNodes)
          .map((n) => n.textContent ?? '')
          .join('');
  return text ? `${el.getAttribute('aria-live')}: ${text}` : null;
}

// When the announcer last wrote a line in real time. A test outside fakeAsync that announces leaves the queue's real
// 1.5 s timer running into the next tests, and while it runs a fakeAsync test's line only waits in the queue.
let lastRealLine = -Infinity;
// Any change to a region counts, the clearing too: a line is written 60 ms after its region is cleared, so a test that
// ends in between would otherwise leave a busy announcer unnoticed.
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
    records.forEach((r) => {
      const line = announcerLine(r);
      if (line) said.push(line);
    });
  const observer = new MutationObserver(read);
  observer.observe(document.body, {
    subtree: true,
    childList: true,
    characterData: true,
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

/** What each live region holds right now. */
export function lgAnnouncerText(): { polite: string; assertive: string } {
  const read = (p: string) =>
    document.querySelector(`.lg-announcer[aria-live="${p}"]`)?.textContent ??
    '';
  return { polite: read('polite'), assertive: read('assertive') };
}
