import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-profession-icon',
  standalone: true,
  imports: [],
  templateUrl: './profession-icon.component.html',
})
export class ProfessionIconComponent {
  @Input() image: string = '';
}
