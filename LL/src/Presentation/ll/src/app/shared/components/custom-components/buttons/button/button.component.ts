import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-button',
  imports: [],
  templateUrl: './button.component.html',
})
export class ButtonComponent {
  @Input() disabled = false;
  @Input() text = '';
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
}
