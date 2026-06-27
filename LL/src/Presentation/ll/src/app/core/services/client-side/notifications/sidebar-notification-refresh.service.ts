import { Injectable } from '@angular/core';
import { ColosseumStateService } from '../../api/colosseum/colosseum-state.service';
import { GuildStateService } from '../../api/guild/guild-state.service';
import { ProphecyNotificationService } from '../../api/prophecies/prophecy-notification.service';

@Injectable({
  providedIn: 'root',
})
export class SidebarNotificationRefreshService {
  private refreshedCharacterId: string | null = null;

  constructor(
    private readonly prophecyNotificationService: ProphecyNotificationService,
    private readonly guildStateService: GuildStateService,
    private readonly colosseumStateService: ColosseumStateService,
  ) {}

  refreshForCharacter(characterId: string | null | undefined): void {
    if (!characterId || characterId === this.refreshedCharacterId) {
      return;
    }

    this.refreshedCharacterId = characterId;
    this.prophecyNotificationService.refreshCount();
    this.guildStateService.refreshNotificationCount();
    this.colosseumStateService.refreshNotificationCount();
  }
}
