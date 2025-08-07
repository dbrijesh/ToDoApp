export const environment = {
  production: true,
  
  // App Configuration
  appName: getEnvVar('APP_NAME', 'Hello World App'),
  appVersion: getEnvVar('APP_VERSION', '1.0.0'),
  
  // SSL Configuration
  sslEnabled: getEnvVar('SSL_ENABLED', 'true') === 'true',
  sslCertValidityDays: parseInt(getEnvVar('SSL_CERT_VALIDITY_DAYS', '365')),
  
  // Server Configuration
  serverPort: parseInt(getEnvVar('SERVER_PORT', '443')),
  serverHost: getEnvVar('SERVER_HOST', 'localhost'),
  
  // Logging
  logLevel: getEnvVar('LOG_LEVEL', 'warn'),
  
  // Feature Flags
  enableHealthCheck: getEnvVar('ENABLE_HEALTH_CHECK', 'true') === 'true',
  enableMetrics: getEnvVar('ENABLE_METRICS', 'true') === 'true',
  
  // API Configuration (if needed in future)
  apiUrl: getEnvVar('API_URL', ''),
  apiTimeout: parseInt(getEnvVar('API_TIMEOUT', '30000'))
};

// Helper function to get environment variables with defaults
function getEnvVar(name: string, defaultValue: string): string {
  // In Angular, environment variables are embedded at build time
  // For runtime environment variables, you'd need to fetch from a config endpoint
  return (globalThis as any)[`__ENV_${name}__`] || defaultValue;
}