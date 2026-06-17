import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../profession-icon/profession-icon.component';

@Component({
  selector: 'app-profession-header',
  standalone: true,
  imports: [ProfessionIconComponent],
  templateUrl: './profession-header.component.html',
  styleUrl: './profession-header.component.css',
})
export class ProfessionHeaderComponent {
  @Input() title: string = '';
  @Input() icon: string = '';
  @Input() level: string = '';
  @Input() experience: string = '';

  active = true;
}
