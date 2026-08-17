import { Component, computed, Input, Signal, signal } from '@angular/core';
import { TabComponent } from '../../../../../shared/components/custom-components/tabs/tab/tab.component';
import { GuildInfoComponent } from './guild-info/guild-info.component';
import { Guild } from '../../../../../shared/models/Dtos/guild/guild';
import { InviteToGuild } from '../../../../../shared/models/requestDtos/guilds/inviteToGuild';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { GuildBuildingsComponent } from './guild-buildings/guild-buildings.component';
import { GuildMissionsComponent } from './guild-missions/guild-missions.component';
import { GuildShopComponent } from './guild-shop/guild-shop.component';
import { GuildRankingsComponent } from './guild-rankings/guild-rankings.component';
import { TabsComponent } from '../../../../../shared/components/custom-components/tabs/tabs.component';
import { NgFor, NgIf } from '@angular/common';
import { HumanizeEnumPipe } from '../../../../../shared/pipes/enums/humanize-enum.pipe';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { GuildVaultComponent } from './guild-vault/guild-vault.component';
import { FormsModule } from '@angular/forms';
import { CharacterService } from '../../../../../core/services/api/character/character.service';
import { GuildRole } from '../../../../../shared/models/Dtos/guild/guildRole';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-in-a-guild',
  imports: [
    NgFor,
    NgIf,
    FormsModule,
    RegularButtonComponent,
    TabComponent,
    GuildInfoComponent,
    TabsComponent,
    GuildBuildingsComponent,
    GuildMissionsComponent,
    GuildShopComponent,
    GuildRankingsComponent,
    HumanizeEnumPipe,
    NumberFormatPipe,
    GuildVaultComponent,
  ],
  templateUrl: './in-a-guild.component.html',
  styleUrl: './in-a-guild.component.scss',
})
export class InAGuildComponent {
  @Input() guild!: Guild;
  readonly claimableDailyOrderCount: Signal<number>;
  readonly guildFavor: Signal<number>;

  readonly descriptionMaxLength = 500;
  readonly editingDescription = signal(false);
  descriptionDraft = '';

  constructor(
    private state: GuildStateService,
    private characterService: CharacterService,
  ) {
    this.claimableDailyOrderCount = this.state.claimableDailyOrderCount;
    this.guildFavor = computed(() => this.state.shop()?.guildFavor ?? 0);
  }

  get canEditDescription(): boolean {
    const characterId = this.characterService.currentCharacterId();
    const role = this.guild?.members?.find(
      (member) => member.characterId === characterId,
    )?.role;

    return role === GuildRole.Leader || role === GuildRole.Officer;
  }

  startEditingDescription(): void {
    if (!this.canEditDescription) return;
    this.descriptionDraft = this.guild.description ?? '';
    this.editingDescription.set(true);
  }

  cancelEditingDescription(): void {
    this.descriptionDraft = this.guild.description ?? '';
    this.editingDescription.set(false);
  }

  saveDescription(): void {
    if (!this.canEditDescription) return;
    const description = this.descriptionDraft.trim();
    if (description.length > this.descriptionMaxLength) return;
    this.state.updateDescription(description);
    this.editingDescription.set(false);
  }

  inviteCharacterByName($event: string) {
    const invite: InviteToGuild = {
      guildId: this.guild.id,
      characterNameOrId: $event,
    };
    this.state.inviteCharacterByName(invite);
  }

  leaveGuild() {
    this.state.leave();
  }

  disbandGuild() {
    this.state.disband();
  }

  rejectApplication($event: string) {
    this.state.rejectApplication($event);
  }

  approveApplication($event: string) {
    this.state.approveApplication($event);
  }

  get resourceSummary() {
    return this.guild.resources ?? [];
  }
}
