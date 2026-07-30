import { NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Tab } from '../../../../shared/models/sidebar-item';
import { ProfessionIconComponent } from '../../../../shared/components/professions/profession-icon/profession-icon.component';
import { NotificationIndicatorComponent } from '../../../../shared/components/notification-indicator/notification-indicator.component';

@Component({
    selector: 'app-sidebar-item',
    imports: [
        NgClass,
        NgIf,
        ProfessionIconComponent,
        NotificationIndicatorComponent,
    ],
    templateUrl: './sidebar-item.component.html'
})
export class SidebarItemComponent {
  @Input() item!: Tab;
  @Input() isActive = false;
  @Input() notificationCount = 0;
  @Input() compact = false;
  @Input() tutorialAttention = false;
}
