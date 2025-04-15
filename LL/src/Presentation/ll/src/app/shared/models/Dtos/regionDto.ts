export interface Region {
  name: string;
  areas: Area[];
  dungeons: Dungeon[];
  raids: Raid[];
}

export interface Area {
  id: string;
  name: string;
  creatures: string[];
  description: string;
  // creatures: Creature[];
}

export interface Dungeon {
  id: string;
  name: string;
  creatures: string[];
  // creatures: Creature[];
}

export interface Raid {
  id: string;
  name: string;
  creatures: string[];
  // creatures: Creature[];
}
