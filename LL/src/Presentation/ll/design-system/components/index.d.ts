import type * as React from 'react';

/** Names of the game's sidebar icons, redrawn in currentColor. */
export type IconName =
  | 'overview' | 'inventory' | 'essences' | 'combat-styles' | 'achievements' | 'soulstones'
  | 'world-map' | 'legacy-ascension' | 'quest-journal' | 'prophecies'
  | 'guild' | 'colosseum' | 'cinder-bazaar' | 'leaderboard' | 'settings'
  /** The Nobility mark: a filled crown, drawn at 12px (icon-marker) beside a name and 16px beside a screen title (Registries · Nobility mark). Not a sidebar icon. */
  | 'nobility';

export type Rarity = 'Common' | 'Uncommon' | 'Rare' | 'Epic' | 'Unique' | 'Legendary' | 'Legacy';
/** Foundations · Space & Density. comfortable: identity and detail views, the Folio, dialogs (48px rows, 44px controls, body text).
 *  standard: the default, panels and forms (40px rows, 40px controls, body-compact rows). compact: tables, inventory, rankings,
 *  guild members, combat logs, order books (32px rows, 32px controls, body-compact rows, numeral-compact numbers).
 *  A `density` prop sets data-density on the component; without it the component follows the nearest data-density container. */
export type Density = 'comfortable' | 'standard' | 'compact';

/** locked (Standards · States): stays in the Tab order (aria-disabled), says "Locked" where the badge goes, shows `reason` —
 *  how it unlocks — in the reason tip beside it, and a click or Enter shows the reason instead of navigating. */
export interface NavItem { id: string; title: string; icon?: IconName; href?: string; badge?: React.ReactNode; badgeLabel?: string; locked?: boolean; reason?: string;
  /** Something waiting with nothing to count: the attention diamond at the item's end. true, or the words ("Quest ready").
   *  A count `badge` comes first, and Locked before both (Standards · State combinations). */
  ready?: boolean | string }
export interface NavSection { label: string; items: NavItem[] }

/** Renders a "Skip to content" link as the first tab stop. Under 60rem the rail becomes a drawer: opening it moves focus
 *  into the rail and makes the rest inert; Escape closes it and returns focus to the button that opened it. The drawer
 *  slides in over duration-base on ease-enter with its scrim, leaves over duration-fast on ease-exit, and once gone is
 *  hidden, so nothing off-screen takes focus. Dragging the floating Chronicle follows the pointer at once. */
export interface GameShellProps {
  /** Usually a NavRail. */
  rail?: React.ReactNode;
  /** A TopBar, or a function receiving { openRail } so the TopBar's menu button can open the rail drawer on narrow screens. */
  top?: React.ReactNode | ((api: { openRail: () => void }) => React.ReactNode);
  /** Usually a Folio. Adds the right-hand column. */
  folio?: React.ReactNode;
  /** Usually KeyHints; pinned to the stage's bottom-right on desktop, hidden under 960px. */
  hints?: React.ReactNode;
  /** A Chronicle (chat + game log). Placed by chatLayout. */
  chronicle?: React.ReactElement<ChronicleProps>;
  /** The player's Chat layout setting (the game's ChatLayout type). docked (default): right side — own column at 1536px+, under the Folio below; floating: draggable drawer over the stage. */
  chatLayout?: 'docked' | 'floating';
  /** Overrides whether the Chronicle counts as open for the layout; defaults to the Chronicle's own `open`. */
  chronicleOpen?: boolean;
  /** Floating drawer position in px from the shell's bottom-left. Leave undefined to let the shell keep it. */
  chroniclePosition?: { left: number; bottom: number } | null;
  /** Called while the drawer is dragged or nudged — persist it like the game's floatingDrawerPosition. */
  onChroniclePositionChange?: (pos: { left: number; bottom: number }) => void;
  /** CSS height of the shell; defaults to 100vh. */
  height?: number | string;
  className?: string;
  /** The stage — usually a Stage. */
  children?: React.ReactNode;
}
export declare function GameShell(props: GameShellProps): React.ReactElement;

export interface TopBarProps {
  /** Character name or screen title. */
  title: React.ReactNode;
  /** Short context after the title: "Lv. 42", a region, a section. */
  eyebrow?: React.ReactNode;
  /** Shows the menu button (narrow screens only). */
  onMenu?: () => void;
  /** A Meter or Track centred in the bar. */
  center?: React.ReactNode;
  /** Right-aligned: CurrencyPills, a Button. */
  children?: React.ReactNode;
  className?: string;
}
export declare function TopBar(props: TopBarProps): React.ReactElement;

export interface NavRailProps {
  sections: NavSection[];
  activeId?: string;
  onNavigate?: (id: string) => void;
  /** Logo and wordmark at the top. */
  header?: React.ReactNode;
  footer?: React.ReactNode;
  /** Icons only (64px wide). */
  compact?: boolean;
  label?: string;
  className?: string;
}
/** Hover fades the surface-raised wash in over duration-fast; the active item and its diamond move at once — the current
 *  location never animates. As GameShell's drawer under 60rem it slides in over duration-base and out over duration-fast. */
export declare function NavRail(props: NavRailProps): React.ReactElement;

export interface TabStripProps {
  tabs: { id: string; label: React.ReactNode; count?: number }[];
  activeId: string;
  onChange?: (id: string) => void;
  /** primary: big condensed tabs with a filled active tab; secondary: small caps filters. Default primary. */
  level?: 'primary' | 'secondary';
  /** Tab height follows the control height: 44, 40 or 32px. */
  density?: Density;
  label?: string;
  className?: string;
}
export declare function TabStrip(props: TabStripProps): React.ReactElement;

