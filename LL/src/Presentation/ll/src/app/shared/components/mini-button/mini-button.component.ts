import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-mini-button',
  standalone: true,
  imports: [],
  templateUrl: './mini-button.component.html',
  styleUrl: './mini-button.component.css',
})
export class MiniButtonComponent {
  @Input() disabled = false;
  @Input() text = '';
}
