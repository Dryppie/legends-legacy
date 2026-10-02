import { Injectable, computed, signal } from '@angular/core';
import { ShowcaseStoryDirective } from './showcase-story.directive';
import { ShowcaseEntry, ShowcaseTier } from './showcase.types';

export type ShowcaseDensity = '' | 'comfortable' | 'standard' | 'compact';
export type ShowcaseTextSize = 'default' | 'large' | 'extra-large';
export type ShowcaseMotion = 'full' | 'reduced';
export type ShowcaseFrame = 'bare' | 'page';
export type ShowcaseGround = 'ground' | 'surface' | 'backdrop';

/** The toolbar's choices. They live in the URL's query (`?density=compact&text=large`), so a link keeps them. */
export interface ShowcaseOptions {
  density: ShowcaseDensity;
  text: ShowcaseTextSize;
  motion: ShowcaseMotion;
  frame: ShowcaseFrame;
  ground: ShowcaseGround;
}

export const SHOWCASE_DEFAULTS: ShowcaseOptions = {
  density: '',
  text: 'default',
  motion: 'full',
  frame: 'bare',
  ground: 'ground',
};

export interface LoadedShowcaseEntry {
  entry: ShowcaseEntry;
  stories: readonly ShowcaseStoryDirective[];
}

/** What the showcase shell has loaded, shared with its pages. Provided by the showcase route only. */
@Injectable()
export class ShowcaseStore {
  readonly entries = signal<readonly LoadedShowcaseEntry[]>([]);
  readonly options = signal<ShowcaseOptions>(SHOWCASE_DEFAULTS);
  readonly ready = signal(false);

  readonly storyCount = computed(() =>
    this.entries().reduce((n, e) => n + e.stories.length, 0),
  );

  find(
    tier: string | null,
    slug: string | null,
  ): LoadedShowcaseEntry | undefined {
    return this.entries().find(
      (e) => e.entry.tier === tier && e.entry.slug === slug,
    );
  }

  inTier(tier: ShowcaseTier): readonly LoadedShowcaseEntry[] {
    return this.entries().filter((e) => e.entry.tier === tier);
  }
}