export interface StageProps {
  /** Scene art URL (a Backgrounds or Cards asset). It is darkened, warmed and slightly blurred. */
  image?: string;
  /** CSS background-position for the art. Default "center". */
  focus?: string;
  label?: string;
  className?: string;
  /** Overlays: a Constellation, captions, an archive grid. Position them absolutely or fill the stage. */
  children?: React.ReactNode;
}
export declare function Stage(props: StageProps): React.ReactElement;

export interface FolioEffect { value: React.ReactNode; text: React.ReactNode }
export interface FolioProps {
  /** Usually an Emblem. */
  emblem?: React.ReactNode;
  eyebrow?: React.ReactNode;
  title?: React.ReactNode;
  /** Lighter first part of the title ("Ember" Wolf). */
  titleSub?: React.ReactNode;
  /** Flavor text; wrap key nouns in <b>. */
  lore?: React.ReactNode;
  /** Effect lines; the value (the magnitude) is set in gilt — in ink when `rarity` is set. */
  effects?: (string | FolioEffect)[];
  /** Set when the Folio explains an item: it becomes an item context — the title takes the rarity colour, a rarity Tag follows it, and magnitudes turn ink (D-017, D-019). */
  rarity?: Rarity;
  actions?: React.ReactNode;
  /** Pinned to the bottom edge — a Track, a secondary action. */
  footer?: React.ReactNode;
  /** URL of a corner ornament (the Ornaments/CornerOrnament.svg asset); drawn as a mask in gilt. The Folio is the screen's one
   *  ornamented framed surface: never beside a Banner (Foundations · Ornament). It has no film grain. */
  cornerSrc?: string;
  /** Left-align the content (stat columns). Default centred. */
  align?: 'center' | 'start';
  ariaLabel?: string;
  className?: string;
  children?: React.ReactNode;
}
export declare function Folio(props: FolioProps): React.ReactElement;

export interface PanelProps {
  title?: React.ReactNode;
  titleAlign?: 'start' | 'end';
  /** Right side of the header: a Tag, a count. */
  aside?: React.ReactNode;
  /** No body padding: a List or table inside runs edge to edge, and its rows take the Panel's inset, so they align with the title.
   *  A Panel has no edge of its own, so its rows' separators never meet a container border (Foundations · Lines). */
  flush?: boolean;
  /** Padding follows the density: 24, 16 or 12px; one step less when the Panel sits inside another surface (the Folio, a Banner). */
  density?: Density;
  className?: string;
  children?: React.ReactNode;
}
export declare function Panel(props: PanelProps): React.ReactElement;

export interface SigilProps {
  value: React.ReactNode;
  label?: React.ReactNode;
  labelPosition?: 'right' | 'left' | 'top' | 'bottom' | 'none';
  size?: 'sm' | 'md' | 'lg';
  /** selected: an arcana edge; ready: a diamond marks that it can be raised; locked: the hex empties and the value greys.
   *  A locked Sigil with onClick stays a focusable button (aria-disabled) whose press shows `reason` (Standards · States). */
  state?: 'default' | 'selected' | 'ready' | 'locked';
  /** Locked: how it unlocks ("Unlocks at level 20"), shown in the reason tip and read as the description. */
  reason?: string;
  /** Makes it a button (aria-pressed follows state === "selected"). */
  onClick?: () => void;
  title?: string;
  className?: string;
  style?: React.CSSProperties;
}
export declare function Sigil(props: SigilProps): React.ReactElement;

export interface ConstellationItem { id: string; label: React.ReactNode; value: React.ReactNode; x: number; y: number; labelPosition?: 'right' | 'left' | 'top' | 'bottom' | 'none'; size?: 'sm' | 'md' | 'lg'; state?: 'default' | 'ready' | 'locked'; reason?: string }
export interface ConstellationProps {
  /** Coordinate space; the component keeps this aspect ratio and scales to its container's width. */
  width?: number;
  height?: number;
  items: ConstellationItem[];
  /** Orbit rings in the same coordinates; strong rings are drawn in gilt. */
  rings?: { cx: number; cy: number; r: number; strong?: boolean }[];
  /** Points on the rings, drawn as short gilt ticks across the nearest ring (not diamonds: Foundations · Shape). */
  nodes?: { x: number; y: number }[];
  selectedId?: string;
  onSelect?: (id: string) => void;
  label?: string;
  className?: string;
}
export declare function Constellation(props: ConstellationProps): React.ReactElement;

export interface MeterProps {
  value: number;
  max: number;
  label?: React.ReactNode;
  tone?: 'hp' | 'sp' | 'xp';
  /** thin: 4px line (default); bar: 10px framed bar for the top bar and combat. */
  size?: 'thin' | 'bar';
  showValue?: boolean;
  /** Unit after the value, set smaller and muted: "HP". */
  unit?: string;
  /** Combat playback: the fill follows each tick over duration-fast. Without it a change is a step, over duration-slow
   *  (Foundations · Motion · Value change). The printed value always changes at once. */
  live?: boolean;
  ariaLabel?: string;
  className?: string;
}
/** Shows "value / max" in tabular figures and reserves the width of "max / max", so a live value never shifts the row.
 *  The fill is scaled, never resized; under reduced motion it moves at once. */
export declare function Meter(props: MeterProps): React.ReactElement;

export interface TrackProps {
  steps: number;
  /** Zero-based index of the current step; earlier steps are done. */
  current: number;
  startLabel?: React.ReactNode;
  endLabel?: React.ReactNode;
  /** Per-step names, used for tooltips and aria-valuetext. */
  labels?: string[];
  tone?: 'gilt' | 'hp' | 'arcana';
  label?: string;
  className?: string;
}
export declare function Track(props: TrackProps): React.ReactElement;

