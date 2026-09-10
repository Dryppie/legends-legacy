import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-guild-link',
  imports: [RouterLink],
  template: `
    @if (guildId) {
      <a
        [routerLink]="['/game/city/guild', guildId]"
        class="text-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-primary"
        (click)="$event.stopPropagation()"
        >{{ name }}</a
      >
    } @else {
      {{ name }}
    }
  `,
})
export class GuildLinkComponent {
  @Input() guildId: string | null | undefined;
  @Input({ required: true }) name!: string;
}
