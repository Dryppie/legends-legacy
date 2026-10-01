/*
 * The Grimoire stylesheet (tokens.css and components.css, generated from design-system/ by sync-styles.mjs) is built
 * as its own bundle, grimoire.css, outside the app's first load (D-096). lgLoadStyles adds it to the page without
 * blocking the first render; main.ts calls it at startup, so it has arrived before a Grimoire screen opens. It
 * comes after the app's own styles, so it wins over Tailwind's preflight as it did when it was injected.
 *
 * Import it from this file, not from index.ts, so the startup bundle doesn't pull in the components.
 */

/** The bundle's file name, set by `bundleName` in angular.json. */
export const LG_STYLESHEET = 'grimoire.css';

/**
 * Adds the Grimoire stylesheet to the page once; safe to call again. The bundle's name carries no hash, so pass the
 * build version (APP_VERSION) to keep a browser from reusing the previous release's copy.
 */
export function lgLoadStyles(version?: string, doc: Document = document): void {
  if (doc.querySelector('link[data-lg-styles]')) return;
  const link = doc.createElement('link');
  link.rel = 'stylesheet';
  link.href = version ? LG_STYLESHEET + '?v=' + encodeURIComponent(version) : LG_STYLESHEET;
  link.setAttribute('data-lg-styles', '');
  doc.head.appendChild(link);
}
