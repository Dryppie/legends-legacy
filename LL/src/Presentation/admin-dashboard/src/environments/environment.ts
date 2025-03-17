const env = (window as any).env;
export const environment = {
  environment: env.environment as 'dev',
  apiBaseUrl: env.apiBaseUrl,
  isLocal: env.isLocal === 'true',
};
