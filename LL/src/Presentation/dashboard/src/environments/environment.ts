interface RuntimeEnvironment {
  environment?: 'dev' | 'test' | 'prod';
  apiBaseUrl?: string;
  isLocal?: string;
}

const env = ((window as typeof window & { env?: RuntimeEnvironment }).env ?? {});
export const environment = {
  environment: env.environment ?? 'dev',
  apiBaseUrl: env.apiBaseUrl ?? '',
  production: false,
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
