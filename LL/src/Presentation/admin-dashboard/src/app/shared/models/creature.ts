import { AttributeType } from './enums/attributeType';

export interface Creature {
  id: number;
  name: string;
  level: number;
  experienceReward: number;
  baseAttributes: AttributeDto[];
}

export interface AttributeDto {
  attributeType: AttributeType;
  value: number;
}
