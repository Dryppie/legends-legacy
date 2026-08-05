# Design Principles

## One meaning per term

A canonical term has one mechanical meaning. Record aliases and deprecations instead of using several verbs for the same operation.

## One source of truth

A shared condition keeps its core behavior regardless of which ability applies it. Abilities may configure parameters or explicit modifiers.

## No hidden mechanics

Every combat outcome modifier is documented or identified as an exception. A display label or tag is not implementation evidence.

## Conditions define reusable behavior

Conditions use common targeting, timing, stacking, removal, resistance, and event rules rather than depending on one ability for their base meaning.

## Avoid duplicate conditions

Do not create synonyms for the same behavior. Create a distinct condition only when its mechanics and gameplay role materially differ.

## Prefer resistance over blanket immunity

Bosses should normally use duration reduction, magnitude caps, resistance, or diminishing returns. Complete immunity is an explicit encounter rule.

## Do not add decorative mechanics

Do not catalogue movement-only Root or movement Slow while combat has no meaningful movement model.

## Avoid universal reaction matrices

Elemental reactions belong to explicit abilities, passives, encounters, or rules; conditions do not automatically react with every other condition.

## Evidence rules

- Separate current implementation from canonical target behavior.
- Make source, target, application identity, event order, rounding, and expiry boundaries explicit.
- Keep namespaced stable IDs lowercase with kebab-case concept segments.
- Treat tags as classification unless code consumes them.
- Express canonical behavior as focused engine tests before marking it Implemented.

See [core concepts](core-concepts.md) and [contributing](contributing.md).
