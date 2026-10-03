# TopBar

The top bar.

**Status:** Draft

The strip across the top of the stage: who you are, how you are doing, what you carry.

**Provide:** `heading` (the character's name, or a screen title), `eyebrow` ("Lv. 42", a region), an optional `lg-top-bar-center` — one "now" thing: a run's Track while a dungeon run or tower climb is in progress, else the pinned quest's Objective (D-110) — and CurrencyPills as the rest of its content, which sits at its end. Set `showMenu` to show the menu button on narrow screens; it emits `(menu)`, and inside a GameShell it opens the rail drawer. Its host is the bar (D-141).

```html
<lg-top-bar heading="Aldric Vane" eyebrow="Lv. 42" showMenu>
  <lg-top-bar-center><lg-track … /></lg-top-bar-center>
  <button lgCurrencyPill name="Cinders" [amount]="cinders"></button>
</lg-top-bar>
```

- Title is `name-header` (Marcellus 24 / 30); eyebrow in `label` style, `gilt`.
- No health bar in the TopBar: HP belongs on combat screens. Keep the centre to one "now" thing: the run's Track, else the pinned quest's Objective, whose full tracker opens in its popover (D-110). Anything more belongs in the page.
- In a GameShell from 60rem its insets are the Page's gutter, and over a backdrop it is `surface` with a hairline under it that continues the docked Chronicle's (D-115).
- In a GameShell under 40rem the centre moves to a second row, full width, and the TopBar grows to 6rem (D-114). Keep the heading short enough for the first row; it truncates.
- Larger text reflows its parts before anything overlaps: in a TopBar under 40rem a CurrencyPill's name gives way to its art (its own container query on `lg-topbar`), and a Track in a centre under 16rem drops its end labels (on `lg-topcenter`).
