# Soaked

`condition.soaked` is a harmful, permanent shared stack pool used by encounter mechanics.

- Applications add their authored stack value to the existing pool.
- The pool is capped at 10 stacks.
- Soaked has no natural duration.
- Ward consumes one charge to negate an entire Soaked application.
- Cleanse removes the complete Soaked pool.
- Soaked has no intrinsic stat modifier or damage; abilities may target, scale from, or consume its stacks.

Nhalia's Moonfall selects the living enemy with the greatest number of Soaked stacks. Entering Low Tide consumes up to all 10 stacks from every affected enemy and deals its authored damage per consumed stack.

Implementation: `StandardConditionType.Soaked` and the standard-condition application and consumption paths in `FastCombatEngine`.
