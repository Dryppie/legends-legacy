import {
  Directive,
  TemplateRef,
  booleanAttribute,
  computed,
  inject,
  input,
  viewChildren,
} from '@angular/core';
import { showcaseSlug } from './showcase.types';

/**
 * One story of a showcase entry: `<ng-template scStory="Locked" notes="…" width="20rem">…</ng-template>`.
 *
 * The template is rendered by the showcase page, not where it is declared, so it can use the entry component's
 * fields and the components the entry imports.
 */
@Directive({ selector: 'ng-template[scStory]' })
export class ShowcaseStoryDirective {
  /** The story's name, in sentence case: `Pending label`. Its slug is part of the URL and the snapshot's name. */
  readonly name = input.required<string>({ alias: 'scStory' });
  /** One or two sentences under the story's name: what to look at. */
  readonly notes = input<string>();
  /** The frame's width (any CSS length, or px as a number). Without it the story takes the content width. */
  readonly width = input<string | number>();
  /** The frame's height, for parts that fill their parent (GameShell, Page). */
  readonly height = input<string | number>();
  /** No padding inside the frame: for parts that run edge to edge (GameShell, Page, Stage). */
  readonly flush = input(false, { transform: booleanAttribute });
  /** Leave the story out of the snapshot run (it moves on its own, or depends on the clock). */
  readonly noSnapshot = input(false, { transform: booleanAttribute });

  readonly template = inject<TemplateRef<unknown>>(TemplateRef);
  readonly slug = computed(() => showcaseSlug(this.name()));
}

/**
 * Base class of every showcase entry component. The entry's template holds only `ng-template scStory` blocks;
 * the showcase reads them from `stories`.
 */
@Directive()
export abstract class ShowcaseEntryComponent {
  readonly stories = viewChildren(ShowcaseStoryDirective);
}
