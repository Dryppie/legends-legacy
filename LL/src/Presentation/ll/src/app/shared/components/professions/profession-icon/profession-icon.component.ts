import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-profession-icon',
  standalone: true,
  imports: [NgIf],
  templateUrl: './profession-icon.component.html',
})
export class ProfessionIconComponent {
  @Input() image: string = '';
  @Input() decorated = true;
}
