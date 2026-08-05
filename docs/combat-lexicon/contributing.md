# Contributing

## Adding or changing an entry

1. Start from the relevant file in [templates](templates/condition-template.md).
2. Use a lowercase kebab-case catalogue ID.
3. Verify behavior in engine code and tests; do not infer it from a name, tag, or tooltip.
4. Separate **Current Implementation** from **Canonical Target Behaviour**.
5. Link exact runtime IDs and repository paths.
6. Describe source/target ownership, stacks, reapplication, duration, tick phase, removal, prevention, resistance, and target restrictions.
7. Add interaction links in both affected entries.
8. Update the tables and status totals in this README and the [condition index](conditions/README.md).
9. Add tests before promoting Proposed/Partial behavior to Implemented.

## Status definitions

- **Implemented:** current behavior matches the canonical contract, with executable evidence.
- **Partially Implemented:** useful behavior exists but one or more material contract clauses differ.
- **Proposed:** canonical term is defined but has no adequate shared implementation.
- **Deprecated:** retained only for migration/history.
- **Unknown:** evidence is insufficient; use temporarily and record the question.

## Review checklist

- No duplicate catalogue IDs.
- All relative Markdown links resolve.
- Required template sections remain present.
- Formulas state rounding and ordering.
- Tags are not mistaken for behavior.
- Documentation-only work does not alter runtime, data, migrations, configuration, or deployment.
