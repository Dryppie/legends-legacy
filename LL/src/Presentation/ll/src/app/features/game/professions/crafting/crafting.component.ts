import {
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import {
  CraftingProfession,
  CraftType,
} from '../../../../shared/models/profession';
import { map, of, switchMap } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterManagerService } from '../../../../core/services/client-side/character-manager/character-manager.service';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { Tab } from '../../../../shared/models/sidebar-item';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { RegularCraftingComponent } from './regular-crafting/regular-crafting.component';
import { TemperingComponent } from './tempering/tempering.component';
import { EquipmentType } from '../../../../shared/models/Dtos/equipmentSlot';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Equipment } from '../../../../shared/models/item';
import { toSignal } from '@angular/core/rxjs-interop';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';

@Component({
  selector: 'app-crafting',
  standalone: true,
  imports: [
    ProfessionHeaderComponent,
    NgIf,
    TabComponent,
    NgSwitch,
    NgSwitchCase,
    RegularCraftingComponent,
    TemperingComponent,
  ],
  templateUrl: './crafting.component.html',
})
export class CraftingComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly professionService = inject(ProfessionsService);
  private readonly inventoryState = inject(InventoryStateService);
  private readonly inventoryService = inject(InventoryService);

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
          this.profession.set(
            this.professionService.getProfessionById(id) as CraftingProfession,
          );
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

  readonly characterProfession = computed(() => {
    const prof = this.profession();
    if (!prof) return undefined;
    return this.professionService
      .characterProfessions()
      .find((cp) => cp.professionType === prof.professionType);
  });

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
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  ngOnDestroy(): void {}

  tabs: Tab[] = [
    {
      label: 'Crafting',
      items: [],
    },
    {
      label: 'Tempering',
      items: [],
    },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
