import { environment } from '../../../../../environments/environment';

export function isGameRealtimeEnabled(): boolean {
  const env = (window as any).env;
  return env?.gameSignalREnabled !== 'false';
}

export function isProductionRuntime(): boolean {
  const runtimeEnvironment =
    typeof window === 'undefined' ? undefined : (window as any).env?.environment;
  return (
    environment.production ||
    environment.environment === 'prod' ||
    runtimeEnvironment === 'prod'
  );
}
