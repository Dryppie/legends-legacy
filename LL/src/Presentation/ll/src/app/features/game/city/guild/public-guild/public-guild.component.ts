import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  map,
  of,
  startWith,
  switchMap,
} from 'rxjs';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { GuildPublic } from '../../../../../shared/models/Dtos/guild/guildPublic';
import { GuildLinkComponent } from '../../../../../shared/components/guild/guild-link.component';
import { HumanizeEnumPipe } from '../../../../../shared/pipes/enums/humanize-enum.pipe';
import { DefaultHeaderComponent } from '../../../../../shared/components/default-header/default-header.component';
import { TabsComponent } from '../../../../../shared/components/custom-components/tabs/tabs.component';
import { TabComponent } from '../../../../../shared/components/custom-components/tabs/tab/tab.component';
import { GuildRankingsComponent } from '../in-a-guild/guild-rankings/guild-rankings.component';

interface GuildView {
  loading: boolean;
  guild: GuildPublic | null;
  error: string | null;
}

@Component({
  selector: 'app-public-guild',
  imports: [
    AsyncPipe,
    RouterLink,
    GuildLinkComponent,
    HumanizeEnumPipe,
    DefaultHeaderComponent,
    TabsComponent,
    TabComponent,
    GuildRankingsComponent,
  ],
  templateUrl: './public-guild.component.html',
  host: { class: 'flex h-full min-h-0 flex-col overflow-hidden' },
})
export class PublicGuildComponent {
  private readonly guildService = inject(GuildService);
  private readonly route = inject(ActivatedRoute);
  private readonly refresh = new BehaviorSubject(0);

  readonly view$ = combineLatest([this.route.paramMap, this.refresh]).pipe(
    switchMap(([params]) =>
      this.guildService.getPublicGuild(params.get('guildId') ?? '').pipe(
        map((guild): GuildView => ({ loading: false, guild, error: null })),
        catchError((error) =>
          of<GuildView>({
            loading: false,
            guild: null,
            error:
              error.status === 404
                ? 'This guild no longer exists or could not be found.'
                : 'Unable to load this guild. Please try again.',
          }),
        ),
        startWith<GuildView>({ loading: true, guild: null, error: null }),
      ),
    ),
  );

  retry(): void {
    this.refresh.next(this.refresh.value + 1);
  }
}
