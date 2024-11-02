import { Creature } from "./creature";

export interface RegionDto {
  name: string;
  areas: Area[];
}

export interface Area {
  name: string;
  creatures: Creature[];
}
