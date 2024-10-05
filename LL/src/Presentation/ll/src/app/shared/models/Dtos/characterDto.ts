import { AttributeDto } from './attributesDto';

export interface CharacterDto {
  id?: string;
  name?: string;
  level?: number;
  experience?: number;
  experienceUntilNextLevel?: number;
  gold?: number;
  rawAttributes?: Array<AttributeDto>;
  attributes?: Array<AttributeDto>;
}
