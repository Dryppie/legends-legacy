import { NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Tab } from '../../../../shared/models/sidebar-item';
import { FeatureIconComponent } from '../../../../shared/components/feature-icon/feature-icon.component';
import { NotificationIndicatorComponent } from '../../../../shared/components/notification-indicator/notification-indicator.component';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-sidebar-item',
  imports: [
    NgClass,
    NgIf,
    FeatureIconComponent,
    NotificationIndicatorComponent,
    RouterLink,
  ],
  templateUrl: './sidebar-item.component.html',
})
export class SidebarItemComponent {
  @Input() item!: Tab;
  @Input() isActive = false;
  @Input() notificationCount = 0;
  @Input() compact = false;
  @Input() questAttention = false;
}