export interface LevelPlateProps {
  level: React.ReactNode;
  kicker?: React.ReactNode;
  xp?: number;
  xpMax?: number;
  /** Unit after the progress numbers. Default "EXP". */
  xpUnit?: string;
  /** Up to two side stats. `unit` follows the value, smaller and muted ("12s", "84 HP/5s"). */
  aside?: { icon?: IconName; label: React.ReactNode; value: number | string; unit?: string }[];
  className?: string;
}
export declare function LevelPlate(props: LevelPlateProps): React.ReactElement;

export interface StatTileProps {
  label: React.ReactNode;
  /** A number (formatted for you) or a formatted string. null shows "—". */
  value: React.ReactNode;
  /** The unit after the value, set smaller and muted: "%", "s". */
  suffix?: React.ReactNode;
  /** The change, as a signed number: shown as ▲ +4, ▼ −1.2s or ±0 (unchanged). */
  delta?: number;
  /** Whether the change helps the player — from the game's rules for this stat, never from the sign (a shorter cooldown is `better`). Without it the delta is `neutral`. */
  deltaPolarity?: 'better' | 'worse' | 'neutral';
  /** The formatted size of the change without its sign ("1.2s"). Default: the absolute delta plus `suffix`. */
  deltaText?: string;
  className?: string;
}
export declare function StatTile(props: StatTileProps): React.ReactElement;

export interface DeltaProps {
  /** Which way the number moved: ▲ with +, ▼ with −, or none, shown as ±0 with no shape (Foundations · Shape). */
  direction: 'up' | 'down' | 'none';
  /** The size of the change, formatted, without a sign: "12%", "1.2s", "0". */
  value: React.ReactNode;
  /** Whether it helps the player. Set it from the stat's rules, never from the sign. Screen readers hear it as a word. */
  polarity: 'better' | 'worse' | 'neutral';
  className?: string;
}
/** A stat change: glyph, sign and value, coloured by polarity (delta-better, delta-worse, delta-neutral). */
export declare function Delta(props: DeltaProps): React.ReactElement;

/** locked (Standards · States): Up and Down still land on it and its `reason` — how it unlocks — opens beside it; it is never
 *  selected, and Enter or a click shows the reason. A locked entry shows the Locked Tag in place of its own tag. */
export interface EntryListItem { id: string; name: React.ReactNode; tag?: React.ReactNode; tagTone?: TagProps['tone']; locked?: boolean; reason?: string;
  /** The attention diamond at the row's end: true, or the words screen readers hear ("1 point to spend"). Not on a locked entry. */
  ready?: boolean | string }
export interface EntryListProps {
  items: EntryListItem[];
  activeId?: string;
  onSelect?: (id: string) => void;
  /** Fade the top and bottom edges. Default true. Give the list a height; it scrolls. */
  fade?: boolean;
  /** comfortable and standard: 24px names on 48 or 40px rows; compact: 17px names (name-row) on 32px rows. */
  density?: Density;
  label?: string;
  className?: string;
}
export declare function EntryList(props: EntryListProps): React.ReactElement;

export interface ItemSlotProps {
  name?: React.ReactNode;
  /** Shown when there is no name (an empty equipment slot: "Off-hand"). */
  slotLabel?: React.ReactNode;
  meta?: React.ReactNode;
  rarity?: Rarity;
  image?: string;
  icon?: IconName;
  quantity?: number;
  selected?: boolean;
  size?: 'sm' | 'md' | 'wide';
  /** false hides the caption under the frame. */
  caption?: boolean;
  /** Standards · States. Blocked (locked, unavailable, restricted, insufficient, cooldown): no press, the reason printed under
   *  the name (or, with no caption, in the reason tip). not-owned: the art at opacity-unowned. undiscovered: no art, no name.
   *  Any other state with a word (equipped, attuned, assigned, captured, listed, escrow, borrowed…) leads the meta line;
   *  equipped and attuned also take the in-use square in the bottom start corner. */
  state?: StateName;
  /** A blocked slot's reason: "Unlocks at level 20", "Inventory full". */
  reason?: string;
  /** insufficient: what is missing, as "Short by 250 Cinders". */
  shortfall?: { amount: number; name: string }[];
  /** cooldown: seconds left, as "Ready in 12s". */
  remaining?: number;
  /** The player's favourite (protected): the 12px ribbon in the bottom start corner once it is drawn, unless the in-use square
   *  (equipped, attuned) holds that corner; otherwise the word in the meta line. */
  favourite?: boolean;
  /** Something waiting for the player: the attention diamond in the top end corner. true, or the words ("Upgrade available"),
   *  which join the accessible name. The claimable state draws it too; a blocked or undiscovered slot never does
   *  (Standards · State combinations). */
  ready?: boolean | string;
  onClick?: () => void;
  className?: string;
}
export declare function ItemSlot(props: ItemSlotProps): React.ReactElement;

export interface ListProps {
  label?: string; density?: Density;
  /** How rows are told apart (Foundations · Lines), one at a time: `separators` (the default) a `line` hairline between rows;
   *  `zebra` every second row on `row-stripe`, for wide or dense rows read across; `spacing` neither, for a short list. */
  rhythm?: 'separators' | 'zebra' | 'spacing';
  className?: string; children?: React.ReactNode;
}
/** A list of ListRows sharing columns (thumbnail, name, quantity, value, trailing), so every value lines up.
 *  Keyboard: one tab stop (the selected row, else the first); Up, Down, Home and End move between rows; Right moves into a
 *  row's trailing action and Left back. Trailing actions leave the tab order. */
