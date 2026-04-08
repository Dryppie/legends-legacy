import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarService } from '../../../../core/services/client-side/sidebar/sidebar.service';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-navbutton',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, NgClass],
  templateUrl: './navbutton.component.html',
})
export class NavbuttonComponent {
  @Input() link: string = '';
  @Input() label: string = '';

  inactiveIcon = 'icons/InactivePlus.svg';
  activeIcon = 'icons/ActivePlus.svg';
}
