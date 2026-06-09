import { Component, Input } from '@angular/core';
import { NgSwitch, NgSwitchCase, NgSwitchDefault } from '@angular/common';

@Component({
  selector: 'app-dungeon-room-icon',
  standalone: true,
  imports: [NgSwitch, NgSwitchCase, NgSwitchDefault],
  templateUrl: './dungeon-room-icon.component.html',
})
export class DungeonRoomIconComponent {
  @Input() type: string | null | undefined;
}
