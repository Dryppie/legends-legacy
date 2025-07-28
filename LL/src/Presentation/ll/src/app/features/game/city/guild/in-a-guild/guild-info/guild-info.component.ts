import {
  Component,
  computed,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { Subscription } from 'rxjs';
import { GuildRole } from '../../../../../../shared/models/Dtos/guild/guildRole';
import { FormsModule } from '@angular/forms';
import { GuildMember } from '../../../../../../shared/models/Dtos/guild/guildMember';
import { GuildService } from '../../../../../../core/services/api/guild/guild.service';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';

@Component({
  selector: 'app-guild-info',
  standalone: true,
  imports: [NgFor, NgIf, NgClass, FormsModule, RegularButtonComponent],
  templateUrl: './guild-info.component.html',
})
export class GuildInfoComponent implements OnInit {
  @Input() guild!: Guild;
  @Output() inviteEvent = new EventEmitter<string>();
  @Output() leaveEvent = new EventEmitter<void>();
  @Output() disbandEvent = new EventEmitter<void>();
  @Output() rejectEvent = new EventEmitter<string>();
  @Output() approveEvent = new EventEmitter<string>();

  guildMembers: GuildMember[] = [];
  readonly character = computed(() => {
    const myId = this.characterService.currentCharacterId(); // string | null
    const guild = this.state.guild(); // Guild | null
    if (!myId || !guild) return null;
    return guild.members.find((m) => m.characterId === myId) ?? null;
  });

  showModal = false;
  inviteName = '';

  showConfirmModal = false;
  confirmAction: 'leave' | 'disband' | null = null;

  showApplicationsModal = false;

  id!: string;
  leaderRole: GuildRole = GuildRole.Leader;
  memberRole: GuildRole = GuildRole.Member;
  subscriptions: Subscription = new Subscription();
  constructor(
    private characterService: CharacterService,
    private state: GuildStateService,
  ) {}

  ngOnInit(): void {
    this.sortGuildMembers();
  }

  private sortGuildMembers() {
    this.guildMembers = [...this.guild.members]; // Or however you get them

    this.guildMembers.sort((a, b) => {
      // First sort by role
      if (a.role < b.role) return -1;
      if (a.role > b.role) return 1;

      // If roles are the same, sort by level (descending)
      return b.level - a.level;
    });
  }

  invite() {
    if (this.inviteName.trim()) {
      this.inviteEvent.emit(this.inviteName);

      this.closeModal();
    }
  }

  openModal() {
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.inviteName = '';
  }

  openApplicationsModal() {
    this.showApplicationsModal = true;
  }

  approveApplication(characterId: string) {
    this.approveEvent.emit(characterId);
  }

  rejectApplication(characterId: string) {
    this.rejectEvent.emit(characterId);
  }

  closeApplicationsModal() {
    this.showApplicationsModal = false;
  }

  openConfirmModal(action: 'leave' | 'disband') {
    this.showConfirmModal = true;
    this.confirmAction = action;
  }

  closeConfirmModal() {
    this.showConfirmModal = false;
    this.confirmAction = null;
  }

  confirmDecision() {
    if (this.confirmAction === 'leave') {
      this.leaveEvent.emit();
    } else if (this.confirmAction === 'disband') {
      this.disbandEvent.emit();
    }

    this.closeConfirmModal();
  }

  isGuildFull(): boolean {
    return this.guildMembers.length >= this.guild.maxMembers;
  }
}
