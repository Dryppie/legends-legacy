import { AttributeType } from './enums/attributeType';

export interface Creature {
  id: string;
  name: string;
  level: number;
  experienceReward: number;
  baseAttributes: AttributeDto[];
}

export interface AttributeDto {
  entityId: string;
  attributeType: AttributeType;
  value: number;
}
