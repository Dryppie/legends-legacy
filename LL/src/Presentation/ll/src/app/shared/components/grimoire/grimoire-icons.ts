/**
 * The game's sidebar icon set (src/assets/icons/sidebar), with the baked gold
 * gradient removed so every icon draws in currentColor.
 */
export const LG_ICONS = {
  'achievements': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<circle cx="12" cy="9" r="4.5"/><path d="M12 6.6l.9 1.7 1.9.3-1.4 1.3.3 1.9-1.7-.9-1.7.9.3-1.9-1.4-1.3 1.9-.3z" stroke-width="1.2"/><path d="M9 13l-2 7 5-2.6L17 20l-2-7"/>' },
  'combat-styles': { viewBox: '0 0 24 24', strokeWidth: 1.75, body: '<path d="M9.4 3.6a8.6 8.6 0 0 0 0 16.8"/><path d="M14.6 3.6a8.6 8.6 0 0 1 0 16.8"/><path d="M12 10.4v3.2"/>' },
  'essences': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M12 3l5.5 4-2 9.5h-7L6.5 7z"/><path d="M6.5 7L12 10.5 17.5 7"/><path d="M12 10.5v6"/><path d="M12 19.5v1.5"/>' },
  'inventory': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M4 9h16v10a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 19z"/><path d="M8 9V6.5A2.5 2.5 0 0 1 10.5 4h3A2.5 2.5 0 0 1 16 6.5V9"/><path d="M4 13h16"/><path d="M10.5 13v2h3v-2"/>' },
  'overview': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M12 3l7 3v5c0 4.5-3 7.5-7 10-4-2.5-7-5.5-7-10V6z"/><circle cx="12" cy="10" r="2.4"/><path d="M8.5 16c.8-1.6 2-2.4 3.5-2.4s2.7.8 3.5 2.4"/>' },
  'soulstones': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M12 3L5 9l7 12 7-12z"/><path d="M5 9h14"/><path d="M12 3L9.5 9 12 21l2.5-12z"/>' },
  'cinder-bazaar': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M12 4v3"/><path d="M4 8l2.5 5h-5L4 8zM20 8l2.5 5h-5L20 8z"/><path d="M4 8h16"/><path d="M1.5 13a2.5 2.5 0 0 0 5 0M17.5 13a2.5 2.5 0 0 0 5 0"/><path d="M12 7v12"/><path d="M8 19h8"/>' },
  'colosseum': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M5 4l11 11"/><path d="M19 4L8 15"/><path d="M4.5 15.5l4 4M19.5 15.5l-4 4"/><path d="M6 17l-2.5 2.5M18 17l2.5 2.5M7.5 20L5 22M16.5 20l2.5 2"/>' },
  'guild': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M6 21V8l-2-2V4h4v2h2V4h4v2h2V4h4v2l-2 2v13"/><path d="M10 21v-5a2 2 0 0 1 4 0v5"/><path d="M4 21h16"/>' },
  'leaderboard': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M4 8l4 3 4-6 4 6 4-3-1.5 9h-13z"/><path d="M6 20h12"/>' },
  'settings': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<circle cx="12" cy="12" r="3"/><path d="M12 2.5v3M12 18.5v3M2.5 12h3M18.5 12h3M5.3 5.3l2.1 2.1M16.6 16.6l2.1 2.1M18.7 5.3l-2.1 2.1M7.4 16.6l-2.1 2.1"/>' },
  'legacy-ascension': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M6 21h12"/><path d="M8 21V9h8v12"/><path d="M7 9V5h2v2h2V5h2v2h2V5h2v4"/><path d="M10.5 21v-4a1.5 1.5 0 0 1 3 0v4"/><path d="M12 2v3"/>' },
  'prophecies': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M19 14.5A7.5 7.5 0 0 1 9.5 5 7.5 7.5 0 1 0 19 14.5z"/><path d="M16.5 4.5l.5 1.5 1.5.5-1.5.5-.5 1.5-.5-1.5L14.5 6l1.5-.5z" stroke-width="1.2"/>' },
  'quest-journal': { viewBox: '1 1 22 22', strokeWidth: 1.6, body: '<path d="M12 7.2v12"/><path d="M12 7.2C10.2 5.6 7.6 4.9 4.5 5.2a1 1 0 0 0-.9 1v10.5a1 1 0 0 0 1.1 1c2.7-.2 5 .4 7.3 1.5"/><path d="M12 7.2c1.8-1.6 4.4-2.3 7.5-2a1 1 0 0 1 .9 1v10.5a1 1 0 0 1-1.1 1c-2.7-.2-5 .4-7.3 1.5"/><path d="M12 4.2 12.9 6l1.8.5-1.8.5-.9 1.8-.9-1.8-1.8-.5L11.1 6z" fill="currentColor" stroke="none" opacity=".85"/>' },
  'world-map': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M9 4L4 6v14l5-2 6 2 5-2V4l-5 2z"/><path d="M9 4v14M15 6v14"/><circle cx="12" cy="10" r="1" fill="currentColor" stroke="none"/>' },
  'nobility': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M3.2 17.6 2.6 7.4l5.3 4.4L12 4l4.1 7.8 5.3-4.4-.6 10.2z" fill="currentColor" stroke="none"/><path d="M3.6 20.2h16.8" stroke-width="2.2" stroke-linecap="butt"/>' },
  // The solid 12px Locked marker (Foundations · Iconography · Inventory): a closed padlock.
  'lock': { viewBox: '0 0 24 24', strokeWidth: 1.6, body: '<path d="M7.5 11V8.2a4.5 4.5 0 0 1 9 0V11" fill="none" stroke-width="2.4" stroke-linecap="butt"/><rect x="4.5" y="11" width="15" height="10" rx="1.6" fill="currentColor" stroke="none"/>' },
} as const;

export type LgIconName = keyof typeof LG_ICONS;

export const LG_ICON_NAMES = Object.keys(LG_ICONS) as LgIconName[];
