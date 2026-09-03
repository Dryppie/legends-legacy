import {
  Component,
  computed,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  signal,
  SimpleChanges,
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
import { CharacterTagComponent } from '../../../../../../shared/components/character/character-tag/character-tag.component';
import { GuildRolePermission } from '../../../../../../shared/models/Dtos/guild/guildRolePermission';
import { PresenceIndicatorComponent } from '../../../../../../shared/components/character/presence-indicator/presence-indicator.component';
import { DialogFocusDirective } from '../../../../../../shared/directives/dialog-focus/dialog-focus.directive';

@Component({
  selector: 'app-guild-info',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    FormsModule,
    RegularButtonComponent,
    CharacterTagComponent,
    PresenceIndicatorComponent,
    DialogFocusDirective,
  ],
  templateUrl: './guild-info.component.html',
})
export class GuildInfoComponent implements OnInit, OnChanges {
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
  confirmAction: 'leave' | 'disband' | 'kick' | null = null;
  pendingKickMember: GuildMember | null = null;

  showApplicationsModal = false;
  rolePermissions: GuildRolePermission[] = [];
  readonly rolePermissionsOpen = signal(false);

  id!: string;
  leaderRole: GuildRole = GuildRole.Leader;
  officerRole: GuildRole = GuildRole.Officer;
  memberRole: GuildRole = GuildRole.Member;
  subscriptions: Subscription = new Subscription();
  constructor(
    private characterService: CharacterService,
    private state: GuildStateService,
  ) {}

  ngOnInit(): void {
    this.sortGuildMembers();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['guild']) {
      this.sortGuildMembers();
      this.rolePermissions = (this.guild.rolePermissions ?? [])
        .filter((permission) => permission.role !== GuildRole.Leader)
        .map((permission) => ({ ...permission }));
    }
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
    this.pendingKickMember = null;
  }

  confirmDecision() {
    if (this.confirmAction === 'leave') {
      this.leaveEvent.emit();
    } else if (this.confirmAction === 'disband') {
      this.disbandEvent.emit();
    } else if (this.confirmAction === 'kick' && this.pendingKickMember) {
      this.state.kickMember(this.pendingKickMember.characterId);
    }

    this.closeConfirmModal();
  }

  isGuildFull(): boolean {
    return this.guildMembers.length >= this.guild.maxMembers;
  }

  permissionFor(role: GuildRole): GuildRolePermission | undefined {
    return this.guild.rolePermissions?.find(
      (permission) => permission.role === role,
    );
  }

  get canInvite(): boolean {
    const member = this.character();
    return !!member && !!this.permissionFor(member.role)?.canInvite;
  }

  get canManageApplications(): boolean {
    const member = this.character();
    return !!member && !!this.permissionFor(member.role)?.canManageApplications;
  }

  canPromote(member: GuildMember): boolean {
    const current = this.character();
    if (!current || member.role !== GuildRole.Member) return false;
    return !!this.permissionFor(current.role)?.canPromoteDemote;
  }

  canDemote(member: GuildMember): boolean {
    const current = this.character();
    return (
      current?.role === GuildRole.Leader && member.role === GuildRole.Officer
    );
  }

  canKick(member: GuildMember): boolean {
    const current = this.character();
    if (!current || member.characterId === current.characterId) return false;
    const roleRank = {
      [GuildRole.Leader]: 0,
      [GuildRole.Officer]: 1,
      [GuildRole.Member]: 2,
    };
    return (
      !!this.permissionFor(current.role)?.canKick &&
      roleRank[member.role] > roleRank[current.role]
    );
  }

  hasMemberActions(member: GuildMember): boolean {
    return (
      this.canPromote(member) || this.canDemote(member) || this.canKick(member)
    );
  }

  promote(member: GuildMember): void {
    this.state.changeMemberRole(member.characterId, GuildRole.Officer);
  }

  demote(member: GuildMember): void {
    this.state.changeMemberRole(member.characterId, GuildRole.Member);
  }

  kick(member: GuildMember): void {
    this.pendingKickMember = member;
    this.confirmAction = 'kick';
    this.showConfirmModal = true;
  }

  get confirmTitle(): string {
    if (this.confirmAction === 'disband') return 'Disband Guild?';
    if (this.confirmAction === 'kick') return 'Kick Member?';
    return 'Leave Guild?';
  }

  get confirmMessage(): string {
    if (this.confirmAction === 'disband') {
      return 'Are you sure you want to disband your guild? All donated equipment, including borrowed and equipped items, will be destroyed. This action cannot be undone.';
    }
    if (this.confirmAction === 'kick') {
      return `Are you sure you want to kick ${this.pendingKickMember?.name ?? 'this member'} from the guild?`;
    }
    return 'Are you sure you want to leave the guild?';
  }

  get confirmButtonLabel(): string {
    if (this.confirmAction === 'disband') return 'Disband';
    if (this.confirmAction === 'kick') return 'Kick';
    return 'Leave';
  }

  savePermissions(permissions: GuildRolePermission): void {
    this.state.updateRolePermissions({ ...permissions });
  }

  toggleRolePermissions(): void {
    this.rolePermissionsOpen.update((open) => !open);
  }
}
