import { EssenceEffectDto } from '../../../models/essence-system';
import { formatAttributeType } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import {
  COMBAT_KEYWORDS,
  CombatKeywordDefinition,
} from './combat-keyword-glossary';
import { ABILITY_TARGETS } from '../ability-target-glossary';

type AttributeValueResolver = (attribute: string) => number;

export class EssenceDescriptionFormatter {
  private readonly magnitudeRange = 0.2;
  private readonly keywordByAlias = new Map<string, CombatKeywordDefinition>();
  private readonly keywordPattern: RegExp;

  constructor() {
    const definitions: readonly CombatKeywordDefinition[] = [
      ...COMBAT_KEYWORDS,
      ...ABILITY_TARGETS.map((target) => ({
        name: target.label,
        aliases: [...target.aliases],
        description: target.description,
      })),
    ];
    const aliases = definitions
      .flatMap((entry) => [entry.name, ...(entry.aliases ?? [])])
      .sort((left, right) => right.length - left.length);

    for (const entry of definitions) {
      for (const alias of [entry.name, ...(entry.aliases ?? [])]) {
        this.keywordByAlias.set(alias.toLowerCase(), entry);
      }
    }

    this.keywordPattern = new RegExp(
      `(^|[^a-zA-Z])(${aliases.map((alias) => this.escapeRegExp(alias)).join('|')})(?:\\(([^)]+)\\))?(?=$|[^a-zA-Z])`,
      'gi',
    );
  }

  format(
    description: string,
    effects: EssenceEffectDto[],
    resolveAttributeValue: AttributeValueResolver,
    abilityName = '',
  ): string {
    const replacements: string[] = [];
    const protect = (html: string): string => {
      const marker = `\uE000${replacements.length}\uE001`;
      replacements.push(html);
      return marker;
    };
    const flattenedEffects = this.flattenEffects(effects);
    const effectIndexes = new Map<string, number>();
    const placeholder = /\{([a-zA-Z]+)(\d*)\}/g;

    let text = description.replace(
      placeholder,
      (token, kind: string, rawIndex: string) => {
        const index = rawIndex ? Number(rawIndex) : 1;
        const scalarValue = this.formatScalarPlaceholder(
          flattenedEffects,
          kind,
          index,
        );
        if (scalarValue) return protect(this.escapeHtml(scalarValue));

        const effect = this.findEffect(flattenedEffects, kind, index);
        return effect
          ? protect(
              this.buildMagnitudeSpan(
                effect,
                this.getDisplayValue(effect, resolveAttributeValue),
                this.getCssClass(effect.type),
                this.getUnit(effect.type),
                resolveAttributeValue,
                undefined,
                undefined,
                undefined,
                this.shouldDisplayRange(effect.type),
                abilityName,
              ),
            )
          : token;
      },
    );

    text = this.decorateAuthoredMagnitudes(
      text,
      flattenedEffects,
      effectIndexes,
      resolveAttributeValue,
      protect,
      abilityName,
    );
    text = this.decorateKeywords(text, protect);
    text = this.decorateStandaloneDamageTypes(text, protect);

    let html = this.escapeHtml(text);
    replacements.forEach((replacement, index) => {
      html = html.replace(`\uE000${index}\uE001`, replacement);
    });

    return html;
  }

