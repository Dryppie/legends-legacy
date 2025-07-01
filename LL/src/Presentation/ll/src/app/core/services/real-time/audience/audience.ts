export type Audience = CharacterAudience | GuildAudience | WorldAudience;

export interface CharacterAudience {
  type: 'character';
  characterId: string; // Guid as string
}

export interface GuildAudience {
  type: 'guild';
  guildId: string; // Guid as string
}

export interface WorldAudience {
  type: 'world';
}
