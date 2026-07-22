import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { DiagnosticsService } from '../../core/services/api/diagnostics/diagnostics.service';
import {
  DungeonSimulationDungeonOption,
  DungeonSimulationEquipmentSlotOption,
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

  get equipmentRarityDropdownOptions(): readonly DropdownOption<string>[] {
    return (this.options?.equipmentRarities ?? []).map((rarity) => ({
      label: rarity.name,
      value: rarity.id,
      detail: `×${rarity.multiplier.toFixed(2)}`,
    }));
  }

  selectEquipmentRarity(selection: DropdownSelection<string>): void {
    this.request.character.equipment.rarity = selection.main;
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
    const attributes = this.effectiveAttributes;
    return Math.max(
      0,
      Math.round(
        this.attribute(attributes, 'Power') * 8 +
          this.attribute(attributes, 'Fortitude') * 8 +
          this.attribute(attributes, 'Precision') * 8 +
          this.attribute(attributes, 'Spirit') * 5 +
          this.attribute(attributes, 'WeaponDamage') * 18 +
          this.attribute(attributes, 'CritChance') * 4 +
          this.attribute(attributes, 'CritDamage') * 1.5 +
          this.attribute(attributes, 'ArmorPenetration') * 2 +
          this.attribute(attributes, 'MagicPenetration') * 2 +
          this.attribute(attributes, 'AttackSpeed') * 3 +
          this.attribute(attributes, 'MaxHealth') * 1.8 +
          this.attribute(attributes, 'Armor') * 4 +
          this.attribute(attributes, 'Resistance') * 4 +
          this.attribute(attributes, 'DodgeChance') * 5 +
          this.attribute(attributes, 'BlockChance') * 3 +
          this.attribute(attributes, 'DamageReduction') * 7 +
          this.attribute(attributes, 'HealingPowerPercent') * 2 +
          this.attribute(attributes, 'HealthRegeneration') * 8 +
          this.attribute(attributes, 'LifeSteal') * 4 +
          this.attribute(attributes, 'Cooldown') * 3 +
          this.attribute(attributes, 'StatusResistance') * 2 +
          this.attribute(attributes, 'CrowdControlResistance') * 2 +
          this.attribute(attributes, 'SummonPower') * 4 +
          this.attribute(attributes, 'SummonHealth') * 0.15,
      ),
    );
  }

  isEquipmentSlotSelected(slotId: string): boolean {
    return this.request.character.equipment.equippedSlots.includes(slotId);
  }

  toggleEquipmentSlot(slotId: string, checked: boolean): void {
    const selected = new Set(this.request.character.equipment.equippedSlots);
    checked ? selected.add(slotId) : selected.delete(slotId);
    this.request.character.equipment.equippedSlots = [...selected];
  }

  clearEquipment(): void {
    this.request.character.equipment.equippedSlots = [];
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

  private get effectiveAttributes(): Record<string, number> {
    const character = this.request.character;
    const attributes: Record<string, number> = {
      MaxHealth: character.maxHealth,
      Power: character.power,
      Fortitude: character.fortitude,
      Spirit: character.spirit,
      Armor: character.armor,
      Resistance: character.resistance,
      Precision: character.precision,
      CritChance: character.critChance,
      CritDamage: character.critDamage,
      AttackSpeed: character.attackSpeed,
      HealthRegeneration: character.healthRegeneration,
    };
    const equippedSlots = new Set(character.equipment.equippedSlots);
    const multiplier = this.selectedEquipmentRarityMultiplier;

    for (const slot of this.options?.equipmentSlots ?? []) {
      if (!equippedSlots.has(slot.id)) continue;

      for (const [attribute, value] of Object.entries(slot.attributeBonuses)) {
        attributes[attribute] =
          (attributes[attribute] ?? 0) + Math.ceil(value * multiplier);
      }
    }

    return attributes;
  }

  private attribute(attributes: Record<string, number>, name: string): number {
    return attributes[name] ?? 0;
  }

  private formatAttributeName(attribute: string): string {
    return attribute.replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
