# Toast

A brief outcome at the top centre of the stage.

**Status:** Draft — its look is Proposed (D-145), for review

The outcome of something the player did, when it isn't shown where they did it: "Settings saved", "Listed for 1,200 Cinders", "Couldn't save. Your change was undone." It arrives over everything, waits long enough to be read, and goes by itself. It is never the only place the outcome or its action lives.

**Use:**

```ts
this.toaster                                    // inject(LgToaster)
  .show({ heading: 'Sold Ashen Blade', text: 'For 1,200 Cinders', tone: 'success', action: 'Undo' })
  .closed.subscribe((why) => why === 'action' && this.buyBack());
```

**Provide:** `heading`, what happened in the words of Standards · States; `text`, a detail; `tone` (`info`, the default, `success`, `warning` or `danger`); `action`, one action's label; `duration` in ms (at least 6000, the default; `null` keeps it until it is dismissed). `closed` emits `timeout`, `dismissed` or `action`; `dismiss()` takes it away. `LgToaster.dismissAll()` clears them all.

**Where they show:** at the top centre of the element an `lg-toast-outlet` marks. The GameShell places one under its TopBar, so a screen in the shell needs none; without one they show at the top centre of the window.

- **A Level 2 float:** `surface-raised`, a `line-strong` hairline, `radius-float`, `shadow-float`, with a 2px start bar in its tone, as the Notice has. The heading is `body-compact` 600 in `ink`, the text a `caption` in `ink-muted`; the action is a link Button, and Dismiss a small Icon button. No icon of its own until the status icons are drawn: the words carry it.
- **The layer:** `z-toast`, above every page layer and dialog, the latest opened included.
- **Three at most,** newest on top, 24rem wide at most; the rest wait their turn, so none shows for less than its time.
- **Each stays at least 6 seconds,** and waits while the pointer or focus is in it. It can always be dismissed.
- **It never takes focus.** It is announced through `LgAnnouncer`: politely, or at once for `danger`. Escape dismisses it only while focus is in it, and focus goes back to where it came from.
- **Motion:** it rises in over `duration-base` and fades out over `duration-fast`; at once under reduced motion.

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Info | Edge: the azure bar | The heading | The heading and text (polite) |
| Success | Edge: the bone bar | "Saved", "Claimed 120 Cinders" | The heading and text (polite) |
| Warning | Edge: the amber bar | The heading | The heading and text (polite) |
| Danger | Edge: the ember bar | "Couldn't save. Your change was undone." | The heading and text (assertive) |
| Hover, focus | — (it waits) | — | Nothing |
| Leaving | Opacity: it fades | — | Nothing |

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | A `group` named by its heading; no live region of its own |
| Keyboard | Its action and Dismiss are buttons; Escape dismisses it while focus is in it |
| Focus | Never taken; given back when it goes while it holds it |
| Announced | Once, as it shows: polite, or assertive for `danger` |
| Hover and tap | It waits while hovered |
| Target size | Dismiss 32px; the action 24px at least |
| Text scaling | The words wrap; the buttons stay on the heading's line |
| Colour | The tone backs the words, never alone |
| Motion | Rises in, fades out; at once under reduced motion |

## Related components

- Notice — a lasting message at the head of a region
- Region state — a region with nothing to show
- Icon button — Dismiss
