// Legend's Legacy — generated from design-system/tokens.json by design-system/scripts/build-tokens.mjs. Do not edit:
// change tokens.json and run the script. The same values are --lg-* custom properties in tokens.css.

/** Motion durations in milliseconds (Foundations · Motion). Under reduced motion every transition ends at once. */
export const LG_DURATION = {
  instant: 0,
  fast: 140,
  base: 220,
  slow: 400,
  reveal: 600,
} as const;

/** Easing curves, as CSS timing functions (Foundations · Motion). */
export const LG_EASING = {
  standard: 'cubic-bezier(0.4, 0, 0.2, 1)',
  enter: 'cubic-bezier(0, 0, 0.2, 1)',
  exit: 'cubic-bezier(0.4, 0, 1, 1)',
} as const;

/**
 * The shell's container breakpoints (Foundations · Layout · Shell breakpoints). rem, so they move with the reading-size
 * setting: compare them with a container's width in rem, not with the window in px.
 */
export const LG_BREAKPOINT = {
  wide: '96rem',
  shell: '60rem',
  narrow: '40rem',
  reflow: '30rem',
  topcenter: '16rem',
  strip: '7.5rem',
} as const;

/** Where a region's Narrow, Medium and Wide content tiers start (Foundations · Layout, D-050). */
export const LG_CONTENT_TIER = {
  wide: '68rem',
  medium: '44rem',
  narrow: '32rem',
} as const;

/** The layer stack, bottom to top (Foundations · Surfaces & Layering). A scrim sits one below the layer it serves. */
export const LG_LAYER = {
  content: 0,
  raised: 1,
  sticky: 10,
  chrome: 20,
  folio: 30,
  chatFloat: 40,
  overlay: 50,
  popover: 100,
  modal: 200,
  confirm: 250,
  popoverDetached: 300,
  toast: 400,
  tour: 500,
  drag: 600,
} as const;