export declare function List(props: ListProps): React.ReactElement;
export interface ListRowProps {
  /** The name, in name-row (Marcellus 17px), in its rarity colour when `rarity` is set. */
  title: React.ReactNode;
  /** Edges the thumbnail, colours the name and adds the rarity code after it. */
  rarity?: Rarity;
  /** Thumbnail: an icon, an image URL, or any node. */
  icon?: IconName; image?: string; thumb?: React.ReactNode;
  /** caption: type, level, guild rank. Beside the name; on its own line in comfortable. */
  meta?: React.ReactNode;
  /** Tags after the name ("Equipped"). */
  tags?: React.ReactNode;
  /** Stack size, shown as ×3. */
  quantity?: number;
  /** A formatted number, right-aligned; a unit after it is set muted. null shows "—". */
  value?: React.ReactNode;
  /** The value changes by itself (a refreshed Bazaar price, a roster's status): each change takes the live-update mark,
   *  a `changed` wash held for duration-reveal (Foundations · Motion). Pair the List with LL.motion.useLiveList. */
  live?: boolean;
  /** An action or a status at the end: a sm Button, Presence. */
  trailing?: React.ReactNode;
  /** Selection is an ink bar and the raised wash — a shape, right in item contexts. */
  selected?: boolean;
  muted?: boolean;
  /** Makes the name a button whose hit area covers the row; trailing actions stay separately clickable. */
  onClick?: () => void;
  density?: Density;
  className?: string;
}
export declare function ListRow(props: ListRowProps): React.ReactElement;

export type ChronicleChannelId = 'all' | 'general' | 'trade' | 'help' | 'guild' | 'whisper' | 'raid' | 'invites' | 'system' | 'loot' | string;
export interface ChronicleMessage {
  id: string | number;
  channel: ChronicleChannelId;
  /** Shown as the tag when the line appears outside its own tab. */
  channelLabel?: string;
  time?: string;
  author?: string;
  /** Whispers: "From Kaelen" / "To Kaelen". */
  direction?: 'from' | 'to';
  /** system and loot lines are set in lore italic without an author. */
  kind?: 'chat' | 'system' | 'loot';
  /** The line mentions the player: a neutral surface-raised wash and a 2px ink edge (D-022). */
  mention?: boolean;
  /** May contain ItemLinks and <span class="lg-mention">@Name</span>. */
  text: React.ReactNode;
}
export interface ChronicleProps {
  channels: { id: ChronicleChannelId; label: React.ReactNode; unread?: number }[];
  activeChannel?: ChronicleChannelId;
  onChannelChange?: (id: ChronicleChannelId) => void;
  messages: ChronicleMessage[];
  /** false shows the one-line ticker. Default true. Opened again, the body rises in over duration-base; closing is at once.
   *  New lines follow the log's foot only while the player is there: scrolled up, the log keeps its place and a
   *  "3 new lines" control jumps to the latest (Foundations · Motion · Live updates). */
  open?: boolean;
  /** What screen readers hear as lines arrive. all (default): the log is a polite live region. mentions: only lines that
   *  mention you and whispers to you, through LL.announce (throttled). off: nothing. Combat events never go in the Chronicle. */
  announce?: 'all' | 'mentions' | 'off';
  onToggle?: () => void;
  composer?: { channel?: ChronicleChannelId; channelLabel?: string; value: string; onChange: (v: string) => void; onSend: (v: string) => void; placeholder?: string; maxLength?: number };
  /** Extra header controls (channel settings, pop-out). */
  aside?: React.ReactNode;
  /** Drawer styling for the floating chat layout (GameShell sets it). */
  floating?: boolean;
  /** Floating drawer: tall size (chronicle-float-tall). GameShell manages it unless you pass it. */
  tall?: boolean;
  onTallToggle?: () => void;
  /** Floating drawer: props spread onto the drag grip button (pointer and keyboard handlers). GameShell provides them. */
  dragHandle?: React.ButtonHTMLAttributes<HTMLButtonElement>;
  label?: string;
  className?: string;
}
export declare function Chronicle(props: ChronicleProps): React.ReactElement;

export interface ItemLinkProps { rarity?: Rarity; meta?: string; onClick?: () => void; className?: string; children?: React.ReactNode }
export declare function ItemLink(props: ItemLinkProps): React.ReactElement;

export interface PageProps { label?: string; /** Defaults to page-max (80rem). */ maxWidth?: number | string; role?: string;
  /** In a host frame that is not GameShell (the game's frame while screens migrate): in the normal flow, filling its parent's
   *  height, with no room kept for a TopBar; the host gives the gutters. */
  flow?: boolean; className?: string; children?: React.ReactNode }
/** Scrolling frame for information screens (no stage art). Place it as GameShell's children, or with `flow` in another frame. It is a layout region (lg-region):
 *  content inside follows the tiers of its width — Stacked, Narrow from 32rem, Medium from 44rem, Wide from 68rem (Foundations · Layout). */
export declare function Page(props: PageProps): React.ReactElement;

export interface PageHeaderProps { title: React.ReactNode; eyebrow?: React.ReactNode; summary?: React.ReactNode; icon?: IconName; className?: string; children?: React.ReactNode }
export declare function PageHeader(props: PageHeaderProps): React.ReactElement;

export interface SearchFieldProps {
  value: string; onChange: (v: string) => void;
  suggestions?: string[]; onSelect?: (v: string) => void; onSubmit?: (v: string) => void;
  loading?: boolean; loadingText?: string;
  /** A search has run for this value: show emptyText when there are no suggestions. */
  searched?: boolean; emptyText?: string;
  placeholder?: string; label?: string; maxLength?: number; className?: string;
}
export declare function SearchField(props: SearchFieldProps): React.ReactElement;

