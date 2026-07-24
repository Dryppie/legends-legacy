import { NgClass } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-banner',
    imports: [NgClass],
    templateUrl: './banner.component.html'
})
export class BannerComponent {
  @Input() image: string = '';
  @Input() bgPosition = 'bg-center';
}
