import {
  Component,
  OnInit,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EquipmentLoadoutService } from '../../../core/services/api/equipment/equipment-loadout.service';
import { EssenceCombatActivity } from '../../models/essence-system';

@Component({
  selector: 'app-equipment-loadouts',
  standalone: true,
  imports: [NgFor, NgIf, FormsModule],
  templateUrl: './equipment-loadouts.component.html',
  styleUrl: './equipment-loadouts.component.scss',
})
export class EquipmentLoadoutsComponent implements OnInit {
  readonly state = inject(EquipmentLoadoutService);
  readonly selectedId = this.state.selectedId;
  readonly selected = this.state.selected;
  readonly confirmDelete = signal(false);
  name = '';
  copyTarget = '';
  readonly activities: { id: EssenceCombatActivity; label: string }[] = [
    { id: 'IdleCombat', label: 'Idle combat' },
    { id: 'Dungeon', label: 'Dungeons' },
    { id: 'Raid', label: 'Raids' },
    { id: 'WorldTower', label: 'World Tower' },
    { id: 'Arena', label: 'Arena' },
    { id: 'Tournament', label: '3v3 Tournament' },
    { id: 'RegionBoss', label: 'Region Boss' },
  ];

  constructor() {
    const savedName = computed(() =>
      JSON.stringify([this.selectedId(), this.selected()?.name]),
    );
    effect(() => {
      savedName();
      untracked(() => {
        this.name = this.selected()?.name ?? '';
      });
    });
    effect(() => {
      this.selectedId();
      untracked(() => {
        this.confirmDelete.set(false);
      });
    });
  }

  ngOnInit(): void {
    this.state.load();
  }

  select(id: string): void {
    this.state.select(id);
  }

  newLoadout(): void {
    this.state.newLoadout();
    if (!this.selected()) this.name = '';
  }

  canSaveName(): boolean {
    const name = this.name.trim();
    return (
      !this.state.busy() && this.selected()?.isUsable !== false &&
      !!name &&
      (this.selected()
        ? name !== this.selected()!.name
        : this.state.loadouts().length < this.state.limit())
    );
  }

  save(): void {
    if (this.canSaveName()) this.state.save(this.selectedId(), this.name);
  }

  remove(): void {
    const id = this.selectedId();
    if (id) this.state.remove(id);
  }

  toggleActivity(activity: EssenceCombatActivity): void {
    const loadout = this.selected();
    if (loadout)
      this.state.setActivities(
        loadout,
        activity,
        !loadout.autoUseActivities.includes(activity),
      );
  }

  activityOwner(activity: EssenceCombatActivity): string | undefined {
    return this.state
      .loadouts()
      .find(
        (loadout) =>
          loadout.id !== this.selectedId() &&
          loadout.autoUseActivities.includes(activity),
      )?.name;
  }
}
