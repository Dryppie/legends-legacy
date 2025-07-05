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

  getWoodcuttingNodes(): GatheringNode[] {
    const gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_young_willow',
        name: 'Young Willow',
        levelRequirement: 1,
        description: 'A slender, supple tree often found near rivers. Its soft wood is easy to cut, making it ideal for novice woodcutters.',
        yield: 'Resources: Willow Log, Sticky Sap, Feather-Lined Nest, Silk Vine, Shimmering Leaf.'
      },
      {
        id: 'woodcutting_amberleaf_maple',
        name: 'Amberleaf Maple',
        levelRequirement: 25,
        description: 'With vibrant amber leaves and a sturdy trunk, this maple yields a denser wood valued by carpenters for its smooth grain.',
        yield: 'Resources: Maple Log, Amber Syrup, Sweet Bark Chips, Honeycomb, Glowing Amber.'
      },
      {
        id: 'woodcutting_ember_ash',
        name: 'Ember Ash',
        levelRequirement: 50,
        description: 'Ashen bark with a faint red glow gives this tree its name. Its timber burns hot and is often used in high-quality forges.',
        yield: 'Resources: Ash Log, Charcoal Chunk, Fire Beetle Carapace, Scorched Resin, Inferno Bark.'
      },
      {
        id: 'woodcutting_moon_birch',
        name: 'Moon Birch',
        levelRequirement: 75,
        description: 'Glowing faintly under moonlight, this rare birch produces a pale wood said to resonate with lunar energy.',
        yield: ''
      },
      {
        id: 'woodcutting_ironwood',
        name: 'Ironwood',
        levelRequirement: 100,
        description: 'True to its name, the trunk of this tree is incredibly dense and durable, requiring strength and precision to fell.',
        yield: ''
      },
      {
        id: 'woodcutting_sun_cedar',
        name: 'Sun Cedar',
        levelRequirement: 125,
        description: 'Basking in direct sunlight, this golden-toned cedar is prized for its fragrant wood and resistance to decay.',
        yield: ''
      },
      {
        id: 'woodcutting_frost_pine',
        name: 'Frost Pine',
        levelRequirement: 150,
        description: 'Native to the frozen highlands, this hardy pine is coated in rime. Its resilient wood is ideal for crafting cold-resistant gear.',
        yield: ''
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
        description: 'Loose chunks of brittle slate scattered near the surface. Ideal for beginners learning the basics of mining.',
        yield: 'Resources: Stone, Flint, Tiny Geode, Jagged Obsidian, Crystalline Powder.'
      },
      {
        id: 'mining_copperbloom_vein',
        name: 'Copperbloom Vein',
        levelRequirement: 25,
        description: 'Veins of copper threaded with green-blue bloom-like oxidization. The ore is soft and plentiful, perfect for novice smithing.',
        yield: 'Resources: Copper Ore, Veinstone Chip, Malachite Shard, Verdant Ore, Living Amber.'
      },
      {
        id: 'mining_tinspine_vein',
        name: 'Tinspine Vein',
        levelRequirement: 50,
        description: 'Thin, jagged deposits of tin that snake through the rock like a spine. Often found in brittle stone that crumbles easily.',
        yield: 'Resources: Tin Ore, River Pearl, Dull Quartz, Galvanic Dust, Frosted Metal Shard.'
      },
      {
        id: 'mining_ironheart_seam',
        name: 'Ironheart Seam',
        levelRequirement: 75,
        description: 'Dense clusters of dark, solid iron embedded deep in the mountain. Trusted by blacksmiths for its unyielding strength.',
        yield: ''
      },
      {
        id: 'mining_silverlight_vein',
        name: 'Silverlight Vein',
        levelRequirement: 100,
        description: 'Lustrous silver veins that reflect torchlight like moonbeams. This rare ore is highly sought after for enchanted gear.',
        yield: ''
      },
      {
        id: 'mining_goldflare_vein',
        name: 'Goldflare Vein',
        levelRequirement: 125,
        description: 'Radiating with a warm golden glow, this precious vein gleams beneath layers of tough granite. Symbol of wealth and ambition.',
        yield: ''
      },
      {
        id: 'mining_mithril_thread',
        name: 'Mithril Thread',
        levelRequirement: 150,
        description: 'Rare, silvery filaments of mithril woven through the deepest rock. Light as air yet stronger than steel—prized by master crafters.',
        yield: ''
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
