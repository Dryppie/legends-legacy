import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
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
  @Input() icon: string = '';
  @Output() itemTapped = new EventEmitter<void>();

  inactiveIcon = 'icons/InactivePlus.svg';
  activeIcon = 'icons/ActivePlus.svg';
}
