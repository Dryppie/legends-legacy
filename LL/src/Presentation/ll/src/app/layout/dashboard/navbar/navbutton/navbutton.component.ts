import { NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarService } from '../../../../core/services/sidebar/sidebar.service';

@Component({
  selector: 'app-navbutton',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgClass],
  templateUrl: './navbutton.component.html',
  styleUrl: './navbutton.component.css',
})
export class NavbuttonComponent {
  @Input() link: string = '';
  @Input() label: string = '';
  @Output() itemTapped = new EventEmitter<void>();

  inactiveIcon = 'icons/InactivePlus.svg';
  activeIcon = 'icons/ActivePlus.svg';

  constructor(private sidebarService: SidebarService) {}

  updateSidebar() {
    this.sidebarService.updateContent(this.link.toLowerCase()); // update based on the label or link
    this.itemTapped.emit();
  }
}
