import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../profession-icon/profession-icon.component';
import { ProfessionActionComponent } from '../profession-action/profession-action.component';

@Component({
  selector: 'app-profession-header',
  standalone: true,
  imports: [ProfessionIconComponent, ProfessionActionComponent],
  templateUrl: './profession-header.component.html',
  styleUrl: './profession-header.component.css',
})
export class ProfessionHeaderComponent {
  @Input() title: string = '';
  @Input() icon: string = '';
  @Input() level: number = 1;
  @Input() experience: number = 0;
  @Input() experienceUntilNextLevel: number = 0;
}
