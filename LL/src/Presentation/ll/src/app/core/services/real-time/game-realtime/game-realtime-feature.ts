export function isGameRealtimeEnabled(): boolean {
  const env = (window as any).env;
  return env?.gameSignalREnabled !== 'false';
}
