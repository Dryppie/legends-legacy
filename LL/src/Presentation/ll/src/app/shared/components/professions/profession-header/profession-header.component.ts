import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../profession-icon/profession-icon.component';
import { ProfessionActionComponent } from '../profession-action/profession-action.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-profession-header',
  standalone: true,
  imports: [ProfessionIconComponent, ProfessionActionComponent, NgIf],
  templateUrl: './profession-header.component.html',
  styleUrl: './profession-header.component.css',
})
export class ProfessionHeaderComponent {
  @Input() title: string = '';
  @Input() level: string = '';
  @Input() experience: string = '';

  active = true;
}
