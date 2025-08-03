import { AttributeType } from '../enums/attributeType';

export interface AttributeDto {
  attributeType: AttributeType;
  value: number;
}

export interface AttributeModifier {
  attributeType: AttributeType;
  amount: number;
  modifierType: ModifierType;
}

export enum ModifierType {
  Flat = 'Flat',
  Additive = 'Additive',
  Multiplicative = 'Multiplicative',
}
