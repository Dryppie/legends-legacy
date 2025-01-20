import { Creature } from './creature';

export interface Region {
  name: string;
  areas: Area[];
}

export interface Area {
  name: string;
  creatures: string[];
  // creatures: Creature[];
}
