# W3C/WCAG and Color Design Audit for LegendsLegacy

## 1. Executive summary

LegendsLegacy already has a recognizable visual identity: dark, textured fantasy surfaces with parchment-gold highlights. The recommended direction is an evolution of that system, not a redesign.

The biggest accessibility risks are structural rather than stylistic:

- Global CSS suppresses visible keyboard focus.
- Primary sidebar navigation uses focusable custom elements rather than native links or buttons.
- Mobile zoom is disabled.
- Shared modals, popovers, toasts, and several clickable `div` elements have incomplete keyboard or screen-reader behavior.
- Low-contrast secondary text and legacy error colors remain in active styles.
- Tokens exist, but Tailwind, component CSS, legacy authentication screens, rarity utilities, and newer combat colors do not consistently share one source of truth.

Overall assessment:

- **Visual identity:** Strong.
- **Responsive foundation:** Generally good in sampled screens.
- **Shared component foundation:** Promising.
- **WCAG 2.2 AA readiness:** Not yet conformant, primarily because of focus, semantics, scaling, and overlay behavior.
- **Color-system maturity:** Intermediate. Good tokens and primitives exist, but enforcement and role discipline are missing.

This was an analysis-only audit. No repository changes were made as part of the audit itself.

## 2. Existing visual and accessibility system

### What is working well

- The near-black texture, warm gold, serif headings, compact information density, and bordered panels form a coherent dark-fantasy language.
- The shared stylesheet already contains useful primitives for shells, panels, badges, controls, tables, modals, toasts, progress indicators, and item rarity.
- User preferences include 14/16/18px text sizing and Atkinson Hyperlegible/system-font options. This is unusually good accessibility infrastructure.
- Sampled mobile inventory at 390×844 reflowed without page-level horizontal overflow.
- Tavern leaderboards use native tables, captions, and row headers.
- Tabs implement `tablist`/`tab`, `aria-selected`, roving `tabindex`, and arrow-key handling.
- Dropdowns use native buttons and expose expanded, popup, option, selected, and disabled states.
- The help drawer has focus containment, Escape behavior, and focus restoration.
- Combat damage types generally pair color with labels or numeric content. Recent working-tree changes also move those colors into shared tokens.
- Several dynamic game areas already contain live regions and progress-bar semantics.
- Prominent animations in the tour, quest tracker, help system, tower, and essence views include reduced-motion handling.

### Finding classifications

Findings use the following classifications:

- **Accessibility:** Tied to WCAG, HTML, keyboard, or assistive-technology behavior.
- **Design system:** Inconsistency or maintainability concern, not automatically a WCAG failure.
- **Subjective:** Art direction or visual preference.

