import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-market-place-resources',
  standalone: true,
  imports: [],
  templateUrl: './market-place-resources.component.html',
})
export class MarketPlaceResourcesComponent {
  private readonly catalystNames = new Set([
    'fury heart',
    'arcane focus',
    "executioner's mark",
    'aegis runestone',
    'warden sigil',
    'endurance core',
    'phoenix ember',
    'spirit prism',
    'venom gland',
    'royal chitin plate',
    'hive ichor',
  ]);

  @Input() resource: string | null = '';

  get title(): string {
    const resource = this.resource?.toLowerCase() ?? '';
    if (resource.startsWith('blueprint:')) return 'Blueprints';
    if (this.catalystNames.has(resource)) return 'Catalysts';
    return 'Resources';
  }

  get subtitle(): string {
    return this.resource || 'All resource orders';
  }
}
