import { Component, Input } from '@angular/core';

export interface TemperingBonusTooltipData {
  attributeName: string;
  originalAmount: string;
  bonusAmount: string;
  finalAmount: string;
}

@Component({
  selector: 'app-tempering-bonus-tooltip-panel',
  standalone: true,
  template: `
    <div
      [id]="tooltipId"
      role="tooltip"
      class="pointer-events-none w-56 rounded-md border border-light_gray bg-texture p-3 text-xs text-zinc-200 shadow-xl"
    >
      <div class="font-semibold text-primary">Tempered attribute</div>
      <div class="mt-1 text-zinc-300">{{ data.attributeName }}</div>
      <dl class="mt-2 grid grid-cols-[1fr_auto] gap-x-4 gap-y-1 tabular-nums">
        <dt class="text-secondary">Original</dt>
        <dd class="text-right text-white">{{ data.originalAmount }}</dd>
        <dt class="text-secondary">Upgrade</dt>
        <dd class="text-right text-emerald-400">{{ data.bonusAmount }}</dd>
        <dt class="border-t border-white/10 pt-1 text-secondary">Final</dt>
        <dd class="border-t border-white/10 pt-1 text-right text-primary">
          {{ data.finalAmount }}
        </dd>
      </dl>
    </div>
  `,
})
export class TemperingBonusTooltipPanelComponent {
  @Input({ required: true }) data!: TemperingBonusTooltipData;
  @Input({ required: true }) tooltipId!: string;
}
