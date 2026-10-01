import { Component, Input, signal } from '@angular/core';
import { injectNobilityAppearance } from './nobility-appearance';

@Component({ selector: 'app-noble-decoration', template: `
  @if (visible()) {
    <span class="mr-1 inline-block align-middle text-[10px] leading-none text-primary" role="img" aria-label="Noble" title="Active Nobility">◆</span>
  }
` })
export class NobleDecorationComponent {
  private readonly id = signal('');
  @Input() set characterId(value: string) { this.id.set(value ?? ''); }
  get characterId(): string { return this.id(); }
  private readonly nobility = injectNobilityAppearance(this.id);
  readonly active = this.nobility.active;
  readonly visible = this.nobility.visible;
}
