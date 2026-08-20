import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import {
  AbilityDamageTypeStats,
  AbilityStats,
  DamageType,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';

type CombatTeamName = 'Friendly' | 'Hostile';
type AbilitySortColumn = 'uses' | 'damage' | 'healing' | 'barrier' | 'threat';
type SortDirection = 'asc' | 'desc';
const DAMAGE_TYPE_ORDER: readonly DamageType[] = [
  'Physical',
  'Magical',
  'Burn',
  'Bleed',
  'Poison',
  'Shadow',
  'None',
];
const DAMAGE_TYPE_ORDER_INDEX = new Map(
  DAMAGE_TYPE_ORDER.map((damageType, index) => [damageType, index]),
);
const DAMAGE_TYPE_COLORS: Readonly<Record<DamageType, string>> = {
  Physical: '#e6e2d9',
  Magical: '#9d86ef',
  Bleed: '#d94d5c',
  Burn: '#ef8a3c',
  Poison: '#82b94b',
  Shadow: '#69b6dd',
  None: '#8d8991',
};
type StatsParticipant = {
  id: string;
  name: string;
  team: CombatTeamName;
  entity: SimpleCombatEntityDto;
  isSummonGroup?: boolean;
  isSummonChild?: boolean;
  summonCount?: number;
  standingCount?: number;
  summonerName?: string;
  summonGroupKey?: string;
  members?: StatsParticipant[];
};

type SummonIdentity = {
  ownerId: string;
  summonId: string;
};

type StatsParticipantGroup = {
  key: string;
  label: string;
  team: CombatTeamName;
  partyNumber: number | null;
  participants: StatsParticipant[];
  damageDone: number;
  isCurrentParty: boolean;
};

@Component({
  selector: 'app-combat-entity-stats',
  imports: [NgIf, NgFor, NgClass, DecimalPipe, RegularButtonComponent],
  templateUrl: './combat-entity-stats.component.html',
})
export class CombatEntityStatsComponent implements OnChanges {
  @Input() playerTeam: SimpleCombatEntityDto[] = [];
  @Input() enemyTeam: SimpleCombatEntityDto[] = [];
  @Input() entityStats!: EntityStats[];
  @Input() playerTeamName: string | null = null;
  @Input() enemyTeamName: string | null = null;
  @Input() combatDurationTicks = 0;
  @Input() useParentScroll = false;
  /** Id of the logged-in character, used to pre-select their own unit. */
  @Input() currentCharacterId: string | null = null;

  selectedStats: EntityStats | null = null;
  selectedEntityId: string = '';
  selectedEntityName: string = '';
  abilitySortColumn: AbilitySortColumn = 'damage';
  abilitySortDirection: SortDirection = 'desc';
  readonly damageTypeLegend: readonly DamageType[] = [
    'Physical',
    'Magical',
    'Burn',
    'Bleed',
    'Poison',
  ];

  playerParticipants: StatsParticipant[] = [];
  enemyParticipants: StatsParticipant[] = [];
  participantGroups: StatsParticipantGroup[] = [];
  hasPartyLayout = false;
  private readonly rawStatsById = new Map<string, EntityStats>();
  private readonly aggregateStats = new Map<string, EntityStats>();
  private readonly expandedSummonGroups = new Set<string>();
  private readonly collapsedParticipantGroups = new Set<string>();

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['entityStats'] ||
      changes['playerTeam'] ||
      changes['enemyTeam'] ||
      changes['currentCharacterId']
    ) {
      this.rawStatsById.clear();
      for (const stats of this.entityStats ?? []) {
        this.rawStatsById.set(stats.entityId, stats);
      }
      this.aggregateStats.clear();
      this.playerParticipants = this.groupSummons(
        this.buildParticipants('Friendly', this.playerTeam),
      );
      this.enemyParticipants = this.groupSummons(
        this.buildParticipants('Hostile', this.enemyTeam),
      );
      this.participantGroups = this.buildParticipantGroups();
      this.pruneExpandedSummonGroups();
      this.pruneCollapsedParticipantGroups();
      this.refreshSelection();
    }
  }

  get visibleParticipants(): StatsParticipant[] {
    return this.participantGroups.flatMap((group) =>
      this.visibleParticipantsForGroup(group),
    );
  }

  visibleParticipantsForGroup(
    group: StatsParticipantGroup,
  ): StatsParticipant[] {
    if (this.isParticipantGroupCollapsed(group)) return [];
    return group.participants.flatMap((participant) =>
      participant.isSummonGroup && this.expandedSummonGroups.has(participant.id)
        ? [participant, ...(participant.members ?? [])]
        : [participant],
    );
  }

  trackParticipantGroup(_index: number, group: StatsParticipantGroup): string {
    return group.key;
  }

  toggleParticipantGroup(group: StatsParticipantGroup): void {
    if (this.collapsedParticipantGroups.has(group.key)) {
      this.collapsedParticipantGroups.delete(group.key);
    } else {
      this.collapsedParticipantGroups.add(group.key);
    }
    this.refreshSelection();
  }

  isParticipantGroupCollapsed(group: StatsParticipantGroup): boolean {
    return this.collapsedParticipantGroups.has(group.key);
  }

  participantSideLabel(participant: StatsParticipant): string {
    const partyNumber = participant.entity.partyNumber;
    if (participant.team === 'Friendly' && partyNumber != null) {
      return (
        'P' +
        partyNumber +
        (participant.isSummonGroup || participant.isSummonChild
          ? ' · Minion'
          : '')
      );
    }
    return this.teamDisplayName(participant.team);
  }

  activateParticipant(participant: StatsParticipant): void {
    this.selectEntity(participant.id);
    if (!participant.isSummonGroup) return;

    if (this.expandedSummonGroups.has(participant.id)) {
      this.expandedSummonGroups.delete(participant.id);
    } else {
      this.expandedSummonGroups.add(participant.id);
    }
  }

  isSummonGroupExpanded(participant: StatsParticipant): boolean {
    return this.expandedSummonGroups.has(participant.id);
  }

  selectEntity(id: string): void {
    this.selectedStats = this.statsFor(id) ?? this.createEmptyStats(id);
    const entity = this.selectableParticipants().find(
      (participant) => participant.id === id,
    );
    this.selectedEntityId = entity?.id ?? '';
    this.selectedEntityName = entity?.name ?? '';
  }

  statsFor(id: string): EntityStats | null {
    return this.aggregateStats.get(id) ?? this.rawStatsById.get(id) ?? null;
  }

  healthPercentage(entity: SimpleCombatEntityDto): number {
    if (entity.maxHealth <= 0) return 0;
    return Math.max(0, Math.min(100, (entity.health / entity.maxHealth) * 100));
  }

  barrierPercentage(entity: SimpleCombatEntityDto): number {
    if (entity.maxHealth <= 0) return 0;
    const availableWidth = 100 - this.healthPercentage(entity);
    return Math.max(
      0,
      Math.min(availableWidth, (entity.barrier / entity.maxHealth) * 100),
    );
  }

  barrierStartPercentage(entity: SimpleCombatEntityDto): number {
    return this.healthPercentage(entity);
  }

  trackParticipant(_index: number, participant: StatsParticipant): string {
    return `${participant.team}:${participant.id}`;
  }

  teamDisplayName(team: CombatTeamName): string {
    const teamName =
      team === 'Friendly' ? this.playerTeamName : this.enemyTeamName;
    return teamName?.trim() || (team === 'Friendly' ? 'Ally' : 'Enemy');
  }

  sortAbilities(column: AbilitySortColumn): void {
    if (this.abilitySortColumn === column) {
      this.abilitySortDirection =
        this.abilitySortDirection === 'desc' ? 'asc' : 'desc';
      return;
    }

    this.abilitySortColumn = column;
    this.abilitySortDirection = 'desc';
  }

  sortIndicator(column: AbilitySortColumn): string {
    if (this.abilitySortColumn !== column) return '';
    return this.abilitySortDirection === 'desc' ? '↓' : '↑';
  }

  sortAriaLabel(column: AbilitySortColumn): string {
    const nextDirection =
      this.abilitySortColumn === column && this.abilitySortDirection === 'desc'
        ? 'ascending'
        : 'descending';
    return 'Sort by ' + column + ' ' + nextDirection;
  }

  averagePrimaryOutput(ability: AbilityStats): number {
    if (ability.uses <= 0) return 0;
    return this.primaryOutputTotal(ability) / ability.uses;
  }

  abilityBarPercentage(ability: AbilityStats): number {
    const maximum = Math.max(
      0,
      ...(this.selectedStats?.abilities ?? []).map((item) =>
        this.primaryOutputTotal(item),
      ),
    );
    if (maximum <= 0) return 0;
    return (this.primaryOutputTotal(ability) / maximum) * 100;
  }

  abilityBarClass(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'bg-primary/65';
    if (ability.totalHealing > 0) return 'bg-success/65';
    if (ability.totalBarrier > 0) return 'bg-[#8ecbff]/55';
    return 'bg-white/10';
  }

  threatBarPercentage(ability: AbilityStats): number {
    const maximum = Math.max(
      0,
      ...(this.selectedStats?.abilities ?? []).map(
        (item) => item.totalThreat ?? 0,
      ),
    );
    if (maximum <= 0) return 0;
    return ((ability.totalThreat ?? 0) / maximum) * 100;
  }

  threatPerSecond(stats: EntityStats | null | undefined): number {
    if (!stats || this.combatDurationTicks <= 0) return 0;
    return ((stats.threatGenerated ?? 0) * 10) / this.combatDurationTicks;
  }

  damageBreakdown(ability: AbilityStats): AbilityDamageTypeStats[] {
    return [...(ability.damageByType ?? [])]
      .filter((entry) => entry.totalDamage > 0)
      .sort(
        (left, right) =>
          (DAMAGE_TYPE_ORDER_INDEX.get(left.damageType) ??
            Number.MAX_SAFE_INTEGER) -
          (DAMAGE_TYPE_ORDER_INDEX.get(right.damageType) ??
            Number.MAX_SAFE_INTEGER),
      );
  }

  damageTypeBarPercentage(entry: AbilityDamageTypeStats): number {
    const maximum = Math.max(
      0,
      ...(this.selectedStats?.abilities ?? []).map(
        (ability) => ability.totalDamage ?? 0,
      ),
    );
    if (maximum <= 0) return 0;
    return (entry.totalDamage / maximum) * 100;
  }

  damageTypeColor(damageType: DamageType): string {
    return DAMAGE_TYPE_COLORS[damageType] ?? DAMAGE_TYPE_COLORS.None;
  }

  damageTypeLabel(damageType: DamageType): string {
    return damageType === 'None' ? 'Untyped' : damageType;
  }

  trackDamageType(_index: number, entry: AbilityDamageTypeStats): DamageType {
    return entry.damageType;
  }

  trackAbility(_index: number, ability: AbilityStats): string {
    return ability.name;
  }

  abilityCategory(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'Attack';
    if (ability.totalHealing > 0) return 'Healing';
    if (ability.totalBarrier > 0) return 'Barrier';
    return 'Utility';
  }

  abilityCategoryClass(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'border-primary/60 text-primary';
    if (ability.totalHealing > 0) return 'border-success/60 ll-text-success';
    if (ability.totalBarrier > 0) return 'border-[#8ecbff]/60 ll-text-info';
    return 'border-white/20 text-secondary';
  }

  get sortedAbilities(): AbilityStats[] {
    const direction = this.abilitySortDirection === 'asc' ? 1 : -1;
    return [...(this.selectedStats?.abilities ?? [])].sort((left, right) => {
      const difference =
        this.abilitySortValue(left) - this.abilitySortValue(right);
      return difference === 0
        ? left.name.localeCompare(right.name)
        : difference * direction;
    });
  }

  isDefeated(entity: SimpleCombatEntityDto): boolean {
    return this.hasKnownHealth(entity) && entity.health <= 0;
  }

  hasKnownHealth(entity: SimpleCombatEntityDto): boolean {
    return entity.maxHealth > 0;
  }

  private refreshSelection(): void {
    const participants = this.visibleParticipants;
    if (
      this.selectedEntityId &&
      participants.some(
        (participant) => participant.id === this.selectedEntityId,
      )
    ) {
      this.selectEntity(this.selectedEntityId);
      return;
    }

    const ownParticipant = this.findOwnParticipant(participants);
    const fallbackParticipant = ownParticipant ?? participants[0];
    if (fallbackParticipant) {
      this.selectEntity(fallbackParticipant.id);
      return;
    }

    this.selectedEntityId = '';
    this.selectedEntityName = '';
    this.selectedStats = null;
  }

  private buildParticipants(
    team: CombatTeamName,
    visibleTeam: SimpleCombatEntityDto[],
  ): StatsParticipant[] {
    const participants = new Map<string, StatsParticipant>();

    for (const entity of visibleTeam) {
      if (!entity.id) continue;
      participants.set(entity.id, {
        id: entity.id,
        name: entity.name,
        team,
        entity,
      });
    }

    for (const stats of this.entityStats ?? []) {
      if (!stats.entityId || !this.isStatsForTeam(stats, team, visibleTeam))
        continue;

      if (!participants.has(stats.entityId)) {
        const entity: SimpleCombatEntityDto = {
          id: stats.entityId,
          name: stats.entityName || stats.entityId,
          imagePath: '',
          health: stats.health ?? -1,
          maxHealth: stats.maxHealth ?? -1,
          barrier: stats.barrier ?? 0,
          level: 1,
        };
        participants.set(stats.entityId, {
          id: stats.entityId,
          name: entity.name,
          team,
          entity,
        });
      }
    }

    return [...participants.values()].sort((a, b) =>
      a.name.localeCompare(b.name),
    );
  }

  private groupSummons(participants: StatsParticipant[]): StatsParticipant[] {
    const namesById = new Map(
      participants.map((participant) => [participant.id, participant.name]),
    );
    const groups = new Map<string, StatsParticipant[]>();
    const result: StatsParticipant[] = [];

    for (const participant of participants) {
      const identity = this.summonIdentity(participant.id);
      if (!identity) {
        result.push(participant);
        continue;
      }

      const key = `${participant.team}:${identity.ownerId}:${identity.summonId}`;
      const existing = groups.get(key);
      if (existing) {
        existing.push(participant);
      } else {
        groups.set(key, [participant]);
        result.push(
          this.createSummonGroup(
            key,
            identity.ownerId,
            participant.team,
            namesById,
            groups,
          ),
        );
      }
    }

    // Groups are created when their first member is encountered. Refresh them
    // after collecting the team so their aggregate includes every instance.
    return result.map((participant) => {
      if (!participant.isSummonGroup) return participant;
      const members = participant.summonGroupKey
        ? (groups.get(participant.summonGroupKey) ?? [])
        : [];
      return this.populateSummonGroup(participant, members);
    });
  }

  private buildParticipantGroups(): StatsParticipantGroup[] {
    const partyNumbers = [
      ...new Set(
        this.playerParticipants
          .map((participant) => participant.entity.partyNumber)
          .filter((partyNumber): partyNumber is number => partyNumber != null),
      ),
    ].sort((left, right) => left - right);
    this.hasPartyLayout = partyNumbers.length > 0;

    const friendlyGroups = this.hasPartyLayout
      ? [
          ...partyNumbers.map((partyNumber) =>
            this.createParticipantGroup(
              'friendly-party-' + partyNumber,
              'Party ' + partyNumber,
              'Friendly',
              partyNumber,
              this.playerParticipants.filter(
                (participant) => participant.entity.partyNumber === partyNumber,
              ),
            ),
          ),
          this.createParticipantGroup(
            'friendly-unassigned',
            this.teamDisplayName('Friendly'),
            'Friendly',
            null,
            this.playerParticipants.filter(
              (participant) => participant.entity.partyNumber == null,
            ),
          ),
        ].filter((group) => group.participants.length > 0)
      : [
          this.createParticipantGroup(
            'friendly',
            this.teamDisplayName('Friendly'),
            'Friendly',
            null,
            this.playerParticipants,
          ),
        ];
    const enemyGroup = this.createParticipantGroup(
      'hostile',
      this.teamDisplayName('Hostile'),
      'Hostile',
      null,
      this.enemyParticipants,
    );
    return [...friendlyGroups, enemyGroup].filter(
      (group) => group.participants.length > 0,
    );
  }

  private createParticipantGroup(
    key: string,
    label: string,
    team: CombatTeamName,
    partyNumber: number | null,
    participants: StatsParticipant[],
  ): StatsParticipantGroup {
    const currentCharacterId = this.currentCharacterId;
    return {
      key,
      label,
      team,
      partyNumber,
      participants,
      damageDone: participants.reduce(
        (total, participant) =>
          total + (this.statsFor(participant.id)?.damageDone ?? 0),
        0,
      ),
      isCurrentParty:
        team === 'Friendly' &&
        !!currentCharacterId &&
        participants.some(
          (participant) =>
            !participant.isSummonGroup &&
            this.isSameEntityId(participant.id, currentCharacterId),
        ),
    };
  }

  private createSummonGroup(
    key: string,
    ownerId: string,
    team: CombatTeamName,
    namesById: Map<string, string>,
    groups: Map<string, StatsParticipant[]>,
  ): StatsParticipant {
    const members = groups.get(key) ?? [];
    const first = members[0];
    return {
      id: `summon-group:${key}`,
      name: first.name,
      team,
      entity: { ...first.entity },
      isSummonGroup: true,
      summonGroupKey: key,
      summonerName: namesById.get(ownerId),
      members,
    };
  }

  private populateSummonGroup(
    group: StatsParticipant,
    members: StatsParticipant[],
  ): StatsParticipant {
    const children = members.map((member) => ({
      ...member,
      isSummonChild: true,
    }));
    const knownHealth = members.every((member) =>
      this.hasKnownHealth(member.entity),
    );
    const entity: SimpleCombatEntityDto = {
      ...group.entity,
      health: knownHealth
        ? members.reduce((total, member) => total + member.entity.health, 0)
        : -1,
      maxHealth: knownHealth
        ? members.reduce((total, member) => total + member.entity.maxHealth, 0)
        : -1,
      barrier: members.reduce(
        (total, member) => total + (member.entity.barrier ?? 0),
        0,
      ),
    };
    const populated = {
      ...group,
      entity,
      members: children,
      summonCount: members.length,
      standingCount: members.filter((member) => !this.isDefeated(member.entity))
        .length,
    };
    this.aggregateStats.set(
      populated.id,
      this.aggregateSummonStats(populated, members),
    );
    return populated;
  }

  private aggregateSummonStats(
    group: StatsParticipant,
    members: StatsParticipant[],
  ): EntityStats {
    const stats = members
      .map((member) => this.rawStatsById.get(member.id))
      .filter((item): item is EntityStats => !!item);
    const abilities = new Map<string, AbilityStats>();
    for (const ability of stats.flatMap((item) => item.abilities ?? [])) {
      const current = abilities.get(ability.name) ?? {
        name: ability.name,
        totalDamage: 0,
        damageByType: [],
        totalHealing: 0,
        uses: 0,
        hits: 0,
        crits: 0,
        summons: 0,
        stuns: 0,
        selfDamage: 0,
        alliedDamage: 0,
        totalBarrier: 0,
        totalThreat: 0,
      };
      abilities.set(ability.name, {
        name: ability.name,
        totalDamage: current.totalDamage + (ability.totalDamage ?? 0),
        damageByType: this.mergeDamageByType(
          current.damageByType,
          ability.damageByType,
        ),
        totalHealing: current.totalHealing + (ability.totalHealing ?? 0),
        uses: current.uses + (ability.uses ?? 0),
        hits: current.hits + (ability.hits ?? 0),
        crits: current.crits + (ability.crits ?? 0),
        summons: current.summons + (ability.summons ?? 0),
        stuns: current.stuns + (ability.stuns ?? 0),
        selfDamage: current.selfDamage + (ability.selfDamage ?? 0),
        alliedDamage: current.alliedDamage + (ability.alliedDamage ?? 0),
        totalBarrier: current.totalBarrier + (ability.totalBarrier ?? 0),
        totalThreat: (current.totalThreat ?? 0) + (ability.totalThreat ?? 0),
      });
    }
    const sum = (selector: (item: EntityStats) => number): number =>
      stats.reduce((total, item) => total + (selector(item) ?? 0), 0);

    return {
      entityId: group.id,
      entityName: group.name,
      abilities: [...abilities.values()],
      damageDone: sum((item) => item.damageDone),
      damageTaken: sum((item) => item.damageTaken),
      healingDone: sum((item) => item.healingDone),
      healingReceived: sum((item) => item.healingReceived),
      healthRegenerated: sum((item) => item.healthRegenerated),
      healthRegenerationPotential: sum(
        (item) => item.healthRegenerationPotential,
      ),
      healthRegenerationOverhealed: sum(
        (item) => item.healthRegenerationOverhealed,
      ),
      healthRegenerationPulses: sum((item) => item.healthRegenerationPulses),
      selfDamageDone: sum((item) => item.selfDamageDone),
      selfDamageTaken: sum((item) => item.selfDamageTaken),
      alliedDamageDone: sum((item) => item.alliedDamageDone),
      alliedDamageTaken: sum((item) => item.alliedDamageTaken),
      team: group.team,
      barrierGenerated: sum((item) => item.barrierGenerated),
      damageBlocked: sum((item) => item.damageBlocked),
      damageRedirectedTo: sum((item) => item.damageRedirectedTo ?? 0),
      damageRedirectedAway: sum((item) => item.damageRedirectedAway ?? 0),
      targetedAttacks: sum((item) => item.targetedAttacks ?? 0),
      attentionSharePercent: sum((item) => item.attentionSharePercent ?? 0),
      threatGenerated: sum((item) => item.threatGenerated ?? 0),
      health: group.entity.health,
      maxHealth: group.entity.maxHealth,
      barrier: group.entity.barrier,
    };
  }

  private findOwnParticipant(
    participants: StatsParticipant[],
  ): StatsParticipant | null {
    const ownId = this.currentCharacterId;
    if (!ownId) return null;
    return (
      participants.find(
        (participant) =>
          !participant.isSummonGroup &&
          !participant.isSummonChild &&
          this.isSameEntityId(participant.id, ownId),
      ) ?? null
    );
  }

  private isSameEntityId(left: string, right: string): boolean {
    const normalize = (value: string) =>
      value.replaceAll('-', '').toLowerCase();
    return normalize(left) === normalize(right);
  }

  private summonIdentity(id: string): SummonIdentity | null {
    const marker = ':summon:';
    const markerIndex = id.lastIndexOf(marker);
    if (markerIndex <= 0) return null;
    const ownerId = id.slice(0, markerIndex);
    const summonId = id.slice(markerIndex + marker.length).split(':')[0];
    return ownerId && summonId ? { ownerId, summonId } : null;
  }

  private selectableParticipants(): StatsParticipant[] {
    return [...this.playerParticipants, ...this.enemyParticipants].flatMap(
      (participant) => [participant, ...(participant.members ?? [])],
    );
  }

  private pruneExpandedSummonGroups(): void {
    const currentGroupIds = new Set(
      [...this.playerParticipants, ...this.enemyParticipants]
        .filter((participant) => participant.isSummonGroup)
        .map((participant) => participant.id),
    );
    for (const groupId of this.expandedSummonGroups) {
      if (!currentGroupIds.has(groupId)) {
        this.expandedSummonGroups.delete(groupId);
      }
    }
  }

  private pruneCollapsedParticipantGroups(): void {
    const currentGroupKeys = new Set(
      this.participantGroups.map((group) => group.key),
    );
    for (const groupKey of this.collapsedParticipantGroups) {
      if (!currentGroupKeys.has(groupKey)) {
        this.collapsedParticipantGroups.delete(groupKey);
      }
    }
  }

  private isStatsForTeam(
    stats: EntityStats,
    team: CombatTeamName,
    visibleTeam: SimpleCombatEntityDto[],
  ): boolean {
    if (stats.team?.toLowerCase() === team.toLowerCase()) return true;
    return (
      !stats.team && visibleTeam.some((entity) => entity.id === stats.entityId)
    );
  }

  private createEmptyStats(id: string): EntityStats | null {
    const participant = this.selectableParticipants().find(
      (item) => item.id === id,
    );
    if (!participant) return null;

    return {
      entityId: participant.id,
      entityName: participant.name,
      abilities: [],
      damageDone: 0,
      damageTaken: 0,
      healingDone: 0,
      healingReceived: 0,
      healthRegenerated: 0,
      healthRegenerationPotential: 0,
      healthRegenerationOverhealed: 0,
      healthRegenerationPulses: 0,
      selfDamageDone: 0,
      selfDamageTaken: 0,
      alliedDamageDone: 0,
      alliedDamageTaken: 0,
      team: '',
      barrierGenerated: 0,
      damageBlocked: 0,
      damageRedirectedTo: 0,
      damageRedirectedAway: 0,
      targetedAttacks: 0,
      attentionSharePercent: 0,
      threatGenerated: 0,
    };
  }

  private abilitySortValue(ability: AbilityStats): number {
    switch (this.abilitySortColumn) {
      case 'uses':
        return ability.uses ?? 0;
      case 'healing':
        return ability.totalHealing ?? 0;
      case 'barrier':
        return ability.totalBarrier ?? 0;
      case 'threat':
        return ability.totalThreat ?? 0;
      default:
        return ability.totalDamage ?? 0;
    }
  }

  private mergeDamageByType(
    left: readonly AbilityDamageTypeStats[] | null | undefined,
    right: readonly AbilityDamageTypeStats[] | null | undefined,
  ): AbilityDamageTypeStats[] {
    const totals = new Map<DamageType, number>();
    for (const entry of [...(left ?? []), ...(right ?? [])]) {
      totals.set(
        entry.damageType,
        (totals.get(entry.damageType) ?? 0) + (entry.totalDamage ?? 0),
      );
    }
    return [...totals].map(([damageType, totalDamage]) => ({
      damageType,
      totalDamage,
    }));
  }

  private primaryOutputTotal(ability: AbilityStats): number {
    if (ability.totalDamage > 0) return ability.totalDamage;
    if (ability.totalHealing > 0) return ability.totalHealing;
    return ability.totalBarrier ?? 0;
  }
}
