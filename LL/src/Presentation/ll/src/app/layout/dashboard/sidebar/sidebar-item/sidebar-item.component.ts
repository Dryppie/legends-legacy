import { NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Tab } from '../../../../shared/models/sidebar-item';
import { ProfessionIconComponent } from '../../../../shared/components/professions/profession-icon/profession-icon.component';

@Component({
  selector: 'app-sidebar-item',
  standalone: true,
  imports: [NgIf, ProfessionIconComponent],
  templateUrl: './sidebar-item.component.html',
})
export class SidebarItemComponent {
  @Input() item!: Tab;
  @Input() isActive = false;
  @Input() notificationCount = 0;
}