  private decorateAuthoredMagnitudes(
    text: string,
    effects: EssenceEffectDto[],
    effectIndexes: Map<string, number>,
    resolveAttributeValue: AttributeValueResolver,
    protect: (html: string) => string,
    abilityName: string,
  ): string {
    const magnitude =
      /\b(\d+(?:\.\d+)?(?:\s*-\s*\d+(?:\.\d+)?)?%)\s+((?:(?:ranged|melee|Physical|Magical|Shadow|Poison|Burn|Bleed)\s+){0,2}(?:Damage|Power|Max Health)|additional damage)\b/gi;

    return text.replace(
      magnitude,
      (
        visibleText: string,
        percentText: string,
        suffix: string,
        offset: number,
      ) => {
        const effectType = this.inferMagnitudeEffectType(text, suffix, offset);
        const effect = this.nextEffect(effects, effectType, effectIndexes);
        const scaling = effect?.scaling?.[0];
        if (!effect || !scaling) return visibleText;

        const percentages = percentText
          .replace(/%/g, '')
          .split('-')
          .map((value) => Number(value.trim()));
        const authoredMinimumCoefficient = percentages[0] / 100;
        const hasAuthoredCoefficientRange = percentages.length > 1;
        const authoredMaximumCoefficient =
          (percentages[1] ?? percentages[0]) / 100;
        const minimumCoefficient = scaling.coefficient;
        const ascensionMultiplier = authoredMinimumCoefficient
          ? minimumCoefficient / authoredMinimumCoefficient
          : 1;
        const maximumCoefficient =
          scaling.maximumCoefficient ??
          (hasAuthoredCoefficientRange
            ? authoredMaximumCoefficient * ascensionMultiplier
            : minimumCoefficient);
        const attributeValue = resolveAttributeValue(scaling.attribute);
        const base = effect.currentValue ?? effect.baseValue ?? 0;
        const rawMinimum = base + attributeValue * minimumCoefficient;
        const rawMaximum = base + attributeValue * maximumCoefficient;
        const previewMinimum = Math.floor(
          rawMinimum * (1 - this.magnitudeRange),
        );
        const previewMaximum = Math.ceil(
          rawMaximum * (1 + this.magnitudeRange),
        );
        const total = `${previewMinimum}-${previewMaximum}`;
        const unit = effectType === 'Heal' ? 'healing' : 'damage';
        const resultLabel =
          effectType === 'Damage' && /damage/i.test(suffix) ? suffix : unit;

        const scaledPercentText = this.formatCoefficientRange(
          minimumCoefficient,
          maximumCoefficient,
        );

        return protect(
          this.buildMagnitudeSpan(
            effect,
            total,
            effectType === 'Heal'
              ? 'heal'
              : this.withDamageTypeClass('dmg', suffix),
            unit,
            resolveAttributeValue,
            visibleText.replace(percentText, scaledPercentText),
            scaledPercentText,
            (minimumCoefficient + maximumCoefficient) / 2,
            true,
            abilityName,
            resultLabel,
          ),
        );
      },
    );
  }

  private decorateKeywords(
    text: string,
    protect: (html: string) => string,
  ): string {
    return text.replace(
      this.keywordPattern,
      (_match, prefix: string, alias: string, value: string | undefined) => {
        const definition = this.keywordByAlias.get(alias.toLowerCase());
        if (!definition) return _match;

        const visibleText = value ? `${alias}(${value})` : alias;
        const description = this.keywordDescription(definition, value);
        const detail = definition.descriptionWithValue
          ? ''
          : this.keywordValueDetail(definition, value);
        const cssClass = this.withDamageTypeClass('keyword', definition.name);
        const span =
          `<span class="${cssClass}" tabindex="0" data-tooltip-kind="keyword" ` +
          `data-title="${this.escapeAttribute(definition.name)}" ` +
          `data-description="${this.escapeAttribute(description)}" ` +
          `data-detail="${this.escapeAttribute(detail)}" ` +
          `aria-label="${this.escapeAttribute(`${definition.name}: ${description} ${detail}`.trim())}">` +
          `${this.escapeHtml(visibleText)}</span>`;

        return `${prefix}${protect(span)}`;
      },
    );
  }

  private decorateStandaloneDamageTypes(
    text: string,
    protect: (html: string) => string,
  ): string {
    const damageTypePhrase =
      /\b(Physical|Magical|Shadow)(\s+(?:(?:Melee|Ranged)\s+)?Damage)\b/gi;

    return text.replace(
      damageTypePhrase,
      (visibleText: string, damageType: string) =>
        protect(
          `<span class="${this.withDamageTypeClass('damage-type', damageType)}">` +
            `${this.escapeHtml(visibleText)}</span>`,
        ),
    );
  }

  private withDamageTypeClass(baseClass: string, text: string): string {
    const damageType = text.match(
      /\b(Physical|Magical|Shadow|Poison|Burn|Bleed)\b/i,
    )?.[1];

    return damageType
      ? `${baseClass} damage-type-${damageType.toLowerCase()}`
      : baseClass;
  }

