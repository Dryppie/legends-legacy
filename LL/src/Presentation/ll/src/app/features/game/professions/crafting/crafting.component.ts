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
import { CraftingProfession } from '../../../../shared/models/profession';
import { map } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { TabComponent } from '../../../../shared/components/custom-components/tabs/tab/tab.component';
import { RegularCraftingComponent } from './regular-crafting/regular-crafting.component';
import { TemperingComponent } from './tempering/tempering.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Equipment } from '../../../../shared/models/item';
import { toSignal } from '@angular/core/rxjs-interop';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import { TabsComponent } from '../../../../shared/components/custom-components/tabs/tabs.component';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import { ToastService } from '../../../../core/services/client-side/components/toast/toast.service';
import {
  TUTORIAL_STEP_CRAFT_EQUIPMENT,
  TUTORIAL_STEP_EQUIP_EQUIPMENT,
} from '../../../../shared/models/tutorial';

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
  private readonly tutorialState = inject(TutorialStateService);
  private readonly toast = inject(ToastService);

  readonly professionId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id') ?? '')),
    { initialValue: '' },
  );

  readonly profession = signal<CraftingProfession | null>(null);
  readonly tutorialCraftingHandoff = signal(false);
  readonly guidePageId = computed(() => {
    if (!this.tutorialCraftingHandoff()) return 'crafting';

    const tutorial = this.tutorialState.state();
    return (
      tutorial?.presentation?.guidePageId ??
      tutorial?.guidePageId ??
      'crafting'
    );
  });

  private readonly craftableEquipmentTypes = [
    EquipmentType.Head,
    EquipmentType.Chest,
    EquipmentType.Legs,
    EquipmentType.Ring,
    EquipmentType.Necklace,
    EquipmentType.Relic,
    EquipmentType.TwoHanded,
    EquipmentType.OneHanded,
    EquipmentType.OffHand,
  ];
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

    effect(
      () => {
        if (this.tutorialState.loading()) {
          return;
        }

        const tutorial = this.tutorialState.state();
        this.tutorialCraftingHandoff.set(
          tutorial?.currentStep === TUTORIAL_STEP_CRAFT_EQUIPMENT &&
            !tutorial.isCompleted,
        );
      },
      { allowSignalWrites: true },
    );
  }

  readonly recipes = computed(() => {
    const prof = this.profession() as CraftingProfession | null;
    if (!prof) return [];
    return prof.recipes;
  });

  readonly inventory = computed(() => this.inventoryState.items());

  readonly characterProfessions = this.professionService.characterProfessions;
  readonly characterProfession = computed(() =>
    this.characterProfessions().find(
      (p) => p.professionType === ProfessionType.Crafting,
    ),
  );

  readonly inventoryEquipment = computed(() => {
    const inventory = this.inventoryState.items();
    const prof = this.profession();
    if (!inventory || !prof) return [];

    return this.inventoryState.items().filter((i) => {
      return (
        i.itemInstance.itemBase.itemType === ItemType.Equipment &&
        this.craftableEquipmentTypes.includes(
          (i.itemInstance.itemBase as Equipment).equipmentType as EquipmentType,
        )
      );
    }) as typeof inventory;
  });

  ngOnInit(): void {
    this.professionService.refresh();
    const wasClaimingTutorialGear =
      this.tutorialState.state()?.currentStep === TUTORIAL_STEP_CRAFT_EQUIPMENT;
    this.tutorialCraftingHandoff.set(wasClaimingTutorialGear);

    this.tutorialState.recordCraftingPageVisited((state) => {
      this.inventoryState.load(true);

      if (
        wasClaimingTutorialGear &&
        state?.currentStep === TUTORIAL_STEP_EQUIP_EQUIPMENT
      ) {
        this.toast.showToast(
          'Tutorial gear received',
          'Crafting granted a Tutorial Chest. Open Inventory to equip it.',
          true,
          'tr',
        );
      }
    });
  }

  getProfessionDetails(id: string) {
    const prof = this.professionService.getProfessionById(
      id,
    ) as CraftingProfession;
    this.profession.set(prof);
  }
}
