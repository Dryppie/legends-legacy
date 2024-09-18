import { NgClass, NgIf } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarItem } from '../../../../shared/models/sidebar-item';
import { ProfessionIconComponent } from '../../../../shared/components/professions/profession-icon/profession-icon.component';

@Component({
  selector: 'app-sidebar-item',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    NgIf,
    NgClass,
    ProfessionIconComponent,
  ],
  templateUrl: './sidebar-item.component.html',
  styleUrl: './sidebar-item.component.css',
})
export class SidebarItemComponent {
  @Input() item!: SidebarItem;
  @Input() isActive = false;
}