  private buildMagnitudeSpan(
    effect: EssenceEffectDto,
    total: string,
    cssClass: string,
    unit: string,
    resolveAttributeValue: AttributeValueResolver,
    visibleText = total,
    scaleDisplay?: string,
    coefficientOverride?: number,
    hasRange = true,
    abilityName = '',
    resultLabel = unit,
  ): string {
    const base = effect.currentValue ?? effect.baseValue ?? 0;
    const scaling = effect.scaling?.[0];
    const attributeValue = scaling
      ? resolveAttributeValue(scaling.attribute)
      : 0;
    const coefficient = coefficientOverride ?? scaling?.coefficient ?? 0;
    const bonus = attributeValue * coefficient;
    const attribute = scaling ? formatAttributeType(scaling.attribute) : '';
    const title =
      abilityName ||
      (unit === 'healing'
        ? 'Estimated healing'
        : unit === 'damage'
          ? 'Estimated damage'
          : `Estimated ${unit.toLowerCase()}`);
    const rollDisplay = hasRange
      ? `±${this.formatPercent(this.magnitudeRange)}`
      : 'Fixed';

    return (
      `<span class="${cssClass}" tabindex="0" data-tooltip-kind="magnitude" ` +
      `data-title="${this.escapeAttribute(title)}" ` +
      `data-base="${this.escapeAttribute(this.formatValue(base))}" ` +
      `data-attr="${this.escapeAttribute(attribute)}" ` +
      `data-attrvalue="${this.escapeAttribute(this.formatValue(attributeValue))}" ` +
      `data-scale="${this.escapeAttribute(this.formatValue(coefficient))}" ` +
      `data-scale-display="${this.escapeAttribute(scaleDisplay ?? this.formatPercent(coefficient))}" ` +
      `data-bonus="${this.escapeAttribute(this.formatValue(bonus))}" ` +
      `data-display="${this.escapeAttribute(total)}" ` +
      `data-unit="${this.escapeAttribute(unit)}" ` +
      `data-result-label="${this.escapeAttribute(resultLabel)}" ` +
      `data-roll-display="${this.escapeAttribute(rollDisplay)}" ` +
      `data-range="${hasRange ? 'true' : 'false'}" ` +
      `aria-label="${this.escapeAttribute(`${title}: ${total} ${unit}.`)}">` +
      `${this.escapeHtml(visibleText)}</span>`
    );
  }

  private inferMagnitudeEffectType(
    description: string,
    suffix: string,
    offset: number,
  ): 'Damage' | 'Heal' {
    if (suffix.toLowerCase().includes('damage')) return 'Damage';

    const precedingText = description
      .slice(Math.max(0, offset - 70), offset)
      .toLowerCase();
    return /heal|healing|restore|regenerat/.test(precedingText)
      ? 'Heal'
      : 'Damage';
  }

  private nextEffect(
    effects: EssenceEffectDto[],
    type: 'Damage' | 'Heal',
    effectIndexes: Map<string, number>,
  ): EssenceEffectDto | undefined {
    const matching = effects.filter((effect) =>
      type === 'Damage'
        ? ['damage', 'reflectdamage'].includes(effect.type.toLowerCase())
        : effect.type.toLowerCase() === 'heal',
    );
    if (matching.length === 0) return undefined;

    const index = effectIndexes.get(type) ?? 0;
    effectIndexes.set(type, index + 1);
    return matching[Math.min(index, matching.length - 1)];
  }

  private keywordDescription(
    definition: CombatKeywordDefinition,
    value: string | undefined,
  ): string {
    return value && definition.descriptionWithValue
      ? definition.descriptionWithValue.replaceAll('{value}', value)
      : definition.description;
  }

  private keywordValueDetail(
    definition: CombatKeywordDefinition,
    value: string | undefined,
  ): string {
    if (!value) return '';

    switch (definition.valueMeaning ?? 'none') {
      case 'seconds':
        return `Duration: ${value} seconds.`;
      case 'potency':
        return `Potency: ${value}.`;
      case 'stacks':
        return `Applies ${value} stacks.`;
      case 'charges':
        return `Grants ${value} charges.`;
      case 'percent':
        return `Magnitude: ${value}%.`;
      default:
        return `Value: ${value}.`;
    }
  }

  private findEffect(
    effects: EssenceEffectDto[],
    kind: string,
    index: number,
  ): EssenceEffectDto | undefined {
    const matching = effects.filter((effect) =>
      this.matchesPlaceholderKind(kind, effect.type),
    );
    return matching[index - 1];
  }

