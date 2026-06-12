import { Component, Input } from '@angular/core';
import { ProfessionIconComponent } from '../professions/profession-icon/profession-icon.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-default-header',
  standalone: true,
  imports: [NgIf, ProfessionIconComponent],
  templateUrl: './default-header.component.html',
})
export class DefaultHeaderComponent {
  @Input() title: string = '';
  @Input() text: string = '';
  @Input() icon: string = '';
  @Input() section: string = '';
}
