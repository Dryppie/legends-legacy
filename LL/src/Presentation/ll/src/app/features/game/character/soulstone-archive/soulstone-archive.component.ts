import { Component, OnInit } from '@angular/core';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { AsyncPipe, NgIf } from '@angular/common';
import { SoulstoneUpgradeService } from '../../../../core/services/api/soulstone-upgrade/soulstone-upgrade.service';
import { map, Observable, shareReplay } from 'rxjs';
import { SoulstoneUpgradeView } from '../../../../shared/models/soulstones/soulstone-upgrade-view';
import { SoulstoneUpgradeType } from '../../../../shared/models/soulstones/soulstone-upgrade-type';
import { SoulstoneUpgradeCardComponent } from './soulstone-upgrade-card/soulstone-upgrade-card.component';

@Component({
  selector: 'app-soulstone-archive',
  standalone: true,
  imports: [
    DefaultHeaderComponent,
    AsyncPipe,
    NgIf,
    SoulstoneUpgradeCardComponent,
  ],
  templateUrl: './soulstone-archive.component.html',
})
export class SoulstoneArchiveComponent implements OnInit {
  readonly character$;
  readonly soulstoneUpgrades$;

  combatUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  gatheringUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  craftingUpgrades$!: Observable<SoulstoneUpgradeView[]>;
  miscUpgrades$!: Observable<SoulstoneUpgradeView[]>;

  constructor(
    private readonly characterService: CharacterService,
    private readonly soulstoneUpgradeService: SoulstoneUpgradeService,
  ) {
    this.character$ = this.characterService.getCurrentCharacter();
    this.soulstoneUpgrades$ = this.soulstoneUpgradeService.soulstoneUpgrades$;
  }

  ngOnInit(): void {
    const grouped$ = this.soulstoneUpgrades$.pipe(shareReplay(1));

    this.combatUpgrades$ = grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Combat),
      ),
    );
    this.gatheringUpgrades$ = grouped$.pipe(
      map((list) =>
        list.filter(
          (u) => u.definition.type === SoulstoneUpgradeType.Gathering,
        ),
      ),
    );
    this.craftingUpgrades$ = grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Crafting),
      ),
    );
    this.miscUpgrades$ = grouped$.pipe(
      map((list) =>
        list.filter((u) => u.definition.type === SoulstoneUpgradeType.Misc),
      ),
    );
  }
}
