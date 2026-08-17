import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AbilityTooltipContainerDirective } from '../../../directives/ability-tooltip-container/ability-tooltip-container.directive';
import { isAbilityTargetSelector } from '../../../models/enums/targeting';
import {
  ABILITY_TARGET_BY_SELECTOR,
  AbilityTargetDefinition,
} from '../ability-target-glossary';

@Component({
  selector: 'app-ability-tags',
  imports: [NgFor, NgIf, AbilityTooltipContainerDirective],
  templateUrl: './ability-tags.component.html',
  styleUrl: './ability-tags.component.scss',
})
export class AbilityTagsComponent {
  private _tags: string[] = [];
  private _targets: AbilityTargetDefinition[] = [];

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

  @Input()
  set targets(value: readonly string[] | null | undefined) {
    const uniqueTargets = new Set<string>();
    const definitions: AbilityTargetDefinition[] = [];

    for (const target of value ?? []) {
      if (!isAbilityTargetSelector(target) || uniqueTargets.has(target)) {
        continue;
      }

      const definition = ABILITY_TARGET_BY_SELECTOR.get(target);
      if (!definition) continue;

      uniqueTargets.add(target);
      definitions.push(definition);
    }

    this._targets = definitions;
  }

  get displayTags(): readonly string[] {
    return this._tags;
  }

  get displayTargets(): readonly AbilityTargetDefinition[] {
    return this._targets;
  }

  trackTag(_index: number, tag: string): string {
    return tag;
  }

  trackTarget(_index: number, target: AbilityTargetDefinition): string {
    return target.selector;
  }
}
