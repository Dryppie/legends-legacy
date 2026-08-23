import { CharacterDto } from '../../../../shared/models/Dtos/characterDto';
import { applyCharacterLevelUp } from './character-state.service';

describe('CharacterStateService level-up synchronization', () => {
  it('applies the authoritative level-up progress to the current character', () => {
    const character = createCharacter();

    const updated = applyCharacterLevelUp(character, {
      characterId: character.id,
      level: 3,
      experience: 42,
      experienceUntilNextLevel: 1_896,
      unlockedEssenceSlots: 1,
    });

    expect(updated).toEqual({
      ...character,
      level: 3,
      experience: 42,
      experienceUntilNextLevel: 1_896,
    });
  });

  it('does not let an older level-up event overwrite newer local progress', () => {
    const character = createCharacter({
      level: 3,
      experience: 75,
      experienceUntilNextLevel: 1_896,
    });

    const updated = applyCharacterLevelUp(character, {
      characterId: character.id,
      level: 3,
      experience: 42,
      experienceUntilNextLevel: 1_896,
      unlockedEssenceSlots: 1,
    });

    expect(updated).toBe(character);
  });
});

function createCharacter(
  overrides: Partial<CharacterDto> = {},
): CharacterDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 2,
    experience: 467,
    experienceUntilNextLevel: 475,
    cinders: 0,
    soulstones: 0,
    fateEcho: 0,
    sigilFragments: 0,
    guildFavor: 0,
    arenaRating: 0,
    ...overrides,
  };
}
