import { Page, test as base } from '@playwright/test';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/** Every run sees the same moment, so nothing that prints a time or an age changes between runs. */
export const FIXED_NOW = new Date('2026-10-02T12:00:00Z');

const FONTS_DIR = join(__dirname, 'fonts');
const FONTS_CSS = readFileSync(join(FONTS_DIR, 'fonts.css'), 'utf8');

/** The API and chat hosts in src/assets/env.js. The showcase needs neither; their calls are answered here. */
const API = 'http://localhost:7050/';
const CHAT = 'http://localhost:5179/';

/**
 * Makes a page independent of the network and the clock:
 * - the game's API: `timesync` answers with FIXED_NOW (the app waits for it at startup); everything else is 401, so
 *   the app starts signed out and opens no realtime connection;
 * - chat: refused;
 * - Google Fonts: answered from ./fonts, so the letters are the same with or without a network;
 * - any other host: refused. The dev server's own address is untouched.
 */
export async function isolate(page: Page, baseURL: string): Promise<void> {
  await page.clock.setFixedTime(FIXED_NOW);
  const own = new URL(baseURL).origin;
  await page.route('**/*', async (route) => {
    const url = route.request().url();
    if (url.startsWith(own)) return route.continue();
    if (url.startsWith(API)) {
      if (url.includes('/timesync')) {
        return route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: String(FIXED_NOW.getTime()),
        });
      }
      return route.fulfill({
        status: 401,
        contentType: 'application/json',
        body: '{}',
      });
    }
    if (url.startsWith('https://fonts.googleapis.com/')) {
      const grimoireFaces = /Marcellus|Barlow|Garamond/.test(
        decodeURIComponent(url),
      );
      return route.fulfill({
        status: 200,
        contentType: 'text/css',
        body: grimoireFaces ? FONTS_CSS : '',
      });
    }
    if (url.startsWith('https://fonts.gstatic.com/grimoire-fixture/')) {
      const file = url.split('/').pop() ?? '';
      if (/^[a-z-]+-latin-\d{3}-(normal|italic)\.woff2$/.test(file)) {
        return route.fulfill({
          status: 200,
          contentType: 'font/woff2',
          body: readFileSync(join(FONTS_DIR, file)),
        });
      }
    }
    if (url.startsWith(CHAT)) return route.abort();
    return route.abort();
  });
}

/** Waits until the story frames on the page have their fonts, images and background art. */
export async function settle(page: Page): Promise<void> {
  await page.evaluate(async () => {
    await document.fonts.ready;
    const frames = Array.from(
      document.querySelectorAll<HTMLElement>('[data-story-frame]'),
    );
    const urls = new Set<string>();
    for (const frame of frames) {
      for (const el of [
        frame,
        ...Array.from(frame.querySelectorAll<HTMLElement>('*')),
      ]) {
        const bg = getComputedStyle(el).backgroundImage;
        for (const m of bg.matchAll(/url\("?(.*?)"?\)/g)) urls.add(m[1]);
      }
      for (const img of Array.from(frame.querySelectorAll('img')))
        if (img.currentSrc) urls.add(img.currentSrc);
    }
    await Promise.all(
      [...urls].map(
        (src) =>
          new Promise<void>((done) => {
            const img = new Image();
            img.onload = img.onerror = () => done();
            img.src = src;
          }),
      ),
    );
    await new Promise((r) =>
      requestAnimationFrame(() => requestAnimationFrame(r)),
    );
  });
}

export interface StoryLink {
  url: string;
  /** `primitives/button/primary` */
  id: string;
  snapshot: boolean;
}

/** Opens the showcase's first page and reads every story's address from it. */
export async function readStories(page: Page): Promise<StoryLink[]> {
  await page.goto('/grimoire');
  await page.locator('[data-showcase-ready]').waitFor();
  return page.$$eval('a[data-story-link]', (links) =>
    links.map((a) => {
      const url = new URL((a as HTMLAnchorElement).href).pathname;
      return {
        url,
        id: url.replace(/^\/grimoire\//, ''),
        snapshot: a.getAttribute('data-snapshot') !== 'false',
      };
    }),
  );
}

/**
 * Moves to another showcase address inside the running app (the router, not a reload), which keeps the run short.
 */
export async function openInApp(page: Page, url: string): Promise<void> {
  await page.evaluate((u) => {
    history.pushState(null, '', u);
    dispatchEvent(new PopStateEvent('popstate', { state: null }));
  }, url);
}

export const test = base.extend<{ showcase: Page }>({
  showcase: async ({ page, baseURL }, use) => {
    await isolate(page, baseURL ?? 'http://localhost:4300');
    await use(page);
  },
});

export { expect } from '@playwright/test';
