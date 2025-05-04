import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ApiService } from '../../api/api.service';
import {
  CraftingProfession,
  GatheringProfession,
  Profession,
  Recipe,
} from '../../../../shared/models/profession';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../shared/models/enums/rarity';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  constructor(private apiService: ApiService) {}

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
    let miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNodes: this.getMiningNodes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    let woodcuttingProfession: GatheringProfession = {
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
    let weaponsmithing: Recipe[] = [
      {
        id: 'rough_iron_knife',
        name: 'Rough Iron Knife',
        levelRequirement: 1,
        item: {
          name: 'Rough Iron Knife',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Charcoal Chunk',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'pickaxe_mk1',
        name: 'Iron Pickaxe Mk I',
        levelRequirement: 1,
        item: {
          name: 'Iron Pickaxe Mk I',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Sticky Sap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'iron_nails',
        name: 'Iron Nails',
        levelRequirement: 3,
        item: {
          name: 'Iron Nails',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 20,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'horseshoe',
        name: 'Horseshoe',
        levelRequirement: 3,
        item: {
          name: 'Horseshoe',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 4,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Charcoal Chunk',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'copper_dagger',
        name: 'Copper Dagger',
        levelRequirement: 5,
        item: {
          name: 'Copper Dagger',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Copper Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Sticky Sap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'copper_hatchet',
        name: 'Copper Hatchet',
        levelRequirement: 5,
        item: {
          name: 'Copper Hatchet',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Copper Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Sticky Sap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'copper_breastplate',
        name: 'Copper Breastplate',
        levelRequirement: 8,
        item: {
          name: 'Copper Breastplate',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Copper Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 8,
          },
          {
            item: {
              name: 'Amber Syrup',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Leather Strap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'bronze_short_sword',
        name: 'Bronze Short‑Sword',
        levelRequirement: 10,
        item: {
          name: 'Bronze Short‑Sword',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Bronze Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'River Pearl',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'bronze_axe',
        name: 'Bronze Axe',
        levelRequirement: 10,
        item: {
          name: 'Bronze Axe',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Bronze Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 4,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'iron_longsword',
        name: 'Iron Longsword',
        levelRequirement: 15,
        item: {
          name: 'Iron Longsword',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 5,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Hematite Lump',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'iron_shield',
        name: 'Iron Shield',
        levelRequirement: 15,
        item: {
          name: 'Iron Shield',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Wooden Board',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Hematite Lump',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'mining_pick_mk2',
        name: 'Mining Pick Mk II',
        levelRequirement: 15,
        item: {
          name: 'Mining Pick Mk II',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Iron Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 4,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Hematite Lump',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'steel_warhammer',
        name: 'Steel Warhammer',
        levelRequirement: 20,
        item: {
          name: 'Steel Warhammer',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Steel Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Iron Bark Shard',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'steel_armor_set',
        name: 'Steel Armor Set',
        levelRequirement: 20,
        item: {
          name: 'Steel Armor Set',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Steel Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 12,
          },
          {
            item: {
              name: 'Iron Bark Shard',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Leather Strap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 4,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'mithril_rapier',
        name: 'Mithril Rapier',
        levelRequirement: 25,
        item: {
          name: 'Mithril Rapier',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Mithril Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 4,
          },
          {
            item: {
              name: 'Mystic Quartz',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Leather Grip',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'mithril_woodcutting_axe',
        name: 'Mithril Woodcutting Axe',
        levelRequirement: 25,
        item: {
          name: 'Mithril Woodcutting Axe',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Mithril Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 5,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Mystic Quartz',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'mithril_plate_armor',
        name: 'Mithril Plate Armor',
        levelRequirement: 30,
        item: {
          name: 'Mithril Plate Armor',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Mithril Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 14,
          },
          {
            item: {
              name: 'Lava Core',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Leather Strap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'mithril_pick_mk3',
        name: 'Mithril Pick Mk III',
        levelRequirement: 30,
        item: {
          name: 'Mithril Pick Mk III',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Mithril Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Lava Core',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'adamantite_greataxe',
        name: 'Adamantite Greataxe',
        levelRequirement: 35,
        item: {
          name: 'Adamantite Greataxe',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Adamantite Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 8,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Obsidian Shard',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'shield_wall',
        name: 'Shield Wall',
        levelRequirement: 35,
        item: {
          name: 'Shield Wall',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Adamantite Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 10,
          },
          {
            item: {
              name: 'Obsidian Shard',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Leather Strap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'obsidian_katana',
        name: 'Obsidian Katana',
        levelRequirement: 40,
        item: {
          name: 'Obsidian Katana',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Obsidian Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Fire Opal',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Wooden Grip',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'obsidian_pick_mk4',
        name: 'Obsidian Pick Mk IV',
        levelRequirement: 40,
        item: {
          name: 'Obsidian Pick Mk IV',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Obsidian Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 7,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Fire Opal',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'arcanite_staff_blade',
        name: 'Arcanite Staff‑Blade',
        levelRequirement: 45,
        item: {
          name: 'Arcanite Staff‑Blade',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Arcanite Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Arcane Crystal',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Rune Fragment',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Wooden Shaft',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'arcanite_armor_set',
        name: 'Arcanite Armor Set',
        levelRequirement: 45,
        item: {
          name: 'Arcanite Armor Set',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Arcanite Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 12,
          },
          {
            item: {
              name: 'Arcane Crystal',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Rune Fragment',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Leather Strap',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 4,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'rune_smith_tool_set',
        name: 'Rune Smith Tool Set',
        levelRequirement: 45,
        item: {
          name: 'Rune Smith Tool Set',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Arcanite Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 8,
          },
          {
            item: {
              name: 'Arcane Crystal',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
          {
            item: {
              name: 'Rune Fragment',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 3,
          },
          {
            item: {
              name: 'Wooden Handle',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'dragonsteel_sword_of_ages',
        name: 'Dragonsteel Sword of Ages',
        levelRequirement: 50,
        item: {
          name: 'Dragonsteel Sword of Ages',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Dragonsteel Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 8,
          },
          {
            item: {
              name: 'Dragon Scale',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
          {
            item: {
              name: 'Heartfire Gem',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 1,
          },
        ],
        itemType: ItemType.Equipment,
      },
      {
        id: 'dragon_scale_armor',
        name: 'Dragon‑Scale Armor',
        levelRequirement: 50,
        item: {
          name: 'Dragon‑Scale Armor',
          description: '',
          iconPath: '',
          id: '0',
          itemType: ItemType.Equipment,
          rarity: Rarity.Common,
        },
        quantity: 1,
        materials: [
          {
            item: {
              name: 'Dragonsteel Ingot',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 14,
          },
          {
            item: {
              name: 'Dragon Scale',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 6,
          },
          {
            item: {
              name: 'Heartfire Gem',
              description: '',
              iconPath: '',
              id: '0',
              itemType: ItemType.Material,
              rarity: Rarity.Common,
            },
            quantity: 2,
          },
        ],
        itemType: ItemType.Equipment,
      },
    ];
    return weaponsmithing;
  }

  getWoodcuttingNodes(): GatheringNode[] {
    let gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_young_willow',
        name: 'Young Willow',
      },
      {
        id: 'woodcutting_amberleaf_maple',
        name: 'Amberleaf Maple',
      },
      {
        id: 'woodcutting_ember_ash',
        name: 'Ember Ash',
      },
      {
        id: 'woodcutting_moon_birch',
        name: 'Moon Birch',
      },
      {
        id: 'woodcutting_ironwood',
        name: 'Ironwood',
      },
      {
        id: 'woodcutting_sun_cedar',
        name: 'Sun Cedar',
      },
      {
        id: 'woodcutting_frost_pine',
        name: 'Frost Pine',
      },
      // {
      //   id: 'woodcutting_blood_oak',
      //   name: 'Blood Oak',
      // },
      // {
      //   id: 'woodcutting_shadow_willow',
      //   name: 'Shadow Willow',
      // },
      // {
      //   id: 'woodcutting_lightning_elm',
      //   name: 'Lightning Elm',
      // },
      // {
      //   id: 'woodcutting_ancestral_yew',
      //   name: 'Ancestral Yew',
      // },
    ];

    return gatheringNodes;
  }

  getMiningNodes(): GatheringNode[] {
    let gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_tree',
        name: 'Slate Shard',
      },
      {
        id: 'woodcutting_oak',
        name: 'Copperbloom Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Tinspine Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Ironheart Seam',
      },
      {
        id: 'woodcutting_',
        name: 'Silverlight Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Goldflare Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Mithril Thread',
      },
      // {
      //   id: 'woodcutting_',
      //   name: 'Adamant Ridge',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Obsidian Mirror',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Arcanite Cluster',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Dragonstone Core',
      // },
    ];

    return gatheringNodes;
  }
}