/** The headline block of an information screen, and its one ornamented framed surface: never beside a Folio (Foundations · Ornament).
 *  The veil (vignette and film grain) is drawn only with an `image`. */
export interface BannerProps { image?: string; focus?: string; cornerSrc?: string; aside?: React.ReactNode; footer?: React.ReactNode; label?: string; className?: string; children?: React.ReactNode }
export declare function Banner(props: BannerProps): React.ReactElement;

/** The screen's one isolated headline number: Marcellus in gilt with proportional figures. null shows "—". */
export interface StatFigureProps { label: React.ReactNode; value: React.ReactNode; caption?: React.ReactNode; title?: string; size?: 'md' | 'sm'; className?: string }
export declare function StatFigure(props: StatFigureProps): React.ReactElement;

export interface JourneyCardProps {
  phase: string; stages?: string[]; title: React.ReactNode; summary?: React.ReactNode;
  objective?: React.ReactNode; objectiveLabel?: string; actions?: React.ReactNode;
  nextUnlock?: React.ReactNode; nextUnlockLabel?: string; id?: string; className?: string;
}
export declare function JourneyCard(props: JourneyCardProps): React.ReactElement;

export interface LedgerRow {
  id?: string; label: React.ReactNode;
  /** A formatted number, with any unit after it: "1,284", "24.8%", "84 HP/5s", "×1.5". Values align on the decimal point
   *  and units are set smaller and muted. Missing or not applicable: omit it or pass null — the row shows "—", never nothing. */
  value?: React.ReactNode;
  sub?: React.ReactNode; description?: React.ReactNode; tipMeta?: React.ReactNode; muted?: boolean;
}
export interface LedgerProps { title: React.ReactNode; rows: LedgerRow[]; /** Row height, padding and type: 48 / 40 / 32px rows; compact sets values in numeral-compact. */ density?: Density; className?: string }
/** Wrap several in <div class="lg-ledgergrid"> (four groups; --3 and --2 for three or two), which follows its region's tier. Rows with a description are one tab stop (Up, Down, Home, End move);
 *  the explanation opens on hover and focus, pins open on click or tap, and closes on Escape. */
export declare function Ledger(props: LedgerProps): React.ReactElement;

export interface LoadoutSlotProps {
  index?: number; state?: 'attuned' | 'open' | 'locked'; name?: React.ReactNode; rarity?: Rarity;
  active?: { name: React.ReactNode; cooldown?: string }; passive?: { name: React.ReactNode };
  icon?: IconName; image?: string;
  /** locked: how it unlocks, printed as the slot's name ("Unlocks at level 20") and read as its description. `unlockLabel` is the older name. */
  reason?: string; unlockLabel?: React.ReactNode;
  hint?: React.ReactNode;
  /** The attention diamond at the end of the head, after the Tag: true, or the words ("Essence ready to attune"). Not on a locked slot. */
  ready?: boolean | string;
  /** One short row for a full loadout (D-103): the name leads, the slot number is for screen readers, an attuned slot has no Tag. */
  compact?: boolean;
  onClick?: () => void; className?: string;
}
export declare function LoadoutSlot(props: LoadoutSlotProps): React.ReactElement;

export interface PresenceProps { online: boolean; lastSeen?: string; compact?: boolean; className?: string }
export declare function Presence(props: PresenceProps): React.ReactElement;

export interface ProfileFact { label: React.ReactNode; value: React.ReactNode; key?: string }
export interface ProfileIdentityProps {
  /** "Combat Profile"; "Viewing player" on someone else's. */
  eyebrow?: React.ReactNode;
  name: React.ReactNode;
  /** Active Nobility: the crown before the name (D-066). */
  noble?: boolean;
  /** Another player's: Presence after the name. */
  presence?: PresenceProps;
  /** Guild, Essences, Achievement Points, Nobility… A value may hold a link, a Tag or a link Button. Falsy entries are skipped. */
  facts?: (ProfileFact | null | false | undefined)[];
  /** The name's heading element. Default h2. */
  as?: 'h1' | 'h2' | 'h3';
  headingId?: string;
  className?: string;
}
/** Who a player is, at the head of a profile: put it in a Banner's body (D-099). */
export declare function ProfileIdentity(props: ProfileIdentityProps): React.ReactElement;

/** Every state in Standards · States, by family. */
export type StateName =
  | 'default' | 'hover' | 'focus-visible' | 'pressed' | 'selected' | 'current' | 'dragging' | 'disabled'
  | 'available' | 'ready' | 'unavailable' | 'locked' | 'restricted' | 'insufficient' | 'cooldown'
  | 'new' | 'unread' | 'in-progress' | 'completed' | 'claimable' | 'claimed' | 'opened' | 'expiring' | 'expired' | 'failed'
  | 'owned' | 'not-owned' | 'equipped' | 'attuned' | 'assigned' | 'captured' | 'listed' | 'escrow' | 'borrowed' | 'favourite'
  | 'discovered' | 'undiscovered' | 'hidden' | 'unknown'
  | 'loading' | 'refreshing' | 'pending' | 'stale' | 'error' | 'empty' | 'offline';
