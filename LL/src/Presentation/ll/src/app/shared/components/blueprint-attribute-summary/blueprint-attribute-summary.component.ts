import { NgFor, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { BlueprintItemMetadata } from '../../models/item';
import { AttributeTypeFormatPipe } from '../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

export function getBlueprintContributedAttributes(
  blueprint: BlueprintItemMetadata,
): string[] {
  return Object.entries(blueprint.bonusStatProfile ?? {})
    .filter(([, weight]) => Number.isFinite(weight) && weight > 0)
    .sort(
      ([leftType, leftWeight], [rightType, rightWeight]) =>
        rightWeight - leftWeight || leftType.localeCompare(rightType),
    )
    .map(([attributeType]) => attributeType);
}

@Component({
  selector: 'app-blueprint-attribute-summary',
  imports: [NgFor, NgIf, AttributeTypeFormatPipe],
  templateUrl: './blueprint-attribute-summary.component.html',
})
export class BlueprintAttributeSummaryComponent {
  @Input({ required: true }) blueprint!: BlueprintItemMetadata;

  get contributedAttributes(): string[] {
    return getBlueprintContributedAttributes(this.blueprint);
  }
}
