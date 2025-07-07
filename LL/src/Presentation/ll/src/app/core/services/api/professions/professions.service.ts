import { Injectable, Signal, signal } from '@angular/core';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ApiService } from '../../api/api.service';
import {
  CraftingProfession,
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
  private readonly _professions = signal<CharacterProfession[]>([]);

  /** cached, shared stream of professions */
  get characterProfessions(): Signal<CharacterProfession[]> {
    return this._professions.asReadonly();
  }

  constructor(private readonly api: ApiService) {}

  /** Public readonly stream.  Subscribe or use it with the async-pipe. */
  refresh(): void {
    this.api.get('profession').subscribe((professions) => {
      // always emit a new array reference so signal change detection fires
      this._professions.set([...professions]);
    });
  }

  addExperience(professionType: ProfessionType, experience: number): void {
    // Work on a copy so we never mutate the current signal value in place
    const updated = [...this._professions()];
    const profession = updated.find((p) => p.professionType === professionType);
    if (!profession) return;

    profession.experience += experience;
    let leveledUp = false;

    while (profession.experience >= profession.experienceUntilNextLevel) {
      profession.experience -= profession.experienceUntilNextLevel;
      profession.level++;
      // profession.experienceUntilNextLevel = calculateNextLevelThreshold(profession.level);
      leveledUp = true;
    }

    this._professions.set(updated);

    if (leveledUp) {
      // ensure consistency with the backend after a level‑up
      this.refresh();
    }
  }

  getProfession(
    professionType: ProfessionType,
  ): CharacterProfession | undefined {
    return this._professions().find((p) => p.professionType === professionType);
  }

  emitUpdate(): void {
    this._professions.set([...this._professions()]);
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
    if (id.includes('jewelrycrafting')) {
      return this.getJewelrycraftingProfession();
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

  getGatheringProfessionsList(): GatheringProfession[] {
    return [this.getMiningProfession(), this.getWoodcuttingProfession()];
  }

  getMiningProfession() {
    const miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNode: this.getMiningNode(),
      iconPath: 'mining',
      professionType: ProfessionType.Mining,
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    const woodcuttingProfession: GatheringProfession = {
      name: 'Woodcutting',
      gatheringNode: this.getWoodcuttingNode(),
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

  getJewelrycraftingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Jewelrycrafting',
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

  getWoodcuttingNode(): GatheringNode {
    const gatheringNode: GatheringNode = {
      id: 'woodcutting_young_willow',
      name: 'Yggdrasil',
      levelRequirement: 1,
      description:
        'A slender, supple tree often found near rivers. Its soft wood is easy to cut, making it ideal for novice woodcutters.',
      yield: 'Resources: Wood.',
    };

    return gatheringNode;
  }

  getMiningNode(): GatheringNode {
    const gatheringNode: GatheringNode = {
      id: 'mining_slate_shard',
      name: 'Primordial Vein',
      levelRequirement: 1,
      description:
        'Loose chunks of brittle slate scattered near the surface. Ideal for beginners learning the basics of mining.',
      yield: 'Resources: Ore.',
    };

    return gatheringNode;
  }
}
