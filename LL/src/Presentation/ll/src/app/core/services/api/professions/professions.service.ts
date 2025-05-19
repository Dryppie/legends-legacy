import { Injectable } from '@angular/core';
import {
  catchError,
  Observable,
  of,
  shareReplay,
  startWith,
  Subject,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ApiService } from '../../api/api.service';
import {
  CraftingProfession,
  GatheringProfession,
  Profession,
  Recipe,
} from '../../../../shared/models/profession';
import { RECIPES_CONTENT } from '../../../../data/recipes-content';
import { CharacterProfession } from '../../../../shared/models/Dtos/characterProfession';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  private readonly refresh$ = new Subject<void>();

  /** cached, shared stream of professions */
  private readonly professionsObservable$ = this.refresh$.pipe(
    // make the first request immediately
    startWith(void 0),
    // hit the API whenever refresh$ emits
    switchMap(() =>
      this.api.get('profession').pipe(
        tap(() => console.log('[Professions] fetched')),
        catchError((err) => {
          console.error('[Professions] fetch failed', err);
          return throwError(() => err);
        }),
      ),
    ),
    // keep the latest value for all current & future subscribers
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  constructor(private readonly api: ApiService) {}

  /** Public readonly stream.  Subscribe or use it with the async-pipe. */
  get professions$(): Observable<CharacterProfession[]> {
    return this.professionsObservable$;
  }

  /** Call this after *any* change (create / update / delete) to bust the cache */
  refresh(): void {
    this.refresh$.next();
  }

  getProfessionById(id: string): Profession {
    if (id.includes('mining')) {
      return this.getMiningProfession();
    }
    if (id.includes('woodcutting')) {
      return this.getWoodcuttingProfession();
    }
    if (id.includes('weaponsmithing')) {
      return this.getWeaponsmithingProfession();
    }
    return { name: '', iconPath: '' };
  }

  getMiningProfession() {
    const miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNodes: this.getMiningNodes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    const woodcuttingProfession: GatheringProfession = {
      name: 'Woodcutting',
      gatheringNodes: this.getWoodcuttingNodes(),
      iconPath: 'woodcutting',
    };
    return woodcuttingProfession;
  }

  getWeaponsmithingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Weaponsmithing',
      recipes: this.getWeaponsmithingRecipes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWeaponsmithingRecipes(): Recipe[] {
    return RECIPES_CONTENT;
  }

  getWoodcuttingNodes(): GatheringNode[] {
    const gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_young_willow',
        name: 'Young Willow',
        levelRequirement: 1,
      },
      {
        id: 'woodcutting_amberleaf_maple',
        name: 'Amberleaf Maple',
        levelRequirement: 25,
      },
      {
        id: 'woodcutting_ember_ash',
        name: 'Ember Ash',
        levelRequirement: 50,
      },
      {
        id: 'woodcutting_moon_birch',
        name: 'Moon Birch',
        levelRequirement: 75,
      },
      {
        id: 'woodcutting_ironwood',
        name: 'Ironwood',
        levelRequirement: 100,
      },
      {
        id: 'woodcutting_sun_cedar',
        name: 'Sun Cedar',
        levelRequirement: 125,
      },
      {
        id: 'woodcutting_frost_pine',
        name: 'Frost Pine',
        levelRequirement: 150,
      },
      // {
      //   id: 'woodcutting_blood_oak',
      //   name: 'Blood Oak',
      //   levelRequirement: 175
      // },
      // {
      //   id: 'woodcutting_shadow_willow',
      //   name: 'Shadow Willow',
      //   levelRequirement: 200
      // },
      // {
      //   id: 'woodcutting_lightning_elm',
      //   name: 'Lightning Elm',
      //   levelRequirement: 225
      // },
      // {
      //   id: 'woodcutting_ancestral_yew',
      //   name: 'Ancestral Yew',
      //   levelRequirement: 250
      // },
    ];

    return gatheringNodes;
  }

  getMiningNodes(): GatheringNode[] {
    const gatheringNodes: GatheringNode[] = [
      {
        id: 'mining_slate_shard',
        name: 'Slate Shard',
        levelRequirement: 1,
      },
      {
        id: 'mining_copperbloom_vein',
        name: 'Copperbloom Vein',
        levelRequirement: 25,
      },
      {
        id: 'mining_tinspine_vein',
        name: 'Tinspine Vein',
        levelRequirement: 50,
      },
      {
        id: 'mining_ironheart_seam',
        name: 'Ironheart Seam',
        levelRequirement: 75,
      },
      {
        id: 'mining_silverlight_vein',
        name: 'Silverlight Vein',
        levelRequirement: 100,
      },
      {
        id: 'mining_goldflare_vein',
        name: 'Goldflare Vein',
        levelRequirement: 125,
      },
      {
        id: 'mining_mithril_thread',
        name: 'Mithril Thread',
        levelRequirement: 150,
      },
      // {
      //   id: 'mining_adamant_ridge',
      //   name: 'Adamant Ridge',
      //   levelRequirement: 175
      // },
      // {
      //   id: 'mining_obsidian_mirror',
      //   name: 'Obsidian Mirror',
      //   levelRequirement: 200
      // },
      // {
      //   id: 'mining_arcanite_cluster',
      //   name: 'Arcanite Cluster',
      //   levelRequirement: 225
      // },
      // {
      //   id: 'mining_dragonstone_core',
      //   name: 'Dragonstone Core',
      //   levelRequirement: 250
      // },
    ];

    return gatheringNodes;
  }
}
