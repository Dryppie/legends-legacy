# TopBar

The top bar.

**Status:** Draft

The strip across the top of the stage: who you are, how you are doing, what you carry.

**Provide:** `title` (character name or screen title), `eyebrow` ("Lv. 42", a region), an optional `center` (a Track while a dungeon run or tower climb is in progress) and CurrencyPills as children. Pass `onMenu` to show the menu button on narrow screens.

- Title is `name-header` (Marcellus 24 / 30); eyebrow in `label` style, `gilt`.
- No health bar in the TopBar: HP belongs on combat screens. Keep the centre empty or to one Track; anything more belongs in the page.
