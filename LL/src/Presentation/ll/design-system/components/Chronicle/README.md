# Chronicle

Chat and the game log.

**Status:** Draft

The Chronicle is the game's chat and log in one place: what people say (General, Trade, Help, Guild, Whispers, Raid) and what happens to you (System lines, Loot).

**Provide:** `channels` (`[{ id, label, unread? }]` — an `all` id shows the merged feed of visible channels), `activeChannel`, `onChannelChange`, `messages` (`[{ id, channel, channelLabel, time, author?, direction?: 'from' | 'to', kind?: 'chat' | 'system' | 'loot', mention?, text }]` — `text` may contain ItemLinks and a `.lg-mention` span), `open` and `onToggle`, and a `composer` (`{ channel, channelLabel, value, onChange, onSend, placeholder? }`). Put channel settings (the visible-channels picker) in `aside`. Channel commands (/g, /w) and mention suggestions stay in your composer logic.

**Where it goes** follows the player's chat-layout setting — pass it to GameShell as `chatLayout` and let the shell place it:
- **Docked:** on the right side — its own column at 96rem (1536px) and wider, or under the Folio below that. Collapsed it becomes a vertical strip (when it owns a column) or a one-line ticker (when it sits under the Folio). The Chronicle picks the strip by itself whenever its container is under 7.5rem (120px) wide.
- **Floating drawer:** the shell adds `floating`, a `tall` toggle and a drag grip (`dragHandle`); use them yourself only if you place the drawer outside GameShell.

- **Chat is a context with a hue owner: the channels** (Foundations · Colour · Context ownership). A channel's `channel-*` colour marks only its tag and its speaker names; message text stays `ink`, and everything else is a glyph, a shape or a word (D-022).
- Item links keep their rarity colour inside their [brackets], so a coloured name in a message is never mistaken for a speaker.
- Five channels own a hue — Trade (amber), Guild (verdigris), Whispers (pale orchid), Raid (ember), Invites (azure); General, Help, System and Loot are neutral and are told apart by their tag word, which every line carries in the All feed.
- System and Loot lines are `lore`-italic in `ink-muted`; items inside are ItemLinks in their rarity colour. Loot replaces the separate Loot History box.
- A line that mentions you gets the neutral `surface-raised` wash and a 2px `ink` edge; `@you` is bold `ink`.
- **The Chronicle is always Compact** — the chat and combat log — and keeps its own metrics; neither a region's `data-density` nor the Compact lists setting changes it.
- The active channel tab takes a 2px `ink` bar; unread counts are `ink` on `surface-raised`, small rectangles at `radius-control` (a circle means presence, Foundations · Shape). By default new lines are announced politely (`aria-live`); `announce` narrows that to mentions and whispers, or turns it off (Accessibility notes).
- **Docked, it is a shell region on Level 0** (Foundations · Surfaces & Layering): `ground` with a `line` top rule, on the Folio's layer (`z-folio`), so the Folio's shadow never falls on it.
- **Floating, it is a Level 1 floating surface:** opaque `surface`, a `line-strong` edge, `radius-float` and `shadow-float`, never blurred or translucent. Its layer is `z-chat-float`: above the stage and the Folio, below every popover, dialog, toast and the tour. On small screens the bottom dock takes the same layer.
- Mention lines take the Level 2 wash (`surface-raised`); the composer's input is a well (`ground-deep`). The collapsed strip's label is `ink-muted`.
- **New lines never move what the player is reading** (Foundations · Motion · Live updates). The log follows the newest line only while the player is at its foot. Scrolled up, it keeps its place — the line being read stays put even as old lines leave the top — and a small "3 new lines" control waits over the log's foot, a Compact Button with `shadow-float`. Pressing it jumps to the latest and focuses the log. Sending a message returns to the foot. Key messages by a stable `id`.
- **Motion:** opened by the player, the Chronicle's body rises `space-2` and fades in over `duration-base` on `ease-enter`; closing is at once, since the frame becomes its bar and height never animates. New lines appear at once, without sliding in. The jump control fades in over `duration-fast`. Dragging the floating drawer follows the pointer with no transition. Under reduced motion all of it happens at once.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A region named "Chronicle"; the channels are a `tablist`; the log is a named, focusable list |
| Keyboard | Channels are one tab stop: Left and Right move and select. The log is focusable, for reading back; while new lines wait, the "new lines" control follows it in the tab order. Enter sends |
| Focus | `focus-ring` on tabs, the log, the composer and the toggles |
| Announced | `announce="all"` (the default): the log is a polite live region. `"mentions"`: only mentions and whispers to you, through the throttled announcer. `"off"`: nothing. Combat events never go here |
| Hover and tap | Nothing |
| Target size | Toggles and the send button are at least 24px; channel tabs are 40px high |
| Text scaling | The channel tabs scroll sideways inside their strip; lines wrap; docked under the Folio, it never takes more than 45% of the screen's height |
| Colour | Every line carries its channel's tag word; a mention adds an `ink` edge and bold `@you` |
| Motion | The body rises in over `duration-base` when the player opens it; closing, new lines and the jump are at once. At once under reduced motion |
