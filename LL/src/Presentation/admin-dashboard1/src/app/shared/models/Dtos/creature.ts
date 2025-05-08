import { AttributeDto } from './attributesDto';

export interface Creature {
  id: string;
  name: string;
  level: number;
  experienceReward: number;
  baseAttributes: AttributeDto[];
}
