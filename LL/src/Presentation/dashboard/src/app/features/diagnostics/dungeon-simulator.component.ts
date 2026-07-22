import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';
import {
  DungeonSimulationDungeonOption,
  DungeonSimulationOptions,
  DungeonSimulationReport,
  DungeonSimulationRequest,
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
export class DungeonSimulatorComponent implements OnInit {
  private readonly numberFormatter = new Intl.NumberFormat();

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

  request: DungeonSimulationRequest = {
    dungeonDefinitionId: '',
    runCount: 25,
    randomSeed: 1337,
    masteryLevel: 0,
    routeStrategy: 'Random',
    character: {
      name: 'Simulated Character',
      level: 10,
      maxHealth: 500,
      power: 50,
      armor: 20,
      resistance: 20,
      precision: 20,
      critChance: 5,
      critDamage: 100,
      attackSpeed: 10,
      essenceIds: [],
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
        },
        error: (error: Error) => {
          this.error = error.message || 'Unable to load simulator options.';
        },
      });
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
        next: (report) => (this.report = report),
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
    const character = this.request.character;
    return Math.max(
      0,
      Math.round(
        character.power * 8 +
          character.precision * 8 +
          character.critChance * 4 +
          character.critDamage * 1.5 +
          character.attackSpeed * 3 +
          character.maxHealth * 0.18 +
          character.armor * 4 +
          character.resistance * 4 +
          Math.max(1, character.level) * 10,
      ),
    );
  }

  isEssenceSelected(essenceId: string): boolean {
    return this.request.character.essenceIds.includes(essenceId);
  }

  toggleEssence(essenceId: string, checked: boolean): void {
    const selected = new Set(this.request.character.essenceIds);
    checked ? selected.add(essenceId) : selected.delete(essenceId);
    this.request.character.essenceIds = [...selected];
  }

  clearEssences(): void {
    this.request.character.essenceIds = [];
  }

  trackDungeon(_: number, dungeon: DungeonSimulationDungeonOption): string {
    return dungeon.id;
  }

  trackEssence(_: number, essence: { id: string }): string {
    return essence.id;
  }

  trackRun(_: number, run: { runNumber: number }): number {
    return run.runNumber;
  }
}
