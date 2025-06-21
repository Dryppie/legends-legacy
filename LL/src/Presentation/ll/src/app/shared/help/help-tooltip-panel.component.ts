import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HelpEntry } from './help.service';

@Component({
  selector: 'app-help-tooltip-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      role="tooltip"
      class="pointer-events-none z-10 flex max-w-xs flex-col items-center rounded-xl p-3 text-sm text-white shadow-lg transition-all duration-150 ease-out group-hover:opacity-100 group-focus:opacity-100"
    >
      <!-- bubble -->
      <div
        class="space-y-1 rounded-md border border-light_gray bg-zinc-800/90 p-1 text-zinc-200 shadow-lg backdrop-blur-sm"
      >
        <strong class="mb-1 block">{{ entry.title }}</strong>
        <p [innerHTML]="entry.body"></p>
      </div>
      <!-- arrow -->
      <div
        class="-z-10 mt-[-4px] h-2 w-2 rotate-45 border border-light_gray bg-zinc-800/90"
      ></div>
    </div>
  `,
})
export class HelpTooltipPanelComponent {
  @Input() entry!: HelpEntry;
}
