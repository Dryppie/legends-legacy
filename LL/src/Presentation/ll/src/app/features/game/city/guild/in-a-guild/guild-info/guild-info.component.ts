import { Component, Input } from '@angular/core';
import { NgFor } from '@angular/common';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';

@Component({
  selector: 'app-guild-info',
  standalone: true,
  imports: [NgFor],
  templateUrl: './guild-info.component.html',
  styleUrl: './guild-info.component.css',
})
export class GuildInfoComponent {
  @Input() guild!: Guild;
}
