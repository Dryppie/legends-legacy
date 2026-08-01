import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnInit } from '@angular/core';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { LeaderboardStateService } from '../../../../../../core/services/api/leaderboard/leaderboard-state.service';
import { LeaderboardBoardEntry } from '../../../../../../shared/models/Dtos/leaderboard/leaderboard';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';

interface GuildRankingRow extends LeaderboardBoardEntry {
  ownerName: string;
  memberCount: number;
  maxMembers: number;
}

@Component({
  selector: 'app-guild-rankings',
  imports: [NgClass, NgIf, NgFor, NumberFormatPipe],
  templateUrl: './guild-rankings.component.html',
})
export class GuildRankingsComponent implements OnInit {
  readonly rankedGuilds = computed<GuildRankingRow[]>(() => {
    const guildsById = new Map(
      this.guildState.allGuilds().map((guild) => [guild.id, guild]),
    );

    return (this.leaderboardState.board()?.entries ?? []).map((entry) => {
      const guild = guildsById.get(entry.participantId);
      return {
        ...entry,
        ownerName: guild?.ownerName ?? 'Unknown',
        memberCount: guild?.memberCount ?? 0,
        maxMembers: guild?.maxMembers ?? 0,
      };
    });
  });

  constructor(
    private readonly guildState: GuildStateService,
    readonly leaderboardState: LeaderboardStateService,
  ) {}

  ngOnInit(): void {
    this.leaderboardState.load('guild-renown', true);
  }

  topGuildName(): string {
    return this.rankedGuilds()[0]?.participantName ?? 'None';
  }

  totalGuilds(): number {
    return this.leaderboardState.board()?.totalParticipants ?? 0;
  }

  isViewerGuild(guildId: string): boolean {
    return (
      guildId === this.leaderboardState.board()?.viewerEntry?.participantId
    );
  }
}
