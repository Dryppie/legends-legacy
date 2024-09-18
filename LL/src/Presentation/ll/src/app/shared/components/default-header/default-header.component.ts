import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../professions/profession-icon/profession-icon.component';

@Component({
  selector: 'app-default-header',
  standalone: true,
  imports: [ProfessionIconComponent],
  templateUrl: './default-header.component.html',
  styleUrl: './default-header.component.css',
})
export class DefaultHeaderComponent {
  @Input() title: string = '';
  @Input() text: string = '';
  @Input() image: string = '';
}
