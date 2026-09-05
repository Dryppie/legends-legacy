import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-feature-icon',
    imports: [NgIf],
    templateUrl: './feature-icon.component.html'
})
export class FeatureIconComponent {
  @Input() image: string = '';
  @Input() decorated = true;
}
