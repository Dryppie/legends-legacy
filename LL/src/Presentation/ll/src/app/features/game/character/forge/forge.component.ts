import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ForgeStateService } from './forge-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EquipmentDisplayComponent } from '../../../../shared/components/equipment/equipment-display/equipment-display.component';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import {
  ForgeKind,
  StarterEquipmentKind,
} from '../../../../shared/models/equipment-progression';
import { formatAttributeType } from '../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { formatAttributeValue } from '../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-forge',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    DefaultHeaderComponent,
    RegularButtonComponent,
    EquipmentDisplayComponent,
  ],
  providers: [ForgeStateService],
  templateUrl: './forge.component.html',
  styleUrl: './forge.component.css',
})
export class ForgeComponent implements OnInit {
  readonly state = inject(ForgeStateService);
  private readonly route = inject(ActivatedRoute);
  readonly type = EquipmentType;
  readonly armorSlots = [
    EquipmentType.Head,
    EquipmentType.Chest,
    EquipmentType.Legs,
  ];
  readonly starterKinds: StarterEquipmentKind[] = [
    'FirstWeapon',
    'ReadyForRoad',
  ];
  activeTab = 'equipment';
  hands = 'two';
  kit: Record<string, string> = { Head: '', Chest: '', Legs: '' };
  offHand = '';
  mainHand = '';
  styleId = '';
  allowFavorite = false;
  starterReview = false;
  readonly operationNames: Record<ForgeKind, string> = {
    ImproveRank: 'Improve rank',
    ChangeStyle: 'Change style',
    Salvage: 'Salvage equipment',
    LearnStyle: 'Learn Blueprint style',
  };
  readonly attributeName = formatAttributeType;
  readonly attributeValue = formatAttributeValue;

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('tab') === 'rewards')
      this.activeTab = 'rewards';
    void this.state.initialize();
  }
  options(...types: EquipmentType[]) {
    return this.state.starters().filter((x) => types.includes(x.equipmentType));
  }
  get kitIds(): string[] {
    return [
      ...this.armorSlots.map((x) => this.kit[x] || ''),
      this.mainHand,
      ...(this.hands === 'two' ? [] : [this.offHand]),
    ];
  }
  get kitValid(): boolean {
    return (
      this.armorSlots.every((slot) =>
        this.options(slot).some((x) => x.definitionId === this.kit[slot]),
      ) &&
      this.options(
        this.hands === 'two' ? this.type.TwoHanded : this.type.OneHanded,
      ).some((x) => x.definitionId === this.mainHand) &&
      (this.hands === 'two' ||
        this.options(this.type.OneHanded, this.type.OffHand).some(
          (x) => x.definitionId === this.offHand,
        ))
    );
  }
  starterName(kind: StarterEquipmentKind): string {
    return kind === 'FirstWeapon' ? 'First Weapon' : 'Ready for the Road';
  }
  definitionName(id: string): string {
    return (
      this.state.starters().find((x) => x.definitionId === id)?.name ??
      this.label(id)
    );
  }
  label(id: string | null | undefined): string {
    if (!id) return 'Plain';
    return (
      this.state.styles().find((x) => x.id === id)?.name ??
      id
        .split('.')
        .pop()!
        .replace(/[_-]/g, ' ')
        .replace(/\b\w/g, (x) => x.toUpperCase())
    );
  }
  starterStats(id: string, ordinary = false): string {
    return Object.entries(
      (ordinary
        ? (this.state.ordinary()?.targets ?? [])
        : this.state.starters()
      ).find((x) => x.definitionId === id)?.stats ?? {},
    )
      .map(
        ([key, value]) =>
          `${formatAttributeType(key, true)} ${formatAttributeValue(value, key, false, true)}`,
      )
      .join(' · ');
  }
  labels(ids: string[]): string {
    return ids.map((id) => this.label(id)).join(', ') || 'None';
  }
  statRows(
    before: Record<string, number> | null | undefined,
    after: Record<string, number> | null | undefined,
  ) {
    return [
      ...new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})]),
    ]
      .sort()
      .map((key) => ({
        key,
        before: before?.[key] ?? 0,
        after: after?.[key] ?? 0,
      }));
  }
  missing(kind: StarterEquipmentKind): number {
    return this.state
      .recovery()
      .filter((x) => x.kind === kind)
      .reduce((sum, x) => sum + x.missing, 0);
  }
  preview(kind: ForgeKind): void {
    const id = this.state.selectedItemId();
    if (!id) return;
    void this.state.preview({
      kind,
      itemInstanceId: id,
      ...(kind === 'ChangeStyle' ? { styleId: this.styleId || null } : {}),
      ...(kind === 'Salvage'
        ? { allowFavoriteSalvage: this.allowFavorite }
        : {}),
    });
  }
  selectItem(id: string): void {
    this.styleId = '';
    this.allowFavorite = false;
    void this.state.selectItem(id);
  }
}
