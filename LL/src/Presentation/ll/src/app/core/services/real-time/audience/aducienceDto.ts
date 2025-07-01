export type AudienceDto =
  | { kind: 'World' } // ‹— match record names exactly
  | { kind: 'Guild'; guildId: string };
