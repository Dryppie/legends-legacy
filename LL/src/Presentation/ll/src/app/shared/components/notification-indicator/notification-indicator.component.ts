import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-notification-indicator',
    imports: [NgIf],
    templateUrl: './notification-indicator.component.html'
})
export class NotificationIndicatorComponent {
  @Input() count = 0;
  @Input() label = 'Pending activity';
}
