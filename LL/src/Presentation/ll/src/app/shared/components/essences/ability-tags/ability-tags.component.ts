import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-ability-tags',
  imports: [NgFor, NgIf],
  templateUrl: './ability-tags.component.html',
  styleUrl: './ability-tags.component.scss',
})
export class AbilityTagsComponent {
  private _tags: string[] = [];

  @Input()
  set tags(value: readonly string[] | null | undefined) {
    const uniqueTags = new Map<string, string>();
    for (const tag of value ?? []) {
      const normalized = tag.trim();
      const key = normalized.toLowerCase();
      if (normalized && !uniqueTags.has(key)) uniqueTags.set(key, normalized);
    }

    this._tags = [...uniqueTags.values()];
  }

  get displayTags(): readonly string[] {
    return this._tags;
  }

  trackTag(_index: number, tag: string): string {
    return tag;
  }
}
