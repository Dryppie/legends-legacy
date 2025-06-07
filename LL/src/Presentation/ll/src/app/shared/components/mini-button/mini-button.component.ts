import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-mini-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mini-button.component.html',
})
export class MiniButtonComponent {
  @Input() disabled = false;
  @Input() text = '';
  @Input() width = '64px'; // default width
}
