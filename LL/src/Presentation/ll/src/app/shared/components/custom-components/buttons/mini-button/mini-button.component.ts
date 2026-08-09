import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-mini-button',
  imports: [CommonModule],
  templateUrl: './mini-button.component.html',
})
export class MiniButtonComponent {
  @Input() disabled = false;
  @Input() text = '';
  @Input() mobileText = '';
  @Input() width = '64px'; // default width
}
