/*
 * The state model (Standards · States). Mirrors LL.states, LL.topState and the blocked-reason helpers in
 * design-system/components/bundle.js; keep the two in step.
 */
import {
  LG_NBSP,
  LG_NONE,
  lgFormatDuration,
  lgFormatNumber,
  lgSpokenDuration,
} from './grimoire-format';

export type LgStateFamily =
  | 'interaction'
  | 'availability'
  | 'lifecycle'
  | 'ownership'
  | 'knowledge'
  | 'data';

export type LgTagTone =
  | 'neutral'
  | 'new'
  | 'gilt'
  | 'danger'
  | 'warning'
  | 'success'
  | 'beneficial'
  | 'harmful'
  | 'locked'
  | 'common'
  | 'uncommon'
  | 'rare'
  | 'epic'
  | 'unique'
  | 'legendary'
  | 'legacy';

export type LgStateName =
  | 'default' | 'hover' | 'focus-visible' | 'pressed' | 'selected' | 'current' | 'dragging' | 'disabled'
  | 'available' | 'ready' | 'unavailable' | 'locked' | 'restricted' | 'insufficient' | 'cooldown'
  | 'new' | 'unread' | 'in-progress' | 'completed' | 'claimable' | 'claimed' | 'opened' | 'expiring' | 'expired' | 'failed'
  | 'owned' | 'not-owned' | 'equipped' | 'attuned' | 'assigned' | 'captured' | 'listed' | 'escrow' | 'borrowed' | 'favourite'
  | 'discovered' | 'undiscovered' | 'hidden' | 'unknown'
  | 'loading' | 'refreshing' | 'pending' | 'stale' | 'error' | 'empty' | 'offline';

/** A blocked state: it stops a press, and the part must say why. */
export type LgBlockedState = 'unavailable' | 'locked' | 'restricted' | 'insufficient' | 'cooldown';

export interface LgStateInfo {
  family: LgStateFamily;
  /** The word the state shows, or null when it has none. */
  word: string | null;
  tone: LgTagTone | null;
  /** What screen readers hear after a name. */
  sr: string | null;
  glyph?: string;
  blocks?: boolean;
}

function st(
  family: LgStateFamily,
  word: string | null,
  tone: LgTagTone | null = null,
  sr: string | null = null,
  extra: Partial<LgStateInfo> = {},
): LgStateInfo {
  return { family, word, tone, sr, ...extra };
}

/** Every state: its family, word, Tag tone and glyph, and what screen readers hear. */
export const LG_STATES: Readonly<Record<LgStateName, LgStateInfo>> = {
  default: st('interaction', null),
  hover: st('interaction', null),
  'focus-visible': st('interaction', null),
  pressed: st('interaction', null, null, 'pressed'),
  selected: st('interaction', null, null, 'selected'),
  current: st('interaction', null, null, 'current'),
  dragging: st('interaction', null, null, 'grabbed'),
  disabled: st('interaction', null, null, 'unavailable'),
  available: st('availability', null),
  ready: st('availability', 'Ready', 'new', 'ready'),
  unavailable: st('availability', null, null, 'unavailable', { blocks: true }),
  locked: st('availability', 'Locked', 'locked', 'locked', { blocks: true }),
  restricted: st('availability', 'Restricted', null, 'unavailable', { blocks: true }),
  insufficient: st('availability', 'Short by', 'warning', 'unavailable', { blocks: true }),
  cooldown: st('availability', 'Ready in', null, 'unavailable', { blocks: true }),
  new: st('lifecycle', 'New', 'new', 'new', { glyph: '+' }),
  unread: st('lifecycle', 'Unread', 'new', 'unread'),
  'in-progress': st('lifecycle', 'In progress', null, 'in progress'),
  completed: st('lifecycle', 'Completed', 'success', 'completed'),
  claimable: st('lifecycle', 'Claimable', 'new', 'claimable'),
  claimed: st('lifecycle', 'Claimed', null, 'claimed'),
  opened: st('lifecycle', 'Opened', null, 'opened'),
  expiring: st('lifecycle', 'Expires in', 'warning', 'expires in'),
  expired: st('lifecycle', 'Expired', null, 'expired'),
  failed: st('lifecycle', 'Failed', 'danger', 'failed', { glyph: '✕' }),
  owned: st('ownership', 'Owned', null, 'owned'),
  'not-owned': st('ownership', 'Not owned', null, 'not owned'),
  equipped: st('ownership', 'Equipped', null, 'equipped'),
  attuned: st('ownership', 'Attuned', null, 'attuned'),
  assigned: st('ownership', 'Assigned', null, 'assigned'),
  captured: st('ownership', 'In snapshot', null, 'in snapshot'),
  listed: st('ownership', 'Listed', null, 'listed'),
  escrow: st('ownership', 'In escrow', null, 'in escrow'),
  borrowed: st('ownership', 'Borrowed', null, 'borrowed'),
  favourite: st('ownership', 'Favourite', null, 'favourite'),
  discovered: st('knowledge', null),
  undiscovered: st('knowledge', 'Undiscovered', null, 'undiscovered'),
  hidden: st('knowledge', null),
  unknown: st('knowledge', LG_NONE, null, 'unknown'),
  loading: st('data', 'Loading…', null, 'loading'),
  refreshing: st('data', 'Updating…', null, null),
  pending: st('data', 'Saving…', null, 'busy'),
  stale: st('data', 'Updated', null, null),
  error: st('data', null, 'danger', null, { glyph: '✕' }),
  empty: st('data', null),
  offline: st('data', 'Reconnecting…', 'warning', null),
};

