import { AttributeType } from './enums/attributeType';

export interface Creature {
  id: number;
  name: string;
  baseAttributes: AttributeDto[];
}

export interface AttributeDto {
  attributeType: AttributeType;
  value: number;
}
