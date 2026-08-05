import { Component, Input } from '@angular/core';
import { AttributeType } from '../../../../models/enums/attributeType';
import {
  formatAttributeTooltip,
  formatAttributeType,
} from '../../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';

export interface AttributeTooltipData {
  attributeType: AttributeType;
}

@Component({
  selector: 'app-attribute-tooltip-panel',
  standalone: true,
  template: `
    <div
      [id]="tooltipId"
      role="tooltip"
      class="pointer-events-none w-64 rounded-md border border-light_gray bg-texture p-3 text-xs text-zinc-200 shadow-xl"
    >
      <div class="font-semibold text-primary">{{ attributeName }}</div>
      <p class="mt-1 leading-relaxed text-zinc-300">{{ description }}</p>
    </div>
  `,
})
export class AttributeTooltipPanelComponent {
  @Input({ required: true }) data!: AttributeTooltipData;
  @Input({ required: true }) tooltipId!: string;

  get attributeName(): string {
    return formatAttributeType(this.data.attributeType);
  }

  get description(): string {
    return formatAttributeTooltip(this.data.attributeType);
  }
}
