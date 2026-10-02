/*
 * The legacy rem rebase (Grimoire D-096; Foundations · Accessibility · Migrating the game).
 *
 * The root text size follows Grimoire: 100%, 115% and 130% (16px at Default) instead of the game's old 14, 16 and 18px.
 * The legacy styles — src/styles.css, src/styles/tokens.css, Tailwind's utilities and the components' own styles —
 * were written for a 14px root, so this plugin multiplies every rem they declare by 0.875 (14 / 16). Legacy screens keep
 * their size at Default, and come out a little larger than before at Large and Extra large, never smaller.
 *
 * Grimoire's styles are left alone: they are written for the 16px root. That is everything under src/app/grimoire/
 * (tokens, base, every part and the showcase), and the own styles of a screen that has moved to Grimoire, which lives
 * in a folder whose name ends in "-grimoire" (character-overview-grimoire/) until it replaces the legacy screen.
 * Media and container queries are left alone too: a rem there is the browser's 16px, not the root's, so it never
 * changed. Only declaration values are rewritten.
 *
 * Delete this plugin (and its line in postcss.config.json) when the last legacy screen has moved to Grimoire.
 */
const FACTOR = 0.875;
const SKIP = /[\\/]src[\\/]app[\\/]grimoire[\\/]|-grimoire[\\/][^\\/]+$/;
const REM = /(^|[^\w.-])(-?(?:\d+\.?\d*|\.\d+))rem\b/g;

function scale(n) {
  return String(+(parseFloat(n) * FACTOR).toFixed(5));
}

const plugin = () => ({
  postcssPlugin: 'postcss-legacy-rem',
  Once(root) {
    const file = (root.source && root.source.input && root.source.input.file) || '';
    if (SKIP.test(file)) return;
    root.walkDecls((decl) => {
      if (decl.value.includes('rem')) decl.value = decl.value.replace(REM, (m, pre, n) => pre + scale(n) + 'rem');
    });
  },
});
plugin.postcss = true;
plugin.FACTOR = FACTOR;
module.exports = plugin;
