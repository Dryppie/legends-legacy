import { Component, effect, Input, OnChanges } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { AbilityTooltipContainerDirective } from '../../../directives/ability-tooltip-container/ability-tooltip-container.directive';
import { AttributeDto } from '../../../models/Dtos/attributesDto';
import { EssenceEffectDto } from '../../../models/essence-system';
import { EssenceDescriptionFormatter } from './essence-description-formatter';

export function resolveEffectiveAttributeValue(
  attribute: string,
  combatAttributes: AttributeDto[],
  baseAttributes: AttributeDto[],
): number {
  const matches = (candidate: AttributeDto) =>
    candidate.attributeType.toLowerCase() === attribute.toLowerCase();

  return (
    combatAttributes.find(matches)?.value ??
    baseAttributes.find(matches)?.value ??
    0
  );
}

@Component({
  selector: 'app-essence-description',
  imports: [AbilityTooltipContainerDirective],
  templateUrl: './essence-description.component.html',
  styleUrls: ['./essence-description.component.scss'],
})
export class EssenceDescriptionComponent implements OnChanges {
  @Input() description = '';
  @Input() abilityName = '';
  @Input() effects: EssenceEffectDto[] = [];
  safeDescription!: SafeHtml;

  private readonly formatter = new EssenceDescriptionFormatter();

  constructor(
    private readonly sanitizer: DomSanitizer,
    private readonly characterState: CharacterStateService,
  ) {
    effect(() => {
      this.characterState.overview();
      this.refreshDescription();
    });
  }

  ngOnChanges(): void {
    this.refreshDescription();
  }

  private refreshDescription(): void {
    const html = this.formatter.format(
      this.description,
      this.effects,
      (attribute) => this.getAttributeValue(attribute),
      this.abilityName,
    );
    this.safeDescription = this.sanitizer.bypassSecurityTrustHtml(html);
  }

  private getAttributeValue(attribute: string): number {
    const overview = this.characterState.overview();
    return resolveEffectiveAttributeValue(
      attribute,
      overview?.baseCombatAttributes ?? [],
      overview?.baseAttributes ?? [],
    );
  }
}
