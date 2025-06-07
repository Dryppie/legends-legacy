import { Component, Input } from '@angular/core';
import { SidebarItem } from '../../../../shared/models/sidebar-item';
import { ProfessionIconComponent } from '../../../../shared/components/professions/profession-icon/profession-icon.component';

@Component({
  selector: 'app-sidebar-item',
  standalone: true,
  imports: [ProfessionIconComponent],
  templateUrl: './sidebar-item.component.html',
})
export class SidebarItemComponent {
  @Input() item!: SidebarItem;
  @Input() isActive = false;
}
