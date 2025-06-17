/* AUTO-GENERATED — DO NOT EDIT */
import { Recipe, CraftType } from '../shared/models/profession';
import { ItemType } from '../shared/models/enums/itemType';
import { Rarity } from '../shared/models/enums/rarity';
import { EquipmentType } from '../shared/models/enums/equipmentType';
import { AttributeType } from '../shared/models/enums/attributeType';
import { ModifierType } from '../shared/models/Dtos/attributesDto';

export const RECIPES_CONTENT = [
  {
    "id": "35ab5f73-3295-4e0f-a1af-6b453319157e",
    "name": "Jagged Obsidian Helm",
    "itemId": "jagged_obsidian_helm",
    "item": {
      "id": "jagged_obsidian_helm",
      "name": "Jagged Obsidian Helm",
      "description": "A heavy helmet forged from jagged obsidian and stone.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Head,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Willpower,
          "amount": 3,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Threat,
          "amount": 1,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 11,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "e7d8803e-d641-4b0a-beb1-93cff9dc378e",
    "name": "Jagged Obsidian Cuirass",
    "itemId": "jagged_obsidian_cuirass",
    "item": {
      "id": "jagged_obsidian_cuirass",
      "name": "Jagged Obsidian Cuirass",
      "description": "A sturdy cuirass crafted from jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Chest,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Constitution,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 28,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 14,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 7,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "fa80e383-3731-4bf2-b319-f6e466372abf",
    "name": "Jagged Obsidian Greaves",
    "itemId": "jagged_obsidian_greaves",
    "item": {
      "id": "jagged_obsidian_greaves",
      "name": "Jagged Obsidian Greaves",
      "description": "Reinforced greaves made of jagged obsidian and stone.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Legs,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Endurance,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 20,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 10,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "a7ee20d7-1085-4a36-b7e9-7578d719bfa3",
    "name": "Willow Hood",
    "itemId": "willow_hood",
    "item": {
      "id": "willow_hood",
      "name": "Willow Hood",
      "description": "A lightweight hood made from willow.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Head,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Instinct,
          "amount": 4,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Health,
          "amount": 7,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 13,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 18,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "d52ccebf-0c2e-4203-8938-69cbaab9392d",
    "name": "Willow Brigandine",
    "itemId": "willow_brigandine",
    "item": {
      "id": "willow_brigandine",
      "name": "Willow Brigandine",
      "description": "A balanced brigandine crafted from willow.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Chest,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Strength,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 21,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 26,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 7,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "bd388959-eee7-4e3f-a496-9e647221fec9",
    "name": "Willow Tassets",
    "itemId": "willow_tassets",
    "item": {
      "id": "willow_tassets",
      "name": "Willow Tassets",
      "description": "Light armor piece.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Legs,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.FightingSpirit,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 12,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "3ee3611d-35b0-4821-ab3e-8c61b20b76ec",
    "name": "Silk Capuche",
    "itemId": "silk_capuche",
    "item": {
      "id": "silk_capuche",
      "name": "Silk Capuche",
      "description": "A light silk hood.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Head,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Luck,
          "amount": 4,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.CritChance,
          "amount": 2,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 12,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "94216f95-ad2a-49bd-9f85-8f606b6f0b9a",
    "name": "Silk Vest",
    "itemId": "silk_vest",
    "item": {
      "id": "silk_vest",
      "name": "Silk Vest",
      "description": "An evasive vest made of silk.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Chest,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Dexterity,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 14,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 8,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "db0d6109-78d2-4d4c-8f20-a1805ef36814",
    "name": "Silk Trousers",
    "itemId": "silk_trousers",
    "item": {
      "id": "silk_trousers",
      "name": "Silk Trousers",
      "description": "Lightweight trousers.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Legs,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Agility,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 14,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 22,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "29c2e282-40cf-4262-bd6c-4e6c71d06c0a",
    "name": "Powder Hood",
    "itemId": "powder_hood",
    "item": {
      "id": "powder_hood",
      "name": "Powder Hood",
      "description": "A hood crafted from crystalline powder.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Head,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Perception,
          "amount": 4,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Mana,
          "amount": 7,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "36d77ec2-9b4a-4998-8660-89b01aaa8363",
    "name": "Powder Robe",
    "itemId": "powder_robe",
    "item": {
      "id": "powder_robe",
      "name": "Powder Robe",
      "description": "A robe woven with crystalline powder.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Chest,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Intelligence,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 25,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 18,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "8ae0644e-7cd4-40c0-9873-39be926f2a44",
    "name": "Powder Pants",
    "itemId": "powder_pants",
    "item": {
      "id": "powder_pants",
      "name": "Powder Pants",
      "description": "Crystalline trousers.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Legs,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Wisdom,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.ArmorForging,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 22,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 16,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "93c7853d-d73c-475e-abbb-64aef79dea1d",
    "name": "Geode-Heart Amulet",
    "itemId": "geode_heart_amulet",
    "item": {
      "id": "geode_heart_amulet",
      "name": "Geode-Heart Amulet",
      "description": "An amulet infused with earth's essence, offering earth resistance and critical damage.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Necklace,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.MaxMana,
          "amount": 11,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "6fcf468e-3e81-44fe-af44-94bea6ed2509",
    "name": "Leafglow Amulet",
    "itemId": "leafglow_amulet",
    "item": {
      "id": "leafglow_amulet",
      "name": "Leafglow Amulet",
      "description": "A glowing amulet that grants mana regeneration and a soft green light.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Necklace,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Agility,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "f3e6cb7a-cc56-4f02-bfd7-a7bfc1e04f77",
    "name": "Obsidian Shard Amulet",
    "itemId": "obsidian_shard_amulet",
    "item": {
      "id": "obsidian_shard_amulet",
      "name": "Obsidian Shard Amulet",
      "description": "A reflective amulet offering physical resistance and slight damage reflection.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Necklace,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.MaxHealth,
          "amount": 16,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "0ba06ecc-ecbc-4ff7-8a26-e85eef576b8e",
    "name": "Flint-Spark Ring",
    "itemId": "flint_spark_ring",
    "item": {
      "id": "flint_spark_ring",
      "name": "Flint-Spark Ring",
      "description": "A ring that enhances critical strikes and adds fire damage to attacks.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Ring,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.CritDamage,
          "amount": 7,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "071450ab-1c64-4f7e-8b70-e9a7596cc14a",
    "name": "Nestling Band",
    "itemId": "nestling_band",
    "item": {
      "id": "nestling_band",
      "name": "Nestling Band",
      "description": "A ring that enhances ranged accuracy and reduces fall damage.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Ring,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Wisdom,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "eaede364-9783-4807-b7ef-cdf3f8e8696c",
    "name": "Stoneguard Ring",
    "itemId": "stoneguard_ring",
    "item": {
      "id": "stoneguard_ring",
      "name": "Stoneguard Ring",
      "description": "A protective ring granting max HP and health regeneration out of combat.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Ring,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.PhysicalDefense,
          "amount": 18,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 30,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "4c34d8b2-70ec-449f-89f9-04da9ce387b7",
    "name": "Powdered Lens",
    "itemId": "powdered_lens",
    "item": {
      "id": "powdered_lens",
      "name": "Powdered Lens",
      "description": "A lens that reveals hidden secrets for a short duration.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Relic,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Perception,
          "amount": 4,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "1827f908-703e-45ce-bc31-b16f0a6a05e1",
    "name": "Sapling Totem",
    "itemId": "sapling_totem",
    "item": {
      "id": "sapling_totem",
      "name": "Sapling Totem",
      "description": "A totem that grants health regeneration while stationary.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Relic,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.ManaRegeneration,
          "amount": 1,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "f9f1a19a-805e-4380-b963-9b877ef122e4",
    "name": "Jagged Obsidian Idol",
    "itemId": "jagged_obsidian_idol",
    "item": {
      "id": "jagged_obsidian_idol",
      "name": "Jagged Obsidian Idol",
      "description": "An idol made from jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.Relic,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Threat,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.JewelryCrafting,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "0e86afb6-ccdb-488a-999b-1b5f5d960462",
    "name": "Stone-Edge Shortsword",
    "itemId": "stone_edge_shortsword",
    "item": {
      "id": "stone_edge_shortsword",
      "name": "Stone-Edge Shortsword",
      "description": "A sharp shortsword forged from stone and jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OneHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.FightingSpirit,
          "amount": 2,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Strength,
          "amount": 1,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 26,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "e1a02d8d-2b93-4e79-82ae-d53e265db518",
    "name": "Jagged Obsidian Hatchet",
    "itemId": "jagged_obsidian_hatchet",
    "item": {
      "id": "jagged_obsidian_hatchet",
      "name": "Jagged Obsidian Hatchet",
      "description": "A durable hatchet crafted from jagged obsidian and stone.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OneHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Strength,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 29,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 7,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "49c0b296-f981-4ba9-950e-bbb87380a669",
    "name": "Flint Morning Star",
    "itemId": "flint_morning_star",
    "item": {
      "id": "flint_morning_star",
      "name": "Flint Morning Star",
      "description": "A spiked mace made of flint and stone, dealing crushing damage.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OneHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Endurance,
          "amount": 4,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 25,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 21,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "d7c9f417-6f0f-4158-bd53-26bd9a76e035",
    "name": "Silkshadow Stiletto",
    "itemId": "silkshadow_stiletto",
    "item": {
      "id": "silkshadow_stiletto",
      "name": "Silkshadow Stiletto",
      "description": "A swift dagger made of silk vine and sharp flint.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OneHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Dexterity,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 24,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 7,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "4d5df6ee-b9dc-4bf4-89c2-3fc56125fbcb",
    "name": "Willow Cleaver",
    "itemId": "willow_cleaver",
    "item": {
      "id": "willow_cleaver",
      "name": "Willow Cleaver",
      "description": "A large cleaver made from sturdy willow wood and obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.FightingSpirit,
          "amount": 4,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Strength,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 63,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "271bf75d-353d-4bc9-946d-884c04e86fa8",
    "name": "Stone-Reaver Greataxe",
    "itemId": "stone_reaver_greataxe",
    "item": {
      "id": "stone_reaver_greataxe",
      "name": "Stone-Reaver Greataxe",
      "description": "A heavy greataxe crafted from solid stone and jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Strength,
          "amount": 5,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 75,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "19e32feb-4d6b-4b21-a594-d037dd45e9d2",
    "name": "Powder-Core Maul",
    "itemId": "powder_core_maul",
    "item": {
      "id": "powder_core_maul",
      "name": "Powder-Core Maul",
      "description": "A massive maul with a crystalline powder core, delivering crushing blows.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Endurance,
          "amount": 4,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Willpower,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 59,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 7,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "87a9a432-3582-4516-84f4-329a9930123b",
    "name": "Nest-Tip Pike",
    "itemId": "nest_tip_pike",
    "item": {
      "id": "nest_tip_pike",
      "name": "Nest-Tip Pike",
      "description": "A long pike tipped with feathers and flint for precision thrusts.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.FightingSpirit,
          "amount": 3,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Dexterity,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 8,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 1,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "8d31de43-d1bf-4f12-b6e0-7f606573b117",
    "name": "Stonebark Heater",
    "itemId": "stonebark_heater",
    "item": {
      "id": "stonebark_heater",
      "name": "Stonebark Heater",
      "description": "A sturdy shield combining stone and willow for reliable protection.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OffHand,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.PhysicalDefense,
          "amount": 15,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Block,
          "amount": 25,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 21,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 18,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "2ea004e5-e9a7-4f23-92df-2c9a65f10f31",
    "name": "Silk Protector",
    "itemId": "silk_protector",
    "item": {
      "id": "silk_protector",
      "name": "Silk Protector",
      "description": "A shield woven with silk, allowing for magical protection.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OffHand,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.MagicalDefense,
          "amount": 15,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Block,
          "amount": 25,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 15,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 23,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "4354fddd-1af6-40a1-b8b2-2ca3436d39a9",
    "name": "Shimmering Willow Staff",
    "itemId": "shimmering_willow_staff",
    "item": {
      "id": "shimmering_willow_staff",
      "name": "Shimmering Willow Staff",
      "description": "A magical staff made of willow wood and shimmering leaves.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Intelligence,
          "amount": 4,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 5,
      "scalingAttribute": AttributeType.Intelligence,
      "scalingAmount": 0.1
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 71,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 8,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "2c3ad41f-b2bf-4a93-aa9c-07a4781c77fc",
    "name": "Geode-Bound Grimoire",
    "itemId": "geode_bound_grimoire",
    "item": {
      "id": "geode_bound_grimoire",
      "name": "Geode-Bound Grimoire",
      "description": "A magical tome embedded with geodes.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OffHand,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Intelligence,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 18,
        "itemId": "sticky_sap",
        "item": {
          "id": "sticky_sap",
          "name": "Sticky Sap",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "ea5b8f8e-ddef-4766-987f-12ce7c56f6a6",
    "name": "Jagged Obsidian Wand",
    "itemId": "jagged_obsidian_wand",
    "item": {
      "id": "jagged_obsidian_wand",
      "name": "Jagged Obsidian Wand",
      "description": "A magical wand forged from jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OneHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Wisdom,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 18,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 19,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 5,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "9b6425ca-20c2-456c-b0f8-2880f9cdb228",
    "name": "Powdered Crystal Orb",
    "itemId": "powdered_crystal_orb",
    "item": {
      "id": "powdered_crystal_orb",
      "name": "Powdered Crystal Orb",
      "description": "An orb made from crystalline powder.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.OffHand,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Wisdom,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 24,
        "itemId": "stone",
        "item": {
          "id": "stone",
          "name": "Stone",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "tiny_geode",
        "item": {
          "id": "tiny_geode",
          "name": "Tiny Geode",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "a75ecbf3-0bd7-42eb-8378-0723391ca7ee",
    "name": "Silk-Strung Longbow",
    "itemId": "silk_strung_longbow",
    "item": {
      "id": "silk_strung_longbow",
      "name": "Silk-Strung Longbow",
      "description": "A longbow strung with resilient silk.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Agility,
          "amount": 5,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 69,
        "itemId": "willow_log",
        "item": {
          "id": "willow_log",
          "name": "Willow Log",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "feather_lined_nest",
        "item": {
          "id": "feather_lined_nest",
          "name": "Feather-lined Nest",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "f1bd183c-1d71-4441-a9a7-55b6eabe632c",
    "name": "Flintlock Crossbow",
    "itemId": "flintlock_crossbow",
    "item": {
      "id": "flintlock_crossbow",
      "name": "Flintlock Crossbow",
      "description": "A crossbow crafted using both willow and flint.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.Agility,
          "amount": 3,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.Willpower,
          "amount": 3,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 65,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 4,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 2,
        "itemId": "shimmering_leaf",
        "item": {
          "id": "shimmering_leaf",
          "name": "Shimmering Leaf",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  },
  {
    "id": "0375e5f4-aec9-4a08-940c-98105e66e0b7",
    "name": "Jagged Obsidian Knuckles",
    "itemId": "jagged_obsidian_knuckles",
    "item": {
      "id": "jagged_obsidian_knuckles",
      "name": "Jagged Obsidian Knuckles",
      "description": "A pair of hard knuckles made from jagged obsidian.",
      "stackable": false,
      "itemType": ItemType.Equipment,
      "rarity": Rarity.Common,
      "equipmentType": EquipmentType.TwoHanded,
      "attributeModifiers": [
        {
          "attributeType": AttributeType.FightingSpirit,
          "amount": 3,
          "modifierType": ModifierType.Flat
        },
        {
          "attributeType": AttributeType.MaxHealth,
          "amount": 25,
          "modifierType": ModifierType.Flat
        }
      ],
      "magnitude": 0,
      "scalingAttribute": AttributeType.Luck,
      "scalingAmount": 0
    },
    "quantity": 1,
    "craftType": CraftType.WeaponSmithing,
    "levelRequirement": 1,
    "materials": [
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 58,
        "itemId": "flint",
        "item": {
          "id": "flint",
          "name": "Flint",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Common
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 9,
        "itemId": "jagged_obsidian",
        "item": {
          "id": "jagged_obsidian",
          "name": "Jagged Obsidian",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 3,
        "itemId": "silk_vine",
        "item": {
          "id": "silk_vine",
          "name": "Silk Vine",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Uncommon
        }
      },
      {
        "recipeId": "00000000-0000-0000-0000-000000000000",
        "quantity": 6,
        "itemId": "crystalline_powder",
        "item": {
          "id": "crystalline_powder",
          "name": "Crystalline Powder",
          "description": "",
          "stackable": true,
          "itemType": ItemType.Material,
          "rarity": Rarity.Rare
        }
      }
    ],
    "itemType": ItemType.Equipment
  }
] satisfies Recipe[];
