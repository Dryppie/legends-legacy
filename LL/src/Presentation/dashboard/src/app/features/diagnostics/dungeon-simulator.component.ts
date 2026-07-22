import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';
import {
  DungeonSimulationDungeonOption,
  DungeonSimulationEquipmentSlotOption,
  DungeonSimulationOptions,
  DungeonSimulationReport,
  DungeonSimulationRequest,
  CombatRatingBreakdown,
} from '../../shared/models/diagnostics/dungeon-simulation';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../shared/components/custom-components/dropdown/dropdown.component';

@Component({
  selector: 'app-dungeon-simulator',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownComponent],
  templateUrl: './dungeon-simulator.component.html',
})
export class DungeonSimulatorComponent implements OnInit, OnDestroy {
  private readonly numberFormatter = new Intl.NumberFormat();
  private ratingRefreshTimer: ReturnType<typeof setTimeout> | null = null;
  private ratingRequestId = 0;

  readonly routeDropdownOptions: readonly DropdownOption<
    DungeonSimulationRequest['routeStrategy']
  >[] = [
    { label: 'Random', value: 'Random', detail: 'Varied paths' },
    { label: 'Safest', value: 'Safest', detail: 'Lowest risk' },
    { label: 'Hardest', value: 'Hardest', detail: 'Highest risk' },
  ];

  options: DungeonSimulationOptions | null = null;
  report: DungeonSimulationReport | null = null;
  loadingOptions = false;
  simulating = false;
  error: string | null = null;
  essenceFilter = '';
  ratingLoading = false;
  combatRating: CombatRatingBreakdown = {
    baseAndEquipment: 0,
    essenceAttributes: 0,
    essenceAbilities: 0,
    total: 0,
  };

  request: DungeonSimulationRequest = {
    dungeonDefinitionId: '',
    runCount: 25,
    randomSeed: 1337,
    masteryLevel: 0,
    routeStrategy: 'Random',
    character: {
      name: 'New Character',
      level: 1,
      maxHealth: 100,
      power: 10,
      fortitude: 10,
      spirit: 10,
      armor: 0,
      resistance: 0,
      precision: 10,
      critChance: 0,
      critDamage: 100,
      attackSpeed: 0,
      healthRegeneration: 2,
      essenceIds: [],
      equipment: {
        rarity: 'Common',
        equippedSlots: [],
      },
    },
  };

  constructor(private readonly diagnostics: DiagnosticsService) {}

  ngOnInit(): void {
    this.loadingOptions = true;
    this.diagnostics
      .getDungeonSimulationOptions()
      .pipe(finalize(() => (this.loadingOptions = false)))
      .subscribe({
        next: (options) => {
          this.options = options;
          this.request.dungeonDefinitionId =
            this.request.dungeonDefinitionId || options.dungeons[0]?.id || '';
          this.scheduleCombatRatingRefresh(0);
        },
        error: (error: Error) => {
          this.error = error.message || 'Unable to load simulator options.';
        },
      });
  }

  ngOnDestroy(): void {
    if (this.ratingRefreshTimer !== null) clearTimeout(this.ratingRefreshTimer);
    this.ratingRequestId++;
  }

  runSimulation(): void {
    if (!this.request.dungeonDefinitionId || this.simulating) return;

    this.simulating = true;
    this.error = null;
    this.report = null;
    this.diagnostics
      .runDungeonSimulation(this.request)
      .pipe(finalize(() => (this.simulating = false)))
      .subscribe({
        next: (report) => {
          this.report = report;
          this.combatRating = report.combatRatingBreakdown;
        },
        error: (error: Error) => {
          this.error = error.message || 'Dungeon simulation failed.';
        },
      });
  }

  get selectedDungeon(): DungeonSimulationDungeonOption | null {
    return (
      this.options?.dungeons.find(
        (dungeon) => dungeon.id === this.request.dungeonDefinitionId,
      ) ?? null
    );
  }

