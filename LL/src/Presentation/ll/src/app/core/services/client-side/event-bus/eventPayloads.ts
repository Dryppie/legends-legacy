export interface EventPayloads {
  'colosseum-combat-finished': {
    outcome: 'Victory' | 'Defeat' | 'Draw';
  } | null;
  // Add more events here...
}
