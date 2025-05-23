import { Component, OnInit } from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { AsyncPipe, NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import {
  CraftingProfession,
  CraftType,
} from '../../../../shared/models/profession';
import { map, of, shareReplay, switchMap, tap } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { CharacterManagerService } from '../../../../core/services/client-side/character-manager/character-manager.service';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { Tab } from '../../../../shared/models/sidebar-item';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { RegularCraftingComponent } from './regular-crafting/regular-crafting.component';
import { TemperingComponent } from './tempering/tempering.component';

@Component({
  selector: 'app-crafting',
  standalone: true,
  imports: [
    ProfessionHeaderComponent,
    NgIf,
    AsyncPipe,
    TabComponent,
    NgSwitch,
    NgSwitchCase,
    RegularCraftingComponent,
    TemperingComponent,
  ],
  templateUrl: './crafting.component.html',
  styleUrl: './crafting.component.css',
})
export class CraftingComponent implements OnInit {
  readonly profession$;
  readonly recipes$;
  readonly inventory$;

  craftType: CraftType = CraftType.ArmorForging;
  // Stub until you wire real actions/queue in the service

  // ────────────────────────────────────── ctor/di ─────────────────────────────
  constructor(
    private readonly route: ActivatedRoute,
    private readonly professionService: ProfessionsService,
    private readonly characterManager: CharacterManagerService,
    private readonly inventoryService: InventoryService,
  ) {
    this.profession$ = this.route.paramMap.pipe(
      map((p) => p.get('id') ?? ''),
      switchMap(async (id) => this.professionService.getProfessionById(id)),
      tap((profession) => {
        this.craftType = profession.professionType as unknown as CraftType;
      }),
      shareReplay(1),
    );

    this.recipes$ = this.profession$.pipe(
      map((p) =>
        (p as CraftingProfession).recipes.filter(
          (r) => r.craftType === this.craftType,
        ),
      ),
    );

    this.inventory$ = this.characterManager.inventory$.pipe(
      switchMap(
        (invDto) =>
          invDto // already cached?
            ? of(invDto) // → just pass it through
            : this.inventoryService.getInventory(), // → make a network call
      ),
      shareReplay(1), // every subscriber sees the same value
    );
  }

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
