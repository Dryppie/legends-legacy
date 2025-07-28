import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-market-place-resources',
  standalone: true,
  imports: [],
  templateUrl: './market-place-resources.component.html',
})
export class MarketPlaceResourcesComponent {
  @Input() resource: string = '';
}