export interface StateInfo {
  family: 'interaction' | 'availability' | 'lifecycle' | 'ownership' | 'knowledge' | 'data';
  /** The exact word on screen, or null where the state shows no word of its own. */
  word: string | null;
  /** The Tag tone it takes. */
  tone: TagProps['tone'] | null;
  /** What screen readers hear after a name: "new", "locked", "equipped". */
  sr: string | null;
  /** Drawn before the word and not read: + for new, ✕ for failed. */
  glyph?: string;
  /** Stops a press, and must give a reason. */
  blocks?: boolean;
}
export interface TagProps {
  /** `new`: new, ready, claimable or actionable (arcana). `danger`: a loss or failure; `warning`: a reversible risk, a shortfall, something expiring; `success`: a confirmed outcome, with ✓ added. Status tones are outlined and belong to shell and data contexts; inside an item context use `neutral` or a rarity tone. `beneficial` and `harmful`: effects and conditions (Empower, Weaken), always named; a beneficial Tag keeps its radius-control corners and a harmful one has its corners cut, so polarity reads without colour. `gilt` is deprecated (D-020) and renders as `neutral`. */
  tone?: 'neutral' | 'new' | 'gilt' | 'danger' | 'warning' | 'success' | 'beneficial' | 'harmful' | 'locked' | 'common' | 'uncommon' | 'rare' | 'epic' | 'unique' | 'legendary' | 'legacy';
  /** A state from Standards · States: its word, tone and glyph come from LL.states — locked (a dashed edge), claimable,
   *  completed (✓), failed (✕), expiring ("Expires in" + value), equipped… Children replace the word ("In Preset 2"). */
  state?: StateName;
  /** A number after the label, kept out of capitals and in tabular figures: Weaken 6s, Short by 12. */
  value?: number | string;
  className?: string;
  children?: React.ReactNode;
}
export declare function Tag(props: TagProps): React.ReactElement;

export interface CurrencyPillProps {
  name: 'Cinders' | 'Soulstones' | string;
  amount: number;
  /** Currency art URL (Currency/Coins.svg for Cinders, Currency/Diamonds.svg for Soulstones). */
  iconSrc?: string;
  /** Abbreviate: 12.5k, 3.2M. Screen readers always hear the full number ("12,480 Cinders"). A short CurrencyPill is always a button: without `onClick` it toggles between 12.5k and 12,480 itself, so the full figure is never hover-only. */
  short?: boolean;
  /** Characters of width to reserve for the amount. The amount never shrinks while mounted either, so live changes do not reflow the TopBar. */
  reserve?: number;
  /** Your own toggle, when the game stores the format as a setting. Without it a `short` CurrencyPill toggles itself. */
  onClick?: () => void;
  title?: string;
  className?: string;
}
export declare function CurrencyPill(props: CurrencyPillProps): React.ReactElement;

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  /** primary: an engraved rectangle with a line-strong edge (default); solid: gilt fill, one per screen; quiet: text only; danger: destructive; link: gilt text for in-panel navigation. Corners are radius-control (D-064). */
  variant?: 'primary' | 'solid' | 'quiet' | 'danger' | 'link';
  /** Omit to follow the region's density (44, 40 or 32px). md pins Standard (40px), sm pins Compact (32px). */
  size?: 'md' | 'sm';
  density?: Density;
  /** Key cap shown after the label, and aria-keyshortcuts. Wire the key yourself. */
  hotkey?: string;
  icon?: IconName;
  /** Standards · States. Blocked — unavailable, locked (a dashed edge), restricted, insufficient, cooldown: the label in
   *  ink-muted, no hover or press, still focusable (aria-disabled), and its reason in the reason tip; a press shows and
   *  announces the reason and never calls onClick. pending: aria-busy, the pendingLabel, and no second press. Plain
   *  `disabled` is for a limit that is obvious beside it, and is rare. */
  state?: 'available' | 'unavailable' | 'locked' | 'restricted' | 'insufficient' | 'cooldown' | 'pending';
  /** Why it is blocked: "Not while in a dungeon"; for locked, how it unlocks: "Unlocks at level 20"; for restricted, who may. */
  reason?: string;
  /** insufficient: what is missing. Becomes "Short by 250 Cinders and 40 Essence Dust". */
  shortfall?: { amount: number; name: string }[];
  /** cooldown: seconds left. Becomes "Ready in 4m 12s", read as "Ready in 4 minutes 12 seconds". */
  remaining?: number;
  /** The progressive label while pending: "Claiming…". Give it always, so the Button keeps room for it and nothing shifts. */
  pendingLabel?: React.ReactNode;
}
/** Feedback (Foundations · Motion): hover fades a layer in over duration-fast — the gilt edge, or the solid fill 6% brighter;
 *  a press sinks the Button 1px at once and releases over duration-fast. Under reduced motion both happen at once. */
export declare function Button(props: ButtonProps): React.ReactElement;

export interface KeyHintsProps { hints: { label: React.ReactNode; key: string }[]; label?: string; className?: string }
export declare function KeyHints(props: KeyHintsProps): React.ReactElement;
export declare function Key(props: { children?: React.ReactNode; className?: string }): React.ReactElement;

export interface HeadingProps {
  /** folio: title-xl, 56px, once per screen; screen: title-lg, 36px; section: title-md, 24px; subsection: title-sm, 20px. */
  level?: 'folio' | 'screen' | 'section' | 'subsection';
  /** Lighter leading words. */
  sub?: React.ReactNode;
  as?: keyof JSX.IntrinsicElements;
  id?: string;
  className?: string;
  children?: React.ReactNode;
}
export declare function Heading(props: HeadingProps): React.ReactElement;

export interface SectionRuleProps {
  /** band: a filled strip with a label, for data groups in stat columns (Status, Biography); hairline: a `line` rule, for any
   *  other group; ornament: the gilt diamond-chain lattice, between lore and effects, at most once per surface (the Folio draws
   *  its own). A second ornament on one surface logs a console warning. */
  variant?: 'band' | 'ornament' | 'hairline';
  label?: React.ReactNode;
  aside?: React.ReactNode;
  align?: 'start' | 'end';
  className?: string;
}
export declare function SectionRule(props: SectionRuleProps): React.ReactElement;