  private formatScalarPlaceholder(
    effects: EssenceEffectDto[],
    kind: string,
    index: number,
  ): string | undefined {
    const normalizedKind = kind.toLowerCase();
    const coefficientField =
      normalizedKind === 'eventscaling'
        ? 'eventMagnitudeCoefficient'
        : normalizedKind === 'conditionscaling'
          ? 'conditionScalingCoefficient'
          : normalizedKind === 'statusscaling'
            ? 'statusScalingCoefficient'
            : undefined;

    if (coefficientField) {
      const matching = effects.filter(
        (effect) => (effect[coefficientField] ?? 0) !== 0,
      );
      const coefficient = matching[index - 1]?.[coefficientField];
      return coefficient
        ? this.formatPercent(Math.abs(coefficient))
        : undefined;
    }

    if (normalizedKind === 'scaling') {
      const matching = effects.filter(
        (effect) => (effect.scaling?.[0]?.coefficient ?? 0) !== 0,
      );
      const coefficient = matching[index - 1]?.scaling?.[0]?.coefficient;
      return coefficient
        ? this.formatPercent(Math.abs(coefficient))
        : undefined;
    }

    const summonMultiplierField =
      normalizedKind === 'summonpower'
        ? 'summonPowerMultiplier'
        : normalizedKind === 'summonhealth'
          ? 'summonHealthMultiplier'
          : undefined;
    if (summonMultiplierField) {
      const matching = effects.filter(
        (effect) =>
          effect.type.toLowerCase() === 'summon' &&
          (effect[summonMultiplierField] ?? 0) > 0,
      );
      const multiplier = matching[index - 1]?.[summonMultiplierField];
      return multiplier ? this.formatPercent(multiplier) : undefined;
    }

    if (normalizedKind !== 'duration') return undefined;

    const matching = effects.filter(
      (effect) => (effect.durationSeconds ?? 0) > 0,
    );
    const duration = matching[index - 1]?.durationSeconds;
    if (!duration) return undefined;

    return `${this.formatValue(duration)} ${duration === 1 ? 'second' : 'seconds'}`;
  }

  private flattenEffects(effects: EssenceEffectDto[]): EssenceEffectDto[] {
    return effects.flatMap((effect) => [
      effect,
      ...this.flattenEffects(effect.nestedEffects ?? []),
    ]);
  }

  private getDisplayValue(
    effect: EssenceEffectDto,
    resolveAttributeValue: AttributeValueResolver,
  ): string {
    const total =
      (effect.currentValue ?? effect.baseValue ?? 0) +
      (effect.scaling ?? []).reduce(
        (sum, scaling) =>
          sum + resolveAttributeValue(scaling.attribute) * scaling.coefficient,
        0,
      );
    if (!this.shouldDisplayRange(effect.type)) return this.formatValue(total);

    const minimum = Math.floor(total * (1 - this.magnitudeRange));
    const maximum = Math.ceil(total * (1 + this.magnitudeRange));
    return `${minimum}-${maximum}`;
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
        : ['ModifyAttribute', 'ModifyStatusEffect'].includes(effectType)
          ? 'value'
          : effectType === 'RestoreResource'
            ? 'resource'
            : 'damage';
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

  private matchesPlaceholderKind(kind: string, effectType: string): boolean {
    const normalizedKind = kind.toLowerCase();
    const normalizedType = effectType.toLowerCase();
    if (normalizedKind === 'damage') {
      return ['damage', 'reflectdamage'].includes(normalizedType);
    }
    if (normalizedKind === 'heal') return normalizedType === 'heal';
    if (normalizedKind === 'barrier') {
      return ['grantbarrier', 'absorbdamage'].includes(normalizedType);
    }
    if (normalizedKind === 'modify') {
      return ['modifyattribute', 'modifystatuseffect'].includes(normalizedType);
    }
    if (normalizedKind === 'resource')
      return normalizedType === 'restoreresource';
    if (normalizedKind === 'status') return normalizedType === 'applystatus';
    return normalizedType === normalizedKind;
  }

  private formatValue(value: number): string {
    return Number.isInteger(value)
      ? `${value}`
      : value.toFixed(2).replace(/\.?0+$/, '');
  }

  private formatPercent(coefficient: number): string {
    return `${this.formatValue(coefficient * 100)}%`;
  }

  private formatCoefficientRange(minimum: number, maximum: number): string {
    const minimumDisplay = this.formatPercent(minimum);
    const maximumDisplay = this.formatPercent(maximum);
    if (maximumDisplay === minimumDisplay) return minimumDisplay;

    return `${minimumDisplay}-${maximumDisplay}`;
  }

  private escapeRegExp(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
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
