# MotionSpecimen

The motion tokens, and every motion category beside its reduced twin.

**Status:** Draft

Foundations · Motion, played on parts from the game. Five parts, top to bottom:

- **Tokens.** The five durations, drawn to one scale, each with a mark that crosses its line in that time ("Play the durations"). The three easings as curves, with their control points, each played over `duration-reveal` so the shape can be seen.
- **Categories.** Each category in a row: its tokens and rules, then the Motion demo, then the same demo under `data-motion="reduced"`, which the system treats exactly like the operating system's setting. Feedback: Buttons and a NavRail to hover and press. State change: three Sigils to select and a "Show perks" toggle. Value change: the XP Meter and the Cinders after "Win the fight" and "Buy an Essence" — counted in the Motion column, at once in the reduced one. Spatial: a small GameShell whose rail drawer opens from the menu button, and the floating Chronicle opening from its bar. Reveal: a victory, once per fight. Live-update highlight: Bazaar prices and a guild payout that change by themselves, marked with the `changed` wash.
- **Live updates.** A Bazaar list fed through `LL.motion.useLiveList`. Turn on "Refresh every 2 seconds" and rest the pointer on a Buy button: rows keep their order, prices change in place with the mark, a sold listing stays, muted and "Sold", and new listings wait in the Panel head. Leave the list and the new order applies. Beside it, a Chronicle: scroll it up and let "3 lines arrive" — the line being read stays put while old lines leave the top, and "3 new lines" waits at the foot.
- **Loops.** The only two allowed, drawn in the preview's own styles since neither is a component: an indeterminate progress indicator while "Search the Bazaar" runs, and a fight's playback — a `live` HP Meter, a looping swing timer and floating damage numbers. Their reduced twins stop the sweeps and land the hits in place.
- **Audit.** `LL.motion.audit` run on the page itself. "Audit again" while a loop runs shows the loops counted, and allowed.

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | Each part is a `section` named by its heading. Each demo names its controls by what they do; the reduced twins add ", reduced motion" to their navigation, groups and lists. The easing curves are images named by their token and curve |
| Keyboard | Every control is an ordinary Tab stop; the NavRail, Lists, Sigils and the Chronicle keep their own keyboard models. Escape closes the small GameShell's rail drawer |
| Focus | `focus-ring` on every control. Opening the rail drawer moves focus into it, and closing returns it to the menu button |
| Announced | The Bazaar's status line is a polite `status`; the Chronicle's log is a polite live region. The indeterminate indicator is a `progressbar` named "Searching the Bazaar". Damage numbers are hidden, since the Meter carries the value |
| Hover and tap | Hover shows the Button and NavRail layers; resting the pointer on the Bazaar list holds it |
| Target size | Buttons 32px or more; rail items 30px |
| Text scaling | The three columns keep their proportions and their contents wrap at 115% and 130% |
| Colour | The live mark is a wash behind a value that is also printed; rarity also shows its code |
| Motion | Nothing moves until a control is pressed or the pointer rests on something. The Motion column moves by the tokens; the reduced column changes at once. The two loops run only while their search or fight runs, and stop under reduced motion |

## Related components

- Button — the command button
- NavRail — the main navigation
- Sigil — the hex stat badge
- Meter — the progress bar
- CurrencyPill — the currency amount
- GameShell — the screen frame
- Chronicle — chat and the game log
- Panel — the content box
- ListRow — the list row