export interface EmblemProps {
  /** Star points, 5–12. Give each attribute or school its own count. */
  points?: number;
  /** Vertex step of the star polygon; defaults to 2 (6 or fewer points) or 3. */
  skip?: number;
  size?: number;
  className?: string;
}
export declare function Emblem(props: EmblemProps): React.ReactElement;

export interface IconProps {
  name: IconName;
  /** The scale (Foundations · Iconography): 16 (icon-sm), 20 (icon-md, default) or 24 (icon-lg) for line icons; 12 (icon-marker)
   *  only for the solid inline markers (nobility). Another size, or a line icon under 16, logs a console warning. */
  size?: 12 | 16 | 20 | 24;
  /** A standalone icon that means something gets a title, which is also its accessible name. */
  title?: string;
  /** Leave it: every icon is drawn at 1.6 on the 24-unit grid. */
  strokeWidth?: number;
  className?: string;
}
/** A line icon from the set, in currentColor. An unknown name draws nothing (and warns): the label beside it is the fallback. */
export declare function Icon(props: IconProps): React.ReactElement | null;

export type LayerKind = 'popover' | 'modal' | 'confirm' | 'popover-detached' | 'tour' | 'drag';
type LayerTarget = Element | React.RefObject<Element> | null;
export interface LayerOptions {
  /** Sets the layer's place in the stack: popover 100, modal 200, confirm 250, popover-detached 300, tour 500, drag 600 (the z-* tokens). */
  kind: LayerKind;
  /** Called when Escape closes this layer: close your UI here. */
  onClose?: () => void;
  /** Where focus returns on close. Defaults to the element focused when the layer opened. */
  opener?: LayerTarget | (() => LayerTarget);
  /** Keep Tab inside these elements while the layer is on top. */
  trap?: LayerTarget | LayerTarget[] | (() => LayerTarget | LayerTarget[]);
  /** Limit the layer to keys pressed inside this element: an embedded widget, or a preview running several screens at once. The game has one stack and no scope. */
  scope?: LayerTarget | (() => LayerTarget);
  /** use() only: false leaves focus where it is on close. */
  restoreFocus?: boolean;
}
/** What LL.ornament.audit counts on one screen (Foundations · Ornament). */
export interface OrnamentCounts {
  /** Ornamented framed surfaces: Folios, Banners and anything tagged data-ornament="frame". Budget 1. */
  framed: number;
  /** Ornament rules: SectionRule ornaments and anything tagged data-ornament="rule" (the Divider asset). Budget 2. */
  rules: number;
  /** Surfaces carrying CornerOrnaments. Only the framed surface may. */
  corners: number;
  /** Surfaces with film grain. Only over art. */
  grain: number;
  /** Elements tagged data-ornament="texture": material textures. Budget 0. */
  textures: number;
  /** Stage and Banner veils. Only over art. */
  vignettes: number;
  /** Elements with a blurred shadow in a light colour. Budget 0: glow is flat. */
  halos: number;
  /** Elements with a looping animation. Budget 0, except the two loops Foundations · Motion allows: an indeterminate
   *  progress indicator (role="progressbar" with no aria-valuenow) and live combat playback (data-motion="playback"). */
  loops: number;
}
/** A live value as LL.motion.useLive shows it. */
export interface LiveValue<T> {
  /** What to print: the counting value while a player-caused change counts, else the value itself. */
  value: T;
  /** True for duration-reveal after a change that was not the player's. */
  changed: boolean;
  /** 'lg-live' or 'lg-live is-changed': put it on the element that holds the value, for the changed wash. */
  className: string;
}
/** A row as LL.motion.useLiveList returns it. */
export interface LiveRow<T> { key: string; item: T; /** The item has left the data; the row stays in place, muted, until the list is released. */ gone: boolean }
declare global {
  interface Window {
    LL: {
      GameShell: typeof GameShell; TopBar: typeof TopBar; NavRail: typeof NavRail; TabStrip: typeof TabStrip;
      Stage: typeof Stage; Folio: typeof Folio; Panel: typeof Panel;
      Sigil: typeof Sigil; Constellation: typeof Constellation; Meter: typeof Meter; Track: typeof Track;
      LevelPlate: typeof LevelPlate; StatTile: typeof StatTile; Delta: typeof Delta;
      Chronicle: typeof Chronicle; ItemLink: typeof ItemLink;
      Page: typeof Page; PageHeader: typeof PageHeader; SearchField: typeof SearchField; Banner: typeof Banner; StatFigure: typeof StatFigure;
      JourneyCard: typeof JourneyCard; Ledger: typeof Ledger; List: typeof List; ListRow: typeof ListRow; LoadoutSlot: typeof LoadoutSlot; Presence: typeof Presence;
      EntryList: typeof EntryList; ItemSlot: typeof ItemSlot; Tag: typeof Tag; CurrencyPill: typeof CurrencyPill;
      Button: typeof Button; KeyHints: typeof KeyHints; Key: typeof Key; Heading: typeof Heading;
      SectionRule: typeof SectionRule; Emblem: typeof Emblem; Icon: typeof Icon & { names: IconName[] };
      /** Foundations · Numerals. Every helper returns "—" for a missing value and uses a true minus (−). */
      format: {
        /** 12,480; −12; a fixed number of decimals when given. */
        number(n: number | null | undefined, digits?: number): string;
        /** 12.5k, 3.2M, with the full figure one hover or focus away. */
        short(n: number): string;
        /** 12–18, with an en dash. */
        range(a: number, b: number, digits?: number): string;
        /** ×1.5. */
        times(x: number, digits?: number): string;
        /** 3,120 / 4,150, with non-breaking spaces round the slash. */
        fraction(value: number, max: number): string;
        /** 24.8%. */
        percent(x: number, digits?: number): string;
        /** 12s, 24.8% (symbol units attach) or 84 HP/5s (word units after a non-breaking space). */
        unit(x: number, unit: string, digits?: number): string;
        /** Splits "184.6 threat/s" into { int: "184", frac: ".6", unit: "threat/s", sp: true } for decimal-aligned columns; null if not a number. */
        parts(v: number | string): { int: string; frac: string; unit: string; sp: boolean } | null;
        /** The em dash for unknown or not applicable. */
        none: string;
        /** Seconds as at most two units: 4m 12s, 2h 14m, 3d 4h, 12s (Foundations · Numerals). */
        duration(seconds: number): string;
        /** The same, for screen readers: "4 minutes 12 seconds". */
        spokenDuration(seconds: number): string;
      };
      /** Standards · States: every state's family, word, Tag tone and what screen readers hear. */
      states: Record<StateName, StateInfo>;
      /** The one Tag a row shows when several states apply (the Tag order in Standards · State combinations), or null. */
      topState(states: StateName[]): StateName | null;
      /** The reason tip for any focusable element (Standards · States · The reason tip). Spread `props` on the element and render
       *  `desc` inside it. word: the state's word ("Locked"); tone: 'warning' for a shortfall; place: 'end' to open beside
       *  rather than below; spoken: the words screen readers hear, if they differ; printed: the reason is already on screen
       *  in the element with this id (no tip, but a press still announces it); title: names the control when its label is
       *  hidden. */
      why(id: string, reason: string, options?: { word?: string; tone?: 'warning'; place?: 'end'; spoken?: string; printed?: boolean; title?: string }): {
        props: Record<string, unknown>; desc: React.ReactElement | null; spoken: string;
      };
      /** Throttled live-region announcement (Foundations · Accessibility · Live regions). Polite by default, one message
       *  every 1.5s at most; a message with the same key replaces the one still waiting; the same text is not repeated within
       *  5s. assertive: errors only. */
      announce(text: string, options?: { key?: string; assertive?: boolean }): void;
      /** The layer stack (Foundations · Surfaces & Layering). Escape closes the topmost layer — the highest z, then the latest
       *  opened — and focus returns to the element that opened it. `trap` keeps Tab inside the layer (a dialog, a confirmation,
       *  the tour: pass the coach mark and the spotlighted element). One modal at a time; a confirmation overlays the dialog
       *  that opened it. Toasts are not layers. */
      layers: {
        open(options: LayerOptions): { close(restoreFocus?: boolean): void; isTop(): boolean };
        /** React: registers the layer while `open` is true; closing or unmounting returns focus to the opener. */
        use(open: boolean, options: LayerOptions): void;
        /** The kind of the topmost open layer, or null. */
        top(): LayerKind | null;
      };
      /** The ornament budget (Foundations · Ornament). audit() counts the decorative devices under `root` (one screen: its
       *  GameShell's .lg-shell) and lists what is over the budget or in a forbidden zone. It cannot see ornament painted into
       *  images, so an eye check still stands. */
      ornament: {
        audit(root?: Element): { counts: OrnamentCounts; budget: { framed: number; rules: number; halos: number; loops: number }; problems: string[]; ok: boolean };
        budget: { framed: number; rules: number; halos: number; loops: number };
      };
      /** Foundations · Motion. The token values in milliseconds and as CSS easings, and the helpers that apply the rules. */
      motion: {
        duration: { instant: 0; fast: number; base: number; slow: number; reveal: number };
        easing: { standard: string; enter: string; exit: string };
        /** A duration token in ms, read from tokens.css when loaded: ms('reveal') → 600. */
        ms(name: 'instant' | 'fast' | 'base' | 'slow' | 'reveal'): number;
        /** The operating system's reduced-motion setting, or data-motion="reduced" on the element's ancestors or the root. */
        reduced(el?: Element | null): boolean;
        /** React: one live value. cause 'player' (after combat, after a purchase) counts to the new value over duration-slow;
         *  any other change shows at once and is marked for duration-reveal. Never both; nothing counts under reduced motion.
         *  mark: false turns the mark off. */
        useLive<T>(value: T, options?: { cause?: 'player' | 'world'; mark?: boolean; reduced?: boolean; el?: Element | { current: Element | null } | null }): LiveValue<T>;
        /** React: a refreshed list that never moves what the player is about to click. Put `ref` on the list element. While the
         *  pointer is over it or focus is inside, rows keep their order, values update in place, gone items stay (gone: true)
         *  and new ones wait in `pending` until the player leaves or you call release(). Keys in `keep` (the selection) stay
         *  in place, gone, even after release. */
        useLiveList<T>(items: T[], options?: { key?: string | ((item: T) => string); keep?: string | string[] | null }): {
          ref: { current: HTMLElement | null }; rows: LiveRow<T>[]; held: boolean; pending: number; release(): void;
        };
        /** What moves against the rules under `root`: a transition or animation on anything but transform and opacity
         *  (visibility may step), a duration past duration-reveal, a loop other than the two allowed. */
        audit(root?: Element): { counts: { transitions: number; animations: number; loops: number }; problems: string[]; ok: boolean };
        /** The selector for the two loops allowed. */
        loopsAllowed: string;
      };
      RARITY_CODES: Record<Rarity, string>;
    };
  }
}
