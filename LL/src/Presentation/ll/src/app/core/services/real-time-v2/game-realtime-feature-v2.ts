export function isGameRealtimeV2Enabled(): boolean {
  const env = (window as any).env;
  return env?.gameSignalRV2Enabled !== 'false';
}
