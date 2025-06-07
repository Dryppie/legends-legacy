import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-info-box',
  standalone: true,
  imports: [NgIf],
  templateUrl: './info-box.component.html',
})
export class InfoBoxComponent {
  @Input() title: string = ''; // Title for the info box
  @Input() description: string = ''; // Description for the info box
  @Input() imageUrl: string = ''; // Optional image URL
}
