import { Component, Input } from '@angular/core';
import { ToolBonusType } from '../../../../models/item';
import {
  ToolBonusTooltipDefinition,
  toolBonusTooltip,
} from './tool-bonus-tooltip';

@Component({
  selector: 'app-tool-bonus-tooltip-panel',
  standalone: true,
  template: `
    <div
      [id]="tooltipId"
      role="tooltip"
      class="pointer-events-none w-72 rounded-md border border-light_gray bg-texture p-3 text-xs text-zinc-200 shadow-xl"
    >
      <div class="font-semibold text-primary">{{ definition.title }}</div>
      <p class="mt-1 leading-relaxed text-zinc-300">
        {{ definition.description }}
      </p>
    </div>
  `,
})
export class ToolBonusTooltipPanelComponent {
  @Input({ required: true }) bonusType!: ToolBonusType;
  @Input({ required: true }) tooltipId!: string;

  get definition(): ToolBonusTooltipDefinition {
    return (
      toolBonusTooltip(this.bonusType) ?? {
        title: 'Tool Bonus',
        description: 'A bonus applied while gathering with this tool.',
      }
    );
  }
}