/** The one Tag on a row, first to last (Standards · State combinations). */
export const LG_TAG_ORDER: readonly LgStateName[] = [
  'failed', 'claimable', 'expiring', 'locked', 'listed', 'escrow', 'borrowed', 'equipped', 'attuned',
  'assigned', 'captured', 'in-progress', 'new', 'completed', 'claimed', 'expired', 'opened',
];

/** Of several states, the one that takes the row's single Tag. */
export function lgTopState(list: readonly LgStateName[] | null | undefined): LgStateName | null {
  let best: LgStateName | null = null;
  let at = Infinity;
  (list || []).forEach((s) => {
    const i = LG_TAG_ORDER.indexOf(s);
    if (i >= 0 && i < at) {
      at = i;
      best = s;
    }
  });
  return best;
}

export function lgIsBlocked(state: string | null | undefined): state is LgBlockedState {
  return !!state && !!LG_STATES[state as LgStateName]?.blocks;
}

const warned = new Set<string>();
/** Warns once per key, like the reference components do. */
export function lgStateWarn(key: string, message: string): void {
  if (warned.has(key) || typeof console === 'undefined') return;
  warned.add(key);
  console.warn(`LL: ${message} (Standards · States).`);
}

export interface LgShortfall {
  amount: number;
  name: string;
}

/** "Short by 120 Cinders and 2 Soulstones" */
export function lgShortfallText(list: readonly LgShortfall[] | null | undefined): string {
  const parts = (list || []).map((x) => lgFormatNumber(x.amount) + LG_NBSP + x.name);
  if (!parts.length) return '';
  return (
    'Short by ' +
    (parts.length > 1 ? parts.slice(0, -1).join(', ') + ' and ' + parts[parts.length - 1] : parts[0])
  );
}

export interface LgBlockedInput {
  reason?: string | null;
  shortfall?: readonly LgShortfall[] | null;
  /** Seconds until a cooldown ends. */
  remaining?: number | null;
}

export interface LgBlockedReason {
  reason: string;
  spoken?: string;
  word: string | null;
  tone: 'warning' | null;
}

/** The reason a blocked part gives, built from its state: Locked's condition, a shortfall, the time left. */
export function lgBlockedReason(state: LgBlockedState, p: LgBlockedInput, who: string): LgBlockedReason {
  let reason = p.reason || '';
  let spoken: string | undefined;
  if (state === 'insufficient' && p.shortfall) reason = lgShortfallText(p.shortfall);
  if (state === 'cooldown' && typeof p.remaining === 'number') {
    reason = 'Ready in ' + lgFormatDuration(p.remaining);
    spoken = 'Ready in ' + lgSpokenDuration(p.remaining);
  }
  if (!reason) {
    lgStateWarn(
      'reason:' + who + state,
      `${who} is ${state} with no reason; ${state === 'locked' ? 'say how it unlocks' : 'say why'}`,
    );
  }
  return {
    reason,
    spoken,
    word: state === 'locked' ? 'Locked' : null,
    tone: state === 'insufficient' ? 'warning' : null,
  };
}

/**
 * The attention mark's words (Standards · State combinations): `ready` is true or the words ("Upgrade available").
 * Mirrors readyWords in bundle.js.
 */
export function lgReadyWords(ready: boolean | string | null | undefined): string | null {
  return ready ? (typeof ready === 'string' ? ready : LG_STATES.ready.word) : null;
}
