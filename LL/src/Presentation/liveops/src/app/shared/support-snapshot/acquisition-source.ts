// Stored source IDs predate the public equipment terminology. Never display them verbatim.
export function equipmentSourceLabel(value: string | null | undefined, fallback = 'Rewards'): string {
  if (!value) return fallback;
  const source = value.replace(/^(?:model[-_]?e|equipment)[:.]/i, '');
  const names: Record<string, string> = {
    starter: 'Starter equipment',
    ordinary: 'Combat reward',
    'protected-dungeon': 'Protected dungeon reward',
    'dungeon-completion': 'Dungeon completion',
    salvage: 'Salvage',
    'admin-compensation': 'Administrator compensation',
  };
  return names[source] ?? source.replace(/[:_-]/g, ' ').replace(/\b\w/g, (character) => character.toUpperCase());
}
