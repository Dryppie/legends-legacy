import {
  Component,
  computed,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  signal,
  SimpleChanges,
} from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import {
  catchError,
  EMPTY,
  of,
  Subject,
  Subscription,
  switchMap,
  timer,
} from 'rxjs';
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
export class GuildInfoComponent implements OnInit, OnChanges, OnDestroy {
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
  readonly inviteSuggestions = signal<string[]>([]);
  readonly inviteSearchLoading = signal(false);
  readonly inviteSearchError = signal(false);
  readonly inviteSuggestionsOpen = signal(false);
  readonly activeInviteSuggestion = signal(-1);
  private readonly inviteSearch = new Subject<string>();

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
  ) {
    this.subscriptions.add(
      this.inviteSearch
        .pipe(
          switchMap((query) => {
            this.inviteSuggestions.set([]);
            this.activeInviteSuggestion.set(-1);
            this.inviteSearchError.set(false);
            this.inviteSearchLoading.set(query.length >= 2);
            if (query.length < 2) return EMPTY;

            return timer(200).pipe(
              switchMap(() =>
                this.characterService.suggestCharacterNames(query),
              ),
              catchError(() => {
                this.inviteSearchError.set(true);
                return of([] as string[]);
              }),
            );
          }),
        )
        .subscribe((names) => {
          this.inviteSearchLoading.set(false);
          this.inviteSuggestions.set(names);
          this.activeInviteSuggestion.set(names.length ? 0 : -1);
        }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.inviteSearch.complete();
  }

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
      this.inviteEvent.emit(this.inviteName.trim());

      this.closeModal();
    }
  }

  openModal() {
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.inviteName = '';
    this.closeInviteSuggestions();
  }

  onInviteNameChange(value: string): void {
    this.inviteName = value;
    this.openInviteSuggestions();
  }

  openInviteSuggestions(): void {
    const query = this.inviteName.trim();
    this.inviteSuggestionsOpen.set(query.length >= 2);
    this.inviteSearch.next(query);
  }

  closeInviteSuggestions(): void {
    this.inviteSuggestionsOpen.set(false);
    this.inviteSearch.next('');
  }

  selectInviteSuggestion(name: string): void {
    this.inviteName = name;
    this.closeInviteSuggestions();
  }

  handleInviteKeydown(event: KeyboardEvent): void {
    if (!this.inviteSuggestionsOpen()) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.closeInviteSuggestions();
      return;
    }

    const names = this.inviteSuggestions();
    if (!names.length) return;

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const direction = event.key === 'ArrowDown' ? 1 : -1;
      this.activeInviteSuggestion.update(
        (index) => (index + direction + names.length) % names.length,
      );
    } else if (event.key === 'Enter' && this.activeInviteSuggestion() >= 0) {
      event.preventDefault();
      this.selectInviteSuggestion(names[this.activeInviteSuggestion()]);
    }
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
