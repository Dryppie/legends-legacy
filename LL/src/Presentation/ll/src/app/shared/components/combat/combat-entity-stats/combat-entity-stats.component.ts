import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import {
  AbilityStats,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';

type CombatTeamName = 'Friendly' | 'Hostile';
type AbilitySortColumn = 'uses' | 'damage' | 'healing' | 'barrier';
type SortDirection = 'asc' | 'desc';
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

@Component({
  selector: 'app-combat-entity-stats',
  imports: [NgIf, NgFor, NgClass, DecimalPipe, RegularButtonComponent],
  templateUrl: './combat-entity-stats.component.html',
})
export class CombatEntityStatsComponent implements OnChanges {
  @Input() playerTeam: SimpleCombatEntityDto[] = [];
  @Input() enemyTeam: SimpleCombatEntityDto[] = [];
  @Input() entityStats!: EntityStats[];

  selectedStats: EntityStats | null = null;
  selectedEntityId: string = '';
  selectedEntityName: string = '';
  abilitySortColumn: AbilitySortColumn = 'damage';
  abilitySortDirection: SortDirection = 'desc';

  playerParticipants: StatsParticipant[] = [];
  enemyParticipants: StatsParticipant[] = [];
  private readonly rawStatsById = new Map<string, EntityStats>();
  private readonly aggregateStats = new Map<string, EntityStats>();
  private readonly expandedSummonGroups = new Set<string>();

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['entityStats'] ||
      changes['playerTeam'] ||
      changes['enemyTeam']
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
      this.pruneExpandedSummonGroups();
      this.refreshSelection();
    }
  }

  get visibleParticipants(): StatsParticipant[] {
    return [...this.playerParticipants, ...this.enemyParticipants].flatMap(
      (participant) =>
        participant.isSummonGroup &&
        this.expandedSummonGroups.has(participant.id)
          ? [participant, ...(participant.members ?? [])]
          : [participant],
    );
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

    const firstParticipant = participants[0];
    if (firstParticipant) {
      this.selectEntity(firstParticipant.id);
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
        totalHealing: 0,
        uses: 0,
        hits: 0,
        crits: 0,
        summons: 0,
        stuns: 0,
        selfDamage: 0,
        alliedDamage: 0,
        totalBarrier: 0,
      };
      abilities.set(ability.name, {
        name: ability.name,
        totalDamage: current.totalDamage + (ability.totalDamage ?? 0),
        totalHealing: current.totalHealing + (ability.totalHealing ?? 0),
        uses: current.uses + (ability.uses ?? 0),
        hits: current.hits + (ability.hits ?? 0),
        crits: current.crits + (ability.crits ?? 0),
        summons: current.summons + (ability.summons ?? 0),
        stuns: current.stuns + (ability.stuns ?? 0),
        selfDamage: current.selfDamage + (ability.selfDamage ?? 0),
        alliedDamage: current.alliedDamage + (ability.alliedDamage ?? 0),
        totalBarrier: current.totalBarrier + (ability.totalBarrier ?? 0),
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
      health: group.entity.health,
      maxHealth: group.entity.maxHealth,
      barrier: group.entity.barrier,
    };
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
      default:
        return ability.totalDamage ?? 0;
    }
  }

  private primaryOutputTotal(ability: AbilityStats): number {
    if (ability.totalDamage > 0) return ability.totalDamage;
    if (ability.totalHealing > 0) return ability.totalHealing;
    return ability.totalBarrier ?? 0;
  }
}
