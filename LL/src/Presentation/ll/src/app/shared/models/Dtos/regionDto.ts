export interface Region {
  name: string;
  areas: Area[];
  dungeons: Dungeon[];
  raids: Raid[];
}

export interface Area {
  name: string;
  creatures: string[];
  // creatures: Creature[];
}

export interface Dungeon {
  name: string;
  creatures: string[];
  // creatures: Creature[];
}

export interface Raid {
  name: string;
  creatures: string[];
  // creatures: Creature[];
}
