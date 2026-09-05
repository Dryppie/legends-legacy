import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-feature-icon',
  standalone: true,
  imports: [],
  templateUrl: './feature-icon.component.html',
})
export class FeatureIconComponent {
  @Input() image: string = '';
}