  get dungeonDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.dungeons ?? []).map((dungeon) => ({
      label: `${dungeon.name} — ${dungeon.difficulty}`,
      value: dungeon.id,
      detail: `${this.numberFormatter.format(dungeon.recommendedCombatRating)} power`,
    }));
  }

  selectDungeon(selection: DropdownSelection<string>): void {
    this.request.dungeonDefinitionId = selection.main;
  }

  selectRouteStrategy(
    selection: DropdownSelection<DungeonSimulationRequest['routeStrategy']>,
  ): void {
    this.request.routeStrategy = selection.main;
  }

  get equipmentRarityDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.equipmentRarities ?? []).map((rarity) => ({
      label: rarity.name,
      value: rarity.id,
      detail: `×${rarity.multiplier.toFixed(2)}`,
    }));
  }

  selectEquipmentRarity(selection: DropdownSelection<string>): void {
    this.request.character.equipment.rarity = selection.main;
    this.scheduleCombatRatingRefresh();
  }

  get filteredEssences() {
    const filter = this.essenceFilter.trim().toLowerCase();
    const essences = this.options?.essences ?? [];
    return filter
      ? essences.filter(
          (essence) =>
            essence.name.toLowerCase().includes(filter) ||
            essence.id.toLowerCase().includes(filter),
        )
      : essences;
  }

  get estimatedCombatRating(): number {
    return this.combatRating.total;
  }

  scheduleCombatRatingRefresh(delay = 180): void {
    if (!this.options) return;
    if (this.ratingRefreshTimer !== null) clearTimeout(this.ratingRefreshTimer);
    this.ratingRefreshTimer = setTimeout(
      () => this.refreshCombatRating(),
      delay,
    );
  }

  private refreshCombatRating(): void {
    this.ratingRefreshTimer = null;
    const requestId = ++this.ratingRequestId;
    this.ratingLoading = true;
    this.diagnostics
      .getDungeonSimulationCombatRating(this.request.character)
      .pipe(
        finalize(() => {
          if (requestId === this.ratingRequestId) this.ratingLoading = false;
        }),
      )
      .subscribe({
        next: (rating) => {
          if (requestId === this.ratingRequestId) this.combatRating = rating;
        },
      });
  }

  isEquipmentSlotSelected(slotId: string): boolean {
    return this.request.character.equipment.equippedSlots.includes(slotId);
  }

  toggleEquipmentSlot(slotId: string, checked: boolean): void {
    const selected = new Set(this.request.character.equipment.equippedSlots);
    checked ? selected.add(slotId) : selected.delete(slotId);
    this.request.character.equipment.equippedSlots = [...selected];
    this.scheduleCombatRatingRefresh();
  }

  clearEquipment(): void {
    this.request.character.equipment.equippedSlots = [];
    this.scheduleCombatRatingRefresh();
  }

  equipmentBonusSummary(slot: DungeonSimulationEquipmentSlotOption): string {
    const multiplier = this.selectedEquipmentRarityMultiplier;
    return Object.entries(slot.attributeBonuses)
      .map(
        ([attribute, value]) =>
          `+${Math.ceil(value * multiplier)} ${this.formatAttributeName(attribute)}`,
      )
      .join(' · ');
  }

  isEssenceSelected(essenceId: string): boolean {
    return this.request.character.essenceIds.includes(essenceId);
  }

  toggleEssence(essenceId: string, checked: boolean): void {
    const selected = new Set(this.request.character.essenceIds);
    checked ? selected.add(essenceId) : selected.delete(essenceId);
    this.request.character.essenceIds = [...selected];
    this.scheduleCombatRatingRefresh();
  }

  clearEssences(): void {
    this.request.character.essenceIds = [];
    this.scheduleCombatRatingRefresh();
  }

  trackDungeon(_: number, dungeon: DungeonSimulationDungeonOption): string {
    return dungeon.id;
  }

  trackEssence(_: number, essence: { id: string }): string {
    return essence.id;
  }

  trackEquipmentSlot(
    _: number,
    slot: DungeonSimulationEquipmentSlotOption,
  ): string {
    return slot.id;
  }

  trackRun(_: number, run: { runNumber: number }): number {
    return run.runNumber;
  }

  private get selectedEquipmentRarityMultiplier(): number {
    return (
      this.options?.equipmentRarities?.find(
        (rarity) => rarity.id === this.request.character.equipment.rarity,
      )?.multiplier ?? 1
    );
  }

  private formatAttributeName(attribute: string): string {
    return attribute.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