The audit uses WCAG 2.2 Level AA as its reference point. A complete conformance claim would still require automated testing, keyboard walkthroughs, screen-reader testing, zoom/reflow testing, and coverage of all application states. See the [WCAG 2.2 Recommendation](https://www.w3.org/TR/WCAG22/).

## 3. Current color catalog

A static scan found approximately 155 literal hex occurrences representing 88 distinct values across 47 frontend source files. This excludes some image assets and runtime compositing.

| Role | Current examples | Assessment |
| --- | --- | --- |
| Canvas/background | `#17171e`, `#0f1016`, index fallback `#171717` | Consistent dark foundation |
| Surfaces | Translucent near-black, white at 4% | Surface levels are too visually similar |
| Primary text | `#f6f0df` | Excellent contrast and on-brand |
| Muted text | `#a9a9b2` | Generally safe |
| Subtle text | `#71717a`, Tailwind `#6d6d6d`, Zinc 500 | Frequently too weak for small text |
| Primary accent | `#f9dca0`, strong `#fcd587` | Strong identity; over-applied |
| Danger | Token `#ff9aa2`, Tailwind `#ff7782`, legacy `#d72e34` | Fragmented; legacy value fails in common contexts |
| Success | `#41f1b6` | Strong contrast |
| Warning | `#ffbb55` | Strong contrast |
| Information | `#8ecbff` | Strong contrast |
| Rarity | Common through legacy, seven token colors | Good semantic registry, but compact views sometimes rely heavily on color |
| Combat damage | Physical, magical, bleed, burn, poison, shadow, untyped | Recent centralization is a good direction |
| Legacy/lore colors | Royal, ancient, blood, assorted component literals | Naming mixes appearance, lore, and functional roles |

Calculated contrast against `#17171e`:

- Primary text: 15.66:1
- Muted text: 7.65:1
- Subtle `#71717a`: 3.69:1 — fails normal text
- Gold `#f9dca0`: 13.39:1
- Danger `#ff9aa2`: 8.83:1
- Legacy danger `#d72e34`: 3.68:1 — fails normal text
- Tailwind light gray `#6d6d6d`: 3.45:1 — fails normal text

On lighter `#363636` surfaces, subtle and legacy values deteriorate further. Normal text generally needs 4.5:1; large text needs 3:1. See [W3C contrast technique G18](https://www.w3.org/WAI/WCAG22/Techniques/general/G18).

The default 12% white border is only about 1.4:1 against the canvas. That is acceptable when merely decorative, but not when the border is the sole way to identify a control or state. Meaningful component boundaries and states need 3:1 against adjacent colors. See [Understanding Non-text Contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast).

## 4. 60–30–10 evaluation

The current distribution roughly follows:

- **60%:** Near-black canvas and texture.
- **30%:** Translucent black panels, navigation, cards, and table regions.
- **10%:** Parchment gold.

The conceptual ratio is sound, but two problems weaken it:

1. The 60% and 30% layers are too close in luminance. Hierarchy often depends on hairline borders rather than clearly different surface levels.
2. Gold is used for headings, icons, active navigation, labels, values, currencies, outlines, and actions. It therefore behaves like a general foreground color rather than a deliberately scarce accent.

Recommended adjustment:

- Increase the perceptual separation among canvas, standard surface, elevated surface, hover, and selected surface.
- Reserve gold primarily for primary actions, selected navigation, focus, major progression, and high-value emphasis.
- Use neutral primary text for routine headings and values.
- Treat semantic colors—danger, warning, success, rarity, and combat—as separate functional namespaces, not as part of the decorative 10%.

## 5. Color and visual-system problems

### Accessibility problems

- Subtle grays are used at 9–10px and below 4.5:1 contrast.
- Authentication errors use `#d72e34`, which fails against both the main canvas and gray panels.
- Some compact rarity presentations communicate rarity mainly through name color.
- Low-opacity disabled content becomes difficult to read. Disabled controls are exempt from contrast requirements, but this remains a usability problem.
- Thin, low-contrast borders sometimes carry too much responsibility for showing selection or component boundaries.
- Text over textured or translucent backgrounds needs runtime verification; token-to-flat-background calculations alone are insufficient.

### Design-system problems

- `src/styles/tokens.css` is not the sole source of truth.
- `tailwind.config.js` duplicates colors and disagrees with tokens—for example, danger.
- Components contain literals, Tailwind Zinc values, legacy palette names, and opacity mixtures that bypass the token layer.
- The dead or currently unused rarity-color utility contains a second, lower-contrast rarity palette. It should eventually be removed or redirected to tokens.
- Appearance names such as `gray` and `light_gray` do not explain whether a color is text, border, surface, or disabled state.
- Google font imports include Mulish and Roboto even though the active token system primarily uses Poppins, Marcellus, and Atkinson Hyperlegible.

### Subjective art-direction concerns

- Gold is overused enough that genuinely important actions do not always feel special.
- Surface depth is slightly flat; panels can appear as many bordered rectangles cut from the same material.
- Gradients and glows are mostly restrained, but isolated legacy screens feel more "generic web fantasy" than the main game shell.
- The current direction should remain dense and game-like. Turning it into a spacious corporate dashboard would weaken its strongest quality.

## 6. Palette directions

### A. Dark stone and parchment gold — recommended

Example foundation:

- Canvas: `#0e0f14`
- Application background: `#17171e`
- Surface: `#202027`
- Elevated surface: `#292830`
- Primary text: `#f6f0df`
- Secondary text: approximately `#b8b5bc`
- Accent: retain `#f9dca0`
- Accent hover/focus: `#fcd587`

**Character:** The existing LegendsLegacy identity, with clearer depth and stricter gold usage.

**Advantages:** Lowest migration risk, strongest brand continuity, and excellent potential contrast.

**Tradeoff:** Success depends on enforcing role discipline; simply adding more gold shades would not solve the hierarchy problem.

### B. Midnight blue and ancient bronze

Foundation: blue-black canvas, slate-blue surfaces, muted bronze accent.

**Character:** Arcane expedition, cold dungeon stone, and aged metal.

**Advantages:** Clearer surface separation and a slightly more atmospheric world-map/dungeon feel.

**Tradeoff:** Materially changes the current brand and requires careful distinction between bronze accent, warning, and legendary rarity.

### C. Charcoal and arcane violet

Foundation: neutral charcoal, subtly violet elevated surfaces, and a light lavender accent.

**Character:** Mystical and magical.

**Advantages:** Clear interactive accent with strong dark-mode contrast.

**Tradeoff:** Competes with magical-damage and epic-rarity colors and loses some of the distinctive parchment aesthetic. This is better suited to a themed region or spell-system variant than the main application palette.

## 7. High-impact component examples

| Component | Current issue | Recommended treatment |
| --- | --- | --- |
| Global styles | `focus:outline-none` and cleared `:focus-visible` styling suppress keyboard focus | Add one highly visible shared focus-ring token; remove blanket suppression |
| Viewport configuration | `maximum-scale=1.0, user-scalable=0` prevents pinch zoom | Use a normal responsive viewport without disabling scaling |
| Sidebar navigation | Custom focusable hosts are rendered as generic elements, not links/buttons | Make `app-sidebar-item` render a native `<a>` for routes and `<button>` for actions |
| Shared modal | Dialog lacks an accessible name and evident shared focus containment/restoration | Add `aria-labelledby`, initial focus, focus trap, Escape, and trigger-focus restoration |
| Inventory item and popover | Clickable item container and hover-triggered details are incomplete for keyboard users | Use a button/link trigger; support focus and Escape; use tooltip versus dialog semantics according to content |
| Authentication button | `disabled` and `type` passed to the component host are not reliably reflected on the inner button | Bind native attributes to the actual `<button>` and expose loading/disabled states programmatically |
| Toast | Clickable `div`, no live-region role, fixed short timeout | Add `status`/`alert`, native dismiss button, and pause or extend timeout |
| Combat/health progress | Visible values are good, but some bars lack `progressbar` semantics | Add accessible names and `aria-valuenow`, `aria-valuemin`, and `aria-valuemax` where the bar conveys state |

## 8. WCAG and interaction recommendations

### Critical standards work

1. Restore visible focus globally. Keyboard users must be able to locate focus. See [Understanding Focus Visible](https://www.w3.org/WAI/WCAG22/Understanding/focus-visible).
2. Convert primary navigation and action hosts to native interactive elements. `tabindex="0"` does not supply link semantics or Enter/Space activation.
3. Remove the viewport scaling restriction. Preserve browser zoom and validate at 200% and 400%, including fixed-height game panes and dialogs. See [Understanding Reflow](https://www.w3.org/WAI/WCAG22/Understanding/reflow.html).

### Overlay and popup behavior

- Give every dialog an accessible name.
- Move focus into dialogs, contain it, close with Escape where appropriate, and restore focus afterward.
- Make hover content dismissible, hoverable, and available from keyboard focus. See [Understanding Content on Hover or Focus](https://www.w3.org/WAI/WCAG22/Understanding/content-on-hover-or-focus).
- Do not assign `role="dialog"` to simple informational tooltips.
- Replace character-tag, filter-result, inventory-item, and similar clickable `div` elements with buttons or links.

### Status and timing

- Announce success and informational toasts using `role="status"`.
- Use `role="alert"` only for important errors requiring immediate announcement.
- Provide an accessible dismiss control and avoid a fixed three-second reading window. See [Understanding Status Messages](https://www.w3.org/WAI/WCAG22/Understanding/status-messages).

### Color and content

- Raise secondary text colors to at least 4.5:1 in every surface context.
- Replace the legacy red and unify danger colors.
- Pair rarity and combat colors with names, icons, patterns, or abbreviations where users must distinguish categories. See [Understanding Use of Color](https://www.w3.org/WAI/WCAG22/Understanding/use-of-color).
- Add semantic values to progress bars while retaining visible numbers.
- Use `alt=""` for decorative profession/button ornamentation; give meaningful images appropriate alternatives.

### Motion and target sizing

- Add a global reduced-motion layer for remaining pulse, sweep, spin, fade-up, and dungeon animations.
- Preserve static loading/status feedback when animation is disabled.
- Audit compact popup actions and icon controls against the 24×24 CSS-pixel minimum or its spacing exceptions. See [Understanding Target Size Minimum](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum.html).

## 9. Recommended color architecture

Keep one controlled structural system, with separate game-semantic registries:

```text
Foundation
  canvas
  app-background
  surface-1
  surface-2
  surface-elevated
  surface-hover
  surface-selected

Text
  text-primary
  text-secondary
  text-muted
  text-disabled
  text-inverse

Borders and focus
  border-subtle
  border-default
  border-strong
  focus-ring

Accent
  accent-primary
  accent-hover
  accent-active
  accent-soft
  on-accent

Semantic feedback
  success / warning / danger / info
  each with foreground, background, and border roles

Game semantics
  rarity-common ... rarity-legacy
  health / barrier / healing
  damage-physical ... damage-shadow
```

Implementation guidance:

- Make CSS custom properties the canonical source.
- Point Tailwind colors at those properties instead of duplicating literals. RGB-channel variables can preserve Tailwind opacity syntax such as `bg-primary/10`.
- Map Angular Material theming to the same variables.
- Reserve lore names such as `ancient` or `blood` for actual game semantics, never general structural styling.
- Add an automated style check that rejects raw color literals in component files, with narrow exceptions for the token file, assets, and genuine data visualizations.
- Avoid uncontrolled token growth: add a token when it represents a reusable role, not every visual variation.
- Keep the current recent damage-token centralization and apply the same pattern to health, barrier, healing, rarity, and status feedback.

## 10. Prioritized roadmap and verification

### Critical

- Restore visible focus.
- Rebuild core sidebar items as native links/buttons.
- Permit browser zoom.

### High

- Standardize dialog focus, labeling, Escape, and restoration behavior.
- Make hover popovers keyboard accessible.
- Replace interactive `div` elements.
- Fix authentication button attribute forwarding and error announcement.
- Replace weak subtle text and legacy danger colors.
- Unify tokens and Tailwind.
- Make toasts accessible and controllable.

### Medium

- Improve surface-layer separation and reduce routine gold usage.
- Add redundant compact rarity indicators.
- Complete progress-bar semantics.
- Expand reduced-motion coverage.
- Audit very small metadata and compact target sizes.
- Add token-enforcement tooling.

### Low

- Remove unused font imports and obsolete palette utilities.
- Normalize decorative image alternatives and heading structure.
- Consolidate minor legacy gradients and one-off colors.

### Verification performed

- Inspected global styles, tokens, Tailwind configuration, shared components, and representative feature templates.
- Calculated contrast for core, semantic, rarity, and legacy colors.
- Sampled rendered Character Overview, Inventory, Essences, Combat, and Tavern states at desktop size.
- Sampled mobile inventory at 390×844.
- Confirmed a temporary Angular development build succeeded, then stopped it.

### Not performed

- Full axe/Lighthouse crawl.
- Screen-reader testing with NVDA, JAWS, or VoiceOver.
- Complete keyboard testing of every route and game state.
- Color-vision simulation or screenshot-based pixel contrast across all textured backgrounds.
- Backend tests, because no implementation was made.

### Repository impact

- No application files were changed by the audit.
- Existing user-owned working-tree changes were preserved.
- No migrations, configuration changes, dependency changes, deployment steps, or external environment changes are required.
