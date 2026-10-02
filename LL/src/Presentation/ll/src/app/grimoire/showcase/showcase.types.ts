import { Type } from '@angular/core';
import { ShowcaseEntryComponent } from './showcase-story.directive';

/** The design system's tiers (ANGULAR_DESIGN_SYSTEM_PLAN.md, section 5). They are the first part of a story's URL. */
export type ShowcaseTier = 'primitives' | 'components' | 'game' | 'shell';

export const SHOWCASE_TIERS: readonly { id: ShowcaseTier; label: string }[] = [
  { id: 'primitives', label: 'Primitives' },
  { id: 'components', label: 'Components' },
  { id: 'game', label: 'Game' },
  { id: 'shell', label: 'Shell' },
];

/**
 * One page of the showcase: a component (or a component and its parts) and its stories.
 *
 * The stories are `<ng-template scStory="…">` blocks in the entry component's template. Their order is the order
 * on the page, and each one has its own URL: /grimoire/<tier>/<slug>/<story slug>.
 */
export interface ShowcaseEntry {
  /** URL part, kebab-case: `button`. Keep it stable: snapshot baselines are named after it. */
  slug: string;
  /** The design system's name for it: `Button`. */
  name: string;
  tier: ShowcaseTier;
  /** One line: what it is (the plain subtitle of its README). */
  summary: string;
  /** The Angular components and directives the entry shows: `LgButtonComponent`. */
  covers: readonly string[];
  /** The guidelines page, from the frontend folder: `src/app/grimoire/primitives/button/README.md`. */
  readme?: string;
  component: Type<ShowcaseEntryComponent>;
}

/** `Pending label` → `pending-label`. */
export function showcaseSlug(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}
