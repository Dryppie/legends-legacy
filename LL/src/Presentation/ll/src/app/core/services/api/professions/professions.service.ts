import { Injectable } from '@angular/core';
import {
  BehaviorSubject,
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
  CraftType,
  GatheringProfession,
  Profession,
  Recipe,
} from '../../../../shared/models/profession';
import { RECIPES_CONTENT } from '../../../../data/recipes-content';
import {
  CharacterProfession,
  ProfessionType,
} from '../../../../shared/models/Dtos/characterProfession';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  private readonly refresh$ = new Subject<void>();
  private readonly localProfessions: CharacterProfession[] = [];
  private readonly professionsSubject$ = new BehaviorSubject<
    CharacterProfession[]
  >([]);

  /** cached, shared stream of professions */
  private readonly characterProfessionsObservable$ = this.refresh$.pipe(
    startWith(void 0),
    switchMap(() =>
      this.api.get('profession').pipe(
        tap((professions) => {
          // Update local cache
          this.localProfessions.length = 0;
          this.localProfessions.push(...professions);
          this.professionsSubject$.next([...this.localProfessions]);
        }),
      ),
    ),
    shareReplay({ bufferSize: 1, refCount: true }),
  );

  constructor(private readonly api: ApiService) {
    this.characterProfessionsObservable$.subscribe();
  }

  /** Public readonly stream.  Subscribe or use it with the async-pipe. */
  get characterProfessions$(): Observable<CharacterProfession[]> {
    return this.professionsSubject$.asObservable();
  }

  /** Call this after *any* change (create / update / delete) to bust the cache */
  refresh(): void {
    this.refresh$.next();
  }

  addExperience(professionType: ProfessionType, experience: number): void {
    const profession = this.localProfessions.find(
      (p) => p.professionType === professionType,
    );
    if (!profession) return;

    profession.experience += experience;
    let leveledUp = false;
    while (profession.experience >= profession.experienceUntilNextLevel) {
      profession.experience -= profession.experienceUntilNextLevel;
      profession.level++;
      leveledUp = true;

      // Optional: Recalculate experienceUntilNextLevel if it scales
      // profession.experienceUntilNextLevel = calculateNextLevelThreshold(profession.level);
    }
    this.professionsSubject$.next([...this.localProfessions]); // emit new state

    if (leveledUp) {
      this.refresh(); // Pull updated data from the backend
    }
  }

  getProfession(
    professionType: ProfessionType,
  ): CharacterProfession | undefined {
    return this.localProfessions.find(
      (p) => p.professionType === professionType,
    );
  }

  emitUpdate(): void {
    this.professionsSubject$.next([...this.localProfessions]);
  }

  getProfessionById(id: string): Profession {
    if (id.includes('mining')) {
      return this.getMiningProfession();
    }
    if (id.includes('woodcutting')) {
      return this.getWoodcuttingProfession();
    }
    if (id.includes('armorforging')) {
      return this.getArmorforgingProfession();
    }
    if (id.includes('jewelcrafting')) {
      return this.getJewelcraftingProfession();
    }
    if (id.includes('weaponsmithing')) {
      return this.getWeaponsmithingProfession();
    }
    return {
      name: '',
      iconPath: '',
      professionType: ProfessionType.ArmorForging,
    };
  }

  getMiningProfession() {
    const miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNodes: this.getMiningNodes(),
      iconPath: 'mining',
      professionType: ProfessionType.Mining,
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    const woodcuttingProfession: GatheringProfession = {
      name: 'Woodcutting',
      gatheringNodes: this.getWoodcuttingNodes(),
      iconPath: 'woodcutting',
      professionType: ProfessionType.Woodcutting,
    };
    return woodcuttingProfession;
  }

  getArmorforgingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Armorforging',
      recipes: this.getWeaponsmithingRecipes(),
      iconPath: 'mining',
      professionType: ProfessionType.ArmorForging,
    };
    return miningProfession;
  }

  getJewelcraftingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Jewelcrafting',
      recipes: this.getWeaponsmithingRecipes(),
      iconPath: 'mining',
      professionType: ProfessionType.JewelryCrafting,
    };
    return miningProfession;
  }

  getWeaponsmithingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Weaponsmithing',
      recipes: this.getWeaponsmithingRecipes(),
      iconPath: 'mining',
      professionType: ProfessionType.WeaponSmithing,
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
