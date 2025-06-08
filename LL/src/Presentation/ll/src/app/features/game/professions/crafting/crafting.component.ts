import {
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgIf } from '@angular/common';
import {
  CraftingProfession,
  CraftType,
} from '../../../../shared/models/profession';
import { map } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { Tab } from '../../../../shared/models/sidebar-item';
import { TabComponent } from '../../../../shared/components/tabs/tab/tab.component';
import { RegularCraftingComponent } from './regular-crafting/regular-crafting.component';
import { TemperingComponent } from './tempering/tempering.component';
import { EquipmentType } from '../../../../shared/models/Dtos/equipmentSlot';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Equipment } from '../../../../shared/models/item';
import { toSignal } from '@angular/core/rxjs-interop';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { TabsComponent } from '../../../../shared/components/tabs/tabs.component';

@Component({
  selector: 'app-crafting',
  standalone: true,
  imports: [
    ProfessionHeaderComponent,
    NgIf,
    TabComponent,
    RegularCraftingComponent,
    TemperingComponent,
    TabsComponent,
  ],
  templateUrl: './crafting.component.html',
})
export class CraftingComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly professionService = inject(ProfessionsService);
  private readonly inventoryState = inject(InventoryStateService);

  readonly professionId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  readonly profession = signal<CraftingProfession | null>(null);

  // Stub until you wire real actions/queue in the service
  allowedTypesByCraft: Record<CraftType, EquipmentType[]> = {
    [CraftType.JewelryCrafting]: [EquipmentType.Ring, EquipmentType.Necklace],
    [CraftType.ArmorForging]: [
      EquipmentType.Head,
      EquipmentType.Chest,
      EquipmentType.Legs,
      EquipmentType.Relic,
    ],
    [CraftType.WeaponSmithing]: [EquipmentType.MainHand, EquipmentType.OffHand],
  };
  // ────────────────────────────────────── ctor/di ─────────────────────────────
  constructor() {
    effect(
      () => {
        const id = this.professionId();
        if (id) {
          this.getProfessionDetails(id);
        }
      },
      { allowSignalWrites: true },
    );
  }

  readonly craftType = computed<CraftType>(() => {
    return (
      (this.profession()?.professionType as unknown as CraftType) ??
      CraftType.ArmorForging
    );
  });

  readonly recipes = computed(() => {
    const prof = this.profession() as CraftingProfession | null;
    if (!prof) return [];
    return prof.recipes.filter((r) => r.craftType === this.craftType());
  });

  readonly inventory = computed(() => this.inventoryState.items());

  readonly characterProfessions = this.professionService.characterProfessions;
  readonly characterProfession = computed(() =>
    this.characterProfessions().find(
      (p) => p.professionType.toLocaleLowerCase() === this.professionId(),
    ),
  );

  readonly inventoryEquipment = computed(() => {
    const inventory = this.inventoryState.items();
    const prof = this.profession();
    if (!inventory || !prof) return [];

    const allowed = this.allowedTypesByCraft[this.craftType()];

    return this.inventoryState.items().filter((i) => {
      return (
        i.itemInstance.itemBase.itemType === ItemType.Equipment &&
        allowed.includes(
          (i.itemInstance.itemBase as Equipment).equipmentType as EquipmentType,
        )
      );
    }) as typeof inventory;
  });

  ngOnInit(): void {
    this.professionService.refresh();
  }

  getProfessionDetails(id: string) {
    const prof = this.professionService.getProfessionById(
      id,
    ) as CraftingProfession;
    this.profession.set(prof);
  }
}
