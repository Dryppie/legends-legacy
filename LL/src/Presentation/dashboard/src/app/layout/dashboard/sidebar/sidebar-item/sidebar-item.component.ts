import { Component, Input } from '@angular/core';
import { SidebarItem } from '../../../../shared/models/sidebar-item';
import { FeatureIconComponent } from '../../../../shared/components/feature-icon/feature-icon.component';

@Component({
  selector: 'app-sidebar-item',
  standalone: true,
  imports: [FeatureIconComponent],
  templateUrl: './sidebar-item.component.html',
})
export class SidebarItemComponent {
  @Input() item!: SidebarItem;
  @Input() isActive = false;
}
