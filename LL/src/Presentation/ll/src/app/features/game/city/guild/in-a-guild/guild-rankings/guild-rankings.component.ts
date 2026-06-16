import { Component, computed } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-guild-rankings',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './guild-rankings.component.html',
})
export class GuildRankingsComponent {
  readonly sortedGuilds;

  constructor(private readonly guildState: GuildStateService) {
    this.sortedGuilds = computed(() =>
      [...this.guildState.allGuilds()].sort(
        (a, b) => b.memberCount - a.memberCount,
      ),
    );
  }

  topGuildName(): string {
    return this.sortedGuilds()[0]?.name ?? 'None';
  }

  totalGuilds(): number {
    return this.sortedGuilds().length;
  }
}
