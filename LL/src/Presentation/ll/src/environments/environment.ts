const env = (window as any).env;
export const environment = {
  environment: env.environment as 'dev' | 'test' | 'prod',
  apiBaseUrl: env.apiBaseUrl,
  chatApiRoot: env.chatApiRoot,
  production: false,
  googleClientId: env.googleClientId,
  isLocal: env.isLocal === 'true',
  // apiUrl: 'https://localhost:7060/api/v1/',
  login: {
    uri: '',
  },
  legendsLegacyWebsite: {
    base: '',
  },
  errorMessage:
    'ERROR! Something went wrong! Please send a ticket to support and attach a screenshot. Thank you!',
  baseDuration: 6,
};
