const env = (window as any).env ?? {};
const runtimeText = (value: unknown, fallback = ''): string => {
  if (typeof value !== 'string' || value.startsWith('${')) {
    return fallback;
  }

  return value;
};

const runtimeUriText = (value: unknown, fallback = ''): string => {
  const encodedValue = runtimeText(value);
  if (!encodedValue) {
    return fallback;
  }

  try {
    return decodeURIComponent(encodedValue.replace(/\+/g, ' '));
  } catch {
    return fallback;
  }
};

export const environment = {
  environment: env.environment as 'dev' | 'test' | 'prod',
  apiBaseUrl: env.apiBaseUrl,
  chatApiRoot: env.chatApiRoot,
  production: false,
  features: {
    raids: env.environment !== 'prod',
  },
  googleClientId: env.googleClientId,
  isLocal: env.isLocal === 'true',
  maintenance: {
    enabled: env.maintenanceEnabled === 'true',
    message: runtimeUriText(
      env.maintenanceMessage,
      "Legend's Legacy is undergoing maintenance.",
    ),
    expectedBack: runtimeUriText(env.maintenanceExpectedBack),
  },
  // apiUrl: 'https://localhost:7060/api/v1/',
  login: {
    uri: '',
  },
  legendsLegacyWebsite: {
    base: '',
  },
  errorMessage:
    'ERROR! Something went wrong! Please send a ticket to support and attach a screenshot. Thank you!',
  baseDuration: 10,
};
