# Governance

How the system changes: who names what, what a status means, where new documentation goes, and the two templates every new section and component page starts from. Decisions are recorded in Governance · Decision Log; the state of every part and the revision queue are in Governance · Audit & consolidation map.

## Rules

**Must**
- Write every new section from the section template and every new component page from the component README template (below).
- Give every part a status: Stable, Revising, Draft or Deprecated.
- Record in the Decision Log any change that adds, reverses or narrows a rule.
- Keep the layer model: a layer draws only on the layers above it (README).
- Fill in a component's Accessibility notes (the README template, below) before it moves past Draft.

**Should**
- Change the highest layer that solves the problem — a token before a component, a component before a pattern, a pattern before an archetype.
- Put component detail on the component's own page, not in a section.
- Keep a section growing inside its own file before splitting it (the section budget, below).

**Never**
- Rename an evocative component; give it a better subtitle instead.
- Remove a part without first marking it Deprecated and naming its replacement.

## Naming

- **Evocative names stay.** Folio, Ledger, Chronicle, Sigil, Constellation, Emblem, Stage keep their names, and each carries a plain subtitle: "Folio — the detail panel".
- **New components get plain descriptive names:** LoadoutSlot, SearchField, CurrencyPill.
- **Compositions** are named `Pattern<Name>` (Patterns) and `Archetype<Name>` (Page Archetypes), after the problem or the screen type.
- **Tokens:** palette primitives are named for their colour (`umber-900`, `hue-rarity-epic`); every other token is named for its role, never its hue: `ink-muted`, not `beige`. The full convention is in Foundations · Colour.
- **Code:** CSS classes use the `lg-` prefix; Angular selectors are `lg-*`.

## Statuses

| Status | Means | Use it in new work? |
| --- | --- | --- |
| Draft | Designed and documented, not yet used by a shipped screen. It may still change. | Yes, expecting change |
| Stable | Used in the shipped game. Its look and API change only through a Decision Log entry. | Yes |
| Revising | In use and being changed; its page says what is moving. | Yes, following its page |
| Deprecated | Being retired; its page names the replacement. | No |

Everything starts at Draft. At this restructure every component, pattern and archetype is Draft, because none is used by a shipped screen yet.

## Where documentation goes

- **Sections** are the Markdown files under `docs/`, listed in reading order in `design-system.json` → `docs.sections`. Titles come from each file's first `#` heading; parts of a larger topic are titled "Parent · Child" (Foundations · Colour). Keep the numeric prefixes so path order is reading order, and add a new section to `docs.sections` in its path position.
- **Child pages.** A one-file section gains a child page in a folder of the same name: `docs/01-principles/01-anti-generic-guardrails.md` sorts straight after `docs/01-principles.md`.
- **The section list.** The Claude Design page showed at most 24 sections; that limit ended with the move to the repository (D-091). Grow a section inside its file before adding one, and put a narrower topic in a child page of its parent. The merge recorded in D-013 stands.
- **Component pages** have no such limit: each part's `README.md` sits beside its code, in `src/app/grimoire/<tier>/<name>/`, and its states are stories in its showcase entry (`src/app/grimoire/showcase/entries/<name>.showcase.ts`, D-129). Put detail there.
- **Patterns and archetypes** are written down in Patterns and Page Archetypes, with the game screen that uses them as their worked example.
- **The existing component pages** open with their plain subtitle and status. Each adopts the full component template the next time it is revised.

## Changing the system

1. Read the Decision Log for anything that already settles it.
2. Find the highest layer that solves it (README, the layer model).
3. Write or update the section or component page from its template; a new part starts at Draft.
4. Run the review checklists in Principles and Principles · Anti-generic guardrails, and the tests in Foundations · Accessibility · Testing.
5. If a rule is added, reversed or narrowed, add a Decision Log entry.
6. Run `scripts/check.mjs`, and commit the change with its backlog id (DS-xxx) in the message. Git keeps the history.

## Section template

Copy this for a new section. The README, Principles, Governance, the Decision Log and the Audit explain the system rather than rule a part of it, so they are exempt. The Components index ends with related sections instead of related components, because its tables already list every component.

```markdown
# <Parent> · <Section name>

<Purpose: one or two sentences — what this section governs and why it exists.>

## Rules

**Must**
- <Required. Breaking it is a bug.>

**Should**
- <The default. Departing from it needs a reason, written down where you depart.>

**Never**
- <Forbidden.>

## Tokens used

| Token | Role here |
| --- | --- |
| `<token-name>` | <what it does in this section> |

## Do and don't

| Do | Don't |
| --- | --- |
| <a concrete right example> | <the matching wrong one> |

## Related components

- <Name> — <plain subtitle>
```

## Component README template

Copy this to `src/app/grimoire/<tier>/<name>/README.md` for a new component, or when an existing page is next revised. The component's inputs, outputs and slots are in its `.component.ts`; the README says how to use them, by their Angular names.

```markdown
# <Name>

<Plain-language subtitle, as one short sentence: "The detail panel." It is the component's summary in the README's index and its showcase entry.>

**Status:** <Stable | Revising | Draft | Deprecated — for Deprecated, name the replacement>

## When to use

- <The job it does, with a real example from the game.>

## When not to use

- <The nearest wrong use, and the part to use instead.>

## Anatomy

1. **<Part>** — <what it holds, and the token or style it takes>

## Supported states

| State | Channel | Words | Screen readers hear |
| --- | --- | --- | --- |
| Default | <The part's own look> | <Its label> | <Its name and role> |
| Hover | Fill: the `surface-raised` wash | — | Nothing |
| Focus-visible | Edge: `focus-ring` | — | <Name, state and description> |
| <Selected, Locked, Equipped, Empty, Loading…> | <Its channel and tokens> | <The exact words> | <The announcement> |

List only the states it has, by the names in Standards · States, with that page's channels and words. A blocked state names its reason input and stays focusable (D-087).

## Density variants

| Variant | Input | Use in |
| --- | --- | --- |
| <Default> | | |

## Content rules

- <Length, case, what the label says, how numbers are formatted.>

## Accessibility notes

| Field | Notes |
| --- | --- |
| Role and name | <The element and its ARIA role; where its accessible name comes from ("the label", `aria-label="Send"`)> |
| Keyboard | <Its tab stops (one per widget), the keys and what they do, what Escape closes> |
| Focus | <Where focus goes when it opens, closes or removes a row; the ring is `focus-ring`> |
| Announced | <What a screen reader hears: states, full numbers, rarity names; any live announcement and its politeness> |
| Hover and tap | <Anything shown on hover, and how it opens on focus and pins on tap — or "Nothing"> |
| Target size | <Its smallest target at each density: 24px minimum, 32px for primary actions> |
| Text scaling | <What reflows or scrolls at 115% and 130%> |
| Colour | <The second carrier for every colour meaning> |
| Motion | <What moves, with its motion tokens and category, and what reduced motion does — or "Nothing"> |

Fill in every field; write "Nothing" rather than leaving one out (Foundations · Accessibility).

## Related components

- <Name> — <plain subtitle>
```
