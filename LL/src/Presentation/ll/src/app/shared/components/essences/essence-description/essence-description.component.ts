import { Component, effect, Input, OnChanges } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { AbilityTooltipContainerDirective } from '../../../directives/ability-tooltip-container/ability-tooltip-container.directive';
import { EssenceEffectDto } from '../../../models/essence-system';
import { formatAttributeType } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

@Component({
    selector: 'app-essence-description',
    imports: [AbilityTooltipContainerDirective],
    templateUrl: './essence-description.component.html',
    styleUrls: ['./essence-description.component.scss']
})
export class EssenceDescriptionComponent implements OnChanges {
  private readonly magnitudeRange = 0.2;

  @Input() description = '';
  @Input() effects: EssenceEffectDto[] = [];
  safeDescription!: SafeHtml;

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
    this.safeDescription = this.sanitizer.bypassSecurityTrustHtml(
      this.buildDescriptionHtml(),
    );
  }

  private buildDescriptionHtml(): string {
    const placeholder = /\{([a-zA-Z]+)(\d*)\}/g;

    return this.escapeHtml(this.description).replace(
      placeholder,
      (token, kind: string, rawIndex: string) => {
        const effect = this.findEffect(kind, rawIndex ? Number(rawIndex) : 1);
        return effect ? this.buildTooltipSpan(effect) : token;
      },
    );
  }

  private findEffect(kind: string, index: number): EssenceEffectDto | undefined {
    const matching = this.flattenEffects(this.effects).filter((effect) =>
      this.matchesPlaceholderKind(kind, effect.type),
    );

    return matching[index - 1];
  }

  private flattenEffects(effects: EssenceEffectDto[]): EssenceEffectDto[] {
    return effects.flatMap((effect) => [
      effect,
      ...this.flattenEffects(effect.nestedEffects ?? []),
    ]);
  }

  private buildTooltipSpan(effect: EssenceEffectDto): string {
    const value = this.getDisplayValue(effect);
    const cssClass = this.getCssClass(effect.type);
    const base = effect.currentValue ?? 0;
    const scaling = effect.scaling?.[0];
    const attributeValue = scaling ? this.getAttributeValue(scaling.attribute) : 0;
    const coefficient = scaling?.coefficient ?? 0;
    const bonus = attributeValue * coefficient;
    const attribute = scaling ? formatAttributeType(scaling.attribute) : '';
    const unit = this.getUnit(effect.type);
    const hasRange = this.shouldDisplayRange(effect.type);

    return (
      `<span class="${cssClass}" ` +
      `data-base="${this.escapeAttribute(this.formatValue(base))}" ` +
      `data-attr="${this.escapeAttribute(attribute)}" ` +
      `data-attrvalue="${this.escapeAttribute(this.formatValue(attributeValue))}" ` +
      `data-scale="${this.escapeAttribute(this.formatValue(coefficient))}" ` +
      `data-bonus="${this.escapeAttribute(this.formatValue(bonus))}" ` +
      `data-display="${this.escapeAttribute(value)}" ` +
      `data-unit="${this.escapeAttribute(unit)}" ` +
      `data-range="${hasRange ? 'true' : 'false'}">` +
      `${this.escapeHtml(value)}</span>`
    );
  }

  private getDisplayValue(effect: EssenceEffectDto): string {
    const total = this.getTotal(effect);

    if (!this.shouldDisplayRange(effect.type)) {
      return this.formatValue(total);
    }

    const min = Math.floor(total * (1 - this.magnitudeRange));
    const max = Math.ceil(total * (1 + this.magnitudeRange));

    return `${min}-${max}`;
  }

  private getTotal(effect: EssenceEffectDto): number {
    return (
      effect.currentValue +
      (effect.scaling ?? []).reduce(
        (sum, scaling) =>
          sum + this.getAttributeValue(scaling.attribute) * scaling.coefficient,
        0,
      )
    );
  }

  private getAttributeValue(attribute: string): number {
    const overview = this.characterState.overview();
    const attributes = [
      ...(overview?.baseAttributes ?? []),
      ...(overview?.baseCombatAttributes ?? []),
    ];

    return (
      attributes.find(
        (x) => x.attributeType.toLowerCase() === attribute.toLowerCase(),
      )?.value ?? 0
    );
  }

  private shouldDisplayRange(effectType: string): boolean {
    return [
      'Damage',
      'Heal',
      'GrantBarrier',
      'ReflectDamage',
      'AbsorbDamage',
      'RestoreResource',
    ].includes(effectType);
  }

  private getCssClass(effectType: string): string {
    if (['Heal', 'GrantBarrier', 'AbsorbDamage'].includes(effectType)) {
      return 'heal';
    }

    if (['ModifyAttribute', 'ModifyStatusEffect'].includes(effectType)) {
      return 'mod';
    }

    return 'dmg';
  }

  private getUnit(effectType: string): string {
    return effectType === 'Heal'
      ? 'healing'
      : ['GrantBarrier', 'AbsorbDamage'].includes(effectType)
        ? 'Barrier'
        : 'damage';
  }

  private formatValue(value: number): string {
    return Number.isInteger(value)
      ? `${value}`
      : value.toFixed(2).replace(/\.?0+$/, '');
  }

  private matchesPlaceholderKind(kind: string, effectType: string): boolean {
    const normalizedKind = kind.toLowerCase();
    const normalizedType = effectType.toLowerCase();

    if (normalizedKind === 'damage') {
      return ['damage', 'reflectdamage'].includes(normalizedType);
    }

    if (normalizedKind === 'heal') {
      return ['heal'].includes(normalizedType);
    }

    if (normalizedKind === 'barrier') {
      return ['grantbarrier', 'absorbdamage'].includes(normalizedType);
    }

    if (normalizedKind === 'modify') {
      return ['modifyattribute', 'modifystatuseffect'].includes(normalizedType);
    }

    if (normalizedKind === 'resource') {
      return normalizedType === 'restoreresource';
    }

    if (normalizedKind === 'status') {
      return normalizedType === 'applystatus';
    }

    return normalizedType === normalizedKind;
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private escapeAttribute(value: string): string {
    return this.escapeHtml(value);
  }
}
